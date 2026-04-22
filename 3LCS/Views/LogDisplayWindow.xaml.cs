using System.Windows;
using ThreeLCS.Models;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Views
{
    public partial class LogDisplayWindow : Window
    {
        private readonly LogDisplayViewModel _vm;

        public LogDisplayWindow(LogDisplayViewModel viewModel)
        {
            InitializeComponent();
            _vm = viewModel;
            DataContext = viewModel;
        }

        public string LogText
        {
            get => _vm.LogText;
            set => _vm.LogText = value;
        }
    }
}
