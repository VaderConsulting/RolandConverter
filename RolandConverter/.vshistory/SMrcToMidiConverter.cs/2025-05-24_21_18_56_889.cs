using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Linq;

namespace RolandConverter
{
    /// <summary>
    /// Provides functionality to convert between Roland S-MRC and standard MIDI file formats.
    /// This class handles the core conversion logic for both S-MRC to MIDI and MIDI to S-MRC conversions.
    /// </summary>
    public class SMrcToMidiConverter
    {
        private const String STANDARD_MIDI_HEADER = "MThd";
        private const String STANDARD_MIDI_TRACK = "MTrk";
        private const Int16 SMRC_PPQN = 96;
        private const Int32 SMRC_HEADER_SIZE = 0xA8; // 168 bytes
        private const Int32 SMRC_TRACK_COUNT = 8;

        private readonly Stream _inputStream;
        private readonly Stream _outputStream;

        /// <summary>
        /// Initializes a new instance of the SMrcToMidiConverter class.
        /// </summary>
        /// <param name="inputStream">The input stream containing the source file data.</param>
        /// <param name="outputStream">The output stream where the converted data will be written.</param>
        /// <exception cref="ArgumentNullException">Thrown when either inputStream or outputStream is null.</exception>
        public SMrcToMidiConverter(Stream inputStream, Stream outputStream)
        {
            _inputStream = inputStream ?? throw new ArgumentNullException(nameof(inputStream));
            _outputStream = outputStream ?? throw new ArgumentNullException(nameof(outputStream));
        }

        /// <summary>
        /// Converts an S-MRC file to standard MIDI format.
        /// </summary>
        /// <exception cref="InvalidDataException">Thrown when the input file is not a valid S-MRC file.</exception>
        /// <exception cref="IOException">Thrown when there is an error reading from or writing to the streams.</exception>
        public void ConvertSmrcToMidi()
        {
            TraceLogger.Log("SMrcToMidiConverter", "Starting S-MRC to MIDI conversion");
            using (BinaryReader reader = new BinaryReader(_inputStream, Encoding.ASCII, true))
            using (BinaryWriter writer = new BinaryWriter(_outputStream, Encoding.ASCII, true))
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
                    var track = smrcTracks[i];
                    bool hasTrackName = false, hasTempo = false, hasTimeSig = false, hasEndOfTrack = false;
                    string trackName = "";
                    int metaCount = 0, midiCount = 0, sysexCount = 0;
                    foreach (var evt in track.Events)
                    {
                        if (evt.Status == 0xFF && evt.Data != null && evt.Data.Length > 0)
                        {
                            if (evt.Data[0] == 0x03) { hasTrackName = true; trackName = System.Text.Encoding.ASCII.GetString(evt.Data, 1, evt.Data.Length - 1); }
                            if (evt.Data[0] == 0x2F) hasEndOfTrack = true;
                            metaCount++;
                            TraceLogger.Log("SMrcToMidiConverter", $"S-MRC Track {i + 1} Meta Event: Type=0x{evt.Data[0]:X2}, Length={evt.Data.Length - 1}");
                        }
                        if (evt.Status == 0xF7 && evt.RolandMetaType == 0x01) hasTempo = true;
                        if (evt.Status == 0xF7 && evt.RolandMetaType == 0x02) hasTimeSig = true;
                        if (evt.Status >= 0x80 && evt.Status <= 0xEF) midiCount++;
                        if (evt.Status == 0xF0 || evt.Status == 0xF7) sysexCount++;
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
                writer.Write(GetBigEndianBytes((Int16)1)); // Format 1 (multiple tracks)
                writer.Write(GetBigEndianBytes((Int16)midiData.TrackCount));
                writer.Write(GetBigEndianBytes((Int16)SMRC_PPQN)); // Fixed PPQN

                // Write MIDI tracks
                for (int i = 0; i < midiData.Tracks.Count; i++)
                {
                    var track = midiData.Tracks[i];
                    writer.Write(STANDARD_MIDI_TRACK.ToCharArray());
                    writer.Write(GetBigEndianBytes(track.Data.Length));
                    writer.Write(track.Data);
                    TraceLogger.Log("SMrcToMidiConverter", $"Wrote MIDI track {i + 1} of length {track.Data.Length} bytes");
                }

                // Validate the output MIDI file
                _outputStream.Position = 0;
                ValidateMidiFile(_outputStream);
            }
            TraceLogger.Log("SMrcToMidiConverter", "Completed S-MRC to MIDI conversion");
        }

