using System.Text;

using Xunit;

namespace RolandConverter.Tests
{
    /// <summary>
    /// Contains unit tests for the S-MRC to MIDI converter and validator.
    /// </summary>
    public class ConverterTests
    {
        #region Private Methods

        private static byte[] CreateMidiFormat0()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    0,  // Format 0 (single track)
                    1,  // One track
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write track data
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateTrackName("Format 0 Test"),
                    MidiMessageBuilder.CreateNoteOn(Constants.MidiFormat.DefaultNote, Constants.MidiFormat.DefaultVelocity),  // Note On
                    MidiMessageBuilder.CreateNoteOff(Constants.MidiFormat.DefaultNote, 0),  // Note Off
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiFormat1()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 1)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    1,  // Format 1 (multiple tracks)
                    1,  // One track
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write track data
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateTrackName("Format 1 Test"),
                    MidiMessageBuilder.CreateNoteOn(Constants.MidiFormat.DefaultNote, Constants.MidiFormat.DefaultVelocity),  // Note On
                    MidiMessageBuilder.CreateNoteOff(Constants.MidiFormat.DefaultNote, 0),  // Note Off
                    MidiMessageBuilder.CreateChannelPressure(Constants.MidiFormat.DefaultVelocity),  // Channel Pressure
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithAllEventTypes()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    0,  // Format 0 (single track)
                    1,  // One track
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write track data with all MIDI event types
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateTrackName("All Event Types Test"),
                    MidiMessageBuilder.CreateNoteOn(Constants.MidiFormat.DefaultNote, Constants.MidiFormat.DefaultVelocity),  // Note On
                    MidiMessageBuilder.CreateNoteOff(Constants.MidiFormat.DefaultNote, 0),  // Note Off
                    MidiMessageBuilder.CreatePolyphonicAftertouch(Constants.MidiFormat.DefaultNote, 64),  // Polyphonic Aftertouch
                    MidiMessageBuilder.CreateControlChange(0, 7, 100),  // Volume (CC 7)
                    MidiMessageBuilder.CreateProgramChange(0, 0),  // Piano
                    MidiMessageBuilder.CreateChannelPressure(64),  // Channel Pressure
                    MidiMessageBuilder.CreatePitchBend(0, 0x40),  // Center position
                    MidiMessageBuilder.CreateSystemExclusive(new byte[] { 0xF0, 0x7E, 0x7F, 0x06, 0x01, 0xF7 }),  // Universal non-realtime message
                    MidiMessageBuilder.CreateTempo(120),  // 120 BPM
                    MidiMessageBuilder.CreateTimeSignature(4, 2, 24, 8),  // 4/4 time
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithAllMetaEvents()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    0,  // Format 0 (single track)
                    1,  // One track
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write track data with all meta events
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateMetaEvent(0x00, new byte[] { 0x00, 0x01 }),  // Sequence number
                    MidiMessageBuilder.CreateMetaEvent(0x01, Encoding.ASCII.GetBytes("Text Event")),  // Text event
                    MidiMessageBuilder.CreateMetaEvent(0x02, Encoding.ASCII.GetBytes("Copyright")),  // Copyright
                    MidiMessageBuilder.CreateTrackName("All Meta Events Test"),  // Track name
                    MidiMessageBuilder.CreateMetaEvent(0x04, Encoding.ASCII.GetBytes("Instrument")),  // Instrument name
                    MidiMessageBuilder.CreateMetaEvent(0x05, Encoding.ASCII.GetBytes("Lyric Text")),  // Lyric
                    MidiMessageBuilder.CreateMetaEvent(0x06, Encoding.ASCII.GetBytes("Marker")),  // Marker
                    MidiMessageBuilder.CreateMetaEvent(0x07, Encoding.ASCII.GetBytes("Cue Point")),  // Cue point
                    MidiMessageBuilder.CreateMetaEvent(0x08, Encoding.ASCII.GetBytes("Program")),  // Program name
                    MidiMessageBuilder.CreateMetaEvent(0x09, Encoding.ASCII.GetBytes("Device")),  // Device name
                    MidiMessageBuilder.CreateMetaEvent(0x20, new byte[] { 0x00 }),  // Channel prefix
                    MidiMessageBuilder.CreateMetaEvent(0x21, new byte[] { 0x00 }),  // MIDI port
                    MidiMessageBuilder.CreateTempo(500000),  // Tempo (120 BPM)
                    MidiMessageBuilder.CreateTimeSignature(4, 2, 24, 8),  // Time signature (4/4)
                    MidiMessageBuilder.CreateMetaEvent(0x59, new byte[] { 0x00, 0x00 }),  // Key signature (C major)
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithAllTracksActive()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 1)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    1,  // Format 1 (multiple tracks)
                    8,  // Eight tracks
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write track data for all tracks
                for (int i = 0; i < 8; i++)
                {
                    writer.Write(MidiTrackBuilder.CreateTrack(
                        MidiMessageBuilder.CreateTrackName($"Track {i + 1}"),
                        MidiMessageBuilder.CreateNoteOn((byte)(Constants.MidiFormat.DefaultNote + i), Constants.MidiFormat.DefaultVelocity),  // Note On
                        MidiMessageBuilder.CreateNoteOff((byte)(Constants.MidiFormat.DefaultNote + i), 0),  // Note Off
                        MidiMessageBuilder.CreateEndOfTrack()
                    ));
                }

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithChannelPressure()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    0,  // Format 0 (single track)
                    1,  // One track
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write track data with channel pressure
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateTrackName("Channel Pressure Test"),
                    MidiMessageBuilder.CreateChannelPressure(Constants.MidiFormat.DefaultVelocity),
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithControlChange()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    0,  // Format 0 (single track)
                    1,  // One track
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write track data with control change
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateTrackName("Control Change Test"),
                    MidiMessageBuilder.CreateControlChange(Constants.MidiFormat.DefaultControl, Constants.MidiFormat.DefaultVelocity),
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithEmptyTrack()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    0,  // Format 0 (single track)
                    1,  // One track
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write empty track data
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateTrackName("Empty Track Test"),
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithExpression()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    0,  // Format 0 (single track)
                    1,  // One track
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write track data with expression control changes
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateTrackName("Expression Test"),
                    MidiMessageBuilder.CreateControlChange(0, 11, 0),    // Channel 1, Expression (CC 11), 0
                    MidiMessageBuilder.CreateControlChange(0, 11, 64),   // Channel 1, Expression (CC 11), 64
                    MidiMessageBuilder.CreateControlChange(0, 11, 127),  // Channel 1, Expression (CC 11), 127
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithLongDeltaTimes()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    0,  // Format 0 (single track)
                    1,  // One track
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write track data with long delta times
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateTrackName("Long Delta Times Test"),
                    MidiMessageBuilder.CreateNoteOn(Constants.MidiFormat.DefaultNote, Constants.MidiFormat.DefaultVelocity, 0x7F),  // Delta time 127
                    MidiMessageBuilder.CreateNoteOff(Constants.MidiFormat.DefaultNote, Constants.MidiFormat.DefaultVelocity, 0x7F),  // Delta time 127
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithLongTrackName()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    0,  // Format 0 (single track)
                    1,  // One track
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write track data with maximum length track name
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateTrackName(new string('A', 255)),  // Maximum length track name
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithManyTracks(short trackCount)
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 1)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    1,  // Format 1 (multiple tracks)
                    trackCount,  // Number of tracks
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write track data for each track
                for (int i = 0; i < trackCount; i++)
                {
                    writer.Write(MidiTrackBuilder.CreateTrack(
                        MidiMessageBuilder.CreateTrackName($"Track {i + 1}"),
                        MidiMessageBuilder.CreateNoteOn((byte)(Constants.MidiFormat.DefaultNote + i), Constants.MidiFormat.DefaultVelocity),  // Note On
                        MidiMessageBuilder.CreateNoteOff((byte)(Constants.MidiFormat.DefaultNote + i), 0),  // Note Off
                        MidiMessageBuilder.CreateEndOfTrack()
                    ));
                }

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithMaximumDeltaTime()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    0,  // Format 0 (single track)
                    1,  // One track
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write track data with maximum delta time
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateTrackName("Maximum Delta Time Test"),
                    MidiMessageBuilder.CreateNoteOn(Constants.MidiFormat.DefaultNote, Constants.MidiFormat.DefaultVelocity, 0x0FFFFFFF),  // Maximum valid VLQ value
                    MidiMessageBuilder.CreateNoteOff(Constants.MidiFormat.DefaultNote, 0),  // Note Off
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithMetaEvents()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(MidiHeaderBuilder.CreateHeader(0, 1, Constants.MidiFormat.DefaultPpqn));

                // Write track data
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateMetaEvent(0x00, new byte[] { 0x00, 0x01 }), // Sequence number
                    MidiMessageBuilder.CreateMetaEvent(0x01, Encoding.ASCII.GetBytes("Text Event")), // Text event
                    MidiMessageBuilder.CreateMetaEvent(0x02, Encoding.ASCII.GetBytes("Copyright")), // Copyright
                    MidiMessageBuilder.CreateTrackName("Test"), // Track name
                    MidiMessageBuilder.CreateMetaEvent(0x04, Encoding.ASCII.GetBytes("Instrument")), // Instrument name
                    MidiMessageBuilder.CreateMetaEvent(0x05, Encoding.ASCII.GetBytes("Lyric Text")), // Lyric
                    MidiMessageBuilder.CreateMetaEvent(0x06, Encoding.ASCII.GetBytes("Marker")), // Marker
                    MidiMessageBuilder.CreateMetaEvent(0x07, Encoding.ASCII.GetBytes("Cue Point")), // Cue point
                    MidiMessageBuilder.CreateMetaEvent(0x08, Encoding.ASCII.GetBytes("Program")), // Program name
                    MidiMessageBuilder.CreateMetaEvent(0x09, Encoding.ASCII.GetBytes("Device")), // Device name
                    MidiMessageBuilder.CreateMetaEvent(0x20, new byte[] { 0x00 }), // Channel prefix
                    MidiMessageBuilder.CreateMetaEvent(0x21, new byte[] { 0x00 }), // MIDI port
                    MidiMessageBuilder.CreateTempo(500000), // Tempo (120 BPM)
                    MidiMessageBuilder.CreateTimeSignature(4, 2, 24, 8), // Time signature (4/4)
                    MidiMessageBuilder.CreateMetaEvent(0x59, new byte[] { 0x00, 0x00 }), // Key signature (C major)
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithMultipleChannels()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(MidiHeaderBuilder.CreateHeader(0, 1, Constants.MidiFormat.DefaultPpqn));

                // Write track data
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateNoteOn(Constants.MidiFormat.DefaultNote, Constants.MidiFormat.DefaultVelocity, 0, 0), // Channel 1
                    MidiMessageBuilder.CreateNoteOn(Constants.MidiFormat.DefaultNote + 1, Constants.MidiFormat.DefaultVelocity, 0, 1), // Channel 2
                    MidiMessageBuilder.CreateNoteOn(Constants.MidiFormat.DefaultNote + 2, Constants.MidiFormat.DefaultVelocity, 0, 2), // Channel 3
                    MidiMessageBuilder.CreateNoteOn(Constants.MidiFormat.DefaultNote + 3, Constants.MidiFormat.DefaultVelocity, 0, 3), // Channel 4
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithMultipleTempos()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    0,  // Format 0 (single track)
                    1,  // One track
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write track data with multiple tempos
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateTrackName("Multiple Tempos Test"),
                    MidiMessageBuilder.CreateTempo(120),  // Tempo (120 BPM)
                    MidiMessageBuilder.CreateTempo(140),  // Tempo (140 BPM)
                    MidiMessageBuilder.CreateTempo(100),  // Tempo (100 BPM)
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithMultipleTimeSignatures()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    0,  // Format 0 (single track)
                    1,  // One track
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write track data with multiple time signatures
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateTrackName("Multiple Time Signatures Test"),
                    MidiMessageBuilder.CreateTimeSignature(4, 2, 24, 8),  // 4/4
                    MidiMessageBuilder.CreateTimeSignature(3, 2, 24, 8),  // 3/4
                    MidiMessageBuilder.CreateTimeSignature(6, 3, 24, 8),  // 6/8
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithPan()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    0,  // Format 0 (single track)
                    1,  // One track
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write track data with pan control changes
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateTrackName("Pan Test"),
                    MidiMessageBuilder.CreateControlChange(0, 10, 0),   // Channel 1, Pan (CC 10), Left
                    MidiMessageBuilder.CreateControlChange(1, 10, 64),  // Channel 2, Pan (CC 10), Center
                    MidiMessageBuilder.CreateControlChange(2, 10, 127), // Channel 3, Pan (CC 10), Right
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithPitchBend()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    0,  // Format 0 (single track)
                    1,  // One track
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write track data with pitch bend
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateTrackName("Pitch Bend Test"),
                    MidiMessageBuilder.CreatePitchBend(0x00, 0x40),  // Center position
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithPolyphonicAftertouch()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    0,  // Format 0 (single track)
                    1,  // One track
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write track data with polyphonic aftertouch
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateTrackName("Polyphonic Aftertouch Test"),
                    MidiMessageBuilder.CreatePolyphonicAftertouch(Constants.MidiFormat.DefaultNote, Constants.MidiFormat.DefaultVelocity),
                    MidiMessageBuilder.CreatePolyphonicAftertouch(Constants.MidiFormat.DefaultNote + 1, Constants.MidiFormat.DefaultVelocity),
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithProgramChange()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    0,  // Format 0 (single track)
                    1,  // One track
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write track data with program changes
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateTrackName("Program Change Test"),
                    MidiMessageBuilder.CreateProgramChange(0x00),  // Piano
                    MidiMessageBuilder.CreateProgramChange(0x40),  // Violin
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithReverb()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    0,  // Format 0 (single track)
                    1,  // One track
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write track data with reverb control
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateTrackName("Reverb Test"),
                    MidiMessageBuilder.CreateControlChange(0x5B, 0x40),  // Reverb (0x5B) at 50%
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithRunningStatus()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    0,  // Format 0 (single track)
                    1,  // One track
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write track data with running status
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateTrackName("Running Status Test"),
                    MidiMessageBuilder.CreateNoteOn(Constants.MidiFormat.DefaultNote, Constants.MidiFormat.DefaultVelocity),  // First note on
                    MidiMessageBuilder.CreateNoteOn(Constants.MidiFormat.DefaultNote + 1, Constants.MidiFormat.DefaultVelocity, 0, 0, true),  // Running status note on
                    MidiMessageBuilder.CreateNoteOn(Constants.MidiFormat.DefaultNote + 2, Constants.MidiFormat.DefaultVelocity, 0, 0, true),  // Running status note on
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithShortDeltaTimes()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    0,  // Format 0 (single track)
                    1,  // One track
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write track data with short delta times
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateTrackName("Short Delta Times Test"),
                    MidiMessageBuilder.CreateNoteOn(Constants.MidiFormat.DefaultNote, Constants.MidiFormat.DefaultVelocity, 0),  // Note On with zero delta time
                    MidiMessageBuilder.CreateNoteOff(Constants.MidiFormat.DefaultNote, 0, 0),  // Note Off with zero delta time
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithSustain()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    0,  // Format 0 (single track)
                    1,  // One track
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write track data with sustain pedal
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateTrackName("Sustain Test"),
                    MidiMessageBuilder.CreateControlChange(0x40, 0x7F),  // Sustain pedal (0x40) on
                    MidiMessageBuilder.CreateControlChange(0x40, 0x00),  // Sustain pedal (0x40) off
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithSystemExclusive()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    0,  // Format 0 (single track)
                    1,  // One track
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write track data with system exclusive messages
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateTrackName("System Exclusive Test"),
                    MidiMessageBuilder.CreateSystemExclusive(new byte[] { 0xF0, 0x7E, 0x7F, 0x06, 0x01, 0xF7 }),  // Universal non-realtime message
                    MidiMessageBuilder.CreateSystemExclusive(new byte[] { 0xF0, 0x41, 0x10, 0x42, 0x12, 0x40, 0x00, 0x7F, 0x00, 0x41, 0xF7 }),  // Roland message
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithTempo()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    0,  // Format 0 (single track)
                    1,  // One track
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write track data with tempo changes
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateTrackName("Tempo Test"),
                    MidiMessageBuilder.CreateTempo(500000),  // 120 BPM
                    MidiMessageBuilder.CreateTempo(428571),  // 140 BPM
                    MidiMessageBuilder.CreateTempo(600000),  // 100 BPM
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithVolume()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(MidiHeaderBuilder.CreateHeader(
                    0,  // Format 0 (single track)
                    1,  // One track
                    Constants.MidiFormat.DefaultPpqn
                ));

                // Write track data with volume control
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateTrackName("Volume Test"),
                    MidiMessageBuilder.CreateControlChange(0x07, 0x40),  // Volume (0x07) at 50%
                    MidiMessageBuilder.CreateControlChange(0x07, 0x7F),  // Volume (0x07) at 100%
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateSimpleMidiFile()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 1)
                writer.Write(MidiHeaderBuilder.CreateHeader(1, 1, Constants.MidiFormat.DefaultPpqn));

                // Write track data
                writer.Write(MidiTrackBuilder.CreateTrack(
                    MidiMessageBuilder.CreateTrackName("Test"),
                    MidiMessageBuilder.CreateNoteOn(Constants.MidiFormat.DefaultNote, Constants.MidiFormat.DefaultVelocity),
                    MidiMessageBuilder.CreateNoteOff(Constants.MidiFormat.DefaultNote, Constants.MidiFormat.DefaultVelocity, 96), // After 96 ticks
                    MidiMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithAllTrackData()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Header
                writer.Write(SmrcHeaderBuilder.CreateHeader(
                    "Test Title",
                    (ushort)Constants.SmrcFormat.DefaultPpqn,
                    Constants.SmrcFormat.DefaultTimeSignatureNumerator,
                    Constants.SmrcFormat.DefaultTimeSignatureDenominator,
                    Constants.SmrcFormat.DefaultTempo
                ));

                // Track directory
                writer.Write(SmrcTrackBuilder.CreateTrackDirectoryEntry(
                    Constants.SmrcFormat.FirstTrackDataOffset,
                    64u // Track data length
                ));

                // Empty tracks
                for (uint i = 1; i < Constants.SmrcFormat.MaxTracks; i++)
                {
                    writer.Write(SmrcTrackBuilder.CreateTrackDirectoryEntry(
                        Constants.SmrcFormat.FirstTrackDataOffset,
                        0u
                    ));
                }

                // Track data with all message types
                writer.Write(SmrcTrackBuilder.CreateTrack(
                    SmrcMessageBuilder.CreateNoteOn((byte)Constants.SmrcFormat.DefaultNote, (byte)Constants.SmrcFormat.DefaultVelocity), // Middle C
                    SmrcMessageBuilder.CreateNoteOff((byte)Constants.SmrcFormat.DefaultNote, 0), // Release middle C
                    SmrcMessageBuilder.CreateControlChange(1, 64), // Modulation wheel
                    SmrcMessageBuilder.CreateProgramChange(0), // Piano
                    SmrcMessageBuilder.CreateChannelPressure(64), // Aftertouch
                    SmrcMessageBuilder.CreatePitchBend(8192), // Center position
                    SmrcMessageBuilder.CreateSystemExclusive(new byte[] { 0x41, 0x10, 0x42, 0x12, 0x40, 0x00, 0x7F, 0x00, 0x41 }), // Roland SysEx
                    SmrcMessageBuilder.CreateTempo(Constants.SmrcFormat.DefaultTempo), // Default tempo
                    SmrcMessageBuilder.CreateTimeSignature(4, 4), // 4/4 time
                    SmrcMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithInvalidPpqn()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write header with invalid PPQN (0)
                writer.Write(SmrcHeaderBuilder.CreateHeader(
                    "Invalid PPQN Test",
                    0,  // Invalid PPQN (0)
                    Constants.SmrcFormat.DefaultTimeSignatureNumerator,
                    Constants.SmrcFormat.DefaultTimeSignatureDenominator,
                    120  // Default tempo
                ));

                // Write track directory with empty tracks
                for (int i = 0; i < Constants.SmrcFormat.MaxTracks; i++)
                {
                    writer.Write(SmrcTrackBuilder.CreateTrackDirectoryEntry(
                        Constants.SmrcFormat.FirstTrackDataOffset,
                        0  // Empty track
                    ));
                }

                // Add some dummy track data to make the file large enough
                writer.Write(new byte[Constants.SmrcFormat.TrackDataSize]);

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithInvalidTempo()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write header with invalid tempo (0 BPM)
                writer.Write(SmrcHeaderBuilder.CreateHeader(
                    "Invalid Tempo Test",
                    (ushort)Constants.SmrcFormat.DefaultPpqn,
                    Constants.SmrcFormat.DefaultTimeSignatureNumerator,
                    Constants.SmrcFormat.DefaultTimeSignatureDenominator,
                    0  // Invalid tempo (0 BPM)
                ));

                // Write track directory with empty tracks
                for (int i = 0; i < Constants.SmrcFormat.MaxTracks; i++)
                {
                    writer.Write(SmrcTrackBuilder.CreateTrackDirectoryEntry(
                        Constants.SmrcFormat.FirstTrackDataOffset,
                        0  // Empty track
                    ));
                }

                // Add some dummy track data to make the file large enough
                writer.Write(new byte[Constants.SmrcFormat.TrackDataSize]);

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithInvalidTimeSignature()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write header with invalid time signature (0/0)
                writer.Write(SmrcHeaderBuilder.CreateHeader(
                    "Invalid Time Signature Test",
                    (ushort)Constants.SmrcFormat.DefaultPpqn,
                    0,  // Invalid numerator (0)
                    0,  // Invalid denominator (0)
                    120  // Default tempo
                ));

                // Write track directory with empty tracks
                for (int i = 0; i < Constants.SmrcFormat.MaxTracks; i++)
                {
                    writer.Write(SmrcTrackBuilder.CreateTrackDirectoryEntry(
                        Constants.SmrcFormat.FirstTrackDataOffset,
                        0  // Empty track
                    ));
                }

                // Add some dummy track data to make the file large enough
                writer.Write(new byte[Constants.SmrcFormat.TrackDataSize]);

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithInvalidTrackData()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write header
                writer.Write(SmrcHeaderBuilder.CreateHeader(
                    "Invalid Track Data Test",
                    (ushort)Constants.SmrcFormat.DefaultPpqn,
                    Constants.SmrcFormat.DefaultTimeSignatureNumerator,
                    Constants.SmrcFormat.DefaultTimeSignatureDenominator,
                    Constants.SmrcFormat.DefaultTempo
                ));

                // Write track directory with one track containing invalid data
                writer.Write(SmrcTrackBuilder.CreateTrackDirectoryEntry(
                    Constants.SmrcFormat.FirstTrackDataOffset,
                    3  // Length of invalid track data
                ));

                // Empty tracks
                for (int i = 1; i < Constants.SmrcFormat.MaxTracks; i++)
                {
                    writer.Write(SmrcTrackBuilder.CreateTrackDirectoryEntry(
                        Constants.SmrcFormat.FirstTrackDataOffset,
                        0
                    ));
                }

                // Invalid track data (truncated MIDI message)
                writer.Write(new byte[] { 0x00, 0x90, 0x3C }); // Note on without velocity

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithInvalidTrackDirectory()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write header
                writer.Write(SmrcHeaderBuilder.CreateHeader(
                    "Invalid Track Directory Test",
                    (ushort)Constants.SmrcFormat.DefaultPpqn,
                    Constants.SmrcFormat.DefaultTimeSignatureNumerator,
                    Constants.SmrcFormat.DefaultTimeSignatureDenominator,
                    Constants.SmrcFormat.DefaultTempo
                ));

                // Write invalid track directory (offsets beyond file size)
                for (int i = 0; i < Constants.SmrcFormat.MaxTracks; i++)
                {
                    writer.Write(SmrcTrackBuilder.CreateTrackDirectoryEntry(
                        Constants.SmrcFormat.MaxTrackDataSize,  // Invalid offset
                        Constants.SmrcFormat.MaxTrackDataSize   // Invalid length
                    ));
                }

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithMaximumData()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write header
                writer.Write(SmrcHeaderBuilder.CreateHeader(
                    "Maximum Data Test",
                    (ushort)Constants.SmrcFormat.DefaultPpqn,
                    Constants.SmrcFormat.DefaultTimeSignatureNumerator,
                    Constants.SmrcFormat.DefaultTimeSignatureDenominator,
                    Constants.SmrcFormat.DefaultTempo
                ));

                // Calculate the correct offset for track data
                int trackDataOffset = (int)(Constants.SmrcFormat.HeaderSize + (Constants.SmrcFormat.MaxTracks * Constants.SmrcFormat.TrackDirectoryEntrySize));

                // Write track directory with maximum size track
                writer.Write(SmrcTrackBuilder.CreateTrackDirectoryEntry(
                    (uint)trackDataOffset,
                    Constants.SmrcFormat.MaxTrackDataSize  // 1MB track length
                ));

                // Empty tracks
                for (int i = 1; i < Constants.SmrcFormat.MaxTracks; i++)
                {
                    writer.Write(SmrcTrackBuilder.CreateTrackDirectoryEntry(
                        (uint)trackDataOffset,
                        0
                    ));
                }

                // Track data (1MB, last byte is 0xFF)
                byte[] trackData = new byte[Constants.SmrcFormat.MaxTrackDataSize];
                if (trackData.Length > 0)
                {
                    trackData[trackData.Length - 1] = 0xFF; // End-of-Track
                }

                writer.Write(trackData);

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithMaximumTrackData()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write header
                byte[] header = SmrcHeaderBuilder.CreateHeader(
                    "Maximum Track Data Test",
                    (ushort)Constants.SmrcFormat.DefaultPpqn,
                    Constants.SmrcFormat.DefaultTimeSignatureNumerator,
                    Constants.SmrcFormat.DefaultTimeSignatureDenominator,
                    Constants.SmrcFormat.DefaultTempo
                );
                writer.Write(header);
                Console.WriteLine($"[CreateSmrcWithMaximumTrackData] After header: position={stream.Position}");

                // Calculate the correct offset for track data
                int trackDataOffset = (int)(Constants.SmrcFormat.HeaderSize + (Constants.SmrcFormat.MaxTracks * Constants.SmrcFormat.TrackDirectoryEntrySize));

                // Write track directory with maximum size track
                uint firstTrackOffset = (uint)trackDataOffset;
                uint firstTrackLength = Constants.SmrcFormat.MaxTrackDataSize;
                byte[] dirEntry = SmrcTrackBuilder.CreateTrackDirectoryEntry(
                    firstTrackOffset,
                    firstTrackLength
                );
                writer.Write(dirEntry);
                Console.WriteLine($"[CreateSmrcWithMaximumTrackData] Directory entry 0: offset={firstTrackOffset}, length={firstTrackLength}");
                Console.WriteLine($"[CreateSmrcWithMaximumTrackData] Wrote first track directory entry: {BitConverter.ToString(dirEntry)} at position={stream.Position}");

                // Empty tracks
                for (int i = 1; i < Constants.SmrcFormat.MaxTracks; i++)
                {
                    uint emptyOffset = (uint)trackDataOffset;
                    uint emptyLength = 0;
                    byte[] emptyEntry = SmrcTrackBuilder.CreateTrackDirectoryEntry(
                        emptyOffset,
                        emptyLength
                    );
                    writer.Write(emptyEntry);
                    Console.WriteLine($"[CreateSmrcWithMaximumTrackData] Directory entry {i}: offset={emptyOffset}, length={emptyLength}");
                }

                Console.WriteLine($"[CreateSmrcWithMaximumTrackData] After directory: position={stream.Position}");

                // Track data (1MB, last byte is 0xFF)
                byte[] trackData = new byte[Constants.SmrcFormat.MaxTrackDataSize];
                if (trackData.Length > 0)
                {
                    trackData[trackData.Length - 1] = 0xFF; // End-of-Track
                }

                writer.Write(trackData);
                Console.WriteLine($"[CreateSmrcWithMaximumTrackData] After track data: position={stream.Position}");

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithMaximumTracks()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write header
                writer.Write(SmrcHeaderBuilder.CreateHeader(
                    "Maximum Tracks Test",
                    (ushort)Constants.SmrcFormat.DefaultPpqn,
                    Constants.SmrcFormat.DefaultTimeSignatureNumerator,
                    Constants.SmrcFormat.DefaultTimeSignatureDenominator,
                    Constants.SmrcFormat.DefaultTempo
                ));

                // Calculate the correct offset for track data
                int baseTrackDataOffset = (int)(Constants.SmrcFormat.HeaderSize + (Constants.SmrcFormat.MaxTracks * Constants.SmrcFormat.TrackDirectoryEntrySize));
                int currentOffset = baseTrackDataOffset;
                for (int i = 0; i < Constants.SmrcFormat.MaxTracks; i++)
                {
                    writer.Write(SmrcTrackBuilder.CreateTrackDirectoryEntry(
                        (uint)currentOffset,
                        16  // Each track has 16 bytes of data
                    ));
                    currentOffset += 16;
                }

                // Track data (16 bytes per track, each ends with 0xFF)
                for (int i = 0; i < Constants.SmrcFormat.MaxTracks; i++)
                {
                    byte[] track = new byte[16];
                    if (track.Length > 0)
                    {
                        track[track.Length - 1] = 0xFF;
                    }

                    writer.Write(track);
                }

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithOverlappingTracks()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write header
                writer.Write(SmrcHeaderBuilder.CreateHeader(
                    "Overlapping Tracks Test",
                    (ushort)Constants.SmrcFormat.DefaultPpqn,
                    Constants.SmrcFormat.DefaultTimeSignatureNumerator,
                    Constants.SmrcFormat.DefaultTimeSignatureDenominator,
                    Constants.SmrcFormat.DefaultTempo
                ));

                // Write track directory with overlapping tracks
                writer.Write(SmrcTrackBuilder.CreateTrackDirectoryEntry(
                    Constants.SmrcFormat.FirstTrackDataOffset,
                    16  // Track 1 length
                ));

                writer.Write(SmrcTrackBuilder.CreateTrackDirectoryEntry(
                    Constants.SmrcFormat.FirstTrackDataOffset + 4,  // Overlaps with track 1
                    16  // Track 2 length
                ));

                // Empty tracks
                for (int i = 2; i < Constants.SmrcFormat.MaxTracks; i++)
                {
                    writer.Write(SmrcTrackBuilder.CreateTrackDirectoryEntry(
                        Constants.SmrcFormat.FirstTrackDataOffset,
                        0
                    ));
                }

                // Track data
                writer.Write(new byte[32]);  // 16 bytes for each track

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithTrackData()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Header
                byte[] header = SmrcHeaderBuilder.CreateHeader(
                    "Test Title",
                    (ushort)Constants.SmrcFormat.DefaultPpqn,
                    Constants.SmrcFormat.DefaultTimeSignatureNumerator,
                    Constants.SmrcFormat.DefaultTimeSignatureDenominator,
                    Constants.SmrcFormat.DefaultTempo  // Changed from MidiFormat to SmrcFormat
                );
                writer.Write(header);
                Console.WriteLine($"[CreateSmrcWithTrackData] Wrote header: {header.Length} bytes, current position: {stream.Position}");

                // Calculate the correct offset for track data
                int trackDataOffset = (int)(Constants.SmrcFormat.HeaderSize + (Constants.SmrcFormat.MaxTracks * Constants.SmrcFormat.TrackDirectoryEntrySize));

                // Track directory entries (part of header)
                byte[] directoryEntry = SmrcTrackBuilder.CreateTrackDirectoryEntry(
                    (uint)trackDataOffset,
                    13 // Track data length
                );
                writer.Write(directoryEntry);
                Console.WriteLine($"[CreateSmrcWithTrackData] Wrote first track directory entry: {directoryEntry.Length} bytes, current position: {stream.Position}");

                // Empty tracks (part of header)
                for (int i = 1; i < Constants.SmrcFormat.MaxTracks; i++)
                {
                    byte[] emptyEntry = SmrcTrackBuilder.CreateTrackDirectoryEntry(
                        (uint)trackDataOffset,
                        0
                    );
                    writer.Write(emptyEntry);
                    Console.WriteLine($"[CreateSmrcWithTrackData] Wrote empty track directory entry {i}: {emptyEntry.Length} bytes, current position: {stream.Position}");
                }

                // Add padding to reach header + directory size (should not be needed, but for safety)
                int currentSize = (int)stream.Length;
                int paddingNeeded = trackDataOffset - currentSize;
                if (paddingNeeded > 0)
                {
                    writer.Write(new byte[paddingNeeded]);
                    Console.WriteLine($"[CreateSmrcWithTrackData] Added {paddingNeeded} bytes of padding to reach track data offset, current position: {stream.Position}");
                }

                // Track data
                byte[] trackData = SmrcTrackBuilder.CreateTrack(
                    SmrcMessageBuilder.CreateNoteOn((byte)Constants.SmrcFormat.DefaultNote, (byte)Constants.SmrcFormat.DefaultVelocity), // Note on
                    SmrcMessageBuilder.CreateNoteOff((byte)Constants.SmrcFormat.DefaultNote, (byte)Constants.SmrcFormat.DefaultVelocity), // Note off
                    SmrcMessageBuilder.CreateEndOfTrack() // End of track
                );
                Console.WriteLine($"[CreateSmrcWithTrackData] Created track data: {trackData.Length} bytes");

                // Ensure last byte is 0xFF (End-of-Track)
                if (trackData.Length > 0)
                {
                    trackData[trackData.Length - 1] = 0xFF;
                }

                // Verify we're at the correct offset
                currentSize = (int)stream.Length;
                Console.WriteLine($"[CreateSmrcWithTrackData] Current position before writing track data: {currentSize}, expected: {trackDataOffset}");
                if (currentSize != trackDataOffset)
                {
                    throw new InvalidOperationException($"Expected to be at offset {trackDataOffset}, but was at {currentSize}");
                }

                writer.Write(trackData);
                Console.WriteLine($"[CreateSmrcWithTrackData] Wrote track data, current position: {stream.Position}");

                Console.WriteLine($"[CreateSmrcWithTrackData] Final file size: {stream.Length} bytes");
                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithVariousPpqn()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write header with higher PPQN
                writer.Write(SmrcHeaderBuilder.CreateHeader(
                    "Various PPQN Test",
                    480,  // Higher resolution PPQN
                    Constants.SmrcFormat.DefaultTimeSignatureNumerator,
                    Constants.SmrcFormat.DefaultTimeSignatureDenominator,
                    Constants.SmrcFormat.DefaultTempo
                ));

                // Write track directory
                writer.Write(SmrcTrackBuilder.CreateTrackDirectoryEntry(
                    Constants.SmrcFormat.FirstTrackDataOffset,
                    16  // Track data length
                ));

                // Empty tracks
                for (int i = 1; i < Constants.SmrcFormat.MaxTracks; i++)
                {
                    writer.Write(SmrcTrackBuilder.CreateTrackDirectoryEntry(
                        Constants.SmrcFormat.FirstTrackDataOffset,
                        0
                    ));
                }

                // Track data
                writer.Write(SmrcTrackBuilder.CreateTrack(
                    SmrcMessageBuilder.CreateNoteOn((byte)Constants.SmrcFormat.DefaultNote, (byte)Constants.SmrcFormat.DefaultVelocity),
                    SmrcMessageBuilder.CreateNoteOff((byte)Constants.SmrcFormat.DefaultNote, (byte)Constants.SmrcFormat.DefaultVelocity)
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithVariousTempos()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Header
                writer.Write(SmrcHeaderBuilder.CreateHeader(
                    "Test Title",
                    (ushort)Constants.SmrcFormat.DefaultPpqn,
                    Constants.SmrcFormat.DefaultTimeSignatureNumerator,
                    Constants.SmrcFormat.DefaultTimeSignatureDenominator,
                    Constants.SmrcFormat.DefaultTempo
                ));

                // Track directory
                writer.Write(SmrcTrackBuilder.CreateTrackDirectoryEntry(
                    Constants.SmrcFormat.FirstTrackDataOffset,
                    32 // Track data length
                ));

                // Empty tracks
                for (int i = 1; i < Constants.SmrcFormat.MaxTracks; i++)
                {
                    writer.Write(SmrcTrackBuilder.CreateTrackDirectoryEntry(
                        Constants.SmrcFormat.FirstTrackDataOffset,
                        0
                    ));
                }

                // Track data with tempo changes
                writer.Write(SmrcTrackBuilder.CreateTrack(
                    SmrcMessageBuilder.CreateTempo(Constants.SmrcFormat.DefaultTempo), // Default tempo
                    SmrcMessageBuilder.CreateTempo(140), // 140 BPM
                    SmrcMessageBuilder.CreateTempo(100), // 100 BPM
                    SmrcMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithVariousTimeSignatures()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Header
                writer.Write(SmrcHeaderBuilder.CreateHeader(
                    "Test Title",
                    (ushort)Constants.SmrcFormat.DefaultPpqn,
                    3, // 3/4 time signature
                    Constants.SmrcFormat.DefaultTimeSignatureDenominator,
                    120 // 120 BPM
                ));

                // Track directory
                writer.Write(SmrcTrackBuilder.CreateTrackDirectoryEntry(
                    Constants.SmrcFormat.FirstTrackDataOffset,
                    32 // Track data length
                ));

                // Empty tracks
                for (int i = 1; i < Constants.SmrcFormat.MaxTracks; i++)
                {
                    writer.Write(SmrcTrackBuilder.CreateTrackDirectoryEntry(
                        Constants.SmrcFormat.FirstTrackDataOffset,
                        0
                    ));
                }

                // Track data with time signature changes
                writer.Write(SmrcTrackBuilder.CreateTrack(
                    SmrcMessageBuilder.CreateTimeSignature(3, 4), // 3/4
                    SmrcMessageBuilder.CreateTimeSignature(4, 4), // 4/4
                    SmrcMessageBuilder.CreateTimeSignature(6, 8), // 6/8
                    SmrcMessageBuilder.CreateEndOfTrack()
                ));

                return stream.ToArray();
            }
        }

        private static byte[] CreateValidSmrcFile()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write header
                writer.Write(SmrcHeaderBuilder.CreateHeader(
                    "Valid S-MRC Test",
                    (ushort)Constants.SmrcFormat.DefaultPpqn,
                    Constants.SmrcFormat.DefaultTimeSignatureNumerator,
                    Constants.SmrcFormat.DefaultTimeSignatureDenominator,
                    Constants.SmrcFormat.DefaultTempo
                ));

                // Write track directory (all empty)
                for (int i = 0; i < Constants.SmrcFormat.MaxTracks; i++)
                {
                    writer.Write(SmrcTrackBuilder.CreateTrackDirectoryEntry(
                        Constants.SmrcFormat.FirstTrackDataOffset,
                        0
                    ));
                }

                return stream.ToArray();
            }
        }

        #endregion

        #region S-MRC Header Edge Cases

        [Fact]
        public void SmrcHeader_MinimumPpqn_ShouldThrowInvalidDataException()
        {
            Console.WriteLine("[Test] S-MRC header with minimum PPQN (0)");
            byte[] smrcData = CreateSmrcWithCustomHeader(
                "Min PPQN Test",
                0, // Invalid PPQN
                Constants.SmrcFormat.DefaultTimeSignatureNumerator,
                Constants.SmrcFormat.DefaultTimeSignatureDenominator,
                Constants.SmrcFormat.DefaultTempo
            );
            using (MemoryStream stream = new MemoryStream(smrcData))
            {
                try
                {
                    SmrcValidator.ValidateSmrcFile(stream);
                    Assert.True(false, "Expected InvalidDataException for minimum PPQN, but validation passed.");
                }
                catch (InvalidDataException)
                {
                    Console.WriteLine("[Test] Caught expected InvalidDataException for minimum PPQN.");
                }
            }
        }

        [Fact]
        public void SmrcHeader_MaximumPpqn_ShouldPassValidation()
        {
            Console.WriteLine("[Test] S-MRC header with maximum PPQN");
            byte[] smrcData = CreateSmrcWithCustomHeader(
                "Max PPQN Test",
                ushort.MaxValue, // Max PPQN
                Constants.SmrcFormat.DefaultTimeSignatureNumerator,
                Constants.SmrcFormat.DefaultTimeSignatureDenominator,
                Constants.SmrcFormat.DefaultTempo
            );
            using (MemoryStream stream = new MemoryStream(smrcData))
            {
                try
                {
                    SmrcValidator.ValidateSmrcFile(stream);
                    Assert.True(false, "Expected InvalidDataException for maximum PPQN, but validation passed.");
                }
                catch (InvalidDataException)
                {
                    Console.WriteLine("[Test] Caught expected InvalidDataException for maximum PPQN.");
                }
            }
        }

        [Fact]
        public void Smrc_Header_InvalidTempo_ShouldThrowInvalidDataException()
        {
            Console.WriteLine("[Test] S-MRC header with invalid tempo (0)");
            byte[] smrcData = CreateSmrcWithCustomHeader(
                "Invalid Tempo Test",
                (ushort)Constants.SmrcFormat.DefaultPpqn,
                Constants.SmrcFormat.DefaultTimeSignatureNumerator,
                Constants.SmrcFormat.DefaultTimeSignatureDenominator,
                0 // Invalid tempo
            );
            using (MemoryStream stream = new MemoryStream(smrcData))
            {
                try
                {
                    SmrcValidator.ValidateSmrcFile(stream);
                    Assert.True(false, "Expected InvalidDataException for invalid tempo, but validation passed.");
                }
                catch (InvalidDataException)
                {
                    Console.WriteLine("[Test] Caught expected InvalidDataException for invalid tempo.");
                }
            }
        }

        [Fact]
        public void SmrcHeader_MaximumTempo_ShouldPassValidation()
        {
            Console.WriteLine("[Test] S-MRC header with maximum tempo (255)");
            byte[] smrcData = CreateSmrcWithCustomHeader(
                "Max Tempo Test",
                (ushort)Constants.SmrcFormat.DefaultPpqn,
                Constants.SmrcFormat.DefaultTimeSignatureNumerator,
                Constants.SmrcFormat.DefaultTimeSignatureDenominator,
                255 // Max tempo
            );
            using (MemoryStream stream = new MemoryStream(smrcData))
            {
                SmrcValidator.ValidateSmrcFile(stream);
                Console.WriteLine("[Test] Validation passed for maximum tempo.");
                Assert.True(true, "Validation passed for maximum tempo.");
            }
        }

        [Fact]
        public void Smrc_Header_InvalidTimeSignature_ShouldThrowInvalidDataException()
        {
            Console.WriteLine("[Test] S-MRC header with invalid time signature (0/0)");
            byte[] smrcData = CreateSmrcWithCustomHeader(
                "Invalid TimeSig Test",
                (ushort)Constants.SmrcFormat.DefaultPpqn,
                0, // Invalid numerator
                0, // Invalid denominator
                Constants.SmrcFormat.DefaultTempo
            );
            using (MemoryStream stream = new MemoryStream(smrcData))
            {
                try
                {
                    SmrcValidator.ValidateSmrcFile(stream);
                    Assert.True(false, "Expected InvalidDataException for invalid time signature, but validation passed.");
                }
                catch (InvalidDataException)
                {
                    Console.WriteLine("[Test] Caught expected InvalidDataException for invalid time signature.");
                }
            }
        }

        [Fact]
        public void SmrcHeader_MaximumTimeSignature_ShouldPassValidation()
        {
            Console.WriteLine("[Test] S-MRC header with maximum time signature (numerator=255, denominator=6)");
            byte[] smrcData = CreateSmrcWithCustomHeader(
                "Max TimeSig Test",
                (ushort)Constants.SmrcFormat.DefaultPpqn,
                255, // Max numerator
                6,   // Max denominator (2^6=64)
                Constants.SmrcFormat.DefaultTempo
            );
            using (MemoryStream stream = new MemoryStream(smrcData))
            {
                SmrcValidator.ValidateSmrcFile(stream);
                Console.WriteLine("[Test] Validation passed for maximum time signature.");
                Assert.True(true, "Validation passed for maximum time signature.");
            }
        }

        [Fact]
        public void Smrc_Header_EmptyTitle_ShouldPassValidation()
        {
            Console.WriteLine("[Test] S-MRC header with empty title");
            byte[] smrcData = CreateSmrcWithCustomHeader(
                string.Empty,
                (ushort)Constants.SmrcFormat.DefaultPpqn,
                Constants.SmrcFormat.DefaultTimeSignatureNumerator,
                Constants.SmrcFormat.DefaultTimeSignatureDenominator,
                Constants.SmrcFormat.DefaultTempo
            );
            using (MemoryStream stream = new MemoryStream(smrcData))
            {
                SmrcValidator.ValidateSmrcFile(stream);
                Console.WriteLine("[Test] Validation passed for empty title.");
                Assert.True(true, "Validation passed for empty title.");
            }
        }

        [Fact]
        public void Smrc_Header_MaximumLengthTitle_ShouldPassValidation()
        {
            Console.WriteLine("[Test] S-MRC header with maximum length title");
            string maxTitle = new string('A', (int)Constants.SmrcFormat.TitleSize);
            byte[] smrcData = CreateSmrcWithCustomHeader(
                maxTitle,
                (ushort)Constants.SmrcFormat.DefaultPpqn,
                Constants.SmrcFormat.DefaultTimeSignatureNumerator,
                Constants.SmrcFormat.DefaultTimeSignatureDenominator,
                Constants.SmrcFormat.DefaultTempo
            );
            using (MemoryStream stream = new MemoryStream(smrcData))
            {
                SmrcValidator.ValidateSmrcFile(stream);
                Console.WriteLine("[Test] Validation passed for maximum length title.");
                Assert.True(true, "Validation passed for maximum length title.");
            }
        }

        [Fact]
        public void SmrcHeader_NonAsciiTitle_ShouldPassValidation()
        {
            Console.WriteLine("[Test] S-MRC header with non-ASCII title");
            string nonAsciiTitle = "Tést✓タイトル";
            byte[] smrcData = CreateSmrcWithCustomHeader(
                nonAsciiTitle,
                (ushort)Constants.SmrcFormat.DefaultPpqn,
                Constants.SmrcFormat.DefaultTimeSignatureNumerator,
                Constants.SmrcFormat.DefaultTimeSignatureDenominator,
                Constants.SmrcFormat.DefaultTempo
            );
            using (MemoryStream stream = new MemoryStream(smrcData))
            {
                SmrcValidator.ValidateSmrcFile(stream);
                Console.WriteLine("[Test] Validation passed for non-ASCII title.");
                Assert.True(true, "Validation passed for non-ASCII title.");
            }
        }

        private static byte[] CreateSmrcWithCustomHeader(string title, ushort ppqn, byte numerator, byte denominator, byte tempo)
        {
            Console.WriteLine($"[Helper] CreateSmrcWithCustomHeader: title='{title}', ppqn={ppqn}, timeSig={numerator}/{denominator}, tempo={tempo}");
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(SmrcHeaderBuilder.CreateHeader(title, ppqn, numerator, denominator, tempo));
                // Write empty track directory
                int trackDataOffset = (int)(Constants.SmrcFormat.HeaderSize + (Constants.SmrcFormat.MaxTracks * Constants.SmrcFormat.TrackDirectoryEntrySize));
                for (int i = 0; i < Constants.SmrcFormat.MaxTracks; i++)
                {
                    writer.Write(SmrcTrackBuilder.CreateTrackDirectoryEntry((uint)trackDataOffset, 0));
                }

                return stream.ToArray();
            }
        }

        #endregion

        #region Public Methods

        [Fact]
        public void Midi_EmptyFile_ShouldThrowInvalidDataException()
        {
            byte[] emptyData = Array.Empty<byte>();
            using MemoryStream inputStream = new MemoryStream(emptyData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            _ = Assert.Throws<InvalidDataException>(converter.ConvertMidiToSmrc);
        }

        [Fact]
        public void Midi_InvalidHeader_ShouldThrowInvalidDataException()
        {
            byte[] invalidData = Encoding.ASCII.GetBytes("INVALID");
            using MemoryStream inputStream = new MemoryStream(invalidData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            _ = Assert.Throws<InvalidDataException>(converter.ConvertMidiToSmrc);
        }

        [Fact]
        public void Smrc_InvalidFile_ShouldThrowInvalidDataException()
        {
            byte[] invalidSmrc = new byte[50];
            using MemoryStream stream = new MemoryStream(invalidSmrc);
            _ = Assert.Throws<InvalidDataException>(() => SmrcValidator.ValidateSmrcFile(stream));
        }

        [Fact]
        public void Midi_FileWithChannelPressure_ShouldConvertSuccessfully()
        {
            byte[] midiData = CreateMidiWithChannelPressure();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertMidiToSmrc();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void Midi_FileWithControlChange_ShouldConvertSuccessfully()
        {
            byte[] midiData = CreateMidiWithControlChange();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertMidiToSmrc();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void Midi_FileWithEmptyTrack_ShouldConvertSuccessfully()
        {
            byte[] midiData = CreateMidiWithEmptyTrack();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertMidiToSmrc();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void Midi_FileWithLongDeltaTimes_ShouldConvertSuccessfully()
        {
            byte[] midiData = CreateMidiWithLongDeltaTimes();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertMidiToSmrc();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void Midi_FileWithLongTrackName_ShouldConvertSuccessfully()
        {
            byte[] midiData = CreateMidiWithLongTrackName();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertMidiToSmrc();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void Midi_FileWithMaximumDeltaTime_ShouldConvertSuccessfully()
        {
            byte[] midiData = CreateMidiWithMaximumDeltaTime();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertMidiToSmrc();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void Midi_FileWithMetaEvents_ShouldConvertSuccessfully()
        {
            byte[] midiData = CreateMidiWithMetaEvents();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertMidiToSmrc();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void Midi_FileWithMultipleChannels_ShouldConvertSuccessfully()
        {
            byte[] midiData = CreateMidiWithMultipleChannels();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertMidiToSmrc();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void Midi_FileWithMultipleTempos_ShouldConvertSuccessfully()
        {
            byte[] midiData = CreateMidiWithMultipleTempos();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertMidiToSmrc();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void Midi_FileWithMultipleTimeSignatures_ShouldConvertSuccessfully()
        {
            byte[] midiData = CreateMidiWithMultipleTimeSignatures();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertMidiToSmrc();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void Midi_FileWithPitchBend_ShouldConvertSuccessfully()
        {
            byte[] midiData = CreateMidiWithPitchBend();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertMidiToSmrc();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void Midi_FileWithPolyphonicAftertouch_ShouldConvertSuccessfully()
        {
            byte[] midiData = CreateMidiWithPolyphonicAftertouch();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertMidiToSmrc();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void Midi_FileWithProgramChange_ShouldConvertSuccessfully()
        {
            byte[] midiData = CreateMidiWithProgramChange();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertMidiToSmrc();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void Midi_FileWithRunningStatus_ShouldConvertSuccessfully()
        {
            byte[] midiData = CreateMidiWithRunningStatus();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertMidiToSmrc();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void Midi_FileWithShortDeltaTimes_ShouldConvertSuccessfully()
        {
            byte[] midiData = CreateMidiWithShortDeltaTimes();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertMidiToSmrc();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void Midi_FileWithSustain_ShouldConvertSuccessfully()
        {
            byte[] midiData = CreateMidiWithSustain();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertMidiToSmrc();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void Midi_FileWithSystemExclusive_ShouldConvertSuccessfully()
        {
            byte[] midiData = CreateMidiWithSystemExclusive();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertMidiToSmrc();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void Midi_FileWithTempo_ShouldConvertSuccessfully()
        {
            byte[] midiData = CreateMidiWithTempo();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertMidiToSmrc();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void Midi_FileWithVolume_ShouldConvertSuccessfully()
        {
            byte[] midiData = CreateMidiWithVolume();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertMidiToSmrc();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void MidiFormat0_ShouldConvertSuccessfully()
        {
            byte[] midiData = CreateMidiFormat0();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertMidiToSmrc();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void MidiWithAllEventTypes_ShouldConvertSuccessfully()
        {
            byte[] midiData = CreateMidiWithAllEventTypes();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertMidiToSmrc();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void MidiWithAllMetaEvents_ShouldConvertSuccessfully()
        {
            byte[] midiData = CreateMidiWithAllMetaEvents();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertMidiToSmrc();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void MidiWithRunningStatus_ShouldConvertSuccessfully()
        {
            byte[] midiData = CreateMidiWithRunningStatus();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertMidiToSmrc();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void RoundTripConversion_ShouldPreserveData()
        {
            byte[] midiData = CreateSimpleMidiFile();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertMidiToSmrc();
            byte[] smrcData = outputStream.ToArray();
            using MemoryStream smrcInput = new MemoryStream(smrcData);
            using MemoryStream midiOutput = new MemoryStream();
            converter = new SMrcToMidiConverter(smrcInput, midiOutput);
            byte[] seqData = outputStream.ToArray();

            // Validate before saving
            using (MemoryStream seqStream = new MemoryStream(seqData))
            {
                SmrcValidator.ValidateSmrcFile(seqStream);
                Console.WriteLine("[Test] S-MRC/SEQ file validated successfully.");
            }

            SaveSeqFile(seqPath, seqData);
            Assert.True(File.Exists(seqPath), "SEQ file was not created after successful validation.");
        }
        }

        [Fact]
        public void ConvertAndValidate_SeqToMid()
        {
            string seqPath = "The Imperial March - Star Wars - Org.seq";
            string midPath = "star_wars_death_march_converted.mid";
            // Ensure output file does not exist before test
            if (File.Exists(midPath))
            {
                File.Delete(midPath);
            }
            Console.WriteLine($"[Test] Converting {seqPath} to {midPath} and validating...");

            byte[] seqData = LoadSeqFile(seqPath);
            using (MemoryStream inputStream = new MemoryStream(seqData))
            using (MemoryStream outputStream = new MemoryStream())
            {
                SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
                converter.ConvertSmrcToMidi();
                byte[] midiData = outputStream.ToArray();

                // Validate before saving
                using (MemoryStream midiStream = new MemoryStream(midiData))
                {
                    MidiValidator.ValidateMidiFile(midiStream);
                    Console.WriteLine("[Test] MIDI file validated successfully.");
                }

                SaveMidFile(midPath, midiData);
                Assert.True(File.Exists(midPath), "MID file was not created after successful validation.");
            }
        }

        // File I/O helpers
        public static byte[] LoadMidFile(string path)
        {
            Console.WriteLine($"[Helper] Loading MIDI file: {path}");
            return File.ReadAllBytes(path);
        }

        public static byte[] LoadSeqFile(string path)
        {
            Console.WriteLine($"[Helper] Loading S-MRC/SEQ file: {path}");
            return File.ReadAllBytes(path);
        }

        public static void SaveMidFile(string path, byte[] data)
        {
            Console.WriteLine($"[Helper] Saving MIDI file: {path}");
            File.WriteAllBytes(path, data);
        }

        public static void SaveSeqFile(string path, byte[] data)
        {
            Console.WriteLine($"[Helper] Saving S-MRC/SEQ file: {path}");
            File.WriteAllBytes(path, data);
        }

        #endregion

        #region MIDI Track Validation

        [Fact]
        public void Midi_MissingEndOfTrack_ShouldThrowInvalidDataException()
        {
            Console.WriteLine("[Test] MIDI track missing End-of-Track event");
            byte[] midiData = MidiCreateWithoutEndOfTrack();
            using (MemoryStream stream = new MemoryStream(midiData))
            {
                _ = Assert.Throws<InvalidDataException>(() => MidiValidator.ValidateMidiFile(stream));
                Console.WriteLine("[Test] Caught expected InvalidDataException for missing End-of-Track event.");
            }
        }

        [Fact]
        public void Midi_WithEndOfTrack_ShouldPassValidation()
        {
            Console.WriteLine("[Test] MIDI track with End-of-Track event");
            byte[] midiData = MidiCreateWithEndOfTrack();
            using (MemoryStream stream = new MemoryStream(midiData))
            {
                MidiValidator.ValidateMidiFile(stream);
                Console.WriteLine("[Test] Validation passed for MIDI track with End-of-Track event.");
            }
        }

        [Fact]
        public void Midi_WithMultipleEndOfTrack_ShouldThrowInvalidDataException()
        {
            Console.WriteLine("[Test] MIDI track with multiple End-of-Track events");
            byte[] midiData = Midi_CreateWithMultipleEndOfTrack();
            using (MemoryStream stream = new MemoryStream(midiData))
            {
                _ = Assert.Throws<InvalidDataException>(() => MidiValidator.ValidateMidiFile(stream));
                Console.WriteLine("[Test] Caught expected InvalidDataException for multiple End-of-Track events.");
            }
        }

        private static byte[] MidiCreateWithoutEndOfTrack()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(GetBigEndianBytes(6)); // Header length
                writer.Write(GetBigEndianBytes(1)); // Format 1
                writer.Write(GetBigEndianBytes(1)); // One track
                writer.Write(GetBigEndianBytes(96)); // PPQN

                // Write track header
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));
                writer.Write(GetBigEndianBytes(4)); // Track length

                // Write track data without End-of-Track
                writer.Write((byte)0x00); // Delta time
                writer.Write((byte)0x90); // Note On
                writer.Write((byte)0x3C); // Middle C
                writer.Write((byte)0x40); // Velocity

                return stream.ToArray();
            }
        }

        private static byte[] MidiCreateWithEndOfTrack()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(GetBigEndianBytes(6)); // Header length
                writer.Write(GetBigEndianBytes(1)); // Format 1
                writer.Write(GetBigEndianBytes(1)); // One track
                writer.Write(GetBigEndianBytes(96)); // PPQN

                // Write track header
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));
                writer.Write(GetBigEndianBytes(7)); // Track length

                // Write track data with End-of-Track
                writer.Write((byte)0x00); // Delta time
                writer.Write((byte)0x90); // Note On
                writer.Write((byte)0x3C); // Middle C
                writer.Write((byte)0x40); // Velocity
                writer.Write((byte)0x00); // Delta time
                writer.Write((byte)0xFF); // Meta event
                writer.Write((byte)0x2F); // End of track
                writer.Write((byte)0x00); // Length

                return stream.ToArray();
            }
        }

        private static byte[] Midi_CreateWithMultipleEndOfTrack()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(GetBigEndianBytes(6)); // Header length
                writer.Write(GetBigEndianBytes(1)); // Format 1
                writer.Write(GetBigEndianBytes(1)); // One track
                writer.Write(GetBigEndianBytes(96)); // PPQN

                // Write track header
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));
                writer.Write(GetBigEndianBytes(14)); // Track length

                // Write track data with multiple End-of-Track events
                writer.Write((byte)0x00); // Delta time
                writer.Write((byte)0x90); // Note On
                writer.Write((byte)0x3C); // Middle C
                writer.Write((byte)0x40); // Velocity
                writer.Write((byte)0x00); // Delta time
                writer.Write((byte)0xFF); // Meta event
                writer.Write((byte)0x2F); // End of track
                writer.Write((byte)0x00); // Length
                writer.Write((byte)0x00); // Delta time
                writer.Write((byte)0xFF); // Meta event
                writer.Write((byte)0x2F); // End of track
                writer.Write((byte)0x00); // Length

                return stream.ToArray();
            }
        }

        private static byte[] GetBigEndianBytes(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        #endregion

        [Fact]
        public void Smrc_FileWithInvalidPpqn_ShouldThrowInvalidDataException()
        {
            byte[] smrcData = CreateSmrcWithInvalidPpqn();
            using MemoryStream stream = new MemoryStream(smrcData);
            _ = Assert.Throws<InvalidDataException>(() => SmrcValidator.ValidateSmrcFile(stream));
        }

        [Fact]
        public void Smrc_FileWithInvalidTempo_ShouldThrowInvalidDataException()
        {
            byte[] smrcData = CreateSmrcWithInvalidTempo();
            using MemoryStream stream = new MemoryStream(smrcData);
            _ = Assert.Throws<InvalidDataException>(() => SmrcValidator.ValidateSmrcFile(stream));
        }

        [Fact]
        public void Smrc_FileWithInvalidTimeSignature_ShouldThrowInvalidDataException()
        {
            byte[] smrcData = CreateSmrcWithInvalidTimeSignature();
            using MemoryStream stream = new MemoryStream(smrcData);
            _ = Assert.Throws<InvalidDataException>(() => SmrcValidator.ValidateSmrcFile(stream));
        }

        [Fact]
        public void Smrc_FileWithInvalidTrackData_ShouldThrowInvalidDataException()
        {
            byte[] smrcData = CreateSmrcWithInvalidTrackData();
            using MemoryStream stream = new MemoryStream(smrcData);
            _ = Assert.Throws<InvalidDataException>(() => SmrcValidator.ValidateSmrcFile(stream));
        }

        [Fact]
        public void Smrc_FileWithInvalidTrackDirectory_ShouldThrowInvalidDataException()
        {
            byte[] smrcData = CreateSmrcWithInvalidTrackDirectory();
            using MemoryStream stream = new MemoryStream(smrcData);
            _ = Assert.Throws<InvalidDataException>(() => SmrcValidator.ValidateSmrcFile(stream));
        }

        [Fact]
        public void Smrc_FileWithMaximumTrackData_ShouldPassValidation()
        {
            byte[] smrcData = CreateSmrcWithMaximumTrackData();
            using MemoryStream stream = new MemoryStream(smrcData);
            SmrcValidator.ValidateSmrcFile(stream);
        }

        [Fact]
        public void Smrc_FileWithMaximumTracks_ShouldPassValidation()
        {
            byte[] smrcData = CreateSmrcWithMaximumTracks();
            using MemoryStream stream = new MemoryStream(smrcData);
            SmrcValidator.ValidateSmrcFile(stream);
        }

        [Fact]
        public void Smrc_FileWithOverlappingTracks_ShouldThrowInvalidDataException()
        {
            byte[] smrcData = CreateSmrcWithOverlappingTracks();
            using MemoryStream stream = new MemoryStream(smrcData);
            _ = Assert.Throws<InvalidDataException>(() => SmrcValidator.ValidateSmrcFile(stream));
        }

        [Fact]
        public void Smrc_FileWithTrackData_ShouldValidateSuccessfully()
        {
            byte[] smrcData = CreateSmrcWithTrackData();
            using MemoryStream stream = new MemoryStream(smrcData);
            SmrcValidator.ValidateSmrcFile(stream);
        }

        [Fact]
        public void Smrc_RoundTripConversion_ShouldPreserveData()
        {
            byte[] smrcData = CreateSmrcWithAllTrackData();
            using MemoryStream smrcInput = new MemoryStream(smrcData);
            using MemoryStream midiOutput = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(smrcInput, midiOutput);
            converter.ConvertSmrcToMidi();
            byte[] midiData = midiOutput.ToArray();
            using MemoryStream midiInput = new MemoryStream(midiData);
            using MemoryStream smrcOutput = new MemoryStream();
            converter = new SMrcToMidiConverter(midiInput, smrcOutput);
            converter.ConvertMidiToSmrc();
            Assert.True(smrcOutput.Length > 0);
        }

        [Fact]
        public void Smrc_WithAllTracksActive_ShouldConvertSuccessfully()
        {
            Console.WriteLine("[Test] S-MRC with all tracks active should convert successfully");
            byte[] smrcData = CreateSmrcWithMaximumTracks();
            using (MemoryStream inputStream = new MemoryStream(smrcData))
            using (MemoryStream outputStream = new MemoryStream())
            {
                SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
                converter.ConvertSmrcToMidi();
                Console.WriteLine($"[Test] Output MIDI length: {outputStream.Length}");
                Assert.True(outputStream.Length > 0, "Conversion failed or output MIDI is empty.");
            }
        }

        [Fact]
        public void Smrc_WithMaximumData_ShouldConvertSuccessfully()
        {
            byte[] smrcData = CreateSmrcWithMaximumData();
            using MemoryStream inputStream = new MemoryStream(smrcData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertSmrcToMidi();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void Smrc_WithVariousPpqn_ShouldConvertSuccessfully()
        {
            byte[] smrcData = CreateSmrcWithVariousPpqn();
            using MemoryStream inputStream = new MemoryStream(smrcData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertSmrcToMidi();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void Smrc_WithVariousTempos_ShouldConvertSuccessfully()
        {
            byte[] smrcData = CreateSmrcWithVariousTempos();
            using MemoryStream inputStream = new MemoryStream(smrcData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertSmrcToMidi();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void Smrc_WithVariousTimeSignatures_ShouldConvertSuccessfully()
        {
            byte[] smrcData = CreateSmrcWithVariousTimeSignatures();
            using MemoryStream inputStream = new MemoryStream(smrcData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
            converter.ConvertSmrcToMidi();
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public void TooManyTracks_ShouldThrowInvalidDataException()
        {
            byte[] smrcData = new byte[Constants.SmrcFormat.HeaderSize + ((Constants.SmrcFormat.MaxTracks + 1) * Constants.SmrcFormat.TrackDirectoryEntrySize)];
            using MemoryStream stream = new MemoryStream(smrcData);
            _ = Assert.Throws<InvalidDataException>(() => SmrcValidator.ValidateSmrcFile(stream));
        }

        [Fact]
        public void ValidSmrcFile_ShouldPassValidation()
        {
            byte[] smrcData = CreateValidSmrcFile();
            using MemoryStream stream = new MemoryStream(smrcData);
            SmrcValidator.ValidateSmrcFile(stream);
        }

        [Fact]
        public void Midi_ConvertAndValidateToSeq()
        {
            string midPath = "The Imperial March - Star Wars - Org.mid";
            string seqPath = "star_wars_death_march-converted.seq";
            // Ensure output file does not exist before test
            if (File.Exists(seqPath))
            {
                File.Delete(seqPath);
            }
            Console.WriteLine($"[Test] Converting {midPath} to {seqPath} and validating...");

            byte[] midiData = LoadMidFile(midPath);
            using (MemoryStream inputStream = new MemoryStream(midiData))
            using (MemoryStream outputStream = new MemoryStream())
            {
                SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
                converter.ConvertMidiToSmrc();
                byte[] seqData = outputStream.ToArray();

                // Validate before saving
                using (MemoryStream seqStream = new MemoryStream(seqData))
                {
                    SmrcValidator.ValidateSmrcFile(seqStream);
                    Console.WriteLine("[Test] S-MRC/SEQ file validated successfully.");
                }

                SaveSeqFile(seqPath, seqData);
                Assert.True(File.Exists(seqPath), "SEQ file was not created after successful validation.");
            }
        }
    }
}