using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;

public interface INugetPackageInstaller {
    /// <summary>
    /// Install nuget package
    /// </summary>
    /// <param name="packagesConfigFolder">The folder that holds packages.config</param>
    /// <param name="packageId"></param>
    /// <param name="version"></param>
    /// <param name="excludeVersion"></param>
    /// <param name="errorsAndInfos"></param>
    void InstallNugetPackage(IFolder packagesConfigFolder, string packageId, string version, bool excludeVersion, IErrorsAndInfos errorsAndInfos);
}