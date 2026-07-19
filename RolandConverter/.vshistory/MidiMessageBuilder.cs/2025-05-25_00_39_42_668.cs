using System.Text;

namespace RolandConverter
{
    /// <summary>
    /// Provides methods for building MIDI messages in a consistent way.
    /// </summary>
    public static class MidiMessageBuilder
    {
        /// <summary>
        /// Creates a note-on message.
        /// </summary>
        /// <param name="note">The note number (0-127).</param>
        /// <param name="velocity">The velocity (0-127).</param>
        /// <returns>A byte array containing the note-on message.</returns>
        public static byte[] CreateNoteOn(byte note, byte velocity)
        {
            return CreateNoteOn(note, velocity, 0, 0, false);
        }

        /// <summary>
        /// Creates a note-on message with delta time.
        /// </summary>
        /// <param name="note">The note number (0-127).</param>
        /// <param name="velocity">The velocity (0-127).</param>
        /// <param name="deltaTime">The delta time in ticks.</param>
        /// <returns>A byte array containing the note-on message.</returns>
        public static byte[] CreateNoteOn(byte note, byte velocity, int deltaTime)
        {
            return CreateNoteOn(note, velocity, deltaTime, 0, false);
        }

        /// <summary>
        /// Creates a note-on message with delta time and channel.
        /// </summary>
        /// <param name="note">The note number (0-127).</param>
        /// <param name="velocity">The velocity (0-127).</param>
        /// <param name="deltaTime">The delta time in ticks.</param>
        /// <param name="channel">The MIDI channel (0-15).</param>
        /// <returns>A byte array containing the note-on message.</returns>
        public static byte[] CreateNoteOn(byte note, byte velocity, int deltaTime, byte channel)
        {
            return CreateNoteOn(note, velocity, deltaTime, channel, false);
        }

        /// <summary>
        /// Creates a note-on message with delta time, channel, and running status flag.
        /// </summary>
        /// <param name="note">The note number (0-127).</param>
        /// <param name="velocity">The velocity (0-127).</param>
        /// <param name="deltaTime">The delta time in ticks.</param>
        /// <param name="channel">The MIDI channel (0-15).</param>
        /// <param name="useRunningStatus">Whether to use running status.</param>
        /// <returns>A byte array containing the note-on message.</returns>
        public static byte[] CreateNoteOn(byte note, byte velocity, int deltaTime, byte channel, bool useRunningStatus)
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write delta time
                WriteVariableLengthQuantity(writer, deltaTime);

                // Write status byte (if not using running status)
                if (!useRunningStatus)
                {
                    writer.Write((byte)(0x90 | channel));
                }

                // Write note and velocity
                writer.Write(note);
                writer.Write(velocity);

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates a note-off message.
        /// </summary>
        /// <param name="note">The note number (0-127).</param>
        /// <param name="velocity">The velocity (0-127).</param>
        /// <returns>A byte array containing the note-off message.</returns>
        public static byte[] CreateNoteOff(byte note, byte velocity)
        {
            return CreateNoteOff(note, velocity, 0, 0);
        }

        /// <summary>
        /// Creates a note-off message with delta time.
        /// </summary>
        /// <param name="note">The note number (0-127).</param>
        /// <param name="velocity">The velocity (0-127).</param>
        /// <param name="deltaTime">The delta time in ticks.</param>
        /// <returns>A byte array containing the note-off message.</returns>
        public static byte[] CreateNoteOff(byte note, byte velocity, int deltaTime)
        {
            return CreateNoteOff(note, velocity, deltaTime, 0);
        }

        /// <summary>
        /// Creates a note-off message with delta time and channel.
        /// </summary>
        /// <param name="note">The note number (0-127).</param>
        /// <param name="velocity">The velocity (0-127).</param>
        /// <param name="deltaTime">The delta time in ticks.</param>
        /// <param name="channel">The MIDI channel (0-15).</param>
        /// <returns>A byte array containing the note-off message.</returns>
        public static byte[] CreateNoteOff(byte note, byte velocity, int deltaTime, byte channel)
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write delta time
                WriteVariableLengthQuantity(writer, deltaTime);

