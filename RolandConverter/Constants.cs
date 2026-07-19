namespace RolandConverter
{
    /// <summary>
    /// Contains constants used throughout the Roland Converter application.
    /// </summary>
    public static class Constants
    {
        #region Classes

        /// <summary>
        /// Constants related to the MIDI file format.
        /// </summary>
        public static class MidiFormat
        {
            #region Constants

            /// <summary>
            /// The channel mask for MIDI status bytes.
            /// </summary>
            public const byte ChannelMask = 0x0F;

            /// <summary>
            /// The MIDI message type for Channel Pressure.
            /// </summary>
            public const byte ChannelPressureMessage = 0xD0;

            /// <summary>
            /// The number of clocks per metronome click in MIDI time signature events.
            /// </summary>
            public const byte ClocksPerMetronomeClick = 24;

            /// <summary>
            /// The default control number for MIDI control change messages.
            /// </summary>
            public const byte DefaultControl = 0x07; // Volume control

            /// <summary>
            /// The default MIDI note value (middle C).
            /// </summary>
            public const int DefaultNote = 0x3C;

            /// <summary>
            /// The default PPQN (Pulses Per Quarter Note) value.
            /// </summary>
            public const int DefaultPpqn = 96;

            /// <summary>
            /// The default tempo in BPM (Beats Per Minute).
            /// </summary>
            public const int DefaultTempo = 120;

            /// <summary>
            /// The default time signature denominator.
            /// </summary>
            public const int DefaultTimeSignatureDenominator = 2;

            /// <summary>
            /// The default time signature numerator.
            /// </summary>
            public const int DefaultTimeSignatureNumerator = 4;

            /// <summary>
            /// The default velocity for MIDI notes.
            /// </summary>
            public const int DefaultVelocity = 0x40;

            /// <summary>
            /// The MIDI message type for End of Track meta event.
            /// </summary>
            public const byte EndOfTrackMetaEvent = 0x2F;

            /// <summary>
            /// The size of the MIDI header chunk in bytes (including chunk type and length).
            /// </summary>
            public const int HeaderChunkSize = 14;

            /// <summary>
            /// The size of the MIDI header in bytes.
            /// </summary>
            public const int HeaderSize = 6;

            /// <summary>
            /// The maximum number of tracks allowed in a MIDI file.
            /// </summary>
            public const int MaxTracks = 65535;

            /// <summary>
            /// The minimum number of tracks required in a MIDI file.
            /// </summary>
            public const int MinTracks = 1;

            /// <summary>
            /// The maximum length of a MIDI track name in bytes.
            /// </summary>
            public const int MaxTrackNameLength = 31;

            /// <summary>
            /// The MIDI message type for Note Off.
            /// </summary>
            public const byte NoteOffMessage = 0x80;

            /// <summary>
            /// The MIDI message type for Note On.
            /// </summary>
            public const byte NoteOnMessage = 0x90;

            /// <summary>
            /// The status mask for MIDI status bytes.
            /// </summary>
            public const byte StatusMask = 0xF0;

            /// <summary>
            /// The MIDI message type for Tempo meta event.
            /// </summary>
            public const byte TempoMetaEvent = 0x51;

            /// <summary>
            /// The number of 32nd notes per quarter note in MIDI time signature events.
            /// </summary>
            public const byte ThirtySecondNotesPerQuarterNote = 8;

            /// <summary>
            /// The MIDI message type for Time Signature meta event.
            /// </summary>
            public const byte TimeSignatureMetaEvent = 0x58;

            /// <summary>
            /// The size of the MIDI track chunk header in bytes (including chunk type and length).
            /// </summary>
            public const int TrackChunkHeaderSize = 8;

            /// <summary>
            /// The MIDI message type for Track Name meta event.
            /// </summary>
            public const byte TrackNameMetaEvent = 0x03;

            /// <summary>
            /// The SMPTE time code format bit in the division field.
            /// </summary>
            public const int SmpteTimeCodeBit = 0x8000;

            #endregion

            #region Fields

            /// <summary>
            /// The MIDI chunk type for header chunk ("MThd").
            /// </summary>
            public static readonly byte[] HeaderChunkType = new byte[] { 0x4D, 0x54, 0x68, 0x64 };

            /// <summary>
            /// The MIDI chunk type for track chunk ("MTrk").
            /// </summary>
            public static readonly byte[] TrackChunkType = new byte[] { 0x4D, 0x54, 0x72, 0x6B };

            #endregion
        }

        /// <summary>
        /// Constants related to Roland-specific MIDI meta events.
        /// </summary>
        public static class RolandMeta
        {
            #region Constants

            /// <summary>
            /// The Roland meta event type for system exclusive end.
            /// </summary>
            public const byte SysExEnd = 0xF7;

            /// <summary>
            /// The Roland meta event type for tempo.
            /// </summary>
            public const byte TempoType = 0x01;

            /// <summary>
            /// The Roland meta event type for time signature.
            /// </summary>
            public const byte TimeSignatureType = 0x02;

            #endregion
        }

        /// <summary>
        /// Constants related to the S-MRC file format.
        /// </summary>
        public static class SmrcFormat
        {
            #region Constants

            /// <summary>
            /// The size of the S-MRC header in bytes.
            /// </summary>
            public const uint HeaderSize = 0xA8;  // 168 bytes

            /// <summary>
            /// The size of the title field in bytes.
            /// </summary>
            public const uint TitleSize = 32;     // 32 bytes for title (ASCII + NUL pad)

            /// <summary>
            /// The size of the reserved bytes in the header.
            /// </summary>
            public const uint HeaderReservedSize = 3;  // 3 bytes reserved in header

            /// <summary>
            /// The size of the track directory in bytes.
            /// </summary>
            public const uint TrackDirectorySize = 128;  // 8 × 16 bytes

            /// <summary>
            /// The size of the reserved bytes in each track directory entry.
            /// </summary>
            public const uint TrackDirectoryReservedSize = 8;  // 8 bytes reserved per entry

            /// <summary>
            /// The size of a track directory entry in bytes.
            /// </summary>
            public const uint TrackDirectoryEntrySize = 16;  // 16 bytes per entry

            /// <summary>
            /// The size of the track data field in bytes.
            /// </summary>
            public const uint TrackDataSize = 4;  // 4 bytes for track data size

            /// <summary>
            /// The offset to the first track data after the header.
            /// </summary>
            public const uint FirstTrackDataOffset = HeaderSize;  // Track data starts after header

            /// <summary>
            /// The default PPQN (Pulses Per Quarter Note) value.
            /// </summary>
            public const uint DefaultPpqn = 96;   // 96 clocks per quarter note

            /// <summary>
            /// The maximum number of tracks supported in an S-MRC file.
            /// </summary>
            public const uint MaxTracks = 8;      // 8 linear phrase tracks

            /// <summary>
            /// The default time signature numerator.
            /// </summary>
            public const byte DefaultTimeSignatureNumerator = 4;    // 4/4 time

            /// <summary>
            /// The default time signature denominator (power of 2).
            /// </summary>
            public const byte DefaultTimeSignatureDenominator = 2;  // 2 = quarter note

            /// <summary>
            /// The minimum time signature numerator.
            /// </summary>
            public const byte MinTimeSignatureNumerator = 1;

            /// <summary>
            /// The maximum time signature numerator.
            /// </summary>
            public const byte MaxTimeSignatureNumerator = 32;

            /// <summary>
            /// The minimum time signature denominator.
            /// </summary>
            public const byte MinTimeSignatureDenominator = 0;  // 0 = not set

            /// <summary>
            /// The maximum time signature denominator.
            /// </summary>
            public const byte MaxTimeSignatureDenominator = 4;  // 4 = 16th note

            /// <summary>
            /// The minimum tempo (BPM).
            /// </summary>
            public const byte MinTempo = 10;

            /// <summary>
            /// The maximum tempo (BPM).
            /// </summary>
            public const byte MaxTempo = 250;

            /// <summary>
            /// The default tempo (BPM).
            /// </summary>
            public const byte DefaultTempo = 120;

            /// <summary>
            /// The maximum size of track data in bytes.
            /// This is a practical limit based on floppy disk capacity.
            /// </summary>
            public const uint MaxTrackDataSize = 0x100000;  // 1MB per track

            /// <summary>
            /// The maximum number of events per track.
            /// </summary>
            public const uint MaxEventsPerTrack = 150000;

            /// <summary>
            /// The maximum number of measures per song.
            /// </summary>
            public const uint MaxMeasuresPerSong = 9999;

            /// <summary>
            /// The default note value for S-MRC (middle C).
            /// </summary>
            public const uint DefaultNote = 0x3C;

            /// <summary>
            /// The default velocity for S-MRC notes.
            /// </summary>
            public const uint DefaultVelocity = 0x40;

            #endregion
        }

        #endregion
    }
}
