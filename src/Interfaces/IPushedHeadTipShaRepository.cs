using System;
using System.Collections.Generic;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces {
    public interface IPushedHeadTipShaRepository {
        List<string> Get(IErrorsAndInfos errorsAndInfos);
        void Remove(string headTipSha, IErrorsAndInfos errorsAndInfos);
        void Add(string headTipSha, IErrorsAndInfos errorsAndInfos);
        DateTime AddedAt(string headTipSha, IErrorsAndInfos errorsAndInfos);
    }
}