                // Write status byte
                writer.Write((byte)(0x80 | channel));

                // Write note and velocity
                writer.Write(note);
                writer.Write(velocity);

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates a program change message.
        /// </summary>
        /// <param name="program">The program number (0-127).</param>
        /// <returns>A byte array containing the program change message.</returns>
        public static byte[] CreateProgramChange(byte program)
        {
            return CreateProgramChange(program, 0, 0);
        }

        /// <summary>
        /// Creates a program change message with delta time.
        /// </summary>
        /// <param name="program">The program number (0-127).</param>
        /// <param name="deltaTime">The delta time in ticks.</param>
        /// <returns>A byte array containing the program change message.</returns>
        public static byte[] CreateProgramChange(byte program, int deltaTime)
        {
            return CreateProgramChange(program, deltaTime, 0);
        }

        /// <summary>
        /// Creates a program change message with delta time and channel.
        /// </summary>
        /// <param name="program">The program number (0-127).</param>
        /// <param name="deltaTime">The delta time in ticks.</param>
        /// <param name="channel">The MIDI channel (0-15).</param>
        /// <returns>A byte array containing the program change message.</returns>
        public static byte[] CreateProgramChange(byte program, int deltaTime, byte channel)
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write delta time
                WriteVariableLengthQuantity(writer, deltaTime);

                // Write status byte
                writer.Write((byte)(0xC0 | channel));

                // Write program number
                writer.Write(program);

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates a control change message.
        /// </summary>
        /// <param name="control">The control number (0-127).</param>
        /// <param name="value">The control value (0-127).</param>
        /// <returns>A byte array containing the control change message.</returns>
        public static byte[] CreateControlChange(byte control, byte value)
        {
            return CreateControlChange(control, value, 0, 0);
        }

        /// <summary>
        /// Creates a control change message with delta time.
        /// </summary>
        /// <param name="control">The control number (0-127).</param>
        /// <param name="value">The control value (0-127).</param>
        /// <param name="deltaTime">The delta time in ticks.</param>
        /// <returns>A byte array containing the control change message.</returns>
        public static byte[] CreateControlChange(byte control, byte value, int deltaTime)
        {
            return CreateControlChange(control, value, deltaTime, 0);
        }

        /// <summary>
        /// Creates a control change message with delta time and channel.
        /// </summary>
        /// <param name="control">The control number (0-127).</param>
        /// <param name="value">The control value (0-127).</param>
        /// <param name="deltaTime">The delta time in ticks.</param>
        /// <param name="channel">The MIDI channel (0-15).</param>
        /// <returns>A byte array containing the control change message.</returns>
        public static byte[] CreateControlChange(byte control, byte value, int deltaTime, byte channel)
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write delta time
                WriteVariableLengthQuantity(writer, deltaTime);

                // Write status byte
                writer.Write((byte)(0xB0 | channel));

                // Write control and value
                writer.Write(control);
                writer.Write(value);

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates a pitch bend message.
        /// </summary>
        /// <param name="lsb">The least significant byte of the pitch bend value.</param>
        /// <param name="msb">The most significant byte of the pitch bend value.</param>
        /// <returns>A byte array containing the pitch bend message.</returns>
        public static byte[] CreatePitchBend(byte lsb, byte msb)
        {
            return CreatePitchBend(lsb, msb, 0, 0);
        }

        /// <summary>
        /// Creates a pitch bend message with delta time.
        /// </summary>
        /// <param name="lsb">The least significant byte of the pitch bend value.</param>
        /// <param name="msb">The most significant byte of the pitch bend value.</param>
        /// <param name="deltaTime">The delta time in ticks.</param>
        /// <returns>A byte array containing the pitch bend message.</returns>
        public static byte[] CreatePitchBend(byte lsb, byte msb, int deltaTime)
        {
            return CreatePitchBend(lsb, msb, deltaTime, 0);
        }

