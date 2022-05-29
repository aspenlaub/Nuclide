using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;

public interface IPushedHeadTipShaRepository {
    Task<List<string>> GetAsync(string nugetFeedId, IErrorsAndInfos errorsAndInfos);
    Task RemoveAsync(string nugetFeedId, string headTipSha, IErrorsAndInfos errorsAndInfos);
    Task AddAsync(string nugetFeedId, string headTipSha, IErrorsAndInfos errorsAndInfos);
    Task AddAsync(string nugetFeedId, string headTipSha, string packageId, string packageVersion, IErrorsAndInfos errorsAndInfos);
    Task<DateTime> AddedAtAsync(string nugetFeedId, string headTipSha, IErrorsAndInfos errorsAndInfos);
    Task<string> PackageVersionAsync(string nugetFeedId, string headTipSha, IErrorsAndInfos errorsAndInfos);
}