using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;

public class SecretBranchesWithPackages : ISecret<BranchesWithPackages> {
    private BranchesWithPackages _DefaultBranchesWithPackages;

    public BranchesWithPackages DefaultValue => _DefaultBranchesWithPackages ??= new BranchesWithPackages {
        new() { Branch = "master" }
    };

    public string Guid => "2BCE8AB0-485B-45DE-A8AE-6CE5E6A9D214";
}