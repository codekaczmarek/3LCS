using System.Collections.Generic;
using ThreeLCS.Models;

namespace ThreeLCS.Services.Interfaces
{
    public interface IExportService
    {
        void ExportCheInstancesToCsv(List<CloudHostedInstance> instances, string filePath);
        void ExportToRdcManXml(List<CloudHostedInstance> instances, string filePath);
    }
}
