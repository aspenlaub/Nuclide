using System.IO;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Gitty;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Gitty.TestUtilities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Autofac;
using LibGit2Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IContainer = Autofac.IContainer;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Test {
    [TestClass]
    public class NugetPackageInstallerTest {
        protected static TestTargetFolder ChabTarget = new TestTargetFolder(nameof(NugetPackageInstallerTest), "Chab");
        private static IContainer vContainer;
        protected static TestTargetInstaller TargetInstaller;
        protected static TestTargetRunner TargetRunner;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context) {
            vContainer = new ContainerBuilder().UseGitty().UseGittyTestUtilities().UseNuclideProtchAndGitty().Build();
            TargetInstaller = vContainer.Resolve<TestTargetInstaller>();
            TargetRunner = vContainer.Resolve<TestTargetRunner>();
            TargetInstaller.DeleteCakeFolder(ChabTarget);
            TargetInstaller.CreateCakeFolder(ChabTarget, out var errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsPlusRelevantInfos());
        }

        [ClassCleanup]
        public static void ClassCleanup() {
            TargetInstaller.DeleteCakeFolder(ChabTarget);
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
            gitUtilities.Clone(url, ChabTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.IsFalse(ChabTarget.Folder().SubFolder(@"src\OctoPack.3.6.0").Exists());
            var sut = vContainer.Resolve<INugetPackageInstaller>();

            sut.InstallNugetPackage(ChabTarget.Folder().SubFolder("src"), "OctoPack", "3.6.0", false, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.IsTrue(errorsAndInfos.Infos.Any(i => i.Contains("Adding package") && i.Contains("to folder")));
            Assert.IsTrue(ChabTarget.Folder().SubFolder(@"src\OctoPack.3.6.0").Exists());

            errorsAndInfos = new ErrorsAndInfos();
            const string oldCakeVersion = "0.24.0";
            sut.InstallNugetPackage(ChabTarget.Folder().SubFolder("tools"), "Cake", oldCakeVersion, true, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any());
            Assert.IsTrue(ChabTarget.Folder().SubFolder(@"tools\Cake").Exists());
            Assert.IsTrue(File.Exists(ChabTarget.Folder().SubFolder(@"tools\Cake").FullName + @"\Cake.exe"));

            errorsAndInfos = new ErrorsAndInfos();
            sut.InstallNugetPackage(ChabTarget.Folder().SubFolder("tools"), "Cake", "", true, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any());
            Assert.IsTrue(errorsAndInfos.Infos.Any(i => i.Contains("Successfully uninstalled") && i.Contains(oldCakeVersion)));
            Assert.IsTrue(errorsAndInfos.Infos.Any(i => i.Contains("Successfully installed")));
        }
    }
}
