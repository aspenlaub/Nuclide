﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Protch.Interfaces;
using NuGet.Common;
using NuGet.Protocol;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide {
    public class NugetPackageToPushFinder : INugetPackageToPushFinder {
        private readonly IFolderResolver vFolderResolver;
        private readonly IGitUtilities vGitUtilities;
        private readonly INugetConfigReader vNugetConfigReader;
        private readonly INugetFeedLister vNugetFeedLister;
        private readonly IProjectFactory vProjectFactory;
        private readonly ISecretRepository vSecretRepository;
        private readonly IPushedHeadTipShaRepository vPushedHeadTipShaRepository;

        public NugetPackageToPushFinder(IFolderResolver folderResolver, IGitUtilities gitUtilities, INugetConfigReader nugetConfigReader, INugetFeedLister nugetFeedLister,
                IProjectFactory projectFactory, IPushedHeadTipShaRepository pushedHeadTipShaRepository, ISecretRepository secretRepository) {
            vFolderResolver = folderResolver;
            vGitUtilities = gitUtilities;
            vNugetConfigReader = nugetConfigReader;
            vNugetFeedLister = nugetFeedLister;
            vProjectFactory = projectFactory;
            vPushedHeadTipShaRepository = pushedHeadTipShaRepository;
            vSecretRepository = secretRepository;
        }

        public async Task<IPackageToPush> FindPackageToPushAsync(string nugetFeedId, IFolder packageFolderWithBinaries, IFolder repositoryFolder, string solutionFileFullName, IErrorsAndInfos errorsAndInfos) {
            IPackageToPush packageToPush = new PackageToPush();
            errorsAndInfos.Infos.Add(Properties.Resources.CheckingProjectVsSolution);
            var projectFileFullName = solutionFileFullName.Replace(".sln", ".csproj");
            if (!File.Exists(projectFileFullName)) {
                errorsAndInfos.Errors.Add(string.Format(Properties.Resources.ProjectFileNotFound, projectFileFullName));
                return packageToPush;
            }

            errorsAndInfos.Infos.Add(Properties.Resources.LoadingProject);
            var project = vProjectFactory.Load(solutionFileFullName, projectFileFullName, errorsAndInfos);
            if (errorsAndInfos.Errors.Any()) { return packageToPush; }

            errorsAndInfos.Infos.Add(Properties.Resources.LoadingNugetFeeds);
            var developerSettingsSecret = new DeveloperSettingsSecret();
            var developerSettings = await vSecretRepository.GetAsync(developerSettingsSecret, errorsAndInfos);
            if (errorsAndInfos.Errors.Any()) { return packageToPush; }

            if (developerSettings == null) {
                errorsAndInfos.Errors.Add(string.Format(Properties.Resources.MissingDeveloperSettings, developerSettingsSecret.Guid + ".xml"));
                return packageToPush;
            }

            var nugetFeedsSecret = new SecretNugetFeeds();
            var nugetFeeds = await vSecretRepository.GetAsync(nugetFeedsSecret, errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) {
                return packageToPush;
            }

            errorsAndInfos.Infos.Add(Properties.Resources.IdentifyingNugetFeed);
            var nugetFeed = nugetFeeds.FirstOrDefault(f => f.Id == nugetFeedId);
            if (nugetFeed == null) {
                errorsAndInfos.Errors.Add(string.Format(Properties.Resources.UnknownNugetFeed, nugetFeedId, nugetFeedsSecret.Guid + ".xml"));
                return packageToPush;
            }

            if (!nugetFeed.IsAFolderToResolve()) {
                var nugetConfigFileFullName = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\NuGet\" + "nuget.config";
                packageToPush.ApiKey = vNugetConfigReader.GetApiKey(nugetConfigFileFullName, nugetFeed.Id, errorsAndInfos);
                if (errorsAndInfos.Errors.Any()) { return packageToPush; }
            }

            errorsAndInfos.Infos.Add(Properties.Resources.IdentifyingFeedUrl);
            var source = nugetFeed.UrlOrResolvedFolder(vFolderResolver, errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return packageToPush; }

            packageToPush.FeedUrl = source;
            if (string.IsNullOrEmpty(packageToPush.FeedUrl)) {
                errorsAndInfos.Errors.Add(string.Format(Properties.Resources.IncompleteDeveloperSettings, developerSettingsSecret.Guid + ".xml"));
                return packageToPush;
            }

            errorsAndInfos.Infos.Add(Properties.Resources.SearchingLocalPackage);
            var localPackageRepository = new FindLocalPackagesResourceV2(packageFolderWithBinaries.FullName);
            var localPackages = localPackageRepository.GetPackages(new NullLogger(), CancellationToken.None).Where(p => !p.Identity.Version.IsPrerelease).ToList();
            if (!localPackages.Any()) {
                errorsAndInfos.Errors.Add(string.Format(Properties.Resources.NoPackageFilesFound, packageFolderWithBinaries.FullName));
                return packageToPush;
            }

            var latestLocalPackageVersion = localPackages.Max(p => p.Identity.Version.Version);
            errorsAndInfos.Infos.Add(string.Format(Properties.Resources.FoundLocalPackage, latestLocalPackageVersion));

            errorsAndInfos.Infos.Add(Properties.Resources.SearchingRemotePackage);
            var packageId = string.IsNullOrWhiteSpace(project.PackageId) ? project.RootNamespace : project.PackageId;
            var remotePackages = await vNugetFeedLister.ListReleasedPackagesAsync(nugetFeedId, packageId, errorsAndInfos);
            if (errorsAndInfos.Errors.Any()) { return packageToPush; }
            if (!remotePackages.Any()) {
                errorsAndInfos.Errors.Add(string.Format(Properties.Resources.NoRemotePackageFilesFound, packageToPush.FeedUrl, packageId));
                return packageToPush;
            }

            errorsAndInfos.Infos.Add(Properties.Resources.LoadingPushedHeadTipShas);
            var pushedHeadTipShas = vPushedHeadTipShaRepository.Get(nugetFeedId, errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return packageToPush; }

            var headTipIdSha = repositoryFolder == null ? "" : vGitUtilities.HeadTipIdSha(repositoryFolder);
            if (!string.IsNullOrWhiteSpace(headTipIdSha) && pushedHeadTipShas.Contains(headTipIdSha)) {
                errorsAndInfos.Infos.Add(string.Format(Properties.Resources.HeadTipShaNotPushed, headTipIdSha));
                return packageToPush;
            }

            var latestRemotePackageVersion = remotePackages.Max(p => p.Identity.Version.Version);
            errorsAndInfos.Infos.Add(string.Format(Properties.Resources.FoundRemotePackage, latestRemotePackageVersion));
            if (latestRemotePackageVersion >= latestLocalPackageVersion) {
                errorsAndInfos.Infos.Add(string.Format(Properties.Resources.RemotePackageHasHigherOrEqualVersion, headTipIdSha));
                return packageToPush;
            }

            errorsAndInfos.Infos.Add(Properties.Resources.CheckingRemotePackageTag);
            var remotePackage = remotePackages.First(p => p.Identity.Version.Version == latestRemotePackageVersion);
            if (!string.IsNullOrEmpty(remotePackage.Tags) && !string.IsNullOrWhiteSpace(headTipIdSha)) {
                errorsAndInfos.Infos.Add(string.Format(Properties.Resources.TagsAre, remotePackage.Tags));
                var tags = remotePackage.Tags.Split(' ');
                if (tags.Contains(headTipIdSha)) {
                    errorsAndInfos.Infos.Add(string.Format(Properties.Resources.PackageAlreadyTaggedWithHeadTipSha, headTipIdSha));
                    return packageToPush;
                }
            }

            errorsAndInfos.Infos.Add(Properties.Resources.PackageNeedsToBePushed);
            packageToPush.PackageFileFullName = packageFolderWithBinaries.FullName + @"\" + packageId + "." + latestLocalPackageVersion + ".nupkg";
            packageToPush.Id = packageId;
            packageToPush.Version = latestLocalPackageVersion.ToString();
            if (File.Exists(packageToPush.PackageFileFullName)) {
                return packageToPush;
            }

            errorsAndInfos.Errors.Add(string.Format(Properties.Resources.FileNotFound, packageToPush.PackageFileFullName));
            return packageToPush;
        }
    }
}