        /// <summary>
        /// Converts a standard MIDI file to S-MRC format.
        /// </summary>
        /// <exception cref="InvalidDataException">Thrown when the input file is not a valid MIDI file.</exception>
        /// <exception cref="NotSupportedException">Thrown when the MIDI file uses unsupported features.</exception>
        /// <exception cref="IOException">Thrown when there is an error reading from or writing to the streams.</exception>
        public void ConvertMidiToSmrc()
        {
            TraceLogger.Log("SMrcToMidiConverter", "Starting MIDI to S-MRC conversion");
            using (BinaryReader reader = new BinaryReader(_inputStream, Encoding.ASCII, true))
            using (BinaryWriter writer = new BinaryWriter(_outputStream, Encoding.ASCII, true))
            {
                // Read and validate MIDI header
                String header = new String(reader.ReadChars(4));
                Int32 headerLength = ReadInt32BigEndian(reader);
                Int16 format = ReadInt16BigEndian(reader);
                Int16 trackCount = ReadInt16BigEndian(reader);
                Int16 division = ReadInt16BigEndian(reader);
                TraceLogger.Log("SMrcToMidiConverter", "=== Input MIDI File Information ===");
                TraceLogger.Log("SMrcToMidiConverter", $"Format: {format}");
                TraceLogger.Log("SMrcToMidiConverter", $"Tracks: {trackCount}");
                TraceLogger.Log("SMrcToMidiConverter", $"Division: {division}");
                TraceLogger.Log("SMrcToMidiConverter", "===============================");

                // Validate MIDI format
                if (format > 1)
                {
                    throw new NotSupportedException($"MIDI format {format} is not supported. Only format 0 and 1 are supported.");
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
                    reader.ReadBytes(headerLength - 6);
                }

                // Read MIDI tracks
                List<MidiTrack> midiTracks = new List<MidiTrack>();
                for (Int32 i = 0; i < trackCount; i++)
                {
                    String trackHeader = new String(reader.ReadChars(4));
                    Int32 trackLength = ReadInt32BigEndian(reader);
                    Byte[] trackData = reader.ReadBytes(trackLength);
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
                        if (pos >= trackData.Length) break;
                        byte status = trackData[pos];
                        if (status < 0x80) status = 0; // running status not handled here
                        else pos++;
                        if (status == 0xFF && pos < trackData.Length)
                        {
                            byte metaType = trackData[pos++];
                            TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: Reading meta event type=0x{metaType:X2} at position {pos}");

                            // Log the next few bytes to help diagnose the issue
                            StringBuilder hexDump = new StringBuilder();
                            for (int j = 0; j < Math.Min(4, trackData.Length - pos); j++)
                            {
                                hexDump.Append($"0x{trackData[pos + j]:X2} ");
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

                            if (metaType == 0x03) { hasTrackName = true; trackName = System.Text.Encoding.ASCII.GetString(trackData, pos, Math.Min(metaLen, 31)); }
                            if (metaType == 0x51) hasTempo = true;
                            if (metaType == 0x58) hasTimeSig = true;
                            if (metaType == 0x2F) hasEndOfTrack = true;
                            metaCount++;
                            TraceLogger.Log("SMrcToMidiConverter", $"MIDI Track {i + 1} Meta Event: Type=0x{metaType:X2}, Length={metaLen}");
                            pos += metaLen;
                        }
                        else if (status >= 0x80 && status <= 0xEF)
                        {
                            int dataLen = GetMidiDataLength(status);
                            midiCount++;
                            pos += dataLen;
                        }
                        else if (status == 0xF0 || status == 0xF7)
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
                _outputStream.Position = 0;
                ValidateSmrcFile(_outputStream);
            }
            TraceLogger.Log("SMrcToMidiConverter", "Completed MIDI to S-MRC conversion");
        }

        /// <summary>
        /// Converts MIDI track data to S-MRC format and writes it to the output stream.
        /// </summary>
        /// <param name="writer">The binary writer for the output stream.</param>
        /// <param name="midiTracks">The list of MIDI tracks to convert.</param>
        /// <param name="division">The MIDI file's time division value.</param>
        private void ConvertMidiToSmrcFormat(BinaryWriter writer, List<MidiTrack> midiTracks, Int16 division)
        {
            // Parse MIDI tracks to extract events
            List<SmrcTrack> smrcTracks = new List<SmrcTrack>();
            String songTitle = ""; // Only set if a track name meta event is found
            Byte? timeSignatureNumerator = null;
            Byte? timeSignatureDenominator = null;
            Byte? tempoBpm = null;

            for (Int32 i = 0; i < midiTracks.Count && i < SMRC_TRACK_COUNT; i++)
            {
                SmrcTrack smrcTrack = ParseMidiTrack(midiTracks[i].Data, division, i == 0,
                    ref songTitle, ref timeSignatureNumerator, ref timeSignatureDenominator, ref tempoBpm);
                smrcTracks.Add(smrcTrack);
            }

            // Pad with empty tracks if needed
            while (smrcTracks.Count < SMRC_TRACK_COUNT)
            {
                smrcTracks.Add(new SmrcTrack { Events = new List<SmrcEvent>() });
            }

            // Write S-MRC header
            // Song title (32 bytes)
            Byte[] titleBytes = new Byte[32];
            if (!string.IsNullOrEmpty(songTitle))
            {
                Byte[] titleEncoded = Encoding.ASCII.GetBytes(songTitle);
                Array.Copy(titleEncoded, 0, titleBytes, 0, Math.Min(titleEncoded.Length, 31));
            }
            writer.Write(titleBytes);

            // PPQN (always 96)
            writer.Write((Int16)SMRC_PPQN);

            // Time signature
            writer.Write(timeSignatureNumerator ?? (Byte)0);
            writer.Write(timeSignatureDenominator ?? (Byte)0);

            // Tempo
            writer.Write(tempoBpm ?? (Byte)120);

            // Reserved bytes
            writer.Write(new Byte[3]);

            // Calculate track offsets and write track directory
            Int32 currentOffset = SMRC_HEADER_SIZE;
            List<Byte[]> trackDataBuffers = new List<Byte[]>();

            for (Int32 i = 0; i < SMRC_TRACK_COUNT; i++)
            {
                Byte[] trackData = ConvertTrackToSmrcFormat(smrcTracks[i]);
                trackDataBuffers.Add(trackData);

                writer.Write(currentOffset); // Start offset
                writer.Write(trackData.Length); // Length
                writer.Write(new Byte[8]); // Reserved

                currentOffset += trackData.Length;
            }

            // Write track data
            foreach (Byte[] trackData in trackDataBuffers)
            {
                writer.Write(trackData);
            }
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
        private SmrcTrack ParseMidiTrack(Byte[] trackData, Int16 division, Boolean isFirstTrack,
            ref String songTitle, ref Byte? timeSignatureNumerator, ref Byte? timeSignatureDenominator, ref Byte? tempoBpm)
        {
            TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: Starting parse of track (isFirstTrack={isFirstTrack})");
            SmrcTrack track = new SmrcTrack { Events = new List<SmrcEvent>() };
            Int32 pos = 0;
            Byte runningStatus = 0;
            Int32 accumulatedDelta = 0;
            Double scaleFactor = (Double)SMRC_PPQN / division;

            while (pos < trackData.Length)
            {
                // Read variable-length delta time
                Int32 deltaTime = ReadVariableLengthValue(trackData, ref pos);
                accumulatedDelta += (Int32)(deltaTime * scaleFactor);
                TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: Read delta time {deltaTime} (scaled to {accumulatedDelta})");

                if (pos >= trackData.Length)
                {
                    break;
                }

                Byte status = trackData[pos];
                if (status < 0x80)
                {
                    // Running status
                    status = runningStatus;
                    TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: Using running status 0x{status:X2}");
                }
                else
                {
                    pos++;
                    runningStatus = status;
                    TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: New status byte 0x{status:X2}");
                }

                // Handle different event types
                if (status == 0xFF) // Meta event
                {
                    if (pos >= trackData.Length)
                    {
                        throw new InvalidDataException("Unexpected end of track while reading meta event type");
                    }

                    Byte metaType = trackData[pos++];
                    TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: Reading meta event type=0x{metaType:X2} at position {pos}");

                    // Log the next few bytes to help diagnose the issue
                    StringBuilder hexDump = new StringBuilder();
                    for (int j = 0; j < Math.Min(4, trackData.Length - pos); j++)
                    {
                        hexDump.Append($"0x{trackData[pos + j]:X2} ");
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

                    if (metaType == 0x03 && isFirstTrack) // Track name
                    {
                        songTitle = Encoding.ASCII.GetString(trackData, pos, Math.Min(metaLen, 31));
                        TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: Found track name meta event: '{songTitle}'");
                        // Preserve as SmrcEvent
                        byte[] data = new byte[metaLen + 1];
                        data[0] = 0x03;
                        Array.Copy(trackData, pos, data, 1, metaLen);
                        track.Events.Add(new SmrcEvent
                        {
                            DeltaTime = (Int16)Math.Min(accumulatedDelta, 65535),
                            Status = 0xFF,
                            Data = data
                        });
                        TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: Added track name meta event to track");
                        accumulatedDelta = 0;
                    }
                    else if (metaType == 0x51 && metaLen == 3) // Tempo
                    {
                        Int32 microsecondsPerQuarter = (trackData[pos] << 16) |
                                                      (trackData[pos + 1] << 8) |
                                                      trackData[pos + 2];
                        tempoBpm = (Byte)(60000000 / microsecondsPerQuarter);
                        TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: Found tempo meta event: {tempoBpm} BPM");

                        // Add Roland tempo event
                        SmrcEvent tempoEvent = new SmrcEvent
                        {
                            DeltaTime = (Int16)Math.Min(accumulatedDelta, 65535),
                            Status = 0xF7,
                            RolandMetaType = 0x01,
                            RolandMetaData = new Byte[] {
                                (Byte)(microsecondsPerQuarter & 0xFF),
                                (Byte)((microsecondsPerQuarter >> 8) & 0xFF)
                            }
                        };
                        track.Events.Add(tempoEvent);
                        TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: Added Roland tempo event");
                        accumulatedDelta = 0;
                    }
                    else if (metaType == 0x58 && metaLen == 4) // Time signature
                    {
                        timeSignatureNumerator = trackData[pos];
                        timeSignatureDenominator = trackData[pos + 1];
                        TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: Found time signature meta event: {timeSignatureNumerator}/{1 << timeSignatureDenominator}");

                        // Add Roland time signature event
                        SmrcEvent timeSigEvent = new SmrcEvent
                        {
                            DeltaTime = (Int16)Math.Min(accumulatedDelta, 65535),
                            Status = 0xF7,
                            RolandMetaType = 0x02,
                            RolandMetaData = new Byte[] { timeSignatureNumerator.Value, timeSignatureDenominator.Value }
                        };
                        track.Events.Add(timeSigEvent);
                        TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: Added Roland time signature event");
                        accumulatedDelta = 0;
                    }
                    else if (metaType == 0x2F) // End of track
                    {
                        TraceLogger.Log("SMrcToMidiConverter", "ParseMidiTrack: Found end of track meta event");
                        // Preserve as SmrcEvent
                        track.Events.Add(new SmrcEvent
                        {
                            DeltaTime = (Int16)Math.Min(accumulatedDelta, 65535),
                            Status = 0xFF,
                            Data = new byte[] { 0x2F }  // Just the meta type, no data
                        });
                        accumulatedDelta = 0;
                        break;
                    }
                    else
                    {
                        TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: Preserving other meta event type=0x{metaType:X2}");
                        // Preserve other meta events
                        if (pos + metaLen > trackData.Length)
                        {
                            throw new InvalidDataException($"Meta event data extends beyond track end. Position={pos}, Length={metaLen}, TrackLength={trackData.Length}");
                        }
                        byte[] data = new byte[metaLen + 1];
                        data[0] = metaType;
                        Array.Copy(trackData, pos, data, 1, metaLen);
                        track.Events.Add(new SmrcEvent
                        {
                            DeltaTime = (Int16)Math.Min(accumulatedDelta, 65535),
                            Status = 0xFF,
                            Data = data
                        });
                        accumulatedDelta = 0;
                    }

                    pos += metaLen;
                }
                else if (status >= 0x80 && status <= 0xEF) // MIDI events
                {
                    Int32 dataLength = GetMidiDataLength(status);
                    byte channel = (byte)(status & 0x0F);
                    string midiEventType = status switch
                    {
                        var s when s >= 0x80 && s <= 0x8F => "Note Off",
                        var s when s >= 0x90 && s <= 0x9F => "Note On",
                        var s when s >= 0xA0 && s <= 0xAF => "Poly Aftertouch",
                        var s when s >= 0xB0 && s <= 0xBF => "Control Change",
                        var s when s >= 0xC0 && s <= 0xCF => "Program Change",
                        var s when s >= 0xD0 && s <= 0xDF => "Channel Aftertouch",
                        var s when s >= 0xE0 && s <= 0xEF => "Pitch Bend",
                        _ => "Unknown MIDI Event"
                    };
                    TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: Found MIDI event type=0x{status:X2} ({midiEventType} on channel {channel}), data length={dataLength}");

                    while (accumulatedDelta > 65535)
                    {
                        // Split large deltas
                        track.Events.Add(new SmrcEvent
                        {
                            DeltaTime = 65535,
                            Status = 0x80, // Note off with velocity 0 (no-op)
                            Data = new Byte[] { 0x00, 0x00 }
                        });
                        accumulatedDelta -= 65535;
                        TraceLogger.Log("SMrcToMidiConverter", "ParseMidiTrack: Split large delta time");
                    }

                    SmrcEvent evt = new SmrcEvent
                    {
                        DeltaTime = (Int16)accumulatedDelta,
                        Status = status,
                        Data = new Byte[dataLength]
                    };

                    Array.Copy(trackData, pos, evt.Data, 0, dataLength);
                    pos += dataLength;

                    track.Events.Add(evt);
                    TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: Added MIDI event with data: {BitConverter.ToString(evt.Data)}");
                    accumulatedDelta = 0;
                }
                else if (status == 0xF0 || status == 0xF7) // SysEx
                {
                    Int32 sysexLength = ReadVariableLengthValue(trackData, ref pos);
                    TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: Found SysEx event, length={sysexLength}");

                    SmrcEvent evt = new SmrcEvent
                    {
                        DeltaTime = (Int16)Math.Min(accumulatedDelta, 65535),
                        Status = status,
                        Data = new Byte[sysexLength]
                    };

                    Array.Copy(trackData, pos, evt.Data, 0, sysexLength);
                    pos += sysexLength;

                    track.Events.Add(evt);
                    TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: Added SysEx event with data: {BitConverter.ToString(evt.Data)}");
                    accumulatedDelta = 0;
                }
                else
                {
                    // Skip unknown events
                    TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: Skipping unknown event type=0x{status:X2}");
                    pos++;
                }
            }

            TraceLogger.Log("SMrcToMidiConverter", $"ParseMidiTrack: Completed parsing track with {track.Events.Count} events");
            return track;
        }

        /// <summary>
        /// Converts a SmrcTrack to its binary S-MRC format representation.
        /// </summary>
        /// <param name="track">The SmrcTrack to convert.</param>
        /// <returns>A byte array containing the track data in S-MRC format.</returns>
        private Byte[] ConvertTrackToSmrcFormat(SmrcTrack track)
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
                    writer.Write(GetBigEndianBytes((Int16)evt.DeltaTime));

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
                    else if (evt.Status >= 0x80 && evt.Status <= 0xEF)
                    {
                        byte channel = (byte)(evt.Status & 0x0F);
                        string midiEventType = evt.Status switch
                        {
                            var s when s >= 0x80 && s <= 0x8F => "Note Off",
                            var s when s >= 0x90 && s <= 0x9F => "Note On",
                            var s when s >= 0xA0 && s <= 0xAF => "Poly Aftertouch",
                            var s when s >= 0xB0 && s <= 0xBF => "Control Change",
                            var s when s >= 0xC0 && s <= 0xCF => "Program Change",
                            var s when s >= 0xD0 && s <= 0xDF => "Channel Aftertouch",
                            var s when s >= 0xE0 && s <= 0xEF => "Pitch Bend",
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
                        writer.Write((Byte)0x7D);
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
                    writer.Write(GetBigEndianBytes((Int16)0)); // Delta time
                    writer.Write((Byte)0xFF); // Meta event
                    writer.Write((Byte)0x2F); // End of track
                    WriteVariableLengthValue(writer, 0); // Length as variable-length value
                }

                byte[] result = stream.ToArray();
                TraceLogger.Log("SMrcToMidiConverter", $"ConvertTrackToSmrcFormat: Raw track data: {BitConverter.ToString(result)}");
                return result;
            }
        }

        /// <summary>
        /// Reads a variable-length value from a byte array.
        /// </summary>
        /// <param name="data">The byte array containing the data.</param>
        /// <param name="pos">The current position in the array, which will be updated.</param>
        /// <returns>The decoded variable-length value.</returns>
        private Int32 ReadVariableLengthValue(Byte[] data, ref Int32 pos)
        {
            Int32 value = 0;
            Byte currentByte;
            Int32 startPos = pos;

            do
            {
                if (pos >= data.Length)
                {
                    throw new InvalidDataException($"Unexpected end of data while reading variable-length value at position {startPos}");
                }

                currentByte = data[pos++];
                value = (value << 7) | (currentByte & 0x7F);
                TraceLogger.Log("SMrcToMidiConverter", $"ReadVariableLengthValue: Read byte 0x{currentByte:X2}, current value={value}, position={pos}");
            } while ((currentByte & 0x80) != 0);

            TraceLogger.Log("SMrcToMidiConverter", $"ReadVariableLengthValue: Final value={value}, bytes read={pos - startPos}");
            return value;
        }

        /// <summary>
        /// Reads and validates the S-MRC file header.
        /// </summary>
        /// <param name="reader">The binary reader for the input stream.</param>
        /// <returns>A SmrcHeader object containing the header information.</returns>
        /// <exception cref="InvalidDataException">Thrown when the header is invalid.</exception>
        private SmrcHeader ReadSmrcHeader(BinaryReader reader)
        {
            SmrcHeader header = new SmrcHeader();

            // Read song title (32 bytes)
            Byte[] titleBytes = reader.ReadBytes(32);
            Int32 titleLength = Array.IndexOf(titleBytes, (Byte)0);
            if (titleLength == -1)
            {
                titleLength = 32;
            }

            header.Title = Encoding.ASCII.GetString(titleBytes, 0, titleLength);

            // Read PPQN (should be 96)
            header.Ppqn = reader.ReadInt16();
            if (header.Ppqn != SMRC_PPQN)
            {
                throw new InvalidDataException($"Invalid PPQN. Expected {SMRC_PPQN}, got {header.Ppqn}");
            }

            // Read time signature
            header.TimeSignatureNumerator = reader.ReadByte();
            header.TimeSignatureDenominator = reader.ReadByte();

            // Read tempo hint
            header.TempoBpm = reader.ReadByte();
            TraceLogger.Log("SMrcToMidiConverter", $"Read tempo hint: {header.TempoBpm}");
            if (header.TempoBpm <= 0 || header.TempoBpm > 255)
            {
                TraceLogger.Log("SMrcToMidiConverter", $"Invalid tempo hint: {header.TempoBpm}. Expected value between 1 and 255.");
                throw new InvalidDataException($"Invalid tempo hint: {header.TempoBpm}. Expected value between 1 and 255.");
            }

            // Skip reserved bytes
            reader.ReadBytes(3);

            // Read track directory (8 tracks)
            header.TrackDirectory = new TrackDirectoryEntry[SMRC_TRACK_COUNT];
            for (Int32 i = 0; i < SMRC_TRACK_COUNT; i++)
            {
                header.TrackDirectory[i] = new TrackDirectoryEntry
                {
                    StartOffset = reader.ReadInt32(),
                    Length = reader.ReadInt32()
                };
                reader.ReadBytes(8); // Skip reserved bytes
            }

            return header;
        }

        /// <summary>
        /// Reads all tracks from an S-MRC file.
        /// </summary>
        /// <param name="reader">The binary reader for the input stream.</param>
        /// <param name="header">The previously read S-MRC header.</param>
        /// <returns>A list of SmrcTrack objects containing the track data.</returns>
        private List<SmrcTrack> ReadSmrcTracks(BinaryReader reader, SmrcHeader header)
        {
            List<SmrcTrack> tracks = new List<SmrcTrack>();

            for (Int32 i = 0; i < SMRC_TRACK_COUNT; i++)
            {
                TrackDirectoryEntry entry = header.TrackDirectory[i];
                if (entry.Length == 0)
                {
                    TraceLogger.Log("SMrcToMidiConverter", $"ReadSmrcTracks: Skipping empty track {i + 1}");
                    continue; // Skip empty tracks
                }

                TraceLogger.Log("SMrcToMidiConverter", $"ReadSmrcTracks: Reading track {i + 1} at offset {entry.StartOffset}, length {entry.Length}");
                // Seek to track start
                _inputStream.Position = entry.StartOffset;

                SmrcTrack track = new SmrcTrack();
                track.Events = new List<SmrcEvent>();

                Int32 bytesRead = 0;
                Byte runningStatus = 0;
                while (bytesRead < entry.Length)
                {
                    // Check if we have enough bytes left for a complete event
                    if (bytesRead + 3 > entry.Length) // Need at least 2 bytes for delta time and 1 for status
                    {
                        throw new InvalidDataException("Incomplete event at end of track");
                    }

                    SmrcEvent evt = new SmrcEvent();

                    // Read delta time (16-bit big-endian)
                    evt.DeltaTime = ReadInt16BigEndian(reader);
                    bytesRead += 2;

                    // Read status byte
                    Byte status = reader.ReadByte();
                    bytesRead += 1;

                    // Handle running status
                    if (status < 0x80)
                    {
                        if (runningStatus == 0)
                        {
                            throw new InvalidDataException($"Invalid status byte: 0x{status:X2}");
                        }
                        status = runningStatus;
                    }
                    else
                    {
                        runningStatus = status;
                    }

                    evt.Status = status;

                    string eventType = status switch
                    {
                        0xFF => "Meta Event",
                        0xF7 => "Roland Meta Event",
                        var s when s >= 0x80 && s <= 0x8F => "Note Off",
                        var s when s >= 0x90 && s <= 0x9F => "Note On",
                        var s when s >= 0xA0 && s <= 0xAF => "Poly Aftertouch",
                        var s when s >= 0xB0 && s <= 0xBF => "Control Change",
                        var s when s >= 0xC0 && s <= 0xCF => "Program Change",
                        var s when s >= 0xD0 && s <= 0xDF => "Channel Aftertouch",
                        var s when s >= 0xE0 && s <= 0xEF => "Pitch Bend",
                        var s when s == 0xF0 => "SysEx",
                        _ => "Unknown Event"
                    };

                    TraceLogger.Log("SMrcToMidiConverter", $"ReadSmrcTracks: Read event status=0x{status:X2} ({eventType}), delta={evt.DeltaTime}");

                    // Read data bytes based on status
                    if (status == 0xFF) // Meta event
                    {
                        if (bytesRead + 2 > entry.Length) // Need 2 more bytes for meta type and length
                        {
                            throw new InvalidDataException("Incomplete meta event at end of track");
                        }

                        // Read meta type byte
                        byte metaType = reader.ReadByte();
                        bytesRead += 1;

                        string metaTypeStr = metaType switch
                        {
                            0x00 => "Sequence Number",
                            0x01 => "Text Event",
                            0x02 => "Copyright Notice",
                            0x03 => "Track Name",
                            0x04 => "Instrument Name",
                            0x05 => "Lyric",
                            0x06 => "Marker",
                            0x07 => "Cue Point",
                            0x20 => "Channel Prefix",
                            0x2F => "End of Track",
                            0x51 => "Set Tempo",
                            0x54 => "SMPTE Offset",
                            0x58 => "Time Signature",
                            0x59 => "Key Signature",
                            _ => $"Unknown Meta Type 0x{metaType:X2}"
                        };

                        TraceLogger.Log("SMrcToMidiConverter", $"ReadSmrcTracks: Found meta event type=0x{metaType:X2} ({metaTypeStr})");

                        // Read length as a variable-length value
                        Int32 metaLength = ReadVariableLengthValue(reader);
                        bytesRead += 1; // Account for the length byte
                        TraceLogger.Log("SMrcToMidiConverter", $"ReadSmrcTracks: Meta event length={metaLength}");

                        // Read meta data
                        evt.Data = new byte[metaLength + 1];
                        evt.Data[0] = metaType;
                        if (metaLength > 0)
                        {
                            if (bytesRead + metaLength > entry.Length)
                            {
                                throw new InvalidDataException($"Meta event data extends beyond track end. Position={bytesRead}, Length={metaLength}, TrackLength={entry.Length}");
                            }
                            reader.Read(evt.Data, 1, metaLength);
                            bytesRead += metaLength;

                            if (metaType == 0x03)
                            {
                                string trackName = Encoding.ASCII.GetString(evt.Data, 1, metaLength);
                                TraceLogger.Log("SMrcToMidiConverter", $"ReadSmrcTracks: Found track name meta event: '{trackName}'");
                            }
                            else
                            {
                                TraceLogger.Log("SMrcToMidiConverter", $"ReadSmrcTracks: Meta event data: {BitConverter.ToString(evt.Data, 1, metaLength)}");
                            }
                        }

                        if (metaType == 0x2F) // End of track
                        {
                            track.Events.Add(evt);
                            break; // End of track reached
                        }
                    }
                    else if (status == 0xF7) // Roland meta event
                    {
                        if (bytesRead + 1 > entry.Length)
                        {
                            throw new InvalidDataException("Incomplete Roland meta event at end of track");
                        }

                        Byte nextByte = reader.ReadByte();
                        bytesRead += 1;

                        if (nextByte == 0x7D)
                        {
                            if (bytesRead + 1 > entry.Length)
                            {
                                throw new InvalidDataException("Incomplete Roland meta event at end of track");
                            }

                            // Roland proprietary meta event
                            evt.RolandMetaType = reader.ReadByte();
                            bytesRead += 1;

                            string rolandMetaTypeStr = evt.RolandMetaType switch
                            {
                                0x01 => "Tempo",
                                0x02 => "Time Signature",
                                _ => $"Unknown Roland Meta Type 0x{evt.RolandMetaType:X2}"
                            };

                            TraceLogger.Log("SMrcToMidiConverter", $"ReadSmrcTracks: Found Roland meta event type=0x{evt.RolandMetaType:X2} ({rolandMetaTypeStr})");

                            if (evt.RolandMetaType == 0x01 || evt.RolandMetaType == 0x02)
                            {
                                if (bytesRead + 2 > entry.Length)
                                {
                                    throw new InvalidDataException("Incomplete Roland meta event data at end of track");
                                }
                                evt.RolandMetaData = reader.ReadBytes(2);
                                bytesRead += 2;
                                TraceLogger.Log("SMrcToMidiConverter", $"ReadSmrcTracks: Roland meta event data: {BitConverter.ToString(evt.RolandMetaData)}");
                            }
                        }
                    }
                    else if (status >= 0x80) // MIDI events
                    {
                        Int32 dataLength = GetMidiDataLength(status);
                        if (dataLength > 0)
                        {
                            if (bytesRead + dataLength > entry.Length)
                            {
                                throw new InvalidDataException("MIDI event data extends beyond track end");
                            }
                            evt.Data = reader.ReadBytes(dataLength);
                            bytesRead += dataLength;
                            TraceLogger.Log("SMrcToMidiConverter", $"ReadSmrcTracks: MIDI event data: {BitConverter.ToString(evt.Data)}");
                        }
                    }
                    else
                    {
                        throw new InvalidDataException($"Invalid status byte: 0x{status:X2}");
                    }

                    track.Events.Add(evt);
                }

                tracks.Add(track);
            }

            return tracks;
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
                        WriteVariableLengthValue(trackWriter, evt.DeltaTime);

                        // Handle Roland meta events
                        if (evt.Status == 0xF7 && evt.RolandMetaType.HasValue)
                        {
                            TraceLogger.Log("SMrcToMidiConverter", $"Converting Roland meta event type {evt.RolandMetaType}");
                            // Convert Roland meta to standard MIDI meta
                            trackWriter.Write((Byte)0xFF); // Meta event

                            if (evt.RolandMetaType == 0x01) // Tempo
                            {
                                trackWriter.Write((Byte)0x51); // Set Tempo
                                trackWriter.Write((Byte)0x03); // Length
                                // Convert little-endian to big-endian
                                Int32 microsecondsPerQuarter = evt.RolandMetaData[0] | (evt.RolandMetaData[1] << 8);
                                trackWriter.Write((Byte)(microsecondsPerQuarter >> 16));
                                trackWriter.Write((Byte)(microsecondsPerQuarter >> 8));
                                trackWriter.Write((Byte)microsecondsPerQuarter);
                            }
                            else if (evt.RolandMetaType == 0x02) // Time signature
                            {
                                trackWriter.Write((Byte)0x58); // Time Signature
                                trackWriter.Write((Byte)0x04); // Length
                                trackWriter.Write(evt.RolandMetaData[0]); // Numerator
                                trackWriter.Write(evt.RolandMetaData[1]); // Denominator
                                trackWriter.Write((Byte)24); // Clocks per metronome click
                                trackWriter.Write((Byte)8); // 32nd notes per quarter note
                            }
                        }
                        else if (evt.Status == 0xFF) // Standard MIDI meta event
                        {
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
                                var s when s >= 0x80 && s <= 0x8F => "Note Off",
                                var s when s >= 0x90 && s <= 0x9F => "Note On",
                                var s when s >= 0xA0 && s <= 0xAF => "Poly Aftertouch",
                                var s when s >= 0xB0 && s <= 0xBF => "Control Change",
                                var s when s >= 0xC0 && s <= 0xCF => "Program Change",
                                var s when s >= 0xD0 && s <= 0xDF => "Channel Aftertouch",
                                var s when s >= 0xE0 && s <= 0xEF => "Pitch Bend",
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
                        trackWriter.Write(GetBigEndianBytes((Int16)0)); // Delta time
                        trackWriter.Write((Byte)0xFF); // Meta event
                        trackWriter.Write((Byte)0x2F); // End of track
                        WriteVariableLengthValue(trackWriter, 0); // Length as variable-length value
                        TraceLogger.Log("SMrcToMidiConverter", "Added end of track meta event");
                    }

                    midiData.Tracks.Add(new MidiTrack { Data = trackStream.ToArray() });
                }
            }

            return midiData;
        }

        /// <summary>
        /// Writes a variable-length value to a binary stream.
        /// </summary>
        /// <param name="writer">The binary writer for the output stream.</param>
        /// <param name="value">The value to write.</param>
        private void WriteVariableLengthValue(BinaryWriter writer, Int32 value)
        {
            if (value < 0x80)
            {
                writer.Write((Byte)value);
            }
            else if (value < 0x4000)
            {
                writer.Write((Byte)(0x80 | (value >> 7)));
                writer.Write((Byte)(value & 0x7F));
            }
            else if (value < 0x200000)
            {
                writer.Write((Byte)(0x80 | (value >> 14)));
                writer.Write((Byte)(0x80 | ((value >> 7) & 0x7F)));
                writer.Write((Byte)(value & 0x7F));
            }
            else
            {
                writer.Write((Byte)(0x80 | (value >> 21)));
                writer.Write((Byte)(0x80 | ((value >> 14) & 0x7F)));
                writer.Write((Byte)(0x80 | ((value >> 7) & 0x7F)));
                writer.Write((Byte)(value & 0x7F));
            }
        }

        /// <summary>
        /// Gets the expected data length for a MIDI status byte.
        /// </summary>
        /// <param name="status">The MIDI status byte.</param>
        /// <returns>The number of data bytes expected for this status byte.</returns>
        private Int32 GetMidiDataLength(Byte status)
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

            if (status == 0xF0 || status == 0xF7)
            {
                return -1; // SysEx (variable length)
            }

            return 0; // System common/realtime
        }

