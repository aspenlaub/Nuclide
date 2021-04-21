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
        protected static TestTargetFolder ChabStandardTarget = new TestTargetFolder(nameof(NugetPackageInstallerTest), "ChabStandard");
        private static IContainer vContainer;
        protected static ITestTargetRunner TargetRunner;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context) {
            vContainer = new ContainerBuilder().UseGittyTestUtilities().UseNuclideProtchGittyAndPegh(new DummyCsArgumentPrompter()).Build();
            TargetRunner = vContainer.Resolve<ITestTargetRunner>();
        }

        [TestInitialize]
        public void Initialize() {
            ChabStandardTarget.Delete();
        }

        [TestCleanup]
        public void TestCleanup() {
            ChabStandardTarget.Delete();
        }

        [TestMethod]
        public void CanInstallNugetPackage() {
            var gitUtilities = new GitUtilities();
            var errorsAndInfos = new ErrorsAndInfos();
            var url = "https://github.com/aspenlaub/" + ChabStandardTarget.SolutionId + ".git";
            gitUtilities.Clone(url, "master", ChabStandardTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.IsFalse(ChabStandardTarget.Folder().SubFolder(@"src\OctoPack.3.6.0").Exists());
            var sut = vContainer.Resolve<INugetPackageInstaller>();

            sut.InstallNugetPackage(ChabStandardTarget.Folder().SubFolder("src"), "OctoPack", "3.6.0", false, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.IsTrue(errorsAndInfos.Infos.Any(i => i.Contains("Adding package") && i.Contains("to folder")));
            Assert.IsTrue(ChabStandardTarget.Folder().SubFolder(@"src\OctoPack.3.6.0").Exists());
        }
    }
}
