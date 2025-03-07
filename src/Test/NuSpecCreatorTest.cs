﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Aspenlaub.Net.GitHub.CSharp.Gitty;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Components;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Gitty.TestUtilities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Components;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Protch;
using Autofac;
using LibGit2Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IContainer = Autofac.IContainer;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Test;

[TestClass]
public class NuSpecCreatorTest {
    private static readonly TestTargetFolder ChabTarget = new(nameof(NuSpecCreator), "Chab");
    private static readonly TestTargetFolder DvinTarget = new(nameof(NuSpecCreator), "Dvin");
    private static readonly TestTargetFolder VishizhukelTarget = new(nameof(NuSpecCreator), "Vishizhukel");
    private static readonly TestTargetFolder VishnetIntegrationTestToolsTarget = new(nameof(NuSpecCreator), "VishnetIntegrationTestTools");
    private static readonly TestTargetFolder LibGit2SharpTarget = new(nameof(NuSpecCreator), "LibGit2Sharp");
    private static readonly TestTargetFolder PakledTarget = new(nameof(NuSpecCreator), "Pakled");
    private static IContainer Container;
    private static IGitUtilities GitUtilities;

    protected XDocument Document;
    protected XmlNamespaceManager NamespaceManager;

    public NuSpecCreatorTest() {
        NamespaceManager = new XmlNamespaceManager(new NameTable());
        NamespaceManager.AddNamespace("cp", XmlNamespaces.CsProjNamespaceUri);
        NamespaceManager.AddNamespace("nu", XmlNamespaces.NuSpecNamespaceUri);
    }

    [ClassInitialize]
    public static void ClassInitialize(TestContext context) {
        Container = new ContainerBuilder().UseGittyTestUtilities().UseProtch().UseNuclideProtchGittyAndPegh("Nuclide", new DummyCsArgumentPrompter()).Build();
        GitUtilities = Container.Resolve<IGitUtilities>();
    }

    [TestInitialize]
    public void Initialize() {
        ChabTarget.Delete();
        DvinTarget.Delete();
        VishizhukelTarget.Delete();
        LibGit2SharpTarget.Delete();
        PakledTarget.Delete();
        VishnetIntegrationTestToolsTarget.Delete();
    }

    [TestCleanup]
    public void TestCleanup() {
        ChabTarget.Delete();
        DvinTarget.Delete();
        VishizhukelTarget.Delete();
        LibGit2SharpTarget.Delete();
        PakledTarget.Delete();
        VishnetIntegrationTestToolsTarget.Delete();
    }

