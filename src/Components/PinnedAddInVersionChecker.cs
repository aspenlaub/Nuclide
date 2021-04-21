using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Gitty;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Components {
    public class PinnedAddInVersionChecker : IPinnedAddInVersionChecker {
        private readonly IPackageConfigsScanner vPackageConfigsScanner;

        public PinnedAddInVersionChecker(IPackageConfigsScanner packageConfigsScanner) {
            vPackageConfigsScanner = packageConfigsScanner;
        }

        public async Task CheckPinnedAddInVersionsAsync(IFolder solutionFolder, IErrorsAndInfos errorsAndInfos) {
            var buildCakeFileName = solutionFolder.FullName + @"\" + BuildCake.Standard;
            if (!File.Exists(buildCakeFileName)) {
                errorsAndInfos.Errors.Add(string.Format(Properties.Resources.FileNotFound, buildCakeFileName));
                return;
            }

            await CheckPinnedAddInVersionsAsync(File.ReadAllLines(buildCakeFileName), solutionFolder, errorsAndInfos);
        }

        public async Task CheckPinnedAddInVersionsAsync(IList<string> cakeScript, IFolder solutionFolder, IErrorsAndInfos errorsAndInfos) {
            var dependencyIdsAndVersions = await vPackageConfigsScanner.DependencyIdsAndVersionsAsync(solutionFolder.FullName, true, errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return; }

            foreach (var dependencyIdAndVersion in dependencyIdsAndVersions) {
                CheckPinnedAddInVersion(cakeScript, errorsAndInfos, dependencyIdAndVersion);
            }
        }

        private static void CheckPinnedAddInVersion(IEnumerable<string> cakeScript, IErrorsAndInfos errorsAndInfos, KeyValuePair<string, string> dependencyIdAndVersion) {
            var lines = cakeScript.Where(l => l.Contains("#addin nuget:") && l.Contains($"package={dependencyIdAndVersion.Key}") && !l.Contains($"package={dependencyIdAndVersion.Key}.")).ToList();
            if (!lines.Any()) {
                return;
            }

            if (lines.All(l => l.Contains($"version={dependencyIdAndVersion.Value}"))) {
                return;
            }

            errorsAndInfos.Errors.Add(string.Format(Properties.Resources.PackageNotPinnedToVersion, dependencyIdAndVersion.Key, dependencyIdAndVersion.Value));
        }
    }
}
