using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Gitty;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Protocol.Core.Types;
using IContainer = Autofac.IContainer;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Test;

[TestClass]
public class NugetFeedListerTest {
    private static IContainer _container;

    [ClassInitialize]
    public static void ClassInitialize(TestContext testContext) {
        _container = new ContainerBuilder().UseGittyTestUtilities().UseNuclideProtchGittyAndPegh("Nuclide").Build();
    }

    [TestMethod]
    public async Task CanFindNuclidePackages() {
        var developerSettingsSecret = new DeveloperSettingsSecret();
        var errorsAndInfos  = new ErrorsAndInfos();
        DeveloperSettings developerSettings = await _container.Resolve<ISecretRepository>().GetAsync(developerSettingsSecret, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.IsNotNull(developerSettings);

        const string packageId = "Nuclide";
        INugetFeedLister sut = _container.Resolve<INugetFeedLister>();

        var packages = (await sut.ListReleasedPackagesAsync(NugetFeed.AspenlaubLocalFeed, packageId, errorsAndInfos)).ToList();
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        Assert.IsNotEmpty(packages, $"No {packageId} package was found");
        Assert.IsGreaterThan(2, packages.Count, $"Only {packages.Count} {packageId} package/-s was/were found");
        foreach (IPackageSearchMetadata package in packages) {
            Assert.AreEqual(packageId, package.Identity.Id);
        }
    }
}