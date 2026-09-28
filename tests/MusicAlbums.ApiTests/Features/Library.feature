Feature: Personal album library
  Users are created on first save and keep a snapshot of each album.

  Scenario: Saving an album creates the user and snapshots tracks
    When I save provider "deezer" album "302127" for user "alice"
    Then the response status is 201
    And the library of "alice" contains an album with 4 tracks

  Scenario: Saving the same album twice conflicts
    When I save provider "deezer" album "302127" for user "bob"
    And I save provider "deezer" album "302127" for user "bob"
    Then the response status is 409

  Scenario: Removing an album empties the library
    When I save provider "deezer" album "302127" for user "carol"
    And I remove the saved album of user "carol"
    Then the response status is 204
    And the library of "carol" is empty

  Scenario: Unknown album in the provider catalogue
    When I save provider "deezer" album "999999999999" for user "erin"
    Then the response status is 404

  Scenario: Unknown users have an empty library
    When I request the library of "newcomer"
    Then the response status is 200
    And the library is empty

  Scenario: Libraries are isolated between users
    When I save provider "deezer" album "302127" for user "frank"
    And I save provider "deezer" album "6575789" for user "grace"
    And I request the library of "grace"
    Then the library contains 1 album
    And the first library album title is "Random Access Memories"
