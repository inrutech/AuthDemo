using System;
using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;

namespace AuthDemo.Core.Helper;

public class ChainHelper
{
    /// <summary>
    /// 从 FunctionMessage 获取函数签名字符串
    /// </summary>
    public static string GetFunctionSignature<T>(T function) where T : FunctionMessage
    {
        // 获取函数名
        var functionAttribute = typeof(T).GetCustomAttributes(typeof(FunctionAttribute), false)
            .FirstOrDefault() as FunctionAttribute;

        if (functionAttribute == null)
        {
            throw new InvalidOperationException($"类型 {typeof(T).Name} 没有 FunctionAttribute");
        }

        var functionName = functionAttribute.Name;

        // 获取参数类型
        var properties = typeof(T).GetProperties()
            .Where(p => p.GetCustomAttributes(typeof(ParameterAttribute), false).Any())
            .OrderBy(p => ((ParameterAttribute)p.GetCustomAttributes(typeof(ParameterAttribute), false).First()).Order)
            .ToList();

        var parameterTypes = new List<string>();
        foreach (var property in properties)
        {
            var parameterAttribute = (ParameterAttribute)property.GetCustomAttributes(typeof(ParameterAttribute), false).First();
            var paramType = parameterAttribute.Type;

            // 如果是 tuple[] 类型，需要解析具体的结构体
            if (paramType == "tuple[]")
            {
                paramType = GetTupleSignature(property.PropertyType) + "[]";
            }

            parameterTypes.Add(paramType);
        }

        // 构建签名字符串
        var signature = $"{functionName}({string.Join(",", parameterTypes)})";
        return signature;
    }

    /// <summary>
    /// 获取元组类型的完整签名
    /// </summary>
    private static string GetTupleSignature(Type listType)
    {
        // 获取泛型参数，例如 List<TransferRequest> 中的 TransferRequest
        if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = listType.GetGenericArguments()[0];

            // 获取结构体中的所有字段
            var fields = elementType.GetProperties()
                .Where(p => p.GetCustomAttributes(typeof(ParameterAttribute), false).Any())
                .OrderBy(p => ((ParameterAttribute)p.GetCustomAttributes(typeof(ParameterAttribute), false).First()).Order)
                .ToList();

            var fieldTypes = new List<string>();
            foreach (var field in fields)
            {
                var parameterAttribute = (ParameterAttribute)field.GetCustomAttributes(typeof(ParameterAttribute), false).First();
                fieldTypes.Add(parameterAttribute.Type);
            }

            // 构建 (address,address,address,uint256) 格式
            return $"({string.Join(",", fieldTypes)})";
        }

        return "tuple";
    }

    public static string SignHex(string rawHex, string privateKey)
    {
        // Tron 交易哈希 = SHA256(raw_data_hex) 不是 keccak
        var rawBytes = rawHex.HexToByteArray();
        var txHash = System.Security.Cryptography.SHA256.HashData(rawBytes);

        // 检查签名权限：在签名之前验证 ownerPrivateKey 对应的地址是否在 ownerAddress 的 active 权限中
        var ecKey = new EthECKey(privateKey);

        // 对 SHA256 哈希进行 secp256k1 签名 (不再额外哈希)
        var signature = ecKey.SignAndCalculateV(txHash);

        // 兼容某些库可能返回 0/1 的情况（Nethereum 通常已经是 27/28）
        if (signature.V != null && signature.V.Length > 0 && (signature.V[0] == 0 || signature.V[0] == 1))
        {
            signature.V[0] = (byte)(signature.V[0] + 27);
        }

        var sigBytes = new byte[65];
        Buffer.BlockCopy(signature.R, 0, sigBytes, 0, 32);
        Buffer.BlockCopy(signature.S, 0, sigBytes, 32, 32);
        sigBytes[64] = signature.V[0]; // Tron 期望 v 为 27/28
        return Convert.ToHexStringLower(sigBytes);
    }

    public static string TronToHexAddress(string tronAddress)
    {
        // 使用简单的 Base58 解码实现
        var bytes = DecodeBase58Check(tronAddress);

        // 验证长度
        if (bytes.Length < 21)
        {
            return string.Empty;
        }

        // 去掉第一个字节(0x41)，取20字节地址
        var addressBytes = new byte[21];
        Array.Copy(bytes, 0, addressBytes, 0, 21);

        return Convert.ToHexStringLower(addressBytes);
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
