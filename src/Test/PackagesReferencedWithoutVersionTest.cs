using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Test;

[TestClass]
public class PackagesReferencedWithoutVersionTest {
    [TestMethod]
    public async Task CanGetPackagesReferencedWithoutVersion() {
        var errorsAndInfos = new ErrorsAndInfos();
        var secret = new SecretPackagesReferencedWithoutVersion();
        IContainer container = new ContainerBuilder().UsePegh("Nuclide", new DummyCsArgumentPrompter()).Build();
        PackagesReferencedWithoutVersion packagesReferencedWithoutVersion = await container.Resolve<ISecretRepository>().GetAsync(secret, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsPlusRelevantInfos());
        Assert.IsNotNull(packagesReferencedWithoutVersion);
        Assert.IsGreaterThanOrEqualTo(3, packagesReferencedWithoutVersion.Count);
    }
}