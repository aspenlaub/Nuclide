using System.IO;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Test;

[TestClass]
public class NugetRestorerTest {
    private static IFolder _automationTestProjectsFolder;

    [ClassInitialize]
    public static void ClassInitialize(TestContext testContext) {
        _automationTestProjectsFolder = new Folder(Path.GetTempPath()).SubFolder("AspenlaubTemp").SubFolder(nameof(NugetRestorerTest));
    }

    [TestInitialize]
    public void Initialize() {
        CleanUp();
    }

    [TestCleanup]
    public void CleanUp() {
        if (!_automationTestProjectsFolder.Exists()) { return; }

        var deleter = new FolderDeleter();
        deleter.DeleteFolder(_automationTestProjectsFolder);
    }
}