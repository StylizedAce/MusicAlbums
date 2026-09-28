using System.Net;
using System.Text;

namespace MusicAlbums.Tests.Support;

internal static class SpotifyTestData
{
    public const string TokenJson = """
        { "access_token": "test-access-token", "token_type": "Bearer", "expires_in": 3600 }
        """;

    public const string SearchResponseJson = """
        {
          "albums": {
            "items": [
              {
                "id": "sp-1",
                "name": "Discovery",
                "release_date": "2001-03-07",
                "release_date_precision": "day",
                "total_tracks": 14,
                "images": [
                  { "url": "https://i.scdn.co/image/small-cover.jpg", "width": 64, "height": 64 },
                  { "url": "https://i.scdn.co/image/large-cover.jpg", "width": 640, "height": 640 }
                ],
                "artists": [ { "id": "a-1", "name": "Daft Punk" } ]
              },
              {
                "id": "sp-2",
                "name": "OK Computer",
                "release_date": "1997-05-28",
                "release_date_precision": "day",
                "total_tracks": 12,
                "images": [],
                "artists": [ { "id": "a-2", "name": "Radiohead" } ]
              }
            ],
            "total": 2
          }
        }
        """;

    public const string AlbumDetailJson = """
        {
          "id": "sp-1",
          "name": "Random Access Memories",
          "release_date": "2013-05",
          "release_date_precision": "month",
          "total_tracks": 13,
          "images": [
            { "url": "https://i.scdn.co/image/ram-cover.jpg", "width": 640, "height": 640 }
          ],
          "artists": [ { "id": "a-1", "name": "Daft Punk" } ],
          "tracks": {
            "items": [
              { "track_number": 1, "name": "Get Lucky", "duration_ms": 369000, "preview_url": "https://p.scdn.co/mp3-preview/get-lucky" },
              { "track_number": 2, "name": "Instant Crush", "duration_ms": 337000, "preview_url": null }
            ]
          }
        }
        """;

    public const string NotFoundErrorJson = """
        { "error": { "status": 404, "message": "Album not found" } }
        """;

    public const string UnauthorizedErrorJson = """
        { "error": { "status": 401, "message": "Invalid access token" } }
        """;

    public static HttpResponseMessage Json(string json, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    public static HttpResponseMessage Respond(HttpRequestMessage request)
    {
        var path = request.RequestUri!.AbsolutePath;

        return path switch
        {
            "/v1/search" => Json(SearchResponseJson),
            "/v1/albums/sp-1" => Json(AlbumDetailJson),
            _ => Json(NotFoundErrorJson, HttpStatusCode.NotFound)
        };
    }
}
