using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MusicAlbums.Core.Exceptions;

namespace MusicAlbums.Api.ErrorHandling;

internal sealed class MusicAlbumsExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<MusicAlbumsExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = Map(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogInformation("Request failed with {StatusCode}: {Message}", statusCode, exception.Message);
        }

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = exception.Message
            }
        });
    }

    private static (int StatusCode, string Title) Map(Exception exception) => exception switch
    {
        UnknownAlbumProviderException => (StatusCodes.Status400BadRequest, "Unknown album provider"),
        ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request"),
        AlbumProviderNotFoundException => (StatusCodes.Status404NotFound, "Album not found in provider catalog"),
        UserNotFoundException => (StatusCodes.Status404NotFound, "Library user not found"),
        AlbumNotInLibraryException => (StatusCodes.Status404NotFound, "Album not found in library"),
        AlbumAlreadyInLibraryException => (StatusCodes.Status409Conflict, "Album already in library"),
        AlbumProviderUnavailableException => (StatusCodes.Status502BadGateway, "Album provider unavailable"),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
    };
}
