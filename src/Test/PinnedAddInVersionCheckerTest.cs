using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Gitty;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Gitty.TestUtilities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Autofac;
using LibGit2Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Test;

[TestClass]
public class PinnedAddInVersionCheckerTest {
    protected static TestTargetFolder PeghTarget = new(nameof(PinnedAddInVersionCheckerTest), "Pegh");
    private static IContainer _container;
    protected static ITestTargetRunner TargetRunner;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context) {
        _container = new ContainerBuilder().UseGittyTestUtilities().UseNuclideProtchGittyAndPegh("Nuclide").Build();
        TargetRunner = _container.Resolve<ITestTargetRunner>();
    }

    [TestInitialize]
    public void Initialize() {
        PeghTarget.Delete();
    }

    [TestCleanup]
    public void TestCleanup() {
        PeghTarget.Delete();
    }

    [TestMethod]
    public async Task CanCheckPinnedAddInVersions() {
        IGitUtilities gitUtilities = _container.Resolve<IGitUtilities>();
        var errorsAndInfos = new ErrorsAndInfos();
        string url = "https://github.com/aspenlaub/" + PeghTarget.SolutionId + ".git";
        gitUtilities.Clone(url, "master", PeghTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

        IDictionary<string, string> mainProjectDependencyIdsAndVersions = await _container.Resolve<IPackageReferencesScanner>().DependencyIdsAndVersionsAsync(PeghTarget.Folder().SubFolder("src").FullName, true, true, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        IDictionary<string, string> dependencyIdsAndVersions = await _container.Resolve<IPackageReferencesScanner>().DependencyIdsAndVersionsAsync(PeghTarget.Folder().FullName, true, false, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());

        Assert.IsGreaterThan(0, mainProjectDependencyIdsAndVersions.Count, "Main project should have package references");
        Assert.IsGreaterThan(mainProjectDependencyIdsAndVersions.Count, dependencyIdsAndVersions.Count, "Solution should not only contain packages that are referenced by the main project");

        IPinnedAddInVersionChecker sut = _container.Resolve<IPinnedAddInVersionChecker>();
        await sut.CheckPinnedAddInVersionsAsync(PeghTarget.Folder(), errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

        await sut.CheckPinnedAddInVersionsAsync([], PeghTarget.Folder(), errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

        string package = dependencyIdsAndVersions.First().Key;
        await sut.CheckPinnedAddInVersionsAsync([$"#addin nuget:?package={package}"], PeghTarget.Folder(), errorsAndInfos);
        Assert.IsTrue(errorsAndInfos.Errors.Any());
        errorsAndInfos.Errors.Clear();

        string version = dependencyIdsAndVersions.First().Value;
        await sut.CheckPinnedAddInVersionsAsync([$"#addin nuget:?package={package}&version={version}"], PeghTarget.Folder(), errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

        await sut.CheckPinnedAddInVersionsAsync([$"#addin nuget:?package={package}&version=3.{version}"], PeghTarget.Folder(), errorsAndInfos);
        Assert.IsTrue(errorsAndInfos.Errors.Any());
    }
}