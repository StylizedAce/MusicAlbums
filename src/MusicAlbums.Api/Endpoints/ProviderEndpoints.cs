using MusicAlbums.Api.Contracts;
using MusicAlbums.Core.Abstractions;

namespace MusicAlbums.Api.Endpoints;

public static class ProviderEndpoints
{
    public static IEndpointRouteBuilder MapProviderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/providers").WithTags("Providers");

        group.MapGet("/", (IAlbumProviderFactory factory) =>
        {
            var defaultProviderName = factory.GetDefault().Name;
            var providers = factory.GetAll()
                .Select(provider => new ProviderResponse(
                    provider.Name,
                    string.Equals(provider.Name, defaultProviderName, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            return TypedResults.Ok(providers);
        })
        .WithName("ListAlbumProviders");

        return app;
    }
}
