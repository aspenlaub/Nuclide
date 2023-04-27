﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Protch;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Components;

public class PackageReferencesScanner : IPackageReferencesScanner {
    private readonly ISecretRepository _SecretRepository;

    public PackageReferencesScanner(ISecretRepository secretRepository) {
        _SecretRepository = secretRepository;
    }

    public async Task<IDictionary<string, string>> DependencyIdsAndVersionsAsync(string projectFolder, bool includeTest, IErrorsAndInfos errorsAndInfos) {
        return await DependencyIdsAndVersionsAsync(projectFolder, includeTest, false, errorsAndInfos);
    }

    public async Task<IDictionary<string, string>> DependencyIdsAndVersionsAsync(string projectFolder, bool includeTest, bool topFolderOnly, IErrorsAndInfos errorsAndInfos) {
        var dependencyIdsAndVersions = new Dictionary<string, string>();
        var searchOption = topFolderOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;

        var secret = new SecretPackagesReferencedWithoutVersion();
        var packagesReferencedWithoutVersion = await _SecretRepository.GetAsync(secret, errorsAndInfos);
        if (errorsAndInfos.AnyErrors()) { return dependencyIdsAndVersions; }

        var namespaceManager = new XmlNamespaceManager(new NameTable());
        namespaceManager.AddNamespace("cp", XmlNamespaces.CsProjNamespaceUri);
        foreach (var fileName in Directory.GetFiles(projectFolder, "*.csproj", searchOption).Where(f => includeTest || !f.Contains(@"Test."))) {
            XDocument document;
            string namespaceSelector;
            try {
                document = XDocument.Load(fileName);
            } catch {
                continue;
            }
            try {
                var targetFrameworkElement = document.XPathSelectElements("./Project/PropertyGroup/TargetFramework", namespaceManager).FirstOrDefault();
                namespaceSelector = targetFrameworkElement != null ? "" : "cp:";
            } catch {
                continue;
            }


            foreach (var element in document.XPathSelectElements("/" + namespaceSelector + "Project/" + namespaceSelector + "ItemGroup/" + namespaceSelector + "PackageReference", namespaceManager)) {
                var id = element.Attribute("Include")?.Value;
                if (string.IsNullOrEmpty(id)) {
                    errorsAndInfos.Errors.Add(string.Format(Properties.Resources.PackageWithoutId, fileName));
                    continue;
                }

                var version = element.Attribute("Version")?.Value;
                if (string.IsNullOrEmpty(version)) {
                    version = element.XPathSelectElement("./" + namespaceSelector + "Version", namespaceManager)?.Value;
                }
                if (packagesReferencedWithoutVersion.Any(p => p.Id == id)) {
                    if (!string.IsNullOrEmpty(version) && !errorsAndInfos.Errors.Any()) {
                        errorsAndInfos.Errors.Add(string.Format(Properties.Resources.PackageWithVersion, fileName, id));
                        continue;
                    }

                    version = "";
                } else if (string.IsNullOrEmpty(version) && !errorsAndInfos.Errors.Any()) {
                    errorsAndInfos.Errors.Add(string.Format(Properties.Resources.PackageWithoutVersion, fileName, id));
                    continue;
                }

                if (dependencyIdsAndVersions.ContainsKey(id) && dependencyIdsAndVersions[id] == version) { continue; }

                if (dependencyIdsAndVersions.ContainsKey(id)) {
                    errorsAndInfos.Errors.Add(string.Format(Properties.Resources.PackageVersionClashDueToFile, fileName, id, version, dependencyIdsAndVersions[id]));
                    continue;
                }

                dependencyIdsAndVersions[id] = version;
            }
        }

        return dependencyIdsAndVersions;
    }
}