namespace RolandConverter
{
    /// <summary>
    /// Represents an entry in the track directory of an S-MRC file.
    /// </summary>
    public class TrackDirectoryEntry
    {
        /// <summary>
        /// Gets or sets the offset of the track data in the file.
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Gets or sets the length of the track data in bytes.
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// Gets or sets the start offset of the track data in the file.
        /// This is an alias for the Offset property for compatibility.
        /// </summary>
        public int StartOffset
        {
            get => Offset;
            set => Offset = value;
        }
    }
}