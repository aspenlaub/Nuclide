using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
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
using Version = Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities.Version;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Components;

public class NuSpecCreator : INuSpecCreator {
    protected XNamespace NugetNamespace;
    protected XmlNamespaceManager NamespaceManager;
    private readonly IPackageReferencesScanner _PackageReferencesScanner;
    private readonly IProjectFactory _ProjectFactory;
    private readonly ISecretRepository _SecretRepository;
    private readonly IBranchesWithPackagesRepository _BranchesWithPackagesRepository;

    public NuSpecCreator(IPackageReferencesScanner packageReferencesScanner, IProjectFactory projectFactory,
            ISecretRepository secretRepository, IBranchesWithPackagesRepository branchesWithPackagesRepository) {
        _PackageReferencesScanner = packageReferencesScanner;
        _ProjectFactory = projectFactory;
        _SecretRepository = secretRepository;
        _BranchesWithPackagesRepository = branchesWithPackagesRepository;
        NugetNamespace = XmlNamespaces.NuSpecNamespaceUri;
        NamespaceManager = new XmlNamespaceManager(new NameTable());
        NamespaceManager.AddNamespace("cp", XmlNamespaces.CsProjNamespaceUri);
    }

    public async Task<XDocument> CreateNuSpecAsync(string solutionFileFullName, string checkedOutBranch, IList<string> tags, IErrorsAndInfos errorsAndInfos) {
        var document = new XDocument();
        string projectFileFullName = solutionFileFullName
            .Replace(".slnx", ".csproj");
        if (!File.Exists(projectFileFullName)) {
            errorsAndInfos.Errors.Add(string.Format(Properties.Resources.ProjectFileNotFound, projectFileFullName));
            return document;
        }

        XDocument projectDocument;
        string namespaceSelector, targetFramework;
        try {
            projectDocument = XDocument.Load(projectFileFullName);
        } catch {
            errorsAndInfos.Errors.Add(string.Format(Properties.Resources.InvalidXmlFile, projectFileFullName));
            return document;
        }
        try {
            XElement targetFrameworkElement = projectDocument.XPathSelectElements("./Project/PropertyGroup/TargetFramework", NamespaceManager).FirstOrDefault();
            namespaceSelector = targetFrameworkElement != null ? "" : "cp:";
            targetFramework = targetFrameworkElement?.Value ?? "";
        } catch {
            errorsAndInfos.Errors.Add(string.Format(Properties.Resources.ErrorReadingTargetFramework, projectFileFullName));
            return document;
        }

        string versionAsString = @"$version$";
        if (namespaceSelector == "") {
            IProject project = _ProjectFactory.Load(solutionFileFullName, projectFileFullName, errorsAndInfos);
            IPropertyGroup releasePropertyGroup = project.PropertyGroups.FirstOrDefault(p => p.Condition.Contains("Release"));
            if (releasePropertyGroup != null) {
                var solutionFolder = new Folder(solutionFileFullName.Substring(0, solutionFileFullName.LastIndexOf('\\')));
                var fullOutputFolder = new Folder(Path.Combine(solutionFolder.FullName, releasePropertyGroup.OutputPath == "" ? @"bin\Release\" : releasePropertyGroup.OutputPath));
                string assemblyFileName = fullOutputFolder.FullName + '\\' + project.RootNamespace + ".dll";
                if (File.Exists(assemblyFileName)) {
                    versionAsString = FileVersionInfo.GetVersionInfo(assemblyFileName).FileVersion;
                }
            }
        }

        string versionFile = projectFileFullName.Substring(0, projectFileFullName.LastIndexOf('\\') + 1) + "version.json";
        if (File.Exists(versionFile)) {
            Version version = JsonSerializer.Deserialize<Version>(await File.ReadAllTextAsync(versionFile));
            if (version == null) {
                throw new FileNotFoundException(versionFile);
            }
            version.Build = DateTime.UtcNow.Subtract(new DateTime(2019, 7, 24)).Days;
            version.Revision = (int)Math.Floor(DateTime.UtcNow.Subtract(DateTime.UtcNow.Date).TotalMinutes);
            versionAsString = version.ToString();
        }

        IDictionary<string, string> dependencyIdsAndVersions = await _PackageReferencesScanner.DependencyIdsAndVersionsAsync(solutionFileFullName.Substring(0, solutionFileFullName.LastIndexOf('\\') + 1), false, errorsAndInfos);
        var element = new XElement(NugetNamespace + "package");
        string solutionId = solutionFileFullName.Substring(solutionFileFullName.LastIndexOf('\\') + 1)
            .Replace(".slnx", "");
        XElement metaData = await ReadMetaDataAsync(solutionId, checkedOutBranch, projectDocument, dependencyIdsAndVersions, tags, namespaceSelector, versionAsString, targetFramework, errorsAndInfos);
        if (metaData == null) {
            errorsAndInfos.Errors.Add(string.Format(Properties.Resources.MissingMetaDataElementInProjectFile, projectFileFullName));
            return document;
        }

        element.Add(metaData);
        XElement files = Files(projectDocument, namespaceSelector, errorsAndInfos);
        if (files == null) {
            errorsAndInfos.Errors.Add(string.Format(Properties.Resources.MissingElementInProjectFile, projectFileFullName));
            return document;
        }

        element.Add(files);
        document.Add(element);
        return document;
    }

    protected async Task<XElement> ReadMetaDataAsync(string solutionId, string checkedOutBranch, XDocument projectDocument, IDictionary<string, string> dependencyIdsAndVersions, IList<string> tags, string namespaceSelector, string version, string targetFramework, IErrorsAndInfos errorsAndInfos) {
        XElement rootNamespaceElement = projectDocument.XPathSelectElements("./" + namespaceSelector + "Project/" + namespaceSelector + "PropertyGroup/" + namespaceSelector + "RootNamespace", NamespaceManager).FirstOrDefault();
        if (rootNamespaceElement == null) { return null; }

        var developerSettingsSecret = new DeveloperSettingsSecret();
        DeveloperSettings developerSettings = await _SecretRepository.GetAsync(developerSettingsSecret, errorsAndInfos);
        if (developerSettings == null) {
            errorsAndInfos.Errors.Add(string.Format(Properties.Resources.MissingDeveloperSettings, developerSettingsSecret.Guid + ".xml"));
            return null;
        }

        IList<string> branchesWithPackages = await _BranchesWithPackagesRepository.GetBranchIdsAsync(errorsAndInfos);
        if (errorsAndInfos.AnyErrors()) {
            return null;
        }
        if (branchesWithPackages == null) {
            errorsAndInfos.Errors.Add(Properties.Resources.MissingBranchesWithPackagesSettings);
            return null;
        }
        if (!branchesWithPackages.Contains(checkedOutBranch)) {
            errorsAndInfos.Errors.Add(string.Format(Properties.Resources.BranchDoesNotHavePackages, checkedOutBranch));
            return null;
        }

        string author = developerSettings.Author;
        string gitHubRepositoryUrl = developerSettings.GitHubRepositoryUrl;
        string faviconUrl = developerSettings.FaviconUrl;

        string packageId
            = projectDocument.XPathSelectElements("./" + namespaceSelector + "Project/" + namespaceSelector + "PropertyGroup/" + namespaceSelector + "PackageId", NamespaceManager).FirstOrDefault()?.Value
              ?? rootNamespaceElement.Value;
        string packageIdWithBranch = packageId
                                     + _BranchesWithPackagesRepository.PackageInfix(checkedOutBranch, true);
        string rootNamespaceWithBranch = rootNamespaceElement.Value
                                         + _BranchesWithPackagesRepository.PackageInfix(checkedOutBranch, true);

        var element = new XElement(NugetNamespace + @"metadata");
        foreach (string elementName in new[] { @"id", @"title", @"description", @"releaseNotes" }) {
            element.Add(
                new XElement(NugetNamespace + elementName, elementName == @"id" ? packageIdWithBranch : rootNamespaceWithBranch));
        }

        foreach (string elementName in new[] { @"authors", @"owners" }) {
            element.Add(new XElement(NugetNamespace + elementName, author));
        }

        element.Add(new XElement(NugetNamespace + @"projectUrl", gitHubRepositoryUrl + solutionId));
        element.Add(new XElement(NugetNamespace + @"icon", "packageicon.png"));
        element.Add(new XElement(NugetNamespace + @"iconUrl", faviconUrl));
        element.Add(new XElement(NugetNamespace + @"requireLicenseAcceptance", @"false"));
        int year = DateTime.Now.Year;
        element.Add(new XElement(NugetNamespace + @"copyright", $"Copyright {year}"));
        element.Add(new XElement(NugetNamespace + @"version", version));
        tags = tags.Where(t => !t.Contains('<') && !t.Contains('>') && !t.Contains('&') && !t.Contains(' ')).ToList();
        if (tags.Any()) {
            element.Add(new XElement(NugetNamespace + @"tags", string.Join(" ", tags)));
        }

        var dependenciesElement = new XElement(NugetNamespace + @"dependencies");
        element.Add(dependenciesElement);

        if (namespaceSelector == "") {
            var groupElement = new XElement(NugetNamespace + "group", new XAttribute("targetFramework", "net" + TargetFrameworkToLibNetSuffix(targetFramework)));
            dependenciesElement.Add(groupElement);
            dependenciesElement = groupElement;
        } else {
            XElement targetFrameworkElement =
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

        foreach (XElement dependencyElement in dependencyIdsAndVersions.Select(dependencyIdAndVersion
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
        XElement rootNamespaceElement = projectDocument.XPathSelectElements("./" + namespaceSelector + "Project/" + namespaceSelector + "PropertyGroup/" + namespaceSelector + "RootNamespace", NamespaceManager).FirstOrDefault();
        if (rootNamespaceElement == null) {
            errorsAndInfos.Errors.Add(Properties.Resources.MissingRootNamespace);
            return null;
        }

        XElement outputPathElement = projectDocument.XPathSelectElements("./" + namespaceSelector + "Project/" + namespaceSelector + "PropertyGroup/" + namespaceSelector + "OutputPath", NamespaceManager).SingleOrDefault(ParentIsReleasePropertyGroup);
        string outputPath = outputPathElement?.Value ?? @"bin\Release\";

        XElement targetFrameworkElement = projectDocument.XPathSelectElements("./" + namespaceSelector + "Project/" + namespaceSelector + "PropertyGroup/" + namespaceSelector + "TargetFrameworkVersion", NamespaceManager).FirstOrDefault()
                                          ?? projectDocument.XPathSelectElements("./" + namespaceSelector + "Project/" + namespaceSelector + "PropertyGroup/" + namespaceSelector + "TargetFramework", NamespaceManager).FirstOrDefault();
        if (targetFrameworkElement == null) {
            errorsAndInfos.Errors.Add(Properties.Resources.MissingTargetFramework);
            return null;
        }

        var filesElement = new XElement(NugetNamespace + @"files");
        string topLevelNamespace = rootNamespaceElement.Value;
        if (!topLevelNamespace.Contains('.')) {
            errorsAndInfos.Errors.Add(string.Format(Properties.Resources.TopLevelNamespaceLacksADot, topLevelNamespace));
            return null;
        }

        topLevelNamespace = topLevelNamespace.Substring(0, topLevelNamespace.IndexOf('.'));
        foreach (XElement fileElement in new[] { @"dll", @"pdb" }.Select(extension
                                                                             => new XElement(NugetNamespace + @"file",
                                                                                             new XAttribute(@"src", outputPath + topLevelNamespace + ".*." + extension),
                                                                                             new XAttribute(@"exclude", string.Join(";", outputPath + @"*.Test*.*", outputPath + @"*.exe", outputPath + @"ref\*.*")),
                                                                                             new XAttribute(@"target", @"lib\net" + TargetFrameworkElementToLibNetSuffix(targetFrameworkElement))))) {
            filesElement.Add(fileElement);
        }

        filesElement.Add(new XElement(NugetNamespace + @"file",
            new XAttribute(@"src", outputPath + "packageicon.png"),
            new XAttribute(@"target", "")));

        var foldersToPack = projectDocument.XPathSelectElements("./" + namespaceSelector + "Project/" + namespaceSelector + "ItemGroup/" + namespaceSelector + "Content", NamespaceManager)
            .Where(IncludesFileToPack).Select(IncludeAttributeValue).Select(f => f.Substring(0, f.LastIndexOf('\\'))).Distinct().ToList();
        foreach (string folderToPack in foldersToPack) {
            string target = folderToPack;
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
        XAttribute attribute = contentElement.Attributes().FirstOrDefault(a => a.Name == "Include");
        return new[] { @"build\", @"lib\", @"runtimes\" }.Any(folder => attribute?.Value.StartsWith(folder) == true) ? attribute?.Value : null;
    }

    private static bool IncludesFileToPack(XElement contentElement) {
        return !string.IsNullOrWhiteSpace(IncludeAttributeValue(contentElement));
    }

    private static string TargetFrameworkElementToLibNetSuffix(XElement targetFrameworkElement) {
        return TargetFrameworkToLibNetSuffix(targetFrameworkElement.Value);
    }

    private static string TargetFrameworkToLibNetSuffix(string targetFramework) {
        string libNetSuffix = targetFramework.StartsWith("v")
            ? targetFramework.Replace("v", "").Replace(".", "")
            : targetFramework.StartsWith("net")
                ? targetFramework.Substring(3)
                : targetFramework;
        if (libNetSuffix.Contains("-")) {
            libNetSuffix = libNetSuffix.Substring(0, libNetSuffix.IndexOf("-", StringComparison.InvariantCulture));
        }
        return libNetSuffix;
    }

    public async Task CreateNuSpecFileIfRequiredOrPresentAsync(bool required, string solutionFileFullName, string checkedOutBranch, IList<string> tags, IErrorsAndInfos errorsAndInfos) {
        string nuSpecFile = solutionFileFullName
            .Replace(".slnx", ".nuspec");
        if (!required && !File.Exists(nuSpecFile)) { return; }

        XDocument document = await CreateNuSpecAsync(solutionFileFullName, checkedOutBranch, tags, errorsAndInfos);
        if (errorsAndInfos.Errors.Any()) { return; }

        string tempFileName = Path.GetTempPath() + @"AspenlaubTemp\temp.nuspec";
        document.Save(tempFileName);
        if (File.Exists(nuSpecFile) && await File.ReadAllTextAsync(nuSpecFile) == await File.ReadAllTextAsync(tempFileName)) { return; }

        File.Copy(tempFileName, nuSpecFile, true);
        errorsAndInfos.Infos.Add(string.Format(Properties.Resources.NuSpecFileUpdated, nuSpecFile));
    }
}