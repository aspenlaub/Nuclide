using Aspenlaub.Net.GitHub.CSharp.Gitty;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Protch;
using Autofac;
using Microsoft.Extensions.DependencyInjection;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide {
    public static class NuclideContainerBuilder {
        public static ContainerBuilder UseNuclideProtchGittyAndPegh(this ContainerBuilder builder, ICsArgumentPrompter csArgumentPrompter) {
            builder.UseGittyAndPegh(csArgumentPrompter).UseProtch();
            builder.RegisterType<DependencyTreeBuilder>().As<IDependencyTreeBuilder>();
            builder.RegisterType<NugetConfigReader>().As<INugetConfigReader>();
            builder.RegisterType<NugetFeedLister>().As<INugetFeedLister>();
            builder.RegisterType<NugetPackageInstaller>().As<INugetPackageInstaller>();
            builder.RegisterType<NugetPackageRestorer>().As<INugetPackageRestorer>();
            builder.RegisterType<NuSpecCreator>().As<INuSpecCreator>();
            builder.RegisterType<ObsoletePackageFinder>().As<IObsoletePackageFinder>();
            builder.RegisterType<PackageConfigsScanner>().As<IPackageConfigsScanner>();
            builder.RegisterType<PinnedAddInVersionChecker>().As<IPinnedAddInVersionChecker>();
            builder.RegisterType<PushedHeadTipShaRepository>().As<IPushedHeadTipShaRepository>();
            return builder;
        }
        // ReSharper disable once UnusedMember.Global
        public static IServiceCollection UseNuclideProtchGittyAndPegh(this IServiceCollection services, ICsArgumentPrompter csArgumentPrompter) {
            services.UseGittyAndPegh(csArgumentPrompter).UseProtch();
            services.AddTransient<IDependencyTreeBuilder, DependencyTreeBuilder>();
            services.AddTransient<INugetConfigReader, NugetConfigReader>();
            services.AddTransient<INugetFeedLister, NugetFeedLister>();
            services.AddTransient<INugetPackageInstaller, NugetPackageInstaller>();
            services.AddTransient<INugetPackageRestorer, NugetPackageRestorer>();
            services.AddTransient<INuSpecCreator, NuSpecCreator>();
            services.AddTransient<IObsoletePackageFinder, ObsoletePackageFinder>();
            services.AddTransient<IPackageConfigsScanner, PackageConfigsScanner>();
            services.AddTransient<IPinnedAddInVersionChecker, PinnedAddInVersionChecker>();
            services.AddTransient<IPushedHeadTipShaRepository, PushedHeadTipShaRepository>();
            return services;
        }
    }
}
