using System.Collections.Generic;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces {
    public interface IPackageConfigsScanner {
        IDictionary<string, string> DependencyIdsAndVersions(string projectFolder, bool includeTest, IErrorsAndInfos errorsAndInfos);
        IDictionary<string, string> DependencyIdsAndVersions(string projectFolder, bool includeTest, bool topFolderOnly, IErrorsAndInfos errorsAndInfos);
    }
}