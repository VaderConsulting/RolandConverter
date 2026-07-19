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
        private const uint SMRC_HEADER_SIZE = Constants.SmrcFormat.FirstTrackDataOffset;

        /// <summary>
        /// The default PPQN (Pulses Per Quarter Note) value for S-MRC files.
        /// </summary>
        private const short SMRC_PPQN = Constants.MidiFormat.DefaultPpqn;

        /// <summary>
        /// The maximum number of tracks supported in an S-MRC file.
        /// </summary>
        private const uint SMRC_TRACK_COUNT = Constants.SmrcFormat.MaxTracks;

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
            TraceLogger.Log("SMrcToMidiConverter", "[LOG] ConvertMidiToSmrcFormat: Start. Track count: " + midiTracks.Count);
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
            long trackDataStart = Constants.SmrcFormat.HeaderSize + (Constants.SmrcFormat.MaxTracks * Constants.SmrcFormat.TrackDirectoryEntrySize);
            long currentOffset = trackDataStart;
            List<byte[]> trackDataBuffers = new List<byte[]>();
            List<long> trackOffsets = new List<long>();
            List<int> trackLengths = new List<int>();

            // First, convert all tracks to byte arrays and calculate offsets/lengths
            for (int i = 0; i < SMRC_TRACK_COUNT; i++)
            {
                byte[] trackData = ConvertTrackToSmrcFormat(smrcTracks[i]);
                trackDataBuffers.Add(trackData);
                trackOffsets.Add(currentOffset);
                trackLengths.Add(trackData.Length);
                currentOffset += trackData.Length;
            }

            // Write the track directory entries
            writer.BaseStream.Position = Constants.SmrcFormat.HeaderSize;
            for (int i = 0; i < SMRC_TRACK_COUNT; i++)
            {
                writer.Write((int)trackOffsets[i]); // Start offset
                writer.Write(trackLengths[i]); // Length
                writer.Write(new byte[8]); // Reserved
                TraceLogger.Log("SMrcToMidiConverter", $"ConvertMidiToSmrcFormat: Writing track directory entry {i}: offset={trackOffsets[i]}, length={trackLengths[i]}");
            }

            // Write the track data at the correct offsets
            for (int i = 0; i < SMRC_TRACK_COUNT; i++)
            {
                writer.BaseStream.Position = trackOffsets[i];
                writer.Write(trackDataBuffers[i]);
            }
            TraceLogger.Log("SMrcToMidiConverter", $"[LOG] ConvertMidiToSmrcFormat: Completed. S-MRC tracks: {smrcTracks.Count}");
        }

        /// <summary>
        /// Converts S-MRC data to standard MIDI format.
        /// </summary>
        /// <param name="header">The S-MRC header information.</param>
        /// <param name="smrcTracks">The list of S-MRC tracks to convert.</param>
        /// <returns>A MidiFileData object containing the converted MIDI data.</returns>
        private MidiFileData ConvertSmrcToMidiData(SmrcHeader header, List<SmrcTrack> smrcTracks)
        {
            TraceLogger.Log("SMrcToMidiConverter", "[LOG] ConvertSmrcToMidiData: Start. S-MRC tracks: " + smrcTracks.Count);
            MidiFileData midiData = new MidiFileData();
            int totalEvents = 0;

            bool insertedTrackName = false;
            bool foundFirstNonEmptyTrack = false;
            for (int trackIdx = 0; trackIdx < smrcTracks.Count; trackIdx++)
            {
                SmrcTrack smrcTrack = smrcTracks[trackIdx];
                using (MemoryStream trackStream = new MemoryStream())
                using (BinaryWriter trackWriter = new BinaryWriter(trackStream))
                {
                    int eventCount = 0;
                    bool hasTrackName = false;
                    int lastEotIndex = -1;
                    string smrcTrackName = string.Empty;

                    // Check if track already has a track name and find last EOT
                    for (int i = 0; i < smrcTrack.Events.Count; i++)
                    {
                        SmrcEvent evt = smrcTrack.Events[i];
                        if (evt.Status == 0xFF && evt.Data != null && evt.Data.Length > 0)
                        {
                            if (evt.Data[0] == 0x03)
                            {
                                hasTrackName = true;
                                if (evt.Data.Length > 1)
                                {
                                    smrcTrackName = Encoding.ASCII.GetString(evt.Data, 1, evt.Data.Length - 1);
                                }
                            }
                            if (evt.Data[0] == 0x2F)
                            {
                                lastEotIndex = i;
                            }
                        }
                    }

                    TraceLogger.Log("SMrcToMidiConverter", $"Track {trackIdx + 1}: S-MRC has TrackName={hasTrackName} ('{smrcTrackName}')");

                    if (smrcTrack.Events.Count > 0)
                    {
                        if (!foundFirstNonEmptyTrack)
                        {
                            foundFirstNonEmptyTrack = true;
                            if (!hasTrackName && !string.IsNullOrEmpty(header.Title))
                            {
                                TraceLogger.Log("SMrcToMidiConverter", $"Track {trackIdx + 1}: Injecting S-MRC song title as Track Name ('{header.Title}') (first non-empty track, no Track Name present)");
                                WriteVariableLengthValue(trackWriter, 0);
                                trackWriter.Write((byte)0xFF);
                                trackWriter.Write((byte)0x03);
                                byte[] titleBytes = Encoding.ASCII.GetBytes(header.Title);
                                WriteVariableLengthValue(trackWriter, titleBytes.Length);
                                trackWriter.Write(titleBytes);
                                insertedTrackName = true;
                            }
                            else if (hasTrackName)
                            {
                                TraceLogger.Log("SMrcToMidiConverter", $"Track {trackIdx + 1}: First non-empty track already has Track Name ('{smrcTrackName}')");
                            }
                        }

                        int eventsToWrite = smrcTrack.Events.Count;
                        if (lastEotIndex != -1)
                        {
                            eventsToWrite = lastEotIndex + 1;
                        }
                        for (int i = 0; i < eventsToWrite; i++)
                        {
                            SmrcEvent evt = smrcTrack.Events[i];
                            if (evt.Status == 0xFF && evt.Data != null && evt.Data.Length > 0 && evt.Data[0] == 0x03)
                            {
                                string trackNameValue = string.Empty;
                                if (evt.Data.Length > 1)
                                {
                                    trackNameValue = Encoding.ASCII.GetString(evt.Data, 1, evt.Data.Length - 1);
                                }
                                if (foundFirstNonEmptyTrack && trackIdx == 0 && hasTrackName)
                                {
                                    TraceLogger.Log("SMrcToMidiConverter", $"Track {trackIdx + 1}: Writing Track Name meta event ('{trackNameValue}') (first non-empty track, present in S-MRC)");
                                }
                                else if (trackIdx != 0)
                                {
                                    TraceLogger.Log("SMrcToMidiConverter", $"Track {trackIdx + 1}: Skipping Track Name meta event ('{trackNameValue}') (not first non-empty track)");
                                    continue;
                                }
                            }
                            eventCount++;
                            int scaledDeltaTime = evt.DeltaTime;
                            WriteVariableLengthValue(trackWriter, scaledDeltaTime);

                            if (evt.Status == 0xF7 && evt.RolandMetaType.HasValue)
                            {
                                trackWriter.Write((byte)0xFF);
                                if (evt.RolandMetaType == 0x01)
                                {
                                    trackWriter.Write((byte)0x51);
                                    trackWriter.Write((byte)0x03);
                                    int microsecondsPerQuarter = evt.RolandMetaData[0] | (evt.RolandMetaData[1] << 8);
                                    trackWriter.Write((byte)(microsecondsPerQuarter >> 16));
                                    trackWriter.Write((byte)(microsecondsPerQuarter >> 8));
                                    trackWriter.Write((byte)microsecondsPerQuarter);
                                }
                                else if (evt.RolandMetaType == 0x02)
                                {
                                    trackWriter.Write((byte)0x58);
                                    trackWriter.Write((byte)0x04);
                                    trackWriter.Write(evt.RolandMetaData[0]);
                                    trackWriter.Write(evt.RolandMetaData[1]);
                                    trackWriter.Write((byte)24);
                                    trackWriter.Write((byte)8);
                                }
                            }
                            else if (evt.Status == 0xFF)
                            {
                                if (evt.Data[0] == 0x51)
                                {
                                    continue;
                                }
                                trackWriter.Write(evt.Status);
                                if (evt.Data != null && evt.Data.Length > 0)
                                {
                                    trackWriter.Write(evt.Data[0]);
                                    if (evt.Data.Length > 1)
                                    {
                                        WriteVariableLengthValue(trackWriter, evt.Data.Length - 1);
                                        trackWriter.Write(evt.Data, 1, evt.Data.Length - 1);
                                    }
                                    else
                                    {
                                        WriteVariableLengthValue(trackWriter, 0);
                                    }
                                }
                            }
                            else if (evt.Status >= 0x80 && evt.Status <= 0xEF)
                            {
                                trackWriter.Write(evt.Status);
                                if (evt.Data != null)
                                {
                                    trackWriter.Write(evt.Data);
                                }
                            }
                        }

                        if (lastEotIndex == -1)
                        {
                            WriteVariableLengthValue(trackWriter, 0);
                            trackWriter.Write((byte)0xFF);
                            trackWriter.Write((byte)0x2F);
                            WriteVariableLengthValue(trackWriter, 0);
                        }
                        midiData.Tracks.Add(new MidiTrack { Data = trackStream.ToArray() });
                        totalEvents += eventCount;
                    }
                    else
                    {
                        WriteVariableLengthValue(trackWriter, 0);
                        trackWriter.Write((byte)0xFF);
                        trackWriter.Write((byte)0x2F);
                        WriteVariableLengthValue(trackWriter, 0);
                        midiData.Tracks.Add(new MidiTrack { Data = trackStream.ToArray() });
                    }
                }
            }

            TraceLogger.Log("SMrcToMidiConverter", $"[LOG] ConvertSmrcToMidiData: Completed. MIDI tracks: {midiData.Tracks.Count}, total events: {totalEvents}");
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
                    // Write delta time (2 bytes, big-endian)
                    byte[] deltaBytes = GetBigEndianBytes(evt.DeltaTime, 2);
                    writer.Write(deltaBytes);
                    TraceLogger.Log("SMrcToMidiConverter", $"ConvertTrackToSmrcFormat: Writing delta={evt.DeltaTime} as bytes: {BitConverter.ToString(deltaBytes)}");

                    // Write status byte (no running status in S-MRC)
                    writer.Write(evt.Status);

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
                        // Only skip meta events that are already included in a SysEx event (0x51 or 0x2F), but never skip 0x03 (track name)
                        if (evt.Data[0] == 0x51)
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
                        }
                        if (evt.Data[0] == 0x03)
                        {
                            string trackName = Encoding.ASCII.GetString(evt.Data, 1, evt.Data.Length - 1);
                            TraceLogger.Log("SMrcToMidiConverter", $"ConvertTrackToSmrcFormat: Writing track name meta event: '{trackName}'");
                        }
                    }
                    // Write data bytes for other events
                    else if (evt.Status is >= 0x80 and <= 0xEF && evt.Data != null)
                    {
                        writer.Write(evt.Data);
                    }
                    else if (evt.Data != null)
                    {
                        writer.Write(evt.Data);
                    }
                }

                // Add end of track meta event if not already present as the last event
                bool lastIsEOT = track.Events.Count > 0 &&
                    track.Events[^1].Status == 0xFF &&
                    track.Events[^1].Data != null &&
                    track.Events[^1].Data.Length > 0 &&
                    track.Events[^1].Data[0] == 0x2F;
                if (!lastIsEOT)
                {
                    writer.Write(GetBigEndianBytes(0, 2)); // Delta time
                    writer.Write((byte)0xFF); // Meta event
                    writer.Write((byte)0x2F); // End of track
                    WriteVariableLengthValue(writer, 0); // Length as variable-length value
                }

                byte[] result = stream.ToArray();
                TraceLogger.Log("SMrcToMidiConverter", $"ConvertTrackToSmrcFormat: Final raw track data: {BitConverter.ToString(result)}");
                TraceLogger.Log("SMrcToMidiConverter", $"ConvertTrackToSmrcFormat: Track data length: {result.Length} bytes");
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
            TraceLogger.Log("SMrcToMidiConverter", $"[LOG] ParseMidiTrack: Start. Data length: {trackData.Length}, isFirstTrack: {isFirstTrack}");
            SmrcTrack track = new SmrcTrack();
            int pos = 0;
            int accumulatedDelta = 0;
            byte runningStatus = 0;
            int eventCount = 0;

            while (pos < trackData.Length)
            {
                // Read variable-length delta time
                int deltaTime = ReadVariableLengthValue(trackData, ref pos);
                accumulatedDelta += deltaTime;

                // Log status byte and running status before event
                if (pos < trackData.Length)
                {
                    TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: Next status byte candidate: 0x{trackData[pos]:X2} at pos={pos}, runningStatus=0x{runningStatus:X2}");
                }

                // Read status byte
                byte status = trackData[pos];
                if (status < 0x80)
                {
                    status = runningStatus;
                }
                else
                {
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
                    int metaLen = 0;
                    if (metaType == 0x32) // Special case for meta type 0x32
                    {
                        metaLen = trackData[pos++]; // Read length as single byte
                        pos += metaLen;
                        continue; // Move to next event
                    }
                    else
                    {
                        metaLen = ReadVariableLengthValue(trackData, ref pos);
                    }
                    if (pos + metaLen > trackData.Length)
                    {
                        break;
                    }

                    byte[] data = new byte[metaLen + 1];
                    data[0] = metaType;
                    Array.Copy(trackData, pos, data, 1, metaLen);
                    if (metaType == 0x03)
                    {
                        string trackName = Encoding.ASCII.GetString(data, 1, metaLen);
                        songTitle = trackName;
                    }
                    if (metaType == 0x2F) // End of track
                    {
                        track.Events.Add(new SmrcEvent
                        {
                            DeltaTime = 0, // Always use 0 for EOT
                            Status = 0xFF,
                            Data = data
                        });
                        accumulatedDelta = 0;
                        pos += metaLen;
                        break; // End of track, stop processing
                    }
                    else
                    {
                        track.Events.Add(new SmrcEvent
                        {
                            DeltaTime = accumulatedDelta,
                            Status = 0xFF,
                            Data = data
                        });
                        accumulatedDelta = 0;
                        pos += metaLen;
                    }
                }
                else if (status == 0xF7) // Roland meta event
                {
                    if (pos >= trackData.Length)
                    {
                        break;
                    }

                    track.Events.Add(new SmrcEvent
                    {
                        DeltaTime = accumulatedDelta,
                        Status = 0xF7,
                        RolandMetaType = trackData[pos++]
                    });
                    int metaLen = ReadVariableLengthValue(trackData, ref pos);
                    if (pos + metaLen > trackData.Length)
                    {
                        break;
                    }

                    track.Events.Add(new SmrcEvent
                    {
                        DeltaTime = accumulatedDelta,
                        Status = 0xF7,
                        RolandMetaData = new byte[metaLen]
                    });
                    Array.Copy(trackData, pos, track.Events[^1].RolandMetaData, 0, metaLen);
                    accumulatedDelta = 0;
                }
                else if (status is >= 0x80 and <= 0xEF) // MIDI event
                {
                    int dataLen = GetMidiDataLength(status);
                    if (pos + dataLen > trackData.Length)
                    {
                        break;
                    }

                    track.Events.Add(new SmrcEvent
                    {
                        DeltaTime = accumulatedDelta,
                        Status = status,
                        Data = new byte[dataLen]
                    });
                    Array.Copy(trackData, pos, track.Events[^1].Data, 0, dataLen);
                    pos += dataLen;
                }
                else if (status == 0xF0) // SysEx event
                {
                    int sysexLen = ReadVariableLengthValue(trackData, ref pos);

                    // Check if we have enough data for the SysEx event
                    if (pos + sysexLen > trackData.Length)
                    {
                        sysexLen = trackData.Length - pos;
                    }

                    // Process embedded meta events within SysEx data
                    int sysexStartPos = pos;
                    int sysexEndPos = pos + sysexLen;
                    bool foundEndOfTrack = false;
                    SmrcEvent metaEvent;
                    List<SmrcEvent> embeddedEvents = [];
                    List<int> metaEventPositions = [];

                    while (pos < sysexEndPos)
                    {
                        if (trackData[pos] == 0xFF) // Meta event
                        {
                            // Check if we have enough bytes for meta type and length
                            if (pos + 2 >= trackData.Length)
                            {
                                break;
                            }

                            byte metaType = trackData[pos + 1];
                            int metaLen = ReadVariableLengthValue(trackData, ref pos);

                            // Check if we have enough data for the meta event
                            if (pos + metaLen > trackData.Length)
                            {
                                metaLen = trackData.Length - pos;
                            }

                            if (metaType == 0x2F) // End of track
                            {
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
                    foreach (SmrcEvent embeddedEvent in embeddedEvents)
                    {
                        track.Events.Add(embeddedEvent);
                    }

                    // Only add the SysEx event if it contains data other than the embedded events
                    if (sysexLen > 0 && !foundEndOfTrack)
                    {
                        // Create a new array excluding the embedded meta events
                        List<byte> sysexData = [];
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
                    pos++;
                }
            }

            TraceLogger.Log("SMrcToMidiConverter", $"[LOG] ParseMidiTrack: Completed. Events parsed: {eventCount}");
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
            if (ppqn is < 24 or > 480)
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
            reader.BaseStream.Position = Constants.SmrcFormat.HeaderSize;
            TrackDirectoryEntry[] trackDirectory = new TrackDirectoryEntry[SMRC_TRACK_COUNT];
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
            TraceLogger.Log("SMrcToMidiConverter", $"[LOG] ReadSmrcTracks: Start. Track directory entries: {SMRC_TRACK_COUNT}");
            List<SmrcTrack> tracks = [];
            int totalEvents = 0;
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
                int eventCount = 0;
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
                        if (metaType == 0x03)
                        {
                            string trackName = Encoding.ASCII.GetString(data, 1, metaLen);
                        }
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
                        if (pos + sysexLen > trackData.Length)
                        {
                            sysexLen = trackData.Length - pos;
                        }
                        evt.Data = new byte[sysexLen];
                        Array.Copy(trackData, pos, evt.Data, 0, sysexLen);
                        pos += sysexLen;
                    }

                    track.Events.Add(evt);
                    eventCount++;
                }

                tracks.Add(track);
                totalEvents += eventCount;
            }

            TraceLogger.Log("SMrcToMidiConverter", $"[LOG] ReadSmrcTracks: Completed. Tracks read: {tracks.Count}, total events: {totalEvents}");
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
                    throw new InvalidDataException($"Unexpected end of data while reading variable-length value at position {startPos}");
                }

                currentByte = data[pos++];
                value = (value << 7) | (currentByte & 0x7F);
                bytesRead++;
            } while ((currentByte & 0x80) != 0);

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

            // After writing all header fields:
            int headerSize = (int)writer.BaseStream.Length;
            if (headerSize < 168)
            {
                writer.Write(new byte[168 - headerSize]);
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

        private static void WriteTrackData(BinaryWriter writer, byte[] trackData, int trackIndex)
        {
            // Calculate the offset for this track - after the track directory
            long offset = (long)Constants.SmrcFormat.HeaderSize +
                         ((long)Constants.SmrcFormat.MaxTracks * (long)Constants.SmrcFormat.TrackDirectoryEntrySize) +
                         ((long)trackIndex * (long)trackData.Length);

            // Write the track directory entry first
            writer.BaseStream.Position = (long)Constants.SmrcFormat.HeaderSize +
                                       ((long)trackIndex * (long)Constants.SmrcFormat.TrackDirectoryEntrySize);
            writer.Write((int)offset);  // Write the offset
            writer.Write(trackData.Length);  // Write the length
            writer.Write(new byte[Constants.SmrcFormat.TrackDirectoryReservedSize]);  // Write reserved bytes

            // Write the track data at the correct position
            writer.BaseStream.Position = offset;
            writer.Write(trackData);
        }

        private byte[] GetBigEndianBytes(long value, int length)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            byte[] result = new byte[length];
            Array.Copy(bytes, bytes.Length - length, result, 0, length);
            return result;
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
                            int metaLen = ReadVariableLengthValue(trackData, ref pos);

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
                // Validate input S-MRC file before reading
                input.Position = 0;
                ValidateSmrcFile(input);
                input.Position = 0;

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

                // Convert to standard MIDI, but only keep non-empty tracks
                MidiFileData midiData = ConvertSmrcToMidiData(smrcHeader, smrcTracks);
                TraceLogger.Log("SMrcToMidiConverter", $"[LOG] Track count before filtering: {midiData.Tracks.Count}");
                // Filter out empty tracks (tracks with only End-of-Track event)
                int beforeCount = midiData.Tracks.Count;
                List<MidiTrack> filteredTracks = new List<MidiTrack>();
                for (int i = 0; i < midiData.Tracks.Count; i++)
                {
                    byte[] data = midiData.Tracks[i].Data;
                    bool isEmpty = (data.Length == 4 && data[0] == 0x00 && data[1] == 0xFF && data[2] == 0x2F && data[3] == 0x00);
                    if (!isEmpty)
                    {
                        filteredTracks.Add(midiData.Tracks[i]);
                        TraceLogger.Log("SMrcToMidiConverter", $"Including MIDI track {i + 1} (length={data.Length}) in output");
                    }
                    else
                    {
                        TraceLogger.Log("SMrcToMidiConverter", $"Excluding empty MIDI track {i + 1} (only EOT)");
                    }
                }
                midiData.Tracks = filteredTracks;
                TraceLogger.Log("SMrcToMidiConverter", $"Filtered MIDI tracks: {beforeCount} -> {midiData.Tracks.Count}");

                // If all tracks are empty, create a single track with the S-MRC song title (if present) and End-of-Track
                if (midiData.Tracks.Count == 0)
                {
                    TraceLogger.Log("SMrcToMidiConverter", "[LOG] All tracks empty after filtering. Creating fallback track with title and End-of-Track.");
                    using (MemoryStream trackStream = new MemoryStream())
                    using (BinaryWriter trackWriter = new BinaryWriter(trackStream))
                    {
                        // Find the first non-empty track with a track name
                        string trackName = null;
                        foreach (SmrcTrack track in smrcTracks)
                        {
                            foreach (SmrcEvent evt in track.Events)
                            {
                                if (evt.Status == 0xFF && evt.Data != null && evt.Data.Length > 0 && evt.Data[0] == 0x03)
                                {
                                    trackName = Encoding.ASCII.GetString(evt.Data, 1, evt.Data.Length - 1);
                                    break;
                                }
                            }
                            if (trackName != null)
                            {
                                break;
                            }
                        }

                        // Write track name meta event if found
                        if (!string.IsNullOrEmpty(trackName))
                        {
                            WriteVariableLengthValue(trackWriter, 0); // Delta time
                            trackWriter.Write((byte)0xFF);
                            trackWriter.Write((byte)0x03);
                            byte[] titleBytes = Encoding.ASCII.GetBytes(trackName);
                            WriteVariableLengthValue(trackWriter, titleBytes.Length);
                            trackWriter.Write(titleBytes);
                        }
                        // Always write End-of-Track
                        WriteVariableLengthValue(trackWriter, 0); // Delta time
                        trackWriter.Write((byte)0xFF);
                        trackWriter.Write((byte)0x2F);
                        WriteVariableLengthValue(trackWriter, 0); // Length
                        byte[] fallbackTrack = trackStream.ToArray();
                        midiData.Tracks.Add(new MidiTrack { Data = fallbackTrack });
                    }
                }

                // Write standard MIDI header
                writer.Write(STANDARD_MIDI_HEADER.ToCharArray());
                writer.Write(GetBigEndianBytes(6, 4)); // Header length
                writer.Write(GetBigEndianBytes(1, 2)); // Format 1 (multiple tracks)
                writer.Write(GetBigEndianBytes(midiData.TrackCount, 2));
                writer.Write(GetBigEndianBytes(smrcHeader.Ppqn, 2)); // Use S-MRC PPQN for MIDI output

                // Write MIDI tracks
                long headerEnd = output.Position;
                for (int i = 0; i < midiData.Tracks.Count; i++)
                {
                    MidiTrack track = midiData.Tracks[i];
                    TraceLogger.Log("SMrcToMidiConverter", $"[LOG] About to write MIDI track {i + 1}: length={track.Data.Length}, position={output.Position}");
                    writer.Write(STANDARD_MIDI_TRACK.ToCharArray());
                    writer.Write(GetBigEndianBytes(track.Data.Length, 4));
                    writer.Write(track.Data);
                    TraceLogger.Log("SMrcToMidiConverter", $"[LOG] Wrote MIDI track {i + 1} of length {track.Data.Length} bytes at position {output.Position - track.Data.Length}");
                }

                TraceLogger.Log("SMrcToMidiConverter", $"[LOG] Output.Position before truncation: {output.Position}");
                TraceLogger.Log("SMrcToMidiConverter", $"[LOG] Output.Length before truncation: {output.Length}");
                output.SetLength(output.Position);
                TraceLogger.Log("SMrcToMidiConverter", $"[LOG] Output.Length after truncation: {output.Length}");

                // Log the full MIDI file bytes
                output.Position = 0;
                byte[] midiBytes = new byte[output.Length];
                output.Read(midiBytes, 0, midiBytes.Length);
                TraceLogger.Log("SMrcToMidiConverter", $"[LOG] Full MIDI file bytes: {BitConverter.ToString(midiBytes)}");
                output.Position = 0;

                // Log header and track chunk offsets/lengths
                if (midiBytes.Length >= 14) // 4+4+2+2+2 = 14 bytes for header
                {
                    string headerStr = Encoding.ASCII.GetString(midiBytes, 0, 4);
                    int headerLen = (midiBytes[4] << 24) | (midiBytes[5] << 16) | (midiBytes[6] << 8) | midiBytes[7];
                    short format = (short)((midiBytes[8] << 8) | midiBytes[9]);
                    short nTracks = (short)((midiBytes[10] << 8) | midiBytes[11]);
                    short division = (short)((midiBytes[12] << 8) | midiBytes[13]);
                    TraceLogger.Log("SMrcToMidiConverter", $"[LOG] MIDI header: '{headerStr}', headerLen={headerLen}, format={format}, nTracks={nTracks}, division={division}");
                    int pos = 14;
                    for (int i = 0; i < nTracks && pos + 8 <= midiBytes.Length; i++)
                    {
                        string trk = Encoding.ASCII.GetString(midiBytes, pos, 4);
                        int trkLen = (midiBytes[pos + 4] << 24) | (midiBytes[pos + 5] << 16) | (midiBytes[pos + 6] << 8) | midiBytes[pos + 7];
                        TraceLogger.Log("SMrcToMidiConverter", $"[LOG] Track {i + 1} chunk: '{trk}', offset={pos}, length={trkLen}");
                        pos += 8 + trkLen;
                    }
                    TraceLogger.Log("SMrcToMidiConverter", $"[LOG] Expected file end after last track: {pos}, actual file length: {midiBytes.Length}");
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
