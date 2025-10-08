using System;
using System.Numerics;
using AuthDemo.Domain.Client.EVM.Struct;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace AuthDemo.Domain.Client.EVM.Function;


/// <summary>
/// 批量转账函数
/// </summary>
[Function("batchTransferToken")]
public class BatchTransferTokenFunction : FunctionMessage
{
    [Parameter("tuple[]", "requests", 1)]
    public List<TransferRequest> Requests { get; set; }
}