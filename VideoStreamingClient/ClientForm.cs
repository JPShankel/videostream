using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using Timer = System.Windows.Forms.Timer;

namespace VideoStreamingClient
{
	public partial class ClientForm : Form
	{
		private TcpClient tcpClient;
		private TcpClient commandClient;
		private NetworkStream stream;
		private NetworkStream commandStream;
		private Thread receiveThread;
		private Thread commandReceiveThread;
		private Thread reconnectThread;
		private bool isConnected = false;
		private bool isCommandConnected = false;
		private bool isPaused = false;
		private bool shouldReconnect = false;
		private bool isReconnecting = false;

		private TextBox serverIpTextBox;
		private NumericUpDown videoPortNumeric;
		private NumericUpDown commandPortNumeric;
		private Button connectButton;
		private Button disconnectButton;
		private Button togglePauseButton;
		private PictureBox videoPictureBox;
		private Label statusLabel;
		private Label fpsLabel;
		private Label chatDisplayLabel;
		private Label networkStatsLabel;
		private Panel controlPanel;
		private Panel chatPanel;
		private TextBox chatTextBox;
		private Button sendChatButton;
		private CheckBox autoReconnectCheckBox;
		private Label connectionQualityLabel;
		private ProgressBar bufferHealthBar;

		// Network resilience
		private int frameCount = 0;
		private int droppedFrames = 0;
		private int reconnectAttempts = 0;
		private DateTime lastFpsUpdate = DateTime.Now;
		private DateTime lastFrameReceived = DateTime.MinValue;
		private DateTime lastCommandReceived = DateTime.MinValue;
		private string lastChatMessage = "";
		private DateTime lastChatTime = DateTime.MinValue;
		private Queue<byte[]> frameBuffer = new Queue<byte[]>();
		private readonly object bufferLock = new object();
		private const int MAX_BUFFER_SIZE = 10;
		private Timer connectionMonitorTimer;
		private Timer bufferDisplayTimer;

		// Connection quality metrics
		private Queue<DateTime> recentFrameTimes = new Queue<DateTime>();
		private Queue<TimeSpan> latencyMeasurements = new Queue<TimeSpan>();
		private DateTime lastPingTime = DateTime.MinValue;

		public ClientForm()
		{
			InitializeComponent();
			InitializeTimers();
		}

		private void InitializeTimers()
		{
			// Monitor connection health every 2 seconds
			connectionMonitorTimer = new Timer();
			connectionMonitorTimer.Interval = 2000;
			connectionMonitorTimer.Tick += ConnectionMonitor_Tick;
			connectionMonitorTimer.Start();

			// Update buffer display every 500ms
			bufferDisplayTimer = new Timer();
			bufferDisplayTimer.Interval = 500;
			bufferDisplayTimer.Tick += BufferDisplay_Tick;
			bufferDisplayTimer.Start();
		}

