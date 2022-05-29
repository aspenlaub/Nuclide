using System.IO;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Gitty;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;
using LibGit2Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IContainer = Autofac.IContainer;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Test;

[TestClass]
public class NugetRestorerTest {
    private static IContainer Container;
    private static IFolder AutomationTestProjectsFolder;

    [ClassInitialize]
    public static void ClassInitialize(TestContext testContext) {
        Container = new ContainerBuilder().UseGittyTestUtilities().UseNuclideProtchGittyAndPegh("Nuclide", new DummyCsArgumentPrompter()).Build();
        AutomationTestProjectsFolder = new Folder(Path.GetTempPath()).SubFolder("AspenlaubTemp").SubFolder(nameof(NugetRestorerTest));
    }

    [TestInitialize]
    public void Initialize() {
        CleanUp();
    }

    [TestCleanup]
    public void CleanUp() {
        if (!AutomationTestProjectsFolder.Exists()) { return; }

        var deleter = new FolderDeleter();
        deleter.DeleteFolder(AutomationTestProjectsFolder);
    }

    [TestMethod]
    public void CanRestoreNugetPackagesForWpfSolution() {
        var errorsAndInfos = new ErrorsAndInfos();
        const string url = "https://github.com/aspenlaub/AutomationTestProjects.git";
        AutomationTestProjectsFolder.CreateIfNecessary();
        Container.Resolve<IGitUtilities>().Clone(url, "master", AutomationTestProjectsFolder, new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        var sut = Container.Resolve<INugetPackageRestorer>();
        errorsAndInfos = new ErrorsAndInfos();
        sut.RestoreNugetPackages(AutomationTestProjectsFolder.FullName + @"\AsyncWpf\AsyncWpf.sln", errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        Assert.IsTrue(errorsAndInfos.Infos.Any(i => i.Contains($"Restored {AutomationTestProjectsFolder.FullName}\\AsyncWpf\\AsyncWpf.csproj")));
    }
}