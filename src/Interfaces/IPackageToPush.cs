﻿namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces {
    public interface IPackageToPush {
        string PackageFileFullName { get; set; }
        string FeedUrl { get; set; }
        string ApiKey { get; set; }
        string Id { get; set; }
        string Version { get; set; }
    }
}