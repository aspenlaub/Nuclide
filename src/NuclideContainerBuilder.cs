﻿using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Autofac;
using Microsoft.Extensions.DependencyInjection;

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
        // ReSharper disable once UnusedMember.Global
        public static IServiceCollection UseNuclide(this IServiceCollection services) {
            services.AddTransient<IDependencyTreeBuilder, DependencyTreeBuilder>();
            services.AddTransient<INugetConfigReader, NugetConfigReader>();
            services.AddTransient<INugetFeedLister, NugetFeedLister>();
            services.AddTransient<INugetPackageInstaller, NugetPackageInstaller>();
            services.AddTransient<INugetPackageToPushFinder, NugetPackageToPushFinder>();
            services.AddTransient<INugetPackageRestorer, NugetPackageRestorer>();
            services.AddTransient<INuSpecCreator, NuSpecCreator>();
            services.AddTransient<IObsoletePackageFinder, ObsoletePackageFinder>();
            services.AddTransient<IPackageConfigsScanner, PackageConfigsScanner>();
            return services;
        }
    }
}
