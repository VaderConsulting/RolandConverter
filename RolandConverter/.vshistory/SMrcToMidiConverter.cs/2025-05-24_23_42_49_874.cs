using System.Diagnostics;
using System.Text;

namespace RolandConverter
{
    /// <summary>
    /// Provides functionality to convert between Roland S-MRC and standard MIDI file formats.
    /// This class handles the core conversion logic for both S-MRC to MIDI and MIDI to S-MRC conversions.
    /// </summary>
    public class SMrcToMidiConverter
    {
        #region Constants

        /// <summary>
        /// The size of the S-MRC header in bytes.
        /// </summary>
        private const int SMRC_HEADER_SIZE = Constants.SmrcFormat.FirstTrackDataOffset;

        /// <summary>
        /// The default PPQN (Pulses Per Quarter Note) value for S-MRC files.
        /// </summary>
        private const short SMRC_PPQN = Constants.MidiFormat.DefaultPpqn;

        /// <summary>
        /// The maximum number of tracks supported in an S-MRC file.
        /// </summary>
        private const int SMRC_TRACK_COUNT = Constants.SmrcFormat.MaxTracks;

        /// <summary>
        /// The standard MIDI header chunk identifier.
        /// </summary>
        private const string STANDARD_MIDI_HEADER = "MThd";

        /// <summary>
        /// The standard MIDI track chunk identifier.
        /// </summary>
        private const string STANDARD_MIDI_TRACK = "MTrk";

        #endregion

        #region Fields

        /// <summary>
        /// The input stream containing the source file data.
        /// </summary>
        private readonly Stream input;

        /// <summary>
        /// The output stream where the converted data will be written.
        /// </summary>
        private readonly Stream output;

