using System;
using System.Collections.Generic;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces {
    public interface IPushedHeadTipShaRepository {
        List<string> Get(string nugetFeedId, IErrorsAndInfos errorsAndInfos);
        void Remove(string nugetFeedId, string headTipSha, IErrorsAndInfos errorsAndInfos);
        void Add(string nugetFeedId, string headTipSha, IErrorsAndInfos errorsAndInfos);
        void Add(string nugetFeedId, string headTipSha, string packageId, string packageVersion, IErrorsAndInfos errorsAndInfos);
        DateTime AddedAt(string nugetFeedId, string headTipSha, IErrorsAndInfos errorsAndInfos);
        string PackageVersion(string nugetFeedId, string headTipSha, IErrorsAndInfos errorsAndInfos);
    }
}
