using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities {
    public class SecretNugetFeeds : ISecret<NugetFeeds> {
        private NugetFeeds vDefaultValue;
        public NugetFeeds DefaultValue => vDefaultValue ?? (vDefaultValue = new NugetFeeds {
            new NugetFeed { Id = "main", IsMainFeed = true, Url = "http://localhost/main/nuget/" },
            new NugetFeed { Id = "experimental", IsMainFeed = false, Url = "http://localhost/experimental/nuget/" },
        });

        public string Guid => "E7E9D86F-C8C6-49DA-BFEC-D8A8233BCAC3";
    }
}
