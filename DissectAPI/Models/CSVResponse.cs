namespace DissectAPI.Models
{
    public class CSVResponse
    {
        public CSVResponse() { }
        
        public string CSV { get; set; }

        /// <summary>
        /// Column titles for the CSV -> dict key is the column name, value is the title
        /// </summary>
        public Dictionary<string, string> ColumnTitles { get; set; }

        /// <summary>
        /// Metadata for the CSV -> dict key is the metadata name, value is the content
        /// </summary>
        public Dictionary<string, string> MetaData { get; set; }

    }
}
