using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: DoNotParallelize]
namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Test;

[TestClass]
public class BranchesWithPackagesTest {
    [TestMethod]
    public async Task CanGetBranchesWithPackages() {
        var errorsAndInfos = new ErrorsAndInfos();
        var secret = new SecretBranchesWithPackages();
        IContainer container = new ContainerBuilder().UsePegh("TheLittleThings").Build();
        BranchesWithPackages branchesWithPackages = await container.Resolve<ISecretRepository>().GetAsync(secret, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsPlusRelevantInfos());
        foreach (string expectedBranch in new[] { "master", "pkg-branch-test" }) {
            Assert.Contains(s => s.Branch == expectedBranch, branchesWithPackages,
                $"Branch {expectedBranch} not found among branches with packages");
        }
    }
}