Feature: Strava OAuth Authorisation Flow
  As an athlete
  I want to connect my Strava account
  So that the app can access my training data

  Scenario: Athlete successfully connects their Strava account
    Given I am authenticated as a valid athlete
    When Strava calls the callback endpoint with a valid authorisation code
    Then the athlete's Strava connection is stored
    And the response redirects to http://localhost:5173/strava-connected

  Scenario: Callback with invalid authorisation code is rejected
    Given I am authenticated as a valid athlete
    When Strava calls the callback endpoint with an invalid authorisation code
    Then the athlete's Strava connection is not stored
    And the response returns a 400 status
