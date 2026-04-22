using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Services.Implementations
{
    public class BackgroundTaskService : IBackgroundTaskService
    {
        private const int RemoveDelayMs = 4000;

        public ObservableCollection<ManagedTask> Tasks { get; } = new();

        public ManagedTask Run(string name, Func<CancellationToken, Task> work)
        {
            var task = new ManagedTask(name);
            Dispatch(() => Tasks.Add(task));
            _ = ExecuteAsync(task, work);
            return task;
        }

        private async Task ExecuteAsync(ManagedTask task, Func<CancellationToken, Task> work)
        {
            try
            {
                await work(task.Token);
                task.Complete();
            }
            catch (OperationCanceledException)
            {
                task.Cancel();
            }
            catch (Exception ex)
            {
                task.Fail(ex.Message);
            }
            finally
            {
                await Task.Delay(RemoveDelayMs);
                Dispatch(() => Tasks.Remove(task));
            }
        }

        private static void Dispatch(Action a)
        {
            if (Application.Current?.Dispatcher?.CheckAccess() == true)
                a();
            else
                Application.Current?.Dispatcher?.Invoke(a);
        }
    }
}
