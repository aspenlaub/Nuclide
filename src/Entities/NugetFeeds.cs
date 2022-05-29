using System.Collections.Generic;
using System.Xml.Serialization;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;

[XmlRoot("NugetFeeds")]
public class NugetFeeds : List<NugetFeed>, ISecretResult<NugetFeeds> {
    public NugetFeeds Clone() {
        var clone = new NugetFeeds();
        clone.AddRange(this);
        return clone;
    }
}