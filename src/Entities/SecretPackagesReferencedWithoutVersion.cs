using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities {
    public class SecretPackagesReferencedWithoutVersion : ISecret<PackagesReferencedWithoutVersion> {
        private PackagesReferencedWithoutVersion vDefaultValue;
        public PackagesReferencedWithoutVersion DefaultValue => vDefaultValue ?? (vDefaultValue = new PackagesReferencedWithoutVersion {
            new PackageReferencedWithoutVersion { Id = "Microsoft.AspNetCore.App" },
            new PackageReferencedWithoutVersion { Id = "Microsoft.NETCore.App" },
            new PackageReferencedWithoutVersion { Id = "Microsoft.AspNetCore.All" }
        });

        public string Guid => "425EB3C3-5312-4511-AE84-06E4AFFF8A9D";
    }
}
