using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Components;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using BranchesWithPackages = Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities.BranchesWithPackages;
using SecretBranchesWithPackages = Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities.SecretBranchesWithPackages;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Components;

public class BranchesWithPackagesRepository : IBranchesWithPackagesRepository {
    private readonly BranchesWithPackages _BranchWithPackages = new();
    private readonly ISecretRepository _SecretRepository;

    public BranchesWithPackagesRepository(ISecretRepository secretRepository) {
        _SecretRepository = secretRepository;
    }

    public async Task<IList<string>> GetBranchIdsAsync(IErrorsAndInfos errorsAndInfos) {
        if (_BranchWithPackages.Any()) {
            return _BranchWithPackages.Select(b => b.Branch).ToList();
        }

        var secret = new SecretBranchesWithPackages();
        _BranchWithPackages.AddRange(await _SecretRepository.GetAsync(secret, errorsAndInfos));
        return _BranchWithPackages.Select(b => b.Branch).ToList();
    }

    public async Task<IList<string>> GetValidFoldersAsync(IErrorsAndInfos errorsAndInfos) {
        var branchIds = await GetBranchIdsAsync(errorsAndInfos);
        return branchIds.Select(LogicalFolderToWorkWith).ToList();
    }

    public string LogicalFolderToWorkWith(string branch) {
        return "GitHub" + PackageInfix(branch, true);
    }

    public string PackageInfix(string branch, bool withDash) {
        return MasterMaind.IsMainOrMaster(branch)
            ? ""
            : (withDash ? "-" : "")
                + string.Join("", branch.Split("-").Select(p => p.Substring(0, 1).ToUpper() + p.Substring(1)));
    }
}