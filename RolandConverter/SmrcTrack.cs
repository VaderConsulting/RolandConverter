namespace RolandConverter
{
    /// <summary>
    /// Represents a track in an S-MRC file.
    /// </summary>
    public class SmrcTrack
    {
        /// <summary>
        /// Gets or sets the list of events in the track.
        /// </summary>
        public List<SmrcEvent> Events { get; set; } = new();
    }
}