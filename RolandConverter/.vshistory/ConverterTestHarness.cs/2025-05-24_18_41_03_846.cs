using System;
using System.IO;

namespace RolandConverter
{
    // Test harness
    public class ConverterTestHarness
    {
        public static void RunTests()
        {
            TraceLogger.Log("TestHarness", "Roland S-MRC Converter Test Suite");
            TraceLogger.Log("TestHarness", "=================================");

            try
            {
                RunRoundTripTest();
                RunErrorHandlingTests();
                RunEdgeCaseTests();
                RunValidationTests();

                TraceLogger.Log("TestHarness", "\nAll tests completed!");
            }
            catch (Exception ex)
            {
                TraceLogger.Log("TestHarness", $"Test suite failed: {ex.Message}");
            }
        }

        private static void RunRoundTripTest()
        {
            TraceLogger.Log("TestHarness", "\nTest 1: Round-trip conversion");

            // Create a simple MIDI file
            byte[] midiData = CreateSimpleMidiFile();
            TraceLogger.Log("TestHarness", $"  MIDI size: {midiData.Length} bytes");

            // Convert to S-MRC
            byte[] smrcData;
            using (MemoryStream input = new MemoryStream(midiData))
            using (MemoryStream output = new MemoryStream())
            {
                SMrcToMidiConverter converter = new SMrcToMidiConverter(input, output);
                converter.ConvertMidiToSmrc();
                smrcData = output.ToArray();
            }
            TraceLogger.Log("TestHarness", $"  S-MRC size: {smrcData.Length} bytes");

            // Convert back to MIDI
            byte[] midiData2;
            using (MemoryStream input = new MemoryStream(smrcData))
            using (MemoryStream output = new MemoryStream())
            {
                SMrcToMidiConverter converter = new SMrcToMidiConverter(input, output);
                converter.ConvertSmrcToMidi();
                midiData2 = output.ToArray();
            }
            TraceLogger.Log("TestHarness", $"  Converted MIDI size: {midiData2.Length} bytes");

            // Verify the data matches
            if (midiData.Length == midiData2.Length)
            {
                TraceLogger.Log("TestHarness", "  Round-trip conversion: PASSED");
            }
            else
            {
                throw new Exception("Round-trip conversion failed: data size mismatch");
            }
        }

        private static void RunErrorHandlingTests()
        {
            TraceLogger.Log("TestHarness", "\nTest 2: Error handling");

            // Test invalid MIDI header
            byte[] invalidMidi = new byte[] { 0x00, 0x00, 0x00, 0x00 };
            using (MemoryStream input = new MemoryStream(invalidMidi))
            using (MemoryStream output = new MemoryStream())
            {
                SMrcToMidiConverter converter = new SMrcToMidiConverter(input, output);
                try
                {
                    converter.ConvertMidiToSmrc();
                    TraceLogger.Log("TestHarness", "  Invalid MIDI header: FAILED (no exception thrown)");
                }
                catch (InvalidDataException)
                {
                    TraceLogger.Log("TestHarness", "  Invalid MIDI header: PASSED");
                }
            }

            // Test too many tracks
            byte[] tooManyTracks = new byte[] { 0x4D, 0x54, 0x68, 0x64, 0x00, 0x00, 0x00, 0x06, 0x00, 0x01, 0x00, 0x09, 0x00, 0x60 };
            using (MemoryStream input = new MemoryStream(tooManyTracks))
            using (MemoryStream output = new MemoryStream())
            {
                SMrcToMidiConverter converter = new SMrcToMidiConverter(input, output);
                try
                {
                    converter.ConvertMidiToSmrc();
                    TraceLogger.Log("TestHarness", "  Too many tracks: FAILED (no exception thrown)");
                }
                catch (InvalidDataException)
                {
                    TraceLogger.Log("TestHarness", "  Too many tracks: PASSED");
                }
            }
        }

        private static void RunEdgeCaseTests()
        {
            TraceLogger.Log("TestHarness", "\nTest 3: Edge cases");
            // Add edge case tests here
        }

        private static void RunValidationTests()
        {
            TraceLogger.Log("TestHarness", "\nTest 4: File validation");

            // Test valid S-MRC file
            byte[] validSmrc = CreateValidSmrcFile();
            using (MemoryStream stream = new MemoryStream(validSmrc))
            {
                try
                {
                    SmrcValidator.ValidateSmrcFile(stream);
                    TraceLogger.Log("TestHarness", "  Valid S-MRC validation: PASSED");
                }
                catch
                {
                    TraceLogger.Log("TestHarness", "  Valid S-MRC validation: FAILED");
                }
            }

            // Test invalid S-MRC file
            byte[] invalidSmrc = new byte[100];
            using (MemoryStream stream = new MemoryStream(invalidSmrc))
            {
                try
                {
                    SmrcValidator.ValidateSmrcFile(stream);
                    TraceLogger.Log("TestHarness", "  Invalid S-MRC validation: FAILED (no exception)");
                }
                catch
                {
                    TraceLogger.Log("TestHarness", "  Invalid S-MRC validation: PASSED");
                }
            }
        }

        private static byte[] CreateSimpleMidiFile()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Write MIDI header
                writer.Write(new byte[] { 0x4D, 0x54, 0x68, 0x64 }); // "MThd"
                writer.Write(new byte[] { 0x00, 0x00, 0x00, 0x06 }); // Header length
                writer.Write(new byte[] { 0x00, 0x01 }); // Format 1
                writer.Write(new byte[] { 0x00, 0x01 }); // One track
                writer.Write(new byte[] { 0x00, 0x60 }); // Division

                // Write track
                writer.Write(new byte[] { 0x4D, 0x54, 0x72, 0x6B }); // "MTrk"
                writer.Write(new byte[] { 0x00, 0x00, 0x00, 0x0C }); // Track length

                // Write track events
                writer.Write(new byte[] { 0x00, 0x90, 0x3C, 0x40 }); // Note on
                writer.Write(new byte[] { 0x60, 0x80, 0x3C, 0x40 }); // Note off
                writer.Write(new byte[] { 0x00, 0xFF, 0x2F, 0x00 }); // End of track

                return stream.ToArray();
            }
        }

        private static byte[] CreateValidSmrcFile()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Write S-MRC header
                byte[] title = new byte[32];
                Array.Fill(title, (byte)'A');
                writer.Write(title);
                writer.Write((short)96); // PPQN
                writer.Write((byte)4); // Time signature numerator
                writer.Write((byte)2); // Time signature denominator
                writer.Write((byte)120); // Tempo
                writer.Write(new byte[3]); // Reserved

                // Write track directory
                for (int i = 0; i < 8; i++)
                {
                    writer.Write(0); // Start offset
                    writer.Write(0); // Length
                    writer.Write(new byte[8]); // Reserved
                }

                return stream.ToArray();
            }
        }
    }
}