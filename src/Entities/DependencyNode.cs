using System;
using System.Collections.Generic;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;

public class DependencyNode : IDependencyNode {
    public List<IDependencyNode> ChildNodes;
    public string Id { get; set; }
    public string Version { get; set; }

    public DependencyNode() {
        ChildNodes = new List<IDependencyNode>();
        Id = "";
        Version = "";
    }

    public List<IDependencyNode> FindNodes(Func<IDependencyNode, bool> criteriaFunc) {
        var nodes = criteriaFunc(this) ? new List<IDependencyNode> { this } : null;
        foreach (var childNodes in ChildNodes.Select(c => c.FindNodes(criteriaFunc)).Where(n => n != null)) {

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