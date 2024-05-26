using System.Data;
using System.Text;

namespace DissectAPI
{
    public static class CSVHelper
    {
        public static string ConvertDatatableToCSV(DataTable dataTable)
        {
            StringBuilder csvOutput = new StringBuilder();
            int columnCount = dataTable.Columns.Count;

            // Add the headers
            for (int i = 0; i < columnCount; i++)
            {
                csvOutput.Append(Escape(dataTable.Columns[i].ColumnName));
                if (i < columnCount - 1)
                    csvOutput.Append(";");
            }
            csvOutput.AppendLine();

            // Add the data rows
            foreach (DataRow row in dataTable.Rows)
            {
                for (int i = 0; i < columnCount; i++)
                {
                    csvOutput.Append(Escape(row[i]?.ToString() ?? ""));
                    if (i < columnCount - 1)
                        csvOutput.Append(";");
                }
                csvOutput.AppendLine();
            }

            return csvOutput.ToString();
        }

        // Method to escape CSV special characters
        private static string Escape(string data)
        {
            if (data.Contains(";") || data.Contains("\"") || data.Contains("\n"))
            {
                data = $"\"{data.Replace("\"", "\"\"")}\""; // Escape double quotes and wrap data in double quotes
            }
            return data;
        }
    }
}
