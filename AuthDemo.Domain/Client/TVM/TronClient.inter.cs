using System;
using System.Reflection;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using Nethereum.Util;
using Zero.Core.Extensions;
using Nethereum.ABI.Model;

namespace AuthDemo.Domain.Client.TVM;

public partial class TronClient
{
    public async Task<bool> CallFunctionAsync<T>(T function, string contractAddress, string functionSignature, string ownerAddress, string ownerPrivateKey, long feeLimit = 10_000_000)
    {
        // // 计算函数选择器 (仅用于日志 / 调试)
        // var sha3 = new Sha3Keccack();
        // var hashBytes = sha3.CalculateHash(System.Text.Encoding.UTF8.GetBytes(functionSignature));
        // var selectorHex = Convert.ToHexStringLower(hashBytes.Take(4).ToArray());
        // _logger?.LogCustom($"预期函数选择器(keccak4)={selectorHex}", _logPath);

        string parameterData = "";
        try
        {
            if (function is AuthDemo.Domain.Client.EVM.Function.BatchTransferFunction batchFunction)
            {
                // 先详细日志列出每条请求，便于用户确认 from / to / token / amount
                LogBatchRequests(batchFunction);
                parameterData = EncodeBatchTransferParameters(batchFunction);
            }
            else
            {
                var encoder = new FunctionCallEncoder();
                var fullEncoded = encoder.EncodeRequest(function, functionSignature);
                _logger?.LogCustom($"Full encoded attempt: {fullEncoded}", _logPath);
                if (fullEncoded.StartsWith("0x") && fullEncoded.Length > 10)
                {
                    parameterData = fullEncoded[10..];
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogCustom($"Parameter encoding failed: {ex.Message}", _logPath);
            return false;
        }

        // 规范化 Tron 地址 (补 41 前缀) - 传入 wallet 接口需要 hex (不含 0x) 并含 41
        contractAddress = NormalizeTronHexAddress(contractAddress);
        ownerAddress = NormalizeTronHexAddress(ownerAddress);

        var response = await TriggerSmartContractAsync(
            ownerAddress: ownerAddress,
            contractAddress: contractAddress,
            functionSelector: functionSignature, // Tron 需要原始函数签名
            parameter: parameterData,
            feeLimit: feeLimit
        );

        if (response.OccurError || response.Result?.Transaction == null)
        {
            _logger?.LogCustom($"TriggerSmartContract failed: {response.ErrorDesc}", _logPath);
            return false;
        }
        // // 在签名前检查过期时间（避免 TRANSACTION_EXPIRATION_ERROR）
        // for (int attempt = 0; attempt < 2; attempt++)
        // {
        //     try
        //     {
        //         if (TryReadExpiration(_lastRawJson, out var expirationMs, out var timestampMs))
        //         {
        //             var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        //             var remain = expirationMs - nowMs;
        //             _logger?.LogCustom($"Expiration 检查: timestamp={timestampMs} expiration={expirationMs} now={nowMs} remain={remain}ms", _logPath);
        //             if (remain <= 0)
        //             {
        //                 _logger?.LogCustom("⚠️ 交易已过期（remain<=0），尝试重新触发获取新 transaction", _logPath);
        //                 response = await TriggerSmartContractAsync(
        //                     ownerAddress: ownerAddress,
        //                     contractAddress: contractAddress,
        //                     functionSelector: functionSignature,
        //                     parameter: parameterData,
        //                     feeLimit: feeLimit
        //                 );
        //                 if (response.OccurError || response.Result?.Transaction == null)
        //                 {
        //                     _logger?.LogCustom("重新触发失败，终止", _logPath);
        //                     return false;
        //                 }
        //                 continue; // 再次循环校验
        //             }
        //             else if (remain < 5_000)
        //             {
        //                 _logger?.LogCustom($"⚠️ 交易将在 {remain}ms 后过期，建议提高广播速度或立即重新触发", _logPath);
        //             }
        //         }
        //         else
        //         {
        //             _logger?.LogCustom("未能从原始 JSON 中解析 expiration/timestamp 字段", _logPath);
        //         }
        //         break; // 正常退出检测循环
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger?.LogCustom($"Expiration 检查异常: {ex.Message}", _logPath);
        //         break;
        //     }
        // }

        // if (string.IsNullOrEmpty(response.Result.Transaction.RawDataHex))
        // {
        //     _logger?.LogCustom("TriggerSmartContract 返回 raw_data_hex 为空，尝试直接解析原始 JSON fallback", _logPath);
        //     try
        //     {
        //         var rawJson = _lastRawJson; // SysResult 不再包含 RawJson 属性，仅使用内部缓存
        //         if (!string.IsNullOrEmpty(rawJson))
        //         {
        //             _logger?.LogCustom($"Fallback 原始 JSON 长度={rawJson.Length}", _logPath);
        //             // 直接用简单查找提取 raw_data_hex 值（避免再次失败）
        //             const string key = "\"raw_data_hex\":";
        //             var idx = rawJson.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        //             if (idx > 0)
        //             {
        //                 var start = idx + key.Length;
        //                 // 期望后面紧接一个引号
        //                 while (start < rawJson.Length && char.IsWhiteSpace(rawJson[start])) start++;
        //                 if (start < rawJson.Length && rawJson[start] == '"')
        //                 {
        //                     start++;
        //                     var end = rawJson.IndexOf('"', start);
        //                     if (end > start)
        //                     {
        //                         var hex = rawJson[start..end];
        //                         if (!string.IsNullOrEmpty(hex) && hex.Length > 10)
        //                         {
        //                             response.Result.Transaction.RawDataHex = hex;
        //                             _logger?.LogCustom($"Fallback 成功提取 raw_data_hex 长度={hex.Length} 前16={hex.Substring(0, Math.Min(16, hex.Length))}...", _logPath);
        //                         }
        //                         else
        //                         {
        //                             _logger?.LogCustom($"Fallback 提取到的 raw_data_hex 内容异常: '{hex}'", _logPath);
        //                         }
        //                     }
        //                     else
        //                     {
        //                         _logger?.LogCustom("Fallback 未找到结束引号或长度不足", _logPath);
        //                     }
        //                 }
        //                 else
        //                 {
        //                     _logger?.LogCustom("Fallback key 后未找到引号，放弃", _logPath);
        //                 }
        //             }
        //             else
        //             {
        //                 _logger?.LogCustom("原始 JSON 中未找到 raw_data_hex 关键字", _logPath);
        //             }
        //         }
        //         else
        //         {
        //             _logger?.LogCustom("_lastRawJson 为空，无法进行 fallback", _logPath);
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger?.LogCustom($"Fallback 解析 raw_data_hex 失败: {ex.Message}", _logPath);
        //     }
        //     if (string.IsNullOrEmpty(response.Result.Transaction.RawDataHex))
        //     {
        //         _logger?.LogCustom("仍然没有 raw_data_hex，终止", _logPath);
        //         return false;
        //     }
        // }
        // Tron 交易哈希 = SHA256(raw_data_hex) 不是 keccak
        var rawBytes = response.Result.Transaction.RawDataHex.HexToByteArray();
        byte[] txHash;
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            txHash = sha256.ComputeHash(rawBytes);
        }
        var computedTxId = Convert.ToHexStringLower(txHash);
        _logger?.LogCustom($"计算 txID={computedTxId} 原始数据长度={rawBytes.Length}", _logPath);

        // 触发返回里自带 txID，可用于校验
        // var returnedTxId = response.Result.Transaction.TxId?.ToLower();
        // if (!string.IsNullOrEmpty(returnedTxId) && returnedTxId != computedTxId)
        // {
        //     _logger?.LogCustom($"⚠️ txID 不一致: returned={returnedTxId} computed={computedTxId}", _logPath);
        // }
        // else
        // {
        //     _logger?.LogCustom($"txID: {computedTxId}", _logPath);
        // }

        // 对 SHA256 哈希进行 secp256k1 签名 (不再额外哈希)
        var ecKey = new EthECKey(ownerPrivateKey);
        var signature = ecKey.SignAndCalculateV(txHash);
        _logger?.LogCustom($"签名生成: R={Convert.ToHexStringLower(signature.R)} S={Convert.ToHexStringLower(signature.S)} V={Convert.ToHexStringLower(signature.V)}", _logPath);

        // 兼容某些库可能返回 0/1 的情况（Nethereum 通常已经是 27/28）
        if (signature.V != null && signature.V.Length > 0 && (signature.V[0] == 0 || signature.V[0] == 1))
        {
            signature.V[0] = (byte)(signature.V[0] + 27);
            _logger?.LogCustom($"Normalize v -> {signature.V[0]}", _logPath);
        }

        var sigBytes = new byte[65];
        Buffer.BlockCopy(signature.R, 0, sigBytes, 0, 32);
        Buffer.BlockCopy(signature.S, 0, sigBytes, 32, 32);
        sigBytes[64] = signature.V[0]; // Tron 期望 v 为 27/28
        string sigHex = Convert.ToHexStringLower(sigBytes);
        _logger?.LogCustom($"Signature(v={sigBytes[64]}): {sigHex}", _logPath);

        _logger?.LogCustom($"⏫ 准备广播交易 (txID={response.Result.Transaction.TxId})", _logPath);
        var response2 = await BroadcastTransactionAsync(response.Result.Transaction, sigHex);
        if (response2.OccurError)
        {
            _logger?.LogCustom($"BroadcastTransaction failed(HTTP/Wrapper): {response2.ErrorDesc}", _logPath);
            return false;
        }
        // 进一步检查链返回的业务字段
        if (response2.Result != null)
        {
            if (!string.IsNullOrEmpty(response2.Result.Error))
            {
                _logger?.LogCustom($"Broadcast 错误: {response2.Result.Error}", _logPath);
                return false;
            }
            if (response2.Result.Result == false && string.IsNullOrEmpty(response2.Result.Txid))
            {
                _logger?.LogCustom($"Broadcast 返回 result=false 且无 txid", _logPath);
                return false;
            }
            if (!string.IsNullOrEmpty(response2.Result.Txid))
            {
                LastBroadcastTxId = response2.Result.Txid;
                _logger?.LogCustom($"✅ 广播成功 txid: {response2.Result.Txid}", _logPath);
                // 立即尝试等待收据并解码日志 / 错误
                await FetchAndLogReceiptAsync(response2.Result.Txid);
                return true;
            }
        }
        // 如果结构未能解析，输出原始 JSON 已在 ResultAsync 里记录
        _logger?.LogCustom($"Broadcast 未解析出 txid，视为失败", _logPath);
        return false;
    }

    private static bool IsValidHex(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return false;
        return hex.All(c => "0123456789ABCDEFabcdef".Contains(c));
    }

    private static string NormalizeTronHexAddress(string addr)
    {
        if (string.IsNullOrWhiteSpace(addr)) return addr;
        var a = addr.Trim();
        if (a.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) a = a[2..];
        a = a.ToLower();
        if (a.Length == 40) a = "41" + a; // 补 41 前缀
        return a;
    }

    private string EncodeBatchTransferParameters(AuthDemo.Domain.Client.EVM.Function.BatchTransferFunction batchFunction)
    {
        try
        {
            // 手动构建 ABI 编码 - 动态数组格式
            // 对于 tuple[] 类型，需要：偏移量 -> 数组长度 -> 元素数据

            var requests = batchFunction.Requests ?? new List<AuthDemo.Domain.Client.EVM.Struct.TransferRequest>();

            var result = new List<string>();

            // 1. 数组偏移量 (32字节) - 动态数组从第0个位置开始，偏移量是0x20 (32)
            result.Add("0000000000000000000000000000000000000000000000000000000000000020");

            // 2. 数组长度 (32字节)
            result.Add(requests.Count.ToString("x").PadLeft(64, '0'));

            // 3. 每个数组元素的编码 (每个元素4个32字节字段)
            foreach (var request in requests)
            {
                // from address (32字节，左填充0)
                var fromHex = request.From?.StartsWith("0x", StringComparison.OrdinalIgnoreCase) == true ? request.From[2..] : request.From ?? "";
                fromHex = NormalizeAbiAddress(fromHex);
                result.Add(fromHex.PadLeft(64, '0'));

                // to address (32字节，左填充0)
                var toHex = request.To?.StartsWith("0x", StringComparison.OrdinalIgnoreCase) == true ? request.To[2..] : request.To ?? "";
                toHex = NormalizeAbiAddress(toHex);
                result.Add(toHex.PadLeft(64, '0'));

                // token address (32字节，左填充0)
                var tokenHex = request.Token?.StartsWith("0x", StringComparison.OrdinalIgnoreCase) == true ? request.Token[2..] : request.Token ?? "";
                tokenHex = NormalizeAbiAddress(tokenHex);
                result.Add(tokenHex.PadLeft(64, '0'));

                // amount (32字节，大端序)
                var amountHex = request.Amount.ToString("x").PadLeft(64, '0');
                result.Add(amountHex);
            }

            var finalResult = string.Join("", result);
            _logger?.LogCustom($"Manual encoding result: {finalResult}", _logPath);

            return finalResult;
        }
        catch (Exception ex)
        {
            _logger?.LogCustom($"Manual encoding failed: {ex.Message}", _logPath);
            return "";
        }
    }

    // 仅用于 ABI 中的 address 20 字节（去掉 41 前缀 / 0x）
    private string NormalizeAbiAddress(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        var h = raw.Trim().ToLower();
        if (h.StartsWith("0x")) h = h[2..];
        // 如果是 Tron 41 前缀（共 42 长度），去掉 41 取后 40 位
        if (h.Length == 42 && h.StartsWith("41")) h = h[2..];
        // 仅保留最后 40（防止意外多出前缀）
        if (h.Length > 40) h = h[^40..];
        if (h.Length != 40)
        {
            _logger?.LogCustom($"⚠️ ABI 地址长度异常({h.Length}) 原始={raw}", _logPath);
        }
        return h;
    }

    private void LogBatchRequests(AuthDemo.Domain.Client.EVM.Function.BatchTransferFunction batchFunction)
    {
        try
        {
            var list = batchFunction.Requests ?? new List<AuthDemo.Domain.Client.EVM.Struct.TransferRequest>();
            _logger?.LogCustom($"BatchTransfer 请求数: {list.Count}", _logPath);
            for (int i = 0; i < list.Count; i++)
            {
                var r = list[i];
                string from = r.From ?? "";
                string to = r.To ?? "";
                string token = r.Token ?? "";
                var amount = r.Amount;
                _logger?.LogCustom($"  [{i}] from={from} to={to} token={token} amount(raw)={amount}", _logPath);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogCustom($"LogBatchRequests 异常: {ex.Message}", _logPath);
        }
    }

    private async Task FetchAndLogReceiptAsync(string txid)
    {
        try
        {
            for (int i = 0; i < 8; i++)
            {
                await Task.Delay(1500);
                var receipt = await GetTransactionInfoByIdAsync(txid);
                if (receipt.Result == null)
                {
                    _logger?.LogCustom($"[Receipt] 第{i + 1}次: 暂无结果", _logPath);
                    continue;
                }
                var json = receipt.Result.ToJson();
                _logger?.LogCustom($"[Receipt] 第{i + 1}次获取 长度={json?.Length}", _logPath);
                if (json != null)
                {
                    if (json.Contains("\"receipt\""))
                    {
                        // 简单抓取 receipt.result
                        var statusIdx = json.IndexOf("\"result\"", StringComparison.OrdinalIgnoreCase);
                        if (statusIdx > 0)
                        {
                            _logger?.LogCustom($"[Receipt] 片段: {json.Substring(Math.Max(0, statusIdx - 40), Math.Min(160, json.Length - statusIdx + 40))}", _logPath);
                        }
                    }
                    // 尝试日志解码 TRC20
                    var transfers = DecodeTrc20TransferLogs(json);
                    if (transfers.Count > 0)
                    {
                        foreach (var t in transfers)
                        {
                            _logger?.LogCustom($"[TRC20 Transfer] token={t.ContractAddress} from={t.From} to={t.To} value={t.Value}", _logPath);
                        }
                    }
                    else
                    {
                        _logger?.LogCustom("[TRC20 Transfer] 未发现 Transfer 事件", _logPath);
                    }
                    // 尝试解码 contractResult 中的 revert 字符串
                    if (json.Contains("contractResult"))
                    {
                        // 粗略截取 contractResult 第一个元素
                        var crIdx = json.IndexOf("contractResult", StringComparison.OrdinalIgnoreCase);
                        var slice = json.Substring(crIdx, Math.Min(300, json.Length - crIdx));
                        _logger?.LogCustom($"[contractResult] 片段: {slice}", _logPath);
                        var revertReason = TryDecodeRevertString(json);
                        if (!string.IsNullOrEmpty(revertReason))
                        {
                            _logger?.LogCustom($"[RevertReason] {revertReason}", _logPath);
                        }
                    }
                }
                // 若已经包含 blockNumber 则认为完成
                if (json != null && json.Contains("blockNumber")) break;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogCustom($"FetchAndLogReceipt 异常: {ex.Message}", _logPath);
        }
    }

    private string? TryDecodeRevertString(string json)
    {
        try
        {
            // 查找 08c379a0 selector
            var sel = "08c379a0";
            var idx = json.IndexOf(sel, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            // 向后截取一段 hex
            var hexBuf = new System.Text.StringBuilder();
            for (int i = idx; i < json.Length && hexBuf.Length < 600; i++)
            {
                char c = json[i];
                if (Uri.IsHexDigit(c)) hexBuf.Append(c);
            }
            var hex = hexBuf.ToString();
            // 标准 ABI: 08c379a0 | 000000..20 | <len> | <data>
            var posLen = 8 + 64; // selector + offset
            if (hex.Length < posLen + 64) return null;
            var lenHex = hex.Substring(posLen, 64);
            if (!int.TryParse(lenHex.TrimStart('0'), System.Globalization.NumberStyles.HexNumber, null, out var strLen) || strLen <= 0) return null;
            var strStart = posLen + 64;
            var bytesHex = hex.Substring(strStart, Math.Min(strLen * 2, hex.Length - strStart));
            var bytes = Convert.FromHexString(bytesHex.PadRight(strLen * 2, '0'));
            return System.Text.Encoding.UTF8.GetString(bytes).Trim('\0');
        }
        catch { }
        return null;
    }

    private bool TryReadExpiration(string? rawJson, out long expiration, out long timestamp)
    {
        expiration = 0; timestamp = 0;
        if (string.IsNullOrEmpty(rawJson)) return false;
        try
        {
            // 简单查找 "expiration":<number>
            if (TryReadLongField(rawJson, "\"expiration\"", out expiration))
            {
                TryReadLongField(rawJson, "\"timestamp\"", out timestamp);
                return true;
            }
        }
        catch { }
        return false;
    }

    private bool TryReadLongField(string json, string key, out long value)
    {
        value = 0;
        var idx = json.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return false;
        idx += key.Length;
        // 跳过空白与冒号
        while (idx < json.Length && (char.IsWhiteSpace(json[idx]) || json[idx] == ':' || json[idx] == '"')) idx++;
        int start = idx;
        while (idx < json.Length && char.IsDigit(json[idx])) idx++;
        if (idx > start && long.TryParse(json[start..idx], out var parsed))
        {
            value = parsed; return true;
        }
        return false;
    }

    // ====== 解析 batchTransferToken calldata 工具 ======
    public class BatchTransferDecodedItem
    {
        public string FromHex { get; set; } = string.Empty;      // 20字节 hex (无 0x)
        public string ToHex { get; set; } = string.Empty;
        public string TokenHex { get; set; } = string.Empty;
        public System.Numerics.BigInteger Amount { get; set; }
    }

    /// <summary>
    /// 解码 batchTransferToken((address,address,address,uint256)[]) 的完整 calldata (含 selector)
    /// fullDataHex: 可能带 0x
    /// </summary>
    public List<BatchTransferDecodedItem> DecodeBatchTransferCalldata(string fullDataHex, bool log = true)
    {
        var list = new List<BatchTransferDecodedItem>();
        if (string.IsNullOrWhiteSpace(fullDataHex)) { if (log) _logger?.LogCustom("Decode 输入为空", _logPath); return list; }
        var hex = fullDataHex.Trim();
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex[2..];
        if (hex.Length < 8 + 64) { if (log) _logger?.LogCustom($"长度过短: {hex.Length}", _logPath); return list; }
        var selector = hex[..8];
        var paramArea = hex[8..];
        try
        {
            // 第一个 word 是动态数组偏移 (一般为 0x20)
            if (paramArea.Length < 64) { if (log) _logger?.LogCustom("paramArea 过短", _logPath); return list; }
            var offsetWord = paramArea[..64];
            var offsetBytes = ParseHexToInt(offsetWord);
            if (offsetBytes < 0 || offsetBytes % 32 != 0) { if (log) _logger?.LogCustom($"偏移异常 offset={offsetBytes}", _logPath); }
            // 偏移是相对 paramArea 起始的字节数
            var offsetHexPos = offsetBytes * 2; // 每字节2hex
            if (offsetHexPos + 64 > paramArea.Length) { if (log) _logger?.LogCustom("偏移指向越界", _logPath); return list; }
            var lengthWord = paramArea.Substring(offsetHexPos, 64);
            var arrayLen = ParseHexToInt(lengthWord);
            if (arrayLen < 0) { if (log) _logger?.LogCustom("数组长度解析失败", _logPath); return list; }
            if (log) _logger?.LogCustom($"[CalldataDecode] selector={selector} offset={offsetBytes} arrayLen={arrayLen}", _logPath);
            // 元素区开始位置
            var elementsStart = offsetHexPos + 64; // 跳过长度 word
            var elementWordCount = 4; // tuple 4 个 word
            var elementHexSize = elementWordCount * 64; // 256 hex chars
            for (int i = 0; i < arrayLen; i++)
            {
                var pos = elementsStart + i * elementHexSize;
                if (pos + elementHexSize > paramArea.Length) { if (log) _logger?.LogCustom($"元素 {i} 越界", _logPath); break; }
                var slice = paramArea.Substring(pos, elementHexSize);
                string Word(int w) => slice.Substring(w * 64, 64);
                var fromWord = Word(0);
                var toWord = Word(1);
                var tokenWord = Word(2);
                var amountWord = Word(3);
                var item = new BatchTransferDecodedItem
                {
                    FromHex = fromWord[^40..],
                    ToHex = toWord[^40..],
                    TokenHex = tokenWord[^40..],
                    Amount = ParseUint256(amountWord)
                };
                list.Add(item);
                if (log)
                {
                    _logger?.LogCustom($"  [Decoded {i}] from=41{item.FromHex} to=41{item.ToHex} token=41{item.TokenHex} amount={item.Amount}", _logPath);
                }
            }
        }
        catch (Exception ex)
        {
            if (log) _logger?.LogCustom($"Calldata 解码异常: {ex.Message}", _logPath);
        }
        return list;
    }

    private int ParseHexToInt(string word64)
    {
        try
        {
            var trimmed = word64.TrimStart('0');
            if (string.IsNullOrEmpty(trimmed)) return 0;
            if (trimmed.Length > 8) // 超出 int 范围但这里只用到小数字 (offset/length)
            {
                // 尝试解析为更大然后再 clamp
                if (int.TryParse(trimmed[^8..], System.Globalization.NumberStyles.HexNumber, null, out var tail)) return tail; // 取低位
            }
            if (int.TryParse(trimmed, System.Globalization.NumberStyles.HexNumber, null, out var val)) return val;
        }
        catch { }
        return -1;
    }

    private System.Numerics.BigInteger ParseUint256(string hex64)
    {
        if (string.IsNullOrWhiteSpace(hex64)) return System.Numerics.BigInteger.Zero;
        var h = hex64.Trim();
        // 保证只含 hex
        if (h.Length > 64) h = h[^64..];
        h = h.TrimStart('0');
        if (h.Length == 0) return System.Numerics.BigInteger.Zero;
        return System.Numerics.BigInteger.Parse("0" + h, System.Globalization.NumberStyles.HexNumber);
    }

    // ================= TRC20 辅助 ===================
    private static readonly Dictionary<string, int> _decimalsCache = new(); // tokenHex41 -> decimals

    private string Normalize41(string addr)
    {
        if (string.IsNullOrWhiteSpace(addr)) return addr;
        var a = addr.Trim();
        if (a.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) a = a[2..];
        a = a.ToLower();
        if (a.Length == 40) a = "41" + a;
        return a;
    }

    private string EncodeAddressParam32(string hex41)
    {
        if (string.IsNullOrWhiteSpace(hex41)) return new string('0', 64);
        var h = hex41.ToLower();
        if (h.StartsWith("0x")) h = h[2..];
        if (h.Length == 42 && h.StartsWith("41")) h = h[2..]; // strip 41
        if (h.Length > 40) h = h[^40..];
        return h.PadLeft(64, '0');
    }

    private string EncodeUintParam32(System.Numerics.BigInteger value)
    {
        if (value.Sign < 0) throw new ArgumentException("uint 参数不能为负");
        var hex = value.ToString("x");
        if (hex.Length > 64) hex = hex[^64..];
        return hex.PadLeft(64, '0');
    }

    private async Task<System.Numerics.BigInteger?> CallTrc20SingleUintResultAsync(string tokenHex41, string functionSignature, string paramData, string ownerHex41)
    {
        tokenHex41 = Normalize41(tokenHex41);
        ownerHex41 = Normalize41(ownerHex41);
        var resp = await TriggerSmartContractAsync(ownerHex41, tokenHex41, functionSignature, paramData, 0);
        if (resp.OccurError)
        {
            _logger?.LogCustom($"TRC20 {functionSignature} 调用失败: {resp.ErrorDesc}", _logPath);
            return null;
        }
        // constant_result 解析
        try
        {
            var raw = _lastRawJson;
            if (string.IsNullOrEmpty(raw)) return null;
            var key = "\"constant_result\"";
            var idx = raw.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            var arrStart = raw.IndexOf('[', idx);
            var arrEnd = raw.IndexOf(']', arrStart + 1);
            if (arrStart < 0 || arrEnd < 0) return null;
            var inner = raw.Substring(arrStart, arrEnd - arrStart + 1);
            // 取第一个字符串
            var q1 = inner.IndexOf('"');
            if (q1 < 0) return null;
            var q2 = inner.IndexOf('"', q1 + 1);
            if (q2 < 0) return null;
            var hex = inner.Substring(q1 + 1, q2 - q1 - 1).Trim();
            if (hex.StartsWith("0x")) hex = hex[2..];
            if (string.IsNullOrEmpty(hex)) return System.Numerics.BigInteger.Zero;
            if (hex.Length > 64) hex = hex[^64..];
            var bi = System.Numerics.BigInteger.Parse("0" + hex.TrimStart('0'), System.Globalization.NumberStyles.HexNumber);
            return bi;
        }
        catch (Exception ex)
        {
            _logger?.LogCustom($"解析 constant_result 失败: {ex.Message}", _logPath);
        }
        return null;
    }

    public async Task<int?> TRC20DecimalsAsync(string tokenHex41, string ownerHex41)
    {
        tokenHex41 = Normalize41(tokenHex41);
        if (_decimalsCache.TryGetValue(tokenHex41, out var cached)) return cached;
        var res = await CallTrc20SingleUintResultAsync(tokenHex41, "decimals()", "", ownerHex41);
        if (res.HasValue)
        {
            var dec = (int)res.Value;
            _decimalsCache[tokenHex41] = dec;
            _logger?.LogCustom($"[TRC20] decimals {tokenHex41} = {dec}", _logPath);
            return dec;
        }
        return null;
    }

    public async Task<System.Numerics.BigInteger?> TRC20BalanceOfAsync(string tokenHex41, string ownerHex41, string queryAddressHex41)
    {
        tokenHex41 = Normalize41(tokenHex41);
        ownerHex41 = Normalize41(ownerHex41);
        queryAddressHex41 = Normalize41(queryAddressHex41);
        var param = EncodeAddressParam32(queryAddressHex41);
        return await CallTrc20SingleUintResultAsync(tokenHex41, "balanceOf(address)", param, ownerHex41);
    }

    public async Task<System.Numerics.BigInteger?> TRC20AllowanceAsync(string tokenHex41, string ownerHex41, string ownerOfTokenHex41, string spenderHex41)
    {
        tokenHex41 = Normalize41(tokenHex41);
        ownerHex41 = Normalize41(ownerHex41);
        ownerOfTokenHex41 = Normalize41(ownerOfTokenHex41);
        spenderHex41 = Normalize41(spenderHex41);
        var p1 = EncodeAddressParam32(ownerOfTokenHex41);
        var p2 = EncodeAddressParam32(spenderHex41);
        var param = p1 + p2;
        return await CallTrc20SingleUintResultAsync(tokenHex41, "allowance(address,address)", param, ownerHex41);
    }

    public async Task<bool> TRC20ApproveAsync(string tokenHex41, string ownerAddressHex41, string ownerPrivateKey, string spenderHex41, System.Numerics.BigInteger amount, long feeLimit = 10_000_000)
    {
        tokenHex41 = Normalize41(tokenHex41);
        ownerAddressHex41 = Normalize41(ownerAddressHex41);
        spenderHex41 = Normalize41(spenderHex41);
        var pSpender = EncodeAddressParam32(spenderHex41);
        var pAmount = EncodeUintParam32(amount);
        var param = pSpender + pAmount;
        _logger?.LogCustom($"[Approve] token={tokenHex41} spender={spenderHex41} amount={amount}", _logPath);
        // 复用 CallFunctionAsync 泛型: 这里用动态对象只为通过类型约束，可再拆专用方法
        return await CallFunctionAsync<object>(new object(), tokenHex41, "approve(address,uint256)", ownerAddressHex41, ownerPrivateKey, feeLimit);
    }

    // 预检批量请求: 检查 balance & allowance
    public async Task PreflightBatchAsync(AuthDemo.Domain.Client.EVM.Function.BatchTransferFunction batch, string officialContractHex41, string queryCallerHex41)
    {
        officialContractHex41 = Normalize41(officialContractHex41);
        queryCallerHex41 = Normalize41(queryCallerHex41);
        var list = batch.Requests ?? new List<AuthDemo.Domain.Client.EVM.Struct.TransferRequest>();
        if (list.Count == 0)
        {
            _logger?.LogCustom("[Preflight] 无请求", _logPath); return;
        }
        _logger?.LogCustom($"[Preflight] 开始: {list.Count} 条", _logPath);
        // 先收集涉及 token & from
        var tokenSet = list.Select(r => Normalize41(r.Token ?? "")).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();
        var fromSet = list.Select(r => Normalize41(r.From ?? "")).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();
        var decimalsMap = new Dictionary<string, int>();
        foreach (var tk in tokenSet)
        {
            var dec = await TRC20DecimalsAsync(tk, queryCallerHex41) ?? 18; // 默认18
            decimalsMap[tk] = dec;
        }
        // 缓存 balance & allowance
        var balanceMap = new Dictionary<(string token, string owner), System.Numerics.BigInteger>();
        var allowanceMap = new Dictionary<(string token, string owner), System.Numerics.BigInteger>();
        foreach (var tk in tokenSet)
        {
            foreach (var fr in fromSet)
            {
                var bal = await TRC20BalanceOfAsync(tk, queryCallerHex41, fr) ?? System.Numerics.BigInteger.MinusOne;
                balanceMap[(tk, fr)] = bal;
                var allow = await TRC20AllowanceAsync(tk, queryCallerHex41, fr, officialContractHex41) ?? System.Numerics.BigInteger.MinusOne;
                allowanceMap[(tk, fr)] = allow;
            }
        }
        int ok = 0, insufficientBal = 0, insufficientAllow = 0;
        for (int i = 0; i < list.Count; i++)
        {
            var r = list[i];
            var token = Normalize41(r.Token ?? "");
            var from = Normalize41(r.From ?? "");
            var amount = r.Amount;
            decimalsMap.TryGetValue(token, out var decs);
            balanceMap.TryGetValue((token, from), out var bal);
            allowanceMap.TryGetValue((token, from), out var allow);
            double human;
            if (decs > 0)
            {
                var denom = System.Numerics.BigInteger.Pow(10, decs);
                // 使用 decimal 以避免精度丢失（当数值过大会回退 double）
                if (amount <= new System.Numerics.BigInteger(long.MaxValue) && denom <= new System.Numerics.BigInteger(long.MaxValue))
                {
                    human = (double)((decimal)(long)amount / (decimal)(long)denom);
                }
                else
                {
                    // 退化：取前若干位再缩放
                    var amountStr = amount.ToString();
                    if (amountStr.Length > decs)
                    {
                        var intPart = amountStr[..(amountStr.Length - decs)];
                        var fracPart = amountStr[(amountStr.Length - decs)..];
                        if (fracPart.Length > 6) fracPart = fracPart[..6];
                        human = double.Parse(intPart + "." + fracPart.TrimEnd('0').PadRight(1, '0'));
                    }
                    else
                    {
                        var frac = amountStr.PadLeft(decs, '0');
                        var intPart = "0";
                        var fracPart = frac;
                        if (fracPart.Length > 6) fracPart = fracPart[..6];
                        human = double.Parse(intPart + "." + fracPart.TrimEnd('0').PadRight(1, '0'));
                    }
                }
            }
            else human = (double)(amount > new System.Numerics.BigInteger(int.MaxValue) ? int.MaxValue : (int)amount);
            string status = "OK";
            if (bal < amount) { status = "INSUFFICIENT_BALANCE"; insufficientBal++; }
            if (allow < amount) { if (status == "OK") status = "INSUFFICIENT_ALLOWANCE"; else status += "+ALLOW"; insufficientAllow++; }
            if (status == "OK") ok++;
            _logger?.LogCustom($"[Preflight][{i}] token={token} from={from} to={r.To} amount={amount} (≈{human} / dec={decs}) bal={bal} allow={allow} => {status}", _logPath);
        }
        _logger?.LogCustom($"[Preflight] 汇总 OK={ok} 缺余额={insufficientBal} 缺授权={insufficientAllow}", _logPath);
    }

    // ============ Base58 ==========
    private static readonly char[] _b58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz".ToCharArray();
    public string ToBase58Address(string hex41)
    {
        if (string.IsNullOrWhiteSpace(hex41)) return hex41;
        var h = hex41.ToLower();
        if (h.StartsWith("0x")) h = h[2..];
        if (h.Length == 40) h = "41" + h; // 补
        if (h.Length != 42 || !h.StartsWith("41")) return hex41; // 非 Tron 直接返回
        var data = Convert.FromHexString(h);
        // Base58Check: payload=data, checksum=SHA256(SHA256(data)) 前4字节
        byte[] hash;
        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            hash = sha.ComputeHash(sha.ComputeHash(data));
        }
        var chk = hash.Take(4).ToArray();
        var all = new byte[data.Length + 4];
        Buffer.BlockCopy(data, 0, all, 0, data.Length);
        Buffer.BlockCopy(chk, 0, all, data.Length, 4);
        // 转 Base58
        var intData = new System.Numerics.BigInteger(all.Reverse().Concat(new byte[] { 0 }).ToArray());
        var sb = new System.Text.StringBuilder();
        while (intData > 0)
        {
            int rem = (int)(intData % 58);
            intData /= 58;
            sb.Append(_b58Alphabet[rem]);
        }
        // 处理前导零 (每个 0x00 -> '1')
        foreach (var b in all)
        {
            if (b == 0) sb.Append('1'); else break;
        }
        var b58 = new string(sb.ToString().Reverse().ToArray());
        return b58;
    }
}
