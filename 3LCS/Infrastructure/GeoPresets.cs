using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ThreeLCS.Infrastructure
{
    public class GeoPreset
    {
        public string Name { get; set; } = string.Empty;
        public string LcsUrl { get; set; } = string.Empty;
        public string LcsUpdateUrl { get; set; } = string.Empty;
        public string LcsDiagUrl { get; set; } = string.Empty;
        public string LcsFixUrl { get; set; } = string.Empty;
    }

    public static class GeoPresets
    {
        private static List<GeoPreset>? _presets;

        public static IReadOnlyList<GeoPreset> All => _presets ??= Load();

        private static List<GeoPreset> Load()
        {
            var assembly = Assembly.GetExecutingAssembly();
            const string resourceName = "ThreeLCS.Infrastructure.lcs-geos.json";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                return new List<GeoPreset>();
            using var reader = new StreamReader(stream);
            return JsonConvert.DeserializeObject<List<GeoPreset>>(reader.ReadToEnd()) ?? new List<GeoPreset>();
        }
    }
}
