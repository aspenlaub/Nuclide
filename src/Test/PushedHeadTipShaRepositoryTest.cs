using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Test;

[TestClass]
public class PushedHeadTipShaRepositoryTest {
    private const string _testId = "{99F6A5C5-9FB9-4E86-AE67-EB4CA91FD710}", _jsonTestId = "{AE41FC25-3F81-419F-AC4E-376C953FB83A}";
    private const string _packageId = nameof(PushedHeadTipShaRepositoryTest), _packageVersion = "24.7.70";
    private static IContainer _container;

    private IPushedHeadTipShaRepository _Sut;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context) {
        _container = new ContainerBuilder().UseNuclideProtchGittyAndPegh("Nuclide").Build();
    }

    [TestInitialize]
    public void Initialize() {
        _Sut = _container.Resolve<IPushedHeadTipShaRepository>();
        var errorsAndInfos = new ErrorsAndInfos();

        _Sut.RemoveAsync(NugetFeed.AspenlaubLocalFeed, _testId, errorsAndInfos).Wait();
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        _Sut.RemoveAsync(NugetFeed.AspenlaubLocalFeed, _jsonTestId, errorsAndInfos).Wait();
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        List<string> headTipShas = _Sut.GetAsync(NugetFeed.AspenlaubLocalFeed, errorsAndInfos).Result;
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.IsNotNull(headTipShas);
        Assert.IsFalse(headTipShas.Any(s => s == _testId || s == _jsonTestId));
    }

    [TestMethod]
    public async Task CanGetRemoveAndAddPushedHeadTipShas() {
        var errorsAndInfos = new ErrorsAndInfos();
        DateTime timeStamp = DateTime.Now.AddSeconds(-10);

        await _Sut.AddAsync(NugetFeed.AspenlaubLocalFeed, _testId, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        List<string> headTipShas = await _Sut.GetAsync(NugetFeed.AspenlaubLocalFeed, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.IsTrue(headTipShas.Any(s => s == _testId));

        DateTime addedAt = await _Sut.AddedAtAsync(NugetFeed.AspenlaubLocalFeed, _testId, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        DateTime now = DateTime.Now.AddSeconds(10);
        Assert.IsTrue(addedAt >= timeStamp && addedAt <= now, $"Time stamp {addedAt.ToLongTimeString()} should be between {timeStamp.ToLongTimeString()} and {now.ToLongTimeString()}");

        await _Sut.RemoveAsync(NugetFeed.AspenlaubLocalFeed, _testId, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        headTipShas = await _Sut.GetAsync(NugetFeed.AspenlaubLocalFeed, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.IsFalse(headTipShas.Any(s => s == _testId));

    }

    [TestMethod]
    public async Task CanGetRemoveAndAddPushedHeadTipShasUsingJson() {
        var errorsAndInfos = new ErrorsAndInfos();

        await _Sut.AddAsync(NugetFeed.AspenlaubLocalFeed, _jsonTestId, _packageId, _packageVersion, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        List<string> headTipShas = await _Sut.GetAsync(NugetFeed.AspenlaubLocalFeed, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.IsTrue(headTipShas.Any(s => s == _jsonTestId));

        string packageVersion = await _Sut.PackageVersionAsync(NugetFeed.AspenlaubLocalFeed, _jsonTestId, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.AreEqual(_packageVersion, packageVersion);

        await _Sut.RemoveAsync(NugetFeed.AspenlaubLocalFeed, _jsonTestId, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        headTipShas = await _Sut.GetAsync(NugetFeed.AspenlaubLocalFeed, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.IsFalse(headTipShas.Any(s => s == _jsonTestId));
    }
}