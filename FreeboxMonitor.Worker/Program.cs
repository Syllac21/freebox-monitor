using FreeboxMonitor.Worker;
using FreeboxMonitor.Worker.Services;
using FreeboxMonitor.Worker.Models;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddHttpClient<FreeboxAuthService>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Freebox:BaseUrl"]!);
    });

builder.Services.AddHttpClient<FreeboxLanService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Freebox:BaseUrl"]!);
});
builder.Services.AddSingleton<DeviceEventRepository>();
builder.Services.AddHostedService<Worker>();

builder.Services.Configure<FreeboxOptions>(
    builder.Configuration.GetSection(FreeboxOptions.SectionName));

var host = builder.Build();
host.Run();
