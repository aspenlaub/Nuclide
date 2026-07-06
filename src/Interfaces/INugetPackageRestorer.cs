using System.Threading;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Skladasu.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;

public interface INugetPackageRestorer {
    Task RestoreNugetPackagesAsync(string solutionFileFullName, IErrorsAndInfos errorsAndInfos, CancellationToken cancellationToken);
}