using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities {
    public class PackageToPush : IPackageToPush {
        public string PackageFileFullName { get; set; }
        public string FeedUrl { get; set; }
        public string ApiKey { get; set; }
        public string Id { get; set; }
        public string Version { get; set; }
    }
}
