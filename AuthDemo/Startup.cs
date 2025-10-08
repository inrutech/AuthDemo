using System;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.OpenApi.Models;
using NLog.Web;
using Scalar.AspNetCore;
using Zero.Core.Extensions;
using Zero.Core.Util;

namespace AuthDemo;

/// <summary>
/// 
/// </summary>
public class Startup
{
    private static readonly Dictionary<string, string> ApiArray = new()
    {
        ["admin"] = "管理后台 API",
        ["other"] = "其他应用 API",
    };

    private static void ConfigureBaseServices(WebApplicationBuilder builder)
    {
        builder.Host.UseNLog();

        builder.Services
            .AddResponseCaching()
            .AddMemoryCache()
            .AddHttpClient()
            .AddHttpContextAccessor()
            .AddSingleton<FileExtensionContentTypeProvider>();

        builder.Services.Configure<ApiBehaviorOptions>(options =>
            options.SuppressModelStateInvalidFilter = true);

        builder.Services.Configure<FormOptions>(options =>
        {
            options.ValueLengthLimit = int.MaxValue;
            options.MultipartBodyLengthLimit = int.MaxValue;
        });

        builder.Services.AddCors(option => option.AddPolicy("cors", policy => policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(_ => true)));
    }

    private static void ConfigureDataAccess(WebApplicationBuilder builder)
    {
        // builder.Services.AddDbContext<DBContext>(options =>
        // {
        //     options.UseNpgsql(builder.Configuration.GetConnectionString("db"),
        //         sqlOptions =>
        //         {
        //             sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
        //             sqlOptions.CommandTimeout(30);
        //         });
        // });
    }

    private static void ConfigureApiServices(WebApplicationBuilder builder)
    {
        StartupHelper.AddApiFilter(builder);
        StartupHelper.AddSwaggerGen(builder, ApiArray);


        // if (builder.Environment.IsProduction())
        // {
        //     builder.Services.AddTronSharp(x =>
        //                 {
        //                     x.Network = TronNetwork.MainNet;
        //                     x.Channel = new GrpcChannelOption { Host = "grpc.trongrid.io", Port = 50051 };
        //                     x.SolidityChannel = new GrpcChannelOption { Host = "grpc.trongrid.io", Port = 50052 };
        //                     x.ProApiKey = "44e143f3-acb2-49ef-9739-a40ce7b8ec42";
        //                 });
        // }
        // else
        // {
        //     builder.Services.AddTronSharp(x =>
        //                 {
        //                     x.Network = TronNetwork.TestNet;
        //                     x.Channel = new GrpcChannelOption { Host = "grpc.shasta.trongrid.io", Port = 50051 };
        //                     x.SolidityChannel = new GrpcChannelOption { Host = "grpc.shasta.trongrid.io", Port = 50052 };
        //                     x.ProApiKey = "44e143f3-acb2-49ef-9739-a40ce7b8ec42";
        //                 });
        // }
    }

    private static void RegisterApplicationModules(WebApplicationBuilder builder)
    {
        builder.Services.AddZeroNetCoreAssembly();

        builder.Services.AddAssembly("AuthDemo.Domain");

        builder.Services.AddAssembly("AuthDemo.Service");

        #region 
        // builder.Services.AddHostedService<AdminLogWorker>();
        #endregion
    }

    /// <summary>
    ///     注册服务
    /// </summary>
    /// <param name="builder"></param>
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        // 基础服务配置
        ConfigureBaseServices(builder);

        // 数据访问层配置
        ConfigureDataAccess(builder);

        // API相关配置
        ConfigureApiServices(builder);

        // 应用模块注册
        RegisterApplicationModules(builder);
    }

    /// <summary>
    /// </summary>
    /// <param name="app"></param>
    public static void ConfigureApplication(WebApplication app)
    {

        var supportedCultures = new[] { "en", "zh-hant" };

        app.UseRequestLocalization(cultureOptions =>
        {
            cultureOptions.AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures)
                .SetDefaultCulture(supportedCultures[0]);
            cultureOptions.FallBackToParentCultures = true;
        });

        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.All
        });

        app.UseCors("cors");
        app.UseResponseCaching();

        app.UseExceptionHandler(StartupHelper.HandlerException);
        app.UseStatusCodePages(StartupHelper.HandlerStatusCode);

        if (!app.Environment.IsProduction())
        {
            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
                {
                    // 优先取 X-Forwarded-Proto 头，否则用 httpReq.Scheme
                    var scheme = httpReq.Headers.TryGetValue("X-Forwarded-Proto", out var proto)
                        ? proto.ToString()
                        : httpReq.Scheme;
                    var baseUrl = $"{scheme}://{httpReq.Host}";
                    swaggerDoc.Servers =
                    [
                        new OpenApiServer { Url = baseUrl, Description = "当前服务器" }
                    ];
                });
            });

            app.MapScalarApiReference(options =>
            {
                options.AddDocuments(ApiArray.Select(x =>
                    new ScalarDocument(x.Key, x.Value, $"{x.Key}/api.json")
                ));
            });
        }

        app.UseDefaultFiles(new DefaultFilesOptions
        {
            DefaultFileNames = { "index.html" }
        });

        app.UseStaticFiles();
        app.UseRouting();
        app.MapDefaultControllerRoute();
    }
}
