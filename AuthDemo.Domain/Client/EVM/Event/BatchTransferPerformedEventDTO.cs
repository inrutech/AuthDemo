using System;
using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AuthDemo.Domain.Client.EVM.Event;

[Event("BatchTransferPerformed")]
public class BatchTransferPerformedEventDTO : IEventDTO
{
    /// <summary>
    /// 发送方地址
    /// </summary>
    [Parameter("address", "from", 1, true)]
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// 接收方地址
    /// </summary>
    [Parameter("address", "to", 2, true)]
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// 代币合约地址
    /// </summary>
    [Parameter("address", "token", 3, true)]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 转账金额
    /// </summary>
    [Parameter("uint256", "value", 4, false)]
    public BigInteger Value { get; set; }

    /// <summary>
    /// 业务ID
    /// </summary>
    [Parameter("uint256", "businessId", 5, false)]
    public BigInteger BusinessId { get; set; }
}