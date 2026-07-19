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
            const string method = nameof(ValidateMidiFile);
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream must be readable", nameof(stream));
            }

            if (stream.Length < 14) // Minimum MIDI file size
            {
                throw new InvalidDataException("File too small to be a valid MIDI file");
            }

            long originalPosition = stream.Position;
            try
            {
                // Log the full file bytes
                stream.Position = 0;
                byte[] allBytes = new byte[stream.Length];
                _ = stream.Read(allBytes, 0, allBytes.Length);
                TraceLogger.Log(method, $"MIDI file bytes (limited to 200): {BitConverter.ToString(allBytes.Take(200))}");
                stream.Position = originalPosition;

                using (BinaryReader reader = new BinaryReader(stream, Encoding.ASCII, true))
                {
                    TraceLogger.Log(method, $"Stream position before header: {reader.BaseStream.Position}");
                    string header = new string(reader.ReadChars(4));
                    TraceLogger.Log(method, $"Read header: '{header}' at position {reader.BaseStream.Position - 4}");
                    if (header != STANDARD_MIDI_HEADER)
                    {
                        throw new InvalidDataException($"Invalid MIDI header: {header}. Expected: {STANDARD_MIDI_HEADER}");
                    }

                    int headerLength = (int)ReadBigEndian(reader, 4);
                    TraceLogger.Log(method, $"Read header length: {headerLength} at position {reader.BaseStream.Position - 4}");
                    if (headerLength != 6)
                    {
                        throw new InvalidDataException($"Invalid MIDI header length: {headerLength}. Expected: 6");
                    }

                    short format = (short)ReadBigEndian(reader, 2);
                    TraceLogger.Log(method, $"Read format: {format} at position {reader.BaseStream.Position - 2}");
                    if (format > 2)
                    {
                        throw new InvalidDataException($"Unsupported MIDI format: {format}. Only formats 0, 1, and 2 are supported.");
                    }

                    short trackCount = (short)ReadBigEndian(reader, 2);
                    TraceLogger.Log(method, $"Read track count: {trackCount} at position {reader.BaseStream.Position - 2}");
                    if (trackCount <= 0)
                    {
                        throw new InvalidDataException($"Invalid track count: {trackCount}. Must be greater than 0.");
                    }

                    if (trackCount > MAX_TRACK_COUNT)
                    {
                        throw new InvalidDataException($"Too many tracks: {trackCount}. Maximum supported: {MAX_TRACK_COUNT}");
                    }

                    short division = (short)ReadBigEndian(reader, 2);
                    TraceLogger.Log(method, $"Read division: {division} at position {reader.BaseStream.Position - 2}");
                    if ((division & 0x8000) != 0)
                    {
                        throw new InvalidDataException("SMPTE time division is not supported. Only PPQ timing is supported.");
                    }

                    // Validate each track
                    for (int i = 0; i < trackCount; i++)
                    {
                        long trackHeaderPos = reader.BaseStream.Position;
                        byte[] trackHeaderBytes = reader.ReadBytes(4);
                        string trackHeader = Encoding.ASCII.GetString(trackHeaderBytes);
                        TraceLogger.Log(method, $"Track {i + 1}: Read track header at position {trackHeaderPos}: '{trackHeader}' (bytes: {BitConverter.ToString(trackHeaderBytes)})");
                        if (trackHeader != STANDARD_MIDI_TRACK)
                        {
                            if (trackHeaderBytes.Length < 4)
                            {
                                TraceLogger.Log(method, $"Track {i + 1}: Track header is too short (length={trackHeaderBytes.Length}), bytes: {BitConverter.ToString(trackHeaderBytes)}, position: {trackHeaderPos}");
                            }
                            else
                            {
                                TraceLogger.Log(method, $"Track {i + 1}: Invalid track header: '{trackHeader}', bytes: {BitConverter.ToString(trackHeaderBytes)}, position: {trackHeaderPos}");
                            }

                            throw new InvalidDataException($"Invalid track header at track {i + 1}: {trackHeader}");
                        }

                        int trackLength = (int)ReadBigEndian(reader, 4);
                        TraceLogger.Log(method, $"Track {i + 1}: Read track length: {trackLength} at position {reader.BaseStream.Position - 4}");
                        if (trackLength <= 0)
                        {
                            throw new InvalidDataException($"Invalid track length at track {i + 1}: {trackLength}. Must be greater than 0.");
                        }

                        if (reader.BaseStream.Position + trackLength > reader.BaseStream.Length)
                        {
                            throw new InvalidDataException($"Track {i + 1} data extends beyond file end. Track length: {trackLength}, Remaining bytes: {reader.BaseStream.Length - reader.BaseStream.Position}");
                        }

                        // Read and validate track data
                        long trackOffset = reader.BaseStream.Position;
                        byte[] trackData = reader.ReadBytes(trackLength);
                        TraceLogger.Log(method, $"Track {i + 1} offset: {trackOffset}, length: {trackLength}");
                        if (trackData.Length > 32)
                        {
                            string firstBytes = BitConverter.ToString(trackData, 0, 16);
                            string lastBytes = BitConverter.ToString(trackData, trackData.Length - 16, 16);
                            TraceLogger.Log(method, $"  First 16 bytes: {firstBytes}");
                            TraceLogger.Log(method, $"  Last 16 bytes:  {lastBytes}");
                        }
                        else
                        {
                            string trackAllBytes = BitConverter.ToString(trackData);
                            TraceLogger.Log(method, $"  All bytes: {trackAllBytes}");
                        }

                        // Check for End-of-Track event (FF 2F 00)
                        bool hasEndOfTrack = false;
                        int pos = 0;
                        while (pos < trackData.Length)
                        {
                            // Read delta time
                            int deltaTime = 0;
                            byte b;
                            do
                            {
                                if (pos >= trackData.Length)
                                {
                                    break;
                                }

                                b = trackData[pos++];
                                deltaTime = (deltaTime << 7) | (b & 0x7F);
                            } while ((b & 0x80) != 0);

                            if (pos >= trackData.Length)
                            {
                                break;
                            }

                            // Check for End-of-Track event
                            if (trackData[pos] == 0xFF && pos + 2 < trackData.Length &&
                                trackData[pos + 1] == 0x2F && trackData[pos + 2] == 0x00)
                            {
                                hasEndOfTrack = true;
                                TraceLogger.Log(method, $"Found End-of-Track event in track {i + 1} at position {pos}");
                                break;
                            }

                            // Skip event data
                            byte status = trackData[pos++];
                            if (status < 0x80)
                            {
                                continue; // Running status
                            }

                            int dataLength = status switch
                            {
                                0xFF => trackData[pos] == 0x2F ? 2 : ReadVariableLengthValue(trackData, ref pos) + 1,
                                0xF0 or 0xF7 => ReadVariableLengthValue(trackData, ref pos),
                                var s when s is >= 0x80 and <= 0xBF => 2,
                                var s when s is >= 0xC0 and <= 0xDF => 1,
                                var s when s is >= 0xE0 and <= 0xEF => 2,
                                _ => 0
                            };

                            pos += dataLength;
                        }

                        if (!hasEndOfTrack)
                        {
                            throw new InvalidDataException($"Track {i + 1} is missing End-of-Track event");
                        }

                        // Skip track data (already read)
                        //reader.BaseStream.Position += trackLength;
                    }

                    // Verify we've reached the end of the file
                    if (reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        throw new InvalidDataException($"Unexpected data after last track. Position: {reader.BaseStream.Position}, File length: {reader.BaseStream.Length}");
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

        ///// <summary>
        ///// Reads a 32-bit integer in big-endian format from the input stream.
        ///// </summary>
        ///// <param name="reader">The binary reader for the input stream.</param>
        ///// <returns>The 32-bit integer value.</returns>
        ///// <exception cref="InvalidDataException">Thrown when the data is invalid or incomplete.</exception>
        //private static int ReadInt32BigEndian(BinaryReader reader, int Length)
        //{
        //    byte[] bytes = reader.ReadBytes(Length);
        //    if (bytes.Length < 4)
        //    {
        //        throw new InvalidDataException("Unexpected end of data while reading 32-bit value");
        //    }

        //    if (BitConverter.IsLittleEndian)
        //    {
        //        Array.Reverse(bytes);
        //    }

        //    return BitConverter.ToInt32(bytes, 0);
        //}

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