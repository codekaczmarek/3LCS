using System;
using System.Diagnostics;

namespace ThreeLCS.Infrastructure
{
    public static class WebBrowserHelper
    {
        public static void FixBrowserVersion()
        {
            var appName = System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetExecutingAssembly().Location);
            FixBrowserVersion(appName);
        }

        public static void FixBrowserVersion(string appName)
        {
            FixBrowserVersion(appName, GetEmbVersion());
        }

        public static void FixBrowserVersion(string appName, int ieVer)
        {
            FixBrowserVersion_Internal("HKEY_LOCAL_MACHINE", appName + ".exe", ieVer);
            FixBrowserVersion_Internal("HKEY_CURRENT_USER", appName + ".exe", ieVer);
            FixBrowserVersion_Internal("HKEY_LOCAL_MACHINE", appName + ".vshost.exe", ieVer);
            FixBrowserVersion_Internal("HKEY_CURRENT_USER", appName + ".vshost.exe", ieVer);
        }

        public static int GetEmbVersion()
        {
            var ieVer = GetBrowserVersion();
            if (ieVer > 9) return ieVer * 1000 + 1;
            if (ieVer > 7) return ieVer * 1111;
            return 7000;
        }

        private static void FixBrowserVersion_Internal(string root, string appName, int ieVer)
        {
            try
            {
                if (Environment.Is64BitOperatingSystem)
                    Microsoft.Win32.Registry.SetValue(root + @"\Software\Wow6432Node\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", appName, ieVer);
                Microsoft.Win32.Registry.SetValue(root + @"\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", appName, ieVer);
            }
            catch { }
        }

        private static int GetBrowserVersion()
        {
            const string strKeyPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer";
            var ls = new[] { "svcVersion", "svcUpdateVersion", "Version", "W2kVersion" };
            var maxVer = 0;
            foreach (var t in ls)
            {
                var objVal = Microsoft.Win32.Registry.GetValue(strKeyPath, t, "0");
                var strVal = Convert.ToString(objVal) ?? "0";
                var iPos = strVal.IndexOf('.');
                if (iPos > 0) strVal = strVal.Substring(0, iPos);
                if (int.TryParse(strVal, out var res)) maxVer = Math.Max(maxVer, res);
            }
            return maxVer;
        }

        public static void OpenUri(string uri)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = uri,
                UseShellExecute = true
            };
            Process.Start(processStartInfo);
        }
    }
}
