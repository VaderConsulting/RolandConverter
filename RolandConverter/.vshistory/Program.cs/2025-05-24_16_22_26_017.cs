namespace RolandConverter
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        public static void ConvertSmrcToMidi(string inputPath, string outputPath)
        {
            // Validate input file
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

        public static void ConvertMidiToSmrc(string inputPath, string outputPath)
        {
            // Validate input file
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
    }
}