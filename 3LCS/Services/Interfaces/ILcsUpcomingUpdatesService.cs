using System.Collections.Generic;
using System.Threading.Tasks;
using ThreeLCS.Models;

namespace ThreeLCS.Services.Interfaces
{
    public interface ILcsUpcomingUpdatesService
    {
        Task<List<UpcomingCalendarViewModels>?> GetUpcomingCalendarsAsync();
    }
}
