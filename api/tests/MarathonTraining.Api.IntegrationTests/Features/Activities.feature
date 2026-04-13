Feature: Activities — sync, manual logging, and retrieval

  # ── Strava sync ───────────────────────────────────────────────────────────

  Scenario: Athlete syncs Strava activities for the first time
    Given I am an authenticated athlete with a Strava connection for activities
    And Strava returns 1 new activity for the athlete
    When I POST to /api/activities/sync
    Then the activities response status is 200
    And the response contains activitiesSynced of 1

  Scenario: Athlete syncs when all activities are already known
    Given I am an authenticated athlete with a Strava connection for activities
    And Strava returns 0 activities for the athlete
    When I POST to /api/activities/sync
    Then the activities response status is 200
    And the response contains activitiesSynced of 0

  Scenario: Sync without a Strava connection returns 422
    Given I am an authenticated athlete without a Strava connection for activities
    When I POST to /api/activities/sync
    Then the activities response status is 422

  # ── Manual logging ─────────────────────────────────────────────────────────

  Scenario: Athlete logs a manual strength activity
    Given I am an authenticated athlete for activities
    When I POST a manual strength activity with duration 45 and RPE 7
    Then the activities response status is 201
    And the response contains a valid activity ID

  Scenario: Manual activity with invalid RPE is rejected
    Given I am an authenticated athlete for activities
    When I POST a manual strength activity with duration 45 and RPE 11
    Then the activities response status is 400

  Scenario: Manual activity with zero duration is rejected
    Given I am an authenticated athlete for activities
    When I POST a manual strength activity with duration 0 and RPE 7
    Then the activities response status is 400

  # ── Activity list ──────────────────────────────────────────────────────────

  Scenario: Athlete retrieves their activity list
    Given I am an authenticated athlete for activities
    And the athlete has 3 seeded activities
    When I request GET /api/activities
    Then the activities response status is 200
    And the response contains 3 activities

  Scenario: Athlete filters activities by type
    Given I am an authenticated athlete for activities
    And the athlete has 3 seeded activities
    When I request GET /api/activities?type=Run
    Then the activities response status is 200
    And the response contains 3 activities

  Scenario: Empty activity list returns zero items
    Given I am an authenticated athlete for activities
    When I request GET /api/activities
    Then the activities response status is 200
    And the response contains 0 activities
