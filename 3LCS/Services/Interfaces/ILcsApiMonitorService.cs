using System.Collections.ObjectModel;
using ThreeLCS.Models;

namespace ThreeLCS.Services.Interfaces
{
    public interface ILcsApiMonitorService
    {
        string CurrentOperation { get; }
        int ActiveCallCount { get; }
        ReadOnlyObservableCollection<ApiMonitorEntry> RecentCalls { get; }
    }
}
