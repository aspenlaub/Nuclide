using System;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;

public class DeveloperSettingsSecret : ISecret<DeveloperSettings> {
    private DeveloperSettings DefaultDeveloperSettings;
    public DeveloperSettings DefaultValue => DefaultDeveloperSettings ??= Sample();

    private DeveloperSettings Sample() {
        return new() {
            Author = Environment.UserName,
            Email = Environment.UserName.Replace('@', '-').Replace(' ', '-') + "@" + Guid + ".com",
            GitHubRepositoryUrl = "https://github.com/" + Guid,
            FaviconUrl = "https://www." + Guid + ".net/favicon.ico"
        };
    }

    public string Guid => "D835A5-C9CA6C10-7409-487F-B406-A9EF9A";
}