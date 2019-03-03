using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Protch;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide {
    public class PackageConfigsScanner : IPackageConfigsScanner {
        public readonly IList<string> PackageIdsWithoutVersion = new List<string> {
            "Microsoft.AspNetCore.App", "Microsoft.NETCore.App", "Microsoft.AspNetCore.All"
        };

        public IDictionary<string, string> DependencyIdsAndVersions(string projectFolder, bool includeTest, IErrorsAndInfos errorsAndInfos) {
            return DependencyIdsAndVersions(projectFolder, includeTest, false, errorsAndInfos);
        }

        public IDictionary<string, string> DependencyIdsAndVersions(string projectFolder, bool includeTest, bool topFolderOnly, IErrorsAndInfos errorsAndInfos) {
            var dependencyIdsAndVersions = new Dictionary<string, string>();
            var searchOption = topFolderOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;
            foreach (var fileName in Directory.GetFiles(projectFolder, "packages.config", searchOption).Where(f => includeTest || !f.Contains(@"Test"))) {
                var document = XDocument.Load(fileName);
                foreach (var element in document.XPathSelectElements("/packages/package")) {
                    var id = element.Attribute("id")?.Value;
                    if (string.IsNullOrEmpty(id)) {
                        errorsAndInfos.Errors.Add(string.Format(Properties.Resources.PackageWithoutId, fileName));
                        continue;
                    }

                    var version = element.Attribute("version")?.Value;
                    if (PackageIdsWithoutVersion.Contains(id)) {
                        if (!string.IsNullOrEmpty(version) && !errorsAndInfos.Errors.Any()) {
                            errorsAndInfos.Errors.Add(string.Format(Properties.Resources.PackageWithVersion, fileName, id));
                            continue;
                        }

                        version = "";
                    } else if (string.IsNullOrEmpty(version) && !errorsAndInfos.Errors.Any()) {
                        errorsAndInfos.Errors.Add(string.Format(Properties.Resources.PackageWithoutVersion, fileName, id));
                        continue;
                    }

                    if (element.Attributes("developmentDependency").Any(a => a.Value.ToLower() == "true")) { continue; }

                    if (dependencyIdsAndVersions.ContainsKey(id) && dependencyIdsAndVersions[id] == version) { continue; }

                    if (dependencyIdsAndVersions.ContainsKey(id)) {
                        errorsAndInfos.Errors.Add(string.Format(Properties.Resources.PackageVersionClashDueToFile, fileName, id, version, dependencyIdsAndVersions[id]));
                        continue;
                    }

                    dependencyIdsAndVersions[id] = version;
                }
            }


            var namespaceManager = new XmlNamespaceManager(new NameTable());
            namespaceManager.AddNamespace("cp", XmlNamespaces.CsProjNamespaceUri);
            foreach (var fileName in Directory.GetFiles(projectFolder, "*.csproj", searchOption).Where(f => includeTest || !f.Contains(@"Test"))) {
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
                    if (PackageIdsWithoutVersion.Contains(id)) {
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
}
