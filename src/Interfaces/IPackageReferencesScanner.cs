using System.Collections.Generic;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;

public interface IPackageReferencesScanner {
    Task<IDictionary<string, string>> DependencyIdsAndVersionsAsync(string projectFolder, bool includeTest, IErrorsAndInfos errorsAndInfos);
    Task<IDictionary<string, string>> DependencyIdsAndVersionsAsync(string projectFolder, bool includeTest, bool topFolderOnly, IErrorsAndInfos errorsAndInfos);
}