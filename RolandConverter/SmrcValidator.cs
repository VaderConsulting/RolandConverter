using System.Text;
using System.Linq;

namespace RolandConverter
{
    /// <summary>
    /// Provides functionality to validate S-MRC files.
    /// </summary>
    public static class SmrcValidator
    {
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
            MidiValidator.ValidateMidiFile(stream);
        }

        /// <summary>
        /// Validates an S-MRC file stream.
        /// </summary>
        /// <param name="stream">The stream containing the S-MRC file data.</param>
        /// <exception cref="InvalidDataException">Thrown when the file is not a valid S-MRC file.</exception>
        public static void ValidateSmrcFile(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream must be readable", nameof(stream));
            }

            if (stream.Length < Constants.SmrcFormat.HeaderSize)
            {
                throw new InvalidDataException($"File too small. S-MRC files must be at least {Constants.SmrcFormat.HeaderSize} bytes");
            }

            long originalPosition = stream.Position;
            try
            {
                // Log the full file bytes
                stream.Position = 0;
                byte[] allBytes = new byte[stream.Length];
                _ = stream.Read(allBytes, 0, allBytes.Length);
                TraceLogger.Log($"S-MRC file bytes (limited to 200): {BitConverter.ToString(allBytes.Take(200).ToArray())}");
                stream.Position = originalPosition;

                using (BinaryReader reader = new BinaryReader(stream, Encoding.ASCII, true))
                {
                    TraceLogger.Log($"Stream position before header: {reader.BaseStream.Position}");

                    // Skip title
                    _ = reader.ReadBytes((int)Constants.SmrcFormat.TitleSize);
                    TraceLogger.Log($"After title: position={reader.BaseStream.Position}");

                    // Check PPQN
                    short ppqn = reader.ReadInt16();
                    TraceLogger.Log($"Read PPQN: {ppqn} at position {reader.BaseStream.Position - 2}");
                    if (ppqn < 24 || ppqn > 480)
                    {
                        throw new InvalidDataException($"Invalid PPQN: {ppqn}. Expected value between 24 and 480.");
                    }

                    // Validate time signature
                    byte numerator = reader.ReadByte();
                    byte denominator = reader.ReadByte();
                    TraceLogger.Log($"Read time signature: {numerator}/{denominator} at position {reader.BaseStream.Position - 2}");
                    if (numerator == 0 || denominator == 0)
                    {
                        throw new InvalidDataException("Time signature numerator and denominator must be non-zero");
                    }

                    if (denominator > 6) // Max 2^6 = 64
                    {
                        throw new InvalidDataException($"Invalid time signature denominator power: {denominator}");
                    }

                    // Validate tempo
                    byte tempo = reader.ReadByte();
                    TraceLogger.Log($"Read tempo: {tempo} at position {reader.BaseStream.Position - 1}");
                    if (tempo is <= 0 or > 255)
                    {
                        throw new InvalidDataException($"Invalid tempo hint: {tempo}. Expected value between 1 and 255.");
                    }

                    // Skip to the end of the header (168 bytes)
                    long toSkip = Constants.SmrcFormat.HeaderSize - reader.BaseStream.Position;
                    if (toSkip < 0)
                    {
                        throw new InvalidDataException($"Stream position {reader.BaseStream.Position} past expected header size {Constants.SmrcFormat.HeaderSize}");
                    }
                    if (toSkip > 0)
                    {
                        reader.ReadBytes((int)toSkip);
                        TraceLogger.Log($"Skipped {toSkip} bytes to reach header size {Constants.SmrcFormat.HeaderSize}");
                    }
                    TraceLogger.Log($"After header reserved: position={reader.BaseStream.Position}");

                    // Ensure we're at the start of the track directory
                    if (reader.BaseStream.Position != Constants.SmrcFormat.HeaderSize)
                    {
                        throw new InvalidDataException($"Expected to be at position {Constants.SmrcFormat.HeaderSize} for track directory, but was at {reader.BaseStream.Position}");
                    }

                    // Log stream position before reading track directory
                    TraceLogger.Log($"Stream position before track directory: {reader.BaseStream.Position}");

                    // Validate track directory
                    int totalTrackLength = 0;
                    List<(int offset, int length)> trackRanges = new List<(int, int)>();
                    for (int i = 0; i < Constants.SmrcFormat.MaxTracks; i++)
                    {
                        // Read and log the raw bytes for offset and length
                        byte[] offsetBytes = reader.ReadBytes(4);
                        byte[] lengthBytes = reader.ReadBytes(4);
                        int offset = BitConverter.ToInt32(offsetBytes, 0);
                        int length = BitConverter.ToInt32(lengthBytes, 0);
                        TraceLogger.Log($"Track {i + 1}: Read offset: {offset} at position {reader.BaseStream.Position - 8}");
                        TraceLogger.Log($"Track {i + 1}: Read length: {length} at position {reader.BaseStream.Position - 4}");
                        _ = reader.ReadBytes((int)Constants.SmrcFormat.TrackDirectoryReservedSize); // Reserved

                        if (length < 0)
                        {
                            throw new InvalidDataException($"Invalid track {i + 1} length: {length}");
                        }

                        TraceLogger.Log($"Track {i + 1}: Checking if offset {offset} < header size {Constants.SmrcFormat.HeaderSize}");
                        if (offset < Constants.SmrcFormat.HeaderSize)
                        {
                            throw new InvalidDataException($"Track {i + 1} offset {offset} overlaps header");
                        }

                        // Check for overlaps with existing tracks
                        int trackEnd = offset + length;
                        foreach ((int existingOffset, int existingLength) in trackRanges)
                        {
                            int existingEnd = existingOffset + existingLength;
                            if (offset < existingEnd && trackEnd > existingOffset)
                            {
                                throw new InvalidDataException($"Track {i + 1} overlaps with another track");
                            }
                        }

                        trackRanges.Add((offset, length));
                        totalTrackLength += length;
                    }
                    // Log stream position after reading track directory
                    TraceLogger.Log($"Stream position after track directory: {reader.BaseStream.Position}");

                    // Verify file size
                    if (reader.BaseStream.Length < Constants.SmrcFormat.HeaderSize + totalTrackLength)
                    {
                        throw new InvalidDataException("File too small for declared track data");
                    }

                    // Validate track data for each non-empty track
                    foreach ((int offset, int length) in trackRanges)
                    {
                        if (length > 0)
                        {
                            reader.BaseStream.Position = offset;
                            byte[] trackBytes = reader.ReadBytes(length);
                            // Log offset, length, and bytes at that location
                            TraceLogger.Log($"Track at offset {offset}, length {length}");
                            if (trackBytes.Length > 32)
                            {
                                string firstBytes = BitConverter.ToString(trackBytes.Take(16).ToArray());
                                string lastBytes = BitConverter.ToString(trackBytes.Skip(Math.Max(0, trackBytes.Length - 16)).Take(16).ToArray());
                                TraceLogger.Log($"  First 16 bytes: {firstBytes}");
                                TraceLogger.Log($"  Last 16 bytes:  {lastBytes}");
                            }
                            else
                            {
                                string trackBytesHex = BitConverter.ToString(trackBytes.Take(200).ToArray());
                                TraceLogger.Log($"  All bytes: {trackBytesHex}");
                            }
                            // For S-MRC, require that the track ends with an End-of-Track event (00-00-FF-2F-00)
                            if (trackBytes.Length > 0)
                            {
                                TraceLogger.Log($"Track at offset {offset} length {length} last 5 bytes: {BitConverter.ToString(trackBytes.Skip(Math.Max(0, trackBytes.Length - 5)).Take(5).ToArray())}");
                            }

                            // Count End-of-Track events
                            int endOfTrackCount = 0;
                            for (int j = 0; j <= trackBytes.Length - 5; j++)
                            {
                                if (trackBytes[j] == 0x00 &&
                                    trackBytes[j + 1] == 0x00 &&
                                    trackBytes[j + 2] == 0xFF &&
                                    trackBytes[j + 3] == 0x2F &&
                                    trackBytes[j + 4] == 0x00)
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
                            if (trackBytes.Length < 5 ||
                                trackBytes[trackBytes.Length - 5] != 0x00 ||
                                trackBytes[trackBytes.Length - 4] != 0x00 ||
                                trackBytes[trackBytes.Length - 3] != 0xFF ||
                                trackBytes[trackBytes.Length - 2] != 0x2F ||
                                trackBytes[trackBytes.Length - 1] != 0x00)
                            {
                                throw new InvalidDataException("Track data must end with an End-of-Track event.");
                            }
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
    }
}
