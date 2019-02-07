using System.Collections.Generic;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces {
    public interface IPinnedAddInVersionChecker {
        void CheckPinnedAddInVersions(IFolder solutionFolder, IErrorsAndInfos errorsAndInfos);
        void CheckPinnedAddInVersions(IList<string> cakeScript, IFolder solutionFolder, IErrorsAndInfos errorsAndInfos);
    }
}
