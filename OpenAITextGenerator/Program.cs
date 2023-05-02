using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAITextGenerator.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(s =>
    {
        s.AddSingleton<IRedisServices, RedisServices>();
        s.AddTransient<IOpenAPIService, OpenAPIService>();
    })
    .Build();

host.Run();
