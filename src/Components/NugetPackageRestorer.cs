using System.IO;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Components;

public class NugetPackageRestorer(IProcessRunner processRunner) : INugetPackageRestorer {
    public void RestoreNugetPackages(string solutionFileFullName, IErrorsAndInfos errorsAndInfos) {
        string directoryName = solutionFileFullName.Substring(0, solutionFileFullName.LastIndexOf('\\'));
        if (solutionFileFullName.EndsWith(".slnx")) {
            string oldSolutionFileFullName = solutionFileFullName.Replace(".slnx", ".sln");
            if (File.Exists(oldSolutionFileFullName)) {
                solutionFileFullName = oldSolutionFileFullName;
            }
        }
        processRunner.RunProcess("nuget.exe", "restore " + solutionFileFullName, new Folder(directoryName), errorsAndInfos);
    }
}