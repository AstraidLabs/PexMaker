using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace PexMaker.Cli.Infrastructure;

internal sealed class TypeResolver : ITypeResolver, IDisposable
{
    private readonly ServiceProvider _provider;

    public TypeResolver(ServiceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public object? Resolve(Type? type)
    {
        return type is null ? null : _provider.GetService(type);
    }

    public void Dispose()
    {
        _provider.Dispose();
    }
}
