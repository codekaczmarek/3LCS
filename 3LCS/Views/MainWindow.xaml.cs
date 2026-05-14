using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using ThreeLCS.Infrastructure;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Loaded += async (_, _) => await viewModel.InitializeAsync();
        }

        // Fix: WPF windows with WindowStyle="None" ignore the taskbar working area when maximized,
        // causing the window to overflow over all screen edges and cover the taskbar.
        // Hooking WM_GETMINMAXINFO lets us constrain the maximized size/position to the
        // monitor's working area (rcWork) instead of the full monitor bounds (rcMonitor).
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(WndProc);
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_GETMINMAXINFO = 0x0024;
            if (msg == WM_GETMINMAXINFO)
            {
                var mmi = Marshal.PtrToStructure<NativeMethods.MINMAXINFO>(lParam);
                var monitor = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
                if (monitor != IntPtr.Zero)
                {
                    var info = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>() };
                    NativeMethods.GetMonitorInfo(monitor, ref info);
                    var work = info.rcWork;   // working area (excludes taskbar)
                    var mon  = info.rcMonitor; // full monitor bounds
                    mmi.ptMaxPosition.x = Math.Abs(work.left - mon.left);
                    mmi.ptMaxPosition.y = Math.Abs(work.top  - mon.top);
                    mmi.ptMaxSize.x     = Math.Abs(work.right  - work.left);
                    mmi.ptMaxSize.y     = Math.Abs(work.bottom - work.top);
                }
                Marshal.StructureToPtr(mmi, lParam, true);
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);
        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                SystemCommands.RestoreWindow(this);
            else
                SystemCommands.MaximizeWindow(this);
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e) => SystemCommands.CloseWindow(this);

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void DataGridRow_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && ItemsControl.ItemsControlFromItemContainer(row) is DataGrid grid)
            {
                if (!row.IsSelected)
                    grid.SelectedItems.Clear();

                row.IsSelected = true;
            }
        }

        private void EnvironmentGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not MainViewModel viewModel || sender is not DataGrid grid) return;

            var rows = grid.SelectedItems.OfType<EnvironmentViewModel>().ToList();
            if (ReferenceEquals(grid, CheDataGrid))
                viewModel.SetSelectedCheRows(rows);
            else if (ReferenceEquals(grid, SaasDataGrid))
                viewModel.SetSelectedSaasRows(rows);
        }

        private void FavouriteTile_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item)
                item.IsSelected = true;
        }
    }
}
