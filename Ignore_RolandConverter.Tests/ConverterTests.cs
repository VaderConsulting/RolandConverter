using System;
using System.IO;
using System.Text;
using Xunit;

namespace RolandConverter.Tests
{
    public class ConverterTests
    {
        [Fact]
        public void Midi_WithEndOfTrack_ShouldPassValidation()
        {
            const string method = nameof(Midi_WithEndOfTrack_ShouldPassValidation);
            TraceLogger.Log(method, "[Test] MIDI track with End-of-Track event");
            byte[] midiData = Midi_CreateWithEndOfTrack();
            TraceLogger.Log(method, $"Generated MIDI data: {BitConverter.ToString(midiData)}");
            if (midiData.Length >= 18)
            {
                TraceLogger.Log(method, $"Bytes 14-17: {BitConverter.ToString(midiData, 14, 4)} (ASCII: '{System.Text.Encoding.ASCII.GetString(midiData, 14, 4)}')");
            }
            else
            {
                TraceLogger.Log(method, $"MIDI data too short to contain track header at 14-17. Length: {midiData.Length}");
            }
            using (MemoryStream stream = new MemoryStream(midiData))
            {
                MidiValidator.ValidateMidiFile(stream);
            }
        }

        private static byte[] Midi_CreateWithEndOfTrack()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(Helper.GetBigEndianBytes(6, 4)); // Header length
                writer.Write(Helper.GetBigEndianBytes(1, 2)); // Format 1
                writer.Write(Helper.GetBigEndianBytes(1, 2)); // One track
                writer.Write(Helper.GetBigEndianBytes(96, 2)); // PPQN

                // Write track header
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));
                writer.Write(Helper.GetBigEndianBytes(8, 4)); // Track length (was 7, should be 8)

                // Write track data with End-of-Track
                writer.Write((byte)0x00); // Delta time
                writer.Write((byte)0x90); // Note On
                writer.Write((byte)0x3C); // Middle C
                writer.Write((byte)0x40); // Velocity
                writer.Write((byte)0x00); // Delta time
                writer.Write((byte)0xFF); // Meta event
                writer.Write((byte)0x2F); // End of track
                writer.Write((byte)0x00); // Length (MUST be present)

                return stream.ToArray();
            }
        }

        private static byte[] Midi_CreateWithoutEndOfTrack()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(Helper.GetBigEndianBytes(6, 4)); // Header length
                writer.Write(Helper.GetBigEndianBytes(1, 2)); // Format 1
                writer.Write(Helper.GetBigEndianBytes(1, 2)); // One track
                writer.Write(Helper.GetBigEndianBytes(96, 2)); // PPQN

                // Write track header
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));
                writer.Write(Helper.GetBigEndianBytes(4, 4)); // Track length

                // Write track data without End-of-Track
                writer.Write((byte)0x00); // Delta time
                writer.Write((byte)0x90); // Note On
                writer.Write((byte)0x3C); // Middle C
                writer.Write((byte)0x40); // Velocity

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
                writer.Write(Helper.GetBigEndianBytes(6, 4)); // Header length
                writer.Write(Helper.GetBigEndianBytes(1, 2)); // Format 1
                writer.Write(Helper.GetBigEndianBytes(1, 2)); // One track
                writer.Write(Helper.GetBigEndianBytes(96, 2)); // PPQN

                // Write track header
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));
                writer.Write(Helper.GetBigEndianBytes(14, 4)); // Track length

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
    }
}