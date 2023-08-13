using System.Collections.Generic;
using System.Xml.Serialization;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;

[XmlRoot("BranchesWithPackages")]
public class BranchesWithPackages : List<BranchWithPackages>, ISecretResult<BranchesWithPackages> {
    public BranchesWithPackages Clone() {
        var clone = new BranchesWithPackages();
        clone.AddRange(this);
        return clone;
    }
}