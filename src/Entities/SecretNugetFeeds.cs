using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;

public class SecretNugetFeeds : ISecret<NugetFeeds> {
    private NugetFeeds _DefaultNugetFeeds;
    public NugetFeeds DefaultValue => _DefaultNugetFeeds ??= new NugetFeeds {
        new() { Id = "main", IsMainFeed = true, Url = "http://localhost/main/nuget/" },
        new() { Id = "experimental", IsMainFeed = false, Url = "http://localhost/experimental/nuget/" },
    };

    public string Guid => "E7E9D86F-C8C6-49DA-BFEC-D8A8233BCAC3";
}