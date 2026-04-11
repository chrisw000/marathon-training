Feature: Athlete Profile Management

  Scenario: First login creates an athlete profile
    Given I am a new authenticated user
    When I call the ensure profile endpoint
    Then the response indicates the profile was created
    And the athlete profile exists in the database

  Scenario: Calling the ensure profile endpoint again is idempotent
    Given I am a new authenticated user
    And my athlete profile already exists
    When I call the ensure profile endpoint
    Then the response indicates the profile already existed
    And there is still only one athlete profile in the database
