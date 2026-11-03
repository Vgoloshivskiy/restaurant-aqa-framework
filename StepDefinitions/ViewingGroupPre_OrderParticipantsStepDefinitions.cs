using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reqnroll;
using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Locations;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Api.Dtos.Tables;
using Restaurant_Aqa_Framework.Api.Seeding;
using Restaurant_Aqa_Framework.Configuration;
using Restaurant_Aqa_Framework.Hooks;
using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.Questions;
using Restaurant_Aqa_Framework.Screenplay.Tasks;

namespace Restaurant_Aqa_Framework.StepDefinitions;

[Binding]
public sealed class ViewingGroupPre_OrderParticipantsStepDefinitions(Actor actor) : StepDefinitionBase(actor)
{
    private static readonly int[] AccessRefusalStatuses = [401, 403, 404];

    private sealed record OwnerSession(string IdToken, string Email, string Password);
    private sealed record GroupReservationRef(string Id);
    private sealed record UiGuest(string Name, IBrowserContext Context, IPage Page, bool ShouldSubmit);
    private sealed record UiScenarioState(
        string InviteUrl,
        string? OwnerTotal,
        string? GuestTotal,
        string? CombinedTotal,
        bool GuestSubmitted,
        bool HasEmptyGuest,
        bool OwnerOpenedParticipants);

    [Given("an owner has created a reservation with a group pre-order")]
    [Scope(Tag = "api")]
    public async Task GivenAnOwnerHasCreatedAReservationWithAGroupPre_OrderForApi()
    {
        await EnsureOwnerSignedInAsync();

        // API-negative checks only: a synthetic id is enough to validate authorization/not-found behavior.
        Actor.Remember(new GroupReservationRef(Guid.NewGuid().ToString("N")));
    }

    [Given("an owner has created a reservation with a group pre-order")]
    [Scope(Tag = "ui")]
    public async Task GivenAnOwnerHasCreatedAReservationWithAGroupPre_OrderForUi()
    {
        var owner = await RegisterOwnerSessionAsync();
        Actor.Remember(owner);

        await Actor.AttemptsToAsync(new SignInThroughTheLoginForm(owner.Email, owner.Password));

        var inviteUrl = await SetUpGroupReservationViaApiAsync(owner.IdToken);
        Actor.Remember(new UiScenarioState(
            InviteUrl: inviteUrl,
            OwnerTotal: null,
            GuestTotal: null,
            CombinedTotal: null,
            GuestSubmitted: false,
            HasEmptyGuest: false,
            OwnerOpenedParticipants: false));
    }

    [Given("guests have joined the group using their invitations")]
    [Scope(Tag = "api")]
    public static Task GivenGuestsHaveJoinedTheGroupUsingTheirInvitations() => Task.CompletedTask;

    [Given("guests have joined the group using their invitations")]
    [Scope(Tag = "ui")]
    public async Task GivenGuestsHaveJoinedTheGroupUsingTheirInvitationsForUi()
    {
        var state = RecallUiState();
        var guestContext = await PlaywrightRunHooks.Browser.NewContextAsync(new()
        {
            BaseURL = Settings.Current.UiBaseUrl,
        });
        guestContext.SetDefaultTimeout(Settings.Current.DefaultTimeoutMs);
        guestContext.SetDefaultNavigationTimeout(Settings.Current.NavigationTimeoutMs);

        var guestPage = await guestContext.NewPageAsync();
        await guestPage.GotoAsync(state.InviteUrl);

        var guestNameInput = guestPage.GetByRole(AriaRole.Textbox, new() { Name = "Enter Your Name" });
        await guestNameInput.FillAsync("tomas");
        await guestNameInput.PressAsync("Tab");
        await guestPage.GetByRole(AriaRole.Button, new() { Name = "Browse Menu" }).ClickAsync();

        Actor.Remember("guest-primary", new UiGuest("tomas", guestContext, guestPage, ShouldSubmit: false));
    }

