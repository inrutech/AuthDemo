using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace AuthDemo.Domain.Client.TVM;

public partial class TriggerSmartContractInfo
{
    [JsonProperty("result"), JsonPropertyName("result")] public Result Result { get; set; } = new();
    [JsonProperty("transaction"), JsonPropertyName("transaction")] public Transaction Transaction { get; set; } = new();
}

public partial class Result
{
    [JsonProperty("result"), JsonPropertyName("result")] public bool ResultResult { get; set; }
}

public partial class Transaction
{
    [JsonProperty("visible"), JsonPropertyName("visible")] public bool Visible { get; set; }
    [JsonProperty("txID"), JsonPropertyName("txID")] public string TxId { get; set; } = string.Empty;
    [JsonProperty("raw_data"), JsonPropertyName("raw_data")] public RawData RawData { get; set; } = new();
    [JsonProperty("raw_data_hex"), JsonPropertyName("raw_data_hex")] public string RawDataHex { get; set; } = string.Empty;
}

public partial class RawData
{
    [JsonProperty("contract"), JsonPropertyName("contract")] public List<Contract> Contract { get; set; } = new();
    [JsonProperty("ref_block_bytes"), JsonPropertyName("ref_block_bytes")] public string RefBlockBytes { get; set; } = string.Empty;
    [JsonProperty("ref_block_hash"), JsonPropertyName("ref_block_hash")] public string RefBlockHash { get; set; } = string.Empty;
    [JsonProperty("expiration"), JsonPropertyName("expiration")] public long Expiration { get; set; }
    [JsonProperty("fee_limit"), JsonPropertyName("fee_limit")] public long FeeLimit { get; set; }
    [JsonProperty("timestamp"), JsonPropertyName("timestamp")] public long Timestamp { get; set; }
}

public partial class Contract
{
    [JsonProperty("parameter"), JsonPropertyName("parameter")] public Parameter Parameter { get; set; } = new();
    [JsonProperty("type"), JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
}

public partial class Parameter
{
    [JsonProperty("value"), JsonPropertyName("value")] public Value Value { get; set; } = new();
    [JsonProperty("type_url"), JsonPropertyName("type_url")] public string TypeUrl { get; set; } = string.Empty;
}

public partial class Value
{
    [JsonProperty("data"), JsonPropertyName("data")] public string Data { get; set; } = string.Empty;
    [JsonProperty("owner_address"), JsonPropertyName("owner_address")] public string OwnerAddress { get; set; } = string.Empty;
    [JsonProperty("contract_address"), JsonPropertyName("contract_address")] public string ContractAddress { get; set; } = string.Empty;
}