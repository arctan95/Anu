using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Anu.Core.Services;

public class ServiceProviderBuilder
{
    public static ServiceProvider? ServiceProvider { get; private set; }
    private static readonly IServiceCollection Services = new ServiceCollection();

    public ServiceProviderBuilder AddSingleton<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        Services.AddSingleton<TService, TImplementation>();
        return this;
    }

    public ServiceProviderBuilder AddSingleton<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]TService>() where TService : class
    {
        Services.AddSingleton<TService>();
        return this;
    }

    public ServiceProviderBuilder AddTransient<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        Services.AddTransient<TService, TImplementation>();
        return this;
    }
    
    public void Build()
    {
        ServiceProvider = Services.BuildServiceProvider();
    }
}