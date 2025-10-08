using System;

namespace AuthDemo.Domain.Client.TVM;

using Newtonsoft.Json;

public class BroadcastTransactionInfo
{
    [JsonProperty("txid")] public string? Txid { get; set; }
    [JsonProperty("result")] public bool Result { get; set; }
    [JsonProperty("code")] public string? Code { get; set; }
    [JsonProperty("message")] public string? Message { get; set; }
    // 某些节点错误可能返回 Error 字段
    [JsonProperty("Error")] public string? Error { get; set; }
}
