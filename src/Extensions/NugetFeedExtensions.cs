using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Extensions;

public static class NugetFeedExtensions {
    public static async Task<string> UrlOrResolvedFolderAsync(this NugetFeed nugetFeed, IFolderResolver folderResolver, IErrorsAndInfos errorsAndInfos) {
        string source = nugetFeed.Url;
        if (!nugetFeed.IsAFolderToResolve()) { return source; }

        IFolder folder = await folderResolver.ResolveAsync(source, errorsAndInfos);
        if (errorsAndInfos.AnyErrors()) { return source; }

        source = folder.FullName;

        return source;
    }
}