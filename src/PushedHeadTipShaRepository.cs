using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide {
    public class PushedHeadTipShaRepository : IPushedHeadTipShaRepository {
        private readonly IComponentProvider vComponentProvider;

        public PushedHeadTipShaRepository() {
            vComponentProvider = new ComponentProvider();
        }

        private IFolder RepositoryFolder(IErrorsAndInfos errorsAndInfos) {
            var folder = vComponentProvider.FolderResolver.Resolve(@"$(CSharp)\GitHub\PushedHeadTipShas", errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return null; }
            folder.CreateIfNecessary();
            return folder;
        }

        public List<string> Get(IErrorsAndInfos errorsAndInfos) {
            var folder = RepositoryFolder(errorsAndInfos);
            return errorsAndInfos.AnyErrors() ? new List<string>() : Directory.GetFiles(folder.FullName, "*.txt", SearchOption.TopDirectoryOnly).Select(f => ExtractHeadTipShaFromFileName(f)).ToList();
        }

        private string ExtractHeadTipShaFromFileName(string fileName) {
            return fileName.Substring(fileName.LastIndexOf('\\') + 1).Replace(".txt", "");
        }

        public void Remove(string headTipSha, IErrorsAndInfos errorsAndInfos) {
            var folder = RepositoryFolder(errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return; }

            var fileName = folder.FullName + '\\' + headTipSha + ".txt";
            if (!File.Exists(fileName)) { return; }

            File.Delete(fileName);
        }

        public void Add(string headTipSha, IErrorsAndInfos errorsAndInfos) {
            var folder = RepositoryFolder(errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return; }

            var fileName = folder.FullName + '\\' + headTipSha + ".txt";
            File.WriteAllText(fileName, headTipSha);
        }
    }
}
