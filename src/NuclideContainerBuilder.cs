using Aspenlaub.Net.GitHub.CSharp.Gitty;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Components;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Protch;
using Autofac;
using Microsoft.Extensions.DependencyInjection;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide;

public static class NuclideContainerBuilder {
    public static ContainerBuilder UseNuclideProtchGittyAndPegh(this ContainerBuilder builder, string applicationName, ICsArgumentPrompter csArgumentPrompter) {
        builder.UseGittyAndPegh(applicationName, csArgumentPrompter).UseProtch();
        builder.RegisterType<DependencyTreeBuilder>().As<IDependencyTreeBuilder>();
        builder.RegisterType<NugetConfigReader>().As<INugetConfigReader>();
        builder.RegisterType<NugetFeedLister>().As<INugetFeedLister>();
        builder.RegisterType<NugetPackageRestorer>().As<INugetPackageRestorer>();
        builder.RegisterType<NuSpecCreator>().As<INuSpecCreator>();
        builder.RegisterType<ObsoletePackageFinder>().As<IObsoletePackageFinder>();
        builder.RegisterType<PackageReferencesScanner>().As<IPackageReferencesScanner>();
        builder.RegisterType<PinnedAddInVersionChecker>().As<IPinnedAddInVersionChecker>();
        builder.RegisterType<PushedHeadTipShaRepository>().As<IPushedHeadTipShaRepository>();
        return builder;
    }
    // ReSharper disable once UnusedMember.Global
    public static IServiceCollection UseNuclideProtchGittyAndPegh(this IServiceCollection services, string applicationName, ICsArgumentPrompter csArgumentPrompter) {
        services.UseGittyAndPegh(applicationName, csArgumentPrompter).UseProtch();
        services.AddTransient<IDependencyTreeBuilder, DependencyTreeBuilder>();
        services.AddTransient<INugetConfigReader, NugetConfigReader>();
        services.AddTransient<INugetFeedLister, NugetFeedLister>();
        services.AddTransient<INugetPackageRestorer, NugetPackageRestorer>();
        services.AddTransient<INuSpecCreator, NuSpecCreator>();
        services.AddTransient<IObsoletePackageFinder, ObsoletePackageFinder>();
        services.AddTransient<IPackageReferencesScanner, PackageReferencesScanner>();
        services.AddTransient<IPinnedAddInVersionChecker, PinnedAddInVersionChecker>();
        services.AddTransient<IPushedHeadTipShaRepository, PushedHeadTipShaRepository>();
        return services;
    }
}