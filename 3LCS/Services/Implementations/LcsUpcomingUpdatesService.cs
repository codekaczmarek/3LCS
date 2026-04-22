using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Services.Implementations
{
    public class LcsUpcomingUpdatesService : LcsServiceBase, ILcsUpcomingUpdatesService
    {
        public LcsUpcomingUpdatesService(ILcsHttpClientService http, ILcsSessionService sessionState, ILcsAuthService auth, ILogger<LcsUpcomingUpdatesService> logger)
            : base(http, sessionState, auth, logger) { }

        public async Task<List<UpcomingCalendarViewModels>?> GetUpcomingCalendarsAsync()
        {
            try
            {
                var url = $"{Http.LcsUrl}/RainierSettings/GetUpcomingCalendars/{Http.LcsProjectId}/?id={Http.LcsProjectId}&_={DateTimeOffset.Now.ToUnixTimeSeconds()}";
                var response = await GetResponseAsync(url);
                if (response?.Success != true || response.Data == null) return null;
                var calendarsJson = JObject.Parse(response.Data.ToString()!)?["UpcomingCalendarViewModels"]?.ToString();
                return calendarsJson == null ? null : JsonConvert.DeserializeObject<List<UpcomingCalendarViewModels>>(calendarsJson);
            }
            catch { return null; }
        }
    }
}
