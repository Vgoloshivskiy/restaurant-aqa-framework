# Homework (Test Automation)

You need to automate end-to-end scenarios for the application, covering the entire flow from user actions in the interface to the results visible to the user.

There are three difficulty levels.

You are responsible for implementing all layers yourself, including scenarios, steps, tasks and questions, page objects with locators and actions, and API clients where necessary, according to your chosen application under test.

**Configuration**

The repository does not contain a `.env` file, passwords, or URLs. Copy `aqa/restaurant-aqa-framework/.env.example` to `.env` and fill it in with **your own data** (URLs, accounts, etc.).

The `.env` file is included in `.gitignore` and must not be committed.

In CI, these values must be configured using repository variables and secrets (see the framework README).

**Submission**

Place your automated pipelines in your own public Git repository.

**The link to this repository is your completed assignment.**

## Architecture (Your Choice)

You may choose any architecture:

- BDD + Screenplay (as implemented in the existing framework);
- Screenplay without BDD;
- BDD with another design pattern, such as Page Object Model / Component Object Model;
- Any other approach you can justify.

At the beginning of your solution's README, specify the architecture and approach you have chosen.

---

# Direction 1. Group Pre-order

You will test **your own homework implementation** (Backend and/or Frontend).

The reservation Owner invites guests, each guest creates their own order, and the Owner sees everything consolidated and confirms a single combined pre-order that is sent to the restaurant.

The difficulty levels correspond to the BE/FE assignments:

- L1 — Viewing participants.
- L2 — Confirming the pre-order.
- L3 — Removing a participant.

## Test Pyramid

- **UI:** Main scenarios (Owner views participants and totals, confirms the pre-order, waiter sees the order, participant removal).
- **API:** Authorization and validation (non-Owner, unauthenticated user, non-existent group, participant belonging to another group, repeated submission), as well as test data preparation (creating guests and their carts).
- If the UI does not have a participant removal button (this is a bonus feature in the Frontend assignment), test Level 3 through the API.

## Behavior Not Defined in the Assignment

Where the specification does not define a specific response code or message (repeated submission, non-Owner access, removal of the last participant), choose the expected behavior yourself, document it in the README, and write your tests accordingly.

For a group with no participants, expect:

- `participants: []`
- `total: 0`

## Level 1

**Viewing Group Pre-order Participants**

The Owner must be able to see which participants have joined the group order, which dishes they have added, the total amount for each participant, and the overall total for all participants.

**Automate the following scenarios:**

- The Owner creates a reservation and invites guests. Guests join using their invitations and add dishes. The Owner can see all participants, their dishes, quantities, and individual totals.
- The overall total equals the sum of all participant totals and changes when a guest modifies their cart.
- A participant who has not yet confirmed their cart is displayed in the list with the appropriate status indicator. Participants without any dishes must also be displayed.
- A group that no one has joined yet.
- Only the Owner of the specific group can view the participant list — not another customer, a guest, a waiter, or an unauthenticated user.
- A group that does not exist.

## Level 2

**Confirming the Consolidated Pre-order**

After reviewing the orders, the Owner confirms a consolidated pre-order that will be sent to the restaurant.

The Owner also has their own session within the group and can add their own dishes.

**During confirmation:**

- Only carts that have been confirmed by their respective participants are included.
- All dishes are collected into a single list. Identical dishes are merged, with their quantities summed up.
- The dishes are added to an order that receives the `SubmittedByCustomer` status.
- The group reservation receives the `Submitted` status.

Confirmation must not be possible if:

- No participant has confirmed their cart.
- The confirmed carts contain no dishes.
- The order cannot be found.
- The reservation or group does not exist.

Only the Owner can confirm the pre-order.

**Automate the following scenarios:**

- Successful confirmation: the Owner can see that the pre-order has been sent to the restaurant. Identical dishes from different guests are merged and their quantities are summed up. Unconfirmed carts are excluded.
- The waiter can see the confirmed order with exactly the items approved by the Owner.
- Each condition that prevents confirmation must be covered by a separate scenario with an expected error message.
- Attempting to confirm a group that has already been confirmed.
- Attempting to confirm the order as someone other than the Owner.

## Level 3

**Removing a Participant from a Group**

The Owner can remove a participant from the group order. After removal, the Owner sees the updated participant list and the recalculated overall total.

A participant can only be removed if they belong to the specific group and actually exist.

Only the Owner can remove participants.

**Automate the following scenarios:**

- Removing one participant from a group containing multiple participants: the remaining participants stay in the group, the total is recalculated, and the removed participant no longer affects the overall total.
- Removing a participant who has already confirmed their cart.
- Removing the last remaining participant.
- Attempting to remove a participant belonging to another group (two independent groups with participants accidentally mixed up) — expected response: `400`.
- Attempting to remove a participant who does not exist — expected response: `404`.
- Attempting to remove a participant as someone other than the Owner.
- End-to-end scenario: multiple guests add dishes → Owner reviews the orders → Owner removes one participant → Owner confirms the pre-order → waiter sees the order without the removed participant.

---

# General Requirements

- **End-to-end scenarios:** Tests must be executed through the UI, following the same steps a real user would perform. Data preparation and state verification may be performed outside the UI. For Direction 1, this can also be done through the API (see the test pyramid).
- **Positive and negative scenarios:** Every difficulty level must include both positive and negative scenarios. Testing only the happy path is not sufficient.
- **Test data management:** Test data must be created before each test and cleaned up afterward. Test execution must not leave unnecessary data in the testing environment. If there is no API for cleanup, use unique data for each test run (for example, a prefix or GUID in email addresses, separate dates, and tables).
- **Time-dependent data:** Generate dynamically (e.g., `now + N`) to prevent flaky tests.
- **Test independence:** Tests must be independent, must not rely on execution order, and must support parallel execution.
- **Test reports:** Execution reports must be available. Failed tests must include debugging artifacts (screenshots, videos, or traces).
- **CI execution:** Tests must run in CI with reports and artifacts. A failed test must not stop the entire pipeline.
- **Solution README:** Must describe:
  - The chosen direction and architectural approach.
  - How to configure and run the tests.
  - How to run a subset of tests (by tag or difficulty level).
  - Where to find the test execution report.
  - For Direction 1: the chosen behavior for cases not explicitly defined in the assignment.