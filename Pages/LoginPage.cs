using Microsoft.Playwright;
using EsamsTests.Config;

namespace EsamsTests.Pages;

public static class LoginPage
{
    public static async Task Login(IPage page, TestSettings settings)
    {
        var baseUrl = settings.BaseUrl;
        var username = settings.LoginUsername;
        var password = settings.LoginPassword;
        var mainUrl = settings.EsamsMainUrl;

        if (string.IsNullOrEmpty(baseUrl)) throw new Exception("BaseUrl is not set in appsettings.json");
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)) throw new Exception("LoginUsername or LoginPassword is not set in appsettings.json");

        await page.Context.ClearCookiesAsync();
        await page.GotoAsync(baseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.Commit, Timeout = 60_000 });

        var loginSplash = page.Locator("button, a, input[type='submit']")
            .Filter(new LocatorFilterOptions { HasText = System.Text.RegularExpressions.Regex.Match("", @"login to esams").ToString() });

        var loginButton = page.Locator("button, a, input[type='submit']")
            .Filter(new LocatorFilterOptions { HasTextRegex = new System.Text.RegularExpressions.Regex(@"login to esams", System.Text.RegularExpressions.RegexOptions.IgnoreCase) })
            .First;

        await loginButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await loginButton.ClickAsync();

        var userInput = page.GetByLabel("Username");
        var passInput = page.GetByLabel("Password");

        await userInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 20_000 });
        await passInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 20_000 });

        await userInput.FillAsync(username);
        await passInput.FillAsync(password);

        var submitButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "login", Exact = false });

        try
        {
            await Task.WhenAll(
                page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(@"/n/esams/", System.Text.RegularExpressions.RegexOptions.IgnoreCase), new PageWaitForURLOptions { Timeout = 20_000 }),
                submitButton.ClickAsync()
            );
        }
        catch
        {
            await page.GotoAsync(mainUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(@"/n/esams/", System.Text.RegularExpressions.RegexOptions.IgnoreCase), new PageWaitForURLOptions { Timeout = 20_000 });
        }

        await Assertions.Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(@"/n/esams/", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }
}