namespace RolandConverter
{
    /// <summary>
    /// The main program class for the Roland Converter application.
    /// Provides entry points and conversion methods for MIDI and S-MRC file formats.
    /// </summary>
    internal static class Program
    {
        #region Public Methods

        /// <summary>
        /// Converts a MIDI file to S-MRC format.
        /// </summary>
        /// <param name="inputPath">The path to the _Input MIDI file.</param>
        /// <param name="outputPath">The path where the _Output S-MRC file will be saved.</param>
        /// <exception cref="InvalidDataException">Thrown when the _Input file is not a valid MIDI file.</exception>
        /// <exception cref="IOException">Thrown when there is an error reading from or writing to the files.</exception>
        public static void ConvertMidiToSmrc(string inputPath, string outputPath)
        {
            // Validate _Input file
            using (FileStream validateStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read))
            {
                SmrcValidator.ValidateMidiFile(validateStream);
            }

            using (FileStream inputStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read))
            using (FileStream outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
                converter.ConvertMidiToSmrc();
            }
        }

        /// <summary>
        /// Converts an S-MRC file to MIDI format.
        /// </summary>
        /// <param name="inputPath">The path to the _Input S-MRC file.</param>
        /// <param name="outputPath">The path where the _Output MIDI file will be saved.</param>
        /// <exception cref="InvalidDataException">Thrown when the _Input file is not a valid S-MRC file.</exception>
        /// <exception cref="IOException">Thrown when there is an error reading from or writing to the files.</exception>
        public static void ConvertSmrcToMidi(string inputPath, string outputPath)
        {
            // Validate _Input file
            using (FileStream validateStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read))
            {
                SmrcValidator.ValidateSmrcFile(validateStream);
            }

            using (FileStream inputStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read))
            using (FileStream outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                SMrcToMidiConverter converter = new SMrcToMidiConverter(inputStream, outputStream);
                converter.ConvertSmrcToMidi();
            }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        #endregion
    }
}
