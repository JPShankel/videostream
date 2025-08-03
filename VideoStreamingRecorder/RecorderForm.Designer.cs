namespace VideoStreamRecorder.Forms
{
	partial class VideoStreamRecorderForm
	{
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.groupBoxConnection = new System.Windows.Forms.GroupBox();
			this.labelServerIP = new System.Windows.Forms.Label();
			this.textBoxServerIP = new System.Windows.Forms.TextBox();
			this.labelVideoPort = new System.Windows.Forms.Label();
			this.numericUpDownVideoPort = new System.Windows.Forms.NumericUpDown();
			this.labelCommandPort = new System.Windows.Forms.Label();
			this.numericUpDownCommandPort = new System.Windows.Forms.NumericUpDown();
			this.buttonConnect = new System.Windows.Forms.Button();
			this.groupBoxPreview = new System.Windows.Forms.GroupBox();
			this.pictureBoxVideo = new System.Windows.Forms.PictureBox();
			this.labelVideoStatus = new System.Windows.Forms.Label();
			this.groupBoxRecording = new System.Windows.Forms.GroupBox();
			this.labelOutputPath = new System.Windows.Forms.Label();
			this.textBoxOutputPath = new System.Windows.Forms.TextBox();
			this.buttonBrowseOutput = new System.Windows.Forms.Button();
			this.buttonStartRecording = new System.Windows.Forms.Button();
			this.buttonStopRecording = new System.Windows.Forms.Button();
			this.groupBoxStatus = new System.Windows.Forms.GroupBox();
			this.labelConnectionStatus = new System.Windows.Forms.Label();
			this.labelRecordingStatus = new System.Windows.Forms.Label();
			this.labelFrameCount = new System.Windows.Forms.Label();
			this.labelCommandCount = new System.Windows.Forms.Label();
			this.labelRecordingTime = new System.Windows.Forms.Label();
			this.textBoxLog = new System.Windows.Forms.TextBox();
			this.timerUpdate = new System.Windows.Forms.Timer(this.components);
			this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
			this.groupBoxConnection.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownVideoPort)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownCommandPort)).BeginInit();
			this.groupBoxPreview.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxVideo)).BeginInit();
			this.groupBoxRecording.SuspendLayout();
			this.groupBoxStatus.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBoxConnection
			// 
			this.groupBoxConnection.Controls.Add(this.labelServerIP);
			this.groupBoxConnection.Controls.Add(this.textBoxServerIP);
			this.groupBoxConnection.Controls.Add(this.labelVideoPort);
			this.groupBoxConnection.Controls.Add(this.numericUpDownVideoPort);
			this.groupBoxConnection.Controls.Add(this.labelCommandPort);
			this.groupBoxConnection.Controls.Add(this.numericUpDownCommandPort);
			this.groupBoxConnection.Controls.Add(this.buttonConnect);
			this.groupBoxConnection.Location = new System.Drawing.Point(12, 12);
			this.groupBoxConnection.Name = "groupBoxConnection";
			this.groupBoxConnection.Size = new System.Drawing.Size(400, 120);
			this.groupBoxConnection.TabIndex = 0;
			this.groupBoxConnection.TabStop = false;
			this.groupBoxConnection.Text = "Server Connection";
			// 
			// labelServerIP
			// 
			this.labelServerIP.AutoSize = true;
			this.labelServerIP.Location = new System.Drawing.Point(15, 25);
			this.labelServerIP.Name = "labelServerIP";
			this.labelServerIP.Size = new System.Drawing.Size(60, 15);
			this.labelServerIP.TabIndex = 0;
			this.labelServerIP.Text = "Server IP:";
			// 
			// textBoxServerIP
			// 
			this.textBoxServerIP.Location = new System.Drawing.Point(85, 22);
			this.textBoxServerIP.Name = "textBoxServerIP";
			this.textBoxServerIP.Size = new System.Drawing.Size(120, 23);
			this.textBoxServerIP.TabIndex = 1;
			this.textBoxServerIP.Text = "127.0.0.1";
			// 
			// labelVideoPort
			// 
			this.labelVideoPort.AutoSize = true;
			this.labelVideoPort.Location = new System.Drawing.Point(15, 55);
			this.labelVideoPort.Name = "labelVideoPort";
			this.labelVideoPort.Size = new System.Drawing.Size(67, 15);
			this.labelVideoPort.TabIndex = 2;
			this.labelVideoPort.Text = "Video Port:";
			// 
			// numericUpDownVideoPort
			// 
			this.numericUpDownVideoPort.Location = new System.Drawing.Point(85, 52);
			this.numericUpDownVideoPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
			this.numericUpDownVideoPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
			this.numericUpDownVideoPort.Name = "numericUpDownVideoPort";
			this.numericUpDownVideoPort.Size = new System.Drawing.Size(80, 23);
			this.numericUpDownVideoPort.TabIndex = 3;
			this.numericUpDownVideoPort.Value = new decimal(new int[] { 8080, 0, 0, 0 });
			// 
			// labelCommandPort
			// 
			this.labelCommandPort.AutoSize = true;
			this.labelCommandPort.Location = new System.Drawing.Point(220, 55);
			this.labelCommandPort.Name = "labelCommandPort";
			this.labelCommandPort.Size = new System.Drawing.Size(87, 15);
			this.labelCommandPort.TabIndex = 4;
			this.labelCommandPort.Text = "Command Port:";
			// 
			// numericUpDownCommandPort
			// 
			this.numericUpDownCommandPort.Location = new System.Drawing.Point(315, 52);
			this.numericUpDownCommandPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
			this.numericUpDownCommandPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
			this.numericUpDownCommandPort.Name = "numericUpDownCommandPort";
			this.numericUpDownCommandPort.Size = new System.Drawing.Size(80, 23);
			this.numericUpDownCommandPort.TabIndex = 5;
			this.numericUpDownCommandPort.Value = new decimal(new int[] { 8081, 0, 0, 0 });
			// 
			// buttonConnect
			// 
			this.buttonConnect.BackColor = System.Drawing.Color.LightGreen;
			this.buttonConnect.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
			this.buttonConnect.Location = new System.Drawing.Point(290, 15);
			this.buttonConnect.Name = "buttonConnect";
			this.buttonConnect.Size = new System.Drawing.Size(100, 30);
			this.buttonConnect.TabIndex = 6;
			this.buttonConnect.Text = "Connect";
			this.buttonConnect.UseVisualStyleBackColor = false;
			this.buttonConnect.Click += new System.EventHandler(this.ButtonConnect_Click);
			// 
			// groupBoxPreview
			// 
			this.groupBoxPreview.Controls.Add(this.pictureBoxVideo);
			this.groupBoxPreview.Controls.Add(this.labelVideoStatus);
			this.groupBoxPreview.Location = new System.Drawing.Point(430, 12);
			this.groupBoxPreview.Name = "groupBoxPreview";
			this.groupBoxPreview.Size = new System.Drawing.Size(420, 320);
			this.groupBoxPreview.TabIndex = 1;
			this.groupBoxPreview.TabStop = false;
			this.groupBoxPreview.Text = "Video Preview";
			// 
			// pictureBoxVideo
			// 
			this.pictureBoxVideo.BackColor = System.Drawing.Color.Black;
			this.pictureBoxVideo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.pictureBoxVideo.Location = new System.Drawing.Point(10, 20);
			this.pictureBoxVideo.Name = "pictureBoxVideo";
			this.pictureBoxVideo.Size = new System.Drawing.Size(400, 270);
			this.pictureBoxVideo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.pictureBoxVideo.TabIndex = 0;
			this.pictureBoxVideo.TabStop = false;
			// 
			// labelVideoStatus
			// 
			this.labelVideoStatus.AutoSize = true;
			this.labelVideoStatus.Location = new System.Drawing.Point(10, 295);
			this.labelVideoStatus.Name = "labelVideoStatus";
			this.labelVideoStatus.Size = new System.Drawing.Size(94, 15);
			this.labelVideoStatus.TabIndex = 1;
			this.labelVideoStatus.Text = "No video signal";
			// 
			// groupBoxRecording
			// 
			this.groupBoxRecording.Controls.Add(this.labelOutputPath);
			this.groupBoxRecording.Controls.Add(this.textBoxOutputPath);
			this.groupBoxRecording.Controls.Add(this.buttonBrowseOutput);
			this.groupBoxRecording.Controls.Add(this.buttonStartRecording);
			this.groupBoxRecording.Controls.Add(this.buttonStopRecording);
			this.groupBoxRecording.Location = new System.Drawing.Point(12, 140);
			this.groupBoxRecording.Name = "groupBoxRecording";
			this.groupBoxRecording.Size = new System.Drawing.Size(400, 120);
			this.groupBoxRecording.TabIndex = 2;
			this.groupBoxRecording.TabStop = false;
			this.groupBoxRecording.Text = "Recording Controls";
			// 
			// labelOutputPath
			// 
			this.labelOutputPath.AutoSize = true;
			this.labelOutputPath.Location = new System.Drawing.Point(15, 25);
			this.labelOutputPath.Name = "labelOutputPath";
			this.labelOutputPath.Size = new System.Drawing.Size(75, 15);
			this.labelOutputPath.TabIndex = 0;
			this.labelOutputPath.Text = "Output Path:";
			// 
			// textBoxOutputPath
			// 
			this.textBoxOutputPath.Location = new System.Drawing.Point(95, 22);
			this.textBoxOutputPath.Name = "textBoxOutputPath";
			this.textBoxOutputPath.Size = new System.Drawing.Size(220, 23);
			this.textBoxOutputPath.TabIndex = 1;
			this.textBoxOutputPath.Text = "recordings";
			// 
			// buttonBrowseOutput
			// 
			this.buttonBrowseOutput.Location = new System.Drawing.Point(325, 21);
			this.buttonBrowseOutput.Name = "buttonBrowseOutput";
			this.buttonBrowseOutput.Size = new System.Drawing.Size(65, 25);
			this.buttonBrowseOutput.TabIndex = 2;
			this.buttonBrowseOutput.Text = "Browse";
			this.buttonBrowseOutput.UseVisualStyleBackColor = true;
			this.buttonBrowseOutput.Click += new System.EventHandler(this.ButtonBrowseOutput_Click);
			// 
			// buttonStartRecording
			// 
			this.buttonStartRecording.BackColor = System.Drawing.Color.LightGreen;
			this.buttonStartRecording.Enabled = false;
			this.buttonStartRecording.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
			this.buttonStartRecording.Location = new System.Drawing.Point(15, 65);
			this.buttonStartRecording.Name = "buttonStartRecording";
			this.buttonStartRecording.Size = new System.Drawing.Size(150, 40);
			this.buttonStartRecording.TabIndex = 3;
			this.buttonStartRecording.Text = "Start Recording";
			this.buttonStartRecording.UseVisualStyleBackColor = false;
			this.buttonStartRecording.Click += new System.EventHandler(this.ButtonStartRecording_Click);
			// 
			// buttonStopRecording
			// 
			this.buttonStopRecording.BackColor = System.Drawing.Color.LightCoral;
			this.buttonStopRecording.Enabled = false;
			this.buttonStopRecording.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
			this.buttonStopRecording.Location = new System.Drawing.Point(180, 65);
			this.buttonStopRecording.Name = "buttonStopRecording";
			this.buttonStopRecording.Size = new System.Drawing.Size(150, 40);
			this.buttonStopRecording.TabIndex = 4;
			this.buttonStopRecording.Text = "Stop Recording";
			this.buttonStopRecording.UseVisualStyleBackColor = false;
			this.buttonStopRecording.Click += new System.EventHandler(this.ButtonStopRecording_Click);
			// 
			// groupBoxStatus
			// 
			this.groupBoxStatus.Controls.Add(this.labelConnectionStatus);
			this.groupBoxStatus.Controls.Add(this.labelRecordingStatus);
			this.groupBoxStatus.Controls.Add(this.labelFrameCount);
			this.groupBoxStatus.Controls.Add(this.labelCommandCount);
			this.groupBoxStatus.Controls.Add(this.labelRecordingTime);
			this.groupBoxStatus.Location = new System.Drawing.Point(12, 270);
			this.groupBoxStatus.Name = "groupBoxStatus";
			this.groupBoxStatus.Size = new System.Drawing.Size(400, 120);
			this.groupBoxStatus.TabIndex = 3;
			this.groupBoxStatus.TabStop = false;
			this.groupBoxStatus.Text = "Status";
			// 
			// labelConnectionStatus
			// 
			this.labelConnectionStatus.AutoSize = true;
			this.labelConnectionStatus.ForeColor = System.Drawing.Color.Red;
			this.labelConnectionStatus.Location = new System.Drawing.Point(15, 25);
			this.labelConnectionStatus.Name = "labelConnectionStatus";
			this.labelConnectionStatus.Size = new System.Drawing.Size(79, 15);
			this.labelConnectionStatus.TabIndex = 0;
			this.labelConnectionStatus.Text = "Disconnected";
			// 
			// labelRecordingStatus
			// 
			this.labelRecordingStatus.AutoSize = true;
			this.labelRecordingStatus.ForeColor = System.Drawing.Color.Red;
			this.labelRecordingStatus.Location = new System.Drawing.Point(15, 45);
			this.labelRecordingStatus.Name = "labelRecordingStatus";
			this.labelRecordingStatus.Size = new System.Drawing.Size(104, 15);
			this.labelRecordingStatus.TabIndex = 1;
			this.labelRecordingStatus.Text = "Recording: Stopped";
			// 
			// labelFrameCount
			// 
			this.labelFrameCount.AutoSize = true;
			this.labelFrameCount.Location = new System.Drawing.Point(15, 65);
			this.labelFrameCount.Name = "labelFrameCount";
			this.labelFrameCount.Size = new System.Drawing.Size(55, 15);
			this.labelFrameCount.TabIndex = 2;
			this.labelFrameCount.Text = "Frames: 0";
			// 
			// labelCommandCount
			// 
			this.labelCommandCount.AutoSize = true;
			this.labelCommandCount.Location = new System.Drawing.Point(15, 85);
			this.labelCommandCount.Name = "labelCommandCount";
			this.labelCommandCount.Size = new System.Drawing.Size(78, 15);
			this.labelCommandCount.TabIndex = 3;
			this.labelCommandCount.Text = "Commands: 0";
			// 
			// labelRecordingTime
			// 
			this.labelRecordingTime.AutoSize = true;
			this.labelRecordingTime.Location = new System.Drawing.Point(200, 25);
			this.labelRecordingTime.Name = "labelRecordingTime";
			this.labelRecordingTime.Size = new System.Drawing.Size(68, 15);
			this.labelRecordingTime.TabIndex = 4;
			this.labelRecordingTime.Text = "Time: 00:00:00";
			// 
			// textBoxLog
			// 
			this.textBoxLog.BackColor = System.Drawing.Color.Black;
			this.textBoxLog.Font = new System.Drawing.Font("Consolas", 9F);
			this.textBoxLog.ForeColor = System.Drawing.Color.Lime;
			this.textBoxLog.Location = new System.Drawing.Point(12, 400);
			this.textBoxLog.Multiline = true;
			this.textBoxLog.Name = "textBoxLog";
			this.textBoxLog.ReadOnly = true;
			this.textBoxLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.textBoxLog.Size = new System.Drawing.Size(838, 180);
			this.textBoxLog.TabIndex = 4;
			// 
			// timerUpdate
			// 
			this.timerUpdate.Interval = 1000;
			this.timerUpdate.Tick += new System.EventHandler(this.TimerUpdate_Tick);
			// 
			// VideoStreamRecorderForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(862, 592);
			this.Controls.Add(this.textBoxLog);
			this.Controls.Add(this.groupBoxStatus);
			this.Controls.Add(this.groupBoxRecording);
			this.Controls.Add(this.groupBoxPreview);
			this.Controls.Add(this.groupBoxConnection);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.Name = "VideoStreamRecorderForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Video Stream Recorder";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.VideoStreamRecorderForm_FormClosing);
			this.Load += new System.EventHandler(this.VideoStreamRecorderForm_Load);
			this.groupBoxConnection.ResumeLayout(false);
			this.groupBoxConnection.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownVideoPort)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownCommandPort)).EndInit();
			this.groupBoxPreview.ResumeLayout(false);
			this.groupBoxPreview.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxVideo)).EndInit();
			this.groupBoxRecording.ResumeLayout(false);
			this.groupBoxRecording.PerformLayout();
			this.groupBoxStatus.ResumeLayout(false);
			this.groupBoxStatus.PerformLayout();
			this.ResumeLayout(false);
		}

		#region Windows Form Designer generated code

		private System.Windows.Forms.GroupBox groupBoxConnection;
		private System.Windows.Forms.Label labelServerIP;
		private System.Windows.Forms.TextBox textBoxServerIP;
		private System.Windows.Forms.Label labelVideoPort;
		private System.Windows.Forms.NumericUpDown numericUpDownVideoPort;
		private System.Windows.Forms.Label labelCommandPort;
		private System.Windows.Forms.NumericUpDown numericUpDownCommandPort;
		private System.Windows.Forms.Button buttonConnect;
		private System.Windows.Forms.GroupBox groupBoxPreview;
		private System.Windows.Forms.PictureBox pictureBoxVideo;
		private System.Windows.Forms.Label labelVideoStatus;
		private System.Windows.Forms.GroupBox groupBoxRecording;
		private System.Windows.Forms.Label labelOutputPath;
		private System.Windows.Forms.TextBox textBoxOutputPath;
		private System.Windows.Forms.Button buttonBrowseOutput;
		private System.Windows.Forms.Button buttonStartRecording;
		private System.Windows.Forms.Button buttonStopRecording;
		private System.Windows.Forms.GroupBox groupBoxStatus;
		private System.Windows.Forms.Label labelConnectionStatus;
		private System.Windows.Forms.Label labelRecordingStatus;
		private System.Windows.Forms.Label labelFrameCount;
		private System.Windows.Forms.Label labelCommandCount;
		private System.Windows.Forms.Label labelRecordingTime;
		private System.Windows.Forms.TextBox textBoxLog;
		private System.Windows.Forms.Timer timerUpdate;
		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;

		#endregion
	}
}