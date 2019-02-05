using System.IO;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Gitty.TestUtilities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;
using LibGit2Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IContainer = Autofac.IContainer;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Test {
    [TestClass]
    public class NugetRestorerTest {
        private static IContainer vContainer;
        private static IFolder vAutomationTestProjectsFolder;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext) {
            vContainer = new ContainerBuilder().UseGittyTestUtilities().UseNuclideProtchAndGitty().Build();
            vAutomationTestProjectsFolder = new Folder(Path.GetTempPath()).SubFolder(nameof(NugetRestorerTest));
        }

        [TestInitialize]
        public void Initialize() {
            CleanUp();
        }

        [TestCleanup]
        public void CleanUp() {
            if (!vAutomationTestProjectsFolder.Exists()) { return; }

            var deleter = new FolderDeleter();
            deleter.DeleteFolder(vAutomationTestProjectsFolder);
        }

        [TestMethod]
        public void CanRestoreNugetPackagesForWpfSolution() {
            var errorsAndInfos = new ErrorsAndInfos();
            const string url = "https://github.com/aspenlaub/AutomationTestProjects.git";
            vAutomationTestProjectsFolder.CreateIfNecessary();
            vContainer.Resolve<IGitUtilities>().Clone(url, vAutomationTestProjectsFolder, new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            var sut = vContainer.Resolve<INugetPackageRestorer>();
            errorsAndInfos = new ErrorsAndInfos();
            sut.RestoreNugetPackages(vAutomationTestProjectsFolder.FullName + @"\AsyncWpf\AsyncWpf.sln", errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.IsTrue(errorsAndInfos.Infos.Any(i => i.Contains("Restore completed")));
        }
    }
}
