using System;
using System.Collections.Generic;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;

public class DependencyNode : IDependencyNode {
    public List<IDependencyNode> ChildNodes = [];
    public string Id { get; set; } = "";
    public string Version { get; set; } = "";

    public List<IDependencyNode> FindNodes(Func<IDependencyNode, bool> criteriaFunc) {
        List<IDependencyNode> nodes = criteriaFunc(this) ? [this] : null;
        foreach (List<IDependencyNode> childNodes in ChildNodes.Select(c => c.FindNodes(criteriaFunc)).Where(n => n != null)) {

            if (nodes == null) {
                nodes = childNodes;
            } else {
                nodes.AddRange(childNodes);
            }
        }

        return nodes;
    }

    public override string ToString() {
        return Id + " " + Version;
    }
}