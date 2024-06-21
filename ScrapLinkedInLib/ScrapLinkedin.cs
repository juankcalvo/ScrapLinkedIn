using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScrapLinkedInDall.Database.Models;
using ScrapLinkedInDall.Database.Contexto;

public class ScrapLinkedin
{
    public async Task LinkedinUsers()
    {
        var loginUrl = "https://www.linkedin.com/login";
        var email = "testmailfeka002@gmail.com";
        var password = "ThisEmailIsForTests";

        var fetcher = new BrowserFetcher();
        await fetcher.DownloadAsync();
        var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = false,
            Args = new string[] { "--start-maximized" }
        });

        var page = await browser.NewPageAsync();
        await page.SetViewportAsync(new ViewPortOptions { Width = 1920, Height = 1080 });

        await page.SetRequestInterceptionAsync(true);
        page.Request += (sender, e) =>
        {
            if (e.Request.ResourceType == ResourceType.Image)
                e.Request.AbortAsync().ConfigureAwait(false);
            else
                e.Request.ContinueAsync().ConfigureAwait(false);
        };

        try
        {
            await page.GoToAsync(loginUrl);
            await page.WaitForSelectorAsync("#username");
            await page.TypeAsync("#username", email);
            await page.TypeAsync("#password", password);
            await page.ClickAsync("div.login__form_action_container button[type='submit']");
            await page.WaitForNavigationAsync();
            await Task.Delay(30000);
            await page.GoToAsync("https://www.linkedin.com/in/viviana-hern%C3%A1ndez-%F0%9F%87%A8%F0%9F%87%B7-a9658129/");
            await page.WaitForXPathAsync("//div[@id='profile-content']/div[1]/div[2]/div[1]/div[1]/aside[1]/section[2]/div[3]/div[1]/div[1]/div[1]/a[1]");
            var showAllButton = await page.XPathAsync("//div[@id='profile-content']/div[1]/div[2]/div[1]/div[1]/aside[1]/section[2]/div[3]/div[1]/div[1]/div[1]/a[1]");

            if (showAllButton.Length > 0)
            {
                await showAllButton[0].ClickAsync();
                await Task.Delay(3000); // Esperar a que la ventana emergente cargue completamente
            }
            else
            {
                Console.WriteLine("Button not found.");
                return;
            }

            var profileLinksXPath = "//a[@data-field='browsemap_card_click']";
            await page.WaitForXPathAsync(profileLinksXPath);

            var profileLinks = await page.XPathAsync(profileLinksXPath);
            var hrefs = new List<string>();

            foreach (var profileLink in profileLinks)
            {
                var href = await profileLink.EvaluateFunctionAsync<string>("el => el.href");
                if (!string.IsNullOrEmpty(href))
                {
                    hrefs.Add(href);
                }
            }

            var visitedProfiles = new HashSet<string>();

            while (true) // Bucle infinito hasta que se detenga manualmente
            {
                if (hrefs.Count == 0)
                {
                    Console.WriteLine("No more profiles to visit.");
                    break;
                }

                var href = hrefs[0];
                hrefs.RemoveAt(0);

                if (visitedProfiles.Contains(href))
                {
                    continue;
                }

                visitedProfiles.Add(href);

                var newPage = await browser.NewPageAsync();
                await newPage.GoToAsync(href);

                await Task.Delay(5000); // Esperar 5 segundos para que el perfil cargue completamente

                await newPage.WaitForSelectorAsync("div.display-flex");

                var name = await GetInnerTextByMultipleXPaths(newPage,
                    "//h1[contains(@class,'text-heading-xlarge inline')]");
                var headline = await GetInnerTextByMultipleXPaths(newPage,
                    "//div[@class='text-body-medium break-words']");
                var location = await GetInnerTextByMultipleXPaths(newPage,
                    "//span[contains(@class,'text-body-small inline')]");

                using (var context = new LinkedInContext())
                {
                    var existingProfile = await context.UserProfiles
                        .FirstOrDefaultAsync(up => up.Name == name && up.Headline == headline && up.Location == location);

                    if (existingProfile == null)
                    {
                        var experiences = new List<Experience>();
                        var experience1 = await GetExperienceDetails(newPage, 1);
                        var experience2 = await GetExperienceDetails(newPage, 2);
                        var experience3 = await GetExperienceDetails(newPage, 3);
                        var experience4 = await GetExperienceDetails(newPage, 4);

                        if (!string.IsNullOrEmpty(experience1.Details) && experience1.Details != " at ()") experiences.Add(experience1);
                        if (!string.IsNullOrEmpty(experience2.Details) && experience2.Details != " at ()") experiences.Add(experience2);
                        if (!string.IsNullOrEmpty(experience3.Details) && experience3.Details != " at ()") experiences.Add(experience3);
                        if (!string.IsNullOrEmpty(experience4.Details) && experience4.Details != " at ()") experiences.Add(experience4);

                        var educations = new List<Education>();
                        var education1 = await GetEducationDetails(newPage, 1);
                        var education2 = await GetEducationDetails(newPage, 2);
                        var education3 = await GetEducationDetails(newPage, 3);
                        var education4 = await GetEducationDetails(newPage, 4);

                        if (!string.IsNullOrEmpty(education1.Details) && education1.Details != " ()") educations.Add(education1);
                        if (!string.IsNullOrEmpty(education2.Details) && education2.Details != " ()") educations.Add(education2);
                        if (!string.IsNullOrEmpty(education3.Details) && education3.Details != " ()") educations.Add(education3);
                        if (!string.IsNullOrEmpty(education4.Details) && education4.Details != " ()") educations.Add(education4);

                        var licenses = new List<LicenseCertification>();
                        var license1 = await GetLicenseDetails(newPage, 1);
                        var license2 = await GetLicenseDetails(newPage, 2);
                        var license3 = await GetLicenseDetails(newPage, 3);
                        var license4 = await GetLicenseDetails(newPage, 4);

                        if (!string.IsNullOrEmpty(license1.Details)) licenses.Add(license1);
                        if (!string.IsNullOrEmpty(license2.Details)) licenses.Add(license2);
                        if (!string.IsNullOrEmpty(license3.Details)) licenses.Add(license3);
                        if (!string.IsNullOrEmpty(license4.Details)) licenses.Add(license4);

                        var userProfile = new UserProfile
                        {
                            Name = name,
                            Headline = headline,
                            Location = location,
                            Experiences = experiences,
                            Educations = educations,
                            LicenseCertifications = licenses
                        };

                        context.UserProfiles.Add(userProfile);
                        await context.SaveChangesAsync();
                    }
                    else
                    {
                        Console.WriteLine($"Profile for {name} already exists.");
                    }
                }

                var newShowAllButton = await newPage.XPathAsync("//div[@id='profile-content']/div[1]/div[2]/div[1]/div[1]/aside[1]/section[2]/div[3]/div[1]/div[1]/div[1]/a[1]");
                if (newShowAllButton.Length > 0)
                {
                    await newShowAllButton[0].ClickAsync();
                    await Task.Delay(000);

                    var newProfileLinks = await newPage.XPathAsync(profileLinksXPath);
                    foreach (var newProfileLink in newProfileLinks)
                    {
                        var newHref = await newProfileLink.EvaluateFunctionAsync<string>("el => el.href");
                        if (!string.IsNullOrEmpty(newHref) && !visitedProfiles.Contains(newHref))
                        {
                            hrefs.Add(newHref);
                        }
                    }
                }

                await newPage.CloseAsync();

                await Task.Delay(15000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
        }
    }

    private async Task<Experience> GetExperienceDetails(IPage page, int position)
    {
        var title = await GetInnerTextByMultipleXPaths(page,
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[3]/div[3]/ul/li[{position}]/div/div[2]/div[1]/div/div/div/div/div/span[1]",
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[4]/div[3]/ul/li[{position}]/div/div[2]/div[1]/div/div/div/div/div/span[1]",
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[4]/div[3]/ul/li[{position}]/div/div[2]/div/div/div/div/div/div/span[1]",
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[5]/div[3]/ul/li[{position}]/div/div[2]/div[1]/div/div/div/div/div/span[1]",
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[6]/div[3]/ul/li[{position}]/div/div[2]/div[1]/div/div/div/div/div/span[1]");

        var company = await GetInnerTextByMultipleXPaths(page,
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[3]/div[3]/ul/li[{position}]/div/div[2]/div[1]/a/div/div/div/div/span[1]",
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[4]/div[3]/ul/li[{position}]/div/div[2]/div/div/span[1]/span[1]",
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[4]/div[3]/ul/li[{position}]/div/div[2]/div[1]/div/span[1]/span[1]",
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[4]/div[3]/ul/li[{position}]/div/div[2]/div[1]/a/div/div/div/div/span[1]",
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[5]/div[3]/ul/li[{position}]/div/div[2]/div/div/span[1]/span[1]",
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[6]/div[3]/ul/li[{position}]/div/div[2]/div[1]/div/span[1]/span[1]");

        var duration = await GetInnerTextByMultipleXPaths(page,
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[3]/div[3]/ul/li[{position}]/div/div[2]/div[1]/a/span/span[1]",
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[4]/div[3]/ul/li[{position}]/div/div[2]/div[1]/a/span/span[1]",
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[4]/div[3]/ul/li[{position}]/div/div[2]/div[1]/div/span[2]/span[1]",
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[5]/div[3]/ul/li[{position}]/div/div[2]/div/div/span[2]/span[1]",
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[6]/div[3]/ul/li[{position}]/div/div[2]/div[1]/div/span[2]/span[1]");

        var details = $"{title} at {company} ({duration})";
        return new Experience { Details = string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(company) && string.IsNullOrWhiteSpace(duration) ? string.Empty : details };
    }

    private async Task<Education> GetEducationDetails(IPage page, int position)
    {
        var degree = await GetInnerTextByMultipleXPaths(page,
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[5]/div[3]/ul/li[{position}]/div/div[2]/div[1]/a/span[1]/span[1]",
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[6]/div[3]/ul/li[{position}]/div/div[2]/div/a/span[1]/span[1]");

        var duration = await GetInnerTextByMultipleXPaths(page,
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[5]/div[3]/ul/li[{position}]/div/div[2]/div[1]/a/span[2]/span[1]",
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[6]/div[3]/ul/li[{position}]/div/div[2]/div/a/span[2]/span[1]");

        var details = $"{degree} ({duration})";
        return new Education { Details = string.IsNullOrWhiteSpace(degree) && string.IsNullOrWhiteSpace(duration) ? string.Empty : details };
    }

    private async Task<LicenseCertification> GetLicenseDetails(IPage page, int position)
    {
        var school = await GetInnerTextByMultipleXPaths(page,
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[7]/div[3]/ul/li[{position}]/div/div[2]/div[1]/a/span[1]/span[1]",
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[6]/div[3]/ul/li[{position}]/div/div[2]/div/div/div/div/div/div/span[1]");

        return new LicenseCertification { Details = string.IsNullOrWhiteSpace(school) ? string.Empty : school };
    }

    private async Task<string> GetInnerTextByMultipleXPaths(IPage page, params string[] xpaths)
    {
        foreach (var xpath in xpaths)
        {
            try
            {
                var element = await page.XPathAsync(xpath);
                if (element.Length > 0)
                {
                    return await element[0].EvaluateFunctionAsync<string>("el => el.innerText");
                }
            }
            catch
            {
                // Continúa con el siguiente XPath si este falla
                continue;
            }
        }
        return string.Empty; // Devuelve vacío si ninguno de los XPath funciona
    }
}