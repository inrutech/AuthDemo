using System;
using Zero.Core.Extensions;
using Zero.Core.Result;

namespace AuthDemo.Domain.Client.TVM;

public partial class TronClient
{
    public Task<SysResult<TriggerSmartContractInfo>> TriggerSmartContractAsync(string ownerAddress, string contractAddress, string functionSelector, string parameter, long feeLimit)
    {
        Dictionary<string, object?> args = new()
        {
            ["owner_address"] = ownerAddress,
            ["contract_address"] = contractAddress,
            ["function_selector"] = functionSelector,
            ["parameter"] = parameter,
            ["fee_limit"] = feeLimit
        };

        string url = RenderUrl("/wallet/triggersmartcontract");

        return PostAsync<TriggerSmartContractInfo>(url, args);
    }

    public Task<SysResult<BroadcastTransactionInfo>> BroadcastTransactionAsync(Transaction transaction, string signature)
    {
        // 直接构造对象使 JSON 字段与 Tron 预期匹配
        var obj = new
        {
            visible = transaction.Visible,
            txID = transaction.TxId,
            raw_data = transaction.RawData,
            raw_data_hex = transaction.RawDataHex,
            signature = new[] { signature }
        };
        string url = RenderUrl("/wallet/broadcasttransaction");
        _logger?.LogCustom($"Broadcast request payload: {obj.ToJson()}", _logPath);
        return PostAsync<BroadcastTransactionInfo>(url, obj.ToJson().DeserializeJson<Dictionary<string, object?>>());
    }
}
