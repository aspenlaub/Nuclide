using System;
using System.IO;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities {
    public class DeveloperSettingsSecret : ISecret<DeveloperSettings> {
        private DeveloperSettings vDeveloperSettings;
        public DeveloperSettings DefaultValue => vDeveloperSettings ?? (vDeveloperSettings = Sample());

        private DeveloperSettings Sample() {
            return new DeveloperSettings {
                Author = Environment.UserName,
                Email = Environment.UserName.Replace('@', '-').Replace(' ', '-') + "@" + Guid + ".com",
                GitHubRepositoryUrl = "https://github.com/" + Guid,
                FaviconUrl = "https://www." + Guid + ".net/favicon.ico",
                NugetFeedUrl = "https://www." + Guid + "nuget" };
        }

        public string Guid => "C9CA6C10-7409-487F-B406-A9EF9AD835A5";
    }
}
