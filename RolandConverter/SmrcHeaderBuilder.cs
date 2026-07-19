using System.Text;

namespace RolandConverter
{
    /// <summary>
    /// Provides methods for building S-MRC header chunks in a consistent way.
    /// </summary>
    public static class SmrcHeaderBuilder
    {
        /// <summary>
        /// Creates an S-MRC header chunk with the specified parameters.
        /// </summary>
        /// <param name="title">The title of the S-MRC file (max 31 chars, ASCII + NUL pad).</param>
        /// <param name="ppqn">The pulses per quarter note (96 default).</param>
        /// <param name="timeSignatureNumerator">The numerator of the time signature (1-32).</param>
        /// <param name="timeSignatureDenominator">The denominator of the time signature (0-4).</param>
        /// <param name="tempo">The tempo in beats per minute (10-250).</param>
        /// <returns>A byte array containing the S-MRC header chunk.</returns>
        public static byte[] CreateHeader(
            string title,
            ushort ppqn,
            byte timeSignatureNumerator,
            byte timeSignatureDenominator,
            byte tempo)
        {
            TraceLogger.Log("SmrcHeaderBuilder", $"Creating header with title='{title}', ppqn={ppqn}, timeSig={timeSignatureNumerator}/{timeSignatureDenominator}, tempo={tempo}");

            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write title (32 bytes, ASCII + NUL pad)
                byte[] titleBytes = Encoding.ASCII.GetBytes(title);
                if (titleBytes.Length > Constants.SmrcFormat.TitleSize - 1)
                {
                    titleBytes = titleBytes.Take((int)(Constants.SmrcFormat.TitleSize - 1)).ToArray();
                }

                writer.Write(titleBytes);
                writer.Write(new byte[Constants.SmrcFormat.TitleSize - titleBytes.Length]);  // Pad with NULs

                // Write PPQN (2 bytes, little-endian)
                writer.Write(ppqn);

                // Write time signature (2 bytes)
                writer.Write(timeSignatureNumerator);    // Numerator (1-32)
                writer.Write(timeSignatureDenominator);  // Denominator (0-4)

                // Write initial tempo (1 byte)
                writer.Write(tempo);  // 10-250 BPM

                // Write reserved bytes (3 bytes)
                writer.Write(new byte[3]);

                // Write track directory (128 bytes)
                writer.Write(new byte[Constants.SmrcFormat.TrackDirectorySize]);

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates a default S-MRC header chunk.
        /// </summary>
        /// <returns>A byte array containing the default S-MRC header chunk.</returns>
        public static byte[] CreateDefaultHeader()
        {
            TraceLogger.Log("SmrcHeaderBuilder", "Creating default header");
            return CreateHeader(
                "Default Title",
                (ushort)Constants.SmrcFormat.DefaultPpqn,
                Constants.SmrcFormat.DefaultTimeSignatureNumerator,
                Constants.SmrcFormat.DefaultTimeSignatureDenominator,
                Constants.SmrcFormat.DefaultTempo
            );
        }
    }
}