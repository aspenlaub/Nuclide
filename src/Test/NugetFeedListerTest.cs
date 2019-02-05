using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Gitty.TestUtilities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IContainer = Autofac.IContainer;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Test {
    [TestClass]
    public class NugetFeedListerTest {
        private static IContainer vContainer;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext) {
            vContainer = new ContainerBuilder().UseGittyTestUtilities().UseNuclideProtchAndGitty().Build();
        }

        [TestMethod]
        public async Task CanFindPakledPackages() {
            const string feedUrl = "https://www.aspenlaub.net/nuget";
            const string packageId = "Aspenlaub.Net.GitHub.CSharp.Pegh";
            var sut = vContainer.Resolve<INugetFeedLister>();
            var packages = (await sut.ListReleasedPackagesAsync(feedUrl, packageId)).ToList();
            Assert.IsTrue(packages.Count > 5);
            foreach (var package in packages) {
                Assert.AreEqual(packageId, package.Identity.Id);
            }
        }
    }
}
