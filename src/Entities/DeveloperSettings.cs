using System.Xml.Serialization;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities {
    [XmlRoot("DeveloperSettings", Namespace = "http://www.aspenlaub.net")]
    public class DeveloperSettings : ISecretResult<DeveloperSettings> {
        public string Author { get; set; }
        public string Email { get; set; }
        public string GitHubRepositoryUrl { get; set; }
        public string FaviconUrl { get; set; }

        public DeveloperSettings Clone() {
            return new DeveloperSettings {
                Author = Author,
                Email = Email,
                GitHubRepositoryUrl = GitHubRepositoryUrl,
                FaviconUrl = FaviconUrl
            };
        }
    }
}
