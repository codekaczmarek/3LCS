using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Services.Implementations
{
    public class ExportService : IExportService
    {
        public void ExportCheInstancesToCsv(List<CloudHostedInstance> instances, string filePath)
        {
            var records = new List<ExportedInstance>();
            foreach (var instance in instances)
            {
                records.Add(new ExportedInstance
                {
                    InstanceName = instance.DisplayName,
                    EnvironmentId = instance.EnvironmentId,
                    DeploymentStatus = instance.DeploymentStatus,
                    CurrentApplicationBuildVersion = instance.CurrentApplicationBuildVersion,
                    CurrentApplicationReleaseName = instance.CurrentApplicationReleaseName,
                    CurrentPlatformReleaseName = instance.CurrentPlatformReleaseName,
                    CurrentPlatformVersion = instance.CurrentPlatformVersion,
                    TopologyName = instance.TopologyName,
                    TopologyType = instance.TopologyType,
                    TopologyVersion = instance.TopologyVersion,
                    BuildInfo = instance.BuildInfo,
                    HostingType = "Cloud-Hosted"
                });
            }
            using var writer = new StreamWriter(filePath);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(records);
        }

        public void ExportToRdcManXml(List<CloudHostedInstance> instances, string filePath)
        {
            var settings = new XmlWriterSettings { Indent = true };
            using var writer = XmlWriter.Create(filePath, settings);
            writer.WriteStartDocument();
            writer.WriteStartElement("RDCMan");
            writer.WriteAttributeString("programVersion", "2.7");
            writer.WriteAttributeString("schemaVersion", "3");

            writer.WriteStartElement("file");
            writer.WriteStartElement("credentialsProfiles");
            writer.WriteEndElement();

            writer.WriteStartElement("properties");
            writer.WriteElementString("expanded", "True");
            writer.WriteElementString("name", "3LCS Export");
            writer.WriteEndElement();

            foreach (var instance in instances)
            {
                if (instance.Instances == null) continue;
                writer.WriteStartElement("group");
                writer.WriteStartElement("properties");
                writer.WriteElementString("expanded", "True");
                writer.WriteElementString("name", instance.DisplayName ?? instance.EnvironmentId ?? "Unknown");
                writer.WriteEndElement();

                foreach (var vm in instance.Instances)
                {
                    if (vm.RDPConnectionDetails == null) continue;
                    writer.WriteStartElement("server");
                    writer.WriteElementString("name", vm.MachineName ?? vm.DisplayName ?? "Unknown");
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

    }
}