        /// <summary>
        /// Creates a pitch bend message with delta time and channel.
        /// </summary>
        /// <param name="lsb">The least significant byte of the pitch bend value.</param>
        /// <param name="msb">The most significant byte of the pitch bend value.</param>
        /// <param name="deltaTime">The delta time in ticks.</param>
        /// <param name="channel">The MIDI channel (0-15).</param>
        /// <returns>A byte array containing the pitch bend message.</returns>
        public static byte[] CreatePitchBend(byte lsb, byte msb, int deltaTime, byte channel)
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write delta time
                WriteVariableLengthQuantity(writer, deltaTime);

                // Write status byte
                writer.Write((byte)(0xE0 | channel));

                // Write pitch bend value
                writer.Write(lsb);
                writer.Write(msb);

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates a channel pressure (aftertouch) message.
        /// </summary>
        /// <param name="pressure">The pressure value (0-127).</param>
        /// <returns>A byte array containing the channel pressure message.</returns>
        public static byte[] CreateChannelPressure(byte pressure)
        {
            return CreateChannelPressure(pressure, 0, 0);
        }

        /// <summary>
        /// Creates a channel pressure message with delta time.
        /// </summary>
        /// <param name="pressure">The pressure value (0-127).</param>
        /// <param name="deltaTime">The delta time in ticks.</param>
        /// <returns>A byte array containing the channel pressure message.</returns>
        public static byte[] CreateChannelPressure(byte pressure, int deltaTime)
        {
            return CreateChannelPressure(pressure, deltaTime, 0);
        }

        /// <summary>
        /// Creates a channel pressure message with delta time and channel.
        /// </summary>
        /// <param name="pressure">The pressure value (0-127).</param>
        /// <param name="deltaTime">The delta time in ticks.</param>
        /// <param name="channel">The MIDI channel (0-15).</param>
        /// <returns>A byte array containing the channel pressure message.</returns>
        public static byte[] CreateChannelPressure(byte pressure, int deltaTime, byte channel)
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write delta time
                WriteVariableLengthQuantity(writer, deltaTime);

                // Write status byte
                writer.Write((byte)(0xD0 | channel));

                // Write pressure value
                writer.Write(pressure);

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates a polyphonic aftertouch message.
        /// </summary>
        /// <param name="note">The note number (0-127).</param>
        /// <param name="pressure">The pressure value (0-127).</param>
        /// <returns>A byte array containing the polyphonic aftertouch message.</returns>
        public static byte[] CreatePolyphonicAftertouch(byte note, byte pressure)
        {
            return CreatePolyphonicAftertouch(note, pressure, 0, 0);
        }

        /// <summary>
        /// Creates a polyphonic aftertouch message with delta time.
        /// </summary>
        /// <param name="note">The note number (0-127).</param>
        /// <param name="pressure">The pressure value (0-127).</param>
        /// <param name="deltaTime">The delta time in ticks.</param>
        /// <returns>A byte array containing the polyphonic aftertouch message.</returns>
        public static byte[] CreatePolyphonicAftertouch(byte note, byte pressure, int deltaTime)
        {
            return CreatePolyphonicAftertouch(note, pressure, deltaTime, 0);
        }

        /// <summary>
        /// Creates a polyphonic aftertouch message with delta time and channel.
        /// </summary>
        /// <param name="note">The note number (0-127).</param>
        /// <param name="pressure">The pressure value (0-127).</param>
        /// <param name="deltaTime">The delta time in ticks.</param>
        /// <param name="channel">The MIDI channel (0-15).</param>
        /// <returns>A byte array containing the polyphonic aftertouch message.</returns>
        public static byte[] CreatePolyphonicAftertouch(byte note, byte pressure, int deltaTime, byte channel)
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write delta time
                WriteVariableLengthQuantity(writer, deltaTime);

