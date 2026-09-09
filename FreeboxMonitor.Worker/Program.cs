using FreeboxMonitor.Worker;
using FreeboxMonitor.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddHttpClient<FreeboxAuthService>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Freebox:BaseUrl"]!);
    });
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
