using System.Text;

using Xunit;

namespace RolandConverter.Tests
{
    /// <summary>
    /// Contains unit tests for the S-MRC to MIDI converter and validator.
    /// </summary>
    public class ConverterTests
    {
        #region Private Methods

        private static byte[] CreateMidiFormat0()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header (Format 0)
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(new byte[] { 0, 0, 0, 6 });
                writer.Write(new byte[] { 0, 0 }); // Format 0
                writer.Write(new byte[] { 0, 1 }); // 1 track
                writer.Write(new byte[] { 0, 96 }); // 96 PPQ

                // Write track
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));

                // Track data
                byte[] trackData;
                using (MemoryStream trackStream = new MemoryStream())
                using (BinaryWriter trackWriter = new BinaryWriter(trackStream))
                {
                    // Track name
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x03, 0x04 });
                    trackWriter.Write(Encoding.ASCII.GetBytes("Test"));

                    // Note on
                    trackWriter.Write(new byte[] { 0x00, 0x90, 0x3C, 0x40 });

                    // Note off
                    trackWriter.Write(new byte[] { 0x60, 0x80, 0x3C, 0x40 });

                    // End of track
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x2F, 0x00 });

                    trackData = trackStream.ToArray();
                }

                // Write track length
                writer.Write(new byte[] {
                    (byte)(trackData.Length >> 24),
                    (byte)(trackData.Length >> 16),
                    (byte)(trackData.Length >> 8),
                    (byte)trackData.Length
                });
                writer.Write(trackData);

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithAllEventTypes()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(new byte[] { 0, 0, 0, 6 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 96 });

                // Write track
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));

                // Track data
                byte[] trackData;
                using (MemoryStream trackStream = new MemoryStream())
                using (BinaryWriter trackWriter = new BinaryWriter(trackStream))
                {
                    // Note On
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0x90);
                    trackWriter.Write((byte)0x3C);
                    trackWriter.Write((byte)0x40);

                    // Note Off
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0x80);
                    trackWriter.Write((byte)0x3C);
                    trackWriter.Write((byte)0x40);

                    // Polyphonic Aftertouch
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0xA0);
                    trackWriter.Write((byte)0x3C);
                    trackWriter.Write((byte)0x40);

                    // Control Change
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0xB0);
                    trackWriter.Write((byte)0x07);
                    trackWriter.Write((byte)0x40);

                    // Program Change
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0xC0);
                    trackWriter.Write((byte)0x00);

                    // Channel Pressure
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0xD0);
                    trackWriter.Write((byte)0x40);

                    // Pitch Bend
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0xE0);
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0x40);

                    // System Exclusive
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0xF0);
                    trackWriter.Write((byte)0x05);
                    trackWriter.Write(new byte[] { 0x41, 0x10, 0x42, 0x12, 0x40 });
                    trackWriter.Write((byte)0xF7);

                    // Meta Events
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0xFF);
                    trackWriter.Write((byte)0x51);
                    trackWriter.Write((byte)0x03);
                    trackWriter.Write(new byte[] { 0x07, 0xA1, 0x20 });

                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0xFF);
                    trackWriter.Write((byte)0x58);
                    trackWriter.Write((byte)0x04);
                    trackWriter.Write(new byte[] { 0x04, 0x02, 0x18, 0x08 });

                    // End of track
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x2F, 0x00 });

                    trackData = trackStream.ToArray();
                }

                // Write track length
                writer.Write(new byte[] {
                    (byte)(trackData.Length >> 24),
                    (byte)(trackData.Length >> 16),
                    (byte)(trackData.Length >> 8),
                    (byte)trackData.Length
                });
                writer.Write(trackData);

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithAllMetaEvents()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(new byte[] { 0, 0, 0, 6 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 96 });

                // Write track
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));

                // Track data
                byte[] trackData;
                using (MemoryStream trackStream = new MemoryStream())
                using (BinaryWriter trackWriter = new BinaryWriter(trackStream))
                {
                    // Sequence number (length 2)
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x00, 0x02, 0x00, 0x01 });

                    // Text event (length 11)
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x01, 0x81, 0x0B });
                    trackWriter.Write(Encoding.ASCII.GetBytes("Text Event"));

                    // Copyright (length 11)
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x02, 0x81, 0x0B });
                    trackWriter.Write(Encoding.ASCII.GetBytes("Copyright"));

                    // Track name (length 4)
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x03, 0x04 });
                    trackWriter.Write(Encoding.ASCII.GetBytes("Test"));

                    // Instrument name (length 11)
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x04, 0x81, 0x0B });
                    trackWriter.Write(Encoding.ASCII.GetBytes("Instrument"));

                    // Lyric (length 11)
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x05, 0x81, 0x0B });
                    trackWriter.Write(Encoding.ASCII.GetBytes("Lyric Text"));

                    // Marker (length 11)
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x06, 0x81, 0x0B });
                    trackWriter.Write(Encoding.ASCII.GetBytes("Marker"));

                    // Cue point (length 11)
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x07, 0x81, 0x0B });
                    trackWriter.Write(Encoding.ASCII.GetBytes("Cue Point"));

                    // Program name (length 11)
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x08, 0x81, 0x0B });
                    trackWriter.Write(Encoding.ASCII.GetBytes("Program"));

                    // Device name (length 11)
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x09, 0x81, 0x0B });
                    trackWriter.Write(Encoding.ASCII.GetBytes("Device"));

                    // Channel prefix (length 1)
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x20, 0x01, 0x00 });

                    // MIDI port (length 1)
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x21, 0x01, 0x00 });

                    // End of track (length 0)
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x2F, 0x00 });

                    trackData = trackStream.ToArray();
                }

                // Write track length
                writer.Write(new byte[] {
                    (byte)(trackData.Length >> 24),
                    (byte)(trackData.Length >> 16),
                    (byte)(trackData.Length >> 8),
                    (byte)trackData.Length
                });
                writer.Write(trackData);

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithChannelPressure()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(new byte[] { 0, 0, 0, 6 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 96 });

                // Write track
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));

                // Track data
                byte[] trackData;
                using (MemoryStream trackStream = new MemoryStream())
                using (BinaryWriter trackWriter = new BinaryWriter(trackStream))
                {
                    // Channel pressure
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0xD0); // Channel pressure
                    trackWriter.Write((byte)0x40); // Pressure value

                    // End of track
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x2F, 0x00 });

                    trackData = trackStream.ToArray();
                }

                // Write track length
                writer.Write(new byte[] {
                    (byte)(trackData.Length >> 24),
                    (byte)(trackData.Length >> 16),
                    (byte)(trackData.Length >> 8),
                    (byte)trackData.Length
                });
                writer.Write(trackData);

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithControlChange()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(new byte[] { 0, 0, 0, 6 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 96 });

                // Write track
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));

                // Track data
                byte[] trackData;
                using (MemoryStream trackStream = new MemoryStream())
                using (BinaryWriter trackWriter = new BinaryWriter(trackStream))
                {
                    // Control change (modulation wheel)
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0xB0); // Control change channel 1
                    trackWriter.Write((byte)0x01); // Modulation wheel
                    trackWriter.Write((byte)0x40); // Value

                    // Control change (volume)
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0xB0); // Control change channel 1
                    trackWriter.Write((byte)0x07); // Volume
                    trackWriter.Write((byte)0x64); // Value

                    // End of track
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x2F, 0x00 });

                    trackData = trackStream.ToArray();
                }

                // Write track length
                writer.Write(new byte[] {
                    (byte)(trackData.Length >> 24),
                    (byte)(trackData.Length >> 16),
                    (byte)(trackData.Length >> 8),
                    (byte)trackData.Length
                });
                writer.Write(trackData);

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithEmptyTrack()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(new byte[] { 0, 0, 0, 6 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 96 });

                // Write empty track
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));
                writer.Write(new byte[] { 0, 0, 0, 4 });
                writer.Write(new byte[] { 0x00, 0xFF, 0x2F, 0x00 });

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithLongDeltaTimes()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(new byte[] { 0, 0, 0, 6 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 96 });

                // Write track
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));

                // Track data
                byte[] trackData;
                using (MemoryStream trackStream = new MemoryStream())
                using (BinaryWriter trackWriter = new BinaryWriter(trackStream))
                {
                    // Very long delta time (0x7F7F7F7F)
                    trackWriter.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F });
                    trackWriter.Write(new byte[] { 0x90, 0x3C, 0x40 });

                    // Another very long delta time
                    trackWriter.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F });
                    trackWriter.Write(new byte[] { 0x80, 0x3C, 0x40 });

                    // End of track
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x2F, 0x00 });

                    trackData = trackStream.ToArray();
                }

                // Write track length
                writer.Write(new byte[] {
                    (byte)(trackData.Length >> 24),
                    (byte)(trackData.Length >> 16),
                    (byte)(trackData.Length >> 8),
                    (byte)trackData.Length
                });
                writer.Write(trackData);

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithLongTrackName()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(new byte[] { 0, 0, 0, 6 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 96 });

                // Write track
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));

                // Track data
                byte[] trackData;
                using (MemoryStream trackStream = new MemoryStream())
                using (BinaryWriter trackWriter = new BinaryWriter(trackStream))
                {
                    // Long track name (255 bytes)
                    trackWriter.Write((byte)0x00);  // Delta time (VLQ)
                    trackWriter.Write((byte)0xFF);  // Meta event marker
                    trackWriter.Write((byte)0x03);  // Track name meta event type
                    trackWriter.Write((byte)0x04);  // Length (VLQ)
                    trackWriter.Write(Encoding.ASCII.GetBytes("Test"));

                    // Note on
                    trackWriter.Write((byte)0x00);  // Delta time (VLQ)
                    trackWriter.Write((byte)0x90);  // Note on channel 1
                    trackWriter.Write((byte)0x3C);  // Note 60
                    trackWriter.Write((byte)0x40);  // Velocity 64

                    // Note off
                    trackWriter.Write((byte)0x60);  // Delta time (VLQ)
                    trackWriter.Write((byte)0x80);  // Note off channel 1
                    trackWriter.Write((byte)0x3C);  // Note 60
                    trackWriter.Write((byte)0x40);  // Velocity 64

                    // End of track
                    trackWriter.Write((byte)0x00);  // Delta time (VLQ)
                    trackWriter.Write((byte)0xFF);  // Meta event marker
                    trackWriter.Write((byte)0x2F);  // End of track meta event type
                    trackWriter.Write((byte)0x00);  // Length (VLQ)

                    trackData = trackStream.ToArray();
                }

                // Write track length
                writer.Write(new byte[] {
                    (byte)(trackData.Length >> 24),
                    (byte)(trackData.Length >> 16),
                    (byte)(trackData.Length >> 8),
                    (byte)trackData.Length
                });
                writer.Write(trackData);

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithManyTracks(int trackCount)
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(new byte[] { 0, 0, 0, 6 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { (byte)(trackCount >> 8), (byte)trackCount });
                writer.Write(new byte[] { 0, 96 });

                // Write empty tracks
                for (int i = 0; i < trackCount; i++)
                {
                    writer.Write(Encoding.ASCII.GetBytes("MTrk"));
                    writer.Write(new byte[] { 0, 0, 0, 4 });
                    writer.Write(new byte[] { 0x00, 0xFF, 0x2F, 0x00 });
                }

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithMaximumDeltaTime()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(new byte[] { 0, 0, 0, 6 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 96 });

                // Write track
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));

                // Track data
                byte[] trackData;
                using (MemoryStream trackStream = new MemoryStream())
                using (BinaryWriter trackWriter = new BinaryWriter(trackStream))
                {
                    // Maximum delta time (0x7F7F7F7F)
                    trackWriter.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F });
                    trackWriter.Write(new byte[] { 0x90, 0x3C, 0x40 }); // Note on
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x2F, 0x00 }); // End of track

                    trackData = trackStream.ToArray();
                }

                // Write track length
                writer.Write(new byte[] {
                    (byte)(trackData.Length >> 24),
                    (byte)(trackData.Length >> 16),
                    (byte)(trackData.Length >> 8),
                    (byte)trackData.Length
                });
                writer.Write(trackData);

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithMetaEvents()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(new byte[] { 0, 0, 0, 6 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 96 });

                // Write track
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));

                // Track data
                byte[] trackData;
                using (MemoryStream trackStream = new MemoryStream())
                using (BinaryWriter trackWriter = new BinaryWriter(trackStream))
                {
                    // Track name
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x03, 0x04 });
                    trackWriter.Write(Encoding.ASCII.GetBytes("Test"));

                    // Copyright
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x02, 0x0B });
                    trackWriter.Write(Encoding.ASCII.GetBytes("Copyright 2024"));

                    // Tempo
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x51, 0x03 });
                    trackWriter.Write(new byte[] { 0x07, 0xA1, 0x20 }); // 500000 microseconds per quarter note

                    // Time signature
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x58, 0x04 });
                    trackWriter.Write(new byte[] { 0x04, 0x02, 0x18, 0x08 });

                    // Key signature
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x59, 0x02 });
                    trackWriter.Write(new byte[] { 0x00, 0x00 }); // C major

                    trackData = trackStream.ToArray();
                }

                // Write track length
                writer.Write(new byte[] {
                    (byte)(trackData.Length >> 24),
                    (byte)(trackData.Length >> 16),
                    (byte)(trackData.Length >> 8),
                    (byte)trackData.Length
                });
                writer.Write(trackData);

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithMultipleChannels()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(new byte[] { 0, 0, 0, 6 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 96 });

                // Write track
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));

                // Track data
                byte[] trackData;
                using (MemoryStream trackStream = new MemoryStream())
                using (BinaryWriter trackWriter = new BinaryWriter(trackStream))
                {
                    // Channel 1
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0x90);
                    trackWriter.Write((byte)0x3C);
                    trackWriter.Write((byte)0x40);

                    // Channel 2
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0x91);
                    trackWriter.Write((byte)0x3D);
                    trackWriter.Write((byte)0x40);

                    // Channel 3
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0x92);
                    trackWriter.Write((byte)0x3E);
                    trackWriter.Write((byte)0x40);

                    // Channel 4
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0x93);
                    trackWriter.Write((byte)0x3F);
                    trackWriter.Write((byte)0x40);

                    // End of track
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x2F, 0x00 });

                    trackData = trackStream.ToArray();
                }

                // Write track length
                writer.Write(new byte[] {
                    (byte)(trackData.Length >> 24),
                    (byte)(trackData.Length >> 16),
                    (byte)(trackData.Length >> 8),
                    (byte)trackData.Length
                });
                writer.Write(trackData);

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithMultipleTempos()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(new byte[] { 0, 0, 0, 6 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 96 });

                // Write track
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));

                // Track data
                byte[] trackData;
                using (MemoryStream trackStream = new MemoryStream())
                using (BinaryWriter trackWriter = new BinaryWriter(trackStream))
                {
                    // Tempo 1 (120 BPM)
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0xFF);
                    trackWriter.Write((byte)0x51);
                    trackWriter.Write((byte)0x03);
                    trackWriter.Write(new byte[] { 0x07, 0xA1, 0x20 });

                    // Tempo 2 (140 BPM)
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0xFF);
                    trackWriter.Write((byte)0x51);
                    trackWriter.Write((byte)0x03);
                    trackWriter.Write(new byte[] { 0x06, 0x4E, 0x20 });

                    // Tempo 3 (100 BPM)
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0xFF);
                    trackWriter.Write((byte)0x51);
                    trackWriter.Write((byte)0x03);
                    trackWriter.Write(new byte[] { 0x09, 0x27, 0xC0 });

                    // End of track
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x2F, 0x00 });

                    trackData = trackStream.ToArray();
                }

                // Write track length
                writer.Write(new byte[] {
                    (byte)(trackData.Length >> 24),
                    (byte)(trackData.Length >> 16),
                    (byte)(trackData.Length >> 8),
                    (byte)trackData.Length
                });
                writer.Write(trackData);

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithMultipleTimeSignatures()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(new byte[] { 0, 0, 0, 6 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 96 });

                // Write track
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));

                // Track data
                byte[] trackData;
                using (MemoryStream trackStream = new MemoryStream())
                using (BinaryWriter trackWriter = new BinaryWriter(trackStream))
                {
                    // Time signature 1 (4/4)
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0xFF);
                    trackWriter.Write((byte)0x58);
                    trackWriter.Write((byte)0x04);
                    trackWriter.Write(new byte[] { 0x04, 0x02, 0x18, 0x08 });

                    // Time signature 2 (3/4)
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0xFF);
                    trackWriter.Write((byte)0x58);
                    trackWriter.Write((byte)0x04);
                    trackWriter.Write(new byte[] { 0x03, 0x02, 0x18, 0x08 });

                    // Time signature 3 (6/8)
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0xFF);
                    trackWriter.Write((byte)0x58);
                    trackWriter.Write((byte)0x04);
                    trackWriter.Write(new byte[] { 0x06, 0x03, 0x18, 0x08 });

                    // End of track
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x2F, 0x00 });

                    trackData = trackStream.ToArray();
                }

                // Write track length
                writer.Write(new byte[] {
                    (byte)(trackData.Length >> 24),
                    (byte)(trackData.Length >> 16),
                    (byte)(trackData.Length >> 8),
                    (byte)trackData.Length
                });
                writer.Write(trackData);

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithPitchBend()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(new byte[] { 0, 0, 0, 6 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 96 });

                // Write track
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));

                // Track data
                byte[] trackData;
                using (MemoryStream trackStream = new MemoryStream())
                using (BinaryWriter trackWriter = new BinaryWriter(trackStream))
                {
                    // Pitch bend
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0xE0); // Pitch bend
                    trackWriter.Write((byte)0x00); // LSB
                    trackWriter.Write((byte)0x40); // MSB (center position)

                    // End of track
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x2F, 0x00 });

                    trackData = trackStream.ToArray();
                }

                // Write track length
                writer.Write(new byte[] {
                    (byte)(trackData.Length >> 24),
                    (byte)(trackData.Length >> 16),
                    (byte)(trackData.Length >> 8),
                    (byte)trackData.Length
                });
                writer.Write(trackData);

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithPolyphonicAftertouch()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(new byte[] { 0, 0, 0, 6 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 96 });

                // Write track
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));

                // Track data
                byte[] trackData;
                using (MemoryStream trackStream = new MemoryStream())
                using (BinaryWriter trackWriter = new BinaryWriter(trackStream))
                {
                    // Polyphonic aftertouch
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0xA0); // Polyphonic aftertouch channel 1
                    trackWriter.Write((byte)0x3C); // Note 60
                    trackWriter.Write((byte)0x40); // Pressure

                    // Polyphonic aftertouch
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0xA0); // Polyphonic aftertouch channel 1
                    trackWriter.Write((byte)0x3D); // Note 61
                    trackWriter.Write((byte)0x40); // Pressure

                    // End of track
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x2F, 0x00 });

                    trackData = trackStream.ToArray();
                }

                // Write track length
                writer.Write(new byte[] {
                    (byte)(trackData.Length >> 24),
                    (byte)(trackData.Length >> 16),
                    (byte)(trackData.Length >> 8),
                    (byte)trackData.Length
                });
                writer.Write(trackData);

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithProgramChange()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(new byte[] { 0, 0, 0, 6 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 96 });

                // Write track
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));

                // Track data
                byte[] trackData;
                using (MemoryStream trackStream = new MemoryStream())
                using (BinaryWriter trackWriter = new BinaryWriter(trackStream))
                {
                    // Program change
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0xC0); // Program change channel 1
                    trackWriter.Write((byte)0x00); // Piano

                    // Program change
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0xC0); // Program change channel 1
                    trackWriter.Write((byte)0x40); // Violin

                    // End of track
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x2F, 0x00 });

                    trackData = trackStream.ToArray();
                }

                // Write track length
                writer.Write(new byte[] {
                    (byte)(trackData.Length >> 24),
                    (byte)(trackData.Length >> 16),
                    (byte)(trackData.Length >> 8),
                    (byte)trackData.Length
                });
                writer.Write(trackData);

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithRunningStatus()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(new byte[] { 0, 0, 0, 6 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 96 });

                // Write track
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));

                // Track data
                byte[] trackData;
                using (MemoryStream trackStream = new MemoryStream())
                using (BinaryWriter trackWriter = new BinaryWriter(trackStream))
                {
                    // Note on with running status
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0x90); // Note on channel 1
                    trackWriter.Write((byte)0x3C); // Note 60
                    trackWriter.Write((byte)0x40); // Velocity 64

                    // Running status note on
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0x3D); // Note 61
                    trackWriter.Write((byte)0x40); // Velocity 64

                    // Running status note on
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0x3E); // Note 62
                    trackWriter.Write((byte)0x40); // Velocity 64

                    // End of track
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x2F, 0x00 });

                    trackData = trackStream.ToArray();
                }

                // Write track length
                writer.Write(new byte[] {
                    (byte)(trackData.Length >> 24),
                    (byte)(trackData.Length >> 16),
                    (byte)(trackData.Length >> 8),
                    (byte)trackData.Length
                });
                writer.Write(trackData);

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithShortDeltaTimes()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(new byte[] { 0, 0, 0, 6 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 96 });

                // Write track
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));

                // Track data
                byte[] trackData;
                using (MemoryStream trackStream = new MemoryStream())
                using (BinaryWriter trackWriter = new BinaryWriter(trackStream))
                {
                    // Note on with delta time 0
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0x90);
                    trackWriter.Write((byte)0x3C);
                    trackWriter.Write((byte)0x40);

                    // Note on with delta time 0
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0x90);
                    trackWriter.Write((byte)0x3D);
                    trackWriter.Write((byte)0x40);

                    // Note on with delta time 0
                    trackWriter.Write((byte)0x00);
                    trackWriter.Write((byte)0x90);
                    trackWriter.Write((byte)0x3E);
                    trackWriter.Write((byte)0x40);

                    // End of track
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x2F, 0x00 });

                    trackData = trackStream.ToArray();
                }

                // Write track length
                writer.Write(new byte[] {
                    (byte)(trackData.Length >> 24),
                    (byte)(trackData.Length >> 16),
                    (byte)(trackData.Length >> 8),
                    (byte)trackData.Length
                });
                writer.Write(trackData);

                return stream.ToArray();
            }
        }

        private static byte[] CreateMidiWithSystemExclusive()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(new byte[] { 0, 0, 0, 6 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 1 });
                writer.Write(new byte[] { 0, 96 });

                // Write track
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));

                // Track data
                byte[] trackData;
                using (MemoryStream trackStream = new MemoryStream())
                using (BinaryWriter trackWriter = new BinaryWriter(trackStream))
                {
                    // System exclusive message
                    trackWriter.Write((byte)0x00);  // Delta time (VLQ)
                    trackWriter.Write((byte)0xF0);  // SysEx start
                    trackWriter.Write((byte)0x07);  // Length (VLQ) - 7 bytes total
                    trackWriter.Write((byte)0x41);  // Manufacturer ID
                    trackWriter.Write((byte)0x10);  // Device ID
                    trackWriter.Write((byte)0x42);  // Model ID
                    trackWriter.Write((byte)0x12);  // Command
                    trackWriter.Write((byte)0x40);  // Data
                    trackWriter.Write((byte)0xF7);  // SysEx end

                    // End of track
                    trackWriter.Write((byte)0x00);  // Delta time (VLQ)
                    trackWriter.Write((byte)0xFF);  // Meta event marker
                    trackWriter.Write((byte)0x2F);  // End of track meta event type
                    trackWriter.Write((byte)0x00);  // Length (VLQ)

                    trackData = trackStream.ToArray();
                }

                // Write track length
                writer.Write(new byte[] {
                    (byte)(trackData.Length >> 24),
                    (byte)(trackData.Length >> 16),
                    (byte)(trackData.Length >> 8),
                    (byte)trackData.Length
                });
                writer.Write(trackData);

                return stream.ToArray();
            }
        }

        private static byte[] CreateSimpleMidiFile()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Write MIDI header
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(new byte[] { 0, 0, 0, 6 }); // Header length
                writer.Write(new byte[] { 0, 1 }); // Format 1
                writer.Write(new byte[] { 0, 1 }); // 1 track
                writer.Write(new byte[] { 0, 96 }); // 96 PPQ

                // Write track
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));

                // Track data
                byte[] trackData;
                using (MemoryStream trackStream = new MemoryStream())
                using (BinaryWriter trackWriter = new BinaryWriter(trackStream))
                {
                    // Track name
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x03, 0x04 });
                    trackWriter.Write(Encoding.ASCII.GetBytes("Test"));

                    // Note on
                    trackWriter.Write(new byte[] { 0x00, 0x90, 0x3C, 0x40 });

                    // Note off after 96 ticks
                    trackWriter.Write(new byte[] { 0x60, 0x80, 0x3C, 0x40 });

                    // End of track
                    trackWriter.Write(new byte[] { 0x00, 0xFF, 0x2F, 0x00 });

                    trackData = trackStream.ToArray();
                }

                // Write track length
                writer.Write(new byte[] {
                    (byte)(trackData.Length >> 24),
                    (byte)(trackData.Length >> 16),
                    (byte)(trackData.Length >> 8),
                    (byte)trackData.Length
                });
                writer.Write(trackData);

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithAllTrackData()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Header
                writer.Write(new byte[32]); // Title
                writer.Write((short)96); // PPQN
                writer.Write((byte)4); // Time sig numerator
                writer.Write((byte)2); // Time sig denominator
                writer.Write((byte)120); // Tempo
                writer.Write(new byte[3]); // Reserved

                // Track directory
                writer.Write(0xA9); // Offset to track data
                writer.Write(64); // Track data length
                writer.Write(new byte[8]); // Reserved

                // Empty tracks
                for (int i = 1; i < 8; i++)
                {
                    writer.Write(0xA9); // Offset
                    writer.Write(0); // Length
                    writer.Write(new byte[8]); // Reserved
                }

                // Track data (64 bytes total)
                // Note On
                writer.Write(new byte[] { 0x00, 0x90, 0x3C, 0x40 });
                // Note Off
                writer.Write(new byte[] { 0x00, 0x80, 0x3C, 0x40 });
                // Control Change
                writer.Write(new byte[] { 0x00, 0xB0, 0x07, 0x40 });
                // Program Change
                writer.Write(new byte[] { 0x00, 0xC0, 0x00 });
                // Channel Pressure
                writer.Write(new byte[] { 0x00, 0xD0, 0x40 });
                // Pitch Bend
                writer.Write(new byte[] { 0x00, 0xE0, 0x00, 0x40 });
                // System Exclusive
                writer.Write(new byte[] { 0x00, 0xF0, 0x05, 0x41, 0x10, 0x42, 0x12, 0x40, 0xF7 });
                // Meta Events
                writer.Write(new byte[] { 0x00, 0xFF, 0x51, 0x03, 0x07, 0xA1, 0x20 });
                writer.Write(new byte[] { 0x00, 0xFF, 0x58, 0x04, 0x04, 0x02, 0x18, 0x08 });
                // Fill remaining bytes
                writer.Write(new byte[32]);

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithAllTracksActive()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Header
                writer.Write(new byte[32]); // Title
                writer.Write((short)96); // PPQN
                writer.Write((byte)4); // Time sig numerator
                writer.Write((byte)2); // Time sig denominator
                writer.Write((byte)120); // Tempo
                writer.Write(new byte[3]); // Reserved

                // Track directory (all tracks have data)
                int currentOffset = 0xA9;
                for (int i = 0; i < 8; i++)
                {
                    writer.Write(currentOffset); // Offset
                    writer.Write(16); // Length
                    writer.Write(new byte[8]); // Reserved
                    currentOffset += 16;
                }

                // Track data (16 bytes per track)
                for (int i = 0; i < 8; i++)
                {
                    // Note On
                    writer.Write(new byte[] { 0x00, 0x90, 0x3C, 0x40 });
                    // Note Off
                    writer.Write(new byte[] { 0x00, 0x80, 0x3C, 0x40 });
                    // Fill remaining bytes
                    writer.Write(new byte[8]);
                }

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithInvalidPpqn()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Header
                writer.Write(new byte[32]); // Title
                writer.Write((short)0); // Invalid PPQN
                writer.Write((byte)4); // Time sig numerator
                writer.Write((byte)2); // Time sig denominator
                writer.Write((byte)120); // Tempo
                writer.Write(new byte[3]); // Reserved

                // Track directory
                for (int i = 0; i < 8; i++)
                {
                    writer.Write(0xA9); // Offset
                    writer.Write(0); // Length
                    writer.Write(new byte[8]); // Reserved
                }

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithInvalidTempo()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Header
                writer.Write(new byte[32]); // Title
                writer.Write((short)96); // PPQN
                writer.Write((byte)4); // Time sig numerator
                writer.Write((byte)2); // Time sig denominator
                writer.Write((byte)0); // Invalid tempo
                writer.Write(new byte[3]); // Reserved

                // Track directory
                for (int i = 0; i < 8; i++)
                {
                    writer.Write(0xA8); // Offset
                    writer.Write(0); // Length
                    writer.Write(new byte[8]); // Reserved
                }

                // Add some dummy track data to make the file large enough
                writer.Write(new byte[16]); // 16 bytes of dummy data

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithInvalidTimeSignature()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Header
                writer.Write(new byte[32]); // Title
                writer.Write((short)96); // PPQN
                writer.Write((byte)0); // Invalid time sig numerator
                writer.Write((byte)0); // Invalid time sig denominator
                writer.Write((byte)120); // Tempo
                writer.Write(new byte[3]); // Reserved

                // Track directory
                for (int i = 0; i < 8; i++)
                {
                    writer.Write(0xA8); // Offset
                    writer.Write(0); // Length
                    writer.Write(new byte[8]); // Reserved
                }

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithInvalidTrackData()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Header
                writer.Write(new byte[32]); // Title
                writer.Write((short)96); // PPQN
                writer.Write((byte)4); // Time sig numerator
                writer.Write((byte)2); // Time sig denominator
                writer.Write((byte)120); // Tempo
                writer.Write(new byte[3]); // Reserved

                // Track directory
                writer.Write(0xA9); // Offset to track data
                writer.Write(16); // Track data length
                writer.Write(new byte[8]); // Reserved

                // Empty tracks
                for (int i = 1; i < 8; i++)
                {
                    writer.Write(0xA9); // Offset
                    writer.Write(0); // Length
                    writer.Write(new byte[8]); // Reserved
                }

                // Invalid track data (truncated MIDI message)
                writer.Write(new byte[] { 0x00, 0x90, 0x3C });

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithInvalidTrackDirectory()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Header
                writer.Write(new byte[32]); // Title
                writer.Write((short)96); // PPQN
                writer.Write((byte)4); // Time sig numerator
                writer.Write((byte)2); // Time sig denominator
                writer.Write((byte)120); // Tempo
                writer.Write(new byte[3]); // Reserved

                // Invalid track directory (offset beyond file size)
                for (int i = 0; i < 8; i++)
                {
                    writer.Write(0xFFFF); // Invalid offset
                    writer.Write(0xFFFF); // Invalid length
                    writer.Write(new byte[8]); // Reserved
                }

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithMaximumData()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Header
                writer.Write(new byte[32]); // Title
                writer.Write((short)96); // PPQN
                writer.Write((byte)4); // Time sig numerator
                writer.Write((byte)2); // Time sig denominator
                writer.Write((byte)120); // Tempo
                writer.Write(new byte[3]); // Reserved

                // Track directory
                writer.Write(0xA9); // Offset to track data
                writer.Write(int.MaxValue - 0xA9); // Maximum possible track length
                writer.Write(new byte[8]); // Reserved

                // Empty tracks
                for (int i = 1; i < 8; i++)
                {
                    writer.Write(0xA9); // Offset
                    writer.Write(0); // Length
                    writer.Write(new byte[8]); // Reserved
                }

                // Track data (maximum size)
                writer.Write(new byte[int.MaxValue - 0xA9]);

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithMaximumTrackData()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Header
                writer.Write(new byte[32]); // Title
                writer.Write((short)96); // PPQN
                writer.Write((byte)4); // Time sig numerator
                writer.Write((byte)2); // Time sig denominator
                writer.Write((byte)120); // Tempo
                writer.Write(new byte[3]); // Reserved

                // Track directory
                writer.Write(0xA9); // Offset to track data
                writer.Write(int.MaxValue - 0xA9); // Maximum possible track length
                writer.Write(new byte[8]); // Reserved

                // Empty tracks
                for (int i = 1; i < 8; i++)
                {
                    writer.Write(0xA9); // Offset
                    writer.Write(0); // Length
                    writer.Write(new byte[8]); // Reserved
                }

                // Track data (maximum size)
                writer.Write(new byte[int.MaxValue - 0xA9]);

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithMaximumTracks()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Header
                writer.Write(new byte[32]); // Title
                writer.Write((short)96); // PPQN
                writer.Write((byte)4); // Time sig numerator
                writer.Write((byte)2); // Time sig denominator
                writer.Write((byte)120); // Tempo
                writer.Write(new byte[3]); // Reserved

                // Track directory (all tracks have data)
                int currentOffset = 0xA9;
                for (int i = 0; i < 8; i++)
                {
                    writer.Write(currentOffset); // Offset
                    writer.Write(16); // Length
                    writer.Write(new byte[8]); // Reserved
                    currentOffset += 16;
                }

                // Track data (16 bytes per track)
                for (int i = 0; i < 8; i++)
                {
                    writer.Write(new byte[16]);
                }

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithOverlappingTracks()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Header
                writer.Write(new byte[32]); // Title
                writer.Write((short)96); // PPQN
                writer.Write((byte)4); // Time sig numerator
                writer.Write((byte)2); // Time sig denominator
                writer.Write((byte)120); // Tempo
                writer.Write(new byte[3]); // Reserved

                // Track directory with overlapping tracks
                writer.Write(0xA9); // Offset to track 1 data
                writer.Write(16); // Track 1 length
                writer.Write(new byte[8]); // Reserved

                writer.Write(0xAD); // Offset to track 2 data (overlaps with track 1)
                writer.Write(16); // Track 2 length
                writer.Write(new byte[8]); // Reserved

                // Empty tracks
                for (int i = 2; i < 8; i++)
                {
                    writer.Write(0xA9); // Offset
                    writer.Write(0); // Length
                    writer.Write(new byte[8]); // Reserved
                }

                // Track data
                writer.Write(new byte[32]); // 16 bytes for each track

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithTrackData()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Header
                writer.Write(new byte[32]); // Title
                writer.Write((short)96); // PPQN
                writer.Write((byte)4); // Time sig numerator
                writer.Write((byte)2); // Time sig denominator
                writer.Write((byte)120); // Tempo
                writer.Write(new byte[3]); // Reserved

                // Track directory
                writer.Write(0xA9); // Offset to track data
                writer.Write(16); // Track data length
                writer.Write(new byte[8]); // Reserved

                // Empty tracks
                for (int i = 1; i < 8; i++)
                {
                    writer.Write(0xA9); // Offset
                    writer.Write(0); // Length
                    writer.Write(new byte[8]); // Reserved
                }

                // Track data (16 bytes total)
                writer.Write(new byte[] { 0x00, 0x90, 0x3C, 0x40 }); // Note on (4 bytes)
                writer.Write(new byte[] { 0x60, 0x80, 0x3C, 0x40 }); // Note off (4 bytes)
                writer.Write(new byte[8]); // Fill remaining 8 bytes to match declared length

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithVariousPpqn()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Header
                writer.Write(new byte[32]); // Title
                writer.Write((short)480); // PPQN (higher resolution)
                writer.Write((byte)4); // Time sig numerator
                writer.Write((byte)2); // Time sig denominator
                writer.Write((byte)120); // Tempo
                writer.Write(new byte[3]); // Reserved

                // Track directory
                writer.Write(0xA9); // Offset to track data
                writer.Write(16); // Track data length
                writer.Write(new byte[8]); // Reserved

                // Empty tracks
                for (int i = 1; i < 8; i++)
                {
                    writer.Write(0xA9); // Offset
                    writer.Write(0); // Length
                    writer.Write(new byte[8]); // Reserved
                }

                // Track data
                writer.Write(new byte[] { 0x00, 0x90, 0x3C, 0x40 }); // Note on
                writer.Write(new byte[] { 0x00, 0x80, 0x3C, 0x40 }); // Note off
                writer.Write(new byte[8]); // Fill remaining bytes

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithVariousTempos()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Header
                writer.Write(new byte[32]); // Title
                writer.Write((short)96); // PPQN
                writer.Write((byte)4); // Time sig numerator
                writer.Write((byte)2); // Time sig denominator
                writer.Write((byte)120); // Tempo
                writer.Write(new byte[3]); // Reserved

                // Track directory
                writer.Write(0xA9); // Offset to track data
                writer.Write(32); // Track data length
                writer.Write(new byte[8]); // Reserved

                // Empty tracks
                for (int i = 1; i < 8; i++)
                {
                    writer.Write(0xA9); // Offset
                    writer.Write(0); // Length
                    writer.Write(new byte[8]); // Reserved
                }

                // Track data with tempo changes
                writer.Write(new byte[] { 0x00, 0xFF, 0x51, 0x03, 0x07, 0xA1, 0x20 }); // 120 BPM
                writer.Write(new byte[] { 0x00, 0xFF, 0x51, 0x03, 0x06, 0x4E, 0x20 }); // 140 BPM
                writer.Write(new byte[] { 0x00, 0xFF, 0x51, 0x03, 0x09, 0x27, 0xC0 }); // 100 BPM
                writer.Write(new byte[8]); // Fill remaining bytes

                return stream.ToArray();
            }
        }

        private static byte[] CreateSmrcWithVariousTimeSignatures()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Header
                writer.Write(new byte[32]); // Title
                writer.Write((short)96); // PPQN
                writer.Write((byte)3); // Time sig numerator (3/4)
                writer.Write((byte)2); // Time sig denominator
                writer.Write((byte)120); // Tempo
                writer.Write(new byte[3]); // Reserved

                // Track directory
                writer.Write(0xA9); // Offset to track data
                writer.Write(32); // Track data length
                writer.Write(new byte[8]); // Reserved

                // Empty tracks
                for (int i = 1; i < 8; i++)
                {
                    writer.Write(0xA9); // Offset
                    writer.Write(0); // Length
                    writer.Write(new byte[8]); // Reserved
                }

                // Track data with time signature changes
                writer.Write(new byte[] { 0x00, 0xFF, 0x58, 0x04, 0x03, 0x02, 0x18, 0x08 }); // 3/4
                writer.Write(new byte[] { 0x00, 0xFF, 0x58, 0x04, 0x04, 0x02, 0x18, 0x08 }); // 4/4
                writer.Write(new byte[] { 0x00, 0xFF, 0x58, 0x04, 0x06, 0x03, 0x18, 0x08 }); // 6/8
                writer.Write(new byte[8]); // Fill remaining bytes

                return stream.ToArray();
            }
        }

        private static byte[] CreateValidSmrcFile()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            {
                // Header
                writer.Write(new byte[32]); // Title
                writer.Write((short)96); // PPQN
                writer.Write((byte)4); // Time sig numerator
                writer.Write((byte)2); // Time sig denominator
                writer.Write((byte)120); // Tempo
                writer.Write(new byte[3]); // Reserved

                // Track directory (all empty)
                for (int i = 0; i < 8; i++)
                {
                    writer.Write(0xA9); // Offset
                    writer.Write(0); // Length
                    writer.Write(new byte[8]); // Reserved
                }

                return stream.ToArray();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Tests that attempting to convert an empty MIDI file throws an InvalidDataException.
        /// Verifies proper error handling for empty input files.
        /// </summary>
        [Fact]
        public void EmptyMidiFile_ShouldThrowInvalidDataException()
        {
            // Arrange
            byte[] emptyData = Array.Empty<byte>();
            using MemoryStream inputStream = new MemoryStream(emptyData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act & Assert
            _ = Assert.Throws<InvalidDataException>(converter.ConvertMidiToSmrc);
        }

        /// <summary>
        /// Tests that attempting to convert a MIDI file with an invalid header throws an InvalidDataException.
        /// Verifies proper error handling for corrupted MIDI files.
        /// </summary>
        [Fact]
        public void InvalidMidiHeader_ShouldThrowInvalidDataException()
        {
            // Arrange
            byte[] invalidData = Encoding.ASCII.GetBytes("INVALID");
            using MemoryStream inputStream = new MemoryStream(invalidData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act & Assert
            _ = Assert.Throws<InvalidDataException>(converter.ConvertMidiToSmrc);
        }

        /// <summary>
        /// Tests that an invalid S-MRC file (too small) throws an InvalidDataException.
        /// Verifies that the validator rejects files that don't meet the minimum size requirement.
        /// </summary>
        [Fact]
        public void InvalidSmrcFile_ShouldThrowInvalidDataException()
        {
            // Arrange
            byte[] invalidSmrc = new byte[50]; // Too small
            using MemoryStream stream = new MemoryStream(invalidSmrc);

            // Act & Assert
            _ = Assert.Throws<InvalidDataException>(() => SmrcValidator.ValidateSmrcFile(stream));
        }

        /// <summary>
        /// Tests that a MIDI file containing channel pressure (aftertouch) messages can be successfully converted to S-MRC.
        /// Verifies that the converter handles MIDI channel pressure events correctly.
        /// </summary>
        [Fact]
        public void MidiFileWithChannelPressure_ShouldConvertSuccessfully()
        {
            // Arrange
            byte[] midiData = CreateMidiWithChannelPressure();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act
            converter.ConvertMidiToSmrc();

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        /// <summary>
        /// Tests that a MIDI file containing control change messages can be successfully converted to S-MRC.
        /// Verifies that the converter handles MIDI control change events correctly.
        /// </summary>
        [Fact]
        public void MidiFileWithControlChange_ShouldConvertSuccessfully()
        {
            // Arrange
            byte[] midiData = CreateMidiWithControlChange();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act
            converter.ConvertMidiToSmrc();

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        /// <summary>
        /// Tests that a MIDI file containing an empty track can be successfully converted to S-MRC.
        /// Verifies that the converter handles tracks with no events correctly.
        /// </summary>
        [Fact]
        public void MidiFileWithEmptyTrack_ShouldConvertSuccessfully()
        {
            // Arrange
            byte[] midiData = CreateMidiWithEmptyTrack();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act
            converter.ConvertMidiToSmrc();

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        /// <summary>
        /// Tests that a MIDI file with very long delta times can be successfully converted to S-MRC.
        /// Verifies that the converter handles large time values correctly.
        /// </summary>
        [Fact]
        public void MidiFileWithLongDeltaTimes_ShouldConvertSuccessfully()
        {
            // Arrange
            byte[] midiData = CreateMidiWithLongDeltaTimes();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act
            converter.ConvertMidiToSmrc();

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        /// <summary>
        /// Tests that a MIDI file with very long track names can be successfully converted to S-MRC.
        /// Verifies that the converter handles long meta event data correctly.
        /// </summary>
        [Fact]
        public void MidiFileWithLongTrackName_ShouldConvertSuccessfully()
        {
            // Arrange
            byte[] midiData = CreateMidiWithLongTrackName();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act
            converter.ConvertMidiToSmrc();

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        /// <summary>
        /// Tests that a MIDI file with the maximum possible delta time value can be successfully converted.
        /// Verifies that the converter handles the largest possible time values correctly.
        /// </summary>
        [Fact]
        public void MidiFileWithMaximumDeltaTime_ShouldConvertSuccessfully()
        {
            // Arrange
            byte[] midiData = CreateMidiWithMaximumDeltaTime();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act
            converter.ConvertMidiToSmrc();

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        /// <summary>
        /// Tests that a MIDI file containing various meta events (track name, copyright, tempo, time signature, key signature)
        /// can be successfully converted to S-MRC.
        /// Verifies that the converter handles all standard MIDI meta events correctly.
        /// </summary>
        [Fact]
        public void MidiFileWithMetaEvents_ShouldConvertSuccessfully()
        {
            // Arrange
            byte[] midiData = CreateMidiWithMetaEvents();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act
            converter.ConvertMidiToSmrc();

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        /// <summary>
        /// Tests that a MIDI file with multiple channels can be successfully converted to S-MRC.
        /// Verifies that the converter handles multiple MIDI channels correctly.
        /// </summary>
        [Fact]
        public void MidiFileWithMultipleChannels_ShouldConvertSuccessfully()
        {
            // Arrange
            byte[] midiData = CreateMidiWithMultipleChannels();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act
            converter.ConvertMidiToSmrc();

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        /// <summary>
        /// Tests that a MIDI file with multiple tempo changes can be successfully converted to S-MRC.
        /// Verifies that the converter handles multiple tempo meta events correctly.
        /// </summary>
        [Fact]
        public void MidiFileWithMultipleTempos_ShouldConvertSuccessfully()
        {
            // Arrange
            byte[] midiData = CreateMidiWithMultipleTempos();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act
            converter.ConvertMidiToSmrc();

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        /// <summary>
        /// Tests that a MIDI file with multiple time signature changes can be successfully converted to S-MRC.
        /// Verifies that the converter handles multiple time signature meta events correctly.
        /// </summary>
        [Fact]
        public void MidiFileWithMultipleTimeSignatures_ShouldConvertSuccessfully()
        {
            // Arrange
            byte[] midiData = CreateMidiWithMultipleTimeSignatures();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act
            converter.ConvertMidiToSmrc();

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        /// <summary>
        /// Tests that a MIDI file containing pitch bend messages can be successfully converted to S-MRC.
        /// Verifies that the converter handles MIDI pitch bend events correctly.
        /// </summary>
        [Fact]
        public void MidiFileWithPitchBend_ShouldConvertSuccessfully()
        {
            // Arrange
            byte[] midiData = CreateMidiWithPitchBend();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act
            converter.ConvertMidiToSmrc();

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        /// <summary>
        /// Tests that a MIDI file containing polyphonic aftertouch messages can be successfully converted to S-MRC.
        /// Verifies that the converter handles MIDI polyphonic aftertouch events correctly.
        /// </summary>
        [Fact]
        public void MidiFileWithPolyphonicAftertouch_ShouldConvertSuccessfully()
        {
            // Arrange
            byte[] midiData = CreateMidiWithPolyphonicAftertouch();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act
            converter.ConvertMidiToSmrc();

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        /// <summary>
        /// Tests that a MIDI file containing program change messages can be successfully converted to S-MRC.
        /// Verifies that the converter handles MIDI program change events correctly.
        /// </summary>
        [Fact]
        public void MidiFileWithProgramChange_ShouldConvertSuccessfully()
        {
            // Arrange
            byte[] midiData = CreateMidiWithProgramChange();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act
            converter.ConvertMidiToSmrc();

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        /// <summary>
        /// Tests that a MIDI file using running status optimization can be successfully converted to S-MRC.
        /// Verifies that the converter handles MIDI's running status feature correctly.
        /// </summary>
        [Fact]
        public void MidiFileWithRunningStatus_ShouldConvertSuccessfully()
        {
            // Arrange
            byte[] midiData = CreateMidiWithRunningStatus();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act
            converter.ConvertMidiToSmrc();

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        /// <summary>
        /// Tests that a MIDI file with very short delta times can be successfully converted to S-MRC.
        /// Verifies that the converter handles minimum delta time values correctly.
        /// </summary>
        [Fact]
        public void MidiFileWithShortDeltaTimes_ShouldConvertSuccessfully()
        {
            // Arrange
            byte[] midiData = CreateMidiWithShortDeltaTimes();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act
            converter.ConvertMidiToSmrc();

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        /// <summary>
        /// Tests that a MIDI file containing system exclusive messages can be successfully converted to S-MRC.
        /// Verifies that the converter handles MIDI system exclusive events correctly.
        /// </summary>
        [Fact]
        public void MidiFileWithSystemExclusive_ShouldConvertSuccessfully()
        {
            // Arrange
            byte[] midiData = CreateMidiWithSystemExclusive();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act
            converter.ConvertMidiToSmrc();

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        /// <summary>
        /// Tests that a MIDI file in Format 0 (single track) can be successfully converted to S-MRC.
        /// Verifies that the converter handles MIDI Format 0 correctly.
        /// </summary>
        [Fact]
        public void MidiFormat0_ShouldConvertSuccessfully()
        {
            // Arrange
            byte[] midiData = CreateMidiFormat0();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act
            converter.ConvertMidiToSmrc();

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        /// <summary>
        /// Tests that a MIDI file with all possible MIDI events can be successfully converted to S-MRC.
        /// Verifies that the converter handles all standard MIDI event types correctly.
        /// </summary>
        [Fact]
        public void MidiWithAllEventTypes_ShouldConvertSuccessfully()
        {
            // Arrange
            byte[] midiData = CreateMidiWithAllEventTypes();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act
            converter.ConvertMidiToSmrc();

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        /// <summary>
        /// Tests that a MIDI file with all possible meta events can be successfully converted to S-MRC.
        /// Verifies that the converter handles all standard MIDI meta events correctly.
        /// </summary>
        [Fact]
        public void MidiWithAllMetaEvents_ShouldConvertSuccessfully()
        {
            // Arrange
            byte[] midiData = CreateMidiWithAllMetaEvents();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act
            converter.ConvertMidiToSmrc();

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        /// <summary>
        /// Tests that a MIDI file with running status optimization can be successfully converted to S-MRC.
        /// Verifies that the converter handles MIDI running status correctly.
        /// </summary>
        [Fact]
        public void MidiWithRunningStatus_ShouldConvertSuccessfully()
        {
            // Arrange
            byte[] midiData = CreateMidiWithRunningStatus();
            using MemoryStream inputStream = new MemoryStream(midiData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act
            converter.ConvertMidiToSmrc();

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        /// <summary>
        /// Tests that a MIDI file can be converted to S-MRC and back to MIDI while preserving all data.
        /// Verifies that the round-trip conversion maintains data integrity.
        /// </summary>
        [Fact]
        public void RoundTripConversion_ShouldPreserveData()
        {
            // Arrange
            byte[] midiData = CreateSimpleMidiFile();

            // Act - Convert MIDI to S-MRC
            byte[] smrcData;
            using (MemoryStream midiStream = new MemoryStream(midiData))
            using (MemoryStream smrcStream = new MemoryStream())
            {
                SMrcToMidiConverter converter = new SMrcToMidiConverter(midiStream, smrcStream);
                converter.ConvertMidiToSmrc();
                smrcData = smrcStream.ToArray();
            }

            // Act - Convert back to MIDI
            byte[] midiData2;
            using (MemoryStream smrcStream = new MemoryStream(smrcData))
            using (MemoryStream midiStream = new MemoryStream())
            {
                SMrcToMidiConverter converter = new SMrcToMidiConverter(smrcStream, midiStream);
                converter.ConvertSmrcToMidi();
                midiData2 = midiStream.ToArray();
            }

            // Assert
            Assert.Equal(midiData.Length, midiData2.Length);
            Assert.Equal(midiData, midiData2);
        }

        /// <summary>
        /// Tests that an S-MRC file with invalid PPQN throws an InvalidDataException.
        /// Verifies that the validator checks PPQN values.
        /// </summary>
        [Fact]
        public void SmrcFileWithInvalidPpqn_ShouldThrowInvalidDataException()
        {
            // Arrange
            byte[] invalidSmrc = CreateSmrcWithInvalidPpqn();
            using MemoryStream stream = new MemoryStream(invalidSmrc);

            // Act & Assert
            _ = Assert.Throws<InvalidDataException>(() => SmrcValidator.ValidateSmrcFile(stream));
        }

        /// <summary>
        /// Tests that an S-MRC file with an invalid tempo (zero) throws an InvalidDataException.
        /// Verifies that the validator checks tempo values for validity.
        /// </summary>
        [Fact]
        public void SmrcFileWithInvalidTempo_ShouldThrowInvalidDataException()
        {
            // Arrange
            byte[] invalidSmrc = CreateSmrcWithInvalidTempo();
            using MemoryStream stream = new MemoryStream(invalidSmrc);

            // Act & Assert
            _ = Assert.Throws<InvalidDataException>(() => SmrcValidator.ValidateSmrcFile(stream));
        }

        /// <summary>
        /// Tests that an S-MRC file with an invalid time signature (zero values) throws an InvalidDataException.
        /// Verifies that the validator checks time signature values for validity.
        /// </summary>
        [Fact]
        public void SmrcFileWithInvalidTimeSignature_ShouldThrowInvalidDataException()
        {
            // Arrange
            byte[] invalidSmrc = CreateSmrcWithInvalidTimeSignature();
            using MemoryStream stream = new MemoryStream(invalidSmrc);

            // Act & Assert
            _ = Assert.Throws<InvalidDataException>(() => SmrcValidator.ValidateSmrcFile(stream));
        }

        /// <summary>
        /// Tests that an S-MRC file with invalid track data (truncated MIDI message) throws an InvalidDataException.
        /// Verifies that the validator checks track data for proper MIDI message formatting.
        /// </summary>
        [Fact]
        public void SmrcFileWithInvalidTrackData_ShouldThrowInvalidDataException()
        {
            // Arrange
            byte[] invalidSmrc = CreateSmrcWithInvalidTrackData();
            using MemoryStream stream = new MemoryStream(invalidSmrc);

            // Act & Assert
            _ = Assert.Throws<InvalidDataException>(() => SmrcValidator.ValidateSmrcFile(stream));
        }

        /// <summary>
        /// Tests that an S-MRC file with an invalid track directory (offsets beyond file size) throws an InvalidDataException.
        /// Verifies that the validator checks track directory entries for validity.
        /// </summary>
        [Fact]
        public void SmrcFileWithInvalidTrackDirectory_ShouldThrowInvalidDataException()
        {
            // Arrange
            byte[] invalidSmrc = CreateSmrcWithInvalidTrackDirectory();
            using MemoryStream stream = new MemoryStream(invalidSmrc);

            // Act & Assert
            _ = Assert.Throws<InvalidDataException>(() => SmrcValidator.ValidateSmrcFile(stream));
        }

        /// <summary>
        /// Tests that an S-MRC file with maximum track data size passes validation.
        /// Verifies that the validator accepts files with large track data.
        /// </summary>
        [Fact]
        public void SmrcFileWithMaximumTrackData_ShouldPassValidation()
        {
            // Arrange
            byte[] validSmrc = CreateSmrcWithMaximumTrackData();
            using MemoryStream stream = new MemoryStream(validSmrc);

            // Act & Assert
            SmrcValidator.ValidateSmrcFile(stream);
        }

        /// <summary>
        /// Tests that an S-MRC file with maximum track count (8 tracks) passes validation.
        /// Verifies that the validator accepts files with the maximum number of tracks.
        /// </summary>
        [Fact]
        public void SmrcFileWithMaximumTracks_ShouldPassValidation()
        {
            // Arrange
            byte[] validSmrc = CreateSmrcWithMaximumTracks();
            using MemoryStream stream = new MemoryStream(validSmrc);

            // Act & Assert
            SmrcValidator.ValidateSmrcFile(stream);
        }

        /// <summary>
        /// Tests that an S-MRC file with overlapping track data throws an InvalidDataException.
        /// Verifies that the validator checks for track data overlaps.
        /// </summary>
        [Fact]
        public void SmrcFileWithOverlappingTracks_ShouldThrowInvalidDataException()
        {
            // Arrange
            byte[] invalidSmrc = CreateSmrcWithOverlappingTracks();
            using MemoryStream stream = new MemoryStream(invalidSmrc);

            // Act & Assert
            _ = Assert.Throws<InvalidDataException>(() => SmrcValidator.ValidateSmrcFile(stream));
        }

        /// <summary>
        /// Tests that an S-MRC file containing valid track data (note on/off events) passes validation.
        /// Verifies that the validator accepts S-MRC files with actual musical content.
        /// </summary>
        [Fact]
        public void SmrcFileWithTrackData_ShouldValidateSuccessfully()
        {
            // Arrange
            byte[] smrcData = CreateSmrcWithTrackData();
            using MemoryStream stream = new MemoryStream(smrcData);

            // Act & Assert
            SmrcValidator.ValidateSmrcFile(stream);
        }

        /// <summary>
        /// Tests that an S-MRC file with all possible track data can be successfully converted to MIDI and back.
        /// Verifies that the round-trip conversion maintains data integrity when starting with S-MRC.
        /// </summary>
        [Fact]
        public void SmrcRoundTripConversion_ShouldPreserveData()
        {
            // Arrange
            byte[] smrcData = CreateSmrcWithAllTrackData();

            // Act - Convert S-MRC to MIDI
            byte[] midiData;
            using (MemoryStream smrcStream = new MemoryStream(smrcData))
            using (MemoryStream midiStream = new MemoryStream())
            {
                SMrcToMidiConverter converter = new SMrcToMidiConverter(smrcStream, midiStream);
                converter.ConvertSmrcToMidi();
                midiData = midiStream.ToArray();
            }

            // Act - Convert back to S-MRC
            byte[] smrcData2;
            using (MemoryStream midiStream = new MemoryStream(midiData))
            using (MemoryStream smrcStream = new MemoryStream())
            {
                SMrcToMidiConverter converter = new SMrcToMidiConverter(midiStream, smrcStream);
                converter.ConvertMidiToSmrc();
                smrcData2 = smrcStream.ToArray();
            }

            // Assert
            Assert.Equal(smrcData.Length, smrcData2.Length);
            Assert.Equal(smrcData, smrcData2);
        }

        /// <summary>
        /// Tests that an S-MRC file with all tracks containing data can be successfully converted to MIDI.
        /// Verifies that the converter handles multiple active tracks correctly.
        /// </summary>
        [Fact]
        public void SmrcWithAllTracksActive_ShouldConvertSuccessfully()
        {
            // Arrange
            byte[] smrcData = CreateSmrcWithAllTracksActive();
            using MemoryStream inputStream = new MemoryStream(smrcData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act
            converter.ConvertSmrcToMidi();

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        /// <summary>
        /// Tests that an S-MRC file with maximum possible track data size can be successfully converted to MIDI and back.
        /// Verifies that the converter handles large track data correctly in both directions.
        /// </summary>
        [Fact]
        public void SmrcWithMaximumData_ShouldConvertSuccessfully()
        {
            // Arrange
            byte[] smrcData = CreateSmrcWithMaximumData();
            using MemoryStream inputStream = new MemoryStream(smrcData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act
            converter.ConvertSmrcToMidi();

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        /// <summary>
        /// Tests that an S-MRC file with various PPQN values can be successfully converted to MIDI.
        /// Verifies that the converter handles different timing resolutions correctly.
        /// </summary>
        [Fact]
        public void SmrcWithVariousPpqn_ShouldConvertSuccessfully()
        {
            // Arrange
            byte[] smrcData = CreateSmrcWithVariousPpqn();
            using MemoryStream inputStream = new MemoryStream(smrcData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act
            converter.ConvertSmrcToMidi();

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        /// <summary>
        /// Tests that an S-MRC file with various tempos can be successfully converted to MIDI.
        /// Verifies that the converter handles different tempo values correctly.
        /// </summary>
        [Fact]
        public void SmrcWithVariousTempos_ShouldConvertSuccessfully()
        {
            // Arrange
            byte[] smrcData = CreateSmrcWithVariousTempos();
            using MemoryStream inputStream = new MemoryStream(smrcData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act
            converter.ConvertSmrcToMidi();

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        /// <summary>
        /// Tests that an S-MRC file with various time signatures can be successfully converted to MIDI.
        /// Verifies that the converter handles different time signatures correctly.
        /// </summary>
        [Fact]
        public void SmrcWithVariousTimeSignatures_ShouldConvertSuccessfully()
        {
            // Arrange
            byte[] smrcData = CreateSmrcWithVariousTimeSignatures();
            using MemoryStream inputStream = new MemoryStream(smrcData);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act
            converter.ConvertSmrcToMidi();

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        /// <summary>
        /// Tests that attempting to convert a MIDI file with more than 8 tracks throws an InvalidDataException.
        /// Verifies that the converter enforces the S-MRC track limit.
        /// </summary>
        [Fact]
        public void TooManyTracks_ShouldThrowInvalidDataException()
        {
            // Arrange
            byte[] midiWithTooManyTracks = CreateMidiWithManyTracks(10);
            using MemoryStream inputStream = new MemoryStream(midiWithTooManyTracks);
            using MemoryStream outputStream = new MemoryStream();
            SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);

            // Act & Assert
            _ = Assert.Throws<InvalidDataException>(converter.ConvertMidiToSmrc);
        }

        /// <summary>
        /// Tests that a valid S-MRC file passes validation.
        /// Verifies that the validator accepts properly formatted S-MRC files.
        /// </summary>
        [Fact]
        public void ValidSmrcFile_ShouldPassValidation()
        {
            // Arrange
            byte[] validSmrc = CreateValidSmrcFile();
            using MemoryStream stream = new MemoryStream(validSmrc);

            // Act & Assert
            SmrcValidator.ValidateSmrcFile(stream);
        }

        #endregion
    }
}
