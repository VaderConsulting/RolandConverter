namespace RolandConverter
{
    public static class Helper
    {
        public static byte[] GetBigEndianBytes(long value, int length)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            byte[] result = new byte[length];
            Array.Copy(bytes, bytes.Length - length, result, 0, length);
            return result;
        }

        /// <summary>
        /// Gets the name of the current method using reflection.
        /// </summary>
        /// <param name="frameOffset">The number of frames to skip in the call stack (default: 1 to skip this method).</param>
        /// <returns>The name of the current method.</returns>
        public static string GetCurrentMethodName(int frameOffset = 1)
        {
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
            System.Diagnostics.StackFrame frame = stackTrace.GetFrame(frameOffset);
            return frame?.GetMethod()?.Name ?? "Unknown";
        }
    }
}
