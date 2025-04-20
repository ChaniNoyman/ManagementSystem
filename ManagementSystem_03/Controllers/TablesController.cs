using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ManagementSystem_03.Models;
using System.Text.Json;

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

        [HttpPost]
        public async Task<IActionResult> Edit(string tableName, IFormCollection formData)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                return Json(new { success = false, message = "שם הטבלה לא סופק." });
            }

            try
            {
                string updateQuery = $"UPDATE {tableName} SET ";
                var parameters = new Dictionary<string, object>();
                List<string> setClauses = new List<string>();
                string theIndexValue = null;

                var columnTypes = await GetColumnTypes(tableName);

                foreach (var key in formData.Keys)
                {
                    if (key.ToLower() == "theindex")
                    {
                        theIndexValue = formData[key];
                    }
                    else if (key != "headers" && columnTypes.ContainsKey(key))
                    {
                        string columnType = columnTypes[key].ToLower();
                        string stringValue = formData[key];
                        object convertedValue = stringValue;

                        if (columnType.Contains("varchar") || columnType.Contains("nvarchar") || columnType.Contains("text"))
                        {
                            if (!string.IsNullOrEmpty(stringValue) && stringValue.All(char.IsDigit))
                            {
                                return Json(new { success = false, message = $"לא ניתן להכניס מספרים בלבד לעמודה '{key}'." });
                            }
                        }
                        else if (!string.IsNullOrEmpty(stringValue))
                        {
                            if (columnType.Contains("int"))
                            {
                                if (int.TryParse(stringValue, out int intValue))
                                {
                                    convertedValue = intValue;
                                }
                                else
                                {
                                    convertedValue = null;
                                }
                            }
                            else if (columnType.Contains("decimal") || columnType.Contains("float") || columnType.Contains("double"))
                            {
                                if (decimal.TryParse(stringValue, out decimal decimalValue))
                                {
                                    convertedValue = decimalValue;
                                }
                                else
                                {
                                    convertedValue = null;
                                }
                            }
                            else if (columnType.Contains("datetime"))
                            {
                                if (DateTime.TryParse(stringValue, out DateTime dateTimeValue))
                                {
                                    convertedValue = dateTimeValue;
                                }
                                else
                                {
                                    convertedValue = null;
                                }
                            }
                            else if (columnType.Contains("bit") || columnType.Contains("bool"))
                            {
                                if (bool.TryParse(stringValue, out bool boolValue))
                                {
                                    convertedValue = boolValue;
                                }
                                else if (stringValue.ToLower() == "true" || stringValue.ToLower() == "false" || stringValue == "1" || stringValue == "0")
                                {
                                    convertedValue = stringValue.ToLower() == "true" || stringValue == "1";
                                }
                                else
                                {
                                    convertedValue = null;
                                }
                            }
                            // הוסף המרות עבור סוגי נתונים נוספים לפי הצורך
                        }
                        else
                        {
                            convertedValue = null;
                        }

                        setClauses.Add($"{key} = @{key}");
                        parameters.Add($"@{key}", convertedValue == null ? DBNull.Value : convertedValue);
                    }
                }

                if (string.IsNullOrEmpty(theIndexValue))
                {
                    return Json(new { success = false, message = "לא נמצא מזהה שורה לעדכון (theIndex)." });
                }

                updateQuery += string.Join(", ", setClauses);
                updateQuery += " WHERE theIndex = @TheIndex";
                parameters.Add("@TheIndex", theIndexValue);

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(updateQuery, connection))
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            return Json(new { success = true });
                        }
                        else
                        {
                            return Json(new { success = false, message = "לא נמצאה שורה לעדכון עם מזהה זה." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "שגיאה בעדכון הנתונים: " + ex.Message });
            }
        }

        // (הפונקציה GetColumnTypes נשארת זהה)
        private async Task<Dictionary<string, string>> GetColumnTypes(string tableName)
        {
            var columnTypes = new Dictionary<string, string>();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = @"SELECT COLUMN_NAME, DATA_TYPE
                         FROM INFORMATION_SCHEMA.COLUMNS
                         WHERE TABLE_NAME = @TableName";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TableName", tableName);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            columnTypes.Add(reader.GetString(0), reader.GetString(1));
                        }
                    }
                }
            }
            return columnTypes;
        }
    }
}