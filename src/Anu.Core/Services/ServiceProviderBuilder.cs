using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Anu.Core.Services;

public class ServiceProviderBuilder
{
    public static ServiceProvider? ServiceProvider { get; private set; }
    private static readonly IServiceCollection _services = new ServiceCollection();

    public ServiceProviderBuilder AddSingleton<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _services.AddSingleton<TService, TImplementation>();
        return this;
    }

    public ServiceProviderBuilder AddSingleton<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]TService>() where TService : class
    {
        _services.AddSingleton<TService>();
        return this;
    }

    public ServiceProviderBuilder AddTransient<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _services.AddTransient<TService, TImplementation>();
        return this;
    }
    
    public void Build()
    {
        ServiceProvider = _services.BuildServiceProvider();
    }
}