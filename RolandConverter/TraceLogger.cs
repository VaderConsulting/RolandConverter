namespace RolandConverter
{
    /// <summary>
    /// Provides logging functionality with timestamps for the Roland Converter application.
    /// </summary>
    public static class TraceLogger
    {
        #region Public Methods

        /// <summary>
        /// Logs a message with a timestamp and automatically determined method name to the console.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Log(string message)
        {
            string methodName = Helper.GetCurrentMethodName(2);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{methodName}] {message}");
        }

        /// <summary>
        /// Logs a message with a timestamp and source to the console.
        /// </summary>
        /// <param name="source">The source of the log message (e.g., class name).</param>
        /// <param name="message">The message to log.</param>
        public static void Log(string source, string message)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{source}] {message}");
        }

        #endregion
    }
}