		private void InitializeComponent()
		{
			this.Size = new Size(900, 700);
			this.Text = "Resilient Video Streaming Client";
			this.StartPosition = FormStartPosition.CenterScreen;
			this.MinimumSize = new Size(600, 500);

			// Main control panel at the top
			controlPanel = new Panel
			{
				Location = new Point(0, 0),
				Size = new Size(900, 100),
				BackColor = Color.LightGray,
				Dock = DockStyle.Top
			};

			// Server IP input
			Label ipLabel = new Label
			{
				Text = "Server IP:",
				Location = new Point(10, 15),
				Size = new Size(60, 20),
				Font = new Font("Arial", 9, FontStyle.Regular)
			};

			serverIpTextBox = new TextBox
			{
				Text = "127.0.0.1",
				Location = new Point(80, 12),
				Size = new Size(120, 23),
				Font = new Font("Arial", 9, FontStyle.Regular)
			};

			// Video port input
			Label videoPortLabel = new Label
			{
				Text = "Video Port:",
				Location = new Point(10, 45),
				Size = new Size(65, 20),
				Font = new Font("Arial", 9, FontStyle.Regular)
			};

			videoPortNumeric = new NumericUpDown
			{
				Location = new Point(80, 42),
				Size = new Size(80, 23),
				Font = new Font("Arial", 9, FontStyle.Regular),
				Minimum = 1,
				Maximum = 65535,
				Value = 8080
			};

			// Command port input
			Label commandPortLabel = new Label
			{
				Text = "Cmd Port:",
				Location = new Point(170, 45),
				Size = new Size(60, 20),
				Font = new Font("Arial", 9, FontStyle.Regular)
			};

			commandPortNumeric = new NumericUpDown
			{
				Location = new Point(235, 42),
				Size = new Size(80, 23),
				Font = new Font("Arial", 9, FontStyle.Regular),
				Minimum = 1,
				Maximum = 65535,
				Value = 8081
			};

			connectButton = new Button
			{
				Text = "Connect",
				Location = new Point(330, 15),
				Size = new Size(100, 27),
				Font = new Font("Arial", 9, FontStyle.Bold),
				BackColor = Color.LightGreen
			};
			connectButton.Click += Connect_Click;

			disconnectButton = new Button
			{
				Text = "Disconnect",
				Location = new Point(330, 45),
				Size = new Size(100, 27),
				Font = new Font("Arial", 9, FontStyle.Bold),
				BackColor = Color.LightCoral,
				Enabled = false
			};
			disconnectButton.Click += Disconnect_Click;

			togglePauseButton = new Button
			{
				Text = "Toggle Pause",
				Location = new Point(450, 30),
				Size = new Size(100, 27),
				Font = new Font("Arial", 9, FontStyle.Bold),
				BackColor = Color.Orange,
				Enabled = false
			};
			togglePauseButton.Click += TogglePause_Click;

			autoReconnectCheckBox = new CheckBox
			{
				Text = "Auto-Reconnect",
				Location = new Point(10, 75),
				Size = new Size(120, 20),
				Font = new Font("Arial", 9, FontStyle.Regular),
				Checked = true
			};

			statusLabel = new Label
			{
				Text = "Disconnected",
				Location = new Point(570, 15),
				Size = new Size(150, 20),
				Font = new Font("Arial", 9, FontStyle.Regular),
				ForeColor = Color.Red
			};

			fpsLabel = new Label
			{
				Text = "FPS: 0",
				Location = new Point(570, 35),
				Size = new Size(80, 20),
				Font = new Font("Arial", 9, FontStyle.Regular),
				ForeColor = Color.Blue
			};

			connectionQualityLabel = new Label
			{
				Text = "Quality: Unknown",
				Location = new Point(570, 55),
				Size = new Size(120, 20),
				Font = new Font("Arial", 9, FontStyle.Regular),
				ForeColor = Color.Gray
			};

			networkStatsLabel = new Label
			{
				Text = "Network: No data",
				Location = new Point(140, 75),
				Size = new Size(200, 20),
				Font = new Font("Arial", 8, FontStyle.Italic),
				ForeColor = Color.Gray
			};

			// Buffer health indicator
			Label bufferLabel = new Label
			{
				Text = "Buffer:",
				Location = new Point(350, 75),
				Size = new Size(50, 20),
				Font = new Font("Arial", 8, FontStyle.Regular)
			};

			bufferHealthBar = new ProgressBar
			{
				Location = new Point(400, 75),
				Size = new Size(100, 15),
				Minimum = 0,
				Maximum = MAX_BUFFER_SIZE,
				Value = 0
			};

			controlPanel.Controls.AddRange(new Control[]
			{
				ipLabel, serverIpTextBox, videoPortLabel, videoPortNumeric,
				commandPortLabel, commandPortNumeric, connectButton,
				disconnectButton, togglePauseButton, autoReconnectCheckBox,
				statusLabel, fpsLabel, connectionQualityLabel, networkStatsLabel,
				bufferLabel, bufferHealthBar
			});

			// Chat panel
			chatPanel = new Panel
			{
				Location = new Point(0, 100),
				Size = new Size(900, 40),
				BackColor = Color.LightYellow,
				Dock = DockStyle.Top
			};

			Label chatLabel = new Label
			{
				Text = "Chat:",
				Location = new Point(10, 12),
				Size = new Size(40, 20),
				Font = new Font("Arial", 9, FontStyle.Bold)
			};

			chatTextBox = new TextBox
			{
				Location = new Point(55, 10),
				Size = new Size(400, 23),
				Font = new Font("Arial", 9, FontStyle.Regular),
				Enabled = false
			};
			chatTextBox.KeyPress += ChatTextBox_KeyPress;

			sendChatButton = new Button
			{
				Text = "Send",
				Location = new Point(465, 9),
				Size = new Size(60, 25),
				Font = new Font("Arial", 9, FontStyle.Regular),
				BackColor = Color.LightGreen,
				Enabled = false
			};
			sendChatButton.Click += SendChat_Click;

			chatDisplayLabel = new Label
			{
				Text = "No chat messages",
				Location = new Point(535, 12),
				Size = new Size(350, 20),
				Font = new Font("Arial", 9, FontStyle.Italic),
				ForeColor = Color.Gray,
				TextAlign = ContentAlignment.MiddleLeft
			};

			chatPanel.Controls.AddRange(new Control[]
			{
				chatLabel, chatTextBox, sendChatButton, chatDisplayLabel
			});

			// Video display area
			videoPictureBox = new PictureBox
			{
				BorderStyle = BorderStyle.FixedSingle,
				SizeMode = PictureBoxSizeMode.Zoom,
				BackColor = Color.Black,
				Dock = DockStyle.Fill
			};

			// Add placeholder text
			ShowPlaceholderText("Video will appear here when connected");

			this.Controls.AddRange(new Control[] { controlPanel, chatPanel, videoPictureBox });
		}

