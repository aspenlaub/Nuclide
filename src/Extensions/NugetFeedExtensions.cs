using Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Extensions {
    public static class NugetFeedExtensions {
        public static string UrlOrResolvedFolder(this NugetFeed nugetFeed, IFolderResolver folderResolver, IErrorsAndInfos errorsAndInfos) {
            var source = nugetFeed.Url;
            if (!nugetFeed.IsAFolderToResolve()) { return source; }

            var folder = folderResolver.Resolve(source, errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return source; }

            source = folder.FullName;

            return source;
        }
    }
}
