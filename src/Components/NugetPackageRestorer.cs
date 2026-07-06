using System.Threading;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Skladasu.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Components;

public class NugetPackageRestorer(IProcessRunner processRunner) : INugetPackageRestorer {
    public async Task RestoreNugetPackagesAsync(string solutionFileFullName, IErrorsAndInfos errorsAndInfos, CancellationToken cancellationToken) {
        string directoryName = solutionFileFullName.Substring(0, solutionFileFullName.LastIndexOf('\\'));
        await processRunner.RunProcessAsync("nuget.exe", "restore " + solutionFileFullName, new Folder(directoryName), errorsAndInfos, cancellationToken);
    }
}