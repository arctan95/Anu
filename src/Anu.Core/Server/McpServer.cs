using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Anu.Core.Services;
using Anu.Core.ViewModels;

namespace Anu.Core.Server;

public class McpServer
{
    private IHost _app;
    
    public McpServer()
    {
        var builder = Host.CreateEmptyApplicationBuilder(settings: null);

        builder.Services.AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();
        
        builder.Logging.AddConsole(options =>
        {
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });
        _app = builder.Build();
    }

    public async Task StartAsync()
    {
        await _app.RunAsync();
    }

    public async Task StopAsync()
    {
        await _app.StopAsync();
    }

}