using System.Collections.Generic;
using System.Xml.Serialization;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities {
    [XmlRoot("PackagesReferencedWithoutVersion")]
    public class PackagesReferencedWithoutVersion : List<PackageReferencedWithoutVersion>, ISecretResult<PackagesReferencedWithoutVersion> {
        public PackagesReferencedWithoutVersion Clone() {
            var clone = new PackagesReferencedWithoutVersion();
            clone.AddRange(this);
            return clone;
        }
    }
}