                // Write status byte
                writer.Write((byte)(0xA0 | channel));

                // Write note and pressure
                writer.Write(note);
                writer.Write(pressure);

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates a system exclusive message.
        /// </summary>
        /// <param name="data">The system exclusive data.</param>
        /// <returns>A byte array containing the system exclusive message.</returns>
        public static byte[] CreateSystemExclusive(byte[] data)
        {
            return CreateSystemExclusive(data, 0);
        }

        /// <summary>
        /// Creates a system exclusive message with delta time.
        /// </summary>
        /// <param name="data">The system exclusive data.</param>
        /// <param name="deltaTime">The delta time in ticks.</param>
        /// <returns>A byte array containing the system exclusive message.</returns>
        public static byte[] CreateSystemExclusive(byte[] data, int deltaTime)
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write delta time
                WriteVariableLengthQuantity(writer, deltaTime);

                // Write status byte
                writer.Write((byte)0xF0);

                // Write data length
                WriteVariableLengthQuantity(writer, data.Length);

                // Write data
                writer.Write(data);

                // Write end of exclusive
                writer.Write((byte)0xF7);

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates a tempo meta event.
        /// </summary>
        /// <param name="bpm">The tempo in beats per minute.</param>
        /// <returns>A byte array containing the tempo meta event.</returns>
        public static byte[] CreateTempo(int bpm)
        {
            return CreateTempo(bpm, 0);
        }

