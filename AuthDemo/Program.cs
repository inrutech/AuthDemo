using AuthDemo;
using Zero.Core.Extensions;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

var builder = WebApplication.CreateBuilder(args);

Startup.ConfigureServices(builder);

var app = builder.Build();

Startup.ConfigureApplication(app);

// 获取 ILogger 实例
var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogCustom("Start.", "Program");

app.Lifetime.ApplicationStopping.Register(() =>
{
    logger.LogCustom("End.", "Program");
});

app.Map("/", () => "Hello World!");

app.Run();