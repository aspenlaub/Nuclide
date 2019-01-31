using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Protch.Interfaces;
using NuGet.Common;
using NuGet.Protocol;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide {
    public class NugetPackageToPushFinder : INugetPackageToPushFinder {
        private readonly IGitUtilities vGitUtilities;
        private readonly INugetConfigReader vNugetConfigReader;
        private readonly INugetFeedLister vNugetFeedLister;
        private readonly IProjectFactory vProjectFactory;
        private readonly ISecretRepository vSecretRepository;

        public NugetPackageToPushFinder(IGitUtilities gitUtilities, INugetConfigReader nugetConfigReader, INugetFeedLister nugetFeedLister, IProjectFactory projectFactory, ISecretRepository secretRepository) {
            vGitUtilities = gitUtilities;
            vNugetConfigReader = nugetConfigReader;
            vNugetFeedLister = nugetFeedLister;
            vProjectFactory = projectFactory;
            vSecretRepository = secretRepository;
        }

        public async Task<IPackageToPush> FindPackageToPushAsync(IFolder packageFolderWithBinaries, IFolder repositoryFolder, string solutionFileFullName, IErrorsAndInfos errorsAndInfos) {
            IPackageToPush packageToPush = new PackageToPush();
            var projectFileFullName = solutionFileFullName.Replace(".sln", ".csproj");
            if (!File.Exists(projectFileFullName)) {
                errorsAndInfos.Errors.Add(string.Format(Properties.Resources.ProjectFileNotFound, projectFileFullName));
                return packageToPush;
            }

            var project = vProjectFactory.Load(solutionFileFullName, projectFileFullName, errorsAndInfos);
            if (errorsAndInfos.Errors.Any()) { return packageToPush; }

            var developerSettingsSecret = new DeveloperSettingsSecret();
            var developerSettings = await vSecretRepository.GetAsync(developerSettingsSecret, errorsAndInfos);
            if (errorsAndInfos.Errors.Any()) { return packageToPush; }

            if (developerSettings == null) {
                errorsAndInfos.Errors.Add(string.Format(Properties.Resources.MissingDeveloperSettings, developerSettingsSecret.Guid + ".xml"));
                return packageToPush;
            }

            var feedId = developerSettings.NugetFeedId;
            if (string.IsNullOrEmpty(feedId)) {
                errorsAndInfos.Errors.Add(string.Format(Properties.Resources.IncompleteDeveloperSettings, developerSettingsSecret.Guid + ".xml"));
                return packageToPush;
            }

            var nugetConfigFileFullName = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\NuGet\" + "nuget.config";
            packageToPush.ApiKey = vNugetConfigReader.GetApiKey(nugetConfigFileFullName, feedId, errorsAndInfos);
            if (errorsAndInfos.Errors.Any()) { return packageToPush; }

            packageToPush.FeedUrl = developerSettings.NugetFeedUrl;
            if (string.IsNullOrEmpty(packageToPush.FeedUrl)) {
                errorsAndInfos.Errors.Add(string.Format(Properties.Resources.IncompleteDeveloperSettings, developerSettingsSecret.Guid + ".xml"));
                return packageToPush;
            }

            var localPackageRepository = new FindLocalPackagesResourceV2(packageFolderWithBinaries.FullName);
            var localPackages = localPackageRepository.GetPackages(new NullLogger(), CancellationToken.None).Where(p => !p.Identity.Version.IsPrerelease).ToList();
            if (!localPackages.Any()) {
                errorsAndInfos.Errors.Add(string.Format(Properties.Resources.NoPackageFilesFound, packageFolderWithBinaries.FullName));
                return packageToPush;
            }

            var latestLocalPackageVersion = localPackages.Max(p => p.Identity.Version.Version);

            var packageId = project.RootNamespace;
            var remotePackages = await vNugetFeedLister.ListReleasedPackagesAsync(packageToPush.FeedUrl, packageId);
            if (!remotePackages.Any()) {
                errorsAndInfos.Errors.Add(string.Format(Properties.Resources.NoRemotePackageFilesFound, packageToPush.FeedUrl, packageId));
                return packageToPush;
            }

            var latestRemotePackageVersion = remotePackages.Max(p => p.Identity.Version.Version);
            if (latestRemotePackageVersion >= latestLocalPackageVersion) { return packageToPush; }

            var remotePackage = remotePackages.First(p => p.Identity.Version.Version == latestRemotePackageVersion);
            if (!string.IsNullOrEmpty(remotePackage.Tags) && repositoryFolder != null) {
                var headTipIdSha = vGitUtilities.HeadTipIdSha(repositoryFolder);
                var tags = remotePackage.Tags.Split(' ');
                if (tags.Contains(headTipIdSha)) { return packageToPush; }
            }

            packageToPush.PackageFileFullName = packageFolderWithBinaries.FullName + @"\" + packageId + "." + latestLocalPackageVersion + ".nupkg";
            if (File.Exists(packageToPush.PackageFileFullName)) { return packageToPush; }

            errorsAndInfos.Errors.Add(string.Format(Properties.Resources.FileNotFound, packageToPush.PackageFileFullName));
            return packageToPush;
        }
    }
}
