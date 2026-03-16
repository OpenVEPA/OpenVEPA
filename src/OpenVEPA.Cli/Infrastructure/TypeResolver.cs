using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace OpenVEPA.Cli.Infrastructure;

/// <summary>
/// Resolves types from the application service provider, falling back
/// to <see cref="ActivatorUtilities"/> for types not registered in the container.
/// </summary>
internal sealed class TypeResolver : ITypeResolver, IDisposable
{
    private readonly IServiceProvider _provider;
    private readonly ConcurrentDictionary<Type, object> _instances;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeResolver"/> class.
    /// </summary>
    /// <param name="provider">The application service provider.</param>
    /// <param name="instances">Additional instances registered through the registrar.</param>
    public TypeResolver(
        IServiceProvider provider,
        ConcurrentDictionary<Type, object> instances)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _instances = instances ?? throw new ArgumentNullException(nameof(instances));
    }

    /// <inheritdoc />
    public object? Resolve(Type? type)
    {
        if (type is null)
        {
            return null;
        }

        if (_instances.TryGetValue(type, out var instance))
        {
            return instance;
        }

        return _provider.GetService(type)
            ?? ActivatorUtilities.CreateInstance(_provider, type);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // The IServiceProvider lifetime is owned by the WebApplication host.
    }
}
