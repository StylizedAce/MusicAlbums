Feature: Provider catalogue
  The API exposes the registered album provider strategies.

  Scenario: Listing providers
    When I request the provider list
    Then the response status is 200
    And the provider list contains "deezer" as the default
    And the provider list contains "spotify"
