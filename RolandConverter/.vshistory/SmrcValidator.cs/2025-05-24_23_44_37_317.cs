using System.Text;
using System.Collections.Generic;

namespace RolandConverter
{
    /// <summary>
    /// Provides functionality to validate S-MRC files.
    /// </summary>
    public static class SmrcValidator
    {
        #region Constants

        /// <summary>
        /// The size of the S-MRC header in bytes.
        /// </summary>
        private const int SMRC_HEADER_SIZE = 0xA8;

        /// <summary>
        /// The default PPQN (Pulses Per Quarter Note) value for S-MRC files.
        /// </summary>
        private const int SMRC_PPQN = 96;

        /// <summary>
        /// The maximum number of tracks supported in an S-MRC file.
        /// </summary>
        private const int SMRC_TRACK_COUNT = 8;

        #endregion

        #region Public Methods

        /// <summary>
        /// Validates a MIDI file stream.
        /// </summary>
        /// <param name="stream">The stream containing the MIDI file data.</param>
        /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the stream is not readable.</exception>
        /// <exception cref="InvalidDataException">Thrown when the file is not a valid MIDI file.</exception>
        public static void ValidateMidiFile(Stream stream)
        {
            MidiValidator.ValidateMidiFile(stream);
        }

        /// <summary>
        /// Validates an S-MRC file stream.
        /// </summary>
        /// <param name="stream">The stream containing the S-MRC file data.</param>
        /// <exception cref="InvalidDataException">Thrown when the file is not a valid S-MRC file.</exception>
        public static void ValidateSmrcFile(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream must be readable", nameof(stream));
            }

            if (stream.Length < SMRC_HEADER_SIZE)
            {
                throw new InvalidDataException($"File too small. S-MRC files must be at least {SMRC_HEADER_SIZE} bytes");
            }

            long originalPosition = stream.Position;
            try
            {
                using (BinaryReader reader = new BinaryReader(stream, Encoding.ASCII, true))
                {
                    // Skip title
                    _ = reader.ReadBytes(32);

                    // Check PPQN
                    short ppqn = reader.ReadInt16();
                    if (ppqn != SMRC_PPQN)
                    {
                        throw new InvalidDataException($"Invalid PPQN. Expected {SMRC_PPQN}, got {ppqn}");
                    }

                    // Validate time signature
                    byte numerator = reader.ReadByte();
                    byte denominator = reader.ReadByte();
                    if (denominator > 6) // Max 2^6 = 64
                    {
                        throw new InvalidDataException($"Invalid time signature denominator power: {denominator}");
                    }

                    // Validate tempo
                    byte tempo = reader.ReadByte();
                    if (tempo is <= 0 or > 255)
                    {
                        throw new InvalidDataException($"Invalid tempo hint: {tempo}. Expected value between 1 and 255.");
                    }

                    // Skip reserved
                    _ = reader.ReadBytes(3);

                    // Validate track directory
                    int totalTrackLength = 0;
                    var trackRanges = new List<(int start, int end)>();
                    for (int i = 0; i < SMRC_TRACK_COUNT; i++)
                    {
                        int offset = reader.ReadInt32();
                        int length = reader.ReadInt32();
                        _ = reader.ReadBytes(8); // Reserved

                        if (length < 0)
                        {
                            throw new InvalidDataException($"Invalid track {i} length: {length}");
                        }

                        if (offset <= SMRC_HEADER_SIZE)
                        {
                            throw new InvalidDataException($"Track {i} offset {offset} overlaps header");
                        }

                        // Check for overlaps with existing tracks
                        int trackEnd = offset + length;
                        foreach (var (start, end) in trackRanges)
                        {
                            if (offset < end && trackEnd > start)
                            {
                                throw new InvalidDataException($"Track {i} overlaps with another track");
                            }
                        }

                        trackRanges.Add((offset, trackEnd));
                        totalTrackLength += length;
                    }

                    // Verify file size
                    if (stream.Length < SMRC_HEADER_SIZE + totalTrackLength)
                    {
                        throw new InvalidDataException("File too small for declared track data");
                    }
                }
            }
            finally
            {
                stream.Position = originalPosition;
            }
        }

        #endregion
    }
}