		private void ShowPlaceholderText(string text)
		{
			Label placeholderLabel = new Label
			{
				Text = text,
				ForeColor = Color.White,
				BackColor = Color.Transparent,
				Font = new Font("Arial", 12, FontStyle.Italic),
				TextAlign = ContentAlignment.MiddleCenter,
				Dock = DockStyle.Fill
			};
			videoPictureBox.Controls.Clear();
			videoPictureBox.Controls.Add(placeholderLabel);
		}

		private void ConnectionMonitor_Tick(object sender, EventArgs e)
		{
			UpdateConnectionQuality();
			CheckForStaleConnections();
		}

		private void BufferDisplay_Tick(object sender, EventArgs e)
		{
			lock (bufferLock)
			{
				bufferHealthBar.Value = Math.Min(frameBuffer.Count, MAX_BUFFER_SIZE);

				// Color code buffer health
				if (frameBuffer.Count < 2)
					bufferHealthBar.ForeColor = Color.Red;
				else if (frameBuffer.Count < 5)
					bufferHealthBar.ForeColor = Color.Orange;
				else
					bufferHealthBar.ForeColor = Color.Green;
			}
		}

		private void UpdateConnectionQuality()
		{
			DateTime now = DateTime.Now;

			// Calculate recent frame rate
			lock (bufferLock)
			{
				// Remove old frame times (older than 5 seconds)
				while (recentFrameTimes.Count > 0 && (now - recentFrameTimes.Peek()).TotalSeconds > 5)
				{
					recentFrameTimes.Dequeue();
				}
			}

			double recentFps = recentFrameTimes.Count / 5.0;

			// Determine connection quality
			string quality;
			Color qualityColor;

			if (!isConnected)
			{
				quality = "Disconnected";
				qualityColor = Color.Red;
			}
			else if (recentFps < 5)
			{
				quality = "Poor";
				qualityColor = Color.Red;
			}
			else if (recentFps < 10)
			{
				quality = "Fair";
				qualityColor = Color.Orange;
			}
			else if (recentFps < 15)
			{
				quality = "Good";
				qualityColor = Color.Blue;
			}
			else
			{
				quality = "Excellent";
				qualityColor = Color.Green;
			}

			connectionQualityLabel.Text = $"Quality: {quality}";
			connectionQualityLabel.ForeColor = qualityColor;

			// Update network stats
			double avgLatency = 0;
			if (latencyMeasurements.Count > 0)
			{
				avgLatency = latencyMeasurements.Average(l => l.TotalMilliseconds);
			}

			networkStatsLabel.Text = $"Frames: {frameCount}, Dropped: {droppedFrames}, Latency: {avgLatency:F0}ms";
		}

		private void CheckForStaleConnections()
		{
			DateTime now = DateTime.Now;

			// Check if video connection is stale (no frames for 5 seconds)
			if (isConnected && (now - lastFrameReceived).TotalSeconds > 5)
			{
				LogConnectionEvent("Video connection appears stale - no frames received");

				if (autoReconnectCheckBox.Checked && !isReconnecting)
				{
					TriggerReconnect("Stale video connection");
				}
			}

			// Check if command connection is stale (no activity for 10 seconds)
			/*
			if (isCommandConnected && (now - lastCommandReceived).TotalSeconds > 10)
			{
				LogConnectionEvent("Command connection appears stale");

				if (autoReconnectCheckBox.Checked && !isReconnecting)
				{
					TriggerReconnect("Stale command connection");
				}
			}
			*/
		}

		private void LogConnectionEvent(string message)
		{
			Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
		}

