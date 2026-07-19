using System.Text;

namespace RolandConverter
{
    /// <summary>
    /// Provides functionality to validate MIDI files.
    /// </summary>
    public static class MidiValidator
    {
        #region Constants

        private const string STANDARD_MIDI_HEADER = "MThd";
        private const string STANDARD_MIDI_TRACK = "MTrk";
        private const int MAX_TRACK_COUNT = 8; // Maximum number of tracks supported by S-MRC

        #endregion

        #region Public Methods

        /// <summary>
        /// Validates a MIDI file stream.
        /// </summary>
        /// <param name="stream">The stream containing the MIDI file data.</param>
        /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the stream is not readable.</exception>
        /// <exception cref="InvalidDataException">Thrown when the file is not a valid MIDI file.</exception>
        public static void ValidateMidiFile(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream must be readable", nameof(stream));
            }

            if (stream.Length < Constants.MidiFormat.HeaderSize)
            {
                throw new InvalidDataException($"File too small. MIDI files must be at least {Constants.MidiFormat.HeaderSize} bytes");
            }

            long originalPosition = stream.Position;
            try
            {
                // Log the full file bytes
                stream.Position = 0;
                byte[] allBytes = new byte[stream.Length];
                _ = stream.Read(allBytes, 0, allBytes.Length);
                TraceLogger.Log($"MIDI file bytes (limited to 200): {BitConverter.ToString(allBytes.Take(200).ToArray())}");
                stream.Position = originalPosition;

                using (BinaryReader reader = new BinaryReader(stream, Encoding.ASCII, true))
                {
                    TraceLogger.Log($"Stream position before header: {reader.BaseStream.Position}");

                    // Validate header chunk
                    string headerChunkId = new string(reader.ReadChars(4));
                    TraceLogger.Log($"Read header chunk ID: {headerChunkId} at position {reader.BaseStream.Position - 4}");
                    if (headerChunkId != "MThd")
                    {
                        throw new InvalidDataException($"Invalid header chunk ID: {headerChunkId}");
                    }

                    int headerLength = (int)ReadBigEndian(reader, 4);
                    TraceLogger.Log($"Read header length: {headerLength} at position {reader.BaseStream.Position - 4}");
                    if (headerLength != 6)
                    {
                        throw new InvalidDataException($"Invalid header length: {headerLength}");
                    }

                    short format = (short)ReadBigEndian(reader, 2);
                    TraceLogger.Log($"Read format: {format} at position {reader.BaseStream.Position - 2}");
                    if (format is < 0 or > 2)
                    {
                        throw new InvalidDataException($"Invalid format: {format}");
                    }

                    short numTracks = (short)ReadBigEndian(reader, 2);
                    TraceLogger.Log($"Read numTracks: {numTracks} at position {reader.BaseStream.Position - 2}");
                    if (numTracks < 1)
                    {
                        throw new InvalidDataException($"Invalid number of tracks: {numTracks}");
                    }

                    if (format == 0 && numTracks != 1)
                    {
                        throw new InvalidDataException("Format 0 MIDI files must have exactly one track");
                    }

                    short division = (short)ReadBigEndian(reader, 2);
                    TraceLogger.Log($"Read division: {division} at position {reader.BaseStream.Position - 2}");
                    if (division == 0)
                    {
                        throw new InvalidDataException("Invalid division value: 0");
                    }

                    // Validate tracks
                    for (int i = 0; i < numTracks; i++)
                    {
                        TraceLogger.Log($"Validating track {i + 1} of {numTracks}");
                        string trackChunkId = new string(reader.ReadChars(4));
                        TraceLogger.Log($"Read track chunk ID: {trackChunkId} at position {reader.BaseStream.Position - 4}");
                        if (trackChunkId != "MTrk")
                        {
                            throw new InvalidDataException($"Invalid track chunk ID: {trackChunkId}");
                        }

                        int trackLength = (int)ReadBigEndian(reader, 4);
                        TraceLogger.Log($"Read track length: {trackLength} at position {reader.BaseStream.Position - 4}");
                        if (trackLength < 0)
                        {
                            throw new InvalidDataException($"Invalid track length: {trackLength}");
                        }

                        // Read and validate track data
                        byte[] trackData = reader.ReadBytes(trackLength);
                        TraceLogger.Log($"Read track data: {BitConverter.ToString(trackData.Take(200).ToArray())}");
                        if (trackData.Length < 4)
                        {
                            throw new InvalidDataException("Track data too short");
                        }

                        // Count End-of-Track events
                        int endOfTrackCount = 0;
                        for (int j = 0; j <= trackData.Length - 4; j++)
                        {
                            if (trackData[j] == 0x00 &&
                                trackData[j + 1] == 0xFF &&
                                trackData[j + 2] == 0x2F &&
                                trackData[j + 3] == 0x00)
                            {
                                endOfTrackCount++;
                            }
                        }

                        if (endOfTrackCount == 0)
                        {
                            throw new InvalidDataException("Track data is missing End-of-Track event.");
                        }
                        else if (endOfTrackCount > 1)
                        {
                            throw new InvalidDataException($"Track data contains {endOfTrackCount} End-of-Track events. Only one is allowed.");
                        }

                        // Verify the End-of-Track event is at the end
                        if (trackData[trackData.Length - 4] != 0x00 ||
                            trackData[trackData.Length - 3] != 0xFF ||
                            trackData[trackData.Length - 2] != 0x2F ||
                            trackData[trackData.Length - 1] != 0x00)
                        {
                            throw new InvalidDataException("Track data must end with an End-of-Track event.");
                        }
                    }
                }
            }
            finally
            {
                stream.Position = originalPosition;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Reads an integer of the specified byte length in big-endian format from the input stream.
        /// </summary>
        /// <param name="reader">The binary reader for the input stream.</param>
        /// <param name="length">The number of bytes to read (2, 4, or 8).</param>
        /// <returns>The integer value as a long.</returns>
        /// <exception cref="InvalidDataException">Thrown when the data is invalid or incomplete.</exception>
        private static long ReadBigEndian(BinaryReader reader, int length)
        {
            byte[] bytes = reader.ReadBytes(length);
            if (bytes.Length < length)
            {
                throw new InvalidDataException($"Unexpected end of data while reading {length * 8}-bit value");
            }

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            // Pad to 8 bytes for BitConverter.ToInt64
            if (bytes.Length < 8)
            {
                byte[] padded = new byte[8];
                Array.Copy(bytes, 0, padded, 8 - bytes.Length, bytes.Length);
                bytes = padded;
            }

            return length switch
            {
                2 => BitConverter.ToInt16(bytes, 8 - 2),
                4 => BitConverter.ToInt32(bytes, 8 - 4),
                8 => BitConverter.ToInt64(bytes, 0),
                _ => throw new ArgumentException("Length must be 2, 4, or 8", nameof(length))
            };
        }

        /// <summary>
        /// Reads a variable-length value from a byte array.
        /// </summary>
        /// <param name="data">The byte array containing the data.</param>
        /// <param name="pos">The current position in the array, which will be updated.</param>
        /// <returns>The decoded variable-length value.</returns>
        private static int ReadVariableLengthValue(byte[] data, ref int pos)
        {
            int value = 0;
            byte b;
            do
            {
                if (pos >= data.Length)
                {
                    break;
                }

                b = data[pos++];
                value = (value << 7) | (b & 0x7F);
            } while ((b & 0x80) != 0);
            return value;
        }

        #endregion
    }
}