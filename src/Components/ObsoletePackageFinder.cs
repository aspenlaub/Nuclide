using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Components {
    public class ObsoletePackageFinder : IObsoletePackageFinder {
        private readonly IPackageConfigsScanner vPackageConfigsScanner;

        public ObsoletePackageFinder(IPackageConfigsScanner packageConfigsScanner) {
            vPackageConfigsScanner = packageConfigsScanner;
        }

        public async Task FindObsoletePackagesAsync(string solutionFolder, IErrorsAndInfos errorsAndInfos) {
            var dependencyIdsAndVersions = await vPackageConfigsScanner.DependencyIdsAndVersionsAsync(solutionFolder, true, errorsAndInfos);
            if (!Directory.Exists(solutionFolder + @"\packages\")) { return; }

            var folders = Directory.GetDirectories(solutionFolder + @"\packages\").ToList().Where(f => !f.Contains("OctoPack") && !f.Contains("CodeAnalysis")).ToList();
            var okayFolders = new List<string>();
            foreach (var dependencyIdAndVersion in dependencyIdsAndVersions) {
                okayFolders.AddRange(folders.Where(f => f.Contains(dependencyIdAndVersion.Key) && f.Contains(dependencyIdAndVersion.Value)));
            }

            folders = folders.Where(f => !okayFolders.Contains(f)).ToList();
            if (!folders.Any()) { return; }

            foreach (var folder in folders.Select(f => new Folder(f))) {
                var deleter = new FolderDeleter();
                foreach (var file in new[] {@"dll", @"pdb", @"nupkg", @"_"}.SelectMany(extension => Directory.GetFiles(folder.FullName, $"*.{extension}", SearchOption.AllDirectories))) {
                    File.Delete(file);
                }

                if (deleter.CanDeleteFolder(folder)) {
                    deleter.DeleteFolder(folder);
                    errorsAndInfos.Infos.Add(string.Format(Properties.Resources.ObsoleteFolderDeleted, folder.FullName));
                } else {
                    errorsAndInfos.Errors.Add(string.Format(Properties.Resources.FolderIsObsolete, folder.FullName));
                }
            }
        }
    }
}
