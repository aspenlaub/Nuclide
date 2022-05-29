// ReSharper disable UnusedMember.Global
namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;

public class NugetFeed {
    public const string AspenlaubNetFeed = "aspenlaub.net", AspenlaubGitHubFeed = "aspenlaub.github.com", NugetOrgFeed = "nuget.org";
    public const string AspenlaubLocalFeed = "aspenlaub.local";

    public string Id { get; set; }
    public bool IsMainFeed { get; set; }

    public string Url { get; set; }
    public string UserForReadAccess { get; set; } = "";
    public string PasswordForReadAccess { get; set; } = "";

    public bool IsAFolderToResolve() {
        return Url.StartsWith("$(");
    }
}