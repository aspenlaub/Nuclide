﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        private async Task<IFolder> RepositoryFolderAsync(IErrorsAndInfos errorsAndInfos) {
            var folder = await vFolderResolver.ResolveAsync(@"$(CSharp)\GitHub\PushedHeadTipShas", errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return null; }
            folder.CreateIfNecessary();
            return folder;
        }

        public async Task<List<string>> GetAsync(string nugetFeedId, IErrorsAndInfos errorsAndInfos) {
            var folder = await RepositoryFolderAsync(errorsAndInfos);
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

        public async Task RemoveAsync(string nugetFeedId, string headTipSha, IErrorsAndInfos errorsAndInfos) {
            var fileName = await TextFileNameAsync(headTipSha, errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return; }

            if (File.Exists(fileName)) {
                File.Delete(fileName);
            }

            fileName = await JsonFileNameAsync(nugetFeedId, headTipSha, errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return; }

            if (File.Exists(fileName)) {
                File.Delete(fileName);
            }
        }


        public async Task AddAsync(string nugetFeedId, string headTipSha, IErrorsAndInfos errorsAndInfos) {
            var fileName = await TextFileNameAsync(headTipSha, errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return; }

            if (File.Exists(fileName)) { return; }

            await File.WriteAllTextAsync(fileName, headTipSha);
        }

        public async Task AddAsync(string nugetFeedId, string headTipSha, string packageId, string packageVersion, IErrorsAndInfos errorsAndInfos) {
            var fileName = await JsonFileNameAsync(nugetFeedId, headTipSha, errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return; }

            if (File.Exists(fileName)) { return; }

            var dictionary = new Dictionary<string, string> {
                {nameof(headTipSha), headTipSha}, {nameof(packageId), packageId}, {nameof(packageVersion), packageVersion}, {nameof(nugetFeedId), nugetFeedId}
            };

            var json = JsonConvert.SerializeObject(dictionary);

            await File.WriteAllTextAsync(fileName, json);
        }

        public async Task<DateTime> AddedAtAsync(string nugetFeedId, string headTipSha, IErrorsAndInfos errorsAndInfos) {
            var folder = await RepositoryFolderAsync(errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return DateTime.MaxValue;}

            var fileName = folder.FullName + '\\' + headTipSha +  ".txt";
            return !File.Exists(fileName) ? DateTime.MaxValue : File.GetLastWriteTime(fileName);
        }

        private async Task<string> TextFileNameAsync(string headTipSha, IErrorsAndInfos errorsAndInfos) {
            var folder = await RepositoryFolderAsync(errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return ""; }

            return folder.FullName + '\\' + headTipSha + ".txt";
        }

        private async Task<string> JsonFileNameAsync(string nugetFeedId, string headTipSha, IErrorsAndInfos errorsAndInfos) {
            var folder = await RepositoryFolderAsync(errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return ""; }

            folder = folder.SubFolder(nugetFeedId);
            folder.CreateIfNecessary();
            return folder.FullName + '\\' + headTipSha + ".json";
        }

        public async Task<string> PackageVersionAsync(string nugetFeedId, string headTipSha, IErrorsAndInfos errorsAndInfos) {
            var fileName = await JsonFileNameAsync(nugetFeedId, headTipSha, errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return ""; }

            if (!File.Exists(fileName)) {
                errorsAndInfos.Errors.Add(string.Format(Properties.Resources.FileNotFound, fileName));
                return "";
            }

            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(await File.ReadAllTextAsync(fileName));
            if (dictionary?.ContainsKey("packageVersion") == true) {
                return dictionary["packageVersion"];
            }

            errorsAndInfos.Errors.Add(string.Format(Properties.Resources.CouldNotDeserializeFromFile, fileName));
            return "";
        }
    }
}
