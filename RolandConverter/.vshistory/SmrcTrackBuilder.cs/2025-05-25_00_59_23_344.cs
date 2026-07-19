using System;
using System.IO;
using System.Collections.Generic;

namespace RolandConverter
{
    /// <summary>
    /// Provides methods for building S-MRC track data.
    /// </summary>
    public static class SmrcTrackBuilder
    {
        /// <summary>
        /// Creates a track with the specified MIDI messages.
        /// </summary>
        /// <param name="messages">The MIDI messages to include in the track.</param>
        /// <returns>A byte array containing the track data.</returns>
        public static byte[] CreateTrack(params byte[][] messages)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                // Calculate total length of all messages
                int totalLength = 0;
                foreach (var message in messages)
                {
                    totalLength += message.Length;
                }

                // Write track length (4 bytes)
                writer.Write(totalLength);

                // Write track data
                foreach (var message in messages)
                {
                    writer.Write(message);
                }

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates an empty track with only an end-of-track message.
        /// </summary>
        /// <returns>A byte array containing an empty track.</returns>
        public static byte[] CreateEmptyTrack()
        {
            return CreateTrack(SmrcMessageBuilder.CreateEndOfTrack());
        }

        /// <summary>
        /// Creates a track directory entry with the specified offset and length.
        /// </summary>
        /// <param name="offset">The offset to the track data.</param>
        /// <param name="length">The length of the track data.</param>
        /// <returns>A byte array containing the track directory entry.</returns>
        public static byte[] CreateTrackDirectoryEntry(int offset, int length)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                // Write track offset (4 bytes)
                writer.Write(offset);

                // Write track length (4 bytes)
                writer.Write(length);

                // Write reserved bytes (2 bytes)
                writer.Write(new byte[2]);

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates track data for a single note.
        /// </summary>
        /// <param name="note">The note value (0-127).</param>
        /// <param name="velocity">The velocity value (0-127).</param>
        /// <param name="duration">The duration in ticks.</param>
        /// <returns>A byte array containing the track data.</returns>
        public static byte[] CreateNoteTrackData(byte note, byte velocity, int duration)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                // Write note on message
                writer.Write(SmrcMessageBuilder.CreateNoteOn(note, velocity));

                // Write duration
                WriteVariableLengthQuantity(writer, duration);

                // Write note off message
                writer.Write(SmrcMessageBuilder.CreateNoteOff(note, 0));

                // Write end of track message
                writer.Write(SmrcMessageBuilder.CreateEndOfTrack());

                return stream.ToArray();
            }
        }

        private static void WriteVariableLengthQuantity(BinaryWriter writer, int value)
        {
            var buffer = new List<byte>();
            buffer.Add((byte)(value & 0x7F));

            while ((value >>= 7) > 0)
            {
                buffer.Insert(0, (byte)((value & 0x7F) | 0x80));
            }

            writer.Write(buffer.ToArray());
        }
    }
}