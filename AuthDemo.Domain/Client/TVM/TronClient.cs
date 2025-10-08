using System;
using System.Net;
using System.Numerics;
using System.Text;
using AuthDemo.Domain.Client.EVM.Function;
using AuthDemo.Domain.Client.EVM.Struct;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.ABI.Model;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Model;
using Nethereum.Signer;
using Nethereum.Util;
using Newtonsoft.Json.Linq;
using Zero.Core.Attribute;
using Zero.Core.Extensions;
using Zero.Core.Result;
using System.Linq;

namespace AuthDemo.Domain.Client.TVM;

[Inject]
public partial class TronClient(IServiceProvider service)
{
    /// <summary>
    /// 日志·数
    /// </summary>
    public string _logPath = $"Client/{nameof(TronClient)}";

    /// <summary>
    /// 日志事件
    /// </summary>
    public ILogger<TronClient>? _logger => service.GetService<ILogger<TronClient>>();

    /// <summary>
    /// HTTP 客户端设置
    /// </summary>
    public IHttpClientFactory? _httpClientFactory => service.GetService<IHttpClientFactory>();

    private HttpClient CreateHttpClient()
    {
        HttpClient client = _httpClientFactory!.CreateClient();
        return client;
    }

    private SysResult<T> ErrorResult<T>(string message)
    {
        return new SysResult<T> { Code = ErrorCode.SYS_FAIL, ErrorDesc = message };
    }

    /// <summary>
    /// 设备名ͽ请求的첽请求
    /// </summary>
    /// <typeparam name="T">请求设置</typeparam>
    /// <param name="message">HTTP 响应消息</param>
    /// <returns>设置ͽ数</returns>
    protected virtual async Task<SysResult<T>> ResultAsync<T>(HttpResponseMessage message) where T : class
    {
        string r = await message.Content.ReadAsStringAsync();
        _logger?.LogCustom($"result:{r}", _logPath);
        if (message.StatusCode != HttpStatusCode.OK)
        {
            return ErrorResult<T>(message.StatusCode.ToString());
        }
        return new SysResult<T> { Code = ErrorCode.SYS_SUCCESS, Result = r.DeserializeJson<T>() };
    }

    /// <summary>
    /// GET 请求
    /// </summary>
    /// <typeparam name="T">请求请求</typeparam>
    /// <param name="url">请求 URL</param>
    /// <returns>设备名</returns>
    public async Task<SysResult<T>> GetAsync<T>(string url) where T : class
    {
        try
        {
            if (url.IsNullOrEmpty())
            {
                return ErrorResult<T>("URL不能为空");
            }

            HttpClient client = CreateHttpClient();
            HttpResponseMessage result = await client.GetAsync(url);
            _logger?.LogCustom($"url:{url}", _logPath);
            return await ResultAsync<T>(result);
        }
        catch (Exception ex)
        {
            _logger?.LogFail(ex);
            return ErrorResult<T>(ex.Message);
        }
    }

    /// <summary>
    /// POST 设置󣨷数ط数ͽ请求
    /// </summary>
    /// <typeparam name="T">请求请求</typeparam>
    /// <param name="url">请求 URL</param>
    /// <param name="args">请求设置</param>
    /// <returns>设备名</returns>
    public async Task<SysResult<T>> PostAsync<T>(string url, Dictionary<string, object?>? args = null) where T : class
    {
        try
        {
            if (url.IsNullOrEmpty())
            {
                return ErrorResult<T>("URL不能为空");
            }

            HttpClient client = CreateHttpClient();
            StringContent content = new(args?.ToJson() ?? string.Empty, Encoding.UTF8, "application/json");
            HttpResponseMessage result = await client.PostAsync(url, content);
            _logger?.LogCustom($"url:{url}\r\ncontent:{content.ToJson()}", _logPath);
            return await ResultAsync<T>(result);
        }
        catch (Exception ex)
        {
            _logger?.LogFail(ex);
            return ErrorResult<T>(ex.Message);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    protected string RenderUrl(string action)
    {
        // 🔧 修复：使用 Shasta 测试网而不是主网
        var url = $"https://api.shasta.trongrid.io{action}";

        return url;
    }

}