        /// <summary>
        /// Creates a tempo meta event with delta time.
        /// </summary>
        /// <param name="bpm">The tempo in beats per minute.</param>
        /// <param name="deltaTime">The delta time in ticks.</param>
        /// <returns>A byte array containing the tempo meta event.</returns>
        public static byte[] CreateTempo(int bpm, int deltaTime)
        {
            // Convert BPM to microseconds per quarter note
            // Formula: 60,000,000 / BPM = microseconds per quarter note
            int microsecondsPerQuarterNote = 60000000 / bpm;

            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write delta time
                WriteVariableLengthQuantity(writer, deltaTime);

                // Write meta event header
                writer.Write((byte)0xFF);
                writer.Write((byte)0x51);
                writer.Write((byte)0x03);

                // Write tempo value
                writer.Write((byte)(microsecondsPerQuarterNote >> 16));
                writer.Write((byte)(microsecondsPerQuarterNote >> 8));
                writer.Write((byte)microsecondsPerQuarterNote);

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates a time signature meta event.
        /// </summary>
        /// <param name="numerator">The time signature numerator.</param>
        /// <param name="denominator">The time signature denominator (as a power of 2).</param>
        /// <param name="clocksPerClick">The number of MIDI clocks per metronome click.</param>
        /// <param name="thirtySecondNotesPerBeat">The number of 32nd notes per 24 MIDI clocks.</param>
        /// <returns>A byte array containing the time signature meta event.</returns>
        public static byte[] CreateTimeSignature(byte numerator, byte denominator, byte clocksPerClick, byte thirtySecondNotesPerBeat)
        {
            return CreateTimeSignature(numerator, denominator, clocksPerClick, thirtySecondNotesPerBeat, 0);
        }

        /// <summary>
        /// Creates a time signature meta event with delta time.
        /// </summary>
        /// <param name="numerator">The time signature numerator.</param>
        /// <param name="denominator">The time signature denominator (as a power of 2).</param>
        /// <param name="clocksPerClick">The number of MIDI clocks per metronome click.</param>
        /// <param name="thirtySecondNotesPerBeat">The number of 32nd notes per 24 MIDI clocks.</param>
        /// <param name="deltaTime">The delta time in ticks.</param>
        /// <returns>A byte array containing the time signature meta event.</returns>
        public static byte[] CreateTimeSignature(byte numerator, byte denominator, byte clocksPerClick, byte thirtySecondNotesPerBeat, int deltaTime)
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write delta time
                WriteVariableLengthQuantity(writer, deltaTime);

                // Write meta event header
                writer.Write((byte)0xFF);
                writer.Write((byte)0x58);
                writer.Write((byte)0x04);

                // Write time signature data
                writer.Write(numerator);
                writer.Write(denominator);
                writer.Write(clocksPerClick);
                writer.Write(thirtySecondNotesPerBeat);

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates a track name meta event.
        /// </summary>
        /// <param name="name">The track name.</param>
        /// <returns>A byte array containing the track name meta event.</returns>
        public static byte[] CreateTrackName(string name)
        {
            return CreateTrackName(name, 0);
        }

        /// <summary>
        /// Creates a track name meta event with delta time.
        /// </summary>
        /// <param name="name">The track name.</param>
        /// <param name="deltaTime">The delta time in ticks.</param>
        /// <returns>A byte array containing the track name meta event.</returns>
        public static byte[] CreateTrackName(string name, int deltaTime)
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write delta time
                WriteVariableLengthQuantity(writer, deltaTime);

                // Write meta event header
                writer.Write((byte)0xFF);
                writer.Write((byte)0x03);

                // Write name length and data
                byte[] nameBytes = Encoding.ASCII.GetBytes(name);
                WriteVariableLengthQuantity(writer, nameBytes.Length);
                writer.Write(nameBytes);

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates a meta event.
        /// </summary>
        /// <param name="type">The meta event type.</param>
        /// <param name="data">The meta event data.</param>
        /// <returns>A byte array containing the meta event.</returns>
        public static byte[] CreateMetaEvent(byte type, byte[] data)
        {
            return CreateMetaEvent(type, data, 0);
        }

        /// <summary>
        /// Creates a meta event with delta time.
        /// </summary>
        /// <param name="type">The meta event type.</param>
        /// <param name="data">The meta event data.</param>
        /// <param name="deltaTime">The delta time in ticks.</param>
        /// <returns>A byte array containing the meta event.</returns>
        public static byte[] CreateMetaEvent(byte type, byte[] data, int deltaTime)
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write delta time
                WriteVariableLengthQuantity(writer, deltaTime);

                // Write meta event header
                writer.Write((byte)0xFF);
                writer.Write(type);

                // Write data length and data
                WriteVariableLengthQuantity(writer, data.Length);
                writer.Write(data);

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates an end of track meta event.
        /// </summary>
        /// <returns>A byte array containing the end of track meta event.</returns>
        public static byte[] CreateEndOfTrack()
        {
            return CreateEndOfTrack(0);
        }

        /// <summary>
        /// Creates an end of track meta event with delta time.
        /// </summary>
        /// <param name="deltaTime">The delta time in ticks.</param>
        /// <returns>A byte array containing the end of track meta event.</returns>
        public static byte[] CreateEndOfTrack(int deltaTime)
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write delta time
                WriteVariableLengthQuantity(writer, deltaTime);

                // Write meta event header
                writer.Write((byte)0xFF);
                writer.Write((byte)0x2F);
                writer.Write((byte)0x00);

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Writes a variable length quantity to the stream.
        /// </summary>
        /// <param name="writer">The binary writer.</param>
        /// <param name="value">The value to write.</param>
        private static void WriteVariableLengthQuantity(BinaryWriter writer, int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Value must be non-negative.");
            }

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
            else if (value < 0x10000000)
            {
                writer.Write((byte)(0x80 | (value >> 21)));
                writer.Write((byte)(0x80 | ((value >> 14) & 0x7F)));
                writer.Write((byte)(0x80 | ((value >> 7) & 0x7F)));
                writer.Write((byte)(value & 0x7F));
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Value is too large for a variable length quantity.");
            }
        }
    }
}