using Accord.Video.VFW;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using SystemDrawing = System.Drawing;
using SystemDrawingImaging = System.Drawing.Imaging;


namespace VideoStreamRecorder.Forms
{
	public partial class RecorderMainForm : Form
	{
		// Network connection
		private TcpClient? tcpClient;
		private NetworkStream? networkStream;
		private bool isConnected = false;

		// Recording state
		private bool isRecording = false;
		private DateTime recordingStartTime;
		private CancellationTokenSource? cancellationTokenSource;

		// Recording streams and video processing
		private FileStream? rawVideoStream;
		private StreamWriter? commandLogWriter;
		private AVIWriter? aviWriter;
		private MemoryStream? frameBuffer;

		// Video settings
		private int videoWidth = 1920;
		private int videoHeight = 1080;
		private int videoFrameRate = 30;
		private int videoQuality = 75; // VFW quality (0-100)

		// Statistics
		private int framesReceived = 0;
		private int commandsReceived = 0;
		private long totalDataReceived = 0;
		private bool isFirstCommand = true;

		// Recording files
		private string? currentVideoFileName;
		private string? currentCommandFileName;
		private string? currentRawVideoFileName;

		private void InitializeVideoWriter()
		{
			try
			{
				// Get resolution settings
				var resolution = comboBoxVideoResolution.Text.Split('x');
				videoWidth = int.Parse(resolution[0]);
				videoHeight = int.Parse(resolution[1]);

				// Get quality settings for VFW quality (0-100)
				var qualityIndex = comboBoxVideoQuality.SelectedIndex;
				videoQuality = qualityIndex switch
				{
					0 => 90,  // High quality
					1 => 75,  // Medium quality  
					2 => 50,  // Low quality
					_ => 75   // Default
				};

				// Initialize Accord VFW AVI writer
				aviWriter = new AVIWriter();
				aviWriter.Open(currentVideoFileName!, videoWidth, videoHeight);
				aviWriter.FrameRate = videoFrameRate;
				aviWriter.Quality = videoQuality;

				frameBuffer = new MemoryStream();

				LogMessage($"Accord VFW writer initialized for AVI ({videoWidth}x{videoHeight}, Quality: {videoQuality}%)");
			}
			catch (Exception ex)
			{
				LogMessage($"Error initializing VFW video writer: {ex.Message}");

				// Fallback to raw file recording
				currentRawVideoFileName = currentVideoFileName!.Replace(".avi", ".raw");
				rawVideoStream = new FileStream(currentRawVideoFileName, FileMode.Create, FileAccess.Write);
				LogMessage("Fallback: Recording to raw video file");
			}
		}

		public RecorderMainForm()
		{
			InitializeComponent();
			InitializeForm();
		}

		private void InitializeForm()
		{
			// Set application icon
			//this.Icon = SystemDrawing.SystemIcons.Application;

			// Initialize timer
			timerUpdate.Start();

			// Set initial UI state
			UpdateUIState();

			LogMessage("Video Stream Recorder initialized");
			LogMessage($"Ready to connect to server");
		}

		#region Form Events

		private void RecorderMainForm_Load(object sender, EventArgs e)
		{
			LogMessage("Application started");

			// Create recordings directory if it doesn't exist
			CreateOutputDirectory();
		}

		private void RecorderMainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (isRecording)
			{
				var result = MessageBox.Show(
					"Recording is in progress. Stop recording and exit?",
					"Recording Active",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Question);

				if (result == DialogResult.No)
				{
					e.Cancel = true;
					return;
				}

				StopRecording();
			}

			DisconnectFromServer();
		}

		private void TimerUpdate_Tick(object sender, EventArgs e)
		{
			UpdateStatusDisplay();
		}

		#endregion

		#region Connection Management

		private async void ButtonConnect_Click(object sender, EventArgs e)
		{
			if (isConnected)
			{
				DisconnectFromServer();
			}
			else
			{
				await ConnectToServerAsync();
			}
		}

