using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using NuGet.Packaging;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Components;

public class PushedHeadTipShaRepository(IFolderResolver folderResolver) : IPushedHeadTipShaRepository {
    private async Task<IFolder> RepositoryFolderAsync(IErrorsAndInfos errorsAndInfos) {
        var folder = await folderResolver.ResolveAsync(@"$(CSharp)\GitHub\PushedHeadTipShas", errorsAndInfos);
        if (errorsAndInfos.AnyErrors()) { return null; }
        folder.CreateIfNecessary();
        return folder;
    }

    public async Task<List<string>> GetAsync(string nugetFeedId, IErrorsAndInfos errorsAndInfos) {
        var repositoryFolderErrorsAndInfos = new ErrorsAndInfos();
        var folder = await RepositoryFolderAsync(repositoryFolderErrorsAndInfos);
        if (repositoryFolderErrorsAndInfos.AnyErrors()) {
            errorsAndInfos.Errors.AddRange(repositoryFolderErrorsAndInfos.Errors);
            return new List<string>();
        }

        var resultFiles = new List<string>();
        resultFiles.AddRange(Directory.GetFiles(folder.FullName, "*.txt", SearchOption.TopDirectoryOnly).Select(ExtractHeadTipShaFromFileName));
        var subFolder = folder.SubFolder("common");
        resultFiles.AddRange(Directory.GetFiles(subFolder.FullName, "*.txt", SearchOption.TopDirectoryOnly).Select(ExtractHeadTipShaFromFileName));
        var subFolder2 = folder.SubFolder("commonarchive");
        subFolder2.CreateIfNecessary();
        resultFiles.AddRange(Directory.GetFiles(subFolder2.FullName, "*.txt", SearchOption.TopDirectoryOnly).Select(ExtractHeadTipShaFromFileName));
        var subFolder3 = folder.SubFolder("aspenlaub.local.archive");
        subFolder3.CreateIfNecessary();
        resultFiles.AddRange(Directory.GetFiles(subFolder3.FullName, "*.txt", SearchOption.TopDirectoryOnly).Select(ExtractHeadTipShaFromFileName));
        if (!resultFiles.Any()) {
            var folders = new[] { folder, subFolder };
            var displayedFolders = string.Join(", ", folders.Select(f => '"' + f.FullName + '"'));
            errorsAndInfos.Errors.Add(string.Format(Properties.Resources.NoPushedHeadTipShasFound, displayedFolders));
        }
        subFolder = folder.SubFolder(string.IsNullOrEmpty(nugetFeedId) ? "nofeed" : nugetFeedId);
        subFolder.CreateIfNecessary();
        resultFiles.AddRange(Directory.GetFiles(subFolder.FullName, "*.json", SearchOption.TopDirectoryOnly).Select(ExtractHeadTipShaFromFileName));
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

        if (fileName == null) {
            throw new Exception(nameof(fileName));
        }

        if (File.Exists(fileName)) { return; }

        await File.WriteAllTextAsync(fileName, headTipSha);
    }

    public async Task AddAsync(string nugetFeedId, string headTipSha, string packageId, string packageVersion, IErrorsAndInfos errorsAndInfos) {
        var fileName = await JsonFileNameAsync(nugetFeedId, headTipSha, errorsAndInfos);
        if (errorsAndInfos.AnyErrors()) { return; }

        if (fileName == null) {
            throw new Exception(nameof(fileName));
        }

        if (File.Exists(fileName)) { return; }

        var dictionary = new Dictionary<string, string> {
            {nameof(headTipSha), headTipSha}, {nameof(packageId), packageId}, {nameof(packageVersion), packageVersion}, {nameof(nugetFeedId), nugetFeedId}
        };

        var json = JsonSerializer.Serialize(dictionary);

        await File.WriteAllTextAsync(fileName, json);
    }

    public async Task<DateTime> AddedAtAsync(string nugetFeedId, string headTipSha, IErrorsAndInfos errorsAndInfos) {
        var fileName = await TextFileNameAsync(headTipSha, errorsAndInfos);
        if (errorsAndInfos.AnyErrors()) { return DateTime.MaxValue;}

        return !File.Exists(fileName) ? DateTime.MaxValue : File.GetLastWriteTime(fileName);
    }

    private async Task<string> TextFileNameAsync(string headTipSha, IErrorsAndInfos errorsAndInfos) {
        var folder = await RepositoryFolderAsync(errorsAndInfos);
        if (errorsAndInfos.AnyErrors()) { return ""; }

        folder = folder.SubFolder("common");
        folder.CreateIfNecessary();

        return folder.FullName + '\\' + headTipSha + ".txt";
    }

    private async Task<string> JsonFileNameAsync(string nugetFeedId, string headTipSha, IErrorsAndInfos errorsAndInfos) {
        var folder = await RepositoryFolderAsync(errorsAndInfos);
        if (errorsAndInfos.AnyErrors()) { return ""; }

        folder = folder.SubFolder(string.IsNullOrEmpty(nugetFeedId) ? "nofeed" : nugetFeedId);
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

        var dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(fileName));
        if (dictionary?.TryGetValue("packageVersion", out var packageVersion) is true) {
            return packageVersion;
        }

        errorsAndInfos.Errors.Add(string.Format(Properties.Resources.CouldNotDeserializeFromFile, fileName));
        return "";
    }
}