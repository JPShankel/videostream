namespace VideoStreamRecorder.Forms
{
	partial class RecorderMainForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.groupBoxConnection = new System.Windows.Forms.GroupBox();
			this.labelPort = new System.Windows.Forms.Label();
			this.numericUpDownPort = new System.Windows.Forms.NumericUpDown();
			this.labelServerIP = new System.Windows.Forms.Label();
			this.textBoxServerIP = new System.Windows.Forms.TextBox();
			this.buttonConnect = new System.Windows.Forms.Button();
			this.groupBoxRecording = new System.Windows.Forms.GroupBox();
			this.labelOutputPath = new System.Windows.Forms.Label();
			this.textBoxOutputPath = new System.Windows.Forms.TextBox();
			this.buttonBrowse = new System.Windows.Forms.Button();
			this.checkBoxRecordCommands = new System.Windows.Forms.CheckBox();
			this.checkBoxRecordVideo = new System.Windows.Forms.CheckBox();
			this.labelVideoQuality = new System.Windows.Forms.Label();
			this.comboBoxVideoQuality = new System.Windows.Forms.ComboBox();
			this.labelVideoResolution = new System.Windows.Forms.Label();
			this.comboBoxVideoResolution = new System.Windows.Forms.ComboBox();
			this.groupBoxStatus = new System.Windows.Forms.GroupBox();
			this.labelConnectionStatus = new System.Windows.Forms.Label();
			this.labelRecordingStatus = new System.Windows.Forms.Label();
			this.labelElapsedTime = new System.Windows.Forms.Label();
			this.labelFramesReceived = new System.Windows.Forms.Label();
			this.labelCommandsReceived = new System.Windows.Forms.Label();
			this.labelDataReceived = new System.Windows.Forms.Label();
			this.groupBoxControls = new System.Windows.Forms.GroupBox();
			this.buttonStartRecording = new System.Windows.Forms.Button();
			this.buttonStopRecording = new System.Windows.Forms.Button();
			this.buttonOpenFolder = new System.Windows.Forms.Button();
			this.textBoxLog = new System.Windows.Forms.TextBox();
			this.labelLog = new System.Windows.Forms.Label();
			this.timerUpdate = new System.Windows.Forms.Timer(this.components);
			this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
			this.labelPort = new System.Windows.Forms.Label();
			this.numericUpDownPort = new System.Windows.Forms.NumericUpDown();
			this.labelServerIP = new System.Windows.Forms.Label();
			this.textBoxServerIP = new System.Windows.Forms.TextBox();
			this.buttonConnect = new System.Windows.Forms.Button();
			this.groupBoxRecording = new System.Windows.Forms.GroupBox();
			this.labelOutputPath = new System.Windows.Forms.Label();
			this.textBoxOutputPath = new System.Windows.Forms.TextBox();
			this.buttonBrowse = new System.Windows.Forms.Button();
			this.checkBoxRecordCommands = new System.Windows.Forms.CheckBox();
			this.checkBoxRecordVideo = new System.Windows.Forms.CheckBox();
			this.groupBoxStatus = new System.Windows.Forms.GroupBox();
			this.labelConnectionStatus = new System.Windows.Forms.Label();
			this.labelRecordingStatus = new System.Windows.Forms.Label();
			this.labelElapsedTime = new System.Windows.Forms.Label();
			this.labelFramesReceived = new System.Windows.Forms.Label();
			this.labelCommandsReceived = new System.Windows.Forms.Label();
			this.labelDataReceived = new System.Windows.Forms.Label();
			this.groupBoxControls = new System.Windows.Forms.GroupBox();
			this.buttonStartRecording = new System.Windows.Forms.Button();
			this.buttonStopRecording = new System.Windows.Forms.Button();
			this.buttonOpenFolder = new System.Windows.Forms.Button();
			this.textBoxLog = new System.Windows.Forms.TextBox();
			this.labelLog = new System.Windows.Forms.Label();
			this.timerUpdate = new System.Windows.Forms.Timer(this.components);
			this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
			this.groupBoxConnection.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownPort)).BeginInit();
			this.groupBoxRecording.SuspendLayout();
			this.groupBoxStatus.SuspendLayout();
			this.groupBoxControls.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBoxConnection
			// 
			this.groupBoxConnection.Controls.Add(this.labelPort);
			this.groupBoxConnection.Controls.Add(this.numericUpDownPort);
			this.groupBoxConnection.Controls.Add(this.labelServerIP);
			this.groupBoxConnection.Controls.Add(this.textBoxServerIP);
			this.groupBoxConnection.Controls.Add(this.buttonConnect);
			this.groupBoxConnection.Location = new System.Drawing.Point(12, 12);
			this.groupBoxConnection.Name = "groupBoxConnection";
			this.groupBoxConnection.Size = new System.Drawing.Size(600, 80);
			this.groupBoxConnection.TabIndex = 0;
			this.groupBoxConnection.TabStop = false;
			this.groupBoxConnection.Text = "Server Connection";
			// 
			// labelPort
			// 
			this.labelPort.AutoSize = true;
			this.labelPort.Location = new System.Drawing.Point(280, 25);
			this.labelPort.Name = "labelPort";
			this.labelPort.Size = new System.Drawing.Size(32, 15);
			this.labelPort.TabIndex = 4;
			this.labelPort.Text = "Port:";
			// 
			// numericUpDownPort
			// 
			this.numericUpDownPort.Location = new System.Drawing.Point(318, 23);
			this.numericUpDownPort.Maximum = new decimal(new int[] {
			65535,
			0,
			0,
			0});
			this.numericUpDownPort.Minimum = new decimal(new int[] {
			1,
			0,
			0,
			0});
			this.numericUpDownPort.Name = "numericUpDownPort";
			this.numericUpDownPort.Size = new System.Drawing.Size(80, 23);
			this.numericUpDownPort.TabIndex = 3;
			this.numericUpDownPort.Value = new decimal(new int[] {
			8080,
			0,
			0,
			0});
			// 
			// labelServerIP
			// 
			this.labelServerIP.AutoSize = true;
			this.labelServerIP.Location = new System.Drawing.Point(10, 25);
			this.labelServerIP.Name = "labelServerIP";
			this.labelServerIP.Size = new System.Drawing.Size(60, 15);
			this.labelServerIP.TabIndex = 2;
			this.labelServerIP.Text = "Server IP:";
			// 
			// textBoxServerIP
			// 
			this.textBoxServerIP.Location = new System.Drawing.Point(76, 22);
			this.textBoxServerIP.Name = "textBoxServerIP";
			this.textBoxServerIP.Size = new System.Drawing.Size(180, 23);
			this.textBoxServerIP.TabIndex = 1;
			this.textBoxServerIP.Text = "127.0.0.1";
			// 
			// buttonConnect
			// 
			this.buttonConnect.BackColor = System.Drawing.Color.LightBlue;
			this.buttonConnect.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
			this.buttonConnect.Location = new System.Drawing.Point(450, 15);
			this.buttonConnect.Name = "buttonConnect";
			this.buttonConnect.Size = new System.Drawing.Size(120, 35);
			this.buttonConnect.TabIndex = 0;
			this.buttonConnect.Text = "Connect";
			this.buttonConnect.UseVisualStyleBackColor = false;
			this.buttonConnect.Click += new System.EventHandler(this.ButtonConnect_Click);
			// 
			// groupBoxRecording
			// 
			this.groupBoxRecording.Controls.Add(this.labelOutputPath);
			this.groupBoxRecording.Controls.Add(this.textBoxOutputPath);
			this.groupBoxRecording.Controls.Add(this.buttonBrowse);
			this.groupBoxRecording.Controls.Add(this.checkBoxRecordCommands);
			this.groupBoxRecording.Controls.Add(this.checkBoxRecordVideo);
			this.groupBoxRecording.Controls.Add(this.labelVideoQuality);
			this.groupBoxRecording.Controls.Add(this.comboBoxVideoQuality);
			this.groupBoxRecording.Controls.Add(this.labelVideoResolution);
			this.groupBoxRecording.Controls.Add(this.comboBoxVideoResolution);
			this.groupBoxRecording.Location = new System.Drawing.Point(12, 98);
			this.groupBoxRecording.Name = "groupBoxRecording";
			this.groupBoxRecording.Size = new System.Drawing.Size(600, 130);
			this.groupBoxRecording.TabIndex = 1;
			this.groupBoxRecording.TabStop = false;
			this.groupBoxRecording.Text = "Recording Options";
			// 
			// labelOutputPath
			// 
			this.labelOutputPath.AutoSize = true;
			this.labelOutputPath.Location = new System.Drawing.Point(10, 25);
			this.labelOutputPath.Name = "labelOutputPath";
			this.labelOutputPath.Size = new System.Drawing.Size(75, 15);
			this.labelOutputPath.TabIndex = 4;
			this.labelOutputPath.Text = "Output Path:";
			// 
			// textBoxOutputPath
			// 
			this.textBoxOutputPath.Location = new System.Drawing.Point(91, 22);
			this.textBoxOutputPath.Name = "textBoxOutputPath";
			this.textBoxOutputPath.Size = new System.Drawing.Size(400, 23);
			this.textBoxOutputPath.TabIndex = 3;
			this.textBoxOutputPath.Text = "recordings";
			// 
			// buttonBrowse
			// 
			this.buttonBrowse.Location = new System.Drawing.Point(497, 21);
			this.buttonBrowse.Name = "buttonBrowse";
			this.buttonBrowse.Size = new System.Drawing.Size(75, 23);
			this.buttonBrowse.TabIndex = 2;
			this.buttonBrowse.Text = "Browse...";
			this.buttonBrowse.UseVisualStyleBackColor = true;
			this.buttonBrowse.Click += new System.EventHandler(this.ButtonBrowse_Click);
			// 
			// checkBoxRecordCommands
			// 
			this.checkBoxRecordCommands.AutoSize = true;
			this.checkBoxRecordCommands.Checked = true;
			this.checkBoxRecordCommands.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxRecordCommands.Location = new System.Drawing.Point(200, 60);
			this.checkBoxRecordCommands.Name = "checkBoxRecordCommands";
			this.checkBoxRecordCommands.Size = new System.Drawing.Size(126, 19);
			this.checkBoxRecordCommands.TabIndex = 1;
			this.checkBoxRecordCommands.Text = "Record Commands";
			this.checkBoxRecordCommands.UseVisualStyleBackColor = true;
			// 
			// checkBoxRecordVideo
			// 
			this.checkBoxRecordVideo.AutoSize = true;
			this.checkBoxRecordVideo.Checked = true;
			this.checkBoxRecordVideo.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxRecordVideo.Location = new System.Drawing.Point(91, 60);
			this.checkBoxRecordVideo.Name = "checkBoxRecordVideo";
			this.checkBoxRecordVideo.Size = new System.Drawing.Size(96, 19);
			this.checkBoxRecordVideo.TabIndex = 0;
			this.checkBoxRecordVideo.Text = "Record Video";
			this.checkBoxRecordVideo.UseVisualStyleBackColor = true;
			// 
			// labelVideoQuality
			// 
			this.labelVideoQuality.AutoSize = true;
			this.labelVideoQuality.Location = new System.Drawing.Point(91, 90);
			this.labelVideoQuality.Name = "labelVideoQuality";
			this.labelVideoQuality.Size = new System.Drawing.Size(48, 15);
			this.labelVideoQuality.TabIndex = 5;
			this.labelVideoQuality.Text = "Quality:";
			// 
			// comboBoxVideoQuality
			// 
			this.comboBoxVideoQuality.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxVideoQuality.FormattingEnabled = true;
			this.comboBoxVideoQuality.Items.AddRange(new object[] {
			"High (90%)",
			"Medium (75%)",
			"Low (50%)"});
			this.comboBoxVideoQuality.Location = new System.Drawing.Point(145, 87);
			this.comboBoxVideoQuality.Name = "comboBoxVideoQuality";
			this.comboBoxVideoQuality.SelectedIndex = 1;
			this.comboBoxVideoQuality.Size = new System.Drawing.Size(120, 23);
			this.comboBoxVideoQuality.TabIndex = 6;
			// 
			// labelVideoResolution
			// 
			this.labelVideoResolution.AutoSize = true;
			this.labelVideoResolution.Location = new System.Drawing.Point(300, 90);
			this.labelVideoResolution.Name = "labelVideoResolution";
			this.labelVideoResolution.Size = new System.Drawing.Size(66, 15);
			this.labelVideoResolution.TabIndex = 7;
			this.labelVideoResolution.Text = "Resolution:";
			// 
			// comboBoxVideoResolution
			// 
			this.comboBoxVideoResolution.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxVideoResolution.FormattingEnabled = true;
			this.comboBoxVideoResolution.Items.AddRange(new object[] {
			"1920x1080",
			"1280x720",
			"854x480",
			"640x360"});
			this.comboBoxVideoResolution.Location = new System.Drawing.Point(372, 87);
			this.comboBoxVideoResolution.Name = "comboBoxVideoResolution";
			this.comboBoxVideoResolution.SelectedIndex = 0;
			this.comboBoxVideoResolution.Size = new System.Drawing.Size(120, 23);
			this.comboBoxVideoResolution.TabIndex = 8;
			// 
			// groupBoxStatus
			// 
			this.groupBoxStatus.Controls.Add(this.labelConnectionStatus);
			this.groupBoxStatus.Controls.Add(this.labelRecordingStatus);
			this.groupBoxStatus.Controls.Add(this.labelElapsedTime);
			this.groupBoxStatus.Controls.Add(this.labelFramesReceived);
			this.groupBoxStatus.Controls.Add(this.labelCommandsReceived);
			this.groupBoxStatus.Controls.Add(this.labelDataReceived);
			this.groupBoxStatus.Location = new System.Drawing.Point(12, 234);
			this.groupBoxStatus.Name = "groupBoxStatus";
			this.groupBoxStatus.Size = new System.Drawing.Size(600, 120);
			this.groupBoxStatus.TabIndex = 2;
			this.groupBoxStatus.TabStop = false;
			this.groupBoxStatus.Text = "Status";
			// 
			// labelConnectionStatus
			// 
			this.labelConnectionStatus.AutoSize = true;
			this.labelConnectionStatus.Location = new System.Drawing.Point(10, 25);
			this.labelConnectionStatus.Name = "labelConnectionStatus";
			this.labelConnectionStatus.Size = new System.Drawing.Size(107, 15);
			this.labelConnectionStatus.TabIndex = 5;
			this.labelConnectionStatus.Text = "Status: Disconnected";
			// 
			// labelRecordingStatus
			// 
			this.labelRecordingStatus.AutoSize = true;
			this.labelRecordingStatus.Location = new System.Drawing.Point(10, 50);
			this.labelRecordingStatus.Name = "labelRecordingStatus";
			this.labelRecordingStatus.Size = new System.Drawing.Size(110, 15);
			this.labelRecordingStatus.TabIndex = 4;
			this.labelRecordingStatus.Text = "Recording: Stopped";
			// 
			// labelElapsedTime
			// 
			this.labelElapsedTime.AutoSize = true;
			this.labelElapsedTime.Location = new System.Drawing.Point(10, 75);
			this.labelElapsedTime.Name = "labelElapsedTime";
			this.labelElapsedTime.Size = new System.Drawing.Size(86, 15);
			this.labelElapsedTime.TabIndex = 3;
			this.labelElapsedTime.Text = "Time: 00:00:00";
			// 
			// labelFramesReceived
			// 
			this.labelFramesReceived.AutoSize = true;
			this.labelFramesReceived.Location = new System.Drawing.Point(300, 25);
			this.labelFramesReceived.Name = "labelFramesReceived";
			this.labelFramesReceived.Size = new System.Drawing.Size(96, 15);
			this.labelFramesReceived.TabIndex = 2;
			this.labelFramesReceived.Text = "Frames Received: 0";
			// 
			// labelCommandsReceived
			// 
			this.labelCommandsReceived.AutoSize = true;
			this.labelCommandsReceived.Location = new System.Drawing.Point(300, 50);
			this.labelCommandsReceived.Name = "labelCommandsReceived";
			this.labelCommandsReceived.Size = new System.Drawing.Size(124, 15);
			this.labelCommandsReceived.TabIndex = 1;
			this.labelCommandsReceived.Text = "Commands Received: 0";
			// 
			// labelDataReceived
			// 
			this.labelDataReceived.AutoSize = true;
			this.labelDataReceived.Location = new System.Drawing.Point(300, 75);
			this.labelDataReceived.Name = "labelDataReceived";
			this.labelDataReceived.Size = new System.Drawing.Size(95, 15);
			this.labelDataReceived.TabIndex = 0;
			this.labelDataReceived.Text = "Data Received: 0 MB";
			// 
			// groupBoxControls
			// 
			this.groupBoxControls.Controls.Add(this.buttonStartRecording);
			this.groupBoxControls.Controls.Add(this.buttonStopRecording);
			this.groupBoxControls.Controls.Add(this.buttonOpenFolder);
			this.groupBoxControls.Location = new System.Drawing.Point(12, 360);
			this.groupBoxControls.Name = "groupBoxControls";
			this.groupBoxControls.Size = new System.Drawing.Size(600, 70);
			this.groupBoxControls.TabIndex = 3;
			this.groupBoxControls.TabStop = false;
			this.groupBoxControls.Text = "Controls";
			// 
			// buttonStartRecording
			// 
			this.buttonStartRecording.BackColor = System.Drawing.Color.LightGreen;
			this.buttonStartRecording.Enabled = false;
			this.buttonStartRecording.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
			this.buttonStartRecording.Location = new System.Drawing.Point(20, 22);
			this.buttonStartRecording.Name = "buttonStartRecording";
			this.buttonStartRecording.Size = new System.Drawing.Size(140, 40);
			this.buttonStartRecording.TabIndex = 0;
			this.buttonStartRecording.Text = "Start Recording";
			this.buttonStartRecording.UseVisualStyleBackColor = false;
			this.buttonStartRecording.Click += new System.EventHandler(this.ButtonStartRecording_Click);
			// 
			// buttonStopRecording
			// 
			this.buttonStopRecording.BackColor = System.Drawing.Color.LightCoral;
			this.buttonStopRecording.Enabled = false;
			this.buttonStopRecording.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
			this.buttonStopRecording.Location = new System.Drawing.Point(180, 22);
			this.buttonStopRecording.Name = "buttonStopRecording";
			this.buttonStopRecording.Size = new System.Drawing.Size(140, 40);
			this.buttonStopRecording.TabIndex = 1;
			this.buttonStopRecording.Text = "Stop Recording";
			this.buttonStopRecording.UseVisualStyleBackColor = false;
			this.buttonStopRecording.Click += new System.EventHandler(this.ButtonStopRecording_Click);
			// 
			// buttonOpenFolder
			// 
			this.buttonOpenFolder.Location = new System.Drawing.Point(450, 22);
			this.buttonOpenFolder.Name = "buttonOpenFolder";
			this.buttonOpenFolder.Size = new System.Drawing.Size(120, 40);
			this.buttonOpenFolder.TabIndex = 2;
			this.buttonOpenFolder.Text = "Open Output Folder";
			this.buttonOpenFolder.UseVisualStyleBackColor = true;
			this.buttonOpenFolder.Click += new System.EventHandler(this.ButtonOpenFolder_Click);
			// 
			// textBoxLog
			// 
			this.textBoxLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
			| System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxLog.BackColor = System.Drawing.Color.Black;
			this.textBoxLog.Font = new System.Drawing.Font("Consolas", 9F);
			this.textBoxLog.ForeColor = System.Drawing.Color.Lime;
			this.textBoxLog.Location = new System.Drawing.Point(12, 455);
			this.textBoxLog.Multiline = true;
			this.textBoxLog.Name = "textBoxLog";
			this.textBoxLog.ReadOnly = true;
			this.textBoxLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.textBoxLog.Size = new System.Drawing.Size(600, 150);
			this.textBoxLog.TabIndex = 4;
			// 
			// labelLog
			// 
			this.labelLog.AutoSize = true;
			this.labelLog.Location = new System.Drawing.Point(12, 437);
			this.labelLog.Name = "labelLog";
			this.labelLog.Size = new System.Drawing.Size(71, 15);
			this.labelLog.TabIndex = 5;
			this.labelLog.Text = "Activity Log:";
			// 
			// timerUpdate
			// 
			this.timerUpdate.Interval = 1000;
			this.timerUpdate.Tick += new System.EventHandler(this.TimerUpdate_Tick);
			// 
			// folderBrowserDialog
			// 
			this.folderBrowserDialog.Description = "Select folder for recordings";
			// 
			// RecorderMainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(624, 617);
			this.Controls.Add(this.labelLog);
			this.Controls.Add(this.textBoxLog);
			this.Controls.Add(this.groupBoxControls);
			this.Controls.Add(this.groupBoxStatus);
			this.Controls.Add(this.groupBoxRecording);
			this.Controls.Add(this.groupBoxConnection);
			this.MinimumSize = new System.Drawing.Size(640, 650);
			this.Name = "RecorderMainForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Video Stream Recorder - AVI Encoding";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.RecorderMainForm_FormClosing);
			this.Load += new System.EventHandler(this.RecorderMainForm_Load);
			this.groupBoxConnection.ResumeLayout(false);
			this.groupBoxConnection.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownPort)).EndInit();
			this.groupBoxRecording.ResumeLayout(false);
			this.groupBoxRecording.PerformLayout();
			this.groupBoxStatus.ResumeLayout(false);
			this.groupBoxStatus.PerformLayout();
			this.groupBoxControls.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBoxConnection;
		private System.Windows.Forms.Label labelPort;
		private System.Windows.Forms.NumericUpDown numericUpDownPort;
		private System.Windows.Forms.Label labelServerIP;
		private System.Windows.Forms.TextBox textBoxServerIP;
		private System.Windows.Forms.Button buttonConnect;
		private System.Windows.Forms.GroupBox groupBoxRecording;
		private System.Windows.Forms.Label labelOutputPath;
		private System.Windows.Forms.TextBox textBoxOutputPath;
		private System.Windows.Forms.Button buttonBrowse;
		private System.Windows.Forms.CheckBox checkBoxRecordCommands;
		private System.Windows.Forms.CheckBox checkBoxRecordVideo;
		private System.Windows.Forms.Label labelVideoQuality;
		private System.Windows.Forms.ComboBox comboBoxVideoQuality;
		private System.Windows.Forms.Label labelVideoResolution;
		private System.Windows.Forms.ComboBox comboBoxVideoResolution;
		private System.Windows.Forms.GroupBox groupBoxStatus;
		private System.Windows.Forms.Label labelConnectionStatus;
		private System.Windows.Forms.Label labelRecordingStatus;
		private System.Windows.Forms.Label labelElapsedTime;
		private System.Windows.Forms.Label labelFramesReceived;
		private System.Windows.Forms.Label labelCommandsReceived;
		private System.Windows.Forms.Label labelDataReceived;
		private System.Windows.Forms.GroupBox groupBoxControls;
		private System.Windows.Forms.Button buttonStartRecording;
		private System.Windows.Forms.Button buttonStopRecording;
		private System.Windows.Forms.Button buttonOpenFolder;
		private System.Windows.Forms.TextBox textBoxLog;
		private System.Windows.Forms.Label labelLog;
		private System.Windows.Forms.Timer timerUpdate;
		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
	}
}