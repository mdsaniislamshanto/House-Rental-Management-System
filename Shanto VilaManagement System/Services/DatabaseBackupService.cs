using System.Data.Common;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Shanto_VilaManagement_System.Data;

namespace Shanto_VilaManagement_System.Services
{
    public class DatabaseBackupService : IDatabaseBackupService
    {
        private readonly AppDbContext _context;

        public DatabaseBackupService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> GenerateSqlBackupAsync()
        {
            var sb = new StringBuilder();
            var connection = _context.Database.GetDbConnection();
            bool shouldClose = false;

            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
                shouldClose = true;
            }

            try
            {
                var dbName = connection.Database ?? "shanto_vila_db";
                var now = DateTime.Now;

                // Header
                sb.AppendLine("-- -------------------------------------------------------------");
                sb.AppendLine("-- Shanto Vila Management System Database Backup");
                sb.AppendLine($"-- Database: `{dbName}`");
                sb.AppendLine($"-- Generated At: {now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine("-- -------------------------------------------------------------");
                sb.AppendLine();
                sb.AppendLine("SET NAMES utf8mb4;");
                sb.AppendLine("SET FOREIGN_KEY_CHECKS = 0;");
                sb.AppendLine("SET SQL_MODE = 'NO_AUTO_VALUE_ON_ZERO';");
                sb.AppendLine("SET time_zone = '+06:00';");
                sb.AppendLine();

                // Get table names
                var tableNames = new List<string>();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SHOW TABLES;";
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var tName = reader.GetString(0);
                        tableNames.Add(tName);
                    }
                }

                // Process each table
                foreach (var table in tableNames)
                {
                    sb.AppendLine("-- -------------------------------------------------------------");
                    sb.AppendLine($"-- Table structure for `{table}`");
                    sb.AppendLine("-- -------------------------------------------------------------");
                    sb.AppendLine($"DROP TABLE IF EXISTS `{table}`;");

                    // Get CREATE TABLE DDL
                    string? createTableDdl = null;
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = $"SHOW CREATE TABLE `{table}`;";
                        using var reader = await cmd.ExecuteReaderAsync();
                        if (await reader.ReadAsync())
                        {
                            createTableDdl = reader.GetString(1);
                        }
                    }

                    if (!string.IsNullOrEmpty(createTableDdl))
                    {
                        sb.AppendLine(createTableDdl + ";");
                        sb.AppendLine();
                    }

                    // Dump Data
                    sb.AppendLine($"-- Dumping data for `{table}`");
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = $"SELECT * FROM `{table}`;";
                        using var reader = await cmd.ExecuteReaderAsync();
                        
                        var columnCount = reader.FieldCount;
                        var rowsBatch = new List<string>();

                        while (await reader.ReadAsync())
                        {
                            var values = new string[columnCount];
                            for (int i = 0; i < columnCount; i++)
                            {
                                if (await reader.IsDBNullAsync(i))
                                {
                                    values[i] = "NULL";
                                }
                                else
                                {
                                    var val = reader.GetValue(i);
                                    if (val is bool b)
                                    {
                                        values[i] = b ? "1" : "0";
                                    }
                                    else if (val is byte or short or int or long or decimal or double or float)
                                    {
                                        values[i] = Convert.ToString(val, System.Globalization.CultureInfo.InvariantCulture)!;
                                    }
                                    else if (val is DateTime dt)
                                    {
                                        values[i] = $"'{dt:yyyy-MM-dd HH:mm:ss}'";
                                    }
                                    else
                                    {
                                        var str = val.ToString() ?? "";
                                        var escaped = str
                                            .Replace("\\", "\\\\")
                                            .Replace("'", "\\'")
                                            .Replace("\r", "\\r")
                                            .Replace("\n", "\\n");
                                        values[i] = $"'{escaped}'";
                                    }
                                }
                            }

                            rowsBatch.Add($"({string.Join(", ", values)})");

                            if (rowsBatch.Count >= 100)
                            {
                                sb.AppendLine($"INSERT INTO `{table}` VALUES");
                                sb.AppendLine(string.Join(",\n", rowsBatch) + ";");
                                rowsBatch.Clear();
                            }
                        }

                        if (rowsBatch.Count > 0)
                        {
                            sb.AppendLine($"INSERT INTO `{table}` VALUES");
                            sb.AppendLine(string.Join(",\n", rowsBatch) + ";");
                            rowsBatch.Clear();
                        }
                    }

                    sb.AppendLine();
                }

                sb.AppendLine("SET FOREIGN_KEY_CHECKS = 1;");
                sb.AppendLine("-- -------------------------------------------------------------");
                sb.AppendLine("-- Backup complete");
                sb.AppendLine("-- -------------------------------------------------------------");

                return Encoding.UTF8.GetBytes(sb.ToString());
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }
    }
}
