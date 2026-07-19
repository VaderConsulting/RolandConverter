namespace RolandConverter
{
    /// <summary>
    /// Represents a track in a MIDI file.
    /// </summary>
    public class MidiTrack
    {
        /// <summary>
        /// Gets or sets the raw MIDI track data.
        /// </summary>
        public byte[]? Data { get; set; }
    }
}