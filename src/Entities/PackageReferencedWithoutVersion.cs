using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;

public class PackageReferencedWithoutVersion : IPackageReferencedWithoutVersion {
    [Key, XmlAttribute("id")]
    public string Id { get; set; }
}