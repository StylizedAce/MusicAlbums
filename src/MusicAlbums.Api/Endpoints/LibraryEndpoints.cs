using Microsoft.AspNetCore.Http.HttpResults;
using MusicAlbums.Api.Contracts;
using MusicAlbums.Core.Abstractions;

namespace MusicAlbums.Api.Endpoints;

public static class LibraryEndpoints
{
    public static IEndpointRouteBuilder MapLibraryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users/{userName}/library").WithTags("Library");

        group.MapGet("/", async (
            string userName,
            ILibraryService library,
            CancellationToken cancellationToken) =>
        {
            var albums = await library.GetLibraryAsync(userName, cancellationToken);
            return TypedResults.Ok(albums.Select(album => album.ToResponse()).ToArray());
        })
        .WithName("GetUserLibrary");

        group.MapGet("/{albumId:guid}", async (
            string userName,
            Guid albumId,
            ILibraryService library,
            CancellationToken cancellationToken) =>
        {
            var album = await library.GetAlbumAsync(userName, albumId, cancellationToken);
            return TypedResults.Ok(album.ToResponse());
        })
        .WithName("GetUserLibraryAlbum");

        group.MapPost("/", async Task<Results<Created<SavedAlbumResponse>, ValidationProblem>> (
            string userName,
            SaveAlbumRequest request,
            ILibraryService library,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.ProviderAlbumId))
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.ProviderAlbumId)] = ["The album identifier is required."]
                });
            }

            var savedAlbum = await library.AddAlbumAsync(userName, request.Provider, request.ProviderAlbumId, cancellationToken);
            var location = $"/api/users/{Uri.EscapeDataString(userName)}/library/{savedAlbum.Id}";

            return TypedResults.Created(location, savedAlbum.ToResponse());
        })
        .WithName("AddAlbumToLibrary");

        group.MapDelete("/{albumId:guid}", async (
            string userName,
            Guid albumId,
            ILibraryService library,
            CancellationToken cancellationToken) =>
        {
            await library.RemoveAlbumAsync(userName, albumId, cancellationToken);
            return TypedResults.NoContent();
        })
        .WithName("RemoveAlbumFromLibrary");

        return app;
    }
}
