﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide {
    public class NugetFeedLister : INugetFeedLister {
        private readonly ISecretRepository vSecretRepository;

        public NugetFeedLister(ISecretRepository secretRepository) {
            vSecretRepository = secretRepository;
        }

        public async Task<IList<IPackageSearchMetadata>> ListReleasedPackagesAsync(string nugetFeedId, string packageId, IErrorsAndInfos errorsAndInfos) {
            var nugetFeedsSecret = new SecretNugetFeeds();
            var nugetFeeds = await vSecretRepository.GetAsync(nugetFeedsSecret, errorsAndInfos);
            var nugetFeed = nugetFeeds.FirstOrDefault(f => f.Id == nugetFeedId);
            if (nugetFeed == null) {
                errorsAndInfos.Errors.Add(string.Format(Properties.Resources.UnknownNugetFeed, nugetFeedId, nugetFeedsSecret.Guid + ".xml"));
                return null;
            }

            var packageSource = new PackageSource(nugetFeed.Url);
            var providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());
            var repository = new SourceRepository(packageSource, providers);
            var packageMetaDataResource = await repository.GetResourceAsync<PackageMetadataResource>();
            var packageMetaData = (await packageMetaDataResource.GetMetadataAsync(packageId, false, false, new NullLogger(), CancellationToken.None)).ToList();
            return packageMetaData;
        }
    }
}
