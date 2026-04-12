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

  Scenario: Authenticated athlete retrieves their profile
    Given I am an authenticated athlete with an existing profile
    When I request the athlete profile endpoint
    Then the response status is 200
    And the response contains the athlete display name
    And the response contains currentPhase "Base"

  Scenario: Unauthenticated request to profile is rejected
    Given I am not authenticated
    When I request the athlete profile endpoint
    Then the response status is 401

  Scenario: Athlete updates their physiology settings
    Given I am an authenticated athlete with an existing profile
    When I submit a valid physiology update
    Then the response status is 200
    And the response reflects the updated values

  Scenario: Invalid HR zones are rejected
    Given I am an authenticated athlete with an existing profile
    When I submit physiology with RestingHr greater than MaxHr
    Then the response status is 400
    And the response is a ProblemDetails with validation errors

  Scenario: Athlete updates training phase
    Given I am an authenticated athlete with an existing profile
    When I update the training phase to "Build"
    Then the response status is 200
    And the response contains currentPhase "Build"

  Scenario: Invalid training phase is rejected
    Given I am an authenticated athlete with an existing profile
    When I update the training phase to "InvalidPhase"
    Then the response status is 400