		private async Task ConnectToServerAsync()
		{
			try
			{
				buttonConnect.Enabled = false;
				buttonConnect.Text = "Connecting...";
				LogMessage($"Connecting to {textBoxServerIP.Text}:{numericUpDownPort.Value}...");

				tcpClient = new TcpClient();
				await tcpClient.ConnectAsync(textBoxServerIP.Text, (int)numericUpDownPort.Value);
				networkStream = tcpClient.GetStream();

				isConnected = true;
				UpdateUIState();

				LogMessage("Connected successfully");

				// Start receiving data
				cancellationTokenSource = new CancellationTokenSource();
				_ = Task.Run(() => ReceiveDataAsync(cancellationTokenSource.Token));
			}
			catch (Exception ex)
			{
				LogMessage($"Connection failed: {ex.Message}");

				MessageBox.Show($"Failed to connect: {ex.Message}", "Connection Error",
					MessageBoxButtons.OK, MessageBoxIcon.Error);

				UpdateUIState();
			}
			finally
			{
				buttonConnect.Enabled = true;
			}
		}

		private void DisconnectFromServer()
		{
			try
			{
				if (isRecording)
				{
					StopRecording();
				}

				isConnected = false;
				cancellationTokenSource?.Cancel();

				networkStream?.Close();
				tcpClient?.Close();

				UpdateUIState();
				LogMessage("Disconnected from server");
			}
			catch (Exception ex)
			{
				LogMessage($"Error during disconnection: {ex.Message}");
			}
		}

		#endregion

		#region Recording Management

		private void ButtonStartRecording_Click(object sender, EventArgs e)
		{
			StartRecording();
		}

		private void ButtonStopRecording_Click(object sender, EventArgs e)
		{
			StopRecording();
		}

