using EnbMaps.Tools;

string command = args.FirstOrDefault()?.ToLowerInvariant() ?? "help";
string repositoryRoot = FindRepositoryRoot();
string originalDirectory = Path.Combine(repositoryRoot, "data", "original");

switch (command)
{
    case "scrape":
        await new Scraper(originalDirectory).RunAsync();
        break;

    case "extract":
        new Extractor(originalDirectory, repositoryRoot).Run();
        break;

    default:
        Console.WriteLine("""
            EnbMaps archival tool

            Usage:
              dotnet run --project EnbMaps.Tools -- scrape    Download the original site into data/original
              dotnet run --project EnbMaps.Tools -- extract   Parse data/original into data/normalized/enbmaps.json
                                                            and copy it (plus images) into EnbMaps/wwwroot
            """);
        break;
}

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EnbMaps.slnx")))
    {
        directory = directory.Parent;
    }

    return directory?.FullName ?? Directory.GetCurrentDirectory();
}
