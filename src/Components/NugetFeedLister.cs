using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Skladasu.Interfaces;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Components;

public class NugetFeedLister(ISecretRepository secretRepository, IFolderResolver folderResolver) : INugetFeedLister {
    public async Task<IList<IPackageSearchMetadata>> ListReleasedPackagesAsync(string nugetFeedId, string packageId, IErrorsAndInfos errorsAndInfos) {
        var nugetFeedsSecret = new SecretNugetFeeds();
        NugetFeeds nugetFeeds = await secretRepository.GetAsync(nugetFeedsSecret, errorsAndInfos);
        NugetFeed nugetFeed = nugetFeeds.FirstOrDefault(f => f.Id == nugetFeedId);
        if (nugetFeed == null) {
            errorsAndInfos.Errors.Add(string.Format(Properties.Resources.UnknownNugetFeed, nugetFeedId, nugetFeedsSecret.Guid + ".xml"));
            return [];
        }

        try {
            string source = await nugetFeed.UrlOrResolvedFolderAsync(folderResolver, errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) {  return []; }

            var packageSource = new PackageSource(source);
            var providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());
            var repository = new SourceRepository(packageSource, providers);
            PackageMetadataResource packageMetaDataResource = await repository.GetResourceAsync<PackageMetadataResource>();
            var packageMetaData = (await packageMetaDataResource.GetMetadataAsync(packageId, false, false, new SourceCacheContext(), new NullLogger(), CancellationToken.None)).ToList();
            return packageMetaData;
        } catch {
            errorsAndInfos.Errors.Add(string.Format(Properties.Resources.CouldNotAccessNugetFeed, nugetFeedId));
            return [];
        }
    }
}