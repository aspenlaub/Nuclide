using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Gitty;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Test {
    [TestClass]
    public class SecretNugetFeedsTest {
        private static IContainer vContainer;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext) {
            vContainer = new ContainerBuilder().UseGittyTestUtilities().UseNuclideProtchGittyAndPegh(new DummyCsArgumentPrompter()).Build();
        }

        [TestMethod]
        public async Task CanGetSecretNugetFeeds() {
            var nugetFeedsSecret = new SecretNugetFeeds();
            var errorsAndInfos = new ErrorsAndInfos();
            var nugetFeeds = await vContainer.Resolve<ISecretRepository>().GetAsync(nugetFeedsSecret, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            Assert.IsNotNull(nugetFeeds);
            Assert.IsTrue(nugetFeeds.Count(f => f.IsMainFeed) == 1);
        }
    }
}
