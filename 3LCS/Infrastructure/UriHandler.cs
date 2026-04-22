using Microsoft.Win32;
using System;
using System.Reflection;
using System.Security.Principal;

namespace ThreeLCS.Infrastructure
{
    public static class UriHandler
    {
        public const string URI_PROTOCOL_NAME = "MS-2LCS";

        public static bool DetectURILaunch(string[] args)
        {
            foreach (string arg in args)
            {
                if (Uri.TryCreate(arg, UriKind.RelativeOrAbsolute, out Uri? srcUri))
                {
                    Uri uri = srcUri.IsAbsoluteUri ? srcUri : new Uri(new Uri("ms-2lcs://lcs.dynamics.com/"), arg);
                    if (uri.Scheme == URI_PROTOCOL_NAME.ToLower()) return true;
                }
            }
            return false;
        }

        public static (bool success, string message) RegisterHandler()
        {
            if (!CheckIsUserAdministrator())
                return (false, "You must be a system administrator to register the URI handler.");

            Registry.ClassesRoot.DeleteSubKeyTree(URI_PROTOCOL_NAME, false);

            var rootKey = Registry.ClassesRoot.CreateSubKey(URI_PROTOCOL_NAME.ToLower());
            if (rootKey != null)
            {
                string appLocation = Assembly.GetExecutingAssembly().Location;
                rootKey.SetValue("", $"URL:{URI_PROTOCOL_NAME.ToLower()}");
                rootKey.SetValue("URL Protocol", "");
                rootKey.CreateSubKey("DefaultIcon")?.SetValue("", appLocation);
                rootKey.CreateSubKey("shell")?.CreateSubKey("open")?.CreateSubKey("command")?.SetValue("", $@"""{appLocation}"" ""%1""");
            }
            return (true, $"{URI_PROTOCOL_NAME} protocol handler registration completed.");
        }

        public static (bool success, string message) RemoveHandler()
        {
            if (!CheckIsUserAdministrator())
                return (false, "You must be a system administrator.");
            Registry.ClassesRoot.DeleteSubKeyTree(URI_PROTOCOL_NAME, false);
            return (true, $"{URI_PROTOCOL_NAME} protocol handler registration removed.");
        }

        private static bool CheckIsUserAdministrator() =>
            new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
