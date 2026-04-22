using System.Windows;
using ThreeLCS.Models;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Views
{
    public partial class PowerShellWindow : Window
    {
        public PowerShellWindow(PowerShellViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        // Stored for context; PowerShell VM runs scripts locally
        public CloudHostedInstance? Instance { get; set; }
    }
}
