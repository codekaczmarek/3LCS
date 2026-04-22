using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Services.Implementations
{
    public class LcsDiagnosticsService : LcsServiceBase, ILcsDiagnosticsService
    {
        private readonly ISettingsService _settings;

        public LcsDiagnosticsService(ILcsHttpClientService http, ILcsSessionService sessionState, ISettingsService settings, ILogger<LcsDiagnosticsService> logger)
            : base(http, sessionState, logger)
        {
            _settings = settings;
        }

        public int GetBuildInfoEnvironmentId(CloudHostedInstance instance)
        {
            var environments = GetDirectSync<List<BuildInfoEnvironment>>(BuildInfoIdUrl(instance));
            return environments?.Count > 0 ? environments.First().Value : 0;
        }

        public BuildInfoDetails? GetEnvironmentBuildInfoDetails(CloudHostedInstance instance, string environmentId)
        {
            var url = $"{Http.LcsDiagUrl}/BuildInfo/GetEnvironmentBuildInfoDetails/{Http.LcsProjectId}?lcsEnvironmentId={instance.EnvironmentId}&environmentId={environmentId}&_={Ts()}";
            var response = GetDirectSync<BuildInfoDetails>(url);
            response?.BuildInfoTreeView?.RemoveAll(x => x.ParentId == null);
            return response;
        }

        public List<Hotfix>? GetAvailableHotfixes(string envId, int hotfixesType)
        {
            try
            {
                var url = $"{Http.LcsUpdateUrl}/cloudupdate/Results/{Http.LcsProjectId}?query=&countries=&industries=&configKeys=&modules=&e={envId}&page=&t={hotfixesType}&_={Ts()}";
                var response = GetResponseSync(url);
                if (response?.Success != true || response.Data == null) return null;
                var allResults = JObject.Parse(response.Data.ToString()!).SelectToken("AllResults");
                if (allResults == null) return null;
                var kbs = JsonConvert.DeserializeObject<List<Hotfix>>(allResults.ToString());
                if (kbs == null) return null;
                foreach (var kb in kbs)
                {
                    kb.Url = $"{_settings.LcsFixUrl}/Issue/Details/{Http.LcsProjectId}?kb={kb.KBNumber}&bugId={kb.BugNumber}";
                    kb.Solution = RemoveUnwantedTags(kb.Solution);
                    kb.Title = RemoveUnwantedTags(kb.Title);
                }
                return kbs;
            }
            catch { return null; }
        }

        public string? GetDiagEnvironmentId(CloudHostedInstance instance)
        {
            var r = GetResponseSync($"{Http.LcsUrl}/Environment/GetDiagEnvironmentId/{Http.LcsProjectId}?environmentId={instance.EnvironmentId}&_={Ts()}");
            return r?.Success == true ? r.Data?.ToString() : null;
        }

        private string BuildInfoIdUrl(CloudHostedInstance instance) =>
            $"{Http.LcsDiagUrl}/BuildInfo/GetEnvironments/{Http.LcsProjectId}?lcsEnvironmentId={instance.EnvironmentId}&environmentId=0&_={Ts()}";

        private static string RemoveUnwantedTags(string? data)
        {
            if (string.IsNullOrEmpty(data)) return string.Empty;
            var document = new HtmlDocument();
            document.LoadHtml(data);
            var nodes = new Queue<HtmlNode>(document.DocumentNode.SelectNodes("./*|./text()") ?? Enumerable.Empty<HtmlNode>());
            while (nodes.Count > 0)
            {
                var node = nodes.Dequeue();
                var parentNode = node.ParentNode;
                if (node.Name == "#text") continue;
                var childNodes = node.SelectNodes("./*|./text()");
                if (childNodes != null)
                    foreach (var child in childNodes) { nodes.Enqueue(child); parentNode.InsertBefore(child, node); }
                parentNode.RemoveChild(node);
            }
            var result = document.DocumentNode.InnerHtml;
            result = Regex.Replace(result, @"&nbsp;|\r\n|\t|\n|\r", " ");
            result = Regex.Replace(result, @"&quot;", "\"");
            return result;
        }
    }
}
