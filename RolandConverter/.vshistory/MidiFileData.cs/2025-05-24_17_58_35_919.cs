namespace RolandConverter
{
    /// <summary>
    /// Represents the data structure of a MIDI file.
    /// </summary>
    public class MidiFileData
    {
        /// <summary>
        /// Gets or sets the format of the MIDI file (0 or 1).
        /// </summary>
        public short Format
        {
            get; set;
        }
        /// <summary>
        /// Gets or sets the number of tracks in the MIDI file.
        /// </summary>
        public short TrackCount
        {
            get
            {
                return (short)Tracks.Count;
            }
        }

        /// <summary>
        /// Gets or sets the time division of the MIDI file.
        /// </summary>
        public short Division
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the list of tracks in the MIDI file.
        /// </summary>
        public List<MidiTrack> Tracks { get; set; } = new List<MidiTrack>();
    }
}