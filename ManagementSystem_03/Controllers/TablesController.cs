using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ManagementSystem_03.Models;

namespace ManagementSystem_03.Controllers
{
    public class TablesController : Controller
    {
        private readonly string _connectionString;

        public TablesController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MyDatabase");
        }

        [HttpGet]
        public async Task<IActionResult> Index(string tableName)
        {
            TablesViewModel viewModel = new TablesViewModel
            {
                TableNames = await GetTableNames(),
                TableData = string.IsNullOrEmpty(tableName) ? null : await GetTableData(tableName)
            };

            ViewBag.SelectedTableName = tableName;

            return View(viewModel);
        }

        private async Task<List<TableName>> GetTableNames()
        {
            List<TableName> tableNames = new List<TableName>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand command = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        tableNames.Add(new TableName { Name = reader.GetString(0) });
                    }
                }
            }

            return tableNames;
        }

        private async Task<TableData> GetTableData(string tableName)
        {
            TableData tableData = new TableData { Data = new List<Dictionary<string, object>>() };

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand command = new SqlCommand($"SELECT * FROM {tableName}", connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Dictionary<string, object> row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row.Add(reader.GetName(i), reader.GetValue(i));
                        }
                        tableData.Data.Add(row);
                    }
                }
            }

            return tableData;
        }

        private async Task<Dictionary<string, object>> GetItemById(string tableName, int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand command = new SqlCommand($"SELECT * FROM {tableName} WHERE Id = @Id", connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            Dictionary<string, object> row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row.Add(reader.GetName(i), reader.GetValue(i));
                            }
                            return row;
                        }
                        else
                        {
                            return null; // רשומה לא נמצאה
                        }
                    }
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeletePost([FromBody] DeletePostRequest request)
        {
            bool success = await Delete(request.TableName, request.Id); // קריאה לפונקציה הפרטית
            if (success)
            {
                // קבל את הנתונים המעודכנים מהטבלה
                var updatedTableData = await GetTableData(request.TableName);

                // העבר את הנתונים המעודכנים לתצוגה
                return View("Index", new TablesViewModel { TableData = updatedTableData, TableNames = await GetTableNames() });
            }
            else
            {
                return View("Error");
            }
        }

        private async Task<bool> Delete(string tableName, int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand command = new SqlCommand($"DELETE FROM {tableName} WHERE theIndex = @Id", connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }

    }
}