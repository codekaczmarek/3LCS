using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.ViewModels
{
    public partial class BackgroundTasksViewModel : ObservableObject
    {
        private readonly IBackgroundTaskService _taskService;

        public ObservableCollection<ManagedTask> Tasks => _taskService.Tasks;

        public BackgroundTasksViewModel(IBackgroundTaskService taskService)
        {
            _taskService = taskService;
        }

        [RelayCommand]
        private void CancelTask(ManagedTask task) => task.Cancel();
    }
}
