namespace RolandConverter
{
    partial class MainForm
    {
#region Fields

        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

#endregion

#region Private Methods

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

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
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

            filesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            filesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            filesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));

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

            buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 10));
            buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

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

#endregion
    }
}
