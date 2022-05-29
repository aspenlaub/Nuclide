using System;
using System.Collections.Generic;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;

public interface IDependencyNode {
    string Id { get; set; }
    string Version { get; set; }
    List<IDependencyNode> FindNodes(Func<IDependencyNode, bool> criteriaFunc);
    // ReSharper disable once UnusedMember.Global
    string ToString();
}