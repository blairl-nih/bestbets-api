Feature: test bestbets controller get method

  Background:
  * url apiHost

  ############################################################
  ## English Tests
  ########################################################
  Scenario: get the live english bestbets for Visuals Online
    Given path 'BestBets', 'live', 'en', 'visuals+online'
    When method get
    Then status 200
    And assert response.length == 1
    And match response[0].name == "Cancer Images"
    ## Test list items

  ## Test empty live

  Scenario: get the live english bestbets for Visuals Online
    Given path 'BestBets', 'preview', 'en', 'visuals+online'
    When method get
    Then status 200
    And assert response.length == 1
    And match response[0].name == "Cancer Images"
    ## Test list items

  ## Test empty preview, but working live


  ############################################################
  ## Spanish Tests
  ########################################################
  Scenario: get the live spanish bestbets for Visuals Online
    Given path 'BestBets', 'live', 'es', 'imágenes'
    When method get
    Then status 200
    And assert response.length == 2
    And match response[0].name == "Multimedia" 
    And match response[1].name == "Fotos de cáncer"
    ## Test list items


#* def first = response[0]
#
#Given path 'users', first.id
#When method get
#Then status 200
#
#Scenario: create a user and then get it by id
#
#* def user =
#"""
#{
#  "name": "Test User",
#  "username": "testuser",
#  "email": "test@user.com",
#  "address": {
#    "street": "Has No Name",
#    "suite": "Apt. 123",
#    "city": "Electri",
#    "zipcode": "54321-6789"
#  }
#}
#"""
#
#Given url 'https://jsonplaceholder.typicode.com/users'
#And request user
#When method post
#Then status 201
#
#* def id = response.id
#* print 'created id is: ' + id
#
#Given path id
## When method get
## Then status 200
## And match response contains user

