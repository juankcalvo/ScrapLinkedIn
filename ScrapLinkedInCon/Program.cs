class Program
{
    static async Task Main(string[] args)
    {

        ScrapLinkedin scrapLinkedin = new ScrapLinkedin();
        await scrapLinkedin.LinkedinUsers();


        Console.ReadKey();
    }
}
