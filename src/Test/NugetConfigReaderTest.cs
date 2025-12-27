using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Gitty;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IContainer = Autofac.IContainer;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Test;

[TestClass]
public class NugetConfigReaderTest {
    protected string NugetConfigFileName = new Folder(Path.GetTempPath()).SubFolder("AspenlaubTemp").FullName + '\\' + nameof(NugetConfigReaderTest) + ".config";
    protected string Source = "hypothetical" + "source.net";
    protected string ApiKey = "thisisnotanapikey";
    private static IContainer _container;

    [ClassInitialize]
    public static void ClassInitialize(TestContext testContext) {
        _container = new ContainerBuilder().UseGittyTestUtilities().UseNuclideProtchGittyAndPegh("Nuclide", new DummyCsArgumentPrompter()).Build();
    }

    [TestCleanup]
    public void Cleanup() {
        if (!File.Exists(NugetConfigFileName)) { return; }

        File.Delete(NugetConfigFileName);
    }

    [TestMethod]
    public void CanGetApiKey() {
        CreateNugetConfig();
        INugetConfigReader sut = _container.Resolve<INugetConfigReader>();
        var errorsAndInfos = new ErrorsAndInfos();
        Assert.AreEqual(ApiKey, sut.GetApiKey(NugetConfigFileName, Source, errorsAndInfos));
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        Assert.AreEqual("", sut.GetApiKey(NugetConfigFileName, Source + "t", errorsAndInfos));
        Assert.IsTrue(errorsAndInfos.Errors.Any(e => e.Contains("No apikey was found for this source")));
    }

    protected void CreateNugetConfig() {
        var contents = new List<string> {
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
            "<configuration>",
            "<packageSources>",
            "<add key=\"" + Source + "\" value=\"https://www.hypothericalsource.net/nuget\" />",
            "</packageSources>",
            "<apikeys>",
            "<add key=\"https://www.hypothericalsource.net/nuget\" value=\"" + ApiKey + "\" />",
            "</apikeys>",
            "</configuration>"
        };

        File.WriteAllLines(NugetConfigFileName, contents);
    }
}