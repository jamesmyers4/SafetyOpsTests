using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using EsamsTests.Config;
using EsamsTests.Pages;
using EsamsTests.Helpers;

namespace EsamsTests.Tests;

[TestFixture]
public class AddUserTests : PageTest
{
    private string? _createdUserSearchToken;
    private string? _createdUserFullName;

    [SetUp]
    public async Task SetUp()
    {
        await LoginPage.Login(Page, ConfigLoader.Settings);
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_createdUserSearchToken != null)
            await CleanupCreatedUser(Page);
    }

    private static string MakeUniqueUserSuffix()
    {
        var ticks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        return $"auto_{ticks[^5..]}_{new Random().Next(9999)}";
    }

    private static async Task NavigateToPersonnelAdminHome(IPage page)
    {
        await page.GetByRole(AriaRole.Link, new() { Name = "Modules" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { NameRegex = new System.Text.RegularExpressions.Regex("Personnel Administration", System.Text.RegularExpressions.RegexOptions.IgnoreCase) }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    private static async Task<ILocator?> FindPersonnelSearchInput(IPage page)
    {
        var candidates = new[]
        {
            page.GetByPlaceholder(new System.Text.RegularExpressions.Regex("search|find|user", System.Text.RegularExpressions.RegexOptions.IgnoreCase)).First,
            page.GetByRole(AriaRole.Textbox, new() { NameRegex = new System.Text.RegularExpressions.Regex("search|find|user", System.Text.RegularExpressions.RegexOptions.IgnoreCase) }).First,
            page.GetByRole(AriaRole.Textbox).First,
        };

        foreach (var candidate in candidates)
        {
            try
            {
                if (await candidate.IsVisibleAsync())
                    return candidate;
            }
            catch
            {
                continue;
            }
        }
        return null;
    }

    private async Task CleanupCreatedUser(IPage page)
    {
        if (_createdUserSearchToken == null) return;

        try
        {
            await NavigateToPersonnelAdminHome(page);

            var searchInput = await FindPersonnelSearchInput(page);
            if (searchInput == null)
            {
                Console.WriteLine("Cleanup: no personnel search input was found.");
                return;
            }

            await searchInput.FillAsync(_createdUserSearchToken);

            try { await searchInput.PressAsync("Enter"); } catch { }

            var searchButton = page.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex("search|find", System.Text.RegularExpressions.RegexOptions.IgnoreCase) }).First;
            try
            {
                if (await searchButton.IsVisibleAsync())
                    await searchButton.ClickAsync();
            }
            catch { }

            try { await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded); } catch { }

            var userRow = page.Locator("tr").Filter(new() { HasText = _createdUserSearchToken }).First;
            if (!await userRow.IsVisibleAsync().ConfigureAwait(false))
            {
                Console.WriteLine($"Cleanup: user row not found for token {_createdUserSearchToken}");
                return;
            }

            var deleteButton = userRow.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex("delete|remove", System.Text.RegularExpressions.RegexOptions.IgnoreCase) }).First;
            if (await deleteButton.IsVisibleAsync())
            {
                await deleteButton.ClickAsync();
            }
            else
            {
                var actionsMenu = userRow.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex("actions|more|options", System.Text.RegularExpressions.RegexOptions.IgnoreCase) }).First;
                if (await actionsMenu.IsVisibleAsync())
                {
                    await actionsMenu.ClickAsync();
                    var menuDelete = page.GetByRole(AriaRole.Menuitem, new() { NameRegex = new System.Text.RegularExpressions.Regex("delete|remove", System.Text.RegularExpressions.RegexOptions.IgnoreCase) }).First;
                    if (await menuDelete.IsVisibleAsync())
                        await menuDelete.ClickAsync();
                }
            }

            var confirmButton = page.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex("confirm|yes|delete|remove", System.Text.RegularExpressions.RegexOptions.IgnoreCase) }).First;
            if (await confirmButton.IsVisibleAsync())
                await confirmButton.ClickAsync();

            try { await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded); } catch { }
            Console.WriteLine($"Cleanup: deleted user {_createdUserFullName ?? _createdUserSearchToken}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cleanup: failed to remove user {_createdUserFullName ?? _createdUserSearchToken}: {ex.Message}");
        }
        finally
        {
            _createdUserSearchToken = null;
            _createdUserFullName = null;
        }
    }

    private static async Task NavigateToAddNewUser(IPage page)
    {
        await page.GetByRole(AriaRole.Link, new() { Name = "Modules" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { NameRegex = new System.Text.RegularExpressions.Regex("Personnel Administration", System.Text.RegularExpressions.RegexOptions.IgnoreCase) }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Add New User" }).ClickAsync();
        await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex("/personnel/create", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Add New User", Level = 3 })).ToBeVisibleAsync();
    }

    private static async Task PickRandomFromSelectList(IPage page, string fieldLabel)
    {
        var titleMap = new Dictionary<string, string>
        {
            { "Department", "Select a Department" },
            { "Employee Category", "Select an Employee Category" },
            { "Organization", "Select an Organization" },
            { "Location", "Select a Location" },
        };

        if (!titleMap.TryGetValue(fieldLabel, out var title))
            throw new Exception($"No title mapping found for field: \"{fieldLabel}\"");

        var fieldSection = page.GetByTitle(title);
        await fieldSection.GetByRole(AriaRole.Button, new() { Name = "Open Select List" }).ClickAsync();

        var dialog = page.GetByRole(AriaRole.Dialog);
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        var dataRows = dialog.GetByRole(AriaRole.Row).Filter(new() { Has = page.GetByRole(AriaRole.Checkbox) });
        await dataRows.First.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        var rows = await dataRows.AllAsync();
        if (rows.Count == 0) throw new Exception($"No data rows found in dialog for: \"{fieldLabel}\"");

        var pick = rows[new Random().Next(rows.Count)];
        await pick.GetByRole(AriaRole.Checkbox).ClickAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    private static async Task PickRandomSubscription(IPage page)
    {
        await page.GetByText("Subscriptions", new() { Exact = true }).ClickAsync();

        var dialog = page.GetByRole(AriaRole.Dialog);
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        var dataRows = dialog.GetByRole(AriaRole.Row).Filter(new() { Has = page.GetByRole(AriaRole.Checkbox) });
        await dataRows.First.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        var rows = await dataRows.AllAsync();
        if (rows.Count == 0) throw new Exception("No data rows found in Subscriptions dialog");

        var pick = rows[new Random().Next(rows.Count)];
        await pick.GetByRole(AriaRole.Checkbox).ClickAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    private static async Task PickRandomGender(IPage page)
    {
        var combobox = page.GetByRole(AriaRole.Combobox, new() { Name = "Gender" });
        await combobox.ClickAsync();

        var listbox = page.GetByRole(AriaRole.Listbox);
        await listbox.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        var options = listbox.GetByRole(AriaRole.Option).Filter(new() { HasNotTextRegex = new System.Text.RegularExpressions.Regex("select", System.Text.RegularExpressions.RegexOptions.IgnoreCase) });
        await options.First.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        var items = await options.AllAsync();
        var pick = items[new Random().Next(items.Count)];
        await pick.ClickAsync();
        await listbox.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
    }

    private async Task FillAddNewUserForm(IPage page)
    {
        var uniqueSuffix = MakeUniqueUserSuffix();
        var first = RandomName.WeightedRandomFirstName();
        var middle = RandomName.WeightedRandomMiddleName();
        var last = RandomName.WeightedRandomLastName();

        var firstName = $"{first.Resolve()} {uniqueSuffix}";
        var middleName = $"{middle.Resolve()} {uniqueSuffix}";
        var lastName = $"{last.Resolve()} {uniqueSuffix}";

        _createdUserSearchToken = uniqueSuffix;
        _createdUserFullName = $"{firstName} {lastName}";

        var reasons = new[] { first.GetReason(), middle.GetReason(), last.GetReason() }
            .Where(r => r != null)
            .ToList();

        if (reasons.Count > 0)
            TestContext.Out.WriteLine($"Adversarial: {string.Join(" | ", reasons)}");

        await PickRandomFromSelectList(page, "Department");
        await PickRandomSubscription(page);
        await PickRandomGender(page);
        await PickRandomFromSelectList(page, "Employee Category");
        await page.GetByLabel("First Name").FillAsync(firstName);
        await page.GetByLabel("Last Name").FillAsync(lastName);
        await page.GetByLabel("Middle Name").FillAsync(middleName);
        await page.GetByRole(AriaRole.Button, new() { Name = "Generate Random Number" }).ClickAsync();
    }

    [Test]
    public async Task FillsAndSubmitsAddNewUserForm()
    {
        await NavigateToAddNewUser(Page);
        await FillAddNewUserForm(Page);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Add User" }).ClickAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Add Another User" })).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Return to PA Home" })).ToBeVisibleAsync();
    }
}