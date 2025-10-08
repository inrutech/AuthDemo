using System;
using System.Reflection;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using Nethereum.Util;
using Zero.Core.Extensions;
using Nethereum.ABI.Model;
using AuthDemo.Core.Helper;
using Nethereum.Contracts;

namespace AuthDemo.Domain.Client.TVM;

public partial class TronClient
{
    public async Task<bool> CallFunctionAsync<T>(T function, string contractAddress, string ownerAddress, string ownerPrivateKey, long feeLimit = 10_000_000) where T : FunctionMessage
    {

        var encoder = new FunctionCallEncoder();
        var fullEncoded = encoder.EncodeRequest(function, string.Empty);
        var parameterData = fullEncoded[2..];

        var functionSignature = ChainHelper.GetFunctionSignature(function);

        var response = await TriggerSmartContractAsync(
            ownerAddress: ownerAddress,
            contractAddress: contractAddress,
            functionSelector: functionSignature,
            parameter: parameterData,
            feeLimit: feeLimit
        );

        if (response.OccurError || response.Result?.Transaction == null)
        {
            return false;
        }

        var signHex = ChainHelper.SignHex(response.Result.Transaction.RawDataHex, ownerPrivateKey);

        var response2 = await BroadcastTransactionAsync(response.Result.Transaction, signHex);
        if (response2.OccurError)
        {
            return false;
        }

        return true;
    }
}
