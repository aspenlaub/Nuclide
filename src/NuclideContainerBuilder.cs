using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Autofac;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide {
    public static class NuclideContainerBuilder {
        public static ContainerBuilder UseNuclide(this ContainerBuilder builder) {
            builder.RegisterType<DependencyTreeBuilder>().As<IDependencyTreeBuilder>();
            builder.RegisterType<NugetConfigReader>().As<INugetConfigReader>();
            builder.RegisterType<NugetFeedLister>().As<INugetFeedLister>();
            builder.RegisterType<NugetPackageInstaller>().As<INugetPackageInstaller>();
            builder.RegisterType<NugetPackageToPushFinder>().As<INugetPackageToPushFinder>();
            builder.RegisterType<NugetPackageRestorer>().As<INugetPackageRestorer>();
            builder.RegisterType<NuSpecCreator>().As<INuSpecCreator>();
            builder.RegisterType<ObsoletePackageFinder>().As<IObsoletePackageFinder>();
            builder.RegisterType<PackageConfigsScanner>().As<IPackageConfigsScanner>();
            return builder;
        }
    }
}