		private void StartRecording()
		{
			if (!isConnected)
			{
				MessageBox.Show("Must be connected to server before recording", "Not Connected",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			try
			{
				// Create output files
				var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
				var outputDir = textBoxOutputPath.Text;

				if (checkBoxRecordVideo.Checked)
				{
					currentVideoFileName = Path.Combine(outputDir, $"video_{timestamp}.avi");

					// Initialize AForge video writer
					InitializeVideoWriter();

					LogMessage($"Video recording to: {currentVideoFileName}");
				}

				if (checkBoxRecordCommands.Checked)
				{
					currentCommandFileName = Path.Combine(outputDir, $"commands_{timestamp}.json");
					commandLogWriter = new StreamWriter(currentCommandFileName, false, Encoding.UTF8);
					commandLogWriter.WriteLine("[");
					isFirstCommand = true;
					LogMessage($"Command recording to: {currentCommandFileName}");
				}

				isRecording = true;
				recordingStartTime = DateTime.Now;

				// Reset counters
				framesReceived = 0;
				commandsReceived = 0;
				totalDataReceived = 0;

				UpdateUIState();
				LogMessage("Recording started");
			}
			catch (Exception ex)
			{
				LogMessage($"Error starting recording: {ex.Message}");
				MessageBox.Show($"Error starting recording: {ex.Message}", "Recording Error",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void StartFFmpegEncoding()
		{
			try
			{
				// Configure FFmpeg for real-time encoding
				var ffmpegArgs = "-f rawvideo -pixel_format yuv420p -video_size 1920x1080 -framerate 30 " +
							   $"-i pipe:0 -c:v libx264 -preset ultrafast -crf 23 -f mp4 \"{currentVideoFileName}\"";

				var ffmpegProcess = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = "ffmpeg",
						Arguments = ffmpegArgs,
						UseShellExecute = false,
						RedirectStandardInput = true,
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						CreateNoWindow = true
					}
				};

				ffmpegProcess.Start();
				var ffmpegInputStream = ffmpegProcess.StandardInput.BaseStream;

				LogMessage("FFmpeg encoder started for real-time MP4 encoding");
			}
			catch (Exception ex)
			{
				LogMessage($"Error starting FFmpeg: {ex.Message}");
				LogMessage("Note: FFmpeg must be installed and accessible from PATH");

				// Fallback to raw file recording
				rawVideoStream = new FileStream(currentRawVideoFileName!, FileMode.Create, FileAccess.Write);
				LogMessage("Fallback: Recording to raw video file");
			}
		}

		private void StopRecording()
		{
			if (!isRecording) return;

			try
			{
				isRecording = false;

				// Close Accord VFW video writer
				if (aviWriter != null)
				{
					try
					{
						aviWriter.Close();
						aviWriter.Dispose();
						aviWriter = null;
						LogMessage($"AVI video saved successfully: {currentVideoFileName}");
					}
					catch (Exception ex)
					{
						LogMessage($"Error closing VFW video writer: {ex.Message}");
					}
				}

				// Close frame buffer
				if (frameBuffer != null)
				{
					try
					{
						frameBuffer.Dispose();
						frameBuffer = null;
					}
					catch (Exception ex)
					{
						LogMessage($"Error disposing frame buffer: {ex.Message}");
					}
				}

				// Close raw video file (fallback mode)
				if (rawVideoStream != null)
				{
					try
					{
						rawVideoStream.Close();
						rawVideoStream.Dispose();
						rawVideoStream = null;
						LogMessage($"Raw video file saved: {currentRawVideoFileName}");
					}
					catch (Exception ex)
					{
						LogMessage($"Error closing raw video stream: {ex.Message}");
					}
				}

				// Close command file
				if (commandLogWriter != null)
				{
					try
					{
						commandLogWriter.WriteLine("\n]");
						commandLogWriter.Close();
						commandLogWriter.Dispose();
						commandLogWriter = null;
						LogMessage($"Command log saved: {currentCommandFileName}");
					}
					catch (Exception ex)
					{
						LogMessage($"Error closing command log: {ex.Message}");
					}
				}

				UpdateUIState();

				var duration = DateTime.Now - recordingStartTime;
				LogMessage($"Recording stopped. Duration: {duration:hh\\:mm\\:ss}");
				LogMessage($"Total frames: {framesReceived}, Commands: {commandsReceived}");
			}
			catch (Exception ex)
			{
				LogMessage($"Error stopping recording: {ex.Message}");
			}
		}

		private async Task ProcessVideoFrameAsync(byte[] buffer, int bytesRead, CancellationToken cancellationToken)
		{
			try
			{
				// Add the new data to the frame buffer
				if (frameBuffer == null)
					frameBuffer = new MemoryStream();

				await frameBuffer.WriteAsync(buffer, 0, bytesRead, cancellationToken);

				// Calculate expected frame size (assuming RGB24 format)
				int expectedFrameSize = videoWidth * videoHeight * 3;

				// If we have enough data for a complete frame
				if (frameBuffer.Length >= expectedFrameSize)
				{
					// Extract one frame worth of data
					byte[] frameData = new byte[expectedFrameSize];
					frameBuffer.Position = 0;
					await frameBuffer.ReadAsync(frameData, 0, expectedFrameSize, cancellationToken);

					// Move remaining data to beginning of buffer
					var remainingData = new byte[frameBuffer.Length - expectedFrameSize];
					await frameBuffer.ReadAsync(remainingData, 0, remainingData.Length, cancellationToken);

					frameBuffer.SetLength(0);
					await frameBuffer.WriteAsync(remainingData, 0, remainingData.Length, cancellationToken);

					// Convert raw bytes to bitmap
					var bitmap = CreateBitmapFromRawData(frameData, videoWidth, videoHeight);

					if (bitmap.Value != null)
					{
						// Write frame to AVI file using VFW
						aviWriter?.AddFrame(bitmap.Value);
						bitmap.Value.Dispose();
					}
				}
			}
			catch (Exception ex)
			{
				LogMessage($"Error processing video frame: {ex.Message}");
			}
		}

		// Replace ambiguous 'Bitmap' and 'PixelFormat' usages with fully qualified names from System.Drawing.Common

		private global::System.Drawing.Bitmap? CreateBitmapFromRawData(byte[] data, int width, int height)
		{
			try
			{
				// Create bitmap from raw RGB data using System.Drawing.Common
				var bitmap = new global::System.Drawing.Bitmap(width, height, global::System.Drawing.Imaging.PixelFormat.Format24bppRgb);
				var bitmapData = bitmap.LockBits(
					new global::System.Drawing.Rectangle(0, 0, width, height),
					global::System.Drawing.Imaging.ImageLockMode.WriteOnly,
					global::System.Drawing.Imaging.PixelFormat.Format24bppRgb
				);

				// Copy raw data to bitmap
				System.Runtime.InteropServices.Marshal.Copy(data, 0, bitmapData.Scan0, Math.Min(data.Length, bitmapData.Stride * height));

				bitmap.UnlockBits(bitmapData);
				return bitmap;
			}
			catch (Exception ex)
			{
				LogMessage($"Error creating bitmap: {ex.Message}");
				return null;
			}
		}

		private async Task ConvertRawToMp4Async()
		{
			// This method is no longer needed with AForge.NET
			// AForge handles the conversion directly
			await Task.CompletedTask;
		}

		#endregion

		#region Data Processing

		private async Task ReceiveDataAsync(CancellationToken cancellationToken)
		{
			byte[] buffer = new byte[4096];
			StringBuilder messageBuffer = new StringBuilder();

			try
			{
				while (isConnected && !cancellationToken.IsCancellationRequested)
				{
					if (networkStream != null && networkStream.CanRead)
					{
						int bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

						if (bytesRead == 0)
						{
							Invoke(() => LogMessage("Server closed the connection"));
							break;
						}

						totalDataReceived += bytesRead;

						// Try to determine if this is command data or video data
						string dataString = Encoding.UTF8.GetString(buffer, 0, Math.Min(bytesRead, 100));

						if (IsLikelyCommandData(dataString))
						{
							// Process as command data
							string fullData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
							messageBuffer.Append(fullData);
							ProcessCommandData(messageBuffer);
						}
						else
						{
							// Process as video frame data
							if (isRecording && checkBoxRecordVideo.Checked)
							{
								if (aviWriter != null)
								{
									// Convert raw bytes to bitmap and write to video
									await ProcessVideoFrameAsync(buffer, bytesRead, cancellationToken);
								}
								else if (rawVideoStream != null)
								{
									// Fallback: write to raw file
									await rawVideoStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
									await rawVideoStream.FlushAsync(cancellationToken);
								}
							}
							Invoke(() => framesReceived++);
						}
					}
					else
					{
						await Task.Delay(10, cancellationToken);
					}
				}
			}
			catch (OperationCanceledException)
			{
				// Expected when cancellation is requested
			}
			catch (Exception ex)
			{
				Invoke(() => LogMessage($"Data reception error: {ex.Message}"));
			}
		}

		private bool IsLikelyCommandData(string data)
		{
			// Simple heuristic to detect command data vs binary video data
			return data.All(c => char.IsControl(c) || (c >= 32 && c <= 126)) &&
				   (data.Contains("{") || data.Contains("command") || data.Contains(":"));
		}

		private void ProcessCommandData(StringBuilder messageBuffer)
		{
			try
			{
				string bufferContent = messageBuffer.ToString();
				string[] lines = bufferContent.Split('\n');

				for (int i = 0; i < lines.Length - 1; i++)
				{
					if (!string.IsNullOrWhiteSpace(lines[i]))
					{
						var command = new CommandEntry
						{
							Timestamp = DateTime.UtcNow,
							Command = lines[i].Trim(),
							Source = "stream"
						};

						if (isRecording && checkBoxRecordCommands.Checked && commandLogWriter != null)
						{
							WriteCommandToLog(command);
						}

						Invoke(() =>
						{
							commandsReceived++;
							LogMessage($"Command: {command.Command}");
						});
					}
				}

				// Keep the last incomplete line in the buffer
				messageBuffer.Clear();
				if (lines.Length > 0)
				{
					messageBuffer.Append(lines[lines.Length - 1]);
				}
			}
			catch (Exception ex)
			{
				Invoke(() => LogMessage($"Error processing command data: {ex.Message}"));
			}
		}

		private void WriteCommandToLog(CommandEntry command)
		{
			try
			{
				if (!isFirstCommand)
				{
					commandLogWriter?.WriteLine(",");
				}

				var json = JsonSerializer.Serialize(command, new JsonSerializerOptions { WriteIndented = true });
				commandLogWriter?.Write(json);
				commandLogWriter?.Flush();

				isFirstCommand = false;
			}
			catch (Exception ex)
			{
				Invoke(() => LogMessage($"Error writing command log: {ex.Message}"));
			}
		}

		#endregion

		#region UI Management

		private void ButtonBrowse_Click(object sender, EventArgs e)
		{
			folderBrowserDialog.SelectedPath = textBoxOutputPath.Text;

			if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
			{
				textBoxOutputPath.Text = folderBrowserDialog.SelectedPath;
				LogMessage($"Output directory changed to: {folderBrowserDialog.SelectedPath}");
			}
		}

		private void ButtonOpenFolder_Click(object sender, EventArgs e)
		{
			try
			{
				if (Directory.Exists(textBoxOutputPath.Text))
				{
					Process.Start("explorer.exe", textBoxOutputPath.Text);
					LogMessage($"Opened output folder: {textBoxOutputPath.Text}");
				}
				else
				{
					MessageBox.Show($"Directory does not exist: {textBoxOutputPath.Text}",
						"Directory Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			}
			catch (Exception ex)
			{
				LogMessage($"Error opening folder: {ex.Message}");
				MessageBox.Show($"Error opening folder: {ex.Message}", "Error",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void UpdateUIState()
		{
			if (InvokeRequired)
			{
				Invoke(UpdateUIState);
				return;
			}

			// Connection UI
			if (isConnected)
			{
				buttonConnect.Text = "Disconnect";
				buttonConnect.BackColor = Color.LightCoral;
				labelConnectionStatus.Text = "Status: Connected";
				labelConnectionStatus.ForeColor = Color.Green;
			}
			else
			{
				buttonConnect.Text = "Connect";
				buttonConnect.BackColor = Color.LightBlue;
				labelConnectionStatus.Text = "Status: Disconnected";
				labelConnectionStatus.ForeColor = Color.Red;
			}

			// Recording UI
			buttonStartRecording.Enabled = isConnected && !isRecording;
			buttonStopRecording.Enabled = isRecording;

			if (isRecording)
			{
				labelRecordingStatus.Text = "Recording: Active";
				labelRecordingStatus.ForeColor = Color.Green;
			}
			else
			{
				labelRecordingStatus.Text = "Recording: Stopped";
				labelRecordingStatus.ForeColor = Color.Red;
			}

			// Disable connection settings during recording
			groupBoxConnection.Enabled = !isRecording;
			textBoxOutputPath.Enabled = !isRecording;
			buttonBrowse.Enabled = !isRecording;
			checkBoxRecordVideo.Enabled = !isRecording;
			checkBoxRecordCommands.Enabled = !isRecording;
		}

		private void UpdateStatusDisplay()
		{
			if (InvokeRequired)
			{
				Invoke(UpdateStatusDisplay);
				return;
			}

			// Update statistics
			labelFramesReceived.Text = $"Frames Received: {framesReceived}";
			labelCommandsReceived.Text = $"Commands Received: {commandsReceived}";
			labelDataReceived.Text = $"Data Received: {totalDataReceived / (1024.0 * 1024.0):F2} MB";

			// Update elapsed time
			if (isRecording)
			{
				var elapsed = DateTime.Now - recordingStartTime;
				labelElapsedTime.Text = $"Time: {elapsed:hh\\:mm\\:ss}";
			}
			else
			{
				labelElapsedTime.Text = "Time: 00:00:00";
			}
		}

		private void LogMessage(string message)
		{
			if (InvokeRequired)
			{
				Invoke(() => LogMessage(message));
				return;
			}

			var timestamp = DateTime.Now.ToString("HH:mm:ss");
			var logEntry = $"[{timestamp}] {message}";

			textBoxLog.AppendText(logEntry + Environment.NewLine);
			textBoxLog.SelectionStart = textBoxLog.Text.Length;
			textBoxLog.ScrollToCaret();
		}

		private void CreateOutputDirectory()
		{
			try
			{
				if (!Directory.Exists(textBoxOutputPath.Text))
				{
					Directory.CreateDirectory(textBoxOutputPath.Text);
					LogMessage($"Created output directory: {textBoxOutputPath.Text}");
				}
			}
			catch (Exception ex)
			{
				LogMessage($"Error creating output directory: {ex.Message}");
				MessageBox.Show($"Error creating output directory: {ex.Message}", "Directory Error",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}

		#endregion

		#region Cleanup

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				try
				{
					// Cancel any ongoing operations
					cancellationTokenSource?.Cancel();

					// Stop recording if active
					if (isRecording)
					{
						isRecording = false;
					}

					// Safely dispose Accord VFW video writer
					if (aviWriter != null)
					{
						try
						{
							aviWriter.Close();
							aviWriter.Dispose();
						}
						catch { /* Ignore disposal errors */ }
						aviWriter = null;
					}

					// Dispose frame buffer
					frameBuffer?.Dispose();

					// Dispose other resources
					rawVideoStream?.Dispose();
					commandLogWriter?.Dispose();
					networkStream?.Dispose();
					tcpClient?.Dispose();
					cancellationTokenSource?.Dispose();
					components?.Dispose();
				}
				catch (Exception ex)
				{
					// Log disposal errors but don't throw
					try
					{
						LogMessage($"Error during cleanup: {ex.Message}");
					}
					catch
					{
						// If logging fails, just ignore
					}
				}
			}
			base.Dispose(disposing);
		}

		#endregion
	}

	// Helper classes for command logging
	public class CommandEntry
	{
		public DateTime Timestamp { get; set; }
		public string Command { get; set; } = string.Empty;
		public string Source { get; set; } = string.Empty;
		public TimeSpan RelativeTime { get; set; }
	}
}