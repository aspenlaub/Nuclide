using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;

public interface INuSpecCreator {
    // ReSharper disable once UnusedMember.Global
    Task CreateNuSpecFileIfRequiredOrPresentAsync(bool required, string solutionFileFullName, IList<string> tags, IErrorsAndInfos errorsAndInfos);

    Task<XDocument> CreateNuSpecAsync(string solutionFileFullName, IList<string> tags, IErrorsAndInfos errorsAndInfos);
}