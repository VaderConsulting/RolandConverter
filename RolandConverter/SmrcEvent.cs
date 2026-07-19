namespace RolandConverter
{
    /// <summary>
    /// Represents a musical event in an S-MRC file.
    /// </summary>
    public class SmrcEvent
    {
        #region Properties

        /// <summary>
        /// Gets or sets the data bytes of the event.
        /// </summary>
        public byte[]? Data
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the delta time of the event in ticks.
        /// </summary>
        public int DeltaTime
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the Roland meta event data, if this is a Roland meta event.
        /// </summary>
        public byte[]? RolandMetaData
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the Roland meta event type, if this is a Roland meta event.
        /// </summary>
        public byte? RolandMetaType
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the status byte of the event.
        /// </summary>
        public byte Status
        {
            get; set;
        }

        #endregion
    }
}
