using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;

public interface INugetPackageRestorer {
    void RestoreNugetPackages(string solutionFileFullName, IErrorsAndInfos errorsAndInfos);
}