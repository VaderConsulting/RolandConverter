namespace RolandConverter
{
    /// <summary>
    /// Contains constants used throughout the Roland Converter application.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Constants related to the S-MRC file format.
        /// </summary>
        public static class SmrcFormat
        {
            /// <summary>
            /// The size of the title field in bytes.
            /// </summary>
            public const int TitleSize = 32;

            /// <summary>
            /// The size of the reserved field in the header in bytes.
            /// </summary>
            public const int HeaderReservedSize = 3;

            /// <summary>
            /// The size of the reserved field in the track directory in bytes.
            /// </summary>
            public const int TrackDirectoryReservedSize = 8;

            /// <summary>
            /// The size of each track directory entry in bytes.
            /// </summary>
            public const int TrackDirectoryEntrySize = 10;

            /// <summary>
            /// The offset to the first track data after the header.
            /// </summary>
            public const int FirstTrackDataOffset = 0xA9;

            /// <summary>
            /// The maximum number of tracks allowed in an S-MRC file.
            /// </summary>
            public const int MaxTracks = 8;

            /// <summary>
            /// The maximum size of track data in bytes (1MB).
            /// </summary>
            public const int MaxTrackDataSize = 1024 * 1024;
        }

        /// <summary>
        /// Constants related to the MIDI file format.
        /// </summary>
        public static class MidiFormat
        {
            /// <summary>
            /// The size of the MIDI header in bytes.
            /// </summary>
            public const int HeaderSize = 6;

            /// <summary>
            /// The size of the MIDI header chunk in bytes (including chunk type and length).
            /// </summary>
            public const int HeaderChunkSize = 14;

            /// <summary>
            /// The size of the MIDI track chunk header in bytes (including chunk type and length).
            /// </summary>
            public const int TrackChunkHeaderSize = 8;

            /// <summary>
            /// The default PPQN (Pulses Per Quarter Note) value.
            /// </summary>
            public const int DefaultPpqn = 96;

            /// <summary>
            /// The default tempo in BPM (Beats Per Minute).
            /// </summary>
            public const int DefaultTempo = 120;

            /// <summary>
            /// The default time signature numerator.
            /// </summary>
            public const int DefaultTimeSignatureNumerator = 4;

            /// <summary>
            /// The default time signature denominator.
            /// </summary>
            public const int DefaultTimeSignatureDenominator = 2;

            /// <summary>
            /// The default velocity for MIDI notes.
            /// </summary>
            public const int DefaultVelocity = 0x40;

            /// <summary>
            /// The default MIDI note value (middle C).
            /// </summary>
            public const int DefaultNote = 0x3C;

            /// <summary>
            /// The MIDI message type for Note On.
            /// </summary>
            public const byte NoteOnMessage = 0x90;

            /// <summary>
            /// The MIDI message type for Note Off.
            /// </summary>
            public const byte NoteOffMessage = 0x80;

            /// <summary>
            /// The MIDI message type for Channel Pressure.
            /// </summary>
            public const byte ChannelPressureMessage = 0xA0;

            /// <summary>
            /// The MIDI message type for End of Track meta event.
            /// </summary>
            public const byte EndOfTrackMetaEvent = 0x2F;

            /// <summary>
            /// The MIDI message type for Track Name meta event.
            /// </summary>
            public const byte TrackNameMetaEvent = 0x03;

            /// <summary>
            /// The MIDI message type for Tempo meta event.
            /// </summary>
            public const byte TempoMetaEvent = 0x51;

            /// <summary>
            /// The MIDI message type for Time Signature meta event.
            /// </summary>
            public const byte TimeSignatureMetaEvent = 0x58;

            /// <summary>
            /// The MIDI chunk type for header chunk ("MThd").
            /// </summary>
            public static readonly byte[] HeaderChunkType = new byte[] { 0x4D, 0x54, 0x68, 0x64 };

            /// <summary>
            /// The MIDI chunk type for track chunk ("MTrk").
            /// </summary>
            public static readonly byte[] TrackChunkType = new byte[] { 0x4D, 0x54, 0x72, 0x6B };

            /// <summary>
            /// The number of clocks per metronome click in MIDI time signature events.
            /// </summary>
            public const byte ClocksPerMetronomeClick = 24;

            /// <summary>
            /// The number of 32nd notes per quarter note in MIDI time signature events.
            /// </summary>
            public const byte ThirtySecondNotesPerQuarterNote = 8;

            /// <summary>
            /// The maximum length of a MIDI track name in bytes.
            /// </summary>
            public const int MaxTrackNameLength = 31;

            /// <summary>
            /// The channel mask for MIDI status bytes.
            /// </summary>
            public const byte ChannelMask = 0x0F;

            /// <summary>
            /// The status mask for MIDI status bytes.
            /// </summary>
            public const byte StatusMask = 0xF0;
        }

        /// <summary>
        /// Constants related to Roland-specific MIDI meta events.
        /// </summary>
        public static class RolandMeta
        {
            /// <summary>
            /// The Roland meta event type for tempo.
            /// </summary>
            public const byte TempoType = 0x01;

            /// <summary>
            /// The Roland meta event type for time signature.
            /// </summary>
            public const byte TimeSignatureType = 0x02;

            /// <summary>
            /// The Roland meta event type for system exclusive end.
            /// </summary>
            public const byte SysExEnd = 0xF7;
        }
    }
}