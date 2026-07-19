namespace RolandConverter.GUI
{
    // Main Form class - typically in MainForm.cs
    public partial class MainForm : Form
    {
        private TextBox _inputFileTextBox;
        private TextBox _outputFileTextBox;
        private Button _inputBrowseButton;
        private Button _outputBrowseButton;
        private RadioButton _toMidiRadioButton;
        private RadioButton _toSmrcRadioButton;
        private Button _convertButton;
        private Button _validateButton;
        private RichTextBox _logTextBox;
        private ProgressBar _progressBar;
        private Label _statusLabel;
        private GroupBox _conversionGroupBox;
        private GroupBox _filesGroupBox;
        private GroupBox _logGroupBox;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Roland S-MRC Converter";
            this.Size = new Size(800, 600);
            this.MinimumSize = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Create controls
            CreateControls();

            // Layout controls
            LayoutControls();

            // Wire up events
            WireEvents();

            // Set initial state
            UpdateConversionDirection();
        }

        private void CreateControls()
        {
            // File selection controls
            _filesGroupBox = new GroupBox
            {
                Text = "Files",
                Dock = DockStyle.Top,
                Height = 120,
                Padding = new Padding(10)
            };

            _inputFileTextBox = new TextBox
            {
                ReadOnly = true,
                BackColor = SystemColors.Window
            };

            _outputFileTextBox = new TextBox
            {
                ReadOnly = true,
                BackColor = SystemColors.Window
            };

            _inputBrowseButton = new Button
            {
                Text = "Browse...",
                Width = 80
            };

            _outputBrowseButton = new Button
            {
                Text = "Browse...",
                Width = 80
            };

            // Conversion options
            _conversionGroupBox = new GroupBox
            {
                Text = "Conversion Direction",
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(10)
            };

            _toMidiRadioButton = new RadioButton
            {
                Text = "S-MRC to MIDI",
                Checked = true,
                AutoSize = true
            };

            _toSmrcRadioButton = new RadioButton
            {
                Text = "MIDI to S-MRC",
                AutoSize = true
            };

            // Action buttons
            _convertButton = new Button
            {
                Text = "Convert",
                Height = 35,
                Font = new Font(this.Font.FontFamily, 10, FontStyle.Bold),
                BackColor = Color.LightGreen
            };

            _validateButton = new Button
            {
                Text = "Validate File",
                Height = 35
            };

            // Progress and status
            _progressBar = new ProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 23,
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };

            _statusLabel = new Label
            {
                Text = "Ready",
                Dock = DockStyle.Bottom,
                Height = 20,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Log output
            _logGroupBox = new GroupBox
            {
                Text = "Output Log",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            _logTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 9),
                BackColor = Color.FromArgb(240, 240, 240)
            };
        }

        private void LayoutControls()
        {
            // Main panel
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Files section
            TableLayoutPanel filesLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                AutoSize = true
            };

            _ = filesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            _ = filesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _ = filesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));

            filesLayout.Controls.Add(new Label { Text = "Input File:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            filesLayout.Controls.Add(_inputFileTextBox, 1, 0);
            filesLayout.Controls.Add(_inputBrowseButton, 2, 0);

            filesLayout.Controls.Add(new Label { Text = "Output File:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
            filesLayout.Controls.Add(_outputFileTextBox, 1, 1);
            filesLayout.Controls.Add(_outputBrowseButton, 2, 1);

            _filesGroupBox.Controls.Add(filesLayout);

            // Conversion options
            FlowLayoutPanel conversionLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            conversionLayout.Controls.Add(_toMidiRadioButton);
            conversionLayout.Controls.Add(new Label { Width = 20 }); // Spacer
            conversionLayout.Controls.Add(_toSmrcRadioButton);

            _conversionGroupBox.Controls.Add(conversionLayout);

            // Buttons panel
            Panel buttonPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(0, 5, 0, 5)
            };

            TableLayoutPanel buttonLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1
            };

            _ = buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            _ = buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 10));
            _ = buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            buttonLayout.Controls.Add(_convertButton, 0, 0);
            buttonLayout.Controls.Add(_validateButton, 2, 0);

            buttonPanel.Controls.Add(buttonLayout);

            // Log section
            _logGroupBox.Controls.Add(_logTextBox);

            // Add all sections to main panel
            mainPanel.Controls.Add(_logGroupBox);
            mainPanel.Controls.Add(buttonPanel);
            mainPanel.Controls.Add(_conversionGroupBox);
            mainPanel.Controls.Add(_filesGroupBox);

            // Add to form
            this.Controls.Add(mainPanel);
            this.Controls.Add(_progressBar);
            this.Controls.Add(_statusLabel);
        }

        private void WireEvents()
        {
            _inputBrowseButton.Click += InputBrowseButton_Click;
            _outputBrowseButton.Click += OutputBrowseButton_Click;
            _toMidiRadioButton.CheckedChanged += ConversionDirection_Changed;
            _toSmrcRadioButton.CheckedChanged += ConversionDirection_Changed;
            _convertButton.Click += ConvertButton_Click;
            _validateButton.Click += ValidateButton_Click;

            // Drag and drop support
            this.AllowDrop = true;
            this.DragEnter += MainForm_DragEnter;
            this.DragDrop += MainForm_DragDrop;
        }

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

        private void ConversionDirection_Changed(object sender, EventArgs e)
        {
            UpdateConversionDirection();
        }

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

        private void UpdateOutputFileName()
        {
            if (string.IsNullOrEmpty(_inputFileTextBox.Text))
            {
                return;
            }

            string inputPath = _inputFileTextBox.Text;
            string directory = Path.GetDirectoryName(inputPath);
            string nameWithoutExt = Path.GetFileNameWithoutExtension(inputPath);

            if (_toMidiRadioButton.Checked)
            {
                _outputFileTextBox.Text = Path.Combine(directory, nameWithoutExt + ".mid");
            }
            else
            {
                _outputFileTextBox.Text = Path.Combine(directory, nameWithoutExt + ".mrc");
            }
        }

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

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(_inputFileTextBox.Text))
            {
                _ = MessageBox.Show("Please select an input file.", "No Input File",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(_outputFileTextBox.Text))
            {
                _ = MessageBox.Show("Please select an output file.", "No Output File",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!File.Exists(_inputFileTextBox.Text))
            {
                _ = MessageBox.Show("The input file does not exist.", "File Not Found",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

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

        private void LogInfo(string message)
        {
            LogMessage(message, Color.Black);
        }

        private void LogSuccess(string message)
        {
            LogMessage(message, Color.Green);
        }

        private void LogError(string message)
        {
            LogMessage(message, Color.Red);
        }

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

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                string filePath = files[0];
                string extension = Path.GetExtension(filePath).ToLower();

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
    }

    // Program entry point - typically in Program.cs
    public static class RolandConverter
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}

// Note: This GUI assumes the converter classes (SMrcToMidiConverter, SmrcValidator, Program) 
// are available in the same namespace or properly referenced.
// 
// To use this in Visual Studio:
// 1. Create a new Windows Forms App (.NET Framework or .NET Core)
// 2. Replace the default Form1 with this MainForm class
// 3. Add the converter classes from the previous artifact
// 4. Update Program.cs to use RolandConverter.Main() as the entry point
//
// The GUI provides:
// - File selection with browse dialogs
// - Drag and drop support
// - Conversion direction selection
// - Progress indication
// - Validation functionality
// - Detailed logging
// - Error handling with user-friendly messages