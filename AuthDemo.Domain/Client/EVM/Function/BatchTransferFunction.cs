using System;
using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace AuthDemo.Domain.Client.EVM.Function;

/// <summary>
/// 批量转账函数
/// </summary>
[Function("batchTransfer")]
public class BatchTransferFunction : FunctionMessage
{
    /// <summary>
    /// 接收者地址列表
    /// </summary>
    [Parameter("address[]", "recipients")]
    public List<string> Recipients { get; set; } = new();

    /// <summary>
    /// 转账金额
    /// </summary>
    [Parameter("uint256", "value", 2)]
    public BigInteger Value { get; set; }
}