using System.Collections.Generic;
using System.Threading.Tasks;
using NuGet.Protocol.Core.Types;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces {
    public interface INugetFeedLister {

        /// <summary>
        /// See what packages are provided by the specified nuget feed
        /// </summary>
        /// <param name="feedUrl">e.g. https://packages.nuget.org/api/v2</param>
        /// <param name="packageId">e.g. Aspenlaub.Net.GitHub.CSharp.Shatilaya</param>
        /// <returns></returns>
        Task<IList<IPackageSearchMetadata>> ListReleasedPackagesAsync(string feedUrl, string packageId);
    }
}
