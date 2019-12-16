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
using Aspenlaub.Net.GitHub.CSharp.Gitty.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Gitty.TestUtilities;
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

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Test {
    [TestClass]
    public class NuSpecCreatorTest {
        protected static TestTargetFolder ChabStandardTarget = new TestTargetFolder(nameof(NuSpecCreator), "ChabStandard");
        protected static TestTargetFolder DvinTarget = new TestTargetFolder(nameof(NuSpecCreator), "Dvin");
        protected static TestTargetFolder VishizhukelTarget = new TestTargetFolder(nameof(NuSpecCreator), "Vishizhukel");
        protected static TestTargetFolder LibGit2SharpTarget = new TestTargetFolder(nameof(NuSpecCreator), "LibGit2Sharp");
        private static IContainer vContainer;
        protected static TestTargetInstaller TargetInstaller;
        protected static TestTargetRunner TargetRunner;

        protected XDocument Document;
        protected XmlNamespaceManager NamespaceManager;

        public NuSpecCreatorTest() {
            NamespaceManager = new XmlNamespaceManager(new NameTable());
            NamespaceManager.AddNamespace("cp", XmlNamespaces.CsProjNamespaceUri);
            NamespaceManager.AddNamespace("nu", XmlNamespaces.NuSpecNamespaceUri);
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context) {
            vContainer = new ContainerBuilder().UseGittyTestUtilities().UseProtch().UseNuclideProtchGittyAndPegh(new DummyCsArgumentPrompter()).Build();
            TargetInstaller = vContainer.Resolve<TestTargetInstaller>();
            TargetRunner = vContainer.Resolve<TestTargetRunner>();
            TargetInstaller.DeleteCakeFolder(ChabStandardTarget);
            TargetInstaller.CreateCakeFolder(ChabStandardTarget, out var errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        }

        [TestInitialize]
        public void Initialize() {
            ChabStandardTarget.Delete();
            DvinTarget.Delete();
            VishizhukelTarget.Delete();
            LibGit2SharpTarget.Delete();
        }

        [TestCleanup]
        public void TestCleanup() {
            ChabStandardTarget.Delete();
            DvinTarget.Delete();
            VishizhukelTarget.Delete();
            LibGit2SharpTarget.Delete();
        }

        [TestMethod]
        public async Task CanCreateNuSpecForChabStandard() {
            var gitUtilities = new GitUtilities();
            var errorsAndInfos = new ErrorsAndInfos();
            const string url = "https://github.com/aspenlaub/ChabStandard.git";
            gitUtilities.Clone(url, "master", ChabStandardTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

            vContainer.Resolve<CakeBuildUtilities>().CopyCakeScriptEmbeddedInAssembly(Assembly.GetExecutingAssembly(), BuildCake.Standard, ChabStandardTarget, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

            vContainer.Resolve<TestTargetRunner>().RunBuildCakeScript(BuildCake.Standard, ChabStandardTarget, "IgnoreOutdatedBuildCakePendingChangesAndDoCreateOrPushPackage", errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.AreEqual(2, errorsAndInfos.Infos.Count(i => i.Contains("Results File:")));

            var sut = vContainer.Resolve<INuSpecCreator>();
            var solutionFileFullName = ChabStandardTarget.Folder().SubFolder("src").FullName + @"\" + ChabStandardTarget.SolutionId + ".sln";
            var projectFileFullName = ChabStandardTarget.Folder().SubFolder("src").FullName + @"\" + ChabStandardTarget.SolutionId + ".csproj";
            Assert.IsTrue(File.Exists(projectFileFullName));
            Document = XDocument.Load(projectFileFullName);
            var targetFrameworkElement = Document.XPathSelectElements("./Project/PropertyGroup/TargetFramework", NamespaceManager).FirstOrDefault();
            Assert.IsNotNull(targetFrameworkElement);
            var rootNamespaceElement = Document.XPathSelectElements("./Project/PropertyGroup/RootNamespace", NamespaceManager).FirstOrDefault();
            Assert.IsNotNull(rootNamespaceElement);
            Document = await sut.CreateNuSpecAsync(solutionFileFullName, new List<string> { "Red", "White", "Blue", "Green<", "Orange&", "Violet>" }, errorsAndInfos);
            Assert.IsNotNull(Document);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            var developerSettingsSecret = new DeveloperSettingsSecret();
            var developerSettings = await vContainer.Resolve<ISecretRepository>().GetAsync(developerSettingsSecret, errorsAndInfos);
            Assert.IsNotNull(developerSettings);
            VerifyTextElement(@"/package/metadata/id", ChabStandardTarget.SolutionId);
            VerifyTextElement(@"/package/metadata/title", @"Aspenlaub.Net.GitHub.CSharp." + ChabStandardTarget.SolutionId);
            VerifyTextElement(@"/package/metadata/description", @"Aspenlaub.Net.GitHub.CSharp." + ChabStandardTarget.SolutionId);
            VerifyTextElement(@"/package/metadata/releaseNotes", @"Aspenlaub.Net.GitHub.CSharp." + ChabStandardTarget.SolutionId);
            VerifyTextElement(@"/package/metadata/authors", developerSettings.Author);
            VerifyTextElement(@"/package/metadata/owners", developerSettings.Author);
            VerifyTextElement(@"/package/metadata/projectUrl", developerSettings.GitHubRepositoryUrl + ChabStandardTarget.SolutionId);
            VerifyTextElement(@"/package/metadata/icon", "packageicon.ico");
            VerifyTextElement(@"/package/metadata/requireLicenseAcceptance", @"false");
            var year = DateTime.Now.Year;
            VerifyTextElement(@"/package/metadata/copyright", $"Copyright {year}");
            VerifyTextElementPattern(@"/package/metadata/version", @"\d+.\d+.\d+.\d+");
            VerifyElements(@"/package/metadata/dependencies/group", "targetFramework", new List<string> { @"netstandard2.0" }, false);
            VerifyElements(@"/package/metadata/dependencies/group/dependency", "id", new List<string> { "Autofac" , "LibGit2Sharp", "Newtonsoft.Json" }, false);
            VerifyElements(@"/package/files/file", "src", new List<string> { @"bin\Release\Aspenlaub.*.dll", @"bin\Release\Aspenlaub.*.pdb", @"bin\Release\packageicon.ico" }, false);
            VerifyElements(@"/package/files/file", "exclude", new List<string> { @"bin\Release\*.Test*.*;bin\Release\*.exe", @"bin\Release\*.Test*.*;bin\Release\*.exe", null }, false);
            var target = @"lib\" + targetFrameworkElement.Value;
            VerifyElements(@"/package/files/file", "target", new List<string> { target, target, "" }, false);
            VerifyTextElement(@"/package/metadata/tags", @"Red White Blue");
        }

        [TestMethod]
        public async Task CanCreateNuSpecForDvin() {
            var gitUtilities = new GitUtilities();
            var errorsAndInfos = new ErrorsAndInfos();
            const string url = "https://github.com/aspenlaub/Dvin.git";
            gitUtilities.Clone(url, "master", DvinTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

            vContainer.Resolve<CakeBuildUtilities>().CopyCakeScriptEmbeddedInAssembly(Assembly.GetExecutingAssembly(), BuildCake.Standard, DvinTarget, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

            vContainer.Resolve<TestTargetRunner>().RunBuildCakeScript(BuildCake.Standard, DvinTarget, "CleanRestorePull", errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

            var sut = vContainer.Resolve<INuSpecCreator>();
            var solutionFileFullName = DvinTarget.Folder().SubFolder("src").FullName + @"\" + DvinTarget.SolutionId + ".sln";
            Document = await sut.CreateNuSpecAsync(solutionFileFullName, new List<string> { "The", "Little", "Things" }, errorsAndInfos);
            Assert.IsNotNull(Document);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            VerifyElementsInverse(@"/package/metadata/dependencies/group/dependency", "id", new List<string> { "Dvin" });
            VerifyElements(@"/package/metadata/dependencies/group/dependency", "id", new List<string> { "Pegh" }, true);
        }

        [TestMethod]
        public async Task CanCreateNuSpecForVishizhukel() {
            var gitUtilities = new GitUtilities();
            var errorsAndInfos = new ErrorsAndInfos();
            const string url = "https://github.com/aspenlaub/Vishizhukel.git";
            gitUtilities.Clone(url, "master", VishizhukelTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

            vContainer.Resolve<CakeBuildUtilities>().CopyCakeScriptEmbeddedInAssembly(Assembly.GetExecutingAssembly(), BuildCake.Standard, VishizhukelTarget, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

            vContainer.Resolve<TestTargetRunner>().RunBuildCakeScript(BuildCake.Standard, VishizhukelTarget, "CleanRestorePull", errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

            var sut = vContainer.Resolve<INuSpecCreator>();
            var solutionFileFullName = VishizhukelTarget.Folder().SubFolder("src").FullName + @"\" + VishizhukelTarget.SolutionId + ".sln";
            Document = await sut.CreateNuSpecAsync(solutionFileFullName, new List<string> { "The", "Little", "Things" }, errorsAndInfos);
            Assert.IsNotNull(Document);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            VerifyElements(@"/package/metadata/dependencies/group/dependency", "id", new List<string> { "Microsoft.EntityFrameworkCore.SqlServer", "Newtonsoft.Json", "Pegh" }, true);
        }

        protected void VerifyTextElement(string xpath, string expectedContents) {
            xpath = xpath.Replace("/", "/nu:");
            var element = Document.XPathSelectElements(xpath, NamespaceManager).FirstOrDefault();
            Assert.IsNotNull(element, $"Element not found using {xpath}, expected {expectedContents}");
            Assert.AreEqual(element.Value, expectedContents, $"Element {xpath} should be {expectedContents}, got: {element.Value}");
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
    }
}