		private void TriggerReconnect(string reason)
		{
			if (isReconnecting) return;

			isReconnecting = true;
			LogConnectionEvent($"Triggering reconnect: {reason}");

			reconnectThread = new Thread(() => AttemptReconnect(reason));
			reconnectThread.IsBackground = true;
			reconnectThread.Start();
		}

		private void AttemptReconnect(string reason)
		{
			const int MAX_RECONNECT_ATTEMPTS = 5;
			const int RECONNECT_DELAY_MS = 3000;

			while (shouldReconnect && reconnectAttempts < MAX_RECONNECT_ATTEMPTS && !isConnected)
			{
				try
				{
					reconnectAttempts++;
					LogConnectionEvent($"Reconnect attempt {reconnectAttempts}/{MAX_RECONNECT_ATTEMPTS}");

					this.Invoke(new Action(() =>
					{
						statusLabel.Text = $"Reconnecting... ({reconnectAttempts}/{MAX_RECONNECT_ATTEMPTS})";
						statusLabel.ForeColor = Color.Orange;
					}));

					// Clean up existing connections
					CleanupConnections();

					Thread.Sleep(RECONNECT_DELAY_MS);

					// Attempt new connections
					if (ConnectToServers())
					{
						reconnectAttempts = 0;
						isReconnecting = false;
						LogConnectionEvent("Reconnection successful");

						this.Invoke(new Action(() =>
						{
							UpdateUIForConnection(true);
						}));
						return;
					}
				}
				catch (Exception ex)
				{
					LogConnectionEvent($"Reconnect attempt failed: {ex.Message}");
				}
			}

			// All reconnect attempts failed
			isReconnecting = false;
			shouldReconnect = false;

			this.Invoke(new Action(() =>
			{
				statusLabel.Text = "Reconnection failed";
				statusLabel.ForeColor = Color.Red;
				connectButton.Enabled = true;
			}));

			LogConnectionEvent($"Reconnection failed after {MAX_RECONNECT_ATTEMPTS} attempts");
		}

