Feature: Health endpoints
  Liveness and readiness probes behave as the platform expects.

  Scenario: Liveness probe is independent of dependencies
    When I request health endpoint "/health/live"
    Then the response is healthy

  Scenario: Readiness probe includes the database
    When I request health endpoint "/health/ready"
    Then the response is healthy
