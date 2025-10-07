using System;

namespace AuthDemo.Core.Config;

public class ChainConfig
{
    /// <summary>
    /// 链ID
    /// </summary>
    public long? ChainId { get; set; }

    /// <summary>
    /// RPC地址
    /// </summary>
    public string? RpcUrl { get; set; }

    /// <summary>
    /// 合约地址
    /// </summary>
    public string? ContractAddress { get; set; }

}
