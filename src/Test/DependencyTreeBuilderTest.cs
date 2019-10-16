using System.IO;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Gitty;
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
    public class DependencyTreeBuilderTest {
        protected static TestTargetFolder ShatilayaTarget = new TestTargetFolder(nameof(DependencyTreeBuilderTest), "Shatilaya");
        private static IContainer vContainer;
        protected static TestTargetInstaller TargetInstaller;
        protected static TestTargetRunner TargetRunner;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context) {
            vContainer = new ContainerBuilder().UseGittyTestUtilities().UseNuclideProtchGittyAndPegh(new DummyCsArgumentPrompter()).Build();
            TargetInstaller = vContainer.Resolve<TestTargetInstaller>();
            TargetRunner = vContainer.Resolve<TestTargetRunner>();
            TargetInstaller.DeleteCakeFolder(ShatilayaTarget);
            TargetInstaller.CreateCakeFolder(ShatilayaTarget, out var errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsPlusRelevantInfos());
        }

        [ClassCleanup]
        public static void ClassCleanup() {
            TargetInstaller.DeleteCakeFolder(ShatilayaTarget);
        }

        [TestInitialize]
        public void Initialize() {
            ShatilayaTarget.Delete();
        }

        [TestCleanup]
        public void TestCleanup() {
            ShatilayaTarget.Delete();
        }

        [TestMethod]
        public void ThereArentAnyUnwantedDependencies() {
            var gitUtilities = new GitUtilities();
            var errorsAndInfos = new ErrorsAndInfos();
            const string url = "https://github.com/aspenlaub/Shatilaya.git";
            gitUtilities.Clone(url, "master", ShatilayaTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            gitUtilities.Reset(ShatilayaTarget.Folder(), "c895d6d9efc93b71a061d580cec2d88f0d78ea9b", errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            var restorer = vContainer.Resolve<INugetPackageRestorer>();
            var sourceFolder = ShatilayaTarget.Folder().SubFolder("src").FullName;
            Directory.CreateDirectory(sourceFolder + @"\packages\");
            restorer.RestoreNugetPackages(sourceFolder + @"\" + ShatilayaTarget.SolutionId + ".sln", errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.IsTrue(errorsAndInfos.Infos.Any(i => i.Contains("package(s) to packages.config")));
            var builder = vContainer.Resolve<IDependencyTreeBuilder>();
            var dependencyTree = builder.BuildDependencyTree(sourceFolder + @"\packages\");
            var nodes = dependencyTree.FindNodes(ContainsValueTuple);
            Assert.IsTrue(nodes.Any());
            Assert.IsTrue(nodes.All(IsCorrectThreadingTasksVersion));
        }

        protected bool ContainsValueTuple(IDependencyNode node) {
            return !string.IsNullOrEmpty(node.Id) && node.Id.Contains("System.ValueTuple");
        }

        protected bool IsCorrectThreadingTasksVersion(IDependencyNode node) {
            return node?.Version == "4.5.0";
        }
    }
}
