using Microsoft.AspNetCore.Http.HttpResults;
using MusicAlbums.Api.Contracts;
using MusicAlbums.Core.Abstractions;
using MusicAlbums.Core.Models;

namespace MusicAlbums.Api.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/albums").WithTags("Catalog");

        group.MapGet("/search", async Task<Results<Ok<SearchAlbumsResponse>, ValidationProblem>> (
            string? q,
            string? provider,
            int? limit,
            int? offset,
            IAlbumCatalogService catalog,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["q"] = ["The 'q' query parameter is required."]
                });
            }

            var query = new AlbumSearchQuery(q.Trim(), limit ?? 25, offset ?? 0);
            var result = await catalog.SearchAlbumsAsync(provider, query, cancellationToken);

            return TypedResults.Ok(result.ToResponse());
        })
        .WithName("SearchAlbums");

        group.MapGet("/{provider}/{providerAlbumId}", async (
            string provider,
            string providerAlbumId,
            IAlbumCatalogService catalog,
            CancellationToken cancellationToken) =>
        {
            var album = await catalog.GetAlbumAsync(provider, providerAlbumId, cancellationToken);
            return TypedResults.Ok(album.ToResponse());
        })
        .WithName("GetAlbum");

        return app;
    }
}
