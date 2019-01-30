using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide {
    public class NugetFeedLister : INugetFeedLister {
        public async Task<IList<IPackageSearchMetadata>> ListReleasedPackagesAsync(string feedUrl, string packageId) {
            var packageSource = new PackageSource(feedUrl);
            var providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());
            var repository = new SourceRepository(packageSource, providers);
            var packageMetaDataResource = await repository.GetResourceAsync<PackageMetadataResource>();
            var packageMetaData = (await packageMetaDataResource.GetMetadataAsync(packageId, false, false, new NullLogger(), CancellationToken.None)).ToList();
            return packageMetaData;
        }
    }
}
