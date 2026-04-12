Feature: Strava Connection Management
  As an athlete
  I want to view and manage my Strava connection status
  So that I can connect, disconnect, and authorise access to my training data

  Scenario: Connected athlete checks their Strava status
    Given I am an authenticated athlete with a connected Strava account
    When I request the Strava connection status
    Then the status response indicates Strava is connected

  Scenario: Unconnected athlete checks their Strava status
    Given I am an authenticated athlete with no Strava connection
    When I request the Strava connection status
    Then the status response indicates Strava is not connected

  Scenario: Athlete disconnects their Strava account
    Given I am an authenticated athlete with a connected Strava account
    When I send a request to disconnect Strava
    Then the disconnect response returns a 204 status
    And the Strava connection no longer exists in the database

  Scenario: Athlete attempts to disconnect with no existing connection
    Given I am an authenticated athlete with no Strava connection
    When I send a request to disconnect Strava
    Then the disconnect response returns a 400 status

  Scenario: Athlete requests the Strava authorisation URL
    Given I am an authenticated athlete with no Strava connection
    When I request the Strava authorisation URL
    Then the response contains a valid Strava OAuth URL
