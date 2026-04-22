using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using ThreeLCS.Infrastructure;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;
using ThreeLCS.Views;

namespace ThreeLCS.Services.Implementations
{
    public class RdpService : IRdpService
    {
        private readonly ILcsCredentialsService _credentials;
        private readonly IDialogService _dialog;

        public RdpService(ILcsCredentialsService credentials, IDialogService dialog)
        {
            _credentials = credentials;
            _dialog = dialog;
        }

        public async Task ConnectAsync(CloudHostedInstance instance, List<RDPConnectionDetails> rdpList)
        {
            if (rdpList.Count == 0)
            {
                _dialog.ShowError("No RDP connections available for this instance.");
                return;
            }

            RDPConnectionDetails? selected;
            if (rdpList.Count == 1)
                selected = rdpList[0];
            else
                selected = ChooseUser(rdpList);

            if (selected == null) return;

            // Launch on a background thread so the UI is not blocked while
            // cmdkey and mstsc processes are being created. No WaitForExit —
            // the user can start additional sessions immediately.
            await Task.Run(() =>
            {
                using var creds = new RdpCredentials(selected.Address!, selected.Username!, selected.Password!);
                var mstsc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\mstsc.exe"),
                        Arguments = $"/v:{selected.Address}:{selected.Port}"
                    }
                };
                mstsc.Start();
            });
        }

        public RDPConnectionDetails? ChooseUser(List<RDPConnectionDetails> rdpList)
        {
            RDPConnectionDetails? result = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var window = (ChooseMachineWindow)App.Services.GetService(typeof(ChooseMachineWindow))!;
                window.Machines = rdpList;
                if (window.ShowDialog() == true)
                    result = window.SelectedConnection;
            });
            return result;
        }
    }
}
