Feature: Album search
  Search uses the selected provider strategy and returns normalized album data.

  Scenario: Search by artist name
    When I search provider "deezer" for "daft punk"
    Then the response status is 200
    And the search returns at least 1 album
    And the first search result exposes artist, title, cover and provider url

  Scenario: Search by album name
    When I search provider "deezer" for "discovery"
    Then the first search result title is "Discovery"

  Scenario: Search through the fake Spotify strategy
    When I search provider "spotify" for "radiohead"
    Then the search returns at least 2 albums
    And the first search result title is "OK Computer"

  Scenario: Unknown provider is rejected
    When I search provider "napster" for "anything"
    Then the response status is 400

  Scenario: Missing query text is rejected
    When I search with no query text
    Then the response status is 400
