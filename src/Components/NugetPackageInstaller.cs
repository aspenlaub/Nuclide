using System.Collections.Generic;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Components {
    public class NugetPackageInstaller : INugetPackageInstaller {
        private readonly IProcessRunner ProcessRunner;

        public NugetPackageInstaller(IProcessRunner processRunner) {
            ProcessRunner = processRunner;
        }

        public void InstallNugetPackage(IFolder packagesConfigFolder, string packageId, string version, bool excludeVersion, IErrorsAndInfos errorsAndInfos) {
            var arguments = new List<string> { "install", packageId };
            if (version != "") {
                arguments.Add("-Version \"" + version + "\"");
            }
            if (excludeVersion) {
                arguments.Add("-ExcludeVersion");
            }
            ProcessRunner.RunProcess("nuget.exe", string.Join(" ", arguments), new Folder(packagesConfigFolder.FullName), errorsAndInfos);
        }
    }
}
