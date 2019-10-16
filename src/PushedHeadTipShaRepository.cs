using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide {
    public class PushedHeadTipShaRepository : IPushedHeadTipShaRepository {
        private readonly IFolderResolver vFolderResolver;

        public PushedHeadTipShaRepository(IFolderResolver folderResolver) {
            vFolderResolver = folderResolver;
        }

        private IFolder RepositoryFolder(IErrorsAndInfos errorsAndInfos) {
            var folder = vFolderResolver.Resolve(@"$(CSharp)\GitHub\PushedHeadTipShas", errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return null; }
            folder.CreateIfNecessary();
            return folder;
        }

        public List<string> Get(IErrorsAndInfos errorsAndInfos) {
            var folder = RepositoryFolder(errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return new List<string>(); }

            var resultFiles = Directory.GetFiles(folder.FullName, "*.txt", SearchOption.TopDirectoryOnly).Select(f => ExtractHeadTipShaFromFileName(f)).ToList();
            if (!resultFiles.Any()) {
                errorsAndInfos.Errors.Add(string.Format(Properties.Resources.NoPushedHeadTipShasFound, folder.FullName));
            }

            return resultFiles;
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
            if (File.Exists(fileName)) { return; }

            File.WriteAllText(fileName, headTipSha);
        }

        public DateTime AddedAt(string headTipSha, IErrorsAndInfos errorsAndInfos) {
            var folder = RepositoryFolder(errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return DateTime.MaxValue;}

            var fileName = folder.FullName + '\\' + headTipSha +  ".txt";
            return !File.Exists(fileName) ? DateTime.MaxValue : File.GetLastWriteTime(fileName);
        }
    }
}
