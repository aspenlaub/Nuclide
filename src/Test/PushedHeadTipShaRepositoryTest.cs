using System;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Test {
    [TestClass]
    public class PushedHeadTipShaRepositoryTest {
        private const string TestId = "{99F6A5C5-9FB9-4E86-AE67-EB4CA91FD710}";
        private static IContainer vContainer;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context) {
            vContainer = new ContainerBuilder().UseNuclideProtchGittyAndPegh(new DummyCsArgumentPrompter()).Build();
        }

        [TestMethod]
        public void CanGetRemoveAndAddPushedHeadTipShas() {
            var sut = vContainer.Resolve<IPushedHeadTipShaRepository>();
            var errorsAndInfos = new ErrorsAndInfos();
            sut.Remove(TestId, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            var headTipShas = sut.Get(errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            Assert.IsNotNull(headTipShas);
            Assert.IsFalse(headTipShas.Any(s => s == TestId));
            var timeStamp = DateTime.Now.AddSeconds(-10);
            sut.Add(TestId, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            headTipShas = sut.Get(errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            Assert.IsTrue(headTipShas.Any(s => s == TestId));
            var addedAt = sut.AddedAt(TestId, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            var now = DateTime.Now.AddSeconds(10);
            Assert.IsTrue(addedAt >= timeStamp && addedAt <= now, $"Time stamp {addedAt.ToLongTimeString()} should be between {timeStamp.ToLongTimeString()} and {now.ToLongTimeString()}");
            sut.Remove(TestId, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            headTipShas = sut.Get(errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            Assert.IsFalse(headTipShas.Any(s => s == TestId));
        }
    }
}
