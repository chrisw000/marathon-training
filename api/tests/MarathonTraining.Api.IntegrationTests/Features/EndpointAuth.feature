Feature: Endpoint Authentication
  As a system
  I want every protected endpoint to reject unauthenticated requests
  So that athlete data is never accessible without a valid identity

  Scenario Outline: Unauthenticated request to protected endpoint is rejected
    When an unauthenticated <method> request is sent to "<route>"
    Then the response is 401 Unauthorized

    Examples:
      | method | route                  |
      | GET    | /me                    |
      | POST   | /api/profile           |
      | GET    | /api/strava/authorise  |
      | DELETE | /api/strava/disconnect |
      | GET    | /api/strava/status     |
