using System.Windows;
using ThreeLCS.Models;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Views
{
    public partial class AddNsgWindow : Window
    {
        private readonly AddNsgViewModel _vm;

        public AddNsgWindow(AddNsgViewModel viewModel)
        {
            InitializeComponent();
            _vm = viewModel;
            DataContext = viewModel;
        }

        public CloudHostedInstance? Instance { get; set; }
        public string RuleName => _vm.RuleName;
        public string IpOrCidr => _vm.IpOrCidr;
    }
}