        /// <summary>
        /// The binary writer for writing to the output stream.
        /// </summary>
        private readonly BinaryWriter writer;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the SMrcToMidiConverter class.
        /// </summary>
        /// <param name="inputStream">The input stream containing the source file data.</param>
        /// <param name="outputStream">The output stream where the converted data will be written.</param>
        /// <exception cref="ArgumentNullException">Thrown when either inputStream or outputStream is null.</exception>
        public SMrcToMidiConverter(Stream inputStream, Stream outputStream)
        {
            input = inputStream ?? throw new ArgumentNullException(nameof(inputStream));
            output = outputStream ?? throw new ArgumentNullException(nameof(outputStream));
            writer = new BinaryWriter(outputStream);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Converts MIDI track data to S-MRC format and writes it to the output stream.
        /// </summary>
        /// <param name="writer">The binary writer for the output stream.</param>
        /// <param name="midiTracks">The list of MIDI tracks to convert.</param>
        /// <param name="division">The MIDI file's time division value.</param>
        private void ConvertMidiToSmrcFormat(BinaryWriter writer, List<MidiTrack> midiTracks, short division)
        {
            // Parse MIDI tracks to extract events
            List<SmrcTrack> smrcTracks = [];
            string songTitle = ""; // Only set if a track name meta event is found
            byte? timeSignatureNumerator = null;
            byte? timeSignatureDenominator = null;
            byte? tempoBpm = null;

            for (int i = 0; i < midiTracks.Count && i < SMRC_TRACK_COUNT; i++)
            {
                SmrcTrack smrcTrack = ParseMidiTrack(midiTracks[i].Data, division, i == 0,
                    ref songTitle, ref timeSignatureNumerator, ref timeSignatureDenominator, ref tempoBpm);
                smrcTracks.Add(smrcTrack);
            }

            // Pad with empty tracks if needed
            while (smrcTracks.Count < SMRC_TRACK_COUNT)
            {
                smrcTracks.Add(new SmrcTrack { Events = [] });
            }

            // Write S-MRC header
            WriteSmrcHeader(songTitle, division, timeSignatureNumerator, timeSignatureDenominator, tempoBpm);

            // Calculate track offsets and write track directory
            int currentOffset = SMRC_HEADER_SIZE;
            List<byte[]> trackDataBuffers = [];

            for (int i = 0; i < SMRC_TRACK_COUNT; i++)
            {
                byte[] trackData = ConvertTrackToSmrcFormat(smrcTracks[i]);
                trackDataBuffers.Add(trackData);

                writer.Write(currentOffset); // Start offset
                writer.Write(trackData.Length); // Length
                writer.Write(new byte[8]); // Reserved

                currentOffset += trackData.Length;
            }

            // Write track data
            foreach (byte[] trackData in trackDataBuffers)
            {
                writer.Write(trackData);
            }
        }

        /// <summary>
        /// Converts S-MRC data to standard MIDI format.
        /// </summary>
        /// <param name="header">The S-MRC header information.</param>
        /// <param name="smrcTracks">The list of S-MRC tracks to convert.</param>
        /// <returns>A MidiFileData object containing the converted MIDI data.</returns>
        private MidiFileData ConvertSmrcToMidiData(SmrcHeader header, List<SmrcTrack> smrcTracks)
        {
            TraceLogger.Log("SMrcToMidiConverter", "Converting S-MRC data to MIDI format");
            MidiFileData midiData = new MidiFileData();

            foreach (SmrcTrack smrcTrack in smrcTracks)
            {
                // Only add non-empty tracks (not just a single End of Track event)
                bool isNonEmpty = smrcTrack.Events.Any(evt => !(evt.Status == 0xFF && evt.Data != null && evt.Data.Length == 1 && evt.Data[0] == 0x2F));
                // If the only event is End of Track, skip this track
                if (smrcTrack.Events.Count == 1 && smrcTrack.Events[0].Status == 0xFF && smrcTrack.Events[0].Data != null && smrcTrack.Events[0].Data.Length == 1 && smrcTrack.Events[0].Data[0] == 0x2F)
                {
                    TraceLogger.Log("SMrcToMidiConverter", "ConvertSmrcToMidiData: Skipping empty track (only End of Track event)");
                    continue;
                }

                using (MemoryStream trackStream = new MemoryStream())
                using (BinaryWriter trackWriter = new BinaryWriter(trackStream))
                {
                    TraceLogger.Log("SMrcToMidiConverter", $"Processing S-MRC track with {smrcTrack.Events.Count} events");

                    bool hasTrackName = false;
                    bool hasEndOfTrack = false;

                    foreach (SmrcEvent evt in smrcTrack.Events)
                    {
                        // Check for track name meta event
                        if (evt.Status == 0xFF && evt.Data != null && evt.Data.Length > 0 && evt.Data[0] == 0x03)
                        {
                            hasTrackName = true;
                            TraceLogger.Log("SMrcToMidiConverter", $"Found track name meta event in S-MRC track");
                        }

                        // Check for end of track meta event
                        if (evt.Status == 0xFF && evt.Data != null && evt.Data.Length > 0 && evt.Data[0] == 0x2F)
                        {
                            hasEndOfTrack = true;
                        }

                        // Write variable-length delta time (MIDI format)
                        // S-MRC delta times are already in the correct scale since both formats use 96 PPQN
                        WriteVariableLengthValue(trackWriter, evt.DeltaTime);

                        // Handle Roland meta events
                        if (evt.Status == 0xF7 && evt.RolandMetaType.HasValue)
                        {
                            TraceLogger.Log("SMrcToMidiConverter", $"Converting Roland meta event type {evt.RolandMetaType}");
                            // Convert Roland meta to standard MIDI meta
                            trackWriter.Write((byte)0xFF); // Meta event

                            if (evt.RolandMetaType == 0x01) // Tempo
                            {
                                trackWriter.Write((byte)0x51); // Set Tempo
                                trackWriter.Write((byte)0x03); // Length
                                // Convert little-endian to big-endian
                                int microsecondsPerQuarter = evt.RolandMetaData[0] | (evt.RolandMetaData[1] << 8);
                                trackWriter.Write((byte)(microsecondsPerQuarter >> 16));
                                trackWriter.Write((byte)(microsecondsPerQuarter >> 8));
                                trackWriter.Write((byte)microsecondsPerQuarter);
                            }
                            else if (evt.RolandMetaType == 0x02) // Time signature
                            {
                                trackWriter.Write((byte)0x58); // Time Signature
                                trackWriter.Write((byte)0x04); // Length
                                trackWriter.Write(evt.RolandMetaData[0]); // Numerator
                                trackWriter.Write(evt.RolandMetaData[1]); // Denominator
                                trackWriter.Write((byte)24); // Clocks per metronome click
                                trackWriter.Write((byte)8); // 32nd notes per quarter note
                            }
                        }
                        else if (evt.Status == 0xFF) // Standard MIDI meta event
                        {
                            // Skip meta events that are already included in a SysEx event
                            if (evt.Data[0] == 0x51 || evt.Data[0] == 0x2F)
                            {
                                continue;
                            }

                            TraceLogger.Log("SMrcToMidiConverter", $"Writing standard MIDI meta event type=0x{evt.Data[0]:X2}, length={evt.Data.Length - 1}");
                            trackWriter.Write(evt.Status);
                            if (evt.Data != null && evt.Data.Length > 0)
                            {
                                trackWriter.Write(evt.Data[0]); // Meta type
                                if (evt.Data.Length > 1)
                                {
                                    WriteVariableLengthValue(trackWriter, evt.Data.Length - 1); // Length as variable-length value
                                    trackWriter.Write(evt.Data, 1, evt.Data.Length - 1); // Data
                                    TraceLogger.Log("SMrcToMidiConverter", $"Wrote meta event data: {BitConverter.ToString(evt.Data, 1, evt.Data.Length - 1)}");
                                }
                                else
                                {
                                    WriteVariableLengthValue(trackWriter, 0); // Length as variable-length value for empty meta events
                                    TraceLogger.Log("SMrcToMidiConverter", "Wrote empty meta event");
                                }
                            }
                        }
                        else
                        {
                            // Write status and data bytes
                            byte channel = (byte)(evt.Status & 0x0F);
                            string midiEventType = evt.Status switch
                            {
                                var s when s is >= 0x80 and <= 0x8F => "Note Off",
                                var s when s is >= 0x90 and <= 0x9F => "Note On",
                                var s when s is >= 0xA0 and <= 0xAF => "Poly Aftertouch",
                                var s when s is >= 0xB0 and <= 0xBF => "Control Change",
                                var s when s is >= 0xC0 and <= 0xCF => "Program Change",
                                var s when s is >= 0xD0 and <= 0xDF => "Channel Aftertouch",
                                var s when s is >= 0xE0 and <= 0xEF => "Pitch Bend",
                                _ => "Unknown MIDI Event"
                            };
                            TraceLogger.Log("SMrcToMidiConverter", $"ConvertTrackToSmrcFormat: Writing event status=0x{evt.Status:X2} ({midiEventType} on channel {channel}), delta={evt.DeltaTime}");
                            trackWriter.Write(evt.Status);
                            if (evt.Data != null)
                            {
                                trackWriter.Write(evt.Data);
                                TraceLogger.Log("SMrcToMidiConverter", $"ConvertTrackToSmrcFormat: Writing event data: {BitConverter.ToString(evt.Data)}");
                            }
                        }
                    }

                    // Add track name if not present and this is the first track, but only if no track name meta event exists
                    if (!hasTrackName && midiData.Tracks.Count == 0)
                    {
                        // Do not add a default track name meta event; preserve only what was in the original
                        // (No action)
                    }

                    // Add end of track meta event if one doesn't exist
                    if (!hasEndOfTrack)
                    {
                        trackWriter.Write(GetBigEndianBytes((short)0)); // Delta time
                        trackWriter.Write((byte)0xFF); // Meta event
                        trackWriter.Write((byte)0x2F); // End of track
                        WriteVariableLengthValue(trackWriter, 0); // Length as variable-length value
                        TraceLogger.Log("SMrcToMidiConverter", "Added end of track meta event");
                    }

                    midiData.Tracks.Add(new MidiTrack { Data = trackStream.ToArray() });
                }
            }

            return midiData;
        }

        /// <summary>
        /// Converts a SmrcTrack to its binary S-MRC format representation.
        /// </summary>
        /// <param name="track">The SmrcTrack to convert.</param>
        /// <returns>A byte array containing the track data in S-MRC format.</returns>
        private byte[] ConvertTrackToSmrcFormat(SmrcTrack track)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                TraceLogger.Log("SMrcToMidiConverter", $"ConvertTrackToSmrcFormat: Converting track with {track.Events.Count} events");
                foreach (SmrcEvent evt in track.Events)
                {
                    // DEBUG: Log event before writing
                    TraceLogger.Log("SMrcToMidiConverter", $"DEBUG: About to write event: delta={evt.DeltaTime}, status=0x{evt.Status:X2}, data={(evt.Data != null ? BitConverter.ToString(evt.Data) : "null")}");
                    // Write 16-bit big-endian delta time
                    writer.Write(GetBigEndianBytes((short)evt.DeltaTime));

                    // Write status byte (no running status in S-MRC)
                    writer.Write(evt.Status);

                    // Write status and data bytes
                    string eventDescription;
                    if (evt.Status == 0xFF)
                    {
                        eventDescription = "Meta Event";
                    }
                    else if (evt.Status == 0xF7)
                    {
                        eventDescription = "Roland Meta Event";
                    }
                    else if (evt.Status is >= 0x80 and <= 0xEF)
                    {
                        byte channel = (byte)(evt.Status & 0x0F);
                        string midiEventType = evt.Status switch
                        {
                            var s when s is >= 0x80 and <= 0x8F => "Note Off",
                            var s when s is >= 0x90 and <= 0x9F => "Note On",
                            var s when s is >= 0xA0 and <= 0xAF => "Poly Aftertouch",
                            var s when s is >= 0xB0 and <= 0xBF => "Control Change",
                            var s when s is >= 0xC0 and <= 0xCF => "Program Change",
                            var s when s is >= 0xD0 and <= 0xDF => "Channel Aftertouch",
                            var s when s is >= 0xE0 and <= 0xEF => "Pitch Bend",
                            _ => "Unknown MIDI Event"
                        };
                        eventDescription = $"{midiEventType} on channel {channel}";
                    }
                    else
                    {
                        eventDescription = "Unknown Event";
                    }

                    TraceLogger.Log("SMrcToMidiConverter", $"ConvertTrackToSmrcFormat: Writing event status=0x{evt.Status:X2} ({eventDescription}), delta={evt.DeltaTime}");

                    // Handle Roland meta events
                    if (evt.RolandMetaType.HasValue)
                    {
                        writer.Write((byte)0x7D);
                        writer.Write(evt.RolandMetaType.Value);
                        if (evt.RolandMetaData != null)
                        {
                            writer.Write(evt.RolandMetaData);
                            TraceLogger.Log("SMrcToMidiConverter", $"ConvertTrackToSmrcFormat: Writing Roland meta event type=0x{evt.RolandMetaType:X2}, data={BitConverter.ToString(evt.RolandMetaData)}");
                        }
                    }
                    // Handle standard MIDI meta events (0xFF)
                    else if (evt.Status == 0xFF && evt.Data != null && evt.Data.Length > 0)
                    {
                        // Skip meta events that are already included in a SysEx event
                        if (evt.Data[0] == 0x51 || evt.Data[0] == 0x2F)
                        {
                            continue;
                        }

                        // Write meta type byte
                        writer.Write(evt.Data[0]);
                        // Write length as a variable-length value
                        WriteVariableLengthValue(writer, evt.Data.Length - 1);
                        // Write meta data (excluding the meta type byte)
                        if (evt.Data.Length > 1)
                        {
                            writer.Write(evt.Data, 1, evt.Data.Length - 1);
                            TraceLogger.Log("SMrcToMidiConverter", $"ConvertTrackToSmrcFormat: Writing meta event type=0x{evt.Data[0]:X2}, length={evt.Data.Length - 1}, data={BitConverter.ToString(evt.Data, 1, evt.Data.Length - 1)}");
                        }
                        else
                        {
                            WriteVariableLengthValue(writer, 0); // Length as variable-length value for empty meta events
                            TraceLogger.Log("SMrcToMidiConverter", "Wrote empty meta event");
                        }

                        if (evt.Data[0] == 0x03)
                        {
                            string trackName = Encoding.ASCII.GetString(evt.Data, 1, evt.Data.Length - 1);
                            TraceLogger.Log("SMrcToMidiConverter", $"ConvertTrackToSmrcFormat: Writing track name meta event: '{trackName}'");
                        }
                    }
                    // Write data bytes for other events
                    else if (evt.Data != null)
                    {
                        writer.Write(evt.Data);
                        TraceLogger.Log("SMrcToMidiConverter", $"ConvertTrackToSmrcFormat: Writing event data: {BitConverter.ToString(evt.Data)}");
                    }
                }

                // Add end of track meta event if not already present
                bool hasEndOfTrack = track.Events.Any(e => e.Status == 0xFF && e.Data != null && e.Data.Length > 0 && e.Data[0] == 0x2F);
                if (!hasEndOfTrack)
                {
                    TraceLogger.Log("SMrcToMidiConverter", "ConvertTrackToSmrcFormat: Adding end of track meta event");
                    writer.Write(GetBigEndianBytes((short)0)); // Delta time
                    writer.Write((byte)0xFF); // Meta event
                    writer.Write((byte)0x2F); // End of track
                    WriteVariableLengthValue(writer, 0); // Length as variable-length value
                }

                byte[] result = stream.ToArray();
                TraceLogger.Log("SMrcToMidiConverter", $"ConvertTrackToSmrcFormat: Raw track data: {BitConverter.ToString(result)}");
                return result;
            }
        }

