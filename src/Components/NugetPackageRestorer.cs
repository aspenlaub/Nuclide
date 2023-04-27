using Aspenlaub.Net.GitHub.CSharp.Gitty.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Components;

public class NugetPackageRestorer : INugetPackageRestorer {
    private readonly IProcessRunner _ProcessRunner;

    public NugetPackageRestorer(IProcessRunner processRunner) {
        _ProcessRunner = processRunner;
    }

    public void RestoreNugetPackages(string solutionFileFullName, IErrorsAndInfos errorsAndInfos) {
        var directoryName = solutionFileFullName.Substring(0, solutionFileFullName.LastIndexOf('\\'));

        _ProcessRunner.RunProcess("nuget.exe", "restore " + solutionFileFullName, new Folder(directoryName), errorsAndInfos);
    }
}