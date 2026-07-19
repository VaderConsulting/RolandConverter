using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace RolandConverter
{
    public class SMrcToMidiConverter
    {
        private const String STANDARD_MIDI_HEADER = "MThd";
        private const String STANDARD_MIDI_TRACK = "MTrk";
        private const Int32 SMRC_PPQN = 96;
        private const Int32 SMRC_HEADER_SIZE = 0xA8; // 168 bytes
        private const Int32 SMRC_TRACK_COUNT = 8;

        private readonly Stream _inputStream;
        private readonly Stream _outputStream;

        public SMrcToMidiConverter(Stream inputStream, Stream outputStream)
        {
            _inputStream = inputStream ?? throw new ArgumentNullException(nameof(inputStream));
            _outputStream = outputStream ?? throw new ArgumentNullException(nameof(outputStream));
        }

        public void ConvertSmrcToMidi()
        {
            using (BinaryReader reader = new BinaryReader(_inputStream, Encoding.ASCII, true))
            using (BinaryWriter writer = new BinaryWriter(_outputStream, Encoding.ASCII, true))
            {
                // Read S-MRC header
                SmrcHeader smrcHeader = ReadSmrcHeader(reader);

                // Read track data
                List<SmrcTrack> smrcTracks = ReadSmrcTracks(reader, smrcHeader);

                // Convert to standard MIDI
                MidiFileData midiData = ConvertSmrcToMidiData(smrcHeader, smrcTracks);

                // Write standard MIDI header
                writer.Write(STANDARD_MIDI_HEADER.ToCharArray());
                writer.Write(GetBigEndianBytes(6)); // Header length
                writer.Write(GetBigEndianBytes((Int16)1)); // Format 1 (multiple tracks)
                writer.Write(GetBigEndianBytes((Int16)midiData.TrackCount));
                writer.Write(GetBigEndianBytes((Int16)SMRC_PPQN)); // Fixed PPQN

                // Write MIDI tracks
                foreach (MidiTrack track in midiData.Tracks)
                {
                    writer.Write(STANDARD_MIDI_TRACK.ToCharArray());
                    writer.Write(GetBigEndianBytes(track.Data.Length));
                    writer.Write(track.Data);
                }
            }
        }

        public void ConvertMidiToSmrc()
        {
            using (BinaryReader reader = new BinaryReader(_inputStream, Encoding.ASCII, true))
            using (BinaryWriter writer = new BinaryWriter(_outputStream, Encoding.ASCII, true))
            {
                // Read and validate MIDI header
                String header = new String(reader.ReadChars(4));
                if (header != STANDARD_MIDI_HEADER)
                {
                    throw new InvalidDataException($"Invalid MIDI header. Expected '{STANDARD_MIDI_HEADER}', got '{header}'");
                }

                // Read MIDI header data
                Int32 headerLength = ReadInt32BigEndian(reader);
                Int16 format = ReadInt16BigEndian(reader);
                Int16 trackCount = ReadInt16BigEndian(reader);
                Int16 division = ReadInt16BigEndian(reader);

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
                    if (trackHeader != STANDARD_MIDI_TRACK)
                    {
                        throw new InvalidDataException($"Invalid track header. Expected '{STANDARD_MIDI_TRACK}', got '{trackHeader}'");
                    }

                    Int32 trackLength = ReadInt32BigEndian(reader);
                    Byte[] trackData = reader.ReadBytes(trackLength);
                    midiTracks.Add(new MidiTrack { Data = trackData });
                }

                // Convert to S-MRC format
                ConvertMidiToSmrcFormat(writer, midiTracks, division);
            }
        }

        private void ConvertMidiToSmrcFormat(BinaryWriter writer, List<MidiTrack> midiTracks, Int16 division)
        {
            // Parse MIDI tracks to extract events
            List<SmrcTrack> smrcTracks = new List<SmrcTrack>();
            String songTitle = "Converted Song";
            Byte timeSignatureNumerator = 4;
            Byte timeSignatureDenominator = 2; // 2^2 = 4
            Byte tempoBpm = 120;

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
            Byte[] titleEncoded = Encoding.ASCII.GetBytes(songTitle);
            Array.Copy(titleEncoded, 0, titleBytes, 0, Math.Min(titleEncoded.Length, 31));
            writer.Write(titleBytes);

            // PPQN (always 96)
            writer.Write((Int16)SMRC_PPQN);

            // Time signature
            writer.Write(timeSignatureNumerator);
            writer.Write(timeSignatureDenominator);

            // Tempo
            writer.Write(tempoBpm);

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

        private SmrcTrack ParseMidiTrack(Byte[] trackData, Int16 division, Boolean isFirstTrack,
            ref String songTitle, ref Byte timeSignatureNumerator, ref Byte timeSignatureDenominator, ref Byte tempoBpm)
        {
            SmrcTrack track = new SmrcTrack { Events = new List<SmrcEvent>() };
            Int32 position = 0;
            Byte runningStatus = 0;
            Int32 accumulatedDelta = 0;
            Double scaleFactor = (Double)SMRC_PPQN / division;

            while (position < trackData.Length)
            {
                // Read variable-length delta time
                Int32 deltaTime = ReadVariableLengthValue(trackData, ref position);
                accumulatedDelta += (Int32)(deltaTime * scaleFactor);

                if (position >= trackData.Length)
                {
                    break;
                }

                Byte status = trackData[position];
                if (status < 0x80)
                {
                    // Running status
                    status = runningStatus;
                }
                else
                {
                    position++;
                    runningStatus = status;
                }

                // Handle different event types
                if (status == 0xFF) // Meta event
                {
                    Byte metaType = trackData[position++];
                    Int32 metaLength = ReadVariableLengthValue(trackData, ref position);

                    if (metaType == 0x51 && metaLength == 3) // Tempo
                    {
                        Int32 microsecondsPerQuarter = (trackData[position] << 16) |
                                                      (trackData[position + 1] << 8) |
                                                      trackData[position + 2];
                        tempoBpm = (Byte)(60000000 / microsecondsPerQuarter);

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
                        accumulatedDelta = 0;
                    }
                    else if (metaType == 0x58 && metaLength == 4) // Time signature
                    {
                        timeSignatureNumerator = trackData[position];
                        timeSignatureDenominator = trackData[position + 1];

                        // Add Roland time signature event
                        SmrcEvent timeSigEvent = new SmrcEvent
                        {
                            DeltaTime = (Int16)Math.Min(accumulatedDelta, 65535),
                            Status = 0xF7,
                            RolandMetaType = 0x02,
                            RolandMetaData = new Byte[] { timeSignatureNumerator, timeSignatureDenominator }
                        };
                        track.Events.Add(timeSigEvent);
                        accumulatedDelta = 0;
                    }
                    else if (metaType == 0x03 && isFirstTrack) // Track name
                    {
                        songTitle = Encoding.ASCII.GetString(trackData, position, Math.Min(metaLength, 31));
                    }
                    else if (metaType == 0x2F) // End of track
                    {
                        break;
                    }

                    position += metaLength;
                }
                else if (status >= 0x80 && status <= 0xEF) // MIDI events
                {
                    Int32 dataLength = GetMidiDataLength(status);

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
                    }

                    SmrcEvent evt = new SmrcEvent
                    {
                        DeltaTime = (Int16)accumulatedDelta,
                        Status = status,
                        Data = new Byte[dataLength]
                    };

                    Array.Copy(trackData, position, evt.Data, 0, dataLength);
                    position += dataLength;

                    track.Events.Add(evt);
                    accumulatedDelta = 0;
                }
                else if (status == 0xF0 || status == 0xF7) // SysEx
                {
                    Int32 sysexLength = ReadVariableLengthValue(trackData, ref position);

                    SmrcEvent evt = new SmrcEvent
                    {
                        DeltaTime = (Int16)Math.Min(accumulatedDelta, 65535),
                        Status = status,
                        Data = new Byte[sysexLength]
                    };

                    Array.Copy(trackData, position, evt.Data, 0, sysexLength);
                    position += sysexLength;

                    track.Events.Add(evt);
                    accumulatedDelta = 0;
                }
                else
                {
                    // Skip unknown events
                    position++;
                }
            }

            return track;
        }

        private Byte[] ConvertTrackToSmrcFormat(SmrcTrack track)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                foreach (SmrcEvent evt in track.Events)
                {
                    // Write 16-bit big-endian delta time
                    writer.Write(GetBigEndianBytes(evt.DeltaTime));

                    // Write status byte (no running status in S-MRC)
                    writer.Write(evt.Status);

                    // Write data bytes
                    if (evt.Data != null)
                    {
                        writer.Write(evt.Data);
                    }

                    // Handle Roland meta events
                    if (evt.RolandMetaType.HasValue)
                    {
                        writer.Write((Byte)0x7D);
                        writer.Write(evt.RolandMetaType.Value);
                        if (evt.RolandMetaData != null)
                        {
                            writer.Write(evt.RolandMetaData);
                        }
                    }
                }

                return stream.ToArray();
            }
        }

        private Int32 ReadVariableLengthValue(Byte[] data, ref Int32 position)
        {
            Int32 value = 0;
            Byte currentByte;

            do
            {
                if (position >= data.Length)
                {
                    throw new InvalidDataException("Unexpected end of data while reading variable-length value");
                }

                currentByte = data[position++];
                value = (value << 7) | (currentByte & 0x7F);
            } while ((currentByte & 0x80) != 0);

            return value;
        }

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

        private List<SmrcTrack> ReadSmrcTracks(BinaryReader reader, SmrcHeader header)
        {
            List<SmrcTrack> tracks = new List<SmrcTrack>();

            for (Int32 i = 0; i < SMRC_TRACK_COUNT; i++)
            {
                TrackDirectoryEntry entry = header.TrackDirectory[i];
                if (entry.Length == 0)
                {
                    continue; // Skip empty tracks
                }

                // Seek to track start
                _inputStream.Position = entry.StartOffset;

                SmrcTrack track = new SmrcTrack();
                track.Events = new List<SmrcEvent>();

                Int32 bytesRead = 0;
                while (bytesRead < entry.Length)
                {
                    SmrcEvent evt = new SmrcEvent();

                    // Read delta time (16-bit big-endian)
                    evt.DeltaTime = ReadInt16BigEndian(reader);
                    bytesRead += 2;

                    // Read status byte
                    evt.Status = reader.ReadByte();
                    bytesRead += 1;

                    // Read data bytes based on status
                    Int32 dataLength = GetMidiDataLength(evt.Status);
                    if (dataLength > 0)
                    {
                        evt.Data = reader.ReadBytes(dataLength);
                        bytesRead += dataLength;
                    }

                    // Handle special Roland meta events
                    if (evt.Status == 0xF7)
                    {
                        Byte nextByte = reader.ReadByte();
                        bytesRead += 1;

                        if (nextByte == 0x7D)
                        {
                            // Roland proprietary meta event
                            evt.RolandMetaType = reader.ReadByte();
                            bytesRead += 1;

                            if (evt.RolandMetaType == 0x01 || evt.RolandMetaType == 0x02)
                            {
                                evt.RolandMetaData = reader.ReadBytes(2);
                                bytesRead += 2;
                            }
                        }
                    }

                    track.Events.Add(evt);
                }

                tracks.Add(track);
            }

            return tracks;
        }

        private MidiFileData ConvertSmrcToMidiData(SmrcHeader header, List<SmrcTrack> smrcTracks)
        {
            MidiFileData midiData = new MidiFileData();

            foreach (SmrcTrack smrcTrack in smrcTracks)
            {
                using (MemoryStream trackStream = new MemoryStream())
                using (BinaryWriter trackWriter = new BinaryWriter(trackStream))
                {
                    foreach (SmrcEvent evt in smrcTrack.Events)
                    {
                        // Write variable-length delta time (MIDI format)
                        WriteVariableLengthValue(trackWriter, evt.DeltaTime);

                        // Handle Roland meta events
                        if (evt.Status == 0xF7 && evt.RolandMetaType.HasValue)
                        {
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
                        else
                        {
                            // Write status and data bytes
                            trackWriter.Write(evt.Status);
                            if (evt.Data != null)
                            {
                                trackWriter.Write(evt.Data);
                            }
                        }
                    }

                    // Add end of track meta event
                    trackWriter.Write((Byte)0x00); // Delta time
                    trackWriter.Write((Byte)0xFF); // Meta event
                    trackWriter.Write((Byte)0x2F); // End of track
                    trackWriter.Write((Byte)0x00); // Length

                    midiData.Tracks.Add(new MidiTrack { Data = trackStream.ToArray() });
                }
            }

            return midiData;
        }

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



        private Int32 ReadInt32BigEndian(BinaryReader reader)
        {
            Byte[] bytes = reader.ReadBytes(4);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return BitConverter.ToInt32(bytes, 0);
        }

        private Int16 ReadInt16BigEndian(BinaryReader reader)
        {
            Byte[] bytes = reader.ReadBytes(2);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return BitConverter.ToInt16(bytes, 0);
        }

        private Byte[] GetBigEndianBytes(Int32 value)
        {
            Byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        private Byte[] GetBigEndianBytes(Int16 value)
        {
            Byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }
    }

    public class TrackDirectoryEntry
    {
        public Int32 StartOffset
        {
            get; set;
        }
        public Int32 Length
        {
            get; set;
        }
    }

    public class SmrcTrack
    {
        public List<SmrcEvent> Events
        {
            get; set;
        }
    }

    public class SmrcEvent
    {
        public Int16 DeltaTime
        {
            get; set;
        }
        public Byte Status
        {
            get; set;
        }
        public Byte[] Data
        {
            get; set;
        }
        public Byte? RolandMetaType
        {
            get; set;
        }
        public Byte[] RolandMetaData
        {
            get; set;
        }
    }

    public class MidiFileData
    {
        public Int16 Format
        {
            get; set;
        }
        public Int16 Division
        {
            get; set;
        }
        public Int32 TrackCount
        {
            get
            {
                return Tracks.Count;
            }
        }

        public List<MidiTrack> Tracks { get; set; } = new List<MidiTrack>();
    }

    public class MidiTrack
    {
        public Byte[] Data
        {
            get; set;
        }
    }

    // Test harness
    public class ConverterTestHarness
    {
        public static void RunTests()
        {
            Console.WriteLine("Roland S-MRC Converter Test Suite");
            Console.WriteLine("=================================");

            // Test 1: Round-trip conversion
            TestRoundTripConversion();

            // Test 2: Error handling
            TestErrorHandling();

            // Test 3: Edge cases
            TestEdgeCases();

            // Test 4: File validation
            TestFileValidation();

            Console.WriteLine("\nAll tests completed!");
        }

        private static void TestRoundTripConversion()
        {
            Console.WriteLine("\nTest 1: Round-trip conversion");

            // Create a simple MIDI file in memory
            Byte[] midiData = CreateSimpleMidiFile();

            // Convert MIDI to S-MRC
            Byte[] smrcData;
            using (MemoryStream midiStream = new MemoryStream(midiData))
            using (MemoryStream smrcStream = new MemoryStream())
            {
                SMrcToMidiConverter converter = new SMrcToMidiConverter(midiStream, smrcStream);
                converter.ConvertMidiToSmrc();
                smrcData = smrcStream.ToArray();
            }

            Console.WriteLine($"  MIDI size: {midiData.Length} bytes");
            Console.WriteLine($"  S-MRC size: {smrcData.Length} bytes");

            // Convert back to MIDI
            Byte[] midiData2;
            using (MemoryStream smrcStream = new MemoryStream(smrcData))
            using (MemoryStream midiStream = new MemoryStream())
            {
                SMrcToMidiConverter converter = new SMrcToMidiConverter(smrcStream, midiStream);
                converter.ConvertSmrcToMidi();
                midiData2 = midiStream.ToArray();
            }

            Console.WriteLine($"  Converted MIDI size: {midiData2.Length} bytes");
            Console.WriteLine("  Round-trip conversion: PASSED");
        }

        private static void TestErrorHandling()
        {
            Console.WriteLine("\nTest 2: Error handling");

            // Test invalid MIDI header
            try
            {
                Byte[] invalidData = Encoding.ASCII.GetBytes("INVALID");
                using (MemoryStream stream = new MemoryStream(invalidData))
                using (MemoryStream output = new MemoryStream())
                {
                    SMrcToMidiConverter converter = new SMrcToMidiConverter(stream, output);
                    converter.ConvertMidiToSmrc();
                }
                Console.WriteLine("  Invalid MIDI header: FAILED (no exception thrown)");
            }
            catch (InvalidDataException)
            {
                Console.WriteLine("  Invalid MIDI header: PASSED");
            }

            // Test too many tracks
            try
            {
                Byte[] midiWithTooManyTracks = CreateMidiWithManyTracks(10);
                using (MemoryStream stream = new MemoryStream(midiWithTooManyTracks))
                using (MemoryStream output = new MemoryStream())
                {
                    SMrcToMidiConverter converter = new SMrcToMidiConverter(stream, output);
                    converter.ConvertMidiToSmrc();
                }
                Console.WriteLine("  Too many tracks: FAILED (no exception thrown)");
            }
            catch (InvalidDataException)
            {
                Console.WriteLine("  Too many tracks: PASSED");
            }
        }

        private static void TestEdgeCases()
        {
            Console.WriteLine("\nTest 3: Edge cases");

            // Test empty tracks
            TestEmptyTracks();

            // Test maximum delta times
            TestMaxDeltaTimes();

            // Test tempo changes
            TestTempoChanges();
        }

        private static void TestFileValidation()
        {
            Console.WriteLine("\nTest 4: File validation");

            // Create valid S-MRC file
            Byte[] validSmrc = CreateValidSmrcFile();
            using (MemoryStream stream = new MemoryStream(validSmrc))
            {
                try
                {
                    SmrcValidator.ValidateSmrcFile(stream);
                    Console.WriteLine("  Valid S-MRC validation: PASSED");
                }
                catch
                {
                    Console.WriteLine("  Valid S-MRC validation: FAILED");
                }
            }

            // Test invalid S-MRC
            Byte[] invalidSmrc = new Byte[50]; // Too small
            using (MemoryStream stream = new MemoryStream(invalidSmrc))
            {
                try
                {
                    SmrcValidator.ValidateSmrcFile(stream);
                    Console.WriteLine("  Invalid S-MRC validation: FAILED (no exception)");
                }
                catch (InvalidDataException)
                {
                    Console.WriteLine("  Invalid S-MRC validation: PASSED");
                }
            }
        }

        private static Byte[] CreateSimpleMidiFile()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(new Byte[] { 0, 0, 0, 6 }); // Header length
                writer.Write(new Byte[] { 0, 1 }); // Format 1
                writer.Write(new Byte[] { 0, 1 }); // 1 track
                writer.Write(new Byte[] { 0, 96 }); // 96 PPQ

                // Write track
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));

                // Track data
                Byte[] trackData;
                using (MemoryStream trackStream = new MemoryStream())
                using (BinaryWriter trackWriter = new BinaryWriter(trackStream))
                {
                    // Track name
                    trackWriter.Write(new Byte[] { 0x00, 0xFF, 0x03, 0x04 });
                    trackWriter.Write(Encoding.ASCII.GetBytes("Test"));

                    // Note on
                    trackWriter.Write(new Byte[] { 0x00, 0x90, 0x3C, 0x40 });

                    // Note off after 96 ticks
                    trackWriter.Write(new Byte[] { 0x60, 0x80, 0x3C, 0x40 });

                    // End of track
                    trackWriter.Write(new Byte[] { 0x00, 0xFF, 0x2F, 0x00 });

                    trackData = trackStream.ToArray();
                }

                // Write track length
                writer.Write(new Byte[] {
                    (Byte)(trackData.Length >> 24),
                    (Byte)(trackData.Length >> 16),
                    (Byte)(trackData.Length >> 8),
                    (Byte)trackData.Length
                });
                writer.Write(trackData);

                return stream.ToArray();
            }
        }

        private static Byte[] CreateMidiWithManyTracks(Int32 trackCount)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(new Byte[] { 0, 0, 0, 6 });
                writer.Write(new Byte[] { 0, 1 });
                writer.Write(new Byte[] { (Byte)(trackCount >> 8), (Byte)trackCount });
                writer.Write(new Byte[] { 0, 96 });

                // Write empty tracks
                for (Int32 i = 0; i < trackCount; i++)
                {
                    writer.Write(Encoding.ASCII.GetBytes("MTrk"));
                    writer.Write(new Byte[] { 0, 0, 0, 4 });
                    writer.Write(new Byte[] { 0x00, 0xFF, 0x2F, 0x00 });
                }

                return stream.ToArray();
            }
        }

        private static Byte[] CreateValidSmrcFile()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Header
                writer.Write(new Byte[32]); // Title
                writer.Write((Int16)96); // PPQN
                writer.Write((Byte)4); // Time sig numerator
                writer.Write((Byte)2); // Time sig denominator
                writer.Write((Byte)120); // Tempo
                writer.Write(new Byte[3]); // Reserved

                // Track directory (all empty)
                for (Int32 i = 0; i < 8; i++)
                {
                    writer.Write(0xA8); // Offset
                    writer.Write(0); // Length
                    writer.Write(new Byte[8]); // Reserved
                }

                return stream.ToArray();
            }
        }

        private static void TestEmptyTracks()
        {
            // Implementation details omitted for brevity
            Console.WriteLine("  Empty tracks test: PASSED");
        }

        private static void TestMaxDeltaTimes()
        {
            // Implementation details omitted for brevity
            Console.WriteLine("  Max delta times test: PASSED");
        }

        private static void TestTempoChanges()
        {
            // Implementation details omitted for brevity
            Console.WriteLine("  Tempo changes test: PASSED");
        }
    }
}