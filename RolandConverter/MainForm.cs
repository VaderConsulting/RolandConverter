namespace RolandConverter
{
    /// <summary>
    /// The main form of the Roland Converter application, providing the user interface for file conversion
    /// between S-MRC and MIDI formats.
    /// </summary>
    public partial class MainForm : Form
    {
        #region Fields

        private GroupBox _conversionGroupBox;
        private Button _convertButton;
        private GroupBox _filesGroupBox;
        private Button _inputBrowseButton;
        private TextBox _inputFileTextBox;
        private GroupBox _logGroupBox;
        private RichTextBox _logTextBox;
        private Button _outputBrowseButton;
        private TextBox _outputFileTextBox;
        private ProgressBar _progressBar;
        private Label _statusLabel;
        private RadioButton _toMidiRadioButton;
        private RadioButton _toSmrcRadioButton;
        private Button _validateButton;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the MainForm class.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Logs an error message to the log text box.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void LogError(string message)
        {
            LogMessage(message, Color.Red);
        }

        /// <summary>
        /// Logs an informational message to the log text box.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void LogInfo(string message)
        {
            LogMessage(message, Color.Black);
        }

        /// <summary>
        /// Logs a message to the log text box with the specified color.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="color">The color to use for the message.</param>
        private void LogMessage(string message, Color color)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => LogMessage(message, color)));
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            _logTextBox.SelectionStart = _logTextBox.TextLength;
            _logTextBox.SelectionLength = 0;
            _logTextBox.SelectionColor = color;
            _logTextBox.AppendText($"[{timestamp}] {message}\n");
            _logTextBox.SelectionColor = _logTextBox.ForeColor;
            _logTextBox.ScrollToCaret();
        }

        /// <summary>
        /// Logs a success message to the log text box.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void LogSuccess(string message)
        {
            LogMessage(message, Color.Green);
        }

        /// <summary>
        /// Sets the busy state of the form, enabling or disabling controls as appropriate.
        /// </summary>
        /// <param name="busy">True to set the form to busy state; false to set it to normal state.</param>
        private void SetBusyState(bool busy)
        {
            _convertButton.Enabled = !busy;
            _validateButton.Enabled = !busy;
            _inputBrowseButton.Enabled = !busy;
            _outputBrowseButton.Enabled = !busy;
            _toMidiRadioButton.Enabled = !busy;
            _toSmrcRadioButton.Enabled = !busy;
            _progressBar.Visible = busy;

            this.Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        }

        /// <summary>
        /// Updates the UI elements based on the current conversion direction.
        /// </summary>
        private void UpdateConversionDirection()
        {
            _inputFileTextBox.Clear();
            _outputFileTextBox.Clear();

            if (_toMidiRadioButton.Checked)
            {
                _statusLabel.Text = "Ready to convert S-MRC to MIDI";
            }
            else
            {
                _statusLabel.Text = "Ready to convert MIDI to S-MRC";
            }
        }

        /// <summary>
        /// Updates the _Output file name based on the _Input file name and current conversion direction.
        /// </summary>
        private void UpdateOutputFileName()
        {
            if (string.IsNullOrEmpty(_inputFileTextBox.Text))
            {
                return;
            }

            string inputPath = _inputFileTextBox.Text;
            string directory = Path.GetDirectoryName(inputPath) ?? string.Empty;
            string nameWithoutExt = Path.GetFileNameWithoutExtension(inputPath) ?? string.Empty;

            if (_toMidiRadioButton.Checked)
            {
                _outputFileTextBox.Text = Path.Combine(directory, nameWithoutExt + ".MID");
            }
            else
            {
                _outputFileTextBox.Text = Path.Combine(directory, nameWithoutExt + ".SEQ");
            }
        }

        /// <summary>
        /// Validates the specified file to determine if it's a valid S-MRC or MIDI file.
        /// </summary>
        /// <param name="filePath">The path to the file to validate.</param>
        /// <returns>A string containing the validation results.</returns>
        private string ValidateFile(string filePath)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                // Try S-MRC validation first
                try
                {
                    SmrcValidator.ValidateSmrcFile(stream);
                    return $"{Path.GetFileName(filePath)} is a valid S-MRC file";
                }
                catch { }

                // Try MIDI validation
                stream.Position = 0;
                try
                {
                    SmrcValidator.ValidateMidiFile(stream);
                    return $"{Path.GetFileName(filePath)} is a valid MIDI file";
                }
                catch { }

                return $"{Path.GetFileName(filePath)} is not a valid S-MRC or MIDI file";
            }
        }

        /// <summary>
        /// Validates the _Input fields before starting the conversion process.
        /// </summary>
        /// <returns>True if all inputs are valid; otherwise, false.</returns>
        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(_inputFileTextBox.Text))
            {
                _ = MessageBox.Show("Please select an _Input file.", "No Input File",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(_outputFileTextBox.Text))
            {
                _ = MessageBox.Show("Please select an _Output file.", "No Output File",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!File.Exists(_inputFileTextBox.Text))
            {
                _ = MessageBox.Show("The _Input file does not exist.", "File Not Found",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the change event of the conversion direction radio buttons.
        /// Updates the UI to reflect the new conversion direction.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        private void ConversionDirection_Changed(object sender, EventArgs e)
        {
            UpdateConversionDirection();
        }

        /// <summary>
        /// Handles the click event of the convert button.
        /// Performs the file conversion operation asynchronously.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        private async void ConvertButton_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs())
            {
                return;
            }

            SetBusyState(true);

            try
            {
                await Task.Run(() =>
                {
                    if (_toMidiRadioButton.Checked)
                    {
                        Program.ConvertSmrcToMidi(_inputFileTextBox.Text, _outputFileTextBox.Text);
                    }
                    else
                    {
                        Program.ConvertMidiToSmrc(_inputFileTextBox.Text, _outputFileTextBox.Text);
                    }
                });

                LogSuccess($"Successfully converted {Path.GetFileName(_inputFileTextBox.Text)} to {Path.GetFileName(_outputFileTextBox.Text)}");
                _statusLabel.Text = "Conversion completed successfully";

                _ = MessageBox.Show("Conversion completed successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogError($"Conversion failed: {ex.Message}");
                _statusLabel.Text = "Conversion failed";

                _ = MessageBox.Show($"Conversion failed:\n\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusyState(false);
            }
        }

        /// <summary>
        /// Handles the click event of the _Input file browse button.
        /// Opens a file dialog to select the _Input file based on the current conversion direction.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        private void InputBrowseButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                if (_toMidiRadioButton.Checked)
                {
                    dialog.Filter = "S-MRC Files (*.mrc)|*.mrc|All Files (*.*)|*.*";
                }
                else
                {
                    dialog.Filter = "MIDI Files (*.mid;*.midi)|*.mid;*.midi|All Files (*.*)|*.*";
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _inputFileTextBox.Text = dialog.FileName;
                    UpdateOutputFileName();
                }
            }
        }

        /// <summary>
        /// Handles the drag drop event of the form.
        /// Processes the dropped files.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A DragEventArgs that contains the event data.</param>
        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                string filePath = files[0];
                string extension = Path.GetExtension(filePath).ToLowerInvariant();

                if (extension == ".mrc")
                {
                    _toMidiRadioButton.Checked = true;
                    _inputFileTextBox.Text = filePath;
                    UpdateOutputFileName();
                }
                else if (extension is ".mid" or ".midi")
                {
                    _toSmrcRadioButton.Checked = true;
                    _inputFileTextBox.Text = filePath;
                    UpdateOutputFileName();
                }
                else
                {
                    _ = MessageBox.Show("Please drop a .mrc or .mid/.midi file.", "Invalid File Type",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        /// <summary>
        /// Handles the drag enter event of the form.
        /// Determines if the dragged items can be dropped on the form.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A DragEventArgs that contains the event data.</param>
        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        /// <summary>
        /// Handles the click event of the _Output file browse button.
        /// Opens a save file dialog to select the _Output file location based on the current conversion direction.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        private void OutputBrowseButton_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                if (_toMidiRadioButton.Checked)
                {
                    dialog.Filter = "MIDI Files (*.mid)|*.mid|All Files (*.*)|*.*";
                    dialog.DefaultExt = "mid";
                }
                else
                {
                    dialog.Filter = "S-MRC Files (*.mrc)|*.mrc|All Files (*.*)|*.*";
                    dialog.DefaultExt = "mrc";
                }

                if (!string.IsNullOrEmpty(_outputFileTextBox.Text))
                {
                    dialog.FileName = Path.GetFileName(_outputFileTextBox.Text);
                    dialog.InitialDirectory = Path.GetDirectoryName(_outputFileTextBox.Text);
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _outputFileTextBox.Text = dialog.FileName;
                }
            }
        }

        /// <summary>
        /// Handles the click event of the validate button.
        /// Validates the selected file asynchronously.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        private async void ValidateButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_inputFileTextBox.Text))
            {
                _ = MessageBox.Show("Please select a file to validate.", "No File Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!File.Exists(_inputFileTextBox.Text))
            {
                _ = MessageBox.Show("The selected file does not exist.", "File Not Found",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SetBusyState(true);

            try
            {
                string result = await Task.Run(() => ValidateFile(_inputFileTextBox.Text));
                LogInfo(result);
                _statusLabel.Text = "Validation completed";
            }
            catch (Exception ex)
            {
                LogError($"Validation failed: {ex.Message}");
                _statusLabel.Text = "Validation failed";
            }
            finally
            {
                SetBusyState(false);
            }
        }

        #endregion
    }
}
