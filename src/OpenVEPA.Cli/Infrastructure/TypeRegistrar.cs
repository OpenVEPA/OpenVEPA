using System.Collections.Concurrent;
using Spectre.Console.Cli;

namespace OpenVEPA.Cli.Infrastructure;

/// <summary>
/// Bridges the Microsoft.Extensions.DependencyInjection container
/// to Spectre.Console.Cli's type registration system.
/// </summary>
internal sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceProvider _provider;
    private readonly ConcurrentDictionary<Type, object> _instances = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeRegistrar"/> class.
    /// </summary>
    /// <param name="provider">The application service provider.</param>
    public TypeRegistrar(IServiceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <inheritdoc />
    public ITypeResolver Build()
    {
        return new TypeResolver(_provider, _instances);
    }

    /// <inheritdoc />
    public void Register(Type service, Type implementation)
    {
        // Command types are resolved via ActivatorUtilities in TypeResolver.
    }

    /// <inheritdoc />
    public void RegisterInstance(Type service, object implementation)
    {
        _instances[service] = implementation;
    }

    /// <inheritdoc />
    public void RegisterLazy(Type service, Func<object> factory)
    {
        _instances[service] = factory();
    }
}
