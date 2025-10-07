using System;
using AuthDemo.Core;
using Nethereum.Contracts;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Zero.Core.Util;

namespace AuthDemo.Domain.Client.EVM;

public partial class BNBClient
{
    /// <summary>
    ///     执行方法
    /// </summary>
    /// <param name="privateKey">The encrypted private key for the transaction sender.</param>
    /// <param name="contractAddress">The address of the contract to call.</param>
    /// <param name="functionMessage">The function message containing call parameters.</param>
    /// <typeparam name="T">The type of the function message.</typeparam>
    /// <returns>The transaction receipt after execution.</returns>
    public async Task<TransactionReceipt> CallFunctionAsync<T>(string privateKey, string contractAddress,
        T functionMessage) where T : FunctionMessage, new()
    {
        string? pk = CryptoHelper.AES_Decrypt(privateKey, Params.AES_KEY);
        Web3? web3 = CacheGetPrivateClient(pk);
        ContractHandler handler = web3.Eth.GetContractHandler(contractAddress);
        HexBigInteger estimate = await handler.EstimateGasAsync(functionMessage);
        functionMessage.Gas = estimate;
        functionMessage.GasPrice = await CacheGetGasPriceAsync();
        return await handler.SendRequestAndWaitForReceiptAsync(functionMessage);
    }
}