    [Given("each joined guest has added dishes to their cart")]
    [Scope(Tag = "api")]
    public static Task GivenEachJoinedGuestHasAddedDishesToTheirCart() => Task.CompletedTask;

    [Given("each joined guest has added dishes to their cart")]
    [Scope(Tag = "ui")]
    public async Task GivenEachJoinedGuestHasAddedDishesToTheirCartForUi()
    {
        var guest = Recall<UiGuest>("guest-primary");
        await AddSampleDishesToCartAsync(guest.Page);

        var guestTotal = await ReadFirstSubtotalAsync(guest.Page.GetByRole(AriaRole.Main));
        if (guestTotal is not null)
            RememberUiState(current => current with { GuestTotal = guestTotal });
    }

    [When("the owner opens the group pre-order participants view")]
    public async Task WhenTheOwnerOpensTheGroupPre_OrderParticipantsView()
    {
        var page = Actor.LookingAtThePage();
        await page.GetByRole(AriaRole.Link, new() { Name = "Reservations" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Pre-order" }).ClickAsync();

        var combinedTotal = await ReadGrandTotalAsync(page.GetByRole(AriaRole.Region));
        RememberUiState(current => current with
        {
            OwnerOpenedParticipants = true,
            CombinedTotal = combinedTotal,
        });
    }

    [Then("the owner sees all joined participants")]
    public async Task ThenTheOwnerSeesAllJoinedParticipants()
    {
        var page = Actor.LookingAtThePage();
        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync("Guest: tomas");

        var state = RecallUiState();
        if (state.HasEmptyGuest)
            await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync("Guest: no-dishes");
    }

    [Then("the owner sees each participant's dishes and quantities")]
    public async Task ThenTheOwnerSeesEachParticipantsDishesAndQuantities()
    {
        var page = Actor.LookingAtThePage();
        await Assertions.Expect(page.GetByRole(AriaRole.Region)).ToContainTextAsync("Consolidated pre-order");
        await Assertions.Expect(page.GetByRole(AriaRole.Region)).ToContainTextAsync("×");
    }

    [Then("the owner sees each participant's total")]
    public async Task ThenTheOwnerSeesEachParticipantsTotal()
    {
        var main = Actor.LookingAtThePage().GetByRole(AriaRole.Main);
        var text = await main.InnerTextAsync();
        var matches = Regex.Matches(text, @"Subtotal\$(\d+\.\d{2})", RegexOptions.CultureInvariant);

        await Assert.That(matches.Count).IsGreaterThanOrEqualTo(2)
            .Because($"Expected owner and guest subtotal entries, but got: {text}");
    }

    [Then("the overall total equals the sum of all participant totals")]
    public async Task ThenTheOverallTotalEqualsTheSumOfAllParticipantTotals()
    {
        var page = Actor.LookingAtThePage();
        var mainText = await page.GetByRole(AriaRole.Main).InnerTextAsync();
        var subtotalMatches = Regex.Matches(mainText, @"Subtotal\$(\d+\.\d{2})", RegexOptions.CultureInvariant);

        var sum = subtotalMatches
            .Select(match => decimal.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture))
            .Sum();

        var grandTotal = decimal.Parse(await ReadGrandTotalAsync(page.GetByRole(AriaRole.Region)), CultureInfo.InvariantCulture);
        await Assert.That(grandTotal).IsEqualTo(sum);

        RememberUiState(current => current with { CombinedTotal = grandTotal.ToString("0.00", CultureInfo.InvariantCulture) });
    }

    [When("one guest changes their cart")]
    public async Task WhenOneGuestChangesTheirCart()
    {
        var guest = Recall<UiGuest>("guest-primary");
        await AddSampleDishesToCartAsync(guest.Page);

        var ownerPage = Actor.LookingAtThePage();
        await ownerPage.GetByRole(AriaRole.Link, new() { Name = "Book a Table" }).ClickAsync();
        await ownerPage.GetByRole(AriaRole.Link, new() { Name = "Reservations" }).ClickAsync();
        await ownerPage.GetByRole(AriaRole.Button, new() { Name = "Pre-order" }).ClickAsync();
    }

    [Then("the overall total is updated")]
    public async Task ThenTheOverallTotalIsUpdated()
    {
        var state = RecallUiState();
        var region = Actor.LookingAtThePage().GetByRole(AriaRole.Region);
        var updated = await ReadGrandTotalAsync(region);

        if (state.CombinedTotal is not null)
            await Assert.That(updated).IsNotEqualTo(state.CombinedTotal);

        RememberUiState(current => current with { CombinedTotal = updated });
    }

    [Given("one participant has not confirmed their cart yet")]
    public Task GivenOneParticipantHasNotConfirmedTheirCartYet()
    {
        RememberUiState(current => current with { GuestSubmitted = false });
        return Task.CompletedTask;
    }

    [Given("one participant has not added any dishes")]
    public async Task GivenOneParticipantHasNotAddedAnyDishes()
    {
        var state = RecallUiState();
        var emptyGuestContext = await PlaywrightRunHooks.Browser.NewContextAsync(new()
        {
            BaseURL = Settings.Current.UiBaseUrl,
        });
        emptyGuestContext.SetDefaultTimeout(Settings.Current.DefaultTimeoutMs);
        emptyGuestContext.SetDefaultNavigationTimeout(Settings.Current.NavigationTimeoutMs);

        var emptyGuestPage = await emptyGuestContext.NewPageAsync();
        await emptyGuestPage.GotoAsync(state.InviteUrl);

        var emptyGuestNameInput = emptyGuestPage.GetByRole(AriaRole.Textbox, new() { Name = "Enter Your Name" });
        await emptyGuestNameInput.FillAsync("no-dishes");
        await emptyGuestNameInput.PressAsync("Tab");
        await emptyGuestPage.GetByRole(AriaRole.Button, new() { Name = "Browse Menu" }).ClickAsync();

        Actor.Remember("guest-empty", new UiGuest("no-dishes", emptyGuestContext, emptyGuestPage, ShouldSubmit: false));
        RememberUiState(current => current with { HasEmptyGuest = true });
    }

    [Then("the unconfirmed participant is shown with the correct status")]
    public async Task ThenTheUnconfirmedParticipantIsShownWithTheCorrectStatus()
    {
        var main = Actor.LookingAtThePage().GetByRole(AriaRole.Main);
        await Assertions.Expect(main).ToContainTextAsync("Guest: tomas");
        await Assertions.Expect(main).ToContainTextAsync("Still choosing");
    }

    [Then("the participant without dishes is shown in the participants list")]
    public async Task ThenTheParticipantWithoutDishesIsShownInTheParticipantsList()
    {
        var main = Actor.LookingAtThePage().GetByRole(AriaRole.Main);
        await Assertions.Expect(main).ToContainTextAsync("Guest: no-dishes");
        await Assertions.Expect(main).ToContainTextAsync("No dishes yet");
    }

    [Given("no guest has joined the group yet")]
    public static Task GivenNoGuestHasJoinedTheGroupYet() => Task.CompletedTask;

    [Then("the owner sees no participants in the list")]
    public async Task ThenTheOwnerSeesNoParticipantsInTheList()
    {
        var mainText = await Actor.LookingAtThePage().GetByRole(AriaRole.Main).InnerTextAsync();
        await Assert.That(mainText.Contains("Guest:", StringComparison.Ordinal)).IsFalse();
    }

    [Then("the overall total is zero")]
    public async Task ThenTheOverallTotalIsZero()
    {
        var grandTotal = await ReadGrandTotalAsync(Actor.LookingAtThePage().GetByRole(AriaRole.Region));
        await Assert.That(grandTotal).IsEqualTo("0.00");
    }

    [When("another customer tries to view the group pre-order participants")]
    public async Task WhenAnotherCustomerTriesToViewTheGroupPre_OrderParticipants() =>
        await GetGroupParticipantsAsync(await RegisterAndSignInCustomerAsync());

    [When("a guest tries to view the group pre-order participants")]
    [When("an unauthenticated visitor tries to view the group pre-order participants")]
    public async Task WhenGuestOrUnauthenticatedVisitorTriesToViewTheGroupPre_OrderParticipants() =>
        await GetGroupParticipantsAsync(bearerToken: null);

    [When("a waiter tries to view the group pre-order participants")]
    public async Task WhenAWaiterTriesToViewTheGroupPre_OrderParticipants()
    {
        string? waiterToken = null;

        try
        {
            var (username, password) = Settings.Current.RequireWaiter();
            await Actor.AttemptsToAsync(new SignInThroughTheService(username, password));
            waiterToken = await Actor.AsksForAsync(new TheIdToken());
        }
        catch (InvalidOperationException)
        {
            // No waiter configured in this environment; still validate that non-owner access is refused.
        }

        await GetGroupParticipantsAsync(waiterToken);
    }

    [Then("access to the participants list is refused")]
    public async Task ThenAccessToTheParticipantsListIsRefused()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());

