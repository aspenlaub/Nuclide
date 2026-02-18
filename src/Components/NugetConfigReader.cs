using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Skladasu.Interfaces;

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

        XElement sourceElement = document.XPathSelectElements("./configuration/packageSources/add[@key=\"" + source + "\"]").FirstOrDefault();
        string sourceKey = sourceElement?.Attribute("value")?.Value;
        if (string.IsNullOrEmpty(sourceKey)) {
            errorsAndInfos.Errors.Add(Properties.Resources.NoApiKeyFound);
            return "";
        }

        XElement apiKeyElement = document.XPathSelectElements("./configuration/apikeys/add[@key=\"" + sourceKey + "\"]").FirstOrDefault();
        string apiKey = apiKeyElement?.Attribute("value")?.Value;
        if (string.IsNullOrEmpty(apiKey)) {
            errorsAndInfos.Errors.Add(Properties.Resources.NoApiKeyFound);
        }

        return apiKey;
    }
}