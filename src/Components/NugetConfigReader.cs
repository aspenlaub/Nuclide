using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Components;

public class NugetConfigReader : INugetConfigReader {
    public string GetApiKey(string nugetConfigFileFullName, string source, IErrorsAndInfos errorsAndInfos) {
        XDocument document;
        try {
            document = XDocument.Load(nugetConfigFileFullName);
        } catch {
            errorsAndInfos.Errors.Add(string.Format(Properties.Resources.InvalidXmlFile, nugetConfigFileFullName));
            return "";
        }

        var sourceElement = document.XPathSelectElements("./configuration/packageSources/add[@key=\"" + source + "\"]").FirstOrDefault();
        var sourceKey = sourceElement?.Attribute("value")?.Value;
        if (string.IsNullOrEmpty(sourceKey)) {
            errorsAndInfos.Errors.Add(Properties.Resources.NoApiKeyFound);
            return "";
        }

        var apiKeyElement = document.XPathSelectElements("./configuration/apikeys/add[@key=\"" + sourceKey + "\"]").FirstOrDefault();
        var apiKey = apiKeyElement?.Attribute("value")?.Value;
        if (string.IsNullOrEmpty(apiKey)) {
            errorsAndInfos.Errors.Add(Properties.Resources.NoApiKeyFound);
        }

        return apiKey;
    }
}