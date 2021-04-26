﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Test {
    [TestClass]
    public class PushedHeadTipShaRepositoryTest {
        private const string TestId = "{99F6A5C5-9FB9-4E86-AE67-EB4CA91FD710}", JsonTestId = "{AE41FC25-3F81-419F-AC4E-376C953FB83A}";
        private const string PackageId = nameof(PushedHeadTipShaRepositoryTest), PackageVersion = "24.7.70";
        private static IContainer vContainer;

        private IPushedHeadTipShaRepository vSut;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context) {
            vContainer = new ContainerBuilder().UseNuclideProtchGittyAndPegh(new DummyCsArgumentPrompter()).Build();
        }

        [TestInitialize]
        public void Initialize() {
            vSut = vContainer.Resolve<IPushedHeadTipShaRepository>();
            var errorsAndInfos = new ErrorsAndInfos();

            vSut.RemoveAsync(NugetFeed.AspenlaubLocalFeed, TestId, errorsAndInfos).Wait();
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            vSut.RemoveAsync(NugetFeed.AspenlaubLocalFeed, JsonTestId, errorsAndInfos).Wait();
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            var headTipShas = vSut.GetAsync(NugetFeed.AspenlaubLocalFeed, errorsAndInfos).Result;
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            Assert.IsNotNull(headTipShas);
            Assert.IsFalse(headTipShas.Any(s => s == TestId || s == JsonTestId));
        }

        [TestMethod]
        public async Task CanGetRemoveAndAddPushedHeadTipShas() {
            var errorsAndInfos = new ErrorsAndInfos();
            var timeStamp = DateTime.Now.AddSeconds(-10);

            await vSut.AddAsync(NugetFeed.AspenlaubLocalFeed, TestId, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            var headTipShas = await vSut.GetAsync(NugetFeed.AspenlaubLocalFeed, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            Assert.IsTrue(headTipShas.Any(s => s == TestId));

            var addedAt = await vSut.AddedAtAsync(NugetFeed.AspenlaubLocalFeed, TestId, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            var now = DateTime.Now.AddSeconds(10);
            Assert.IsTrue(addedAt >= timeStamp && addedAt <= now, $"Time stamp {addedAt.ToLongTimeString()} should be between {timeStamp.ToLongTimeString()} and {now.ToLongTimeString()}");

            await vSut.RemoveAsync(NugetFeed.AspenlaubLocalFeed, TestId, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            headTipShas = await vSut.GetAsync(NugetFeed.AspenlaubLocalFeed, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            Assert.IsFalse(headTipShas.Any(s => s == TestId));

        }

        [TestMethod]
        public async Task CanGetRemoveAndAddPushedHeadTipShasUsingJson() {
            var errorsAndInfos = new ErrorsAndInfos();

            await vSut.AddAsync(NugetFeed.AspenlaubLocalFeed, JsonTestId, PackageId, PackageVersion, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            var headTipShas = await vSut.GetAsync(NugetFeed.AspenlaubLocalFeed, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            Assert.IsTrue(headTipShas.Any(s => s == JsonTestId));

            var packageVersion = await vSut.PackageVersionAsync(NugetFeed.AspenlaubLocalFeed, JsonTestId, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            Assert.AreEqual(PackageVersion, packageVersion);

            await vSut.RemoveAsync(NugetFeed.AspenlaubLocalFeed, JsonTestId, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            headTipShas = await vSut.GetAsync(NugetFeed.AspenlaubLocalFeed, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            Assert.IsFalse(headTipShas.Any(s => s == JsonTestId));
        }
    }
}
