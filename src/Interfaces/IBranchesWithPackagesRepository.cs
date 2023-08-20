using System.Collections.Generic;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;

public interface IBranchesWithPackagesRepository {
    Task<IList<string>> GetBranchIdsAsync(IErrorsAndInfos errorsAndInfos);
    Task<IList<string>> GetValidFoldersAsync(IErrorsAndInfos errorsAndInfos);
    string LogicalFolderToWorkWith(string branch);
    string PackageInfix(string branch, bool withDash);
}