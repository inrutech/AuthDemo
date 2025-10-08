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
using AuthDemo.Core.Helper;

namespace AuthDemo.Service.Admin;

public class TestService(IServiceProvider provider) : BaseService(provider)
{
    private const string TronApi = "https://api.shasta.trongrid.io";
    public async Task<SysResult<bool>> TestAsync()
    {
        try
        {
            var contractAddress = "TB956Pr3ATuLSVAi5fffzMLPK4m74xSJfY";
            var ownerAddress = "TNh4gyJ3kwRE3NNH4DHG9o6gzBgqNJrk6a";
            var ownerPrivateKey = "0a45b108a63be72571041b1ff92ad73df2cb8fe8a06be316ad85b4526c66bfef";
            var fromAddress = "TSNDLSQQ1oehM6EbDoSR3fRCF129bypKC7";
            var toAddress = "TNh4gyJ3kwRE3NNH4DHG9o6gzBgqNJrk6a";
            var tokenAddress = "TC2p2VDDafzqTUePmZ2Q8XXD2eBqmU6wPG"; // USDT TRC20

            // 🔥 尝试调用你实际部署的合约！
            var function = new BatchTransferFunction
            {
                Requests =
                [
                    new() {
                        From = "0x" + ChainHelper.TronToHexAddress(fromAddress)[2..], // 20字节以太坊格式地址
                        To = "0x" + ChainHelper.TronToHexAddress(toAddress)[2..],
                        Token = "0x" + ChainHelper.TronToHexAddress(tokenAddress)[2..], // TRC20 token 仍然只需要20字节
                        Amount = new BigInteger(1000_000 * 1_000_000_000_000) // 1000 USDT (9位小数)
                    }
                ]
            };

            // �🚀 实际调用你的合约
            var success = await TronClient.CallFunctionAsync(
                function,
                ChainHelper.TronToHexAddress(contractAddress),
                ChainHelper.TronToHexAddress(ownerAddress),
                ownerPrivateKey
            );

            return Result(success);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"测试异常: {ex.Message}");
            return Result(false);
        }
    }

}
