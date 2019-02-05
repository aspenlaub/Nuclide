using Aspenlaub.Net.GitHub.CSharp.Gitty.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide {
    public class NugetPackageRestorer : INugetPackageRestorer {
        private readonly IProcessRunner vProcessRunner;

        public NugetPackageRestorer(IProcessRunner processRunner) {
            vProcessRunner = processRunner;
        }

        public void RestoreNugetPackages(string solutionFileFullName, IErrorsAndInfos errorsAndInfos) {
            var directoryName = solutionFileFullName.Substring(0, solutionFileFullName.LastIndexOf('\\'));

            vProcessRunner.RunProcess("nuget.exe", "restore " + solutionFileFullName, directoryName, errorsAndInfos);
        }
    }
}
