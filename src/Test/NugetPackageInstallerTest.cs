using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Gitty;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Components;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Gitty.TestUtilities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Autofac;
using LibGit2Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IContainer = Autofac.IContainer;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Test {
    [TestClass]
    public class NugetPackageInstallerTest {
        protected static TestTargetFolder ChabTarget = new(nameof(NugetPackageInstallerTest), "Chab");
        private static IContainer Container;
        protected static ITestTargetRunner TargetRunner;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context) {
            Container = new ContainerBuilder().UseGittyTestUtilities().UseNuclideProtchGittyAndPegh(new DummyCsArgumentPrompter()).Build();
            TargetRunner = Container.Resolve<ITestTargetRunner>();
        }

        [TestInitialize]
        public void Initialize() {
            ChabTarget.Delete();
        }

        [TestCleanup]
        public void TestCleanup() {
            ChabTarget.Delete();
        }

        [TestMethod]
        public void CanInstallNugetPackage() {
            var gitUtilities = new GitUtilities();
            var errorsAndInfos = new ErrorsAndInfos();
            var url = "https://github.com/aspenlaub/" + ChabTarget.SolutionId + ".git";
            gitUtilities.Clone(url, "master", ChabTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.IsFalse(ChabTarget.Folder().SubFolder(@"src\OctoPack.3.6.0").Exists());
            var sut = Container.Resolve<INugetPackageInstaller>();

            sut.InstallNugetPackage(ChabTarget.Folder().SubFolder("src"), "OctoPack", "3.6.0", false, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.IsTrue(errorsAndInfos.Infos.Any(i => i.Contains("Adding package") && i.Contains("to folder")));
            Assert.IsTrue(ChabTarget.Folder().SubFolder(@"src\OctoPack.3.6.0").Exists());
        }
    }
}
