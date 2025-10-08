using System;

namespace AuthDemo.Domain.Client.TVM;

using System.Text.Json.Serialization;
using Newtonsoft.Json;

public class BroadcastTransactionInfo
{
    [JsonPropertyName("txid")] public string? Txid { get; set; }
    [JsonPropertyName("result")] public bool Result { get; set; }
    [JsonPropertyName("code")] public string? Code { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    // 某些节点错误可能返回 Error 字段
    [JsonPropertyName("Error")] public string? Error { get; set; }
}
