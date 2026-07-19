using System;
using System.Text;

namespace RolandConverter
{
    /// <summary>
    /// Provides methods for building S-MRC messages in a consistent way.
    /// </summary>
    public static class SmrcMessageBuilder
    {
        /// <summary>
        /// Creates a note on message.
        /// </summary>
        /// <param name="note">The note value (0-127).</param>
        /// <param name="velocity">The velocity value (0-127).</param>
        /// <returns>A byte array containing the note on message.</returns>
        public static byte[] CreateNoteOn(byte note, byte velocity)
        {
            return new byte[] { (byte)(Constants.MidiFormat.NoteOnMessage | 0), note, velocity };
        }

        /// <summary>
        /// Creates a note off message.
        /// </summary>
        /// <param name="note">The note value (0-127).</param>
        /// <param name="velocity">The velocity value (0-127).</param>
        /// <returns>A byte array containing the note off message.</returns>
        public static byte[] CreateNoteOff(byte note, byte velocity)
        {
            return new byte[] { (byte)(Constants.MidiFormat.NoteOffMessage | 0), note, velocity };
        }

        /// <summary>
        /// Creates a control change message.
        /// </summary>
        /// <param name="control">The control number (0-127).</param>
        /// <param name="value">The control value (0-127).</param>
        /// <returns>A byte array containing the control change message.</returns>
        public static byte[] CreateControlChange(byte control, byte value)
        {
            return new byte[] { (byte)(0xB0 | 0), control, value };
        }

        /// <summary>
        /// Creates a program change message.
        /// </summary>
        /// <param name="program">The program number (0-127).</param>
        /// <returns>A byte array containing the program change message.</returns>
        public static byte[] CreateProgramChange(byte program)
        {
            return new byte[] { (byte)(0xC0 | 0), program };
        }

        /// <summary>
        /// Creates a channel pressure message.
        /// </summary>
        /// <param name="pressure">The pressure value (0-127).</param>
        /// <returns>A byte array containing the channel pressure message.</returns>
        public static byte[] CreateChannelPressure(byte pressure)
        {
            return new byte[] { (byte)(0xD0 | 0), pressure };
        }

        /// <summary>
        /// Creates a pitch bend message.
        /// </summary>
        /// <param name="value">The pitch bend value (0-16383).</param>
        /// <returns>A byte array containing the pitch bend message.</returns>
        public static byte[] CreatePitchBend(int value)
        {
            return new byte[] { (byte)(0xE0 | 0), (byte)(value & 0x7F), (byte)((value >> 7) & 0x7F) };
        }

        /// <summary>
        /// Creates a system exclusive message.
        /// </summary>
        /// <param name="data">The system exclusive data.</param>
        /// <returns>A byte array containing the system exclusive message.</returns>
        public static byte[] CreateSystemExclusive(byte[] data)
        {
            byte[] message = new byte[data.Length + 2];
            message[0] = 0xF0;
            Array.Copy(data, 0, message, 1, data.Length);
            message[message.Length - 1] = Constants.RolandMeta.SysExEnd;
            return message;
        }

        /// <summary>
        /// Creates a tempo message.
        /// </summary>
        /// <param name="bpm">The tempo in BPM (Beats Per Minute).</param>
        /// <returns>A byte array containing the tempo message.</returns>
        public static byte[] CreateTempo(int bpm)
        {
            // Convert BPM to microseconds per quarter note
            int microsecondsPerQuarterNote = 60000000 / bpm;
            return new byte[] { 0xFF, Constants.MidiFormat.TempoMetaEvent, 0x03,
                (byte)((microsecondsPerQuarterNote >> 16) & 0xFF),
                (byte)((microsecondsPerQuarterNote >> 8) & 0xFF),
                (byte)(microsecondsPerQuarterNote & 0xFF) };
        }

        /// <summary>
        /// Creates a time signature message.
        /// </summary>
        /// <param name="numerator">The time signature numerator.</param>
        /// <param name="denominator">The time signature denominator (as a power of 2).</param>
        /// <returns>A byte array containing the time signature message.</returns>
        public static byte[] CreateTimeSignature(int numerator, int denominator)
        {
            return new byte[] { 0xFF, Constants.MidiFormat.TimeSignatureMetaEvent, 0x04,
                (byte)numerator,
                (byte)(Math.Log2(denominator)),
                Constants.MidiFormat.ClocksPerMetronomeClick,
                Constants.MidiFormat.ThirtySecondNotesPerQuarterNote };
        }

        /// <summary>
        /// Creates a track name message.
        /// </summary>
        /// <param name="name">The track name.</param>
        /// <returns>A byte array containing the track name message.</returns>
        public static byte[] CreateTrackName(string name)
        {
            byte[] nameBytes = System.Text.Encoding.ASCII.GetBytes(name);
            byte[] message = new byte[nameBytes.Length + 3];
            message[0] = 0xFF;
            message[1] = Constants.MidiFormat.TrackNameMetaEvent;
            message[2] = (byte)nameBytes.Length;
            Array.Copy(nameBytes, 0, message, 3, nameBytes.Length);
            return message;
        }

        /// <summary>
        /// Creates an end of track message.
        /// </summary>
        /// <returns>A byte array containing the end of track message.</returns>
        public static byte[] CreateEndOfTrack()
        {
            return new byte[] { 0xFF, Constants.MidiFormat.EndOfTrackMetaEvent, 0x00 };
        }

        /// <summary>
        /// Writes a variable length quantity to the binary _Writer.
        /// </summary>
        /// <param name="writer">The binary _Writer to write to.</param>
        /// <param name="value">The value to write.</param>
        private static void WriteVariableLengthQuantity(BinaryWriter writer, int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Value must be non-negative.");
            }

            if (value == 0)
            {
                writer.Write((byte)0);
                return;
            }

            List<byte> bytes = new List<byte>();
            while (value > 0)
            {
                bytes.Add((byte)(value & 0x7F));
                value >>= 7;
            }

            for (int i = bytes.Count - 1; i >= 0; i--)
            {
                if (i > 0)
                {
                    writer.Write((byte)(bytes[i] | 0x80));
                }
                else
                {
                    writer.Write(bytes[i]);
                }
            }
        }
    }
}