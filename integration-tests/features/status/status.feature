Feature: test bestbets controller health

  Background:
    * url apiHost

  Scenario: test status check
    Given path '/BestBets/status'
    When method get
    Then status 200
