using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using ThreeLCS.Infrastructure;
using ThreeLCS.Models;

namespace ThreeLCS.ViewModels
{
    public partial class CustomLinksViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<CustomLink> _links = new();
        [ObservableProperty] private CustomLink? _selectedLink;

        [RelayCommand]
        private void Add()
        {
            Links.Add(new CustomLink { Link = "https://", LinkLabel = "New Link" });
        }

        [RelayCommand]
        private void Remove()
        {
            if (SelectedLink != null) Links.Remove(SelectedLink);
        }

        [RelayCommand]
        private void Save()
        {
            foreach (Window window in Application.Current.Windows)
                if (window.DataContext == this) { window.DialogResult = true; break; }
        }

        [RelayCommand]
        private void OpenLink(CustomLink? link)
        {
            if (link?.Link != null) WebBrowserHelper.OpenUri(link.Link);
        }
    }
}
