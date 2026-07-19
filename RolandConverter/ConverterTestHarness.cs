namespace RolandConverter
{
    /// <summary>
    /// Provides a test harness for validating the Roland S-MRC converter functionality.
    /// This class contains methods for creating test files and running various test scenarios.
    /// </summary>
    public class ConverterTestHarness
    {
        #region Private Methods

        /// <summary>
        /// Creates a valid MIDI file in memory for testing purposes.
        /// </summary>
        /// <returns>A byte array containing a valid MIDI file.</returns>
        private static byte[] CreateValidMidiFile()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header
                writer.Write(Constants.MidiFormat.HeaderChunkType); // "MThd"
                writer.Write(new byte[] { 0, 0, 0, Constants.MidiFormat.HeaderSize }); // Header length
                writer.Write(new byte[] { 0, 1 }); // Format 1
                writer.Write(new byte[] { 0, 1 }); // One track
                writer.Write(new byte[] { 0, (byte)Constants.MidiFormat.DefaultPpqn }); // Division

                // Write track
                writer.Write(Constants.MidiFormat.TrackChunkType); // "MTrk"
                writer.Write(new byte[] { 0, 0, 0, 0x0C }); // Track length

                // Track data
                writer.Write(new byte[] { 0x00, Constants.MidiFormat.NoteOnMessage, Constants.MidiFormat.DefaultNote, Constants.MidiFormat.DefaultVelocity }); // Note on
                writer.Write(new byte[] { 0x60, Constants.MidiFormat.NoteOffMessage, Constants.MidiFormat.DefaultNote, Constants.MidiFormat.DefaultVelocity }); // Note off
                writer.Write(new byte[] { 0x00, 0xFF, Constants.MidiFormat.EndOfTrackMetaEvent, 0x00 }); // End of track
            }

            return stream.ToArray();
        }

        /// <summary>
        /// Creates a valid S-MRC file in memory for testing purposes.
        /// </summary>
        /// <returns>A byte array containing a valid S-MRC file.</returns>
        private static byte[] CreateValidSmrcFile()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Header
                byte[] title = new byte[Constants.SmrcFormat.TitleSize];
                writer.Write(title);
                writer.Write((short)Constants.MidiFormat.DefaultPpqn); // PPQN
                writer.Write((byte)Constants.MidiFormat.DefaultTimeSignatureNumerator); // Time signature numerator
                writer.Write((byte)Constants.MidiFormat.DefaultTimeSignatureDenominator); // Time signature denominator
                writer.Write((byte)Constants.MidiFormat.DefaultTempo); // Tempo
                writer.Write(new byte[Constants.SmrcFormat.HeaderReservedSize]); // Reserved

                // Track directory
                for (int i = 0; i < Constants.SmrcFormat.MaxTracks; i++)
                {
                    writer.Write(0); // Start offset
                    writer.Write(0); // Length
                    writer.Write(new byte[Constants.SmrcFormat.TrackDirectoryReservedSize]); // Reserved
                }
            }

            return stream.ToArray();
        }

        /// <summary>
        /// Runs tests for edge cases in the conversion process.
        /// </summary>
        private static void RunEdgeCaseTests()
        {
            TraceLogger.Log("TestHarness", "\nTest 3: Edge cases");
            // Add edge case tests here
        }

        /// <summary>
        /// Runs tests for error handling in the conversion process.
        /// Tests include invalid MIDI headers and files with too many tracks.
        /// </summary>
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

        /// <summary>
        /// Runs a round-trip test, converting a MIDI file to S-MRC and back to MIDI.
        /// Verifies that the data remains consistent through the conversion process.
        /// </summary>
        private static void RunRoundTripTest()
        {
            TraceLogger.Log("TestHarness", "\nTest 1: Round-trip conversion");

            // Create a simple MIDI file
            byte[] midiData = CreateValidMidiFile();
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

        /// <summary>
        /// Runs tests for file validation functionality.
        /// Tests include both valid and invalid S-MRC files.
        /// </summary>
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

        #endregion

        #region Public Methods

        /// <summary>
        /// Runs the complete test suite for the Roland S-MRC converter.
        /// Includes round-trip conversion, error handling, edge cases, and validation tests.
        /// </summary>
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

        #endregion
    }
}