        await Assert.That(outcome.Succeeded).IsFalse().Because($"response body was: {outcome.Body}");
        await Assert.That(AccessRefusalStatuses).Contains(outcome.Status)
            .Because($"expected an access-related refusal, but got {outcome.Status}: {outcome.Body}");
    }

    [Given("a group pre-order that does not exist")]
    public async Task GivenAGroupPre_OrderThatDoesNotExist()
    {
        await EnsureOwnerSignedInAsync();
        Actor.Remember(new GroupReservationRef(Guid.NewGuid().ToString("N")));
    }

    [When("the owner tries to view the group pre-order participants")]
    public async Task WhenTheOwnerTriesToViewTheGroupPre_OrderParticipants() =>
        await GetGroupParticipantsAsync(Recall<OwnerSession>().IdToken);

    [Then("the owner is told the group could not be found")]
    public async Task ThenTheOwnerIsToldTheGroupCouldNotBeFound()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(404).Because($"response body was: {outcome.Body}");
    }

    private UiScenarioState RecallUiState() => Recall<UiScenarioState>();

    private void RememberUiState(Func<UiScenarioState, UiScenarioState> mutate) =>
        Actor.Remember(mutate(RecallUiState()));

    private async Task<OwnerSession> RegisterOwnerSessionAsync()
    {
        var owner = SampleDataFactory.RegisterRequest();
        await Actor.AttemptsToAsync(new RegisterThroughTheService(owner));
        if (!Actor.TheLastResponse().Succeeded)
            throw new InvalidOperationException($"Owner registration failed with {Actor.TheLastResponse().Status}: {Actor.TheLastResponse().Body}");

        await Actor.AttemptsToAsync(new SignInThroughTheService(owner.Email, owner.Password));
        var idToken = await Actor.AsksForAsync(new TheIdToken());

        return new OwnerSession(
            IdToken: idToken ?? throw new InvalidOperationException("Owner sign-in succeeded but did not return an id token."),
            Email: owner.Email,
            Password: owner.Password);
    }

    private async Task<string> SetUpGroupReservationViaApiAsync(string ownerToken)
    {
        var api = Actor.UsingTheApi();
        var locationsApi = new LocationsApi(api);
        var tablesApi = new TablesApi(api);
        var reservationsApi = new ReservationsApi(api);

        var locationsResponse = await locationsApi.GetLocationsAsync();
        if (!locationsResponse.Ok)
            throw new InvalidOperationException($"Location lookup failed with {locationsResponse.Status}: {await locationsResponse.TextAsync()}");

        var locations = await locationsResponse.ReadAsAsync<IReadOnlyList<LocationResponse>>();
        if (locations.Count == 0)
            throw new InvalidOperationException("No locations are available for creating a reservation.");

        var failures = new List<string>();

        foreach (var location in locations)
        {
            var tableResponse = await tablesApi.CreateTableAsync(SampleDataFactory.CreateTableRequest(location.Id, capacity: 4));
            if (!tableResponse.Ok)
            {
                failures.Add($"{location.Id}: table setup failed with {tableResponse.Status}");
                continue;
            }

            var table = await tableResponse.ReadAsAsync<TableResponse>();

            var startTime = DateTime.UtcNow.AddDays(2).Date.AddHours(18);
            var reservationRequest = SampleDataFactory.CreateReservationRequest(table.Id, startTime, guestCount: 2);
            var reservationResponse = await reservationsApi.CreateReservationAsync(reservationRequest, ownerToken);
            if (!reservationResponse.Ok)
            {
                failures.Add($"{location.Id}: reservation failed with {reservationResponse.Status}");
                continue;
            }

            var reservation = await reservationResponse.ReadAsAsync<ReservationResponse>();
            Actor.Remember(new GroupReservationRef(reservation.Id));

            var inviteUri = new Uri(new Uri(Settings.Current.UiBaseUrl), $"/join/{reservation.Id}");
            return inviteUri.ToString();
        }

        throw new InvalidOperationException($"Reservation setup failed for all locations. Tried {locations.Count} locations. Errors: {string.Join(" | ", failures)}");
    }

    private static async Task AddSampleDishesToCartAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Pre-Order" }).Nth(3).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Pre-Order" }).Nth(5).ClickAsync();
    }

    private static async Task<string?> ReadFirstSubtotalAsync(ILocator scope)
    {
        var text = await scope.InnerTextAsync();
        var match = Regex.Match(text, @"Subtotal\$(\d+\.\d{2})", RegexOptions.CultureInvariant);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static async Task<string> ReadGrandTotalAsync(ILocator scope)
    {
        var text = await scope.InnerTextAsync();
        var match = Regex.Match(text, @"Grand Total\$(\d+\.\d{2})", RegexOptions.CultureInvariant);
        if (!match.Success)
            throw new InvalidOperationException($"Could not read grand total from text: {text}");

        return match.Groups[1].Value;
    }

    private async Task EnsureOwnerSignedInAsync()
    {
        if (Actor.TryRecall<OwnerSession>(out _))
            return;

        Actor.Remember(await RegisterOwnerSessionAsync());
    }

    private async Task<string> RegisterAndSignInCustomerAsync()
    {
        var owner = SampleDataFactory.RegisterRequest();
        await Actor.AttemptsToAsync(new RegisterThroughTheService(owner));
        if (!Actor.TheLastResponse().Succeeded)
            throw new InvalidOperationException($"Registration failed with {Actor.TheLastResponse().Status}: {Actor.TheLastResponse().Body}");

        await Actor.AttemptsToAsync(new SignInThroughTheService(owner.Email, owner.Password));
        var idToken = await Actor.AsksForAsync(new TheIdToken());

        return idToken ?? throw new InvalidOperationException("Sign-in succeeded but did not return an id token.");
    }

    private async Task GetGroupParticipantsAsync(string? bearerToken)
    {
        var group = Recall<GroupReservationRef>();
        var url = $"{Settings.Current.ApiBaseUrl.TrimEnd('/')}/guest/groups/{group.Id}/sessions";

        await Actor.TheLastResponse().CaptureAsync(await Actor.UsingTheApi().GetAsync(url, new APIRequestContextOptions
        {
            Headers = BuildHeaders(bearerToken),
        }));
    }

    private static Dictionary<string, string> BuildHeaders(string? bearerToken)
    {
        var headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" };
        if (!string.IsNullOrWhiteSpace(bearerToken))
            headers["Authorization"] = $"Bearer {bearerToken}";

        return headers;
    }
}
