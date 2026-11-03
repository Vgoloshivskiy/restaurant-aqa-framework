Feature: Viewing group pre-order participants

  The reservation owner can review everyone who joined the group pre-order,
  what each participant added, and the running totals before any submission.

  Rule: The owner can review participants and totals

  @ui @level1 @smoke
  Scenario: The owner sees all joined participants with their dishes and totals
    Given an owner has created a reservation with a group pre-order
    And guests have joined the group using their invitations
    And each joined guest has added dishes to their cart
    When the owner opens the group pre-order participants view
    Then the owner sees all joined participants
    And the owner sees each participant's dishes and quantities
    And the owner sees each participant's total

  @ui @level1
  Scenario: The overall total equals the sum of participant totals and updates after a cart change
    Given an owner has created a reservation with a group pre-order
    And guests have joined the group using their invitations
    And each joined guest has added dishes to their cart
    When the owner opens the group pre-order participants view
    Then the overall total equals the sum of all participant totals
    When one guest changes their cart
    Then the overall total is updated

  @ui @level1
  Scenario: A participant without confirmed cart is shown with status and empty-cart participants are visible
    Given an owner has created a reservation with a group pre-order
    And guests have joined the group using their invitations
    And one participant has not confirmed their cart yet
    And one participant has not added any dishes
    When the owner opens the group pre-order participants view
    Then the unconfirmed participant is shown with the correct status
    And the participant without dishes is shown in the participants list

  @ui @level1
  Scenario: The owner views a group that no one has joined yet
    Given an owner has created a reservation with a group pre-order
    And no guest has joined the group yet
    When the owner opens the group pre-order participants view
    Then the owner sees no participants in the list
    And the overall total is zero

  Rule: Access is restricted to the owner of that group

  @api @level1 @negative
  Scenario Outline: Someone other than the owner cannot view the participants list
    Given an owner has created a reservation with a group pre-order
    And guests have joined the group using their invitations
    When <persona> tries to view the group pre-order participants
    Then access to the participants list is refused

    Examples: Non-owner access attempts
    | persona                    |
    | another customer           |
    | a guest                    |
    | a waiter                   |
    | an unauthenticated visitor |

  @api @level1 @negative
  Scenario: A non-existent group cannot be viewed
    Given a group pre-order that does not exist
    When the owner tries to view the group pre-order participants
    Then the owner is told the group could not be found
