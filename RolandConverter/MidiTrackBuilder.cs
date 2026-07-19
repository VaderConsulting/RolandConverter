using System.Text;

namespace RolandConverter
{
    /// <summary>
    /// Provides methods for building MIDI track chunks in a consistent way.
    /// </summary>
    public static class MidiTrackBuilder
    {
        /// <summary>
        /// Creates a MIDI track chunk with the specified messages.
        /// </summary>
        /// <param name="messages">The MIDI messages to include in the track.</param>
        /// <returns>A byte array containing the MIDI track chunk.</returns>
        public static byte[] CreateTrack(params byte[][] messages)
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write track chunk type
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));

                // Calculate track data length
                int trackDataLength = messages.Sum(msg => msg.Length);

                // Write track length (placeholder)
                long lengthPosition = writer.BaseStream.Position;
                writer.Write(new byte[] { 0, 0, 0, 0 });

                // Write track data
                foreach (byte[] message in messages)
                {
                    writer.Write(message);
                }

                // Update track length
                long endPosition = writer.BaseStream.Position;
                writer.BaseStream.Position = lengthPosition;
                writer.Write(new byte[] {
                    (byte)(trackDataLength >> 24),
                    (byte)(trackDataLength >> 16),
                    (byte)(trackDataLength >> 8),
                    (byte)trackDataLength
                });

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates an empty MIDI track chunk.
        /// </summary>
        /// <returns>A byte array containing an empty MIDI track chunk.</returns>
        public static byte[] CreateEmptyTrack()
        {
            return CreateTrack(MidiMessageBuilder.CreateEndOfTrack());
        }
    }
}