    [TestMethod]
    public async Task CanCreateNuSpecForChab() {
        var gitUtilities = new GitUtilities();
        var errorsAndInfos = new ErrorsAndInfos();
        const string url = "https://github.com/aspenlaub/Chab.git";
        gitUtilities.Clone(url, "master", ChabTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

        Container.Resolve<IEmbeddedCakeScriptCopier>().CopyCakeScriptEmbeddedInAssembly(Assembly.GetExecutingAssembly(), BuildCake.Standard, ChabTarget, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

        Container.Resolve<ITestTargetRunner>().RunBuildCakeScript(BuildCake.Standard, ChabTarget, "IgnoreOutdatedBuildCakePendingChangesAndDoCreateOrPushPackage", errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        Assert.AreEqual(2, errorsAndInfos.Infos.Count(i => i.Contains("Results File:")));

        var sut = Container.Resolve<INuSpecCreator>();
        var solutionFileFullName = ChabTarget.Folder().SubFolder("src").FullName + @"\" + ChabTarget.SolutionId + ".sln";
        var projectFileFullName = ChabTarget.Folder().SubFolder("src").FullName + @"\" + ChabTarget.SolutionId + ".csproj";
        Assert.IsTrue(File.Exists(projectFileFullName));
        Document = XDocument.Load(projectFileFullName);
        var targetFrameworkElement = Document.XPathSelectElements("./Project/PropertyGroup/TargetFramework", NamespaceManager).FirstOrDefault();
        Assert.IsNotNull(targetFrameworkElement);
        var rootNamespaceElement = Document.XPathSelectElements("./Project/PropertyGroup/RootNamespace", NamespaceManager).FirstOrDefault();
        Assert.IsNotNull(rootNamespaceElement);
        var checkedOutBranch = GitUtilities.CheckedOutBranch(ChabTarget.Folder());
        Document = await sut.CreateNuSpecAsync(solutionFileFullName, checkedOutBranch , new List<string> { "Red", "White", "Blue", "Green<", "Orange&", "Violet>" }, errorsAndInfos);
        Assert.IsNotNull(Document);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        var developerSettingsSecret = new DeveloperSettingsSecret();
        var developerSettings = await Container.Resolve<ISecretRepository>().GetAsync(developerSettingsSecret, errorsAndInfos);
        Assert.IsNotNull(developerSettings);
        VerifyTextElement(@"/package/metadata/id", ChabTarget.SolutionId);
        VerifyTextElement(@"/package/metadata/title", @"Aspenlaub.Net.GitHub.CSharp." + ChabTarget.SolutionId);
        VerifyTextElement(@"/package/metadata/description", @"Aspenlaub.Net.GitHub.CSharp." + ChabTarget.SolutionId);
        VerifyTextElement(@"/package/metadata/releaseNotes", @"Aspenlaub.Net.GitHub.CSharp." + ChabTarget.SolutionId);
        VerifyTextElement(@"/package/metadata/authors", developerSettings.Author);
        VerifyTextElement(@"/package/metadata/owners", developerSettings.Author);
        VerifyTextElement(@"/package/metadata/projectUrl", developerSettings.GitHubRepositoryUrl + ChabTarget.SolutionId);
        VerifyTextElement(@"/package/metadata/iconUrl", developerSettings.FaviconUrl);
        VerifyTextElement(@"/package/metadata/icon", "packageicon.png");
        VerifyTextElement(@"/package/metadata/requireLicenseAcceptance", @"false");
        var year = DateTime.Now.Year;
        VerifyTextElement(@"/package/metadata/copyright", $"Copyright {year}");
        VerifyTextElementPattern(@"/package/metadata/version", @"\d+.\d+.\d+.\d+");
        VerifyElements(@"/package/metadata/dependencies/group", "targetFramework", new List<string> { @"net9.0" }, false);
        VerifyElements(@"/package/metadata/dependencies/group/dependency", "id", new List<string> { "Autofac" }, false);
        VerifyElements(@"/package/files/file", "src", new List<string> { @"bin\Release\Aspenlaub.*.dll", @"bin\Release\Aspenlaub.*.pdb", @"bin\Release\packageicon.png" }, false);
        VerifyElements(@"/package/files/file", "exclude", new List<string> { @"bin\Release\*.Test*.*;bin\Release\*.exe;bin\Release\ref\*.*", @"bin\Release\*.Test*.*;bin\Release\*.exe;bin\Release\ref\*.*", null }, false);
        var target = @"lib\" + targetFrameworkElement.Value;
        VerifyElements(@"/package/files/file", "target", new List<string> { target, target, "" }, false);
        VerifyTextElement(@"/package/metadata/tags", @"Red White Blue");
        VerifyTargetFrameworkMoniker(@"/package/metadata/dependencies/group", "targetFramework");
        VerifyTargetFrameworkMoniker(@"/package/files/file", "target");
    }

    [TestMethod]
    public async Task CanCreateNuSpecForDvin() {
        var gitUtilities = new GitUtilities();
        var errorsAndInfos = new ErrorsAndInfos();
        const string url = "https://github.com/aspenlaub/Dvin.git";
        gitUtilities.Clone(url, "master", DvinTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

        Container.Resolve<IEmbeddedCakeScriptCopier>().CopyCakeScriptEmbeddedInAssembly(Assembly.GetExecutingAssembly(), BuildCake.Standard, DvinTarget, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

        Container.Resolve<ITestTargetRunner>().RunBuildCakeScript(BuildCake.Standard, DvinTarget, "CleanRestorePull", errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

        var sut = Container.Resolve<INuSpecCreator>();
        var solutionFileFullName = DvinTarget.Folder().SubFolder("src").FullName + @"\" + DvinTarget.SolutionId + ".sln";
        var checkedOutBranch = GitUtilities.CheckedOutBranch(DvinTarget.Folder());
        Document = await sut.CreateNuSpecAsync(solutionFileFullName, checkedOutBranch, new List<string> { "The", "Little", "Things" }, errorsAndInfos);
        Assert.IsNotNull(Document);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        VerifyElementsInverse(@"/package/metadata/dependencies/group/dependency", "id", new List<string> { "Dvin" });
        VerifyElements(@"/package/metadata/dependencies/group/dependency", "id", new List<string> { "Pegh" }, true);
        VerifyTargetFrameworkMoniker(@"/package/metadata/dependencies/group", "targetFramework");
        VerifyTargetFrameworkMoniker(@"/package/files/file", "target");
    }

    [TestMethod]
    public async Task CanCreateNuSpecForVishizhukel() {
        var gitUtilities = new GitUtilities();
        var errorsAndInfos = new ErrorsAndInfos();
        const string url = "https://github.com/aspenlaub/Vishizhukel.git";
        gitUtilities.Clone(url, "master", VishizhukelTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

        Container.Resolve<IEmbeddedCakeScriptCopier>().CopyCakeScriptEmbeddedInAssembly(Assembly.GetExecutingAssembly(), BuildCake.Standard, VishizhukelTarget, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

        Container.Resolve<ITestTargetRunner>().RunBuildCakeScript(BuildCake.Standard, VishizhukelTarget, "CleanRestorePull", errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

        var sut = Container.Resolve<INuSpecCreator>();
        var solutionFileFullName = VishizhukelTarget.Folder().SubFolder("src").FullName + @"\" + VishizhukelTarget.SolutionId + ".sln";
        var checkedOutBranch = GitUtilities.CheckedOutBranch(VishizhukelTarget.Folder());
        Document = await sut.CreateNuSpecAsync(solutionFileFullName, checkedOutBranch, new List<string> { "The", "Little", "Things" }, errorsAndInfos);
        Assert.IsNotNull(Document);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        VerifyElements(@"/package/metadata/dependencies/group/dependency", "id", new List<string> { "Dvin", "Microsoft.EntityFrameworkCore.SqlServer", "System.ComponentModel.Annotations" }, true);
        VerifyTargetFrameworkMoniker(@"/package/metadata/dependencies/group", "targetFramework");
        VerifyTargetFrameworkMoniker(@"/package/files/file", "target");
    }

    [TestMethod]
    public async Task CanCreateNuSpecForVishnetIntegrationTestTools() {
        var gitUtilities = new GitUtilities();
        var errorsAndInfos = new ErrorsAndInfos();
        const string url = "https://github.com/aspenlaub/VishnetIntegrationTestTools.git";
        gitUtilities.Clone(url, "master", VishnetIntegrationTestToolsTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

        Container.Resolve<IEmbeddedCakeScriptCopier>().CopyCakeScriptEmbeddedInAssembly(Assembly.GetExecutingAssembly(), BuildCake.Standard, VishnetIntegrationTestToolsTarget, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

        Container.Resolve<ITestTargetRunner>().RunBuildCakeScript(BuildCake.Standard, VishnetIntegrationTestToolsTarget, "CleanRestorePull", errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

        var sut = Container.Resolve<INuSpecCreator>();
        var solutionFileFullName = VishnetIntegrationTestToolsTarget.Folder().SubFolder("src").FullName + @"\" + VishnetIntegrationTestToolsTarget.SolutionId + ".sln";
        var checkedOutBranch = GitUtilities.CheckedOutBranch(VishnetIntegrationTestToolsTarget.Folder());
        Document = await sut.CreateNuSpecAsync(solutionFileFullName, checkedOutBranch, new List<string> { "The", "Little", "Things" }, errorsAndInfos);
        Assert.IsNotNull(Document);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        VerifyElements(@"/package/metadata/dependencies/group/dependency", "id", new List<string> { "MSTest.TestAdapter", "MSTest.TestFramework", "TashClient" }, true);
        VerifyTargetFrameworkMoniker(@"/package/metadata/dependencies/group", "targetFramework");
        VerifyTargetFrameworkMoniker(@"/package/files/file", "target");
    }

    [TestMethod]
    public async Task CanCreateNuSpecForPakledPkgBranchTest() {
        var gitUtilities = new GitUtilities();
        var errorsAndInfos = new ErrorsAndInfos();
        const string url = "https://github.com/aspenlaub/Pakled.git";
        gitUtilities.Clone(url, "pkg-branch-test", PakledTarget.Folder(), new CloneOptions { BranchName = "pkg-branch-test" }, true, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

        Container.Resolve<IEmbeddedCakeScriptCopier>().CopyCakeScriptEmbeddedInAssembly(Assembly.GetExecutingAssembly(), BuildCake.Standard, PakledTarget, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

        Container.Resolve<ITestTargetRunner>().RunBuildCakeScript(BuildCake.Standard, PakledTarget, "IgnoreOutdatedBuildCakePendingChangesAndDoCreateOrPushPackage", errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        Assert.AreEqual(2, errorsAndInfos.Infos.Count(i => i.Contains("Results File:")));

        var sut = Container.Resolve<INuSpecCreator>();
        var solutionFileFullName = PakledTarget.Folder().SubFolder("src").FullName + @"\" + PakledTarget.SolutionId + ".sln";
        var projectFileFullName = PakledTarget.Folder().SubFolder("src").FullName + @"\" + PakledTarget.SolutionId + ".csproj";
        Assert.IsTrue(File.Exists(projectFileFullName));
        Document = XDocument.Load(projectFileFullName);
        var targetFrameworkElement = Document.XPathSelectElements("./Project/PropertyGroup/TargetFramework", NamespaceManager).FirstOrDefault();
        Assert.IsNotNull(targetFrameworkElement);
        var rootNamespaceElement = Document.XPathSelectElements("./Project/PropertyGroup/RootNamespace", NamespaceManager).FirstOrDefault();
        Assert.IsNotNull(rootNamespaceElement);
        var checkedOutBranch = GitUtilities.CheckedOutBranch(PakledTarget.Folder());
        Document = await sut.CreateNuSpecAsync(solutionFileFullName, checkedOutBranch, new List<string> { "Red", "White", "Blue", "Green<", "Orange&", "Violet>" }, errorsAndInfos);
        Assert.IsNotNull(Document);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        var developerSettingsSecret = new DeveloperSettingsSecret();
        var developerSettings = await Container.Resolve<ISecretRepository>().GetAsync(developerSettingsSecret, errorsAndInfos);
        Assert.IsNotNull(developerSettings);
        var idWithBranch = PakledTarget.SolutionId + "-PkgBranchTest";
        VerifyTextElement(@"/package/metadata/id", idWithBranch);
        VerifyTextElement(@"/package/metadata/title", @"Aspenlaub.Net.GitHub.CSharp." + idWithBranch);
        VerifyTextElement(@"/package/metadata/description", @"Aspenlaub.Net.GitHub.CSharp." + idWithBranch);
        VerifyTextElement(@"/package/metadata/releaseNotes", @"Aspenlaub.Net.GitHub.CSharp." + idWithBranch);
        VerifyTextElement(@"/package/metadata/authors", developerSettings.Author);
        VerifyTextElement(@"/package/metadata/owners", developerSettings.Author);
        VerifyTextElement(@"/package/metadata/projectUrl", developerSettings.GitHubRepositoryUrl + PakledTarget.SolutionId);
        VerifyTextElement(@"/package/metadata/iconUrl", developerSettings.FaviconUrl);
        VerifyTextElement(@"/package/metadata/icon", "packageicon.png");
        VerifyTextElement(@"/package/metadata/requireLicenseAcceptance", @"false");
        var year = DateTime.Now.Year;
        VerifyTextElement(@"/package/metadata/copyright", $"Copyright {year}");
        VerifyTextElementPattern(@"/package/metadata/version", @"2.4.\d+.\d+");
        VerifyElements(@"/package/metadata/dependencies/group", "targetFramework", new List<string> { @"net9.0" }, false);
        VerifyElements(@"/package/metadata/dependencies/group/dependency", "id", new List<string> { "Autofac", "System.Text.Json" }, false);
        VerifyElements(@"/package/files/file", "src", new List<string> { @"bin\Release\Aspenlaub.*.dll", @"bin\Release\Aspenlaub.*.pdb", @"bin\Release\packageicon.png" }, false);
        VerifyElements(@"/package/files/file", "exclude", new List<string> { @"bin\Release\*.Test*.*;bin\Release\*.exe;bin\Release\ref\*.*", @"bin\Release\*.Test*.*;bin\Release\*.exe;bin\Release\ref\*.*", null }, false);
        var target = @"lib\" + targetFrameworkElement.Value;
        VerifyElements(@"/package/files/file", "target", new List<string> { target, target, "" }, false);
        VerifyTextElement(@"/package/metadata/tags", @"Red White Blue");
        VerifyTargetFrameworkMoniker(@"/package/metadata/dependencies/group", "targetFramework");
        VerifyTargetFrameworkMoniker(@"/package/files/file", "target");
    }

    protected void VerifyTextElement(string xpath, string expectedContents) {
        xpath = xpath.Replace("/", "/nu:");
        var element = Document.XPathSelectElements(xpath, NamespaceManager).FirstOrDefault();
        Assert.IsNotNull(element, $"Element not found using {xpath}, expected {expectedContents}");
        Assert.AreEqual(expectedContents, element.Value, $"Element {xpath} should be {expectedContents}, got: {element.Value}");
    }

    protected void VerifyTextElementPattern(string xpath, string expectedPattern) {
        xpath = xpath.Replace("/", "/nu:");
        var element = Document.XPathSelectElements(xpath, NamespaceManager).FirstOrDefault();
        Assert.IsNotNull(element, $"Element not found using {xpath}, expected {expectedPattern}");
        var regEx = new Regex(expectedPattern, RegexOptions.IgnoreCase);
        var versionMatch = regEx.Match(element.Value);
        Assert.IsTrue(versionMatch.Success, $"Element {xpath} should be {expectedPattern}, got: {element.Value}");
    }

    protected void VerifyElements(string xpath, string attributeName, IList<string> attributeValues, bool couldBeMore) {
        xpath = xpath.Replace("/", "/nu:");
        var elements = Document.XPathSelectElements(xpath, NamespaceManager).ToList();
        if (couldBeMore) {
            Assert.IsTrue(attributeValues.Count <= elements.Count, $"Expected at least {attributeValues.Count} elements using {xpath}, got {elements.Count}");
        } else {
            Assert.AreEqual(attributeValues.Count, elements.Count, $"Expected {attributeValues.Count} elements using {xpath}, got {elements.Count}");
        }
        for (var i = 0; i < attributeValues.Count; i ++) {
            var element = elements[i];
            var attributeValue = attributeValues[i];
            var actualValue = element.Attribute(attributeName)?.Value;
            Assert.AreEqual(attributeValue, actualValue, $"Expected {attributeValue} for {attributeName} using {xpath}, got {actualValue}");
        }
    }

    protected void VerifyElementsInverse(string xpath, string attributeName, IList<string> unexpectedAttributeValueComponents) {
        xpath = xpath.Replace("/", "/nu:");
        var elements = Document.XPathSelectElements(xpath, NamespaceManager).ToList();
        foreach (var element in elements) {
            var actualValue = element.Attribute(attributeName)?.Value;
            Assert.IsFalse(unexpectedAttributeValueComponents.Any(c => actualValue?.Contains(c) == true));
        }
    }

    protected void VerifyTargetFrameworkMoniker(string xpath, string attributeName) {
        xpath = xpath.Replace("/", "/nu:");
        var elements = Document.XPathSelectElements(xpath, NamespaceManager).ToList();
        foreach (var element in elements) {
            var actualMoniker = element.Attribute(attributeName)?.Value;
            Assert.IsNotNull(actualMoniker);
            Assert.IsFalse(actualMoniker.Contains("-windows"));
        }
    }
}