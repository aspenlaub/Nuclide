using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Gitty.TestUtilities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IContainer = Autofac.IContainer;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Test {
    [TestClass]
    public class NugetFeedListerTest {
        private static IContainer vContainer;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext) {
            vContainer = new ContainerBuilder().UseGittyTestUtilities().UseNuclideProtchGittyAndPegh(new DummyCsArgumentPrompter()).Build();
        }

        [TestMethod]
        public async Task CanFindPakledPackages() {
            var developerSettingsSecret = new DeveloperSettingsSecret();
            var errorsAndInfos  = new ErrorsAndInfos();
            var developerSettings = await vContainer.Resolve<ISecretRepository>().GetAsync(developerSettingsSecret, errorsAndInfos);
            Assert.IsNotNull(developerSettings);

            const string packageId = "Aspenlaub.Net.GitHub.CSharp.Pegh";
            var sut = vContainer.Resolve<INugetFeedLister>();
            var packages = (await sut.ListReleasedPackagesAsync(developerSettings.NugetFeedUrl, packageId)).ToList();
            Assert.IsTrue(packages.Count > 5);
            foreach (var package in packages) {
                Assert.AreEqual(packageId, package.Identity.Id);
            }
        }
    }
}
