using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Gitty;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Seoa.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Skladasu.Entities;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Test;

[TestClass]
public class SecretNugetFeedsTest {
    private static IContainer _container;

    [ClassInitialize]
    public static void ClassInitialize(TestContext testContext) {
        _container = new ContainerBuilder().UseGittyTestUtilities().UseNuclideProtchGittyAndPegh("Nuclide").Build();
    }

    [TestMethod]
    public async Task CanGetSecretNugetFeeds() {
        var nugetFeedsSecret = new SecretNugetFeeds();
        var errorsAndInfos = new ErrorsAndInfos();
        NugetFeeds nugetFeeds = await _container.Resolve<ISecretRepository>().GetAsync(nugetFeedsSecret, errorsAndInfos);
        Assert.That.ThereWereNoErrors(errorsAndInfos);
        Assert.IsNotNull(nugetFeeds);
        Assert.HasCount(1, nugetFeeds.Where(f => f.IsMainFeed));
    }
}