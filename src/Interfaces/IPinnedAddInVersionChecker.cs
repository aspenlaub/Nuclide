using System.Collections.Generic;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;

public interface IPinnedAddInVersionChecker {
    Task CheckPinnedAddInVersionsAsync(IFolder solutionFolder, IErrorsAndInfos errorsAndInfos);
    Task CheckPinnedAddInVersionsAsync(IList<string> cakeScript, IFolder solutionFolder, IErrorsAndInfos errorsAndInfos);
}