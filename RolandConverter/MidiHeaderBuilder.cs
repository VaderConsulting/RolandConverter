using System.Text;

namespace RolandConverter
{
    /// <summary>
    /// Provides methods for building MIDI header chunks in a consistent way.
    /// </summary>
    public static class MidiHeaderBuilder
    {
        /// <summary>
        /// Creates a MIDI header chunk.
        /// </summary>
        /// <param name="format">The MIDI format (0, 1, or 2).</param>
        /// <param name="numTracks">The number of tracks.</param>
        /// <param name="ppqn">The pulses per quarter note.</param>
        /// <returns>A byte array containing the MIDI header chunk.</returns>
        public static byte[] CreateHeader(short format, short numTracks, short ppqn)
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write header chunk type
                writer.Write(Encoding.ASCII.GetBytes("MThd"));

                // Write header length (always 6)
                writer.Write(new byte[] { 0, 0, 0, 6 });

                // Write format
                writer.Write(new byte[] { (byte)(format >> 8), (byte)format });

                // Write number of tracks
                writer.Write(new byte[] { (byte)(numTracks >> 8), (byte)numTracks });

                // Write PPQN
                writer.Write(new byte[] { (byte)(ppqn >> 8), (byte)ppqn });

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates a MIDI header chunk with default values.
        /// </summary>
        /// <returns>A byte array containing the MIDI header chunk.</returns>
        public static byte[] CreateDefaultHeader()
        {
            return CreateHeader(1, 1, Constants.MidiFormat.DefaultPpqn);
        }

        /// <summary>
        /// Creates a MIDI header chunk for a multi-track file.
        /// </summary>
        /// <param name="numTracks">The number of tracks.</param>
        /// <returns>A byte array containing the MIDI header chunk.</returns>
        public static byte[] CreateMultiTrackHeader(short numTracks)
        {
            return CreateHeader(1, numTracks, Constants.MidiFormat.DefaultPpqn);
        }
    }
}