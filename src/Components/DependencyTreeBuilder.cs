using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Protocol;
using LocalPackageInfo = NuGet.Protocol.LocalPackageInfo;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Components;

public class DependencyTreeBuilder : IDependencyTreeBuilder {
    public IDependencyNode BuildDependencyTree(string packagesFolder) {
        var logger = new NullLogger();
        var repository = new FindLocalPackagesResourceV2(packagesFolder);
        IEnumerable<LocalPackageInfo> packages = repository.GetPackages(logger, CancellationToken.None);
        return BuildDependencyTree(repository, packages, []);
    }

    protected IDependencyNode BuildDependencyTree(FindLocalPackagesResource repository, IEnumerable<LocalPackageInfo> packages, IList<DependencyNode> ignoreNodes) {
        var logger = new NullLogger();
        var tree = new DependencyNode();
        foreach (LocalPackageInfo package in packages) {
            tree.Id = package.Identity.Id;
            tree.Version = package.Identity.Version.ToString();
            if (ignoreNodes.Any(n => EqualNodes(n, tree))) {
                continue;
            }

            IList<LocalPackageInfo> dependentPackages = [];
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (PackageDependencyGroup dependencySet in package.Nuspec.GetDependencyGroups()) {
                // ReSharper disable once LoopCanBePartlyConvertedToQuery
                foreach (LocalPackageInfo dependentPackage in dependencySet.Packages.SelectMany(d => repository.FindPackagesById(d.Id, logger, CancellationToken.None))) {
                    var dependencyNode = new DependencyNode { Id = dependentPackage.Identity.Id, Version = dependentPackage.Identity.Version.Version.ToString() };
                    if (ignoreNodes.Any(n => EqualNodes(n, dependencyNode))) {
                        continue;
                    }

                    ignoreNodes.Add(dependencyNode);
                    dependentPackages.Add(dependentPackage);
                }
            }

            if (!dependentPackages.Any()) { continue; }

            tree.ChildNodes.Add(BuildDependencyTree(repository, dependentPackages, ignoreNodes));
        }

        return tree;
    }

    protected static bool EqualNodes(DependencyNode node1, DependencyNode node2) {
        return node1.Id == node2.Id && node1.Version == node2.Version;
    }
}