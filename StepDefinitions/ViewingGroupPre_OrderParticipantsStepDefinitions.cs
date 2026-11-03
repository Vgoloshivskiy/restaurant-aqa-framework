using Reqnroll;
using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.Tasks;
using System;

namespace Restaurant_Aqa_Framework.StepDefinitions
{
    [Binding]
    public class ViewingGroupPre_OrderParticipantsStepDefinitions
    {
        [Given("an owner has created a reservation with a group pre-order")]
        public void GivenAnOwnerHasCreatedAReservationWithAGroupPre_Order()
        {
            Actor.AttemptsToAsync(new ChooseAnExistingLocation());
        }

        [Given("guests have joined the group using their invitations")]
        public void GivenGuestsHaveJoinedTheGroupUsingTheirInvitations()
        {
            throw new PendingStepException();
        }

        [Given("each joined guest has added dishes to their cart")]
        public void GivenEachJoinedGuestHasAddedDishesToTheirCart()
        {
            throw new PendingStepException();
        }

        [When("the owner opens the group pre-order participants view")]
        public void WhenTheOwnerOpensTheGroupPre_OrderParticipantsView()
        {
            throw new PendingStepException();
        }

        [Then("the owner sees all joined participants")]
        public void ThenTheOwnerSeesAllJoinedParticipants()
        {
            throw new PendingStepException();
        }

        [Then("the owner sees each participant's dishes and quantities")]
        public void ThenTheOwnerSeesEachParticipantsDishesAndQuantities()
        {
            throw new PendingStepException();
        }

        [Then("the owner sees each participant's total")]
        public void ThenTheOwnerSeesEachParticipantsTotal()
        {
            throw new PendingStepException();
        }

        [Then("the overall total equals the sum of all participant totals")]
        public void ThenTheOverallTotalEqualsTheSumOfAllParticipantTotals()
        {
            throw new PendingStepException();
        }

        [When("one guest changes their cart")]
        public void WhenOneGuestChangesTheirCart()
        {
            throw new PendingStepException();
        }

        [Then("the overall total is updated")]
        public void ThenTheOverallTotalIsUpdated()
        {
            throw new PendingStepException();
        }

        [Given("one participant has not confirmed their cart yet")]
        public void GivenOneParticipantHasNotConfirmedTheirCartYet()
        {
            throw new PendingStepException();
        }

        [Given("one participant has not added any dishes")]
        public void GivenOneParticipantHasNotAddedAnyDishes()
        {
            throw new PendingStepException();
        }

        [Then("the unconfirmed participant is shown with the correct status")]
        public void ThenTheUnconfirmedParticipantIsShownWithTheCorrectStatus()
        {
            throw new PendingStepException();
        }

        [Then("the participant without dishes is shown in the participants list")]
        public void ThenTheParticipantWithoutDishesIsShownInTheParticipantsList()
        {
            throw new PendingStepException();
        }

        [Given("no guest has joined the group yet")]
        public void GivenNoGuestHasJoinedTheGroupYet()
        {
            throw new PendingStepException();
        }

        [Then("the owner sees no participants in the list")]
        public void ThenTheOwnerSeesNoParticipantsInTheList()
        {
            throw new PendingStepException();
        }

        [Then("the overall total is zero")]
        public void ThenTheOverallTotalIsZero()
        {
            throw new PendingStepException();
        }

        [When("another customer tries to view the group pre-order participants")]
        public void WhenAnotherCustomerTriesToViewTheGroupPre_OrderParticipants()
        {
            throw new PendingStepException();
        }

        [Then("access to the participants list is refused")]
        public void ThenAccessToTheParticipantsListIsRefused()
        {
            throw new PendingStepException();
        }

        [Given("a group pre-order that does not exist")]
        public void GivenAGroupPre_OrderThatDoesNotExist()
        {
            throw new PendingStepException();
        }

        [When("the owner tries to view the group pre-order participants")]
        public void WhenTheOwnerTriesToViewTheGroupPre_OrderParticipants()
        {
            throw new PendingStepException();
        }

        [Then("the owner is told the group could not be found")]
        public void ThenTheOwnerIsToldTheGroupCouldNotBeFound()
        {
            throw new PendingStepException();
        }

    }
}
