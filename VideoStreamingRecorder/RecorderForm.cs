using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace VideoStreamRecorder.Forms
{
	public partial class VideoStreamRecorderForm : Form
	{
		// Network connections
		private TcpClient? videoClient;
		private TcpClient? commandClient;
		private NetworkStream? videoStream;
		private NetworkStream? commandStream;
		private bool isConnected = false;

		// Recording state
		private bool isRecording = false;
		private DateTime recordingStartTime;
		private CancellationTokenSource? cancellationTokenSource;

		// Video processing
		private VideoWriter? videoWriter;
		private StreamWriter? commandLogWriter;
		private Mat? currentFrame;
		private List<byte> frameBuffer = new List<byte>();
		private bool expectingFrameHeader = true;
		private int expectedFrameSize = 0;

		// Statistics
		private int frameCount = 0;
		private int commandCount = 0;
		private DateTime lastFrameTime = DateTime.MinValue;
		private bool isFirstCommand = true;
		private string lastChatMessage = "";
		private DateTime lastChatTime = DateTime.MinValue;

		// Recording files
		private string? currentVideoFile;
		private string? currentCommandFile;

		public VideoStreamRecorderForm()
		{
			InitializeComponent();
			InitializeForm();
		}

		private void InitializeForm()
		{
			this.Icon = SystemIcons.Application;
			timerUpdate.Start();

			// Create recordings directory
			if (!Directory.Exists("recordings"))
			{
				Directory.CreateDirectory("recordings");
			}

			LogMessage("Video Stream Recorder initialized");
			LogMessage("Ready to connect to server with dual streams");
		}

		#region Form Events

		private void VideoStreamRecorderForm_Load(object sender, EventArgs e)
		{
			LogMessage("Application started");
			UpdateUI();
		}

		private void VideoStreamRecorderForm_FormClosing(object sender, FormClosingEventArgs e)
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
			}

			StopRecording();
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

				var serverIP = textBoxServerIP.Text;
				var videoPort = (int)numericUpDownVideoPort.Value;
				var commandPort = (int)numericUpDownCommandPort.Value;

				LogMessage($"Connecting to video stream at {serverIP}:{videoPort}");
				LogMessage($"Connecting to command stream at {serverIP}:{commandPort}");

				// Connect to video stream
				videoClient = new TcpClient();
				await videoClient.ConnectAsync(serverIP, videoPort);
				videoStream = videoClient.GetStream();
				LogMessage("Video stream connected");

				// Connect to command stream
				commandClient = new TcpClient();
				await commandClient.ConnectAsync(serverIP, commandPort);
				commandStream = commandClient.GetStream();
				LogMessage("Command stream connected");

				isConnected = true;
				UpdateUI();

				LogMessage("Connected to both video and command streams");

				// Reset frame buffer
				frameBuffer.Clear();
				expectingFrameHeader = true;
				expectedFrameSize = 0;

				// Start receiving data from both streams
				cancellationTokenSource = new CancellationTokenSource();
				_ = Task.Run(() => ReceiveVideoDataAsync(cancellationTokenSource.Token));
				_ = Task.Run(() => ReceiveCommandDataAsync(cancellationTokenSource.Token));
			}
			catch (Exception ex)
			{
				LogMessage($"Connection failed: {ex.Message}");
				MessageBox.Show($"Failed to connect: {ex.Message}", "Connection Error",
					MessageBoxButtons.OK, MessageBoxIcon.Error);

				DisconnectFromServer();
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
				StopRecording();

				isConnected = false;
				cancellationTokenSource?.Cancel();

				videoStream?.Close();
				videoClient?.Close();
				commandStream?.Close();
				commandClient?.Close();

				// Reset counters and buffers
				frameCount = 0;
				commandCount = 0;
				frameBuffer.Clear();
				expectingFrameHeader = true;
				expectedFrameSize = 0;
				lastChatMessage = "";
				lastChatTime = DateTime.MinValue;

				UpdateUI();
				LogMessage("Disconnected from server");

				// Clear preview
				pictureBoxVideo.Image?.Dispose();
				pictureBoxVideo.Image = null;
			}
			catch (Exception ex)
			{
				LogMessage($"Error during disconnection: {ex.Message}");
			}
		}

		#endregion

		#region Video Stream Processing

		private async Task ReceiveVideoDataAsync(CancellationToken cancellationToken)
		{
			byte[] buffer = new byte[65536]; // Increased buffer size for video frames

			try
			{
				LogMessage("Starting video data reception");

				while (isConnected && !cancellationToken.IsCancellationRequested)
				{
					if (videoStream != null && videoStream.CanRead)
					{
						int bytesRead = await videoStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

						if (bytesRead == 0)
						{
							Invoke(() => LogMessage("Video stream closed by server"));
							break;
						}

						// Add received data to frame buffer
						for (int i = 0; i < bytesRead; i++)
						{
							frameBuffer.Add(buffer[i]);
						}

						// Process complete frames from buffer
						await ProcessFrameBufferAsync(cancellationToken);
					}
					else
					{
						await Task.Delay(10, cancellationToken);
					}
				}
			}
			catch (OperationCanceledException)
			{
				LogMessage("Video reception cancelled");
			}
			catch (Exception ex)
			{
				Invoke(() => LogMessage($"Video stream error: {ex.Message}"));
			}
		}

		private async Task ProcessFrameBufferAsync(CancellationToken cancellationToken)
		{
			try
			{
				while (frameBuffer.Count > 0)
				{
					if (expectingFrameHeader)
					{
						// Look for frame header/size information
						if (TryParseFrameHeader(out int frameSize))
						{
							expectedFrameSize = frameSize;
							expectingFrameHeader = false;
						}
						else
						{
							break; // Not enough data for header
						}
					}
					else
					{
						// Check if we have complete frame data
						if (frameBuffer.Count >= expectedFrameSize)
						{
							var frameData = frameBuffer.Take(expectedFrameSize).ToArray();
							frameBuffer.RemoveRange(0, expectedFrameSize);

							await ProcessCompleteVideoFrameAsync(frameData, cancellationToken);

							expectingFrameHeader = true;
							expectedFrameSize = 0;
						}
						else
						{
							break; // Not enough data for complete frame
						}
					}
				}
			}
			catch (Exception ex)
			{
				Invoke(() => LogMessage($"Error processing frame buffer: {ex.Message}"));
			}
		}

		private bool TryParseFrameHeader(out int frameSize)
		{
			frameSize = 0;

			// Method 1: Look for JPEG header and try to determine size
			if (frameBuffer.Count >= 2 && frameBuffer[0] == 0xFF && frameBuffer[1] == 0xD8)
			{
				// Find JPEG end marker (0xFF 0xD9)
				for (int i = 2; i < frameBuffer.Count - 1; i++)
				{
					if (frameBuffer[i] == 0xFF && frameBuffer[i + 1] == 0xD9)
					{
						frameSize = i + 2;
						return true;
					}
				}
				return false; // JPEG not complete yet
			}

			// Method 2: Look for PNG header and try to determine size
			if (frameBuffer.Count >= 8 &&
				frameBuffer[0] == 0x89 && frameBuffer[1] == 0x50 &&
				frameBuffer[2] == 0x4E && frameBuffer[3] == 0x47)
			{
				// Look for PNG end chunk (IEND)
				for (int i = 8; i < frameBuffer.Count - 7; i++)
				{
					if (frameBuffer[i] == 0x49 && frameBuffer[i + 1] == 0x45 &&
						frameBuffer[i + 2] == 0x4E && frameBuffer[i + 3] == 0x44)
					{
						frameSize = i + 8; // Include CRC
						return true;
					}
				}
				return false; // PNG not complete yet
			}

			// Method 3: If using a custom protocol with size header
			if (frameBuffer.Count >= 4)
			{
				// Assuming first 4 bytes are frame size (little-endian)
				frameSize = BitConverter.ToInt32(frameBuffer.Take(4).ToArray(), 0);
				if (frameSize > 0 && frameSize < 10 * 1024 * 1024) // Sanity check: < 10MB
				{
					frameBuffer.RemoveRange(0, 4); // Remove header
					return true;
				}
			}

			// Method 4: Assume fixed raw frame size (fallback)
			int rawFrameSize = 640 * 480 * 3; // BGR format
			if (frameBuffer.Count >= rawFrameSize)
			{
				frameSize = rawFrameSize;
				return true;
			}

			return false;
		}

		private async Task ProcessCompleteVideoFrameAsync(byte[] frameData, CancellationToken cancellationToken)
		{
			try
			{
				Mat? frame = null;

				// Method 1: Try to decode as image (JPEG, PNG, etc.)
				if (IsImageData(frameData))
				{
					using var memoryStream = new MemoryStream(frameData);
					try
					{
						using var bitmap = new Bitmap(memoryStream);
						frame = BitmapConverter.ToMat(bitmap);
					}
					catch (Exception ex)
					{
						LogMessage($"Image decode failed: {ex.Message}");
						frame = null;
					}
				}

				// Method 2: Try as raw pixel data if image decode failed
				if (frame == null && IsRawPixelData(frameData))
				{
					int width = 640;  // Adjust based on your stream
					int height = 480; // Adjust based on your stream

					if (frameData.Length >= width * height * 3)
					{
						try
						{
							frame = new Mat(height, width, MatType.CV_8UC3);
							Marshal.Copy(frameData, 0, frame.Data, Math.Min(frameData.Length, width * height * 3));
							LogMessage($"Processed raw frame: {frame.Width}x{frame.Height}");
						}
						catch (Exception ex)
						{
							LogMessage($"Raw frame processing failed: {ex.Message}");
							frame?.Dispose();
							frame = null;
						}
					}
				}

				// Method 3: Create visualization frame if unable to decode
				if (frame == null)
				{
					frame = CreateVisualizationFrame(frameData);
					LogMessage("Using visualization frame (no video data decoded)");
				}

				// Ensure frame is the correct size for video writer
				if (frame != null)
				{
					// Resize frame to match video writer size if needed
					if (isRecording && videoWriter != null &&
						(frame.Width != 640 || frame.Height != 480))
					{
						var resizedFrame = new Mat();
						Cv2.Resize(frame, resizedFrame, new OpenCvSharp.Size(640, 480));
						frame.Dispose();
						frame = resizedFrame;
					}

					AddFrameOverlays(frame);

					// Write frame to video file if recording
					if (isRecording && videoWriter != null && videoWriter.IsOpened())
					{
						try
						{
							videoWriter.Write(frame);
							// Force flush to ensure frame is written
							// Note: OpenCV doesn't have a direct flush, but this ensures the write operation completes
						}
						catch (Exception ex)
						{
							LogMessage($"Error writing frame to video: {ex.Message}");
						}
					}

					// Update preview
					Invoke(() => UpdateVideoPreview(frame));

					// Update frame count and time
					Invoke(() =>
					{
						frameCount++;
						lastFrameTime = DateTime.Now;
					});

					frame.Dispose();
				}

				await Task.CompletedTask;
			}
			catch (Exception ex)
			{
				Invoke(() => LogMessage($"Error processing complete video frame: {ex.Message}"));
			}
		}

		private Mat CreateVisualizationFrame(byte[] frameData)
		{
			var frame = new Mat(480, 640, MatType.CV_8UC3, new Scalar(30, 30, 30));

			// Add header info
			Cv2.PutText(frame, "Video Stream Data", new OpenCvSharp.Point(20, 40),
				HersheyFonts.HersheySimplex, 1.2, new Scalar(0, 255, 0), 2);

			// Show data statistics
			Cv2.PutText(frame, $"Data size: {frameData.Length} bytes", new OpenCvSharp.Point(20, 80),
				HersheyFonts.HersheySimplex, 0.8, new Scalar(255, 255, 255), 2);

			Cv2.PutText(frame, $"Frame count: {frameCount + 1}", new OpenCvSharp.Point(20, 120),
				HersheyFonts.HersheySimplex, 0.8, new Scalar(255, 255, 255), 2);

			// Show first 64 bytes as a visual pattern
			Cv2.PutText(frame, "Data pattern:", new OpenCvSharp.Point(20, 160),
				HersheyFonts.HersheySimplex, 0.6, new Scalar(200, 200, 200), 1);

			for (int i = 0; i < Math.Min(64, frameData.Length); i++)
			{
				int x = 20 + (i % 32) * 18;
				int y = 180 + (i / 32) * 18;
				byte value = frameData[i];

				// Create colored square based on byte value
				var color = new Scalar(value, (255 - value), (value + 128) % 256);
				Cv2.Rectangle(frame, new OpenCvSharp.Point(x, y), new OpenCvSharp.Point(x + 15, y + 15), color, -1);
			}

			// Add hex dump of first 16 bytes
			var hexString = string.Join(" ", frameData.Take(16).Select(b => b.ToString("X2")));
			Cv2.PutText(frame, $"Hex: {hexString}", new OpenCvSharp.Point(20, 240),
				HersheyFonts.HersheySimplex, 0.5, new Scalar(180, 180, 180), 1);

			return frame;
		}

		private void AddFrameOverlays(Mat frame)
		{
			// Add timestamp
			var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
			Cv2.PutText(frame, timestamp, new OpenCvSharp.Point(10, frame.Height - 10),
				HersheyFonts.HersheySimplex, 0.6, new Scalar(255, 255, 255), 1);

			// Add recording indicator
			if (isRecording)
			{
				Cv2.Circle(frame, new OpenCvSharp.Point(frame.Width - 50, 50), 25, new Scalar(0, 0, 255), -1);
				Cv2.PutText(frame, "REC", new OpenCvSharp.Point(frame.Width - 70, 60),
					HersheyFonts.HersheySimplex, 0.8, new Scalar(255, 255, 255), 2);
			}

			// Add chat overlay if there's a recent message
			if (!string.IsNullOrEmpty(lastChatMessage) &&
				(DateTime.Now - lastChatTime).TotalSeconds < 15) // Show for 15 seconds
			{
				// Draw semi-transparent background for chat
				var chatBackground = new OpenCvSharp.Rect(10, 10, Math.Min(frame.Width - 20, 500), 50);
				Cv2.Rectangle(frame, chatBackground, new Scalar(0, 0, 0, 180), -1);

				// Draw chat text
				Cv2.PutText(frame, "CHAT:", new OpenCvSharp.Point(20, 35),
					HersheyFonts.HersheySimplex, 0.7, new Scalar(0, 255, 255), 2);

				// Truncate message if too long
				string displayMessage = lastChatMessage;
				if (displayMessage.Length > 60)
				{
					displayMessage = displayMessage.Substring(0, 57) + "...";
				}

				Cv2.PutText(frame, displayMessage, new OpenCvSharp.Point(80, 35),
					HersheyFonts.HersheySimplex, 0.6, new Scalar(255, 255, 255), 1);
			}
		}

		// Helper method to detect image data
		private bool IsImageData(byte[] buffer)
		{
			if (buffer.Length < 4) return false;

			// Check for JPEG signature
			if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
				return true;

			// Check for PNG signature
			if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47)
				return true;

			// Check for BMP signature
			if (buffer[0] == 0x42 && buffer[1] == 0x4D)
				return true;

			return false;
		}

		// Helper method to detect raw pixel data
		private bool IsRawPixelData(byte[] buffer)
		{
			// Check for common frame sizes
			int[] commonSizes = {
				640 * 480 * 3,    // VGA BGR
				640 * 480 * 4,    // VGA BGRA
				1280 * 720 * 3,   // HD BGR
				1280 * 720 * 4,   // HD BGRA
				1920 * 1080 * 3,  // Full HD BGR
				1920 * 1080 * 4   // Full HD BGRA
			};

			return commonSizes.Contains(buffer.Length);
		}

		#endregion

		#region Command Stream Processing

		private async Task ReceiveCommandDataAsync(CancellationToken cancellationToken)
		{
			byte[] buffer = new byte[1024];
			StringBuilder messageBuffer = new StringBuilder();

			try
			{
				LogMessage("Starting command data reception");

				while (isConnected && !cancellationToken.IsCancellationRequested)
				{
					if (commandStream != null && commandStream.CanRead)
					{
						int bytesRead = await commandStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

						if (bytesRead == 0)
						{
							Invoke(() => LogMessage("Command stream closed by server"));
							break;
						}

						string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
						messageBuffer.Append(data);

						// Process complete command messages
						ProcessCommandMessages(messageBuffer);
					}
					else
					{
						await Task.Delay(10, cancellationToken);
					}
				}
			}
			catch (OperationCanceledException)
			{
				LogMessage("Command reception cancelled");
			}
			catch (Exception ex)
			{
				Invoke(() => LogMessage($"Command stream error: {ex.Message}"));
			}
		}

		private void ProcessCommandMessages(StringBuilder messageBuffer)
		{
			try
			{
				string content = messageBuffer.ToString();
				string[] lines = content.Split('\n');

				for (int i = 0; i < lines.Length - 1; i++)
				{
					if (!string.IsNullOrWhiteSpace(lines[i]))
					{
						string commandLine = lines[i].Trim();

						var command = new CommandEntry
						{
							Timestamp = DateTime.UtcNow,
							Command = commandLine,
							Source = "command_stream"
						};

						// Check if this is a chat command echo
						ProcessChatCommand(commandLine);

						// Save to command log if recording
						if (isRecording && commandLogWriter != null)
						{
							SaveCommandToLog(command);
						}

						Invoke(() =>
						{
							commandCount++;
							LogMessage($"Command: {commandLine}");
						});
					}
				}

				// Keep the last incomplete line
				messageBuffer.Clear();
				if (lines.Length > 0)
				{
					messageBuffer.Append(lines[lines.Length - 1]);
				}
			}
			catch (Exception ex)
			{
				Invoke(() => LogMessage($"Error processing commands: {ex.Message}"));
			}
		}

		private void ProcessChatCommand(string commandJson)
		{
			try
			{
				var command = JsonSerializer.Deserialize<Dictionary<string, object>>(commandJson);

				if (command.ContainsKey("type"))
				{
					string type = command["type"].ToString();

					if (type.Equals("command_echo", StringComparison.OrdinalIgnoreCase))
					{
						// Process echoed command
						if (command.ContainsKey("original_command"))
						{
							string originalCommand = command["original_command"].ToString();
							var originalCommandObj = JsonSerializer.Deserialize<Dictionary<string, object>>(originalCommand);

							if (originalCommandObj.ContainsKey("type") &&
								originalCommandObj["type"].ToString().Equals("CHAT", StringComparison.OrdinalIgnoreCase))
							{
								// Extract chat message
								string message = originalCommandObj.ContainsKey("message") ?
									originalCommandObj["message"].ToString() : "";
								string sender = originalCommandObj.ContainsKey("sender") ?
									originalCommandObj["sender"].ToString() : "Unknown";

								Invoke(() =>
								{
									lastChatMessage = $"{sender}: {message}";
									lastChatTime = DateTime.Now;
									LogMessage($"Chat: {lastChatMessage}");
								});
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				// Log parsing errors but don't crash
				LogMessage($"Error parsing command for chat: {ex.Message}");
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
				var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
				var outputDir = textBoxOutputPath.Text;

				if (!Directory.Exists(outputDir))
				{
					Directory.CreateDirectory(outputDir);
				}

				// Initialize video recording with raw codec
				currentVideoFile = Path.Combine(outputDir, $"video_{timestamp}.avi");

				// Try multiple raw codec options
				bool writerInitialized = false;

				// Option 1: Uncompressed RGB
				int fourcc = VideoWriter.FourCC('D', 'I', 'B', ' '); // DIB (raw RGB)
				videoWriter = new VideoWriter(currentVideoFile, fourcc, 30.0, new OpenCvSharp.Size(640, 480), true);

				if (videoWriter.IsOpened())
				{
					writerInitialized = true;
					LogMessage("Video writer initialized with DIB codec");
				}

				if (!writerInitialized)
				{
					// Option 2: Try raw (0)
					videoWriter?.Release();
					videoWriter = new VideoWriter(currentVideoFile, 0, 30.0, new OpenCvSharp.Size(640, 480), true);

					if (videoWriter.IsOpened())
					{
						writerInitialized = true;
						LogMessage("Video writer initialized with raw codec (0)");
					}
				}

				if (!writerInitialized)
				{
					// Option 3: Use MJPG as reliable fallback (nearly lossless)
					videoWriter?.Release();
					fourcc = VideoWriter.FourCC('M', 'J', 'P', 'G');
					videoWriter = new VideoWriter(currentVideoFile, fourcc, 30.0, new OpenCvSharp.Size(640, 480), true);

					if (videoWriter.IsOpened())
					{
						writerInitialized = true;
						LogMessage("Video writer initialized with MJPG codec (fallback)");
					}
				}

				if (!writerInitialized)
				{
					videoWriter?.Release();
					videoWriter = null;
					throw new Exception("Failed to initialize video writer with any codec");
				}

				// Initialize command recording
				currentCommandFile = Path.Combine(outputDir, $"commands_{timestamp}.json");
				commandLogWriter = new StreamWriter(currentCommandFile, false, Encoding.UTF8);
				commandLogWriter.WriteLine("[");
				isFirstCommand = true;

				isRecording = true;
				recordingStartTime = DateTime.Now;

				UpdateUI();
				LogMessage("Recording started");
				LogMessage($"Video file: {currentVideoFile}");
				LogMessage($"Command file: {currentCommandFile}");
			}
			catch (Exception ex)
			{
				LogMessage($"Error starting recording: {ex.Message}");
				MessageBox.Show($"Error starting recording: {ex.Message}", "Recording Error",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
				StopRecording();
			}
		}

		private void StopRecording()
		{
			if (!isRecording) return;

			try
			{
				isRecording = false;

				// Close video writer with proper flushing
				if (videoWriter != null)
				{
					try
					{
						// Ensure all frames are written before closing
						videoWriter.Release();
						videoWriter.Dispose();
						videoWriter = null;

						// Add small delay to ensure file is fully written
						Thread.Sleep(100);

						if (File.Exists(currentVideoFile!))
						{
							var fileInfo = new FileInfo(currentVideoFile);
							LogMessage($"Video recording saved: {currentVideoFile}");
							LogMessage($"Video file size: {fileInfo.Length / 1024.0:F2} KB");
							LogMessage($"Frames recorded: {frameCount}");

							// Verify file has content
							if (fileInfo.Length == 0)
							{
								LogMessage("WARNING: Video file is empty - no frames were written");
							}
							else if (fileInfo.Length < 1000)
							{
								LogMessage("WARNING: Video file is very small - may contain no valid frames");
							}
						}
					}
					catch (Exception videoEx)
					{
						LogMessage($"Error closing video writer: {videoEx.Message}");
					}
				}

				// Close command log
				if (commandLogWriter != null)
				{
					try
					{
						commandLogWriter.WriteLine("\n]");
						commandLogWriter.Close();
						commandLogWriter.Dispose();
						commandLogWriter = null;
						LogMessage($"Command log saved: {currentCommandFile}");
					}
					catch (Exception cmdEx)
					{
						LogMessage($"Error closing command log: {cmdEx.Message}");
					}
				}

				var duration = DateTime.Now - recordingStartTime;
				LogMessage($"Recording stopped. Duration: {duration:hh\\:mm\\:ss}");
				LogMessage($"Total frames processed: {frameCount}, Commands: {commandCount}");

				UpdateUI();
			}
			catch (Exception ex)
			{
				LogMessage($"Error stopping recording: {ex.Message}");
			}
		}

		private void SaveCommandToLog(CommandEntry command)
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
				LogMessage($"Error saving command: {ex.Message}");
			}
		}

		#endregion

		#region UI Management

		private void ButtonBrowseOutput_Click(object sender, EventArgs e)
		{
			folderBrowserDialog.SelectedPath = textBoxOutputPath.Text;

			if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
			{
				textBoxOutputPath.Text = folderBrowserDialog.SelectedPath;
				LogMessage($"Output directory: {folderBrowserDialog.SelectedPath}");
			}
		}
		private void UpdateUI()
		{
			if (InvokeRequired)
			{
				Invoke(UpdateUI);
				return;
			}

			// Connection UI
			if (isConnected)
			{
				buttonConnect.Text = "Disconnect";
				buttonConnect.BackColor = Color.LightCoral;
				labelConnectionStatus.Text = "Connected to both streams";
				labelConnectionStatus.ForeColor = Color.Green;
				buttonStartRecording.Enabled = true;
			}
			else
			{
				buttonConnect.Text = "Connect";
				buttonConnect.BackColor = Color.LightGreen;
				labelConnectionStatus.Text = "Disconnected";
				labelConnectionStatus.ForeColor = Color.Red;
				buttonStartRecording.Enabled = false;
			}

			// Recording UI
			buttonStopRecording.Enabled = isRecording;
			groupBoxConnection.Enabled = !isRecording;
			textBoxOutputPath.Enabled = !isRecording;
			buttonBrowseOutput.Enabled = !isRecording;

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
		}

		private void UpdateStatusDisplay()
		{
			if (InvokeRequired)
			{
				Invoke(UpdateStatusDisplay);
				return;
			}

			labelFrameCount.Text = $"Frames: {frameCount}";
			labelCommandCount.Text = $"Commands: {commandCount}";

			if (isRecording)
			{
				var elapsed = DateTime.Now - recordingStartTime;
				labelRecordingTime.Text = $"Time: {elapsed:hh\\:mm\\:ss}";
			}
			else
			{
				labelRecordingTime.Text = "Time: 00:00:00";
			}

			// Update video status based on recent frames
			if (DateTime.Now - lastFrameTime < TimeSpan.FromSeconds(2) && frameCount > 0)
			{
				labelVideoStatus.Text = $"Live video - {frameCount} frames";
				labelVideoStatus.ForeColor = Color.Green;
			}
			else if (isConnected)
			{
				labelVideoStatus.Text = "Connected - No video frames";
				labelVideoStatus.ForeColor = Color.Orange;
			}
			else
			{
				labelVideoStatus.Text = "No video signal";
				labelVideoStatus.ForeColor = Color.Red;
			}
		}

		private void UpdateVideoPreview(Mat frame)
		{
			try
			{
				if (InvokeRequired)
				{
					Invoke(() => UpdateVideoPreview(frame));
					return;
				}

				if (frame == null || frame.Empty())
				{
					return;
				}

				// Convert OpenCV Mat to Bitmap for display
				using var bitmap = BitmapConverter.ToBitmap(frame);

				// Dispose previous image
				pictureBoxVideo.Image?.Dispose();

				// Set new image (create a copy since the bitmap will be disposed)
				pictureBoxVideo.Image = new Bitmap(bitmap);
			}
			catch (Exception ex)
			{
				LogMessage($"Error updating preview: {ex.Message}");
			}
		}

		private void LogMessage(string message)
		{
			if (InvokeRequired)
			{
				Invoke(() => LogMessage(message));
				return;
			}

			var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
			var logEntry = $"[{timestamp}] {message}";

			textBoxLog.AppendText(logEntry + Environment.NewLine);
			textBoxLog.SelectionStart = textBoxLog.Text.Length;
			textBoxLog.ScrollToCaret();
		}

		#endregion

		#region Cleanup

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				try
				{
					StopRecording();
					DisconnectFromServer();

					currentFrame?.Dispose();
					videoWriter?.Dispose();
					commandLogWriter?.Dispose();
					videoStream?.Dispose();
					videoClient?.Dispose();
					commandStream?.Dispose();
					commandClient?.Dispose();
					cancellationTokenSource?.Dispose();
					pictureBoxVideo.Image?.Dispose();
					components?.Dispose();
				}
				catch (Exception ex)
				{
					// Log disposal error but don't throw
					try
					{
						LogMessage($"Disposal error: {ex.Message}");
					}
					catch
					{
						// Ignore if logging fails during disposal
					}
				}
			}
			base.Dispose(disposing);
		}

		#endregion
	}
}

// Command entry class for JSON serialization
public class CommandEntry
{
	public DateTime Timestamp { get; set; }
	public string Command { get; set; } = string.Empty;
	public string Source { get; set; } = string.Empty;
}