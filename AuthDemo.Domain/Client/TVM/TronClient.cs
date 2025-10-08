using System;
using System.Net;
using System.Numerics;
using System.Text;
using AuthDemo.Domain.Client.EVM.Function;
using AuthDemo.Domain.Client.EVM.Struct;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.ABI.Model;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Model;
using Nethereum.Signer;
using Nethereum.Util;
using Newtonsoft.Json.Linq;
using Zero.Core.Attribute;
using Zero.Core.Extensions;
using Zero.Core.Result;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace AuthDemo.Domain.Client.TVM;

[Inject]
public partial class TronClient(IServiceProvider service)
{
    /// <summary>
    /// 日志·数
    /// </summary>
    public string _logPath = $"Client/{nameof(TronClient)}";

    /// <summary>
    /// 日志事件
    /// </summary>
    public ILogger<TronClient>? _logger => service.GetService<ILogger<TronClient>>();

    /// <summary>
    /// HTTP 客户端设置
    /// </summary>
    public IHttpClientFactory? _httpClientFactory => service.GetService<IHttpClientFactory>();

    private HttpClient CreateHttpClient()
    {
        HttpClient client = _httpClientFactory!.CreateClient();
        return client;
    }

    private SysResult<T> ErrorResult<T>(string message)
    {
        return new SysResult<T> { Code = ErrorCode.SYS_FAIL, ErrorDesc = message };
    }

    // 记录最近一次原始 JSON 响应，供 fallback 使用
    internal string? _lastRawJson; // 若 SysResult 内部也带 RawJson，则以 SysResult.RawJson 优先
    // 最近一次成功广播的交易ID（供外部查询）
    public string? LastBroadcastTxId { get; internal set; }

