Feature: Training load and TSS

  Scenario: Training load with no activities returns empty result
    Given I am an authenticated athlete for training load
    And the athlete has no training activities
    When I request GET /api/training/load?from=2026-01-01&to=2026-01-31
    Then the training load response status is 200
    And the training load response is an empty array

  Scenario: Athlete retrieves training load for a date range
    Given I am an authenticated athlete for training load
    And the athlete has activities with known TSS values
    When I request GET /api/training/load?from=2026-01-01&to=2026-01-31
    Then the training load response status is 200
    And the response contains ATL CTL and TSB values

  Scenario: Invalid date range returns 400
    Given I am an authenticated athlete for training load
    When I request GET /api/training/load?from=invalid&to=2026-01-31
    Then the training load response status is 400

  Scenario: Unauthenticated request to training load is rejected
    Given I am not authenticated for training load
    When I request GET /api/training/load
    Then the training load response status is 401

  Scenario: Athlete retrieves week summary
    Given I am an authenticated athlete for training load
    And the athlete has a training week with activities
    When I request GET /api/training/week/2026-01-06
    Then the training load response status is 200
    And the response contains week summary fields

  Scenario: TSS recalculation returns count of recalculated activities
    Given I am an authenticated athlete for training load
    And the athlete has activities with known TSS values
    When I POST to /api/training/recalculate
    Then the training load response status is 200
    And the response contains a recalculated count

  Scenario: Unauthenticated request to training endpoints is rejected
    Given I am not authenticated for training load
    When I request GET /api/training/week/2026-01-06
    Then the training load response status is 401
