namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces {
    public interface IDependencyTreeBuilder {
        IDependencyNode BuildDependencyTree(string packagesFolder);
    }
}
