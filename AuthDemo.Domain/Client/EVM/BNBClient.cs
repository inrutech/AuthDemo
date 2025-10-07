using System;
using System.Numerics;
using AuthDemo.Core;
using AuthDemo.Core.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nethereum.ABI.Model;
using Nethereum.Contracts;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Zero.Core.Attribute;
using Zero.Core.Inject;

namespace AuthDemo.Domain.Client.EVM;

[Inject(OptionsLifetime = ServiceLifetime.Singleton)]
public partial class BNBClient
{
    private readonly IServiceProvider _services;

    /// <summary>
    /// 获取主机环境
    /// </summary>
    protected IHostEnvironment HostEnvironment => _services.GetService<IHostEnvironment>()!;

    /// <summary>
    /// 构造函数，初始化 BNBClient。
    /// </summary>
    /// <param name="service">服务提供器。</param>
    public BNBClient(IServiceProvider service)
    {
        _services = service;
        TransferEvent = CacheGetTransferEvent();
    }

    /// <summary>
    /// Web3 客户端实例。
    /// </summary>
    private Web3? _web3 { get; set; }

    /// <summary>
    /// 转账事件签名。
    /// </summary>
    public string TransferEvent { get; set; }


    /// <summary>
    /// 链配置。
    /// </summary>
    public ChainConfig ChainConfig { get; set; }

    /// <summary>
    /// Web 缓存。
    /// </summary>
    protected WebCache WebCache => _services.GetService<WebCache>()!;

    /// <summary>
    /// 获取私有 Web3 客户端。
    /// </summary>
    /// <param name="privateKey">私钥。</param>
    /// <returns>Web3 客户端。</returns>
    protected Web3 CacheGetPrivateClient(string privateKey)
    {
        string key = WebCache.RenderKey(Params.APP_KEY, "PrivateClient", privateKey);
        return WebCache.Get(key, () =>
        {
            Account account = new(privateKey, ChainConfig.ChainId);

            // 创建 HttpClientHandler，处理 SSL 验证
            HttpClientHandler handler = new()
            {
                // 如果使用自签名证书（开发环境），请取消注释以下行
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            HttpClient httpClient = new(handler)
            {
                // 使用 HttpClient 的 SslProtocols 属性来设置 TLS 协议
                DefaultRequestVersion = new Version(2, 0),
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
            };

            var rpc = ChainConfig.RpcUrl;
            RpcClient rpcClient = new(new Uri(rpc!), httpClient);

            Web3 web3 = new(account, rpcClient);

            return web3;
        });
    }
    /// <summary>
    /// 获取 Web3 客户端。
    /// </summary>
    /// <returns>Web3 客户端。</returns>
    protected Web3 GetWeb3()
    {
        if (_web3 == null)
        {
            HttpClientHandler handler = new()
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
            HttpClient httpClient = new(handler)
            {
                // 使用 HttpClient 的 SslProtocols 属性来设置 TLS 协议
                DefaultRequestVersion = new Version(2, 0),
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
            };

            var rpc = ChainConfig.RpcUrl;
            RpcClient rpcClient = new(new Uri(rpc!), httpClient);
            _web3 = new Web3(rpcClient);
        }

        return _web3;
    }

    private string CacheGetTransferEvent()
    {
        string key = WebCache.RenderKey(Params.APP_KEY, "Event", nameof(TransferEvent));
        return WebCache.Get(key, () =>
        {
            EventABI? transferEventABI = Event<TransferEventDTO>.GetEventABI();
            return $"0x{transferEventABI.Sha3Signature}";
        });
    }

    private async Task<BigInteger> CacheGetGasPriceAsync()
    {
        string key = WebCache.RenderKey(Params.APP_KEY, "GasPrice");
        return await WebCache.Get(key, async () =>
        {
            HexBigInteger? gasPrice = await GetWeb3().Eth.GasPrice.SendRequestAsync();
            return gasPrice?.Value ?? 0;
        });
    }
}
