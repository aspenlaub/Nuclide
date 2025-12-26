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
using IContainer = Autofac.IContainer;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Test;

[TestClass]
public class NugetFeedListerTest {
    private static IContainer _container;

    [ClassInitialize]
    public static void ClassInitialize(TestContext testContext) {
        _container = new ContainerBuilder().UseGittyTestUtilities().UseNuclideProtchGittyAndPegh("Nuclide", new DummyCsArgumentPrompter()).Build();
    }

    [TestMethod]
    public async Task CanFindNuclidePackages() {
        var developerSettingsSecret = new DeveloperSettingsSecret();
        var errorsAndInfos  = new ErrorsAndInfos();
        var developerSettings = await _container.Resolve<ISecretRepository>().GetAsync(developerSettingsSecret, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.IsNotNull(developerSettings);

        const string packageId = "Nuclide";
        var sut = _container.Resolve<INugetFeedLister>();

        var packages = (await sut.ListReleasedPackagesAsync(NugetFeed.AspenlaubLocalFeed, packageId, errorsAndInfos)).ToList();
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        Assert.IsTrue(packages.Count > 1, $"No {packageId} package was found");
        Assert.IsTrue(packages.Count > 2, $"Only {packages.Count} {packageId} package/-s was/were found");
        foreach (var package in packages) {
            Assert.AreEqual(packageId, package.Identity.Id);
        }
    }
}