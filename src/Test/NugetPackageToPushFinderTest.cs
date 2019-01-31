using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Gitty;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Gitty.TestUtilities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Protch;
using Aspenlaub.Net.GitHub.CSharp.Protch.Interfaces;
using Autofac;
using LibGit2Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IContainer = Autofac.IContainer;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Test {
    [TestClass]
    public class NugetPackageToPushFinderTest {
        protected static TestTargetFolder PakledTarget = new TestTargetFolder(nameof(NugetPackageToPushFinderTest), "Pakled");
        protected static TestTargetFolder ChabStandardTarget = new TestTargetFolder(nameof(NugetPackageToPushFinderTest), "ChabStandard");
        private static IContainer vContainer;
        protected static TestTargetInstaller TargetInstaller;
        protected static TestTargetRunner TargetRunner;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context) {
            vContainer = new ContainerBuilder().UseGitty().UseGittyTestUtilities().UseProtch().UseNuclide().Build();
            TargetInstaller = vContainer.Resolve<TestTargetInstaller>();
            TargetRunner = vContainer.Resolve<TestTargetRunner>();
            TargetInstaller.DeleteCakeFolder(PakledTarget);
            TargetInstaller.CreateCakeFolder(PakledTarget, out var errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsPlusRelevantInfos());
            TargetInstaller.DeleteCakeFolder(ChabStandardTarget);
            TargetInstaller.CreateCakeFolder(ChabStandardTarget, out errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsPlusRelevantInfos());
        }

        [ClassCleanup]
        public static void ClassCleanup() {
            TargetInstaller.DeleteCakeFolder(PakledTarget);
            TargetInstaller.DeleteCakeFolder(ChabStandardTarget);
        }

        [TestInitialize]
        public void Initialize() {
            PakledTarget.Delete();
            ChabStandardTarget.Delete();
        }

        [TestCleanup]
        public void TestCleanup() {
            PakledTarget.Delete();
            ChabStandardTarget.Delete();
        }

        [TestMethod]
        public async Task CanFindNugetPackagesToPushForPakled() {
            var errorsAndInfos = new ErrorsAndInfos();
            var developerSettings = await GetDeveloperSettingsAsync(errorsAndInfos);

            CloneTarget(PakledTarget, errorsAndInfos);

            RunCakeScript(PakledTarget, true, errorsAndInfos);

            errorsAndInfos = new ErrorsAndInfos();
            var sut = vContainer.Resolve<INugetPackageToPushFinder>();
            var packageToPush = await sut.FindPackageToPushAsync(PakledTarget.Folder().ParentFolder().SubFolder(PakledTarget.SolutionId + @"Bin\Release"), PakledTarget.Folder(), PakledTarget.Folder().SubFolder("src").FullName + @"\" + PakledTarget.SolutionId + ".sln", errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.AreEqual(developerSettings.NugetFeedUrl, packageToPush.FeedUrl);
            Assert.IsTrue(packageToPush.ApiKey.Length > 256);
        }

        private async Task<DeveloperSettings> GetDeveloperSettingsAsync(IErrorsAndInfos errorsAndInfos) {
            var developerSettingsSecret = new DeveloperSettingsSecret();
            var developerSettings = await vContainer.Resolve<ISecretRepository>().GetAsync(developerSettingsSecret, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.IsNotNull(developerSettings);
            return developerSettings;
        }

        [TestMethod]
        public async Task PackageForTheSameCommitIsNotPushed() {
            var errorsAndInfos = new ErrorsAndInfos();
            var developerSettings = await GetDeveloperSettingsAsync(errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

            CloneTarget(PakledTarget, errorsAndInfos);

            var packages = await vContainer.Resolve<INugetFeedLister>().ListReleasedPackagesAsync(developerSettings.NugetFeedUrl, @"Aspenlaub.Net.GitHub.CSharp." + PakledTarget.SolutionId);
            if (!packages.Any()) { return; }

            var latestPackageVersion = packages.Max(p => p.Identity.Version.Version);
            var latestPackage = packages.First(p => p.Identity.Version.Version == latestPackageVersion);

            var headTipIdSha = vContainer.Resolve<IGitUtilities>().HeadTipIdSha(PakledTarget.Folder());
            if (!latestPackage.Tags.Contains(headTipIdSha)) {
                return; // $"No package has been pushed for {headTipIdSha} and {PakledTarget.SolutionId}, please run build.cake for this solution"
            }

            RunCakeScript(PakledTarget, false, errorsAndInfos);

            packages = await vContainer.Resolve<INugetFeedLister>().ListReleasedPackagesAsync(developerSettings.NugetFeedUrl, @"Aspenlaub.Net.GitHub.CSharp." + PakledTarget.SolutionId);
            Assert.AreEqual(latestPackageVersion, packages.Max(p => p.Identity.Version.Version));
        }

        private static void CloneTarget(ITestTargetFolder testTargetFolder, IErrorsAndInfos errorsAndInfos) {
            var gitUtilities = new GitUtilities();
            var url = "https://github.com/aspenlaub/" + testTargetFolder.SolutionId + ".git";
            gitUtilities.Clone(url, testTargetFolder.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        }

        private void RunCakeScript(ITestTargetFolder testTargetFolder, bool disableNugetPush, IErrorsAndInfos errorsAndInfos) {
            var projectLogic = vContainer.Resolve<IProjectLogic>();
            var projectFactory = vContainer.Resolve<IProjectFactory>();
            var solutionFileFullName = testTargetFolder.Folder().SubFolder("src").FullName + '\\' + testTargetFolder.SolutionId + ".sln";
            var projectErrorsAndInfos = new ErrorsAndInfos();
            Assert.IsTrue(projectLogic.DoAllNetStandardOrCoreConfigurationsHaveNuspecs(projectFactory.Load(solutionFileFullName, solutionFileFullName.Replace(".sln", ".csproj"), projectErrorsAndInfos)));

            var target = disableNugetPush ? "IgnoreOutdatedBuildCakePendingChangesAndDoNotPush" : "IgnoreOutdatedBuildCakePendingChanges";
            TargetRunner.RunBuildCakeScript(BuildCake.Standard, testTargetFolder, target, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        }

        [TestMethod]
        public async Task CanFindNugetPackagesToPushForChabStandard() {
            var errorsAndInfos = new ErrorsAndInfos();
            var developerSettings = await GetDeveloperSettingsAsync(errorsAndInfos);

            CloneTarget(ChabStandardTarget, errorsAndInfos);

            RunCakeScript(ChabStandardTarget, true, errorsAndInfos);

            Assert.IsFalse(errorsAndInfos.Infos.Any(i => i.Contains("No test")));

            errorsAndInfos = new ErrorsAndInfos();
            var sut = vContainer.Resolve<INugetPackageToPushFinder>();
            var packageToPush = await sut.FindPackageToPushAsync(ChabStandardTarget.Folder().ParentFolder().SubFolder(ChabStandardTarget.SolutionId + @"Bin\Release"), ChabStandardTarget.Folder(), ChabStandardTarget.Folder().SubFolder("src").FullName + @"\" + ChabStandardTarget.SolutionId + ".sln", errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.AreEqual(developerSettings.NugetFeedUrl, packageToPush.FeedUrl);
            Assert.IsTrue(packageToPush.ApiKey.Length > 256);
        }
    }
}
