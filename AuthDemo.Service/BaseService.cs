using AuthDemo.Domain.Client.TVM;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Zero.Core.Attribute;
using Zero.Core.Extensions;
using Zero.Core.Inject;
using Zero.Core.Result;

namespace AuthDemo.Service;

[Inject]
public class BaseService(IServiceProvider provider)
{

    public IHttpClientFactory HttpClientFactory => provider.GetService<IHttpClientFactory>()!;

    public IHttpContextAccessor HttpContextAccessor => provider.GetService<IHttpContextAccessor>()!;

    // public DBContext DB => _provider.GetService<DBContext>()!;

    public WebCache WebCache => provider.GetService<WebCache>()!;

    public Snowflake Snowflake => provider.GetService<Snowflake>()!;

    public WebClient WebClient => provider.GetService<WebClient>()!;

    public TronClient TronClient => provider.GetService<TronClient>()!;

    protected SysResult<T> Result<T>(T? model)
    {
        return Result(model, ErrorCode.SYS_SUCCESS, null);
    }

    // protected SysResult<T1, T2> Result<T1, T2>(T1? model, T2 extend)
    // {
    //     return Result(model, ErrorCode.SYS_SUCCESS, null, extend);
    // }

    protected SysResult<T> Result<T>(T? model, ErrorCode code)
    {
        return Result(model, code, code.GetDescription());
    }

    protected SysResult<T> Result<T>(T? model, ErrorCode code, string? errorDesc)
    {
        return new SysResult<T>
        {
            Code = code,
            Result = model,
            ErrorDesc = errorDesc
        };
    }
}
