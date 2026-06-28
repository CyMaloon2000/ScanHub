using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using InventoryScanner.Models;
using InventoryScanner.Repositories.Interfaces;
using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.Services.Export
{
    /// <summary>
    /// Defines the contract for export format strategies.
    /// </summary>
    public interface IExportStrategy
    {
        string Format { get; }
        string FileExtension { get; }
        Task<string> ExportAsync(ExportData data, string outputDirectory);
    }

    /// <summary>
    /// Exports inventory data to CSV format.
    /// </summary>
    public class CsvExportStrategy : IExportStrategy
    {
        public string Format => "CSV";
        public string FileExtension => ".csv";

        public async Task<string> ExportAsync(ExportData data, string outputDirectory)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filePath = Path.Combine(outputDirectory, $"inventory_export_{timestamp}.csv");

            var sb = new StringBuilder();

            sb.AppendLine("SessionId,SessionName,BuildingId,BuildingName,LocationId,LocationName,RoomId,RoomName,OfficeId,OfficeName,Description,Status,CreatedBy,CreatedDate,UpdatedDate,ScanCount,ScanId,UniqueIdentifier,BarcodeType,ScannedAt,ItemStatus,Remarks");

            foreach (var sessionItem in data.Sessions)
            {
                var s = sessionItem.Session;
                if (sessionItem.Items.Count == 0)
                {
                    sb.AppendLine(string.Join(",",
                        CsvEscape(s.SessionId.ToString()),
                        CsvEscape(s.SessionName),
                        CsvEscape(s.BuildingId.ToString()),
                        CsvEscape(s.BuildingName),
                        CsvEscape(s.LocationId.ToString()),
                        CsvEscape(s.LocationName),
                        CsvEscape(s.RoomId.ToString()),
                        CsvEscape(s.RoomName),
                        CsvEscape(s.OfficeId?.ToString() ?? string.Empty),
                        CsvEscape(s.OfficeName),
                        CsvEscape(s.Description),
                        CsvEscape(s.Status),
                        CsvEscape(s.CreatedBy),
                        CsvEscape(FormatDate(s.CreatedDate)),
                        CsvEscape(FormatDate(s.UpdatedDate)),
                        CsvEscape(s.ScanCount.ToString()),
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty));
                }
                else
                {
                    foreach (var item in sessionItem.Items)
                    {
                        sb.AppendLine(string.Join(",",
                            CsvEscape(s.SessionId.ToString()),
                            CsvEscape(s.SessionName),
                            CsvEscape(s.BuildingId.ToString()),
                            CsvEscape(s.BuildingName),
                            CsvEscape(s.LocationId.ToString()),
                            CsvEscape(s.LocationName),
                            CsvEscape(s.RoomId.ToString()),
                            CsvEscape(s.RoomName),
                            CsvEscape(s.OfficeId?.ToString() ?? string.Empty),
                            CsvEscape(s.OfficeName),
                            CsvEscape(s.Description),
                            CsvEscape(s.Status),
                            CsvEscape(s.CreatedBy),
                            CsvEscape(FormatDate(s.CreatedDate)),
                            CsvEscape(FormatDate(s.UpdatedDate)),
                            CsvEscape(s.ScanCount.ToString()),
                            CsvEscape(item.ScanId.ToString()),
                            CsvEscape(item.UniqueIdentifier),
                            CsvEscape(item.BarcodeType),
                            CsvEscape(FormatDate(item.ScannedAt)),
                            CsvEscape(item.Status),
                            CsvEscape(item.Remarks)));
                    }
                }
            }

            await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
            return filePath;
        }

        private static string FormatDate(DateTime value) => value.ToString("yyyy-MM-dd HH:mm:ss");

        private static string CsvEscape(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
    }

    /// <summary>
    /// Exports inventory data to JSON format.
    /// </summary>
    public class JsonExportStrategy : IExportStrategy
    {
        public string Format => "JSON";
        public string FileExtension => ".json";

        public async Task<string> ExportAsync(ExportData data, string outputDirectory)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filePath = Path.Combine(outputDirectory, $"inventory_export_{timestamp}.json");

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var export = new
            {
                data.ExportVersion,
                data.ExportDate,
                data.ExportedBy,
                Sessions = data.Sessions.Select(sessionItem => new
                {
                    Session = new
                    {
                        sessionItem.Session.SessionId,
                        sessionItem.Session.SessionName,
                        sessionItem.Session.BuildingId,
                        sessionItem.Session.BuildingName,
                        sessionItem.Session.LocationId,
                        sessionItem.Session.LocationName,
                        sessionItem.Session.RoomId,
                        sessionItem.Session.RoomName,
                        sessionItem.Session.OfficeId,
                        sessionItem.Session.OfficeName,
                        sessionItem.Session.Description,
                        sessionItem.Session.Status,
                        sessionItem.Session.CreatedBy,
                        sessionItem.Session.CreatedDate,
                        sessionItem.Session.UpdatedDate,
                        sessionItem.Session.ScanCount
                    },
                    sessionItem.Items
                })
            };

            var json = JsonSerializer.Serialize(export, options);
            await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
            return filePath;
        }
    }

    /// <summary>
    /// Exports inventory data to a plain-text tabular format (no external dependency needed).
    /// </summary>
    public class TextExportStrategy : IExportStrategy
    {
        public string Format => "TXT";
        public string FileExtension => ".txt";

        public async Task<string> ExportAsync(ExportData data, string outputDirectory)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filePath = Path.Combine(outputDirectory, $"inventory_export_{timestamp}.txt");

            var sb = new StringBuilder();
            sb.AppendLine($"Inventory Export Report");
            sb.AppendLine($"Generated: {data.ExportDate:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Exported By: {data.ExportedBy}");
            sb.AppendLine(new string('=', 80));
            sb.AppendLine();

            foreach (var sessionItem in data.Sessions)
            {
                var s = sessionItem.Session;
                sb.AppendLine($"SESSION: {s.SessionName}");
                sb.AppendLine($"  Building: {s.BuildingName} ({s.BuildingId})");
                sb.AppendLine($"  Location: {s.LocationName} ({s.LocationId})");
                sb.AppendLine($"  Room: {s.RoomName} ({s.RoomId})");
                sb.AppendLine($"  Office: {(string.IsNullOrWhiteSpace(s.OfficeName) ? "N/A" : $"{s.OfficeName} ({s.OfficeId})")}");
                sb.AppendLine($"  Description: {s.Description}");
                sb.AppendLine($"  Status: {s.Status} | Created By: {s.CreatedBy}");
                sb.AppendLine($"  Created: {s.CreatedDate:yyyy-MM-dd HH:mm:ss} | Updated: {s.UpdatedDate:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"  Scanned: {s.ScanCount}");
                sb.AppendLine(new string('-', 60));

                if (sessionItem.Items.Count > 0)
                {
                    sb.AppendLine($"  {"#",-5} {"Barcode",-30} {"Type",-15} {"Status",-12} {"Scanned At",-20} Remarks");
                    sb.AppendLine($"  {new string('-', 5)} {new string('-', 30)} {new string('-', 15)} {new string('-', 12)} {new string('-', 20)} {new string('-', 20)}");

                    int idx = 1;
                    foreach (var item in sessionItem.Items)
                    {
                        sb.AppendLine($"  {idx,-5} {item.UniqueIdentifier,-30} {item.BarcodeType,-15} {item.Status,-12} {item.ScannedAt:yyyy-MM-dd HH:mm:ss} {item.Remarks}");
                        idx++;
                    }
                }
                else
                {
                    sb.AppendLine("  No scanned items.");
                }
                sb.AppendLine();
            }

            await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
            return filePath;
        }
    }

    /// <summary>
    /// Factory for creating export strategies based on format selection.
    /// </summary>
    public static class ExportStrategyFactory
    {
        private static readonly Dictionary<string, Func<IExportStrategy>> _strategies = new(StringComparer.OrdinalIgnoreCase)
        {
            { "CSV", () => new CsvExportStrategy() },
            { "JSON", () => new JsonExportStrategy() },
            { "TXT", () => new TextExportStrategy() }
        };

        public static IExportStrategy Create(string format)
        {
            if (_strategies.TryGetValue(format, out var factory))
                return factory();

            throw new ArgumentException($"Unsupported export format: {format}");
        }

        public static string[] GetSupportedFormats() => _strategies.Keys.ToArray();
    }
}
