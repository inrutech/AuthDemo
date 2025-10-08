using System;
using System.Diagnostics.Contracts;
using System.Numerics;
using AuthDemo.Domain.Client.EVM.Function;
using AuthDemo.Domain.Client.EVM.Struct;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts; // <-- 关键！FunctionMessage 在这里           // FunctionMessage 的基类
using Nethereum.ABI.FunctionEncoding;
using Zero.Core.Result;
using Zero.Core.Extensions;
using System.Text;
using Org.BouncyCastle.Utilities.Encoders;
using Nethereum.Signer.Crypto;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Contracts.QueryHandlers.MultiCall;
using AuthDemo.Domain.Client.TVM;
using Nethereum.Signer;
using Nethereum.Util;

namespace AuthDemo.Service.Admin;

public class TestService(IServiceProvider provider) : BaseService(provider)
{
    private const string TronApi = "https://api.shasta.trongrid.io";
    public async Task<SysResult<bool>> TestAsync()
    {
        try
        {
            // � 验证你的合约地址转换
            var yourContractTronAddr = "TB956Pr3ATuLSVAi5fffzMLPK4m74xSJfY";
            // 21 字节 (含 41 前缀) 的合约地址 hex（用于 Tron HTTP API owner_address / contract_address 字段）
            var yourContractHex41 = TronBase58ToHex41(yourContractTronAddr); // 长度 42, 以 41 开头
                                                                             // 测试其他地址
            var fromAddress41 = TronBase58ToHex41("TSNDLSQQ1oehM6EbDoSR3fRCF129bypKC7");
            var toAddress41 = TronBase58ToHex41("TNh4gyJ3kwRE3NNH4DHG9o6gzBgqNJrk6a");
            var fromAddress = fromAddress41[2..]; // 20 bytes for ABI
            var toAddress = toAddress41[2..];

            // 🔥 尝试调用你实际部署的合约！
            var function = new BatchTransferFunction
            {
                Requests =
                [
                    new() {
                        From = "0x" + fromAddress,
                        To = "0x" + toAddress,
                        Token = "0x" + TronToEth("TC2p2VDDafzqTUePmZ2Q8XXD2eBqmU6wPG"), // TRC20 token 仍然只需要20字节
                        Amount = new BigInteger(1000_000 * 1_000_000_000_000) // 1000 USDT (9位小数)
                    }
                ]
            };

            // �🚀 实际调用你的合约
            var success = await TronClient.CallFunctionAsync(
                function,
                yourContractHex41, // 41 前缀合约地址
                "batchTransferToken((address,address,address,uint256)[])",
                fromAddress41, // 传入 owner 也使用41前缀
                "0a45b108a63be72571041b1ff92ad73df2cb8fe8a06be316ad85b4526c66bfef"
            );

            return Result(success);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"测试异常: {ex.Message}");
            return Result(false);
        }
    }

    // Tron Base58 地址 → 41前缀 Hex(42字符，不含0x)
    private static string TronBase58ToHex41(string base58)
    {
        var bytes = DecodeBase58Check(base58); // 返回含 version + payload (20) + checksum 已被裁剪? 我们当前实现返回全数据
        if (bytes.Length < 21)
            throw new FormatException("Invalid Tron address bytes");
        // 规范：第一个字节应为 0x41
        var version = bytes[0];
        if (version != 0x41)
            throw new FormatException($"Unexpected Tron version byte: 0x{version:X2}");
        // 取前 21 字节（version + 20 payload）
        var core = new byte[21];
        Array.Copy(bytes, 0, core, 0, 21);
        return Convert.ToHexString(core).ToLower();
    }

    // Tron 地址 → 20字节 EVM 风格（旧函数名保留供参数编码使用）
    private static string TronToEth(string tronAddr)
    {
        try
        {
            // 使用简单的 Base58 解码实现
            var bytes = DecodeBase58Check(tronAddr);

            // 验证长度
            if (bytes.Length < 21)
                throw new FormatException("Invalid Tron address length");

            // 去掉第一个字节(0x41)，取20字节地址
            var addressBytes = new byte[20];
            Array.Copy(bytes, 1, addressBytes, 0, 20);

            return Convert.ToHexStringLower(addressBytes);
        }
        catch (Exception ex)
        {
            throw new FormatException($"Failed to convert Tron address {tronAddr} to Ethereum format: {ex.Message}", ex);
        }
    }

    // 简单的 Base58Check 解码实现
    private static byte[] DecodeBase58Check(string input)
    {
        const string alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        var decoded = BigInteger.Zero;
        var multi = BigInteger.One;

        // 从右到左处理每个字符
        for (int i = input.Length - 1; i >= 0; i--)
        {
            int digit = alphabet.IndexOf(input[i]);
            if (digit < 0)
                throw new FormatException($"Invalid Base58 character: {input[i]}");
            decoded += multi * digit;
            multi *= 58;
        }

        // 转换为字节数组
        var bytes = decoded.ToByteArray();

        // 处理前导零
        int leadingZeros = 0;
        for (int i = 0; i < input.Length && input[i] == '1'; i++)
            leadingZeros++;

        // BigInteger 是小端序，需要反转
        if (bytes[bytes.Length - 1] == 0)
        {
            // 移除 BigInteger 添加的额外零字节
            Array.Resize(ref bytes, bytes.Length - 1);
        }
        Array.Reverse(bytes);

        // 添加前导零
        if (leadingZeros > 0)
        {
            var result = new byte[bytes.Length + leadingZeros];
            Array.Copy(bytes, 0, result, leadingZeros, bytes.Length);
            return result;
        }

        return bytes;
    }

}
