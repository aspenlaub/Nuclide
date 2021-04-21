using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Newtonsoft.Json;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Components {
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

        public List<string> Get(string nugetFeedId, IErrorsAndInfos errorsAndInfos) {
            var folder = RepositoryFolder(errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return new List<string>(); }

            var resultFiles = new List<string>();
            resultFiles.AddRange(Directory.GetFiles(folder.FullName, "*.txt", SearchOption.TopDirectoryOnly).Select(f => ExtractHeadTipShaFromFileName(f)));
            var subFolder = folder.SubFolder(nugetFeedId);
            subFolder.CreateIfNecessary();
            resultFiles.AddRange(Directory.GetFiles(subFolder.FullName, "*.json", SearchOption.TopDirectoryOnly).Select(f => ExtractHeadTipShaFromFileName(f)));
            if (!resultFiles.Any()) {
                errorsAndInfos.Errors.Add(string.Format(Properties.Resources.NoPushedHeadTipShasFound, folder.FullName));
            }

            return resultFiles;
        }

        private string ExtractHeadTipShaFromFileName(string fileName) {
            return fileName.Substring(fileName.LastIndexOf('\\') + 1).Replace(".txt", "").Replace(".json", "");
        }

        public void Remove(string nugetFeedId, string headTipSha, IErrorsAndInfos errorsAndInfos) {
            var fileName = TextFileName(headTipSha, errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return; }

            if (File.Exists(fileName)) {
                File.Delete(fileName);
            }

            fileName = JsonFileName(nugetFeedId, headTipSha, errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return; }

            if (File.Exists(fileName)) {
                File.Delete(fileName);
            }
        }


        public void Add(string nugetFeedId, string headTipSha, IErrorsAndInfos errorsAndInfos) {
            var fileName = TextFileName(headTipSha, errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return; }

            if (File.Exists(fileName)) { return; }

            File.WriteAllText(fileName, headTipSha);
        }

        public void Add(string nugetFeedId, string headTipSha, string packageId, string packageVersion, IErrorsAndInfos errorsAndInfos) {
            var fileName = JsonFileName(nugetFeedId, headTipSha, errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return; }

            if (File.Exists(fileName)) { return; }

            var dictionary = new Dictionary<string, string> {
                {nameof(headTipSha), headTipSha}, {nameof(packageId), packageId}, {nameof(packageVersion), packageVersion}, {nameof(nugetFeedId), nugetFeedId}
            };

            var json = JsonConvert.SerializeObject(dictionary);

            File.WriteAllText(fileName, json);
        }

        public DateTime AddedAt(string nugetFeedId, string headTipSha, IErrorsAndInfos errorsAndInfos) {
            var folder = RepositoryFolder(errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return DateTime.MaxValue;}

            var fileName = folder.FullName + '\\' + headTipSha +  ".txt";
            return !File.Exists(fileName) ? DateTime.MaxValue : File.GetLastWriteTime(fileName);
        }

        private string TextFileName(string headTipSha, IErrorsAndInfos errorsAndInfos) {
            var folder = RepositoryFolder(errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return ""; }

            return folder.FullName + '\\' + headTipSha + ".txt";
        }

        private string JsonFileName(string nugetFeedId, string headTipSha, IErrorsAndInfos errorsAndInfos) {
            var folder = RepositoryFolder(errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return ""; }

            folder = folder.SubFolder(nugetFeedId);
            folder.CreateIfNecessary();
            return folder.FullName + '\\' + headTipSha + ".json";
        }

        public string PackageVersion(string nugetFeedId, string headTipSha, IErrorsAndInfos errorsAndInfos) {
            var fileName = JsonFileName(nugetFeedId, headTipSha, errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return ""; }

            if (!File.Exists(fileName)) {
                errorsAndInfos.Errors.Add(string.Format(Properties.Resources.FileNotFound, fileName));
                return "";
            }

            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(fileName));
            if (dictionary?.ContainsKey("packageVersion") == true) {
                return dictionary["packageVersion"];
            }

            errorsAndInfos.Errors.Add(string.Format(Properties.Resources.CouldNotDeserializeFromFile, fileName));
            return "";
        }
    }
}
