using System.IO;
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

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Test;

[TestClass]
public class DependencyTreeBuilderTest {
    protected static TestTargetFolder ShatilayaTarget = new(nameof(DependencyTreeBuilderTest), "Shatilaya");
    private static IContainer _container;
    protected static ITestTargetRunner TargetRunner;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context) {
        _container = new ContainerBuilder().UseGittyTestUtilities().UseNuclideProtchGittyAndPegh("Nuclide").Build();
        TargetRunner = _container.Resolve<ITestTargetRunner>();
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
        gitUtilities.Reset(ShatilayaTarget.Folder(), "7f08dcdef49705466557d9d318596b90feecaf6d", errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        INugetPackageRestorer restorer = _container.Resolve<INugetPackageRestorer>();
        string sourceFolder = ShatilayaTarget.Folder().SubFolder("src").FullName;
        Directory.CreateDirectory(sourceFolder + @"\packages\");
        restorer.RestoreNugetPackages(sourceFolder + @"\" + ShatilayaTarget.SolutionId + ".slnx", errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        Assert.IsFalse(errorsAndInfos.Infos.Any(i => i.Contains("package(s) to packages.config")));
    }
}