        /// <summary>
        /// Reads a 32-bit integer in big-endian format.
        /// </summary>
        /// <param name="reader">The binary reader for the input stream.</param>
        /// <returns>The read 32-bit integer.</returns>
        private Int32 ReadInt32BigEndian(BinaryReader reader)
        {
            Byte[] bytes = reader.ReadBytes(4);
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
        /// Reads a 16-bit integer in big-endian format.
        /// </summary>
        /// <param name="reader">The binary reader for the input stream.</param>
        /// <returns>The read 16-bit integer.</returns>
        private Int16 ReadInt16BigEndian(BinaryReader reader)
        {
            Byte[] bytes = reader.ReadBytes(2);
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
        /// Converts a 32-bit integer to a big-endian byte array.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>A 4-byte array containing the big-endian representation.</returns>
        private Byte[] GetBigEndianBytes(Int32 value)
        {
            Byte[] bytes = BitConverter.GetBytes(value);
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
        private Byte[] GetBigEndianBytes(Int16 value)
        {
            Byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        private void ValidateMidiFile(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.ASCII, true))
            {
                // Validate header
                String header = new String(reader.ReadChars(4));
                if (header != STANDARD_MIDI_HEADER)
                {
                    throw new InvalidDataException($"Invalid MIDI header: {header}");
                }

                Int32 headerLength = ReadInt32BigEndian(reader);
                if (headerLength < 6)
                {
                    throw new InvalidDataException($"Invalid MIDI header length: {headerLength}");
                }

                Int16 format = ReadInt16BigEndian(reader);
                if (format > 1)
                {
                    throw new InvalidDataException($"Invalid MIDI format: {format}");
                }

                Int16 trackCount = ReadInt16BigEndian(reader);
                if (trackCount <= 0)
                {
                    throw new InvalidDataException($"Invalid track count: {trackCount}");
                }

                Int16 division = ReadInt16BigEndian(reader);
                if ((division & 0x8000) != 0)
                {
                    throw new InvalidDataException("SMPTE time division is not supported");
                }

                // Validate tracks
                for (int i = 0; i < trackCount; i++)
                {
                    String trackHeader = new String(reader.ReadChars(4));
                    if (trackHeader != STANDARD_MIDI_TRACK)
                    {
                        throw new InvalidDataException($"Invalid track header: {trackHeader}");
                    }

                    Int32 trackLength = ReadInt32BigEndian(reader);
                    if (trackLength <= 0)
                    {
                        throw new InvalidDataException($"Invalid track length: {trackLength}");
                    }

                    Byte[] trackData = reader.ReadBytes(trackLength);
                    ValidateMidiTrack(trackData);
                }
            }
        }

        private void ValidateMidiTrack(Byte[] trackData)
        {
            Int32 pos = 0;
            Boolean hasEndOfTrack = false;

            while (pos < trackData.Length)
            {
                // Read delta time
                Int32 delta = ReadVariableLengthValue(trackData, ref pos);
                if (pos >= trackData.Length) break;

                // Read status byte
                Byte status = trackData[pos];
                if (status < 0x80) status = 0; // running status not handled here
                else pos++;

                if (status == 0xFF) // Meta event
                {
                    if (pos >= trackData.Length) throw new InvalidDataException("Unexpected end of track data");
                    Byte metaType = trackData[pos++];
                    int metaLen = ReadVariableLengthValue(trackData, ref pos);
                    if (pos + metaLen > trackData.Length) throw new InvalidDataException("Meta event data extends beyond track end");
                    if (metaType == 0x2F) hasEndOfTrack = true;
                    pos += metaLen;
                }
                else if (status >= 0x80 && status <= 0xEF) // MIDI event
                {
                    Int32 dataLen = GetMidiDataLength(status);
                    if (pos + dataLen > trackData.Length) throw new InvalidDataException("MIDI event data extends beyond track end");
                    pos += dataLen;
                }
                else if (status == 0xF0 || status == 0xF7) // SysEx
                {
                    Int32 sysexLen = ReadVariableLengthValue(trackData, ref pos);
                    if (pos + sysexLen > trackData.Length) throw new InvalidDataException("SysEx data extends beyond track end");
                    pos += sysexLen;
                }
                else
                {
                    throw new InvalidDataException($"Invalid status byte: 0x{status:X2}");
                }
            }

            if (!hasEndOfTrack)
            {
                throw new InvalidDataException("Track does not end with an End of Track meta event");
            }
        }

        private void ValidateSmrcFile(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.ASCII, true))
            {
                // Validate header
                Byte[] titleBytes = reader.ReadBytes(32);
                Int32 titleLength = Array.IndexOf(titleBytes, (Byte)0);
                if (titleLength == -1) titleLength = 32;

                Int16 ppqn = reader.ReadInt16();
                if (ppqn != SMRC_PPQN)
                {
                    throw new InvalidDataException($"Invalid PPQN: {ppqn}");
                }

                Byte timeSignatureNumerator = reader.ReadByte();
                Byte timeSignatureDenominator = reader.ReadByte();
                Byte tempoBpm = reader.ReadByte();

                // Skip reserved bytes
                reader.ReadBytes(3);

                // Validate track directory
                TrackDirectoryEntry[] trackDirectory = new TrackDirectoryEntry[SMRC_TRACK_COUNT];
                for (Int32 i = 0; i < SMRC_TRACK_COUNT; i++)
                {
                    trackDirectory[i] = new TrackDirectoryEntry
                    {
                        StartOffset = reader.ReadInt32(),
                        Length = reader.ReadInt32()
                    };
                    reader.ReadBytes(8); // Skip reserved bytes

                    if (trackDirectory[i].StartOffset < SMRC_HEADER_SIZE)
                    {
                        throw new InvalidDataException($"Invalid track start offset: {trackDirectory[i].StartOffset}");
                    }
                }

                // Validate tracks
                for (Int32 i = 0; i < SMRC_TRACK_COUNT; i++)
                {
                    if (trackDirectory[i].Length == 0)
                    {
                        TraceLogger.Log("SMrcToMidiConverter", $"ValidateSmrcFile: Skipping validation of empty track {i + 1}");
                        continue;
                    }

                    stream.Position = trackDirectory[i].StartOffset;
                    ValidateSmrcTrack(reader, trackDirectory[i].Length);
                }
            }
        }

