using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.ViewModels
{
    public partial class UpcomingUpdatesViewModel : ObservableObject
    {
        private readonly ILcsUpcomingUpdatesService _updatesService;

        [ObservableProperty] private ObservableCollection<UpcomingCalendarViewModels> _updates = new();
        [ObservableProperty] private bool _isBusy;

        public UpcomingUpdatesViewModel(ILcsUpcomingUpdatesService updatesService)
        {
            _updatesService = updatesService;
        }

        [RelayCommand]
        private async Task Load()
        {
            IsBusy = true;
            try
            {
                var updates = await _updatesService.GetUpcomingCalendarsAsync();
                if (updates != null) Updates = new ObservableCollection<UpcomingCalendarViewModels>(updates);
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private void Close()
        {
            foreach (Window window in Application.Current.Windows)
                if (window.DataContext == this) { window.Close(); break; }
        }
    }
}
