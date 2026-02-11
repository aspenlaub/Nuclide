using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private static readonly TestTargetFolder _chabTarget = new(nameof(NuSpecCreator), "Chab");
    private static readonly TestTargetFolder _dvinTarget = new(nameof(NuSpecCreator), "Dvin");
    private static readonly TestTargetFolder _vishizhukelTarget = new(nameof(NuSpecCreator), "Vishizhukel");
    private static readonly TestTargetFolder _vishnetIntegrationTestToolsTarget = new(nameof(NuSpecCreator), "VishnetIntegrationTestTools");
    private static readonly TestTargetFolder _libGit2SharpTarget = new(nameof(NuSpecCreator), "LibGit2Sharp");
    private static readonly TestTargetFolder _pakledTarget = new(nameof(NuSpecCreator), "Pakled");
    private static IContainer _container;
    private static IGitUtilities _gitUtilities;

    protected XDocument Document;
    protected XmlNamespaceManager NamespaceManager;

    public NuSpecCreatorTest() {
        NamespaceManager = new XmlNamespaceManager(new NameTable());
        NamespaceManager.AddNamespace("cp", XmlNamespaces.CsProjNamespaceUri);
        NamespaceManager.AddNamespace("nu", XmlNamespaces.NuSpecNamespaceUri);
    }

    [ClassInitialize]
    public static void ClassInitialize(TestContext context) {
        _container = new ContainerBuilder().UseGittyTestUtilities().UseProtch().UseNuclideProtchGittyAndPegh("Nuclide").Build();
        _gitUtilities = _container.Resolve<IGitUtilities>();
    }

    [TestInitialize]
    public void Initialize() {
        _chabTarget.Delete();
        _dvinTarget.Delete();
        _vishizhukelTarget.Delete();
        _libGit2SharpTarget.Delete();
        _pakledTarget.Delete();
        _vishnetIntegrationTestToolsTarget.Delete();
    }

    [TestCleanup]
    public void TestCleanup() {
        _chabTarget.Delete();
        _dvinTarget.Delete();
        _vishizhukelTarget.Delete();
        _libGit2SharpTarget.Delete();
        _pakledTarget.Delete();
        _vishnetIntegrationTestToolsTarget.Delete();
    }

    [TestMethod]
    public async Task CanCreateNuSpecForChab() {
        var gitUtilities = new GitUtilities();
        var errorsAndInfos = new ErrorsAndInfos();
        const string url = "https://github.com/aspenlaub/Chab.git";
        gitUtilities.Clone(url, "master", _chabTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

        await _container.Resolve<IShatilayaRunner>().RunShatilayaAsync(_chabTarget.Folder(), "IgnorePendingChangesAndDoNotCreateOrPushPackage", errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        Assert.AreEqual(2, errorsAndInfos.Infos.Count(i => i.Contains("Results File:")));

        INuSpecCreator sut = _container.Resolve<INuSpecCreator>();
        string solutionFileFullName = _chabTarget.Folder().SubFolder("src").FullName + @"\" + _chabTarget.SolutionId + ".slnx";
        Assert.IsTrue(File.Exists(solutionFileFullName));
        string projectFileFullName = _chabTarget.Folder().SubFolder("src").FullName + @"\" + _chabTarget.SolutionId + ".csproj";
        Assert.IsTrue(File.Exists(projectFileFullName));
        Document = XDocument.Load(projectFileFullName);
        XElement targetFrameworkElement = Document.XPathSelectElements("./Project/PropertyGroup/TargetFramework", NamespaceManager).FirstOrDefault();
        Assert.IsNotNull(targetFrameworkElement);
        XElement rootNamespaceElement = Document.XPathSelectElements("./Project/PropertyGroup/RootNamespace", NamespaceManager).FirstOrDefault();
        Assert.IsNotNull(rootNamespaceElement);
        string checkedOutBranch = _gitUtilities.CheckedOutBranch(_chabTarget.Folder());
        Document = await sut.CreateNuSpecAsync(solutionFileFullName, checkedOutBranch , ["Red", "White", "Blue", "Green<", "Orange&", "Violet>"], errorsAndInfos);
        Assert.IsNotNull(Document);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        var developerSettingsSecret = new DeveloperSettingsSecret();
        DeveloperSettings developerSettings = await _container.Resolve<ISecretRepository>().GetAsync(developerSettingsSecret, errorsAndInfos);
        Assert.IsNotNull(developerSettings);
        VerifyTextElement(@"/package/metadata/id", _chabTarget.SolutionId);
        VerifyTextElement(@"/package/metadata/title", @"Aspenlaub.Net.GitHub.CSharp." + _chabTarget.SolutionId);
        VerifyTextElement(@"/package/metadata/description", @"Aspenlaub.Net.GitHub.CSharp." + _chabTarget.SolutionId);
        VerifyTextElement(@"/package/metadata/releaseNotes", @"Aspenlaub.Net.GitHub.CSharp." + _chabTarget.SolutionId);
        VerifyTextElement(@"/package/metadata/authors", developerSettings.Author);
        VerifyTextElement(@"/package/metadata/owners", developerSettings.Author);
        VerifyTextElement(@"/package/metadata/projectUrl", developerSettings.GitHubRepositoryUrl + _chabTarget.SolutionId);
        VerifyTextElement(@"/package/metadata/iconUrl", developerSettings.FaviconUrl);
        VerifyTextElement(@"/package/metadata/icon", "packageicon.png");
        VerifyTextElement(@"/package/metadata/requireLicenseAcceptance", @"false");
        int year = DateTime.Now.Year;
        VerifyTextElement(@"/package/metadata/copyright", $"Copyright {year}");
        VerifyTextElementPattern(@"/package/metadata/version", @"\d+.\d+.\d+.\d+");
        VerifyElements(@"/package/metadata/dependencies/group", "targetFramework", [@"net10.0"], false);
        VerifyElements(@"/package/metadata/dependencies/group/dependency", "id", ["Autofac"], false);
        VerifyElements(@"/package/files/file", "src", [@"bin\Release\Aspenlaub.*.dll", @"bin\Release\Aspenlaub.*.pdb", @"bin\Release\packageicon.png"], false);
        VerifyElements(@"/package/files/file", "exclude", [@"bin\Release\*.Test*.*;bin\Release\*.exe;bin\Release\ref\*.*", @"bin\Release\*.Test*.*;bin\Release\*.exe;bin\Release\ref\*.*", null], false);
        string target = @"lib\" + targetFrameworkElement.Value;
        VerifyElements(@"/package/files/file", "target", [target, target, ""], false);
        VerifyTextElement(@"/package/metadata/tags", @"Red White Blue");
        VerifyTargetFrameworkMoniker(@"/package/metadata/dependencies/group", "targetFramework");
        VerifyTargetFrameworkMoniker(@"/package/files/file", "target");
    }

    [TestMethod]
    public async Task CanCreateNuSpecForDvin() {
        var gitUtilities = new GitUtilities();
        var errorsAndInfos = new ErrorsAndInfos();
        const string url = "https://github.com/aspenlaub/Dvin.git";
        gitUtilities.Clone(url, "master", _dvinTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

        await _container.Resolve<IShatilayaRunner>().RunShatilayaAsync(_dvinTarget.Folder(), "CleanRestorePull", errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

        INuSpecCreator sut = _container.Resolve<INuSpecCreator>();
        string solutionFileFullName = _dvinTarget.Folder().SubFolder("src").FullName + @"\" + _dvinTarget.SolutionId + ".slnx";
        Assert.IsTrue(File.Exists(solutionFileFullName));
        string checkedOutBranch = _gitUtilities.CheckedOutBranch(_dvinTarget.Folder());
        Document = await sut.CreateNuSpecAsync(solutionFileFullName, checkedOutBranch, ["The", "Little", "Things"], errorsAndInfos);
        Assert.IsNotNull(Document);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        VerifyElementsInverse(@"/package/metadata/dependencies/group/dependency", "id", ["Dvin"]);
        VerifyElements(@"/package/metadata/dependencies/group/dependency", "id", ["Pegh"], true);
        VerifyTargetFrameworkMoniker(@"/package/metadata/dependencies/group", "targetFramework");
        VerifyTargetFrameworkMoniker(@"/package/files/file", "target");
    }

    [TestMethod]
    public async Task CanCreateNuSpecForVishizhukel() {
        var gitUtilities = new GitUtilities();
        var errorsAndInfos = new ErrorsAndInfos();
        const string url = "https://github.com/aspenlaub/Vishizhukel.git";
        gitUtilities.Clone(url, "master", _vishizhukelTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

        await _container.Resolve<IShatilayaRunner>().RunShatilayaAsync(_vishizhukelTarget.Folder(), "CleanRestorePull", errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

        INuSpecCreator sut = _container.Resolve<INuSpecCreator>();
        string solutionFileFullName = _vishizhukelTarget.Folder().SubFolder("src").FullName + @"\" + _vishizhukelTarget.SolutionId + ".slnx";
        Assert.IsTrue(File.Exists(solutionFileFullName));
        string checkedOutBranch = _gitUtilities.CheckedOutBranch(_vishizhukelTarget.Folder());
        Document = await sut.CreateNuSpecAsync(solutionFileFullName, checkedOutBranch, ["The", "Little", "Things"], errorsAndInfos);
        Assert.IsNotNull(Document);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        VerifyElements(@"/package/metadata/dependencies/group/dependency", "id", ["Dvin", "Microsoft.EntityFrameworkCore.SqlServer", "System.ComponentModel.Annotations"], true);
        VerifyTargetFrameworkMoniker(@"/package/metadata/dependencies/group", "targetFramework");
        VerifyTargetFrameworkMoniker(@"/package/files/file", "target");
    }

    [TestMethod]
    public async Task CanCreateNuSpecForVishnetIntegrationTestTools() {
        var gitUtilities = new GitUtilities();
        var errorsAndInfos = new ErrorsAndInfos();
        const string url = "https://github.com/aspenlaub/VishnetIntegrationTestTools.git";
        gitUtilities.Clone(url, "master", _vishnetIntegrationTestToolsTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

        await _container.Resolve<IShatilayaRunner>().RunShatilayaAsync(_vishnetIntegrationTestToolsTarget.Folder(), "CleanRestorePull", errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

        INuSpecCreator sut = _container.Resolve<INuSpecCreator>();
        string solutionFileFullName = _vishnetIntegrationTestToolsTarget.Folder().SubFolder("src").FullName + @"\" + _vishnetIntegrationTestToolsTarget.SolutionId + ".slnx";
        Assert.IsTrue(File.Exists(solutionFileFullName));
        string checkedOutBranch = _gitUtilities.CheckedOutBranch(_vishnetIntegrationTestToolsTarget.Folder());
        Document = await sut.CreateNuSpecAsync(solutionFileFullName, checkedOutBranch, ["The", "Little", "Things"], errorsAndInfos);
        Assert.IsNotNull(Document);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        VerifyElements(@"/package/metadata/dependencies/group/dependency", "id", ["MSTest.TestAdapter", "MSTest.TestFramework", "TashClient"], true);
        VerifyTargetFrameworkMoniker(@"/package/metadata/dependencies/group", "targetFramework");
        VerifyTargetFrameworkMoniker(@"/package/files/file", "target");
    }

    [TestMethod]
    public async Task CanCreateNuSpecForPakledPkgBranchTest() {
        var gitUtilities = new GitUtilities();
        var errorsAndInfos = new ErrorsAndInfos();
        const string url = "https://github.com/aspenlaub/Pakled.git";
        gitUtilities.Clone(url, "pkg-branch-test", _pakledTarget.Folder(), new CloneOptions { BranchName = "pkg-branch-test" }, true, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

        await _container.Resolve<IShatilayaRunner>().RunShatilayaAsync(_pakledTarget.Folder(), "IgnorePendingChangesAndDoNotCreateOrPushPackage", errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        Assert.AreEqual(2, errorsAndInfos.Infos.Count(i => i.Contains("Results File:")));

        INuSpecCreator sut = _container.Resolve<INuSpecCreator>();
        string solutionFileFullName = _pakledTarget.Folder().SubFolder("src").FullName + @"\" + _pakledTarget.SolutionId + ".slnx";
        Assert.IsTrue(File.Exists(solutionFileFullName));
        string projectFileFullName = _pakledTarget.Folder().SubFolder("src").FullName + @"\" + _pakledTarget.SolutionId + ".csproj";
        Assert.IsTrue(File.Exists(projectFileFullName));
        Document = XDocument.Load(projectFileFullName);
        XElement targetFrameworkElement = Document.XPathSelectElements("./Project/PropertyGroup/TargetFramework", NamespaceManager).FirstOrDefault();
        Assert.IsNotNull(targetFrameworkElement);
        XElement rootNamespaceElement = Document.XPathSelectElements("./Project/PropertyGroup/RootNamespace", NamespaceManager).FirstOrDefault();
        Assert.IsNotNull(rootNamespaceElement);
        string checkedOutBranch = _gitUtilities.CheckedOutBranch(_pakledTarget.Folder());
        Document = await sut.CreateNuSpecAsync(solutionFileFullName, checkedOutBranch, ["Red", "White", "Blue", "Green<", "Orange&", "Violet>"], errorsAndInfos);
        Assert.IsNotNull(Document);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        var developerSettingsSecret = new DeveloperSettingsSecret();
        DeveloperSettings developerSettings = await _container.Resolve<ISecretRepository>().GetAsync(developerSettingsSecret, errorsAndInfos);
        Assert.IsNotNull(developerSettings);
        string idWithBranch = _pakledTarget.SolutionId + "-PkgBranchTest";
        VerifyTextElement(@"/package/metadata/id", idWithBranch);
        VerifyTextElement(@"/package/metadata/title", @"Aspenlaub.Net.GitHub.CSharp." + idWithBranch);
        VerifyTextElement(@"/package/metadata/description", @"Aspenlaub.Net.GitHub.CSharp." + idWithBranch);
        VerifyTextElement(@"/package/metadata/releaseNotes", @"Aspenlaub.Net.GitHub.CSharp." + idWithBranch);
        VerifyTextElement(@"/package/metadata/authors", developerSettings.Author);
        VerifyTextElement(@"/package/metadata/owners", developerSettings.Author);
        VerifyTextElement(@"/package/metadata/projectUrl", developerSettings.GitHubRepositoryUrl + _pakledTarget.SolutionId);
        VerifyTextElement(@"/package/metadata/iconUrl", developerSettings.FaviconUrl);
        VerifyTextElement(@"/package/metadata/icon", "packageicon.png");
        VerifyTextElement(@"/package/metadata/requireLicenseAcceptance", @"false");
        int year = DateTime.Now.Year;
        VerifyTextElement(@"/package/metadata/copyright", $"Copyright {year}");
        VerifyTextElementPattern(@"/package/metadata/version", @"2.4.\d+.\d+");
        VerifyElements(@"/package/metadata/dependencies/group", "targetFramework", [@"net10.0"], false);
        VerifyElements(@"/package/metadata/dependencies/group/dependency", "id", ["Autofac", "System.Text.Json"], false);
        VerifyElements(@"/package/files/file", "src", [@"bin\Release\Aspenlaub.*.dll", @"bin\Release\Aspenlaub.*.pdb", @"bin\Release\packageicon.png"], false);
        VerifyElements(@"/package/files/file", "exclude", [@"bin\Release\*.Test*.*;bin\Release\*.exe;bin\Release\ref\*.*", @"bin\Release\*.Test*.*;bin\Release\*.exe;bin\Release\ref\*.*", null], false);
        string target = @"lib\" + targetFrameworkElement.Value;
        VerifyElements(@"/package/files/file", "target", [target, target, ""], false);
        VerifyTextElement(@"/package/metadata/tags", @"Red White Blue");
        VerifyTargetFrameworkMoniker(@"/package/metadata/dependencies/group", "targetFramework");
        VerifyTargetFrameworkMoniker(@"/package/files/file", "target");
    }

    protected void VerifyTextElement(string xpath, string expectedContents) {
        xpath = xpath.Replace("/", "/nu:");
        XElement element = Document.XPathSelectElements(xpath, NamespaceManager).FirstOrDefault();
        Assert.IsNotNull(element, $"Element not found using {xpath}, expected {expectedContents}");
        Assert.AreEqual(expectedContents, element.Value, $"Element {xpath} should be {expectedContents}, got: {element.Value}");
    }

    protected void VerifyTextElementPattern(string xpath, string expectedPattern) {
        xpath = xpath.Replace("/", "/nu:");
        XElement element = Document.XPathSelectElements(xpath, NamespaceManager).FirstOrDefault();
        Assert.IsNotNull(element, $"Element not found using {xpath}, expected {expectedPattern}");
        var regEx = new Regex(expectedPattern, RegexOptions.IgnoreCase);
        Match versionMatch = regEx.Match(element.Value);
        Assert.IsTrue(versionMatch.Success, $"Element {xpath} should be {expectedPattern}, got: {element.Value}");
    }

    protected void VerifyElements(string xpath, string attributeName, IList<string> attributeValues, bool couldBeMore) {
        xpath = xpath.Replace("/", "/nu:");
        var elements = Document.XPathSelectElements(xpath, NamespaceManager).ToList();
        if (couldBeMore) {
            Assert.IsGreaterThanOrEqualTo(attributeValues.Count, elements.Count, $"Expected at least {attributeValues.Count} elements using {xpath}, got {elements.Count}");
        } else {
            Assert.HasCount(attributeValues.Count, elements, $"Expected {attributeValues.Count} elements using {xpath}, got {elements.Count}");
        }
        for (int i = 0; i < attributeValues.Count; i ++) {
            XElement element = elements[i];
            string attributeValue = attributeValues[i];
            string actualValue = element.Attribute(attributeName)?.Value;
            Assert.AreEqual(attributeValue, actualValue, $"Expected {attributeValue} for {attributeName} using {xpath}, got {actualValue}");
        }
    }

    protected void VerifyElementsInverse(string xpath, string attributeName, IList<string> unexpectedAttributeValueComponents) {
        xpath = xpath.Replace("/", "/nu:");
        var elements = Document.XPathSelectElements(xpath, NamespaceManager).ToList();
        foreach (XElement element in elements) {
            string actualValue = element.Attribute(attributeName)?.Value;
            Assert.IsFalse(unexpectedAttributeValueComponents.Any(c => actualValue?.Contains(c) == true));
        }
    }

    protected void VerifyTargetFrameworkMoniker(string xpath, string attributeName) {
        xpath = xpath.Replace("/", "/nu:");
        var elements = Document.XPathSelectElements(xpath, NamespaceManager).ToList();
        foreach (XElement element in elements) {
            string actualMoniker = element.Attribute(attributeName)?.Value;
            Assert.IsNotNull(actualMoniker);
            Assert.DoesNotContain("-windows", actualMoniker);
        }
    }
}