        private void ValidateSmrcTrack(BinaryReader reader, Int32 length)
        {
            Int32 bytesRead = 0;
            bool hasEndOfTrack = false;
            Byte runningStatus = 0;

            TraceLogger.Log("SMrcToMidiConverter", $"ValidateSmrcTrack: Starting validation of track with length {length}");

            while (bytesRead < length)
            {
                // Check if we have enough bytes left for a complete event
                if (bytesRead + 3 > length) // Need at least 2 bytes for delta time and 1 for status
                {
                    throw new InvalidDataException("Incomplete event at end of track");
                }

                // Read delta time
                Int16 deltaTime = ReadInt16BigEndian(reader);
                bytesRead += 2;
                TraceLogger.Log("SMrcToMidiConverter", $"ValidateSmrcTrack: Read delta time {deltaTime}, bytesRead={bytesRead}");

                // Read status byte
                Byte status = reader.ReadByte();
                bytesRead += 1;
                TraceLogger.Log("SMrcToMidiConverter", $"ValidateSmrcTrack: Read status byte 0x{status:X2}, bytesRead={bytesRead}");

                // Handle running status
                if (status < 0x80)
                {
                    if (runningStatus == 0)
                    {
                        TraceLogger.Log("SMrcToMidiConverter", $"ValidateSmrcTrack: Invalid status byte 0x{status:X2} with no running status");
                        throw new InvalidDataException($"Invalid status byte: 0x{status:X2}");
                    }
                    TraceLogger.Log("SMrcToMidiConverter", $"ValidateSmrcTrack: Using running status 0x{runningStatus:X2}");
                    status = runningStatus;
                }
                else
                {
                    runningStatus = status;
                    TraceLogger.Log("SMrcToMidiConverter", $"ValidateSmrcTrack: New running status 0x{status:X2}");
                }

                if (status == 0xFF) // Meta event
                {
                    if (bytesRead + 2 > length) // Need 2 more bytes for meta type and length
                    {
                        throw new InvalidDataException("Incomplete meta event at end of track");
                    }

                    Byte metaType = reader.ReadByte();
                    bytesRead += 1;
                    TraceLogger.Log("SMrcToMidiConverter", $"ValidateSmrcTrack: Meta event type=0x{metaType:X2}, bytesRead={bytesRead}");

                    Int32 metaLength = ReadVariableLengthValue(reader);
                    bytesRead += 1; // Account for the length byte
                    TraceLogger.Log("SMrcToMidiConverter", $"ValidateSmrcTrack: Meta event length={metaLength}, bytesRead={bytesRead}");

                    if (metaLength > 0)
                    {
                        if (bytesRead + metaLength > length)
                        {
                            throw new InvalidDataException("Meta event data extends beyond track end");
                        }
                        reader.ReadBytes(metaLength);
                        bytesRead += metaLength;
                        TraceLogger.Log("SMrcToMidiConverter", $"ValidateSmrcTrack: Read {metaLength} bytes of meta data, bytesRead={bytesRead}");
                    }

                    if (metaType == 0x2F)
                    {
                        hasEndOfTrack = true;
                        TraceLogger.Log("SMrcToMidiConverter", "ValidateSmrcTrack: Found end of track meta event");
                        break; // End of track reached
                    }
                }
                else if (status == 0xF7) // Roland meta event
                {
                    if (bytesRead + 1 > length)
                    {
                        throw new InvalidDataException("Incomplete Roland meta event at end of track");
                    }

                    Byte nextByte = reader.ReadByte();
                    bytesRead += 1;
                    TraceLogger.Log("SMrcToMidiConverter", $"ValidateSmrcTrack: Roland meta event next byte=0x{nextByte:X2}, bytesRead={bytesRead}");

                    if (nextByte == 0x7D)
                    {
                        if (bytesRead + 1 > length)
                        {
                            throw new InvalidDataException("Incomplete Roland meta event at end of track");
                        }

                        Byte rolandMetaType = reader.ReadByte();
                        bytesRead += 1;
                        TraceLogger.Log("SMrcToMidiConverter", $"ValidateSmrcTrack: Roland meta type=0x{rolandMetaType:X2}, bytesRead={bytesRead}");

                        if (rolandMetaType == 0x01 || rolandMetaType == 0x02)
                        {
                            if (bytesRead + 2 > length)
                            {
                                throw new InvalidDataException("Incomplete Roland meta event data at end of track");
                            }
                            reader.ReadBytes(2);
                            bytesRead += 2;
                            TraceLogger.Log("SMrcToMidiConverter", $"ValidateSmrcTrack: Read 2 bytes of Roland meta data, bytesRead={bytesRead}");
                        }
                    }
                }
                else if (status >= 0x80) // MIDI events
                {
                    Int32 dataLength = GetMidiDataLength(status);
                    if (dataLength > 0)
                    {
                        if (bytesRead + dataLength > length)
                        {
                            throw new InvalidDataException("MIDI event data extends beyond track end");
                        }
                        reader.ReadBytes(dataLength);
                        bytesRead += dataLength;
                        TraceLogger.Log("SMrcToMidiConverter", $"ValidateSmrcTrack: Read {dataLength} bytes of MIDI data, bytesRead={bytesRead}");
                    }
                }
                else
                {
                    TraceLogger.Log("SMrcToMidiConverter", $"ValidateSmrcTrack: Invalid status byte 0x{status:X2}");
                    throw new InvalidDataException($"Invalid status byte: 0x{status:X2}");
                }
            }

            if (!hasEndOfTrack)
            {
                TraceLogger.Log("SMrcToMidiConverter", "ValidateSmrcTrack: Track does not end with an End of Track meta event");
                throw new InvalidDataException("Track does not end with an End of Track meta event");
            }

            TraceLogger.Log("SMrcToMidiConverter", "ValidateSmrcTrack: Track validation completed successfully");
        }

        /// <summary>
        /// Reads a variable-length value from a BinaryReader.
        /// </summary>
        /// <param name="reader">The BinaryReader to read from.</param>
        /// <returns>The decoded variable-length value.</returns>
        private Int32 ReadVariableLengthValue(BinaryReader reader)
        {
            Int32 value = 0;
            Byte currentByte;
            do
            {
                currentByte = reader.ReadByte();
                value = (value << 7) | (currentByte & 0x7F);
            } while ((currentByte & 0x80) != 0);
            return value;
        }
    }
}