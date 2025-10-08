using System;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace AuthDemo.Domain.Client.TVM;

public partial class TriggerSmartContractInfo
{
    [JsonPropertyName("result")] public Result Result { get; set; }
    [JsonPropertyName("transaction")] public Transaction Transaction { get; set; }
}

public partial class Result
{
    [JsonPropertyName("result")] public bool ResultResult { get; set; }
}

public partial class Transaction
{
    [JsonPropertyName("visible")] public bool Visible { get; set; }
    [JsonPropertyName("txID")] public string TxId { get; set; }
    [JsonPropertyName("raw_data")] public RawData RawData { get; set; }
    [JsonPropertyName("raw_data_hex")] public string RawDataHex { get; set; }
}

public partial class RawData
{
    [JsonPropertyName("contract")] public List<Contract> Contract { get; set; }
    [JsonPropertyName("ref_block_bytes")] public string RefBlockBytes { get; set; }
    [JsonPropertyName("ref_block_hash")] public string RefBlockHash { get; set; }
    [JsonPropertyName("expiration")] public long Expiration { get; set; }
    [JsonPropertyName("fee_limit")] public long FeeLimit { get; set; }
    [JsonPropertyName("timestamp")] public long Timestamp { get; set; }
}

public partial class Contract
{
    [JsonPropertyName("parameter")] public Parameter Parameter { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; }
}

public partial class Parameter
{
    [JsonPropertyName("value")] public Value Value { get; set; }
    [JsonPropertyName("type_url")] public string TypeUrl { get; set; }
}

public partial class Value
{
    [JsonPropertyName("data")] public string Data { get; set; }
    [JsonPropertyName("owner_address")] public string OwnerAddress { get; set; }
    [JsonPropertyName("contract_address")] public string ContractAddress { get; set; }
}