    /// <summary>
    /// 设备名ͽ请求的첽请求
    /// </summary>
    /// <typeparam name="T">请求设置</typeparam>
    /// <param name="message">HTTP 响应消息</param>
    /// <returns>设置ͽ数</returns>
    protected virtual async Task<SysResult<T>> ResultAsync<T>(HttpResponseMessage message) where T : class
    {
        string r = await message.Content.ReadAsStringAsync();
    _logger?.LogCustom($"result:{r}", _logPath);
    _lastRawJson = r; // 兼容旧逻辑
        if (message.StatusCode != HttpStatusCode.OK)
        {
            return ErrorResult<T>(message.StatusCode.ToString());
        }

        // 检查 Tron 网络返回的错误
        if (r.Contains("\"code\":") && !r.Contains("\"code\":\"SUCCESS\""))
        {
            // 如果响应包含错误代码，解析错误信息
            try
            {
                var errorResponse = r.DeserializeJson<dynamic>();
                if (errorResponse?.result?.code != null)
                {
                    string errorCode = errorResponse.result.code.ToString();
                    string errorMessage = errorResponse.result.message?.ToString() ?? "";

                    // 尝试解码十六进制错误消息
                    if (!string.IsNullOrEmpty(errorMessage) && errorMessage.Length > 0 && errorMessage.All(c => "0123456789ABCDEFabcdef".Contains(c)))
                    {
                        try
                        {
                            var bytes = Convert.FromHexString(errorMessage);
                            var decodedMessage = System.Text.Encoding.UTF8.GetString(bytes);
                            _logger?.LogCustom($"Decoded error message: {decodedMessage}", _logPath);
                        }
                        catch
                        {
                            // 如果解码失败，使用原始消息
                        }
                    }

                    return ErrorResult<T>($"Tron Error: {errorCode} - {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogCustom($"Failed to parse error response: {ex.Message}", _logPath);
            }
        }

    return new SysResult<T> { Code = ErrorCode.SYS_SUCCESS, Result = r.DeserializeJson<T>() };
    }

    /// <summary>
    /// GET 请求
    /// </summary>
    /// <typeparam name="T">请求请求</typeparam>
    /// <param name="url">请求 URL</param>
    /// <returns>设备名</returns>
    public async Task<SysResult<T>> GetAsync<T>(string url) where T : class
    {
        try
        {
            if (url.IsNullOrEmpty())
            {
                return ErrorResult<T>("URL不能为空");
            }

            HttpClient client = CreateHttpClient();
            HttpResponseMessage result = await client.GetAsync(url);
            _logger?.LogCustom($"url:{url}", _logPath);
            return await ResultAsync<T>(result);
        }
        catch (Exception ex)
        {
            _logger?.LogFail(ex);
            return ErrorResult<T>(ex.Message);
        }
    }

    /// <summary>
    /// POST 设置󣨷数ط数ͽ请求
    /// </summary>
    /// <typeparam name="T">请求请求</typeparam>
    /// <param name="url">请求 URL</param>
    /// <param name="args">请求设置</param>
    /// <returns>设备名</returns>
    public async Task<SysResult<T>> PostAsync<T>(string url, Dictionary<string, object?>? args = null) where T : class
    {
        try
        {
            if (url.IsNullOrEmpty())
            {
                return ErrorResult<T>("URL不能为空");
            }

            HttpClient client = CreateHttpClient();
            StringContent content = new(args?.ToJson() ?? string.Empty, Encoding.UTF8, "application/json");
            HttpResponseMessage result = await client.PostAsync(url, content);
            _logger?.LogCustom($"url:{url}\r\ncontent:{content.ToJson()}", _logPath);
            return await ResultAsync<T>(result);
        }
        catch (Exception ex)
        {
            _logger?.LogFail(ex);
            return ErrorResult<T>(ex.Message);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    protected string RenderUrl(string action)
    {
        // 🔧 修复：使用 Shasta 测试网而不是主网
        var url = $"https://api.shasta.trongrid.io{action}";

        return url;
    }

    /// <summary>
    /// 查询交易基础信息（未确认/原始结构）
    /// </summary>
    public Task<SysResult<dynamic>> GetTransactionByIdAsync(string txid)
        => PostAsync<dynamic>("/wallet/gettransactionbyid", new Dictionary<string, object?> { ["value"] = txid });

    /// <summary>
    /// 查询交易执行结果（包含收据：fee, blockNumber, contractResult 等）
    /// </summary>
    public Task<SysResult<dynamic>> GetTransactionInfoByIdAsync(string txid)
        => PostAsync<dynamic>("/wallet/gettransactioninfobyid", new Dictionary<string, object?> { ["value"] = txid });

    // 解析 TRC20 Transfer 事件 (与 ERC20 相同的 topic0)
    public List<Trc20TransferLog> DecodeTrc20TransferLogs(string? receiptJson)
    {
        var list = new List<Trc20TransferLog>();
        if (string.IsNullOrWhiteSpace(receiptJson)) return list;
        try
        {
            var j = JObject.Parse(receiptJson);
            var logs = j["log"] as JArray;
            if (logs == null) return list;
            var transferTopic = "ddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef";
            foreach (var log in logs)
            {
                var topics = log["topics"] as JArray;
                if (topics == null || topics.Count < 3) continue;
                var topic0 = topics[0]?.ToString().Trim('"').ToLower();
                if (topic0 != null && topic0.StartsWith("0x")) topic0 = topic0[2..];
                if (topic0 != transferTopic) continue;
                // Indexed address topics 在 Tron 里 32 字节，取后 40 hex 作为地址
                string ExtractAddress(string? t)
                {
                    if (string.IsNullOrEmpty(t)) return string.Empty;
                    t = t.Trim('"');
                    if (t.StartsWith("0x")) t = t[2..];
                    if (t.Length >= 40) return t[^40..];
                    return t;
                }
                var from = ExtractAddress(topics[1]?.ToString());
                var to = ExtractAddress(topics[2]?.ToString());
                var dataHex = log["data"]?.ToString()?.Trim('"');
                if (string.IsNullOrEmpty(dataHex)) continue;
                if (dataHex.StartsWith("0x")) dataHex = dataHex[2..];
                // 金额 uint256
                try
                {
                    var value = System.Numerics.BigInteger.Parse("0" + dataHex, System.Globalization.NumberStyles.HexNumber);
                    list.Add(new Trc20TransferLog
                    {
                        From = from,
                        To = to,
                        Value = value.ToString(),
                        ValueBigInt = value,
                        ContractAddress = (log["address"]?.ToString() ?? string.Empty).Trim('"').Replace("0x","")
                    });
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogCustom($"DecodeTrc20TransferLogs error: {ex.Message}", _logPath);
        }
        return list;
    }
}

public class Trc20TransferLog
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty; // 十进制字符串
    public System.Numerics.BigInteger ValueBigInt { get; set; }
    public string ContractAddress { get; set; } = string.Empty; // 触发的 token 合约地址 (hex, 可能是 41 开头或 20 bytes)
}