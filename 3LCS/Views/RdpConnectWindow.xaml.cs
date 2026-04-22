using System.Windows;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Views
{
    public partial class RdpConnectWindow : Window
    {
        public RdpConnectWindow()
        {
            InitializeComponent();
            var vm = new RdpConnectViewModel { Args = System.Environment.GetCommandLineArgs() };
            DataContext = vm;
            Loaded += (s, e) => vm.ConnectCommand.Execute(null);
        }
    }
}