		private void ChatTextBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == (char)Keys.Enter)
			{
				e.Handled = true;
				SendChat_Click(sender, e);
			}
		}

		private void SendChat_Click(object sender, EventArgs e)
		{
			if (!isCommandConnected)
			{
				MessageBox.Show("Commands not connected.", "Not Connected",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			string message = chatTextBox.Text.Trim();
			if (string.IsNullOrEmpty(message))
			{
				return;
			}

			try
			{
				// Create chat command
				var command = new
				{
					type = "CHAT",
					message = message,
					timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
					clientId = Environment.MachineName,
					sender = Environment.UserName
				};

				string jsonCommand = JsonConvert.SerializeObject(command) + "\n";
				byte[] commandBytes = Encoding.UTF8.GetBytes(jsonCommand);

				// Send with retry logic
				if (SendCommandWithRetry(commandBytes))
				{
					chatTextBox.Clear();
				}
			}
			catch (Exception ex)
			{
				LogConnectionEvent($"Failed to send chat: {ex.Message}");
			}
		}

		private bool SendCommandWithRetry(byte[] commandBytes, int maxRetries = 3)
		{
			for (int attempt = 1; attempt <= maxRetries; attempt++)
			{
				try
				{
					if (isCommandConnected && commandStream != null)
					{
						commandStream.Write(commandBytes, 0, commandBytes.Length);
						commandStream.Flush();
						return true;
					}
				}
				catch (Exception ex)
				{
					LogConnectionEvent($"Command send attempt {attempt} failed: {ex.Message}");

					if (attempt < maxRetries)
					{
						Thread.Sleep(500 * attempt); // Exponential backoff
					}
				}
			}

			// All attempts failed - trigger reconnect if enabled
			if (autoReconnectCheckBox.Checked)
			{
				TriggerReconnect("Command send failed");
			}

			return false;
		}

		private void Connect_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrWhiteSpace(serverIpTextBox.Text))
			{
				MessageBox.Show("Please enter a server IP address.", "Invalid Input",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			connectButton.Enabled = false;
			shouldReconnect = true;
			reconnectAttempts = 0;

			if (ConnectToServers())
			{
				UpdateUIForConnection(true);
			}
			else
			{
				connectButton.Enabled = true;
			}
		}

		private bool ConnectToServers()
		{
			try
			{
				this.Invoke(new Action(() =>
				{
					statusLabel.Text = "Connecting...";
					statusLabel.ForeColor = Color.Orange;
					Application.DoEvents();
				}));

				string serverIP = "";
				int videoPort = 0;
				int commandPort = 0;

				this.Invoke(new Action(() =>
				{
					serverIP = serverIpTextBox.Text;
					videoPort = (int)videoPortNumeric.Value;
					commandPort = (int)commandPortNumeric.Value;
				}));

				// Connect to video stream with timeout
				tcpClient = new TcpClient();
				if (!tcpClient.ConnectAsync(serverIP, videoPort).Wait(5000))
				{
					throw new Exception("Video connection timeout");
				}
				stream = tcpClient.GetStream();
				stream.ReadTimeout = 10000; // 10 second read timeout
				stream.WriteTimeout = 5000;  // 5 second write timeout

				isConnected = true;
				receiveThread = new Thread(ReceiveVideo);
				receiveThread.IsBackground = true;
				receiveThread.Start();

				// Connect to command stream with timeout
				commandClient = new TcpClient();
				if (!commandClient.ConnectAsync(serverIP, commandPort).Wait(5000))
				{
					throw new Exception("Command connection timeout");
				}
				commandStream = commandClient.GetStream();
				commandStream.ReadTimeout = 10000;
				commandStream.WriteTimeout = 5000;

				isCommandConnected = true;
				commandReceiveThread = new Thread(ReceiveCommands);
				commandReceiveThread.IsBackground = true;
				commandReceiveThread.Start();

				// Clear video buffer and reset stats
				lock (bufferLock)
				{
					frameBuffer.Clear();
					recentFrameTimes.Clear();
					latencyMeasurements.Clear();
				}

				frameCount = 0;
				droppedFrames = 0;
				lastFrameReceived = DateTime.Now;
				lastCommandReceived = DateTime.Now;

				LogConnectionEvent("Connected successfully");
				return true;
			}
			catch (Exception ex)
			{
				CleanupConnections();
				LogConnectionEvent($"Connection failed: {ex.Message}");

				this.Invoke(new Action(() =>
				{
					statusLabel.Text = "Connection failed";
					statusLabel.ForeColor = Color.Red;
					MessageBox.Show($"Connection failed: {ex.Message}", "Connection Error",
						MessageBoxButtons.OK, MessageBoxIcon.Error);
				}));
				return false;
			}
		}

		private void UpdateUIForConnection(bool connected)
		{
			if (connected)
			{
				disconnectButton.Enabled = true;
				togglePauseButton.Enabled = true;
				chatTextBox.Enabled = true;
				sendChatButton.Enabled = true;
				serverIpTextBox.Enabled = false;
				videoPortNumeric.Enabled = false;
				commandPortNumeric.Enabled = false;
				statusLabel.Text = $"Connected (V:{videoPortNumeric.Value} C:{commandPortNumeric.Value})";
				statusLabel.ForeColor = Color.Green;
				videoPictureBox.Controls.Clear();
			}
			else
			{
				connectButton.Enabled = true;
				disconnectButton.Enabled = false;
				togglePauseButton.Enabled = false;
				chatTextBox.Enabled = false;
				sendChatButton.Enabled = false;
				serverIpTextBox.Enabled = true;
				videoPortNumeric.Enabled = true;
				commandPortNumeric.Enabled = true;
				statusLabel.Text = "Disconnected";
				statusLabel.ForeColor = Color.Red;
				ShowPlaceholderText("Video will appear here when connected");
			}
		}

		private void Disconnect_Click(object sender, EventArgs e)
		{
			shouldReconnect = false;
			DisconnectAll();
		}

		private void DisconnectAll()
		{
			shouldReconnect = false;
			isReconnecting = false;
			CleanupConnections();
			UpdateUIForConnection(false);

			fpsLabel.Text = "FPS: 0";
			connectionQualityLabel.Text = "Quality: Disconnected";
			connectionQualityLabel.ForeColor = Color.Red;
			networkStatsLabel.Text = "Network: Disconnected";

			isPaused = false;
			reconnectAttempts = 0;
			this.Text = "Resilient Video Streaming Client";
		}

		private void CleanupConnections()
		{
			// Cleanup video connection
			isConnected = false;
			try
			{
				stream?.Close();
				tcpClient?.Close();
			}
			catch { }

			if (receiveThread != null && receiveThread.IsAlive)
			{
				receiveThread.Join(2000);
			}

			// Cleanup command connection
			isCommandConnected = false;
			try
			{
				commandStream?.Close();
				commandClient?.Close();
			}
			catch { }

			if (commandReceiveThread != null && commandReceiveThread.IsAlive)
			{
				commandReceiveThread.Join(2000);
			}

			// Clear buffers
			lock (bufferLock)
			{
				while (frameBuffer.Count > 0)
				{
					frameBuffer.Dequeue();
				}
			}

			// Clear video display
			if (videoPictureBox.Image != null)
			{
				videoPictureBox.Image.Dispose();
				videoPictureBox.Image = null;
			}
		}

		private void ReceiveVideo()
		{
			byte[] sizeBuffer = new byte[4];
			int consecutiveErrors = 0;
			const int MAX_CONSECUTIVE_ERRORS = 10;

			while (isConnected && consecutiveErrors < MAX_CONSECUTIVE_ERRORS)
			{
				try
				{
					// Read frame size with timeout handling
					int bytesRead = 0;
					while (bytesRead < 4 && isConnected)
					{
						try
						{
							int read = stream.Read(sizeBuffer, bytesRead, 4 - bytesRead);
							if (read == 0)
							{
								throw new Exception("Connection closed by server");
							}
							bytesRead += read;
						}
						catch (IOException ex) when (ex.Message.Contains("timeout"))
						{
							// Handle read timeout gracefully
							LogConnectionEvent("Video read timeout - retrying");
							continue;
						}
					}

					if (!isConnected) break;

					int frameSize = BitConverter.ToInt32(sizeBuffer, 0);

					// Validate frame size with more lenient limits for poor networks
					if (frameSize <= 0 || frameSize > 15 * 1024 * 1024) // Max 15MB per frame
					{
						droppedFrames++;
						LogConnectionEvent($"Invalid frame size: {frameSize} bytes - skipping");
						continue;
					}

					// Read frame data with partial read handling
					byte[] frameData = new byte[frameSize];
					bytesRead = 0;
					DateTime frameStartTime = DateTime.Now;

					while (bytesRead < frameSize && isConnected)
					{
						try
						{
							int read = stream.Read(frameData, bytesRead, frameSize - bytesRead);
							if (read == 0)
							{
								throw new Exception("Connection closed during frame read");
							}
							bytesRead += read;

							// Check for extremely slow reads (indicates network issues)
							if ((DateTime.Now - frameStartTime).TotalSeconds > 10)
							{
								throw new Exception("Frame read timeout - network too slow");
							}
						}
						catch (IOException ex) when (ex.Message.Contains("timeout"))
						{
							LogConnectionEvent("Frame read timeout - continuing");
							break;
						}
					}

					if (bytesRead < frameSize)
					{
						droppedFrames++;
						LogConnectionEvent($"Incomplete frame received: {bytesRead}/{frameSize} bytes");
						continue;
					}

					// Successfully received complete frame
					lastFrameReceived = DateTime.Now;
					consecutiveErrors = 0;

					// Add to buffer for smooth playback during network hiccups
					lock (bufferLock)
					{
						frameBuffer.Enqueue(frameData);
						recentFrameTimes.Enqueue(DateTime.Now);

						// Limit buffer size to prevent memory issues
						while (frameBuffer.Count > MAX_BUFFER_SIZE)
						{
							frameBuffer.Dequeue();
						}

						// Clean old frame times
						while (recentFrameTimes.Count > 0 &&
							   (DateTime.Now - recentFrameTimes.Peek()).TotalSeconds > 5)
						{
							recentFrameTimes.Dequeue();
						}
					}

					// Process frame from buffer (smooths out network jitter)
					ProcessNextBufferedFrame();
				}
				catch (Exception ex)
				{
					consecutiveErrors++;
					droppedFrames++;
					LogConnectionEvent($"Video receive error ({consecutiveErrors}/{MAX_CONSECUTIVE_ERRORS}): {ex.Message}");

					if (consecutiveErrors < MAX_CONSECUTIVE_ERRORS)
					{
						// Wait before retry, with exponential backoff
						Thread.Sleep(Math.Min(5000, 500 * consecutiveErrors));
					}
				}
			}

			// Connection lost - attempt reconnect if enabled
			if (autoReconnectCheckBox.Checked && shouldReconnect && !isReconnecting)
			{
				TriggerReconnect("Video connection lost");
			}
		}

		private void ProcessNextBufferedFrame()
		{
			byte[] frameData = null;

			lock (bufferLock)
			{
				if (frameBuffer.Count > 0)
				{
					frameData = frameBuffer.Dequeue();
				}
			}

			if (frameData != null)
			{
				try
				{
					// Convert to image and display
					using (MemoryStream ms = new MemoryStream(frameData))
					{
						Image frame = Image.FromStream(ms);

						this.Invoke(new Action(() =>
						{
							if (videoPictureBox.Image != null)
							{
								videoPictureBox.Image.Dispose();
							}

							// Create a copy of the image with overlays
							Bitmap frameWithOverlays = new Bitmap(frame);
							using (Graphics g = Graphics.FromImage(frameWithOverlays))
							{
								DrawChatOverlay(g, frameWithOverlays.Width, frameWithOverlays.Height);
								DrawNetworkStatsOverlay(g, frameWithOverlays.Width, frameWithOverlays.Height);
							}

							videoPictureBox.Image = frameWithOverlays;
							frame.Dispose();

							// Update FPS counter
							frameCount++;
							if ((DateTime.Now - lastFpsUpdate).TotalSeconds >= 1.0)
							{
								double fps = frameCount / (DateTime.Now - lastFpsUpdate).TotalSeconds;
								fpsLabel.Text = $"FPS: {fps:F1}";
								frameCount = 0;
								lastFpsUpdate = DateTime.Now;
							}
						}));
					}
				}
				catch (Exception ex)
				{
					LogConnectionEvent($"Frame processing error: {ex.Message}");
					droppedFrames++;
				}
			}
		}

		private void ReceiveCommands()
		{
			byte[] buffer = new byte[4096];
			StringBuilder messageBuffer = new StringBuilder();
			int consecutiveErrors = 0;
			const int MAX_CONSECUTIVE_ERRORS = 5;

			while (isCommandConnected && consecutiveErrors < MAX_CONSECUTIVE_ERRORS)
			{
				try
				{
					if (!commandStream.DataAvailable)
					{
						continue;
					}
					int bytesRead = commandStream.Read(buffer, 0, buffer.Length);
					if (bytesRead == 0)
					{
						LogConnectionEvent("Command stream closed by server");
						break;
					}

					lastCommandReceived = DateTime.Now;
					consecutiveErrors = 0; // Reset error counter on successful read

					string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
					messageBuffer.Append(data);

					// Process complete messages (separated by newlines)
					string content = messageBuffer.ToString();
					string[] lines = content.Split('\n');

					for (int i = 0; i < lines.Length - 1; i++)
					{
						if (!string.IsNullOrWhiteSpace(lines[i]))
						{
							ProcessReceivedCommand(lines[i].Trim());
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
					consecutiveErrors++;
					LogConnectionEvent($"Command receive error ({consecutiveErrors}/{MAX_CONSECUTIVE_ERRORS}): {ex.Message}");

					if (consecutiveErrors < MAX_CONSECUTIVE_ERRORS)
					{
						Thread.Sleep(1000); // Wait before retry
					}
				}
			}

			// Connection lost - attempt reconnect if enabled
			if (autoReconnectCheckBox.Checked && shouldReconnect && !isReconnecting)
			{
				TriggerReconnect("Command connection lost");
			}
		}

		private void ProcessReceivedCommand(string commandJson)
		{
			try
			{
				var command = JsonConvert.DeserializeObject<Dictionary<string, object>>(commandJson);

				if (command.ContainsKey("type"))
				{
					string type = command["type"].ToString();

					if (type.Equals("command_echo", StringComparison.OrdinalIgnoreCase))
					{
						// Measure latency
						if (command.ContainsKey("server_time"))
						{
							long serverTime = Convert.ToInt64(command["server_time"]);
							long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
							TimeSpan latency = TimeSpan.FromMilliseconds(currentTime - serverTime);

							lock (bufferLock)
							{
								latencyMeasurements.Enqueue(latency);
								if (latencyMeasurements.Count > 10)
								{
									latencyMeasurements.Dequeue();
								}
							}
						}

						// Process echoed command
						if (command.ContainsKey("original_command"))
						{
							string originalCommand = command["original_command"].ToString();
							var originalCommandObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(originalCommand);

							if (originalCommandObj.ContainsKey("type") &&
								originalCommandObj["type"].ToString().Equals("CHAT", StringComparison.OrdinalIgnoreCase))
							{
								// Update chat display
								string message = originalCommandObj.ContainsKey("message") ?
									originalCommandObj["message"].ToString() : "";
								string sender = originalCommandObj.ContainsKey("sender") ?
									originalCommandObj["sender"].ToString() : "Unknown";

								this.Invoke(new Action(() =>
								{
									lastChatMessage = $"{sender}: {message}";
									lastChatTime = DateTime.Now;
									chatDisplayLabel.Text = $"Last: {lastChatMessage}";
									chatDisplayLabel.ForeColor = Color.DarkGreen;
								}));
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				// Log error but don't crash on parsing errors
				LogConnectionEvent($"Error processing command: {ex.Message}");
			}
		}

		private void TogglePause_Click(object sender, EventArgs e)
		{
			if (!isCommandConnected)
			{
				MessageBox.Show("Commands not connected.", "Not Connected",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			try
			{
				// Create toggle pause command
				var command = new
				{
					type = "TOGGLEPAUSE",
					timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
					clientId = Environment.MachineName
				};

				string jsonCommand = JsonConvert.SerializeObject(command) + "\n";
				byte[] commandBytes = Encoding.UTF8.GetBytes(jsonCommand);

				if (SendCommandWithRetry(commandBytes))
				{
					// Update button text to reflect current state
					isPaused = !isPaused;
					togglePauseButton.Text = isPaused ? "Resume Stream" : "Pause Stream";
					togglePauseButton.BackColor = isPaused ? Color.Red : Color.Orange;

					// Show status message
					this.Text = $"Resilient Video Streaming Client - {(isPaused ? "PAUSED" : "PLAYING")}";
				}
			}
			catch (Exception ex)
			{
				LogConnectionEvent($"Failed to send pause command: {ex.Message}");
			}
		}

		private void DrawChatOverlay(Graphics g, int width, int height)
		{
			if (!string.IsNullOrEmpty(lastChatMessage) &&
				(DateTime.Now - lastChatTime).TotalSeconds < 10) // Show chat for 10 seconds
			{
				// Draw semi-transparent background
				using (SolidBrush backgroundBrush = new SolidBrush(Color.FromArgb(180, Color.Black)))
				{
					using (Font chatFont = new Font("Arial", 12, FontStyle.Bold))
					{
						SizeF textSize = g.MeasureString(lastChatMessage, chatFont);
						RectangleF backgroundRect = new RectangleF(
							10, 10,
							Math.Min(textSize.Width + 20, width - 20),
							textSize.Height + 10
						);

						g.FillRectangle(backgroundBrush, backgroundRect);

						// Draw chat text
						using (SolidBrush textBrush = new SolidBrush(Color.White))
						{
							g.DrawString(lastChatMessage, chatFont, textBrush, 20, 15);
						}
					}
				}
			}
		}

		private void DrawNetworkStatsOverlay(Graphics g, int width, int height)
		{
			// Only show network stats if there are issues
			if (droppedFrames > 0 || isReconnecting)
			{
				using (SolidBrush backgroundBrush = new SolidBrush(Color.FromArgb(150, Color.DarkRed)))
				{
					Rectangle statsRect = new Rectangle(width - 200, height - 60, 190, 50);
					g.FillRectangle(backgroundBrush, statsRect);

					using (Pen borderPen = new Pen(Color.Yellow, 1))
					{
						g.DrawRectangle(borderPen, statsRect);
					}
				}

				using (Font statsFont = new Font("Arial", 8, FontStyle.Bold))
				using (SolidBrush textBrush = new SolidBrush(Color.White))
				{
					g.DrawString("NETWORK ISSUES", statsFont, textBrush, width - 195, height - 55);
					g.DrawString($"Dropped: {droppedFrames}", new Font("Arial", 7), textBrush, width - 195, height - 40);

					if (isReconnecting)
					{
						g.DrawString($"Reconnecting... ({reconnectAttempts})", new Font("Arial", 7),
							new SolidBrush(Color.Yellow), width - 195, height - 25);
					}
					else
					{
						int bufferCount = 0;
						lock (bufferLock) { bufferCount = frameBuffer.Count; }
						g.DrawString($"Buffer: {bufferCount}/{MAX_BUFFER_SIZE}", new Font("Arial", 7), textBrush, width - 195, height - 25);
					}
				}
			}
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			shouldReconnect = false;
			connectionMonitorTimer?.Stop();
			bufferDisplayTimer?.Stop();
			DisconnectAll();

			connectionMonitorTimer?.Dispose();
			bufferDisplayTimer?.Dispose();

			base.OnFormClosing(e);
		}
	}
}