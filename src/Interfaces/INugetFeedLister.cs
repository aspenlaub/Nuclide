using System.Collections.Generic;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Skladasu.Interfaces;
using NuGet.Protocol.Core.Types;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;

public interface INugetFeedLister {
    Task<IList<IPackageSearchMetadata>> ListReleasedPackagesAsync(string nugetFeedId, string packageId, IErrorsAndInfos errorsAndInfos);
}