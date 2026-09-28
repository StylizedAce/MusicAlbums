using System.Net;
using System.Text;

namespace MusicAlbums.TestSupport;

public static class DeezerTestData
{
    public const string DiscoveryAlbumId = "302127";

    public const string SearchResponseJson = """
        {
          "data": [
            {
              "id": 302127,
              "title": "Discovery",
              "link": "https://www.deezer.com/album/302127",
              "cover_xl": "https://cdn-images.dzcdn.net/images/cover/discovery/1000x1000.jpg",
              "nb_tracks": 14,
              "artist": { "id": 27, "name": "Daft Punk" }
            }
          ],
          "total": 95
        }
        """;

    public const string AlbumDetailJson = """
        {
          "id": 302127,
          "title": "Discovery",
          "link": "https://www.deezer.com/album/302127",
          "cover_xl": "https://cdn-images.dzcdn.net/images/cover/discovery/1000x1000.jpg",
          "nb_tracks": 14,
          "release_date": "2001-03-07",
          "artist": { "id": 27, "name": "Daft Punk" },
          "tracks": {
            "data": [
              { "id": 3135553, "title": "One More Time", "duration": 320, "preview": "https://cdns-preview.dzcdn.net/stream/one-more-time.mp3" },
              { "id": 3135554, "title": "Aerodynamic", "duration": 212 },
              { "id": 3135555, "title": "Digital Love", "duration": 301 },
              { "id": 3135556, "title": "Harder, Better, Faster, Stronger", "duration": 224 }
            ]
          }
        }
        """;

    public const string NoDataErrorJson = """
        { "error": { "type": "DataException", "message": "no data", "code": 800 } }
        """;

    public static HttpResponseMessage Respond(HttpRequestMessage request)
    {
        var path = request.RequestUri!.AbsolutePath;

        return path switch
        {
            "/search/album" => Json(SearchResponseJson),
            $"/album/{DiscoveryAlbumId}" => Json(AlbumDetailJson),
            _ when path.StartsWith("/album/", StringComparison.Ordinal) => Json(NoDataErrorJson),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        };
    }

    public static HttpResponseMessage Json(string json, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}
