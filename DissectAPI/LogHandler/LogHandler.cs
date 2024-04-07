using Serilog;

namespace DissectAPI.LogHandler
{
    public static class LogHandler
    {
        /// <summary>
        /// Deletes log files older than the specified age.
        /// </summary>
        /// <param name="logDirectory">The directory containing the log files.</param>
        /// <param name="maxAge">The maximum age of log files to retain.</param>
        public static void CleanupOldLogFiles(string logDirectory, TimeSpan maxAge)
        {
            var now = DateTime.Now;
            var logFiles = Directory.GetFiles(logDirectory, "*.txt"); // Adjust the pattern as needed

            foreach (var file in logFiles)
            {
                try
                {
                    var lastWriteTime = File.GetLastWriteTime(file);
                    if (now - lastWriteTime > maxAge)
                    {
                        File.Delete(file);
                        Console.WriteLine($"Deleted old log file: {file}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting file {file}: {ex.Message}");
                    Log.Error(ex, "Error deleting file {file}", file);
                }
            }
        }
    }
}
