using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Protch;
using Aspenlaub.Net.GitHub.CSharp.Protch.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide {
    public class NuSpecCreator : INuSpecCreator {
        protected XNamespace NugetNamespace;
        protected XmlNamespaceManager NamespaceManager;
        private readonly IPackageConfigsScanner vPackageConfigsScanner;
        private readonly IProjectFactory vProjectFactory;
        private readonly ISecretRepository vSecretRepository;

        public NuSpecCreator(IPackageConfigsScanner packageConfigsScanner, IProjectFactory projectFactory, ISecretRepository secretRepository) {
            vPackageConfigsScanner = packageConfigsScanner;
            vProjectFactory = projectFactory;
            vSecretRepository = secretRepository;
            NugetNamespace = XmlNamespaces.NuSpecNamespaceUri;
            NamespaceManager = new XmlNamespaceManager(new NameTable());
            NamespaceManager.AddNamespace("cp", XmlNamespaces.CsProjNamespaceUri);
        }

        public async Task<XDocument> CreateNuSpecAsync(string solutionFileFullName, IList<string> tags, IErrorsAndInfos errorsAndInfos) {
            var document = new XDocument();
            var projectFile = solutionFileFullName.Replace(".sln", ".csproj");
            if (!File.Exists(projectFile)) {
                errorsAndInfos.Errors.Add(string.Format(Properties.Resources.ProjectFileNotFound, projectFile));
                return document;
            }

            XDocument projectDocument;
            string namespaceSelector, targetFramework;
            try {
                projectDocument = XDocument.Load(projectFile);
            } catch {
                errorsAndInfos.Errors.Add(string.Format(Properties.Resources.InvalidXmlFile, projectFile));
                return document;
            }
            try {
                var targetFrameworkElement = projectDocument.XPathSelectElements("./Project/PropertyGroup/TargetFramework", NamespaceManager).FirstOrDefault();
                namespaceSelector = targetFrameworkElement != null ? "" : "cp:";
                targetFramework = targetFrameworkElement != null ? targetFrameworkElement.Value : "";
            } catch {
                errorsAndInfos.Errors.Add(string.Format(Properties.Resources.ErrorReadingTargetFramework, projectFile));
                return document;
            }

            var version = @"$version$";
            if (namespaceSelector == "") {
                var project = vProjectFactory.Load(solutionFileFullName, projectFile, errorsAndInfos);
                var releasePropertyGroup = project.PropertyGroups.FirstOrDefault(p => p.Condition.Contains("Release"));
                if (releasePropertyGroup != null) {
                    var solutionFolder = new Folder(solutionFileFullName.Substring(0, solutionFileFullName.LastIndexOf('\\')));
                    var fullOutputFolder = new Folder(Path.Combine(solutionFolder.FullName, releasePropertyGroup.OutputPath == "" ? @"bin\Release\" : releasePropertyGroup.OutputPath));
                    var assemblyFileName = fullOutputFolder.FullName + '\\' + project.RootNamespace + ".dll";
                    if (File.Exists(assemblyFileName)) {
                        version = FileVersionInfo.GetVersionInfo(assemblyFileName).FileVersion;
                    }
                }
            }

            var dependencyIdsAndVersions = await vPackageConfigsScanner.DependencyIdsAndVersionsAsync(solutionFileFullName.Substring(0, solutionFileFullName.LastIndexOf('\\') + 1), false, errorsAndInfos);
            var element = new XElement(NugetNamespace + "package");
            var solutionId = solutionFileFullName.Substring(solutionFileFullName.LastIndexOf('\\') + 1).Replace(".sln", "");
            var metaData = await ReadMetaDataAsync(solutionId, projectDocument, dependencyIdsAndVersions, tags, namespaceSelector, version, targetFramework, errorsAndInfos);
            if (metaData == null) {
                errorsAndInfos.Errors.Add(string.Format(Properties.Resources.MissingMetaDataElementInProjectFile, projectFile));
                return document;
            }

            element.Add(metaData);
            var files = Files(projectDocument, namespaceSelector, errorsAndInfos);
            if (files == null) {
                errorsAndInfos.Errors.Add(string.Format(Properties.Resources.MissingElementInProjectFile, projectFile));
                return document;
            }

            element.Add(files);
            document.Add(element);
            return document;
        }

        protected async Task<XElement> ReadMetaDataAsync(string solutionId, XDocument projectDocument, IDictionary<string, string> dependencyIdsAndVersions, IList<string> tags, string namespaceSelector, string version, string targetFramework, IErrorsAndInfos errorsAndInfos) {
            var rootNamespaceElement = projectDocument.XPathSelectElements("./" + namespaceSelector + "Project/" + namespaceSelector + "PropertyGroup/" + namespaceSelector + "RootNamespace", NamespaceManager).FirstOrDefault();
            if (rootNamespaceElement == null) { return null; }

            var developerSettingsSecret = new DeveloperSettingsSecret();
            var developerSettings = await vSecretRepository.GetAsync(developerSettingsSecret, errorsAndInfos);
            if (developerSettings == null) {
                errorsAndInfos.Errors.Add(string.Format(Properties.Resources.MissingDeveloperSettings, developerSettingsSecret.Guid + ".xml"));
                return null;
            }

            var author = developerSettings.Author;
            var gitHubRepositoryUrl = developerSettings.GitHubRepositoryUrl;

            var packageId
                = projectDocument.XPathSelectElements("./" + namespaceSelector + "Project/" + namespaceSelector + "PropertyGroup/" + namespaceSelector + "PackageId", NamespaceManager).FirstOrDefault()?.Value
                ?? rootNamespaceElement.Value;

            var element = new XElement(NugetNamespace + @"metadata");
            foreach (var elementName in new[] { @"id", @"title", @"description", @"releaseNotes" }) {
                element.Add(new XElement(NugetNamespace + elementName, elementName == @"id" ? packageId : rootNamespaceElement.Value));
            }

            foreach (var elementName in new[] { @"authors", @"owners" }) {
                element.Add(new XElement(NugetNamespace + elementName, author));
            }

            element.Add(new XElement(NugetNamespace + @"projectUrl", gitHubRepositoryUrl + solutionId));
            element.Add(new XElement(NugetNamespace + @"icon", "packageicon.ico"));
            element.Add(new XElement(NugetNamespace + @"requireLicenseAcceptance", @"false"));
            var year = DateTime.Now.Year;
            element.Add(new XElement(NugetNamespace + @"copyright", $"Copyright {year}"));
            element.Add(new XElement(NugetNamespace + @"version", version));
            tags = tags.Where(t => !t.Contains('<') && !t.Contains('>') && !t.Contains('&') && !t.Contains(' ')).ToList();
            if (tags.Any()) {
                element.Add(new XElement(NugetNamespace + @"tags", string.Join(" ", tags)));
            }

            var dependenciesElement = new XElement(NugetNamespace + @"dependencies");
            element.Add(dependenciesElement);

            if (namespaceSelector == "") {
                var groupElement = new XElement(NugetNamespace + "group", new XAttribute("targetFramework", targetFramework));
                dependenciesElement.Add(groupElement);
                dependenciesElement = groupElement;
            } else {
                var targetFrameworkElement =
                    projectDocument.XPathSelectElements("./" + namespaceSelector + "Project/" + namespaceSelector + "PropertyGroup/" + namespaceSelector + "TargetFrameworkVersion", NamespaceManager)
                        .FirstOrDefault()
                    ?? projectDocument.XPathSelectElements("./" + namespaceSelector + "Project/" + namespaceSelector + "PropertyGroup/" + namespaceSelector + "TargetFramework", NamespaceManager)
                        .FirstOrDefault();
                if (targetFrameworkElement != null) {
                    var groupElement = new XElement(NugetNamespace + "group", new XAttribute("targetFramework", "net" + TargetFrameworkElementToLibNetSuffix(targetFrameworkElement)));
                    dependenciesElement.Add(groupElement);
                    dependenciesElement = groupElement;
                }
            }

            foreach (var dependencyElement in dependencyIdsAndVersions.Select(dependencyIdAndVersion
                => dependencyIdAndVersion.Value == ""
                    ? new XElement(NugetNamespace + @"dependency", new XAttribute("id", dependencyIdAndVersion.Key))
                    : new XElement(NugetNamespace + @"dependency", new XAttribute("id", dependencyIdAndVersion.Key), new XAttribute("version", dependencyIdAndVersion.Value)))) {
                dependenciesElement.Add(dependencyElement);
            }

            return element;
        }

        private static bool ParentIsReleasePropertyGroup(XElement e) {
            return e.Parent?.Attributes("Condition").Any(v => v.Value.Contains("Release")) == true;
        }

        protected XElement Files(XDocument projectDocument, string namespaceSelector, IErrorsAndInfos errorsAndInfos) {
            var rootNamespaceElement = projectDocument.XPathSelectElements("./" + namespaceSelector + "Project/" + namespaceSelector + "PropertyGroup/" + namespaceSelector + "RootNamespace", NamespaceManager).FirstOrDefault();
            if (rootNamespaceElement == null) {
                errorsAndInfos.Errors.Add(Properties.Resources.MissingRootNamespace);
                return null;
            }

            var outputPathElement = projectDocument.XPathSelectElements("./" + namespaceSelector + "Project/" + namespaceSelector + "PropertyGroup/" + namespaceSelector + "OutputPath", NamespaceManager).SingleOrDefault(ParentIsReleasePropertyGroup);
            var outputPath = outputPathElement == null ? @"bin\Release\" : outputPathElement.Value;

            var targetFrameworkElement = projectDocument.XPathSelectElements("./" + namespaceSelector + "Project/" + namespaceSelector + "PropertyGroup/" + namespaceSelector + "TargetFrameworkVersion", NamespaceManager).FirstOrDefault()
                ?? projectDocument.XPathSelectElements("./" + namespaceSelector + "Project/" + namespaceSelector + "PropertyGroup/" + namespaceSelector + "TargetFramework", NamespaceManager).FirstOrDefault();
            if (targetFrameworkElement == null) {
                errorsAndInfos.Errors.Add(Properties.Resources.MissingTargetFramework);
                return null;
            }

            var filesElement = new XElement(NugetNamespace + @"files");
            var topLevelNamespace = rootNamespaceElement.Value;
            if (!topLevelNamespace.Contains('.')) {
                errorsAndInfos.Errors.Add(string.Format(Properties.Resources.TopLevelNamespaceLacksADot, topLevelNamespace));
                return null;
            }

            topLevelNamespace = topLevelNamespace.Substring(0, topLevelNamespace.IndexOf('.'));
            foreach (var fileElement in new[] { @"dll", @"pdb" }.Select(extension
                  => new XElement(NugetNamespace + @"file",
                      new XAttribute(@"src", outputPath + topLevelNamespace + ".*." + extension),
                      new XAttribute(@"exclude", string.Join(";", outputPath + @"*.Test*.*", outputPath + @"*.exe")),
                      new XAttribute(@"target", @"lib\net" + TargetFrameworkElementToLibNetSuffix(targetFrameworkElement))))) {
                filesElement.Add(fileElement);
            }

            filesElement.Add(new XElement(NugetNamespace + @"file",
                new XAttribute(@"src", outputPath + "packageicon.ico"),
                new XAttribute(@"target", "")));

            var foldersToPack = projectDocument.XPathSelectElements("./" + namespaceSelector + "Project/" + namespaceSelector + "ItemGroup/" + namespaceSelector + "Content", NamespaceManager)
                .Where(IncludesFileToPack).Select(IncludeAttributeValue).Select(f => f.Substring(0, f.LastIndexOf('\\'))).Distinct().ToList();
            foreach (var folderToPack in foldersToPack) {
                var target = folderToPack;
                if (folderToPack.StartsWith("lib")) {
                    target = @"lib\net" + TargetFrameworkElementToLibNetSuffix(targetFrameworkElement) + target.Substring(3);
                }
                filesElement.Add(new XElement(NugetNamespace + @"file",
                    new XAttribute(@"src", outputPath + folderToPack + @"\*.*"),
                    new XAttribute(@"exclude", ""),
                    new XAttribute(@"target", target)));
            }

            return filesElement;
        }

        private static string IncludeAttributeValue(XElement contentElement) {
            var attribute = contentElement.Attributes().FirstOrDefault(a => a.Name == "Include");
            return new[] { @"build\", @"lib\", @"runtimes\" }.Any(folder => attribute?.Value.StartsWith(folder) == true) ? attribute?.Value : null;
        }

        private static bool IncludesFileToPack(XElement contentElement) {
            return !string.IsNullOrWhiteSpace(IncludeAttributeValue(contentElement));
        }

        private static string TargetFrameworkElementToLibNetSuffix(XElement targetFrameworkElement) {
            var targetFramework = targetFrameworkElement.Value;
            return targetFramework.StartsWith("v") ? targetFramework.Replace("v", "").Replace(".", "") : targetFramework.StartsWith("net") ? targetFramework.Substring(3) : targetFramework;
        }

        public async Task CreateNuSpecFileIfRequiredOrPresentAsync(bool required, string solutionFileFullName, IList<string> tags, IErrorsAndInfos errorsAndInfos) {
            var nuSpecFile = solutionFileFullName.Replace(".sln", ".nuspec");
            if (!required && !File.Exists(nuSpecFile)) { return; }

            var document = await CreateNuSpecAsync(solutionFileFullName, tags, errorsAndInfos);
            if (errorsAndInfos.Errors.Any()) { return; }

            var tempFileName = Path.GetTempPath() + @"AspenlaubTemp\temp.nuspec";
            document.Save(tempFileName);
            if (File.Exists(nuSpecFile) && File.ReadAllText(nuSpecFile) == File.ReadAllText(tempFileName)) { return; }

            File.Copy(tempFileName, nuSpecFile, true);
            errorsAndInfos.Infos.Add(string.Format(Properties.Resources.NuSpecFileUpdated, nuSpecFile));
        }
    }
}
