using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AuthDemo.Domain.Client.EVM.Struct;

/// <summary>
/// 转账请求结构体
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class TransferRequest
{
    /// <summary>
    /// 发送方地址
    /// </summary>
    [Parameter("address", "from")]
    public string? From { get; set; }

    /// <summary>
    /// 接收方地址
    /// </summary>
    [Parameter("address", "to", 2)]
    public string? To { get; set; }

    /// <summary>
    /// 设置Һ的Լ地址
    /// </summary>
    [Parameter("address", "token", 3)]
    public string? Token { get; set; }

    /// <summary>
    /// 转账金额
    /// </summary>
    [Parameter("uint256", "amount", 4)]
    public BigInteger Amount { get; set; }

    /// <summary>
    /// ҵ数ID
    /// </summary>
    [Parameter("uint256", "businessId", 5)]
    public BigInteger BusinessId { get; set; }
}
