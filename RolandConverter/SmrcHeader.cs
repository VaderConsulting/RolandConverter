namespace RolandConverter
{
    /// <summary>
    /// Represents the header information of an S-MRC file.
    /// </summary>
    public class SmrcHeader
    {
        /// <summary>
        /// Gets or sets the title of the song.
        /// </summary>
        public string Title
        {
            get; set;
        }
        /// <summary>
        /// Gets or sets the pulses per quarter note (PPQN) value.
        /// </summary>
        public Int16 Ppqn
        {
            get; set;
        }
        /// <summary>
        /// Gets or sets the numerator of the time signature.
        /// </summary>
        public Byte TimeSignatureNumerator
        {
            get; set;
        }
        /// <summary>
        /// Gets or sets the denominator of the time signature (as a power of 2).
        /// </summary>
        public Byte TimeSignatureDenominator
        {
            get; set;
        }
        /// <summary>
        /// Gets or sets the tempo in beats per minute.
        /// </summary>
        public Byte TempoBpm
        {
            get; set;
        }
        /// <summary>
        /// Gets or sets the list of track directory entries.
        /// </summary>
        public TrackDirectoryEntry[] TrackDirectory
        {
            get; set;
        }
    }
}