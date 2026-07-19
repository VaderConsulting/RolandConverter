using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

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
            if (messages.Length > Constants.SmrcFormat.MaxEventsPerTrack)
            {
                throw new ArgumentException($"Track exceeds maximum event count of {Constants.SmrcFormat.MaxEventsPerTrack}");
            }

            TraceLogger.Log($"Creating track with {messages.Length} messages");

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Write track data with delta times
                for (int i = 0; i < messages.Length; i++)
                {
                    // Add delta time of 0 before each message
                    writer.Write((byte)0);
                    // If this is the End-of-Track message, add an extra delta time byte
                    if (messages[i].Length == 3 && messages[i][0] == 0xFF && messages[i][1] == Constants.MidiFormat.EndOfTrackMetaEvent)
                    {
                        writer.Write((byte)0);
                    }
                    writer.Write(messages[i]);
                }

                byte[] result = stream.ToArray();
                if (result.Length > Constants.SmrcFormat.MaxTrackDataSize)
                {
                    throw new ArgumentException($"Track exceeds maximum size of {Constants.SmrcFormat.MaxTrackDataSize} bytes");
                }

                TraceLogger.Log($"Final track size: {result.Length} bytes");
                return result;
            }
        }

        /// <summary>
        /// Creates an empty track with only an end-of-track message.
        /// </summary>
        /// <returns>A byte array containing an empty track.</returns>
        public static byte[] CreateEmptyTrack()
        {
            TraceLogger.Log("Creating empty track");
            return CreateTrack(SmrcMessageBuilder.CreateEndOfTrack());
        }

        /// <summary>
        /// Creates a track directory entry with the specified offset and length.
        /// </summary>
        /// <param name="startOffset">The start offset to the track data.</param>
        /// <param name="trackLength">The length of the track data.</param>
        /// <returns>A byte array containing the track directory entry.</returns>
        public static byte[] CreateTrackDirectoryEntry(uint startOffset, uint trackLength)
        {
            TraceLogger.Log($"Creating track directory entry: startOffset={startOffset}, trackLength={trackLength}");

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Write start offset (4 bytes)
                writer.Write(startOffset);
                TraceLogger.Log($"Wrote track start offset: {startOffset}");

                // Write track length (4 bytes)
                writer.Write(trackLength);
                TraceLogger.Log($"Wrote track length: {trackLength} bytes");

                // Write reserved bytes (8 bytes)
                writer.Write(new byte[8]);

                byte[] result = stream.ToArray();
                TraceLogger.Log($"Final directory entry size: {result.Length} bytes");
                return result;
            }
        }

        /// <summary>
        /// Creates track data for a single note.
        /// </summary>
        /// <param name="note">The note value (0-127).</param>
        /// <param name="velocity">The velocity value (0-127).</param>
        /// <param name="duration">The duration in ticks.</param>
        /// <returns>A byte array containing the track data.</returns>
        public static byte[] CreateNoteTrackData(byte note, byte velocity, uint duration)
        {
            TraceLogger.Log($"Creating note track data: note={note}, velocity={velocity}, duration={duration}");

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Write note on message
                writer.Write(SmrcMessageBuilder.CreateNoteOn(note, velocity));
                TraceLogger.Log("Wrote note on message");

                // Write duration
                WriteVariableLengthQuantity(writer, duration);
                TraceLogger.Log($"Wrote duration: {duration}");

                // Write note off message
                writer.Write(SmrcMessageBuilder.CreateNoteOff(note, 0));
                TraceLogger.Log("Wrote note off message");

                // Write end of track message
                writer.Write(SmrcMessageBuilder.CreateEndOfTrack());
                TraceLogger.Log("Wrote end of track marker");

                byte[] result = stream.ToArray();
                TraceLogger.Log($"Final note track data size: {result.Length} bytes");
                return result;
            }
        }

        private static void WriteVariableLengthQuantity(BinaryWriter writer, uint value)
        {
            List<byte> buffer = new List<byte>();
            buffer.Add((byte)(value & 0x7F));

            while ((value >>= 7) > 0)
            {
                buffer.Insert(0, (byte)((value & 0x7F) | 0x80));
            }

            writer.Write(buffer.ToArray());
        }
    }
}