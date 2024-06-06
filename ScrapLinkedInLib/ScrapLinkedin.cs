﻿using PuppeteerSharp;
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
        var email = "testmailfeka00@gmail.com";
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

            using (var context = new LinkedInContext())
            {
                foreach (var href in hrefs)
                {
                    var newPage = await browser.NewPageAsync();
                    await newPage.GoToAsync(href);
                    await newPage.WaitForSelectorAsync("div.display-flex");

                    var name = await GetInnerTextByMultipleXPaths(newPage,
                        "//h1[contains(@class,'text-heading-xlarge inline')]");
                    var headline = await GetInnerTextByMultipleXPaths(newPage,
                        "//div[@class='text-body-medium break-words']");
                    var location = await GetInnerTextByMultipleXPaths(newPage,
                        "//span[contains(@class,'text-body-small inline')]");

                    // Verificar si el perfil ya existe en la base de datos
                    var existingProfile = await context.UserProfiles
                        .FirstOrDefaultAsync(up => up.Name == name && up.Headline == headline && up.Location == location);

                    if (existingProfile == null)
                    {
                        // Extract four experiences
                        var experience1 = await GetExperienceDetails(newPage, 1);
                        var experience2 = await GetExperienceDetails(newPage, 2);
                        var experience3 = await GetExperienceDetails(newPage, 3);
                        var experience4 = await GetExperienceDetails(newPage, 4);

                        // Extract four educations
                        var education1 = await GetEducationDetails(newPage, 1);
                        var education2 = await GetEducationDetails(newPage, 2);
                        var education3 = await GetEducationDetails(newPage, 3);
                        var education4 = await GetEducationDetails(newPage, 4);

                        // Extract four licenses and certifications
                        var license1 = await GetLicenseDetails(newPage, 1);
                        var license2 = await GetLicenseDetails(newPage, 2);
                        var license3 = await GetLicenseDetails(newPage, 3);
                        var license4 = await GetLicenseDetails(newPage, 4);

                        var userProfile = new UserProfile
                        {
                            Name = name,
                            Headline = headline,
                            Location = location,
                            Experiences = new List<Experience>
                            {
                                experience1,
                                experience2,
                                experience3,
                                experience4
                            },
                            Educations = new List<Education>
                            {
                                education1,
                                education2,
                                education3,
                                education4
                            },
                            LicenseCertifications = new List<LicenseCertification>
                            {
                                license1,
                                license2,
                                license3,
                                license4
                            }
                        };

                        context.UserProfiles.Add(userProfile);
                        await context.SaveChangesAsync();
                    }
                    else
                    {
                        Console.WriteLine($"Profile for {name} already exists.");
                    }

                    await newPage.CloseAsync();

                    // Add delay between each profile to avoid 426 error
                    await Task.Delay(5000); // 5 seconds
                }
            }

            await Task.Delay(-1);
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
        }
    }

    private async Task<Experience> GetExperienceDetails(IPage page, int position)
    {
        var title = await GetInnerTextByMultipleXPaths(page,
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[5]/div[3]/ul/li[{position}]/div/div[2]/div[1]/div/div/div/div/div/span[1]",
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[4]/div[3]/ul/li[{position}]/div/div[2]/div[1]/div/div/div/div/div/span[1]");

        var company = await GetInnerTextByMultipleXPaths(page,
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[5]/div[3]/ul/li[{position}]/div/div[2]/div/div/span[1]/span[1]",
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[4]/div[3]/ul/li[{position}]/div/div[2]/div[1]/div/span[1]/span[1]");

        var duration = await GetInnerTextByMultipleXPaths(page,
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[5]/div[3]/ul/li[{position}]/div/div[2]/div/div/span[2]/span[1]",
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[4]/div[3]/ul/li[{position}]/div/div[2]/div[1]/div/span[2]/span[1]");

        return new Experience
        {
            Details = $"{title} at {company} ({duration})"
        };
    }

    private async Task<Education> GetEducationDetails(IPage page, int position)
    {
        var degree = await GetInnerTextByMultipleXPaths(page,
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[6]/div[3]/ul/li[{position}]/div/div[2]/div/a/span[1]/span[1]",
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[5]/div[3]/ul/li[{position}]/div/div[2]/div[1]/a/span[1]/span[1]");

        var duration = await GetInnerTextByMultipleXPaths(page,
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[6]/div[3]/ul/li[{position}]/div/div[2]/div/a/span[2]/span[1]",
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[5]/div[3]/ul/li[{position}]/div/div[2]/div[1]/a/span[2]/span[1]");

        return new Education
        {
            Details = $"{degree} ({duration})"
        };
    }

    private async Task<LicenseCertification> GetLicenseDetails(IPage page, int position)
    {
        var school = await GetInnerTextByMultipleXPaths(page,
            $"//*[@id=\"profile-content\"]/div/div[2]/div/div/main/section[7]/div[3]/ul/li[{position}]/div/div[2]/div[1]/a/span[1]/span[1]",
            "otro_xpath_para_school"); // Puedes agregar más XPaths aquí cuando los tengas

        return new LicenseCertification
        {
            Details = school
        };
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