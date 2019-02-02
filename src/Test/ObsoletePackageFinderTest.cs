﻿using System.IO;
using System.Linq;
using System.Reflection;
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
    public class ObsoletePackageFinderTest {
        protected static TestTargetFolder ChabTarget = new TestTargetFolder(nameof(ObsoletePackageFinderTest), "Chab");
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
        public void CanFindObsoletePackages() {
            var gitUtilities = new GitUtilities();
            var errorsAndInfos = new ErrorsAndInfos();
            var url = "https://github.com/aspenlaub/" + ChabTarget.SolutionId + ".git";
            gitUtilities.Clone(url, ChabTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

            vContainer.Resolve<TestTargetRunner>().IgnoreOutdatedBuildCakePendingChangesAndDoNotPush(Assembly.GetExecutingAssembly(), ChabTarget, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

            errorsAndInfos = new ErrorsAndInfos();
            var sut = vContainer.Resolve<IObsoletePackageFinder>();
            var solutionFolder = ChabTarget.Folder().SubFolder("src");
            sut.FindObsoletePackages(solutionFolder.FullName, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any());
            Assert.IsFalse(errorsAndInfos.Infos.Any());
            var obsoleteFolder = solutionFolder.SubFolder(@"packages\ObsoPack");
            Directory.CreateDirectory(obsoleteFolder.FullName);
            foreach(var extension in new[] { "cs", "dll", "json", "_", "cake", "php", "txt", "docx", "exe", "js", "css", "bat", "cmd", "xlsx", "csv", "sln" }) {
                File.WriteAllText(obsoleteFolder.FullName + "\\somefile." + extension, "Delete me");
            }
            File.WriteAllText(obsoleteFolder.FullName + "\\somefile.csproj", "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Project ToolsVersion=\"14.0\" DefaultTargets=\"Build\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"></Project>");

            sut.FindObsoletePackages(solutionFolder.FullName, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.IsTrue(errorsAndInfos.Infos.Any(i => i.Contains(obsoleteFolder.FullName) && i.Contains("has been deleted")));
        }
    }
}