        /// <summary>
        /// Converts a 32-bit integer to a big-endian byte array.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>A 4-byte array containing the big-endian representation.</returns>
        private byte[] GetBigEndianBytes(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        /// <summary>
        /// Converts a 16-bit integer to a big-endian byte array.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>A 2-byte array containing the big-endian representation.</returns>
        private byte[] GetBigEndianBytes(short value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        /// <summary>
        /// Gets the expected data length for a MIDI status byte.
        /// </summary>
        /// <param name="status">The MIDI status byte.</param>
        /// <returns>The number of data bytes expected for this status byte.</returns>
        private int GetMidiDataLength(byte status)
        {
            if (status < 0x80)
            {
                return 0; // Invalid
            }

            if (status < 0xC0)
            {
                return 2; // Note on/off, aftertouch, control change
            }

            if (status < 0xE0)
            {
                return 1; // Program change, channel pressure
            }

            if (status < 0xF0)
            {
                return 2; // Pitch bend
            }

            if (status is 0xF0 or 0xF7)
            {
                return -1; // SysEx (variable length)
            }

            return 0; // System common/realtime
        }

        /// <summary>
        /// Parses a MIDI track and converts it to S-MRC format.
        /// </summary>
        /// <param name="trackData">The raw MIDI track data.</param>
        /// <param name="division">The MIDI file's time division value.</param>
        /// <param name="isFirstTrack">Whether this is the first track in the file.</param>
        /// <param name="songTitle">Reference to the song title, which may be updated from meta events.</param>
        /// <param name="timeSignatureNumerator">Reference to the time signature numerator.</param>
        /// <param name="timeSignatureDenominator">Reference to the time signature denominator.</param>
        /// <param name="tempoBpm">Reference to the tempo in BPM.</param>
        /// <returns>A SmrcTrack containing the converted events.</returns>
        private SmrcTrack ParseMidiTrack(byte[] trackData, short division, bool isFirstTrack,
            ref string songTitle, ref byte? timeSignatureNumerator, ref byte? timeSignatureDenominator, ref byte? tempoBpm)
        {
            SmrcTrack track = new SmrcTrack();
            int pos = 0;
            int accumulatedDelta = 0;
            byte runningStatus = 0;
            SmrcEvent currentEvent;

            Debug.WriteLine($"ParseMidiTrack: Starting parse of track (isFirstTrack={isFirstTrack}), trackData.Length={trackData.Length}");

            while (pos < trackData.Length)
            {
                Debug.WriteLine($"ParseMidiTrack: Top of loop, pos={pos}, accumulatedDelta={accumulatedDelta}");
                // Read variable-length delta time
                int deltaTime = ReadVariableLengthValue(trackData, ref pos);
                Debug.WriteLine($"ParseMidiTrack: After ReadVariableLengthValue, pos={pos}, deltaTime={deltaTime}");
                accumulatedDelta += deltaTime;
                Debug.WriteLine($"ParseMidiTrack: Read delta time {deltaTime} (accumulated to {accumulatedDelta}), pos={pos}");

                // Read status byte
                byte status = trackData[pos];
                Debug.WriteLine($"ParseMidiTrack: Read status byte 0x{status:X2} at pos={pos}");

                if (status < 0x80)
                {
                    Debug.WriteLine($"ParseMidiTrack: Using running status 0x{runningStatus:X2} at pos={pos}");
                    status = runningStatus;
                }
                else
                {
                    Debug.WriteLine($"ParseMidiTrack: New status byte 0x{status:X2} at pos={pos + 1}");
                    runningStatus = status;
                    pos++;
                }

                if (status == 0xFF) // Meta event
                {
                    if (pos >= trackData.Length)
                    {
                        break;
                    }

                    byte metaType = trackData[pos++];
                    int metaLen = ReadVariableLengthValue(trackData, ref pos);
                    if (pos + metaLen > trackData.Length)
                    {
                        break;
                    }

                    byte[] data = new byte[metaLen + 1];
                    data[0] = metaType;
                    Array.Copy(trackData, pos, data, 1, metaLen);
                    currentEvent = new SmrcEvent
                    {
                        DeltaTime = accumulatedDelta,
                        Status = 0xFF,
                        Data = data
                    };
                    track.Events.Add(currentEvent);
                    accumulatedDelta = 0;

                    // Skip this meta event if it's already included in a SysEx event
                    if (metaType == 0x51 || metaType == 0x2F)
                    {
                        continue;
                    }
                }
                else if (status == 0xF7) // Roland meta event
                {
                    if (pos >= trackData.Length)
                    {
                        break;
                    }

                    currentEvent = new SmrcEvent
                    {
                        DeltaTime = accumulatedDelta,
                        Status = 0xF7,
                        RolandMetaType = trackData[pos++]
                    };
                    int metaLen = ReadVariableLengthValue(trackData, ref pos);
                    if (pos + metaLen > trackData.Length)
                    {
                        break;
                    }

                    currentEvent.RolandMetaData = new byte[metaLen];
                    Array.Copy(trackData, pos, currentEvent.RolandMetaData, 0, metaLen);
                    track.Events.Add(currentEvent);
                    accumulatedDelta = 0;
                }
                else if (status is >= 0x80 and <= 0xEF) // MIDI event
                {
                    int dataLen = GetMidiDataLength(status);
                    if (pos + dataLen > trackData.Length)
                    {
                        break;
                    }

                    currentEvent = new SmrcEvent
                    {
                        DeltaTime = accumulatedDelta,
                        Status = status
                    };
                    if (dataLen > 0)
                    {
                        currentEvent.Data = new byte[dataLen];
                        Array.Copy(trackData, pos, currentEvent.Data, 0, dataLen);
                        pos += dataLen;
                    }
                    track.Events.Add(currentEvent);
                    accumulatedDelta = 0;
                }
                else if (status == 0xF0) // SysEx event
                {
                    int sysexLen = ReadVariableLengthValue(trackData, ref pos);
                    Debug.WriteLine($"ParseMidiTrack: Found SysEx event at pos={pos}, length={sysexLen}, current pos={pos}");

                    // Check if we have enough data for the SysEx event
                    if (pos + sysexLen > trackData.Length)
                    {
                        Debug.WriteLine($"ParseMidiTrack: SysEx event data extends beyond track end at position {pos}, using available data");
                        sysexLen = trackData.Length - pos;
                    }

                    // Process embedded meta events within SysEx data
                    int sysexStartPos = pos;
                    int sysexEndPos = pos + sysexLen;
                    bool foundEndOfTrack = false;
                    SmrcEvent metaEvent;
                    List<SmrcEvent> embeddedEvents = new List<SmrcEvent>();
                    List<int> metaEventPositions = new List<int>();

                    while (pos < sysexEndPos)
                    {
                        if (trackData[pos] == 0xFF) // Meta event
                        {
                            // Check if we have enough bytes for meta type and length
                            if (pos + 2 >= trackData.Length)
                            {
                                Debug.WriteLine($"ParseMidiTrack: Unexpected end of track while reading meta event in SysEx at pos={pos}");
                                break;
                            }

                            byte metaType = trackData[pos + 1];
                            int metaLen = ReadVariableLengthValue(trackData, ref pos);
                            Debug.WriteLine($"ParseMidiTrack: Found embedded meta event type=0x{metaType:X2}, length={metaLen} at pos={pos}");

                            // Check if we have enough data for the meta event
                            if (pos + metaLen > trackData.Length)
                            {
                                Debug.WriteLine($"ParseMidiTrack: Meta event data extends beyond track end at position {pos}, using available data");
                                metaLen = trackData.Length - pos;
                            }

                            if (metaType == 0x2F) // End of track
                            {
                                Debug.WriteLine("ParseMidiTrack: Found end of track meta event in SysEx, breaking");
                                foundEndOfTrack = true;
                                break;
                            }
                            else if (metaType == 0x51) // Tempo
                            {
                                // Convert tempo event to Roland format
                                byte[] tempoData = new byte[metaLen];
                                Array.Copy(trackData, pos, tempoData, 0, metaLen);
                                metaEvent = new SmrcEvent
                                {
                                    DeltaTime = accumulatedDelta,
                                    Status = 0xFF,
                                    Data = tempoData
                                };
                                embeddedEvents.Add(metaEvent);
                                metaEventPositions.Add(pos - 2); // Store the start position of the meta event
                                accumulatedDelta = 0;
                            }
                            pos += metaLen;
                        }
                        else
                        {
                            pos++;
                        }
                    }

                    // Add the embedded events first
                    foreach (var embeddedEvent in embeddedEvents)
                    {
                        track.Events.Add(embeddedEvent);
                    }

                    // Only add the SysEx event if it contains data other than the embedded events
                    if (sysexLen > 0 && !foundEndOfTrack)
                    {
                        // Create a new array excluding the embedded meta events
                        List<byte> sysexData = new List<byte>();
                        for (int j = sysexStartPos; j < sysexEndPos; j++)
                        {
                            // Skip meta events that we've already processed
                            if (metaEventPositions.Contains(j))
                            {
                                // Skip the meta event header (0xFF) and type byte
                                j += 2;
                                // Skip the length bytes
                                while (j < sysexEndPos && (trackData[j] & 0x80) != 0)
                                {
                                    j++;
                                }
                                j++;
                                // Skip the meta event data
                                int metaLen = ReadVariableLengthValue(trackData, ref j);
                                j += metaLen - 1;
                                continue;
                            }
                            sysexData.Add(trackData[j]);
                        }

                        if (sysexData.Count > 0)
                        {
                            metaEvent = new SmrcEvent
                            {
                                DeltaTime = accumulatedDelta,
                                Status = 0xF0,
                                Data = sysexData.ToArray()
                            };
                            track.Events.Add(metaEvent);
                            accumulatedDelta = 0;
                        }
                    }
                }
                else
                {
                    int dataLen = GetMidiDataLength(status);
                    if (dataLen > 0)
                    {
                        Debug.WriteLine($"ParseMidiTrack: Found MIDI event type=0x{status:X2}, data length={dataLen}, pos={pos}");
                        byte[] eventData = new byte[dataLen];
                        Array.Copy(trackData, pos, eventData, 0, dataLen);
                        Debug.WriteLine($"ParseMidiTrack: Copying MIDI event data from pos={pos} to pos={pos + dataLen}");
                        pos += dataLen;

                        currentEvent = new SmrcEvent
                        {
                            DeltaTime = accumulatedDelta,
                            Status = status,
                            Data = eventData
                        };
                        track.Events.Add(currentEvent);
                        Debug.WriteLine($"ParseMidiTrack: Added MIDI event with data: {BitConverter.ToString(eventData)} at pos={pos}");
                        accumulatedDelta = 0;
                    }
                    else
                    {
                        Debug.WriteLine($"ParseMidiTrack: Skipping unknown event type=0x{status:X2} at pos={pos}");
                        pos++;
                    }
                }
            }

            Debug.WriteLine($"ParseMidiTrack: Completed parsing track with {track.Events.Count} events, final pos={pos}");
            return track;
        }

        /// <summary>
        /// Reads a 16-bit integer in big-endian format from the input stream.
        /// </summary>
        /// <param name="reader">The binary reader for the input stream.</param>
        /// <returns>The 16-bit integer value.</returns>
        /// <exception cref="InvalidDataException">Thrown when the data is invalid or incomplete.</exception>
        private short ReadInt16BigEndian(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(2);
            if (bytes.Length < 2)
            {
                throw new InvalidDataException("Unexpected end of data while reading 16-bit value");
            }

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return BitConverter.ToInt16(bytes, 0);
        }

        /// <summary>
        /// Reads a 32-bit integer in big-endian format from the input stream.
        /// </summary>
        /// <param name="reader">The binary reader for the input stream.</param>
        /// <returns>The 32-bit integer value.</returns>
        /// <exception cref="InvalidDataException">Thrown when the data is invalid or incomplete.</exception>
        private int ReadInt32BigEndian(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(4);
            if (bytes.Length < 4)
            {
                throw new InvalidDataException("Unexpected end of data while reading 32-bit value");
            }

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return BitConverter.ToInt32(bytes, 0);
        }

        /// <summary>
        /// Reads the S-MRC header from the input stream.
        /// </summary>
        /// <param name="reader">The binary reader for the input stream.</param>
        /// <returns>A SmrcHeader object containing the header information.</returns>
        /// <exception cref="InvalidDataException">Thrown when the header data is invalid.</exception>
        private SmrcHeader ReadSmrcHeader(BinaryReader reader)
        {
            // Read title (32 bytes)
            byte[] titleBytes = reader.ReadBytes(32);
            string title = Encoding.ASCII.GetString(titleBytes).TrimEnd('\0');

            // Read PPQN (2 bytes)
            short ppqn = reader.ReadInt16();
            if (ppqn < 24 || ppqn > 480)
            {
                throw new InvalidDataException($"Invalid PPQN: {ppqn}. Expected value between 24 and 480.");
            }

            // Read time signature (2 bytes)
            byte numerator = reader.ReadByte();
            byte denominator = reader.ReadByte();
            if (denominator > 6) // Max 2^6 = 64
            {
                throw new InvalidDataException($"Invalid time signature denominator power: {denominator}");
            }

            // Read tempo (1 byte)
            byte tempo = reader.ReadByte();
            if (tempo is <= 0 or > 255)
            {
                throw new InvalidDataException($"Invalid tempo hint: {tempo}. Expected value between 1 and 255.");
            }

            // Skip reserved bytes (3 bytes)
            _ = reader.ReadBytes(3);

            // Read track directory
            var trackDirectory = new TrackDirectoryEntry[SMRC_TRACK_COUNT];
            for (int i = 0; i < SMRC_TRACK_COUNT; i++)
            {
                int offset = reader.ReadInt32();
                int length = reader.ReadInt32();
                _ = reader.ReadBytes(8); // Skip reserved bytes

                trackDirectory[i] = new TrackDirectoryEntry
                {
                    Offset = offset,
                    Length = length
                };
            }

            return new SmrcHeader
            {
                Title = title,
                Ppqn = ppqn,
                TimeSignatureNumerator = numerator,
                TimeSignatureDenominator = denominator,
                TempoBpm = tempo,
                TrackDirectory = trackDirectory
            };
        }

        /// <summary>
        /// Reads the S-MRC tracks from the input stream.
        /// </summary>
        /// <param name="reader">The binary reader for the input stream.</param>
        /// <param name="header">The S-MRC header information.</param>
        /// <returns>A list of SmrcTrack objects containing the track data.</returns>
        /// <exception cref="InvalidDataException">Thrown when the track data is invalid.</exception>
        private List<SmrcTrack> ReadSmrcTracks(BinaryReader reader, SmrcHeader header)
        {
            List<SmrcTrack> tracks = [];
            for (int i = 0; i < SMRC_TRACK_COUNT; i++)
            {
                TrackDirectoryEntry entry = header.TrackDirectory[i];
                if (entry.Length == 0)
                {
                    // Empty track
                    tracks.Add(new SmrcTrack { Events = [] });
                    continue;
                }

                // Validate track offset and length
                if (entry.Offset < SMRC_HEADER_SIZE)
                {
                    throw new InvalidDataException($"Invalid track offset {entry.Offset} for track {i}. Must be at least {SMRC_HEADER_SIZE}.");
                }

                if (entry.Offset + entry.Length > reader.BaseStream.Length)
                {
                    throw new InvalidDataException($"Track {i} data extends beyond file end. Offset: {entry.Offset}, Length: {entry.Length}, File Size: {reader.BaseStream.Length}");
                }

                // Seek to track data
                reader.BaseStream.Position = entry.Offset;
                byte[] trackData = reader.ReadBytes(entry.Length);

                // Parse track data
                SmrcTrack track = new SmrcTrack { Events = [] };
                int pos = 0;
                int accumulatedDelta = 0;
                while (pos < trackData.Length)
                {
                    // Read delta time (2 bytes, big-endian)
                    if (pos + 2 > trackData.Length)
                    {
                        break;
                    }

                    short deltaTime = (short)((trackData[pos] << 8) | trackData[pos + 1]);
                    pos += 2;
                    accumulatedDelta += deltaTime;

                    // Check if we have enough bytes for status byte
                    if (pos >= trackData.Length)
                    {
                        break;
                    }

                    byte status = trackData[pos++];
                    SmrcEvent evt = new SmrcEvent { DeltaTime = deltaTime, Status = status };

                    // Handle different event types
                    if (status == 0xFF) // Meta event
                    {
                        if (pos >= trackData.Length)
                        {
                            break;
                        }

                        byte metaType = trackData[pos++];
                        int metaLen = ReadVariableLengthValue(trackData, ref pos);
                        if (pos + metaLen > trackData.Length)
                        {
                            break;
                        }

                        byte[] data = new byte[metaLen + 1];
                        data[0] = metaType;
                        Array.Copy(trackData, pos, data, 1, metaLen);
                        evt.Data = data;
                        pos += metaLen;
                    }
                    else if (status == 0xF7) // Roland meta event
                    {
                        if (pos >= trackData.Length)
                        {
                            break;
                        }

                        evt.RolandMetaType = trackData[pos++];
                        int metaLen = ReadVariableLengthValue(trackData, ref pos);
                        if (pos + metaLen > trackData.Length)
                        {
                            break;
                        }

                        evt.RolandMetaData = new byte[metaLen];
                        Array.Copy(trackData, pos, evt.RolandMetaData, 0, metaLen);
                        pos += metaLen;
                    }
                    else if (status is >= 0x80 and <= 0xEF) // MIDI event
                    {
                        int dataLen = GetMidiDataLength(status);
                        if (pos + dataLen > trackData.Length)
                        {
                            break;
                        }

                        evt.Data = new byte[dataLen];
                        Array.Copy(trackData, pos, evt.Data, 0, dataLen);
                        pos += dataLen;
                    }
                    else if (status == 0xF0) // SysEx event
                    {
                        int sysexLen = ReadVariableLengthValue(trackData, ref pos);
                        Debug.WriteLine($"ParseMidiTrack: Found SysEx event at pos={pos}, length={sysexLen}, current pos={pos}");

                        // Check if we have enough data for the SysEx event
                        if (pos + sysexLen > trackData.Length)
                        {
                            Debug.WriteLine($"ParseMidiTrack: SysEx event data extends beyond track end at position {pos}, using available data");
                            sysexLen = trackData.Length - pos;
                        }

                        // Process embedded meta events within SysEx data
                        int sysexStartPos = pos;
                        int sysexEndPos = pos + sysexLen;
                        bool foundEndOfTrack = false;
                        SmrcEvent metaEvent;
                        List<SmrcEvent> embeddedEvents = new List<SmrcEvent>();
                        List<int> metaEventPositions = new List<int>();

                        while (pos < sysexEndPos)
                        {
                            if (trackData[pos] == 0xFF) // Meta event
                            {
                                // Check if we have enough bytes for meta type and length
                                if (pos + 2 >= trackData.Length)
                                {
                                    Debug.WriteLine($"ParseMidiTrack: Unexpected end of track while reading meta event in SysEx at pos={pos}");
                                    break;
                                }

                                byte metaType = trackData[pos + 1];
                                int metaLen = ReadVariableLengthValue(trackData, ref pos);
                                Debug.WriteLine($"ParseMidiTrack: Found embedded meta event type=0x{metaType:X2}, length={metaLen} at pos={pos}");

                                // Check if we have enough data for the meta event
                                if (pos + metaLen > trackData.Length)
                                {
                                    Debug.WriteLine($"ParseMidiTrack: Meta event data extends beyond track end at position {pos}, using available data");
                                    metaLen = trackData.Length - pos;
                                }

                                if (metaType == 0x2F) // End of track
                                {
                                    Debug.WriteLine("ParseMidiTrack: Found end of track meta event in SysEx, breaking");
                                    foundEndOfTrack = true;
                                    break;
                                }
                                else if (metaType == 0x51) // Tempo
                                {
                                    // Convert tempo event to Roland format
                                    byte[] tempoData = new byte[metaLen];
                                    Array.Copy(trackData, pos, tempoData, 0, metaLen);
                                    metaEvent = new SmrcEvent
                                    {
                                        DeltaTime = accumulatedDelta,
                                        Status = 0xFF,
                                        Data = tempoData
                                    };
                                    embeddedEvents.Add(metaEvent);
                                    metaEventPositions.Add(pos - 2); // Store the start position of the meta event
                                    accumulatedDelta = 0;
                                }
                                pos += metaLen;
                            }
                            else
                            {
                                pos++;
                            }
                        }

                        // Add the embedded events first
                        foreach (var embeddedEvent in embeddedEvents)
                        {
                            track.Events.Add(embeddedEvent);
                        }

                        // Only add the SysEx event if it contains data other than the embedded events
                        if (sysexLen > 0 && !foundEndOfTrack)
                        {
                            // Create a new array excluding the embedded meta events
                            List<byte> sysexData = new List<byte>();
                            for (int j = sysexStartPos; j < sysexEndPos; j++)
                            {
                                // Skip meta events that we've already processed
                                if (metaEventPositions.Contains(j))
                                {
                                    // Skip the meta event header (0xFF) and type byte
                                    j += 2;
                                    // Skip the length bytes
                                    while (j < sysexEndPos && (trackData[j] & 0x80) != 0)
                                    {
                                        j++;
                                    }
                                    j++;
                                    // Skip the meta event data
                                    int metaLen = ReadVariableLengthValue(trackData, ref j);
                                    j += metaLen - 1;
                                    continue;
                                }
                                sysexData.Add(trackData[j]);
                            }

                            if (sysexData.Count > 0)
                            {
                                metaEvent = new SmrcEvent
                                {
                                    DeltaTime = accumulatedDelta,
                                    Status = 0xF0,
                                    Data = sysexData.ToArray()
                                };
                                track.Events.Add(metaEvent);
                                accumulatedDelta = 0;
                            }
                        }
                    }

                    track.Events.Add(evt);
                }

                tracks.Add(track);
            }

            return tracks;
        }

        /// <summary>
        /// Reads a variable-length value from a byte array.
        /// </summary>
        /// <param name="data">The byte array containing the data.</param>
        /// <param name="pos">The current position in the array, which will be updated.</param>
        /// <returns>The decoded variable-length value.</returns>
        /// <exception cref="InvalidDataException">Thrown when the data is invalid or incomplete.</exception>
        private int ReadVariableLengthValue(byte[] data, ref int pos)
        {
            int value = 0;
            byte currentByte;
            int startPos = pos;
            int bytesRead = 0;

            do
            {
                if (pos >= data.Length)
                {
                    TraceLogger.Log("SMrcToMidiConverter", $"ReadVariableLengthValue: Unexpected end of data at pos={pos}, startPos={startPos}");
                    throw new InvalidDataException($"Unexpected end of data while reading variable-length value at position {startPos}");
                }

                currentByte = data[pos++];
                value = (value << 7) | (currentByte & 0x7F);
                bytesRead++;
                TraceLogger.Log("SMrcToMidiConverter", $"ReadVariableLengthValue: Read byte 0x{currentByte:X2}, current value={value}, position={pos}, bytesRead={bytesRead}");
            } while ((currentByte & 0x80) != 0);

            TraceLogger.Log("SMrcToMidiConverter", $"ReadVariableLengthValue: Final value={value}, bytes read={pos - startPos}, startPos={startPos}, endPos={pos}");
            return value;
        }

        /// <summary>
        /// Validates a MIDI file for correctness.
        /// </summary>
        /// <param name="stream">The stream containing the MIDI file data.</param>
        /// <exception cref="InvalidDataException">Thrown when the file is not a valid MIDI file.</exception>
        private void ValidateMidiFile(Stream stream)
        {
            MidiValidator.ValidateMidiFile(stream);
        }

        /// <summary>
        /// Validates an S-MRC file for correctness.
        /// </summary>
        /// <param name="stream">The stream containing the S-MRC file data.</param>
        /// <exception cref="InvalidDataException">Thrown when the file is not a valid S-MRC file.</exception>
        private void ValidateSmrcFile(Stream stream)
        {
            // (Use the previous implementation you had for ValidateSmrcFile)
            // ...
        }

        /// <summary>
        /// Writes the S-MRC header to the output stream.
        /// </summary>
        /// <param name="title">The song title.</param>
        /// <param name="ppqn">The PPQN (Pulses Per Quarter Note) value.</param>
        /// <param name="timeSignatureNumerator">The time signature numerator.</param>
        /// <param name="timeSignatureDenominator">The time signature denominator.</param>
        /// <param name="tempoBpm">The tempo in BPM.</param>
        private void WriteSmrcHeader(string title, short? ppqn, byte? timeSignatureNumerator, byte? timeSignatureDenominator, byte? tempoBpm)
        {
            // Song title (32 bytes)
            byte[] titleBytes = new byte[Constants.SmrcFormat.TitleSize];
            if (!string.IsNullOrEmpty(title))
            {
                byte[] titleEncoded = Encoding.ASCII.GetBytes(title);
                Array.Copy(titleEncoded, 0, titleBytes, 0, Math.Min(titleEncoded.Length, Constants.MidiFormat.MaxTrackNameLength));
            }
            writer.Write(titleBytes);

            // PPQN (always 96)
            writer.Write(ppqn ?? SMRC_PPQN);

            // Time signature
            writer.Write(timeSignatureNumerator ?? Constants.MidiFormat.DefaultTimeSignatureNumerator);
            writer.Write(timeSignatureDenominator ?? Constants.MidiFormat.DefaultTimeSignatureDenominator);

            // Tempo
            writer.Write(tempoBpm ?? Constants.MidiFormat.DefaultTempo);

            // Reserved
            writer.Write(new byte[Constants.SmrcFormat.HeaderReservedSize]);

            // Track directory
            for (int i = 0; i < SMRC_TRACK_COUNT; i++)
            {
                writer.Write(0); // Start offset
                writer.Write(0); // Length
                writer.Write(new byte[Constants.SmrcFormat.TrackDirectoryReservedSize]); // Reserved
            }
        }

        /// <summary>
        /// Writes a variable-length value to the output stream.
        /// </summary>
        /// <param name="writer">The binary writer for the output stream.</param>
        /// <param name="value">The value to write.</param>
        private void WriteVariableLengthValue(BinaryWriter writer, int value)
        {
            if (value < 0x80)
            {
                writer.Write((byte)value);
            }
            else if (value < 0x4000)
            {
                writer.Write((byte)(0x80 | (value >> 7)));
                writer.Write((byte)(value & 0x7F));
            }
            else if (value < 0x200000)
            {
                writer.Write((byte)(0x80 | (value >> 14)));
                writer.Write((byte)(0x80 | ((value >> 7) & 0x7F)));
                writer.Write((byte)(value & 0x7F));
            }
            else
            {
                writer.Write((byte)(0x80 | (value >> 21)));
                writer.Write((byte)(0x80 | ((value >> 14) & 0x7F)));
                writer.Write((byte)(0x80 | ((value >> 7) & 0x7F)));
                writer.Write((byte)(value & 0x7F));
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Converts a standard MIDI file to S-MRC format.
        /// </summary>
        /// <exception cref="InvalidDataException">Thrown when the input file is not a valid MIDI file.</exception>
        /// <exception cref="NotSupportedException">Thrown when the MIDI file uses unsupported features.</exception>
        /// <exception cref="IOException">Thrown when there is an error reading from or writing to the streams.</exception>
        public void ConvertMidiToSmrc()
        {
            TraceLogger.Log("SMrcToMidiConverter", "Starting MIDI to S-MRC conversion");
            using (BinaryReader reader = new BinaryReader(input, Encoding.ASCII, true))
            using (BinaryWriter writer = new BinaryWriter(output, Encoding.ASCII, true))
            {
                // Read and validate MIDI header
                string header = new string(reader.ReadChars(4));
                int headerLength = ReadInt32BigEndian(reader);
                short format = ReadInt16BigEndian(reader);
                short trackCount = ReadInt16BigEndian(reader);
                short division = ReadInt16BigEndian(reader);
                TraceLogger.Log("SMrcToMidiConverter", "=== Input MIDI File Information ===");
                TraceLogger.Log("SMrcToMidiConverter", $"Format: {format}");
                TraceLogger.Log("SMrcToMidiConverter", $"Tracks: {trackCount}");
                TraceLogger.Log("SMrcToMidiConverter", $"Division: {division}");
                TraceLogger.Log("SMrcToMidiConverter", "===============================");

                // Validate MIDI format
                if (format > 2)
                {
                    throw new NotSupportedException($"MIDI format {format} is not supported. Only formats 0, 1, and 2 are supported.");
                }

                if (trackCount > SMRC_TRACK_COUNT)
                {
                    throw new InvalidDataException($"Too many tracks. S-MRC supports maximum {SMRC_TRACK_COUNT} tracks, found {trackCount}");
                }

                if ((division & 0x8000) != 0)
                {
                    throw new NotSupportedException("SMPTE time division is not supported. Only PPQ timing is supported.");
                }

                // Skip any extra header bytes
                if (headerLength > 6)
                {
                    _ = reader.ReadBytes(headerLength - 6);
                }

                // Read MIDI tracks
                List<MidiTrack> midiTracks = [];
                for (int i = 0; i < trackCount; i++)
                {
                    string trackHeader = new string(reader.ReadChars(4));
                    int trackLength = ReadInt32BigEndian(reader);
                    byte[] trackData = reader.ReadBytes(trackLength);
                    midiTracks.Add(new MidiTrack { Data = trackData });
                    TraceLogger.Log("SMrcToMidiConverter", $"MIDI Track {i + 1}: Length={trackLength}");

                    // Analyze meta events in the track
                    bool hasTrackName = false, hasTempo = false, hasTimeSig = false, hasEndOfTrack = false;
                    string trackName = "";
                    int metaCount = 0, midiCount = 0, sysexCount = 0;
                    int pos = 0;
                    while (pos < trackData.Length)
                    {
                        int delta = ReadVariableLengthValue(trackData, ref pos);
                        if (pos >= trackData.Length)
                        {
                            break;
                        }

                        byte status = trackData[pos];
                        if (status < 0x80)
                        {
                            status = 0; // running status not handled here
                        }
                        else
                        {
                            pos++;
                        }

                        if (status == 0xFF && pos < trackData.Length)
                        {
                            byte metaType = trackData[pos++];
                            TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: Reading meta event type=0x{metaType:X2} at position {pos}");

                            // Log the next few bytes to help diagnose the issue
                            StringBuilder hexDump = new StringBuilder();
                            for (int j = 0; j < Math.Min(4, trackData.Length - pos); j++)
                            {
                                _ = hexDump.Append($"0x{trackData[pos + j]:X2} ");
                            }

                            TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: Next bytes at position {pos}: {hexDump}");

                            // Read meta event length - handle both variable-length and single-byte formats
                            int metaLen;
                            if (metaType == 0x32) // Special case for meta type 0x32
                            {
                                metaLen = trackData[pos++]; // Read length as single byte
                                TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: Reading meta event type=0x{metaType:X2} with single-byte length={metaLen}");

                                // Skip the data bytes for this meta event
                                pos += metaLen;
                                TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: Skipped {metaLen} bytes of meta event data");
                                continue; // Move to next event
                            }
                            else
                            {
                                metaLen = ReadVariableLengthValue(trackData, ref pos);
                                TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: Reading meta event type=0x{metaType:X2} with variable-length value={metaLen}");
                            }

                            TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: Found meta event type=0x{metaType:X2}, length={metaLen}, position={pos}");

                            // Validate meta event length
                            if (pos + metaLen > trackData.Length)
                            {
                                throw new InvalidDataException($"Invalid meta event length. Type=0x{metaType:X2}, Length={metaLen}, RemainingBytes={trackData.Length - pos}, Position={pos}, TotalLength={trackData.Length}");
                            }

                            if (metaType == 0x03)
                            {
                                hasTrackName = true;
                                trackName = System.Text.Encoding.ASCII.GetString(trackData, pos, Math.Min(metaLen, 31));
                            }

                            if (metaType == 0x51)
                            {
                                hasTempo = true;
                            }

                            if (metaType == 0x58)
                            {
                                hasTimeSig = true;
                            }

                            if (metaType == 0x2F)
                            {
                                hasEndOfTrack = true;
                            }

                            metaCount++;
                            TraceLogger.Log("SMrcToMidiConverter", $"MIDI Track {i + 1} Meta Event: Type=0x{metaType:X2}, Length={metaLen}");
                            pos += metaLen;
                        }
                        else if (status is >= 0x80 and <= 0xEF)
                        {
                            int dataLen = GetMidiDataLength(status);
                            midiCount++;
                            pos += dataLen;
                        }
                        else if (status is 0xF0 or 0xF7)
                        {
                            int sysexLen = ReadVariableLengthValue(trackData, ref pos);
                            sysexCount++;
                            pos += sysexLen;
                        }
                        else
                        {
                            // unknown event, skip one byte
                            pos++;
                        }
                    }

                    TraceLogger.Log("SMrcToMidiConverter", $"MIDI Track {i + 1}: TrackName={hasTrackName} ('{trackName}'), Tempo={hasTempo}, TimeSig={hasTimeSig}, EndOfTrack={hasEndOfTrack}, MetaEvents={metaCount}, MIDIEvents={midiCount}, SysExEvents={sysexCount}, TotalBytes={trackData.Length}");
                }

                // Convert to S-MRC format
                ConvertMidiToSmrcFormat(writer, midiTracks, division);

                // Validate the output S-MRC file
                output.Position = 0;
                ValidateSmrcFile(output);
            }

            TraceLogger.Log("SMrcToMidiConverter", "Completed MIDI to S-MRC conversion");
        }

        /// <summary>
        /// Converts an S-MRC file to standard MIDI format.
        /// </summary>
        /// <exception cref="InvalidDataException">Thrown when the input file is not a valid S-MRC file.</exception>
        /// <exception cref="IOException">Thrown when there is an error reading from or writing to the streams.</exception>
        public void ConvertSmrcToMidi()
        {
            TraceLogger.Log("SMrcToMidiConverter", "Starting S-MRC to MIDI conversion");
            using (BinaryReader reader = new BinaryReader(input, Encoding.ASCII, true))
            using (BinaryWriter writer = new BinaryWriter(output, Encoding.ASCII, true))
            {
                // Read S-MRC header
                SmrcHeader smrcHeader = ReadSmrcHeader(reader);
                TraceLogger.Log("SMrcToMidiConverter", "=== Input S-MRC File Information ===");
                TraceLogger.Log("SMrcToMidiConverter", $"Title: '{smrcHeader.Title}'");
                TraceLogger.Log("SMrcToMidiConverter", $"PPQN: {smrcHeader.Ppqn}");
                TraceLogger.Log("SMrcToMidiConverter", $"Time Signature: {smrcHeader.TimeSignatureNumerator}/{1 << smrcHeader.TimeSignatureDenominator}");
                TraceLogger.Log("SMrcToMidiConverter", $"Tempo: {smrcHeader.TempoBpm} BPM");
                TraceLogger.Log("SMrcToMidiConverter", $"Track Count: {smrcHeader.TrackDirectory.Length}");
                TraceLogger.Log("SMrcToMidiConverter", "===============================");

                // Read track data
                List<SmrcTrack> smrcTracks = ReadSmrcTracks(reader, smrcHeader);
                TraceLogger.Log("SMrcToMidiConverter", $"S-MRC Track Count: {smrcTracks.Count}");
                for (int i = 0; i < smrcTracks.Count; i++)
                {
                    SmrcTrack track = smrcTracks[i];
                    bool hasTrackName = false, hasTempo = false, hasTimeSig = false, hasEndOfTrack = false;
                    string trackName = "";
                    int metaCount = 0, midiCount = 0, sysexCount = 0;
                    foreach (SmrcEvent evt in track.Events)
                    {
                        if (evt.Status == 0xFF && evt.Data != null && evt.Data.Length > 0)
                        {
                            if (evt.Data[0] == 0x03)
                            {
                                hasTrackName = true;
                                trackName = System.Text.Encoding.ASCII.GetString(evt.Data, 1, evt.Data.Length - 1);
                            }

                            if (evt.Data[0] == 0x2F)
                            {
                                hasEndOfTrack = true;
                            }

                            metaCount++;
                            TraceLogger.Log("SMrcToMidiConverter", $"S-MRC Track {i + 1} Meta Event: Type=0x{evt.Data[0]:X2}, Length={evt.Data.Length - 1}");
                        }

                        if (evt.Status == 0xF7 && evt.RolandMetaType == 0x01)
                        {
                            hasTempo = true;
                        }

                        if (evt.Status == 0xF7 && evt.RolandMetaType == 0x02)
                        {
                            hasTimeSig = true;
                        }

                        if (evt.Status is >= 0x80 and <= 0xEF)
                        {
                            midiCount++;
                        }

                        if (evt.Status is 0xF0 or 0xF7)
                        {
                            sysexCount++;
                        }
                    }

                    TraceLogger.Log("SMrcToMidiConverter", $"S-MRC Track {i + 1}: TrackName={hasTrackName} ('{trackName}'), Tempo={hasTempo}, TimeSig={hasTimeSig}, EndOfTrack={hasEndOfTrack}, MetaEvents={metaCount}, MIDIEvents={midiCount}, SysExEvents={sysexCount}, TotalEvents={track.Events.Count}");
                }

                // Convert to standard MIDI
                MidiFileData midiData = ConvertSmrcToMidiData(smrcHeader, smrcTracks);
                TraceLogger.Log("SMrcToMidiConverter", "=== Output MIDI File Information ===");
                TraceLogger.Log("SMrcToMidiConverter", $"Format: 1");
                TraceLogger.Log("SMrcToMidiConverter", $"Tracks: {midiData.TrackCount}");
                TraceLogger.Log("SMrcToMidiConverter", $"Division: {SMRC_PPQN}");
                TraceLogger.Log("SMrcToMidiConverter", "===============================");

                // Write standard MIDI header
                writer.Write(STANDARD_MIDI_HEADER.ToCharArray());
                writer.Write(GetBigEndianBytes(6)); // Header length
                writer.Write(GetBigEndianBytes((short)1)); // Format 1 (multiple tracks)
                writer.Write(GetBigEndianBytes(midiData.TrackCount));
                writer.Write(GetBigEndianBytes(SMRC_PPQN)); // Fixed PPQN

                // Write MIDI tracks
                for (int i = 0; i < midiData.Tracks.Count; i++)
                {
                    MidiTrack track = midiData.Tracks[i];
                    writer.Write(STANDARD_MIDI_TRACK.ToCharArray());
                    writer.Write(GetBigEndianBytes(track.Data.Length));
                    writer.Write(track.Data);
                    TraceLogger.Log("SMrcToMidiConverter", $"Wrote MIDI track {i + 1} of length {track.Data.Length} bytes");
                }

                // Validate the output MIDI file
                output.Position = 0;
                ValidateMidiFile(output);
            }

            TraceLogger.Log("SMrcToMidiConverter", "Completed S-MRC to MIDI conversion");
        }

        #endregion
    }
}
