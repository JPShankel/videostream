using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Concurrent;

// Alternative: Use AForge.NET instead of DirectShow
// Install-Package AForge.Video
// Install-Package AForge.Video.DirectShow
using AForge.Video;
using AForge.Video.DirectShow;
using DirectShowLib;

namespace VideoStreamingServer
{
	public partial class ServerForm : Form
	{
		private TcpListener tcpListener;
		private TcpListener commandListener;
		private Thread serverThread;
		private Thread commandThread;
		private bool isStreaming = false;
		private Button startButton;
		private Button stopButton;
		private Label statusLabel;
		private Label clientCountLabel;
		private Label commandStatusLabel;
		private ComboBox cameraComboBox;
		private RichTextBox commandLogTextBox;
		private int connectedClients = 0;
		private int connectedCommandClients = 0;
		private const int VIDEO_PORT = 8080;
		private const int COMMAND_PORT = 8081;

		// Network simulation controls
		private GroupBox networkSimulationGroup;
		private CheckBox enableNetworkSimulationCheckBox;
		private TrackBar latencyTrackBar;
		private TrackBar packetLossTrackBar;
		private TrackBar bandwidthTrackBar;
		private CheckBox enableDropoutCheckBox;
		private Label latencyLabel;
		private Label packetLossLabel;
		private Label bandwidthLabel;
		private Label dropoutLabel;

		// Network simulation state
		private Random networkRandom = new Random();
		private bool networkSimulationEnabled = false;
		private int latencyMs = 0;
		private int packetLossPercent = 0;
		private int bandwidthLimitKbps = 0; // 0 = unlimited
		private bool enableRandomDropouts = false;
		private DateTime lastDropoutTime = DateTime.MinValue;
		private int dropoutDurationMs = 0;

		// AForge.NET objects (alternative to DirectShow)
		private FilterInfoCollection videoDevices;
		private VideoCaptureDevice videoSource;
		private readonly object frameLock = new object();
		private byte[] currentFrame;
		private bool hasNewFrame = false;

		// Command handling
		private readonly object commandLock = new object();
		private Queue<string> pendingCommands = new Queue<string>();
		private bool streamPaused = false;

		// Persistent chat message storage
		private readonly object lastChatLock = new object();
		private string lastChatMessage = "";
		private string lastChatSender = "";
		private DateTime lastChatTimestamp = DateTime.MinValue;

		// Command client management for echoing
		private ConcurrentBag<TcpClient> commandClients = new ConcurrentBag<TcpClient>();
		private readonly object commandClientsLock = new object();

		public ServerForm()
		{
			InitializeComponent();
			LoadCameraDevices();
		}

		private void InitializeComponent()
		{
			this.Size = new Size(700, 650);
			this.Text = "Camera Video Streaming Server with Commands";
			this.StartPosition = FormStartPosition.CenterScreen;
			this.FormBorderStyle = FormBorderStyle.Sizable;
			this.MinimumSize = new Size(700, 650);

			// Title label
			Label titleLabel = new Label
			{
				Text = "Camera Video Streaming Server",
				Font = new Font("Arial", 14, FontStyle.Bold),
				Location = new Point(50, 20),
				Size = new Size(600, 25),
				TextAlign = ContentAlignment.MiddleCenter
			};

			// Camera selection
			Label cameraLabel = new Label
			{
				Text = "Select Camera:",
				Location = new Point(50, 60),
				Size = new Size(100, 20),
				Font = new Font("Arial", 10, FontStyle.Regular)
			};

			cameraComboBox = new ComboBox
			{
				Location = new Point(160, 58),
				Size = new Size(200, 25),
				DropDownStyle = ComboBoxStyle.DropDownList
			};

			startButton = new Button
			{
				Text = "Start Server",
				Location = new Point(50, 100),
				Size = new Size(120, 35),
				Font = new Font("Arial", 10, FontStyle.Regular),
				BackColor = Color.LightGreen
			};
			startButton.Click += StartServer_Click;

			stopButton = new Button
			{
				Text = "Stop Server",
				Location = new Point(200, 100),
				Size = new Size(120, 35),
				Font = new Font("Arial", 10, FontStyle.Regular),
				BackColor = Color.LightCoral,
				Enabled = false
			};
			stopButton.Click += StopServer_Click;

			statusLabel = new Label
			{
				Text = "Server stopped",
				Location = new Point(50, 160),
				Size = new Size(500, 20),
				Font = new Font("Arial", 10, FontStyle.Regular)
			};

			clientCountLabel = new Label
			{
				Text = "Video clients: 0",
				Location = new Point(50, 185),
				Size = new Size(200, 20),
				Font = new Font("Arial", 10, FontStyle.Regular)
			};

			commandStatusLabel = new Label
			{
				Text = "Command clients: 0",
				Location = new Point(280, 185),
				Size = new Size(200, 20),
				Font = new Font("Arial", 10, FontStyle.Regular)
			};

			// Port info labels
			Label videoPortLabel = new Label
			{
				Text = $"Video port: {VIDEO_PORT}",
				Location = new Point(50, 210),
				Size = new Size(200, 20),
				Font = new Font("Arial", 9, FontStyle.Italic),
				ForeColor = Color.Gray
			};

			Label commandPortLabel = new Label
			{
				Text = $"Command port: {COMMAND_PORT}",
				Location = new Point(280, 210),
				Size = new Size(200, 20),
				Font = new Font("Arial", 9, FontStyle.Italic),
				ForeColor = Color.Gray
			};

			// Network simulation group
			CreateNetworkSimulationControls();

			// Command log section
			Label commandLogLabel = new Label
			{
				Text = "Command Log:",
				Location = new Point(50, 445),
				Size = new Size(100, 20),
				Font = new Font("Arial", 10, FontStyle.Bold)
			};

			commandLogTextBox = new RichTextBox
			{
				Location = new Point(50, 470),
				Size = new Size(600, 130),
				Font = new Font("Consolas", 9, FontStyle.Regular),
				BackColor = Color.Black,
				ForeColor = Color.LimeGreen,
				ReadOnly = true,
				ScrollBars = RichTextBoxScrollBars.Vertical
			};

			this.Controls.AddRange(new Control[]
			{
				titleLabel, cameraLabel, cameraComboBox, startButton, stopButton,
				statusLabel, clientCountLabel, commandStatusLabel,
				videoPortLabel, commandPortLabel, networkSimulationGroup,
				commandLogLabel, commandLogTextBox
			});
		}

		private void CreateNetworkSimulationControls()
		{
			networkSimulationGroup = new GroupBox
			{
				Text = "Network Simulation",
				Location = new Point(50, 240),
				Size = new Size(600, 200),
				Font = new Font("Arial", 10, FontStyle.Bold)
			};

			enableNetworkSimulationCheckBox = new CheckBox
			{
				Text = "Enable Network Simulation",
				Location = new Point(15, 25),
				Size = new Size(200, 20),
				Font = new Font("Arial", 9, FontStyle.Bold),
				ForeColor = Color.DarkBlue
			};
			enableNetworkSimulationCheckBox.CheckedChanged += EnableNetworkSimulation_CheckedChanged;

			// Latency controls
			Label latencyTitleLabel = new Label
			{
				Text = "Latency (ms):",
				Location = new Point(15, 55),
				Size = new Size(80, 20),
				Font = new Font("Arial", 9, FontStyle.Regular)
			};

			latencyTrackBar = new TrackBar
			{
				Location = new Point(100, 50),
				Size = new Size(150, 45),
				Minimum = 0,
				Maximum = 2000,
				Value = 0,
				TickFrequency = 200
			};
			latencyTrackBar.ValueChanged += LatencyTrackBar_ValueChanged;

			latencyLabel = new Label
			{
				Text = "0 ms",
				Location = new Point(260, 55),
				Size = new Size(60, 20),
				Font = new Font("Arial", 9, FontStyle.Regular),
				ForeColor = Color.Blue
			};

			// Packet loss controls
			Label packetLossTitleLabel = new Label
			{
				Text = "Packet Loss (%):",
				Location = new Point(15, 100),
				Size = new Size(100, 20),
				Font = new Font("Arial", 9, FontStyle.Regular)
			};

			packetLossTrackBar = new TrackBar
			{
				Location = new Point(120, 95),
				Size = new Size(130, 45),
				Minimum = 0,
				Maximum = 50,
				Value = 0,
				TickFrequency = 5
			};
			packetLossTrackBar.ValueChanged += PacketLossTrackBar_ValueChanged;

			packetLossLabel = new Label
			{
				Text = "0%",
				Location = new Point(260, 100),
				Size = new Size(60, 20),
				Font = new Font("Arial", 9, FontStyle.Regular),
				ForeColor = Color.Red
			};

			// Bandwidth controls
			Label bandwidthTitleLabel = new Label
			{
				Text = "Bandwidth Limit:",
				Location = new Point(350, 55),
				Size = new Size(100, 20),
				Font = new Font("Arial", 9, FontStyle.Regular)
			};

			bandwidthTrackBar = new TrackBar
			{
				Location = new Point(460, 50),
				Size = new Size(120, 45),
				Minimum = 0,
				Maximum = 1000,
				Value = 0,
				TickFrequency = 100
			};
			bandwidthTrackBar.ValueChanged += BandwidthTrackBar_ValueChanged;

			bandwidthLabel = new Label
			{
				Text = "Unlimited",
				Location = new Point(460, 95),
				Size = new Size(120, 20),
				Font = new Font("Arial", 9, FontStyle.Regular),
				ForeColor = Color.Orange,
				TextAlign = ContentAlignment.MiddleCenter
			};

			// Connection dropout controls
			enableDropoutCheckBox = new CheckBox
			{
				Text = "Random Connection Dropouts",
				Location = new Point(350, 120),
				Size = new Size(200, 20),
				Font = new Font("Arial", 9, FontStyle.Regular),
				ForeColor = Color.DarkRed
			};
			enableDropoutCheckBox.CheckedChanged += EnableDropout_CheckedChanged;

			dropoutLabel = new Label
			{
				Text = "Simulates random 1-3s disconnections",
				Location = new Point(350, 145),
				Size = new Size(220, 20),
				Font = new Font("Arial", 8, FontStyle.Italic),
				ForeColor = Color.Gray
			};

			// Reset button
			Button resetSimulationButton = new Button
			{
				Text = "Reset to Normal",
				Location = new Point(15, 160),
				Size = new Size(120, 25),
				Font = new Font("Arial", 9, FontStyle.Regular),
				BackColor = Color.LightBlue
			};
			resetSimulationButton.Click += ResetSimulation_Click;

			networkSimulationGroup.Controls.AddRange(new Control[]
			{
				enableNetworkSimulationCheckBox, latencyTitleLabel, latencyTrackBar, latencyLabel,
				packetLossTitleLabel, packetLossTrackBar, packetLossLabel,
				bandwidthTitleLabel, bandwidthTrackBar, bandwidthLabel,
				enableDropoutCheckBox, dropoutLabel, resetSimulationButton
			});
		}

		private void EnableNetworkSimulation_CheckedChanged(object sender, EventArgs e)
		{
			networkSimulationEnabled = enableNetworkSimulationCheckBox.Checked;

			// Enable/disable all simulation controls
			latencyTrackBar.Enabled = networkSimulationEnabled;
			packetLossTrackBar.Enabled = networkSimulationEnabled;
			bandwidthTrackBar.Enabled = networkSimulationEnabled;
			enableDropoutCheckBox.Enabled = networkSimulationEnabled;

			if (networkSimulationEnabled)
			{
				LogCommand("SIMULATION", "Network simulation ENABLED");
				UpdateSimulationValues();
			}
			else
			{
				LogCommand("SIMULATION", "Network simulation DISABLED");
			}
		}

		private void LatencyTrackBar_ValueChanged(object sender, EventArgs e)
		{
			latencyMs = latencyTrackBar.Value;
			latencyLabel.Text = $"{latencyMs} ms";
			latencyLabel.ForeColor = latencyMs > 500 ? Color.Red : (latencyMs > 100 ? Color.Orange : Color.Blue);
		}

		private void PacketLossTrackBar_ValueChanged(object sender, EventArgs e)
		{
			packetLossPercent = packetLossTrackBar.Value;
			packetLossLabel.Text = $"{packetLossPercent}%";
			packetLossLabel.ForeColor = packetLossPercent > 10 ? Color.Red : (packetLossPercent > 2 ? Color.Orange : Color.Green);
		}

		private void BandwidthTrackBar_ValueChanged(object sender, EventArgs e)
		{
			bandwidthLimitKbps = bandwidthTrackBar.Value;
			if (bandwidthLimitKbps == 0)
			{
				bandwidthLabel.Text = "Unlimited";
				bandwidthLabel.ForeColor = Color.Green;
			}
			else
			{
				bandwidthLabel.Text = $"{bandwidthLimitKbps} Kbps";
				bandwidthLabel.ForeColor = bandwidthLimitKbps < 100 ? Color.Red : (bandwidthLimitKbps < 500 ? Color.Orange : Color.Blue);
			}
		}

		private void EnableDropout_CheckedChanged(object sender, EventArgs e)
		{
			enableRandomDropouts = enableDropoutCheckBox.Checked;
			LogCommand("SIMULATION", $"Random dropouts {(enableRandomDropouts ? "ENABLED" : "DISABLED")}");
		}

		private void ResetSimulation_Click(object sender, EventArgs e)
		{
			enableNetworkSimulationCheckBox.Checked = false;
			latencyTrackBar.Value = 0;
			packetLossTrackBar.Value = 0;
			bandwidthTrackBar.Value = 0;
			enableDropoutCheckBox.Checked = false;

			LogCommand("SIMULATION", "Network simulation reset to normal conditions");
		}

		private void UpdateSimulationValues()
		{
			if (networkSimulationEnabled)
			{
				LogCommand("SIMULATION", $"Latency: {latencyMs}ms, Packet Loss: {packetLossPercent}%, " +
					$"Bandwidth: {(bandwidthLimitKbps == 0 ? "Unlimited" : bandwidthLimitKbps + " Kbps")}, " +
					$"Dropouts: {(enableRandomDropouts ? "Enabled" : "Disabled")}");
			}
		}

		private async Task<bool> SimulateNetworkConditions(byte[] data)
		{
			if (!networkSimulationEnabled)
				return true;

			// Simulate packet loss
			if (packetLossPercent > 0 && networkRandom.Next(100) < packetLossPercent)
			{
				return false; // Drop this packet
			}

			// Simulate random dropouts
			if (enableRandomDropouts)
			{
				DateTime now = DateTime.Now;

				// Check if we're in a dropout period
				if (dropoutDurationMs > 0 && (now - lastDropoutTime).TotalMilliseconds < dropoutDurationMs)
				{
					return false; // Still in dropout
				}

				// Random chance to start a new dropout (1% chance per call)
				if (dropoutDurationMs == 0 && networkRandom.Next(1000) < 10)
				{
					dropoutDurationMs = networkRandom.Next(1000, 3000); // 1-3 second dropout
					lastDropoutTime = now;
					LogCommand("SIMULATION", $"Connection dropout started ({dropoutDurationMs}ms)");
					return false;
				}

				// End dropout if duration has passed
				if (dropoutDurationMs > 0 && (now - lastDropoutTime).TotalMilliseconds >= dropoutDurationMs)
				{
					LogCommand("SIMULATION", "Connection dropout ended");
					dropoutDurationMs = 0;
				}
			}

			// Simulate latency
			if (latencyMs > 0)
			{
				await Task.Delay(latencyMs);
			}

			// Simulate bandwidth limitation
			if (bandwidthLimitKbps > 0)
			{
				// Calculate delay based on data size and bandwidth limit
				double bytesPerMs = (bandwidthLimitKbps * 1024.0) / (8.0 * 1000.0); // Convert Kbps to bytes per ms
				double transmitTimeMs = data.Length / bytesPerMs;

				if (transmitTimeMs > 1)
				{
					await Task.Delay((int)transmitTimeMs);
				}
			}

			return true; // Packet delivered successfully
		}

		private void LoadCameraDevices()
		{
			cameraComboBox.Items.Clear();

			try
			{
				// Using AForge.NET for camera enumeration
				videoDevices = new FilterInfoCollection(AForge.Video.DirectShow.FilterCategory.VideoInputDevice);

				foreach (AForge.Video.DirectShow.FilterInfo device in videoDevices)
				{
					cameraComboBox.Items.Add(device.Name);
				}

				if (cameraComboBox.Items.Count > 0)
				{
					cameraComboBox.SelectedIndex = 0;
				}
				else
				{
					cameraComboBox.Items.Add("No cameras found");
					cameraComboBox.SelectedIndex = 0;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error loading cameras: {ex.Message}", "Camera Error",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				cameraComboBox.Items.Add("Error loading cameras");
				cameraComboBox.SelectedIndex = 0;
			}
		}

		private void StartServer_Click(object sender, EventArgs e)
		{
			if (cameraComboBox.SelectedItem == null || cameraComboBox.Items.Count == 0 ||
				cameraComboBox.SelectedItem.ToString().Contains("No cameras") ||
				cameraComboBox.SelectedItem.ToString().Contains("Error"))
			{
				MessageBox.Show("Please select a valid camera device.", "No Camera Selected",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			try
			{
				// Initialize camera
				if (!InitializeCamera())
				{
					MessageBox.Show("Failed to initialize camera.", "Camera Error",
						MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}

				// Start camera capture
				videoSource.Start();

				// Start TCP servers
				tcpListener = new TcpListener(IPAddress.Any, VIDEO_PORT);
				commandListener = new TcpListener(IPAddress.Any, COMMAND_PORT);

				serverThread = new Thread(StartVideoListening);
				serverThread.IsBackground = true;
				serverThread.Start();

				commandThread = new Thread(StartCommandListening);
				commandThread.IsBackground = true;
				commandThread.Start();

				isStreaming = true;
				startButton.Enabled = false;
				stopButton.Enabled = true;
				cameraComboBox.Enabled = false;
				statusLabel.Text = $"Server started - Video: {VIDEO_PORT}, Commands: {COMMAND_PORT}";
				statusLabel.ForeColor = Color.Green;

				LogCommand("SERVER", "Server started successfully");
				if (networkSimulationEnabled)
				{
					UpdateSimulationValues();
				}
			}
			catch (Exception ex)
			{
				CleanupCamera();
				MessageBox.Show($"Error starting server: {ex.Message}", "Error",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void StopServer_Click(object sender, EventArgs e)
		{
			isStreaming = false;

			// Stop camera
			CleanupCamera();

			// Close all command clients
			lock (commandClientsLock)
			{
				var clientList = new List<TcpClient>();
				while (commandClients.TryTake(out TcpClient client))
				{
					try
					{
						client?.Close();
					}
					catch { }
				}
			}

			// Stop TCP servers
			tcpListener?.Stop();
			commandListener?.Stop();

			if (serverThread != null && serverThread.IsAlive)
			{
				serverThread.Join(2000);
			}

			if (commandThread != null && commandThread.IsAlive)
			{
				commandThread.Join(2000);
			}

			startButton.Enabled = true;
			stopButton.Enabled = false;
			cameraComboBox.Enabled = true;
			statusLabel.Text = "Server stopped";
			statusLabel.ForeColor = Color.Red;
			connectedClients = 0;
			connectedCommandClients = 0;
			UpdateClientCounts();

			LogCommand("SERVER", "Server stopped");

			// Clear last chat message when server stops
			lock (lastChatLock)
			{
				lastChatMessage = "";
				lastChatSender = "";
				lastChatTimestamp = DateTime.MinValue;
			}
		}

		private bool InitializeCamera()
		{
			try
			{
				int selectedIndex = cameraComboBox.SelectedIndex;
				if (selectedIndex < 0 || selectedIndex >= videoDevices.Count)
					return false;

				// Create video source
				videoSource = new VideoCaptureDevice(videoDevices[selectedIndex].MonikerString);

				// Set camera resolution (optional)
				if (videoSource.VideoCapabilities.Length > 0)
				{
					// Choose a reasonable resolution
					VideoCapabilities selectedCaps = null;
					foreach (var cap in videoSource.VideoCapabilities)
					{
						if (cap.FrameSize.Width == 640 && cap.FrameSize.Height == 480)
						{
							selectedCaps = cap;
							break;
						}
					}

					// If 640x480 not found, use the first available
					if (selectedCaps == null && videoSource.VideoCapabilities.Length > 0)
					{
						selectedCaps = videoSource.VideoCapabilities[0];
					}

					if (selectedCaps != null)
					{
						videoSource.VideoResolution = selectedCaps;
					}
				}

				// Set up event handler for new frames
				videoSource.NewFrame += VideoSource_NewFrame;

				return true;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Camera initialization error: {ex.Message}", "Error",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
		}

		private void CleanupCamera()
		{
			try
			{
				if (videoSource != null)
				{
					if (videoSource.IsRunning)
					{
						videoSource.SignalToStop();
						videoSource.WaitForStop();
					}

					videoSource.NewFrame -= VideoSource_NewFrame;
					videoSource = null;
				}
			}
			catch (Exception ex)
			{
				// Log error but don't show message box during cleanup
				Console.WriteLine($"Cleanup error: {ex.Message}");
			}
		}

		private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
		{
			try
			{
				lock (frameLock)
				{
					// AForge provides the frame as a Bitmap - we need to clone it
					// because the original will be disposed after this event
					using (Bitmap originalFrame = (Bitmap)eventArgs.Frame.Clone())
					{
						// Scale down for better performance
						int newWidth = Math.Min(640, originalFrame.Width);
						int newHeight = Math.Min(480, originalFrame.Height);

						// Ensure minimum size
						if (newWidth < 160) newWidth = 160;
						if (newHeight < 120) newHeight = 120;

						using (Bitmap scaledBitmap = new Bitmap(newWidth, newHeight, PixelFormat.Format24bppRgb))
						{
							using (Graphics g = Graphics.FromImage(scaledBitmap))
							{
								g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
								g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
								g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
								g.DrawImage(originalFrame, 0, 0, newWidth, newHeight);

								// Process any pending commands that affect the video
								ProcessPendingCommands(g, newWidth, newHeight);

								// Draw network simulation indicators
								if (networkSimulationEnabled)
								{
									DrawNetworkSimulationIndicators(g, newWidth, newHeight);
								}

								// Always draw the last chat message (if any) on every frame
								DrawLastChatMessage(g, newWidth, newHeight);
							}

							// Convert to JPEG byte array
							currentFrame = BitmapToByteArray(scaledBitmap, 75L);
							hasNewFrame = true;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Frame processing error: {ex.Message}");
				// Reset frame state on error
				lock (frameLock)
				{
					hasNewFrame = false;
					currentFrame = null;
				}
			}
		}

		private void DrawLastChatMessage(Graphics g, int width, int height)
		{
			lock (lastChatLock)
			{
				// Only draw if we have a chat message
				if (!string.IsNullOrEmpty(lastChatMessage))
				{
					try
					{
						// Calculate age of message for visual effects
						TimeSpan messageAge = DateTime.Now - lastChatTimestamp;
						bool isRecentMessage = messageAge.TotalSeconds <= 30; // Highlight recent messages

						// Prepare display text
						string displayText = string.IsNullOrEmpty(lastChatSender) ?
							lastChatMessage :
							$"{lastChatSender}: {lastChatMessage}";

						// Truncate long messages
						if (displayText.Length > 50)
						{
							displayText = displayText.Substring(0, 47) + "...";
						}

						// Calculate overlay dimensions and position
						using (Font chatFont = new Font("Arial", 11, FontStyle.Bold))
						{
							SizeF textSize = g.MeasureString(displayText, chatFont);

							// Position at bottom-left corner with some padding
							float overlayX = 10;
							float overlayY = height - textSize.Height - 25;
							float overlayWidth = Math.Min(textSize.Width + 20, width - 20);
							float overlayHeight = textSize.Height + 12;

							// Choose background color based on message age
							Color backgroundColor = isRecentMessage ?
								Color.FromArgb(200, Color.DarkBlue) :
								Color.FromArgb(150, Color.DarkGray);

							// Draw background
							using (SolidBrush backgroundBrush = new SolidBrush(backgroundColor))
							{
								g.FillRectangle(backgroundBrush, overlayX, overlayY, overlayWidth, overlayHeight);
							}

							// Draw border
							Color borderColor = isRecentMessage ? Color.Cyan : Color.Gray;
							using (Pen borderPen = new Pen(borderColor, 1))
							{
								g.DrawRectangle(borderPen, overlayX, overlayY, overlayWidth, overlayHeight);
							}

							// Draw "CHAT:" label
							using (SolidBrush labelBrush = new SolidBrush(isRecentMessage ? Color.Yellow : Color.LightGray))
							{
								g.DrawString("CHAT:", new Font("Arial", 9, FontStyle.Bold),
									labelBrush, overlayX + 5, overlayY + 3);
							}

							// Draw the actual chat message
							using (SolidBrush textBrush = new SolidBrush(Color.White))
							{
								SizeF labelSize = g.MeasureString("CHAT:", new Font("Arial", 9, FontStyle.Bold));
								g.DrawString(displayText, new Font("Arial", 9, FontStyle.Regular),
									textBrush, overlayX + labelSize.Width + 8, overlayY + 5);
							}

							// Add timestamp indicator for recent messages
							if (isRecentMessage)
							{
								string timeAgo = $"{(int)messageAge.TotalSeconds}s ago";
								using (Font timeFont = new Font("Arial", 7, FontStyle.Italic))
								using (SolidBrush timeBrush = new SolidBrush(Color.LightGray))
								{
									SizeF timeSize = g.MeasureString(timeAgo, timeFont);
									g.DrawString(timeAgo, timeFont, timeBrush,
										overlayX + overlayWidth - timeSize.Width - 5,
										overlayY + overlayHeight - timeSize.Height - 2);
								}
							}
						}
					}
					catch (Exception ex)
					{
						LogCommand("ERROR", $"Error drawing last chat message: {ex.Message}");
					}
				}
			}
		}

		private void DrawNetworkSimulationIndicators(Graphics g, int width, int height)
		{
			try
			{
				// Draw simulation status indicator
				using (SolidBrush backgroundBrush = new SolidBrush(Color.FromArgb(150, Color.DarkBlue)))
				{
					Rectangle indicatorRect = new Rectangle(width - 200, height - 80, 190, 70);
					g.FillRectangle(backgroundBrush, indicatorRect);

					using (Pen borderPen = new Pen(Color.Yellow, 2))
					{
						g.DrawRectangle(borderPen, indicatorRect);
					}
				}

				using (Font indicatorFont = new Font("Arial", 8, FontStyle.Bold))
				using (SolidBrush textBrush = new SolidBrush(Color.White))
				{
					g.DrawString("NET SIM", indicatorFont, textBrush, width - 195, height - 75);
					g.DrawString($"Lat: {latencyMs}ms", new Font("Arial", 7), textBrush, width - 195, height - 60);
					g.DrawString($"Loss: {packetLossPercent}%", new Font("Arial", 7), textBrush, width - 195, height - 45);

					string bwText = bandwidthLimitKbps == 0 ? "BW: ∞" : $"BW: {bandwidthLimitKbps}K";
					g.DrawString(bwText, new Font("Arial", 7), textBrush, width - 195, height - 30);

					if (enableRandomDropouts)
					{
						g.DrawString("DROPOUTS", new Font("Arial", 7, FontStyle.Bold),
							new SolidBrush(Color.Red), width - 195, height - 15);
					}
				}

				// Show active dropout indicator
				if (dropoutDurationMs > 0 && (DateTime.Now - lastDropoutTime).TotalMilliseconds < dropoutDurationMs)
				{
					using (SolidBrush dropoutBrush = new SolidBrush(Color.FromArgb(200, Color.Red)))
					{
						g.FillRectangle(dropoutBrush, 0, 0, width, height);
					}

					using (Font dropoutFont = new Font("Arial", 24, FontStyle.Bold))
					using (SolidBrush dropoutTextBrush = new SolidBrush(Color.White))
					{
						string dropoutText = "CONNECTION DROPOUT";
						SizeF textSize = g.MeasureString(dropoutText, dropoutFont);
						g.DrawString(dropoutText, dropoutFont, dropoutTextBrush,
							(width - textSize.Width) / 2, (height - textSize.Height) / 2);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error drawing network simulation indicators: {ex.Message}");
			}
		}

		private void ProcessPendingCommands(Graphics g, int width, int height)
		{
			lock (commandLock)
			{
				while (pendingCommands.Count > 0)
				{
					string command = pendingCommands.Dequeue();
					ExecuteDrawCommand(g, command, width, height);
				}
			}

			// Draw pause indicator if stream is paused
			if (streamPaused)
			{
				DrawPauseIndicator(g, width, height);
			}
		}

		private void DrawPauseIndicator(Graphics g, int width, int height)
		{
			try
			{
				// Draw semi-transparent overlay
				using (SolidBrush overlay = new SolidBrush(Color.FromArgb(128, Color.Black)))
				{
					g.FillRectangle(overlay, 0, 0, width, height);
				}

				// Draw pause symbol (two vertical bars)
				int centerX = width / 2;
				int centerY = height / 2;
				int barWidth = 20;
				int barHeight = 60;
				int barSpacing = 15;

				using (SolidBrush pauseBrush = new SolidBrush(Color.White))
				{
					// Left bar
					g.FillRectangle(pauseBrush, centerX - barSpacing - barWidth, centerY - barHeight / 2, barWidth, barHeight);
					// Right bar
					g.FillRectangle(pauseBrush, centerX + barSpacing, centerY - barHeight / 2, barWidth, barHeight);
				}

				// Draw "PAUSED" text
				using (Font pauseFont = new Font("Arial", 16, FontStyle.Bold))
				using (SolidBrush textBrush = new SolidBrush(Color.White))
				{
					string pauseText = "PAUSED";
					SizeF textSize = g.MeasureString(pauseText, pauseFont);
					float textX = (width - textSize.Width) / 2;
					float textY = centerY + barHeight / 2 + 20;
					g.DrawString(pauseText, pauseFont, textBrush, textX, textY);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error drawing pause indicator: {ex.Message}");
			}
		}

		private void ExecuteDrawCommand(Graphics g, string command, int width, int height)
		{
			try
			{
				var commandObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(command);
				if (commandObj.ContainsKey("type"))
				{
					string type = commandObj["type"].ToString();

					switch (type.ToLower())
					{
						case "line":
							DrawLine(g, commandObj);
							break;
						case "rectangle":
							DrawRectangle(g, commandObj);
							break;
						case "circle":
							DrawCircle(g, commandObj);
							break;
						case "text":
							DrawText(g, commandObj);
							break;
						case "togglepause":
							HandleTogglePause(commandObj);
							break;
						case "chat":
							HandleChatCommand(g, commandObj, width, height);
							break;
						case "clearchat":
							HandleClearChatCommand(commandObj);
							break;
					}
				}
			}
			catch (Exception ex)
			{
				LogCommand("ERROR", $"Failed to execute command: {ex.Message}");
			}
		}

		private void HandleChatCommand(Graphics g, Dictionary<string, object> command, int width, int height)
		{
			try
			{
				string message = GetValueOrDefault(command, "message", "").ToString();
				string sender = GetValueOrDefault(command, "sender", "Unknown").ToString();

				// Store as the last chat message
				lock (lastChatLock)
				{
					lastChatMessage = message;
					lastChatSender = sender;
					lastChatTimestamp = DateTime.Now;
				}

				LogCommand("CHAT", $"{sender}: {message}");
			}
			catch (Exception ex)
			{
				LogCommand("ERROR", $"HandleChatCommand error: {ex.Message}");
			}
		}

		private void HandleClearChatCommand(Dictionary<string, object> command)
		{
			try
			{
				string clientId = GetValueOrDefault(command, "clientId", "Unknown").ToString();

				// Clear the last chat message
				lock (lastChatLock)
				{
					lastChatMessage = "";
					lastChatSender = "";
					lastChatTimestamp = DateTime.MinValue;
				}

				LogCommand("CLEARCHAT", $"Chat cleared by {clientId}");
			}
			catch (Exception ex)
			{
				LogCommand("ERROR", $"HandleClearChatCommand error: {ex.Message}");
			}
		}

		private void DrawChatOverlay(Graphics g, string chatMessage, int width, int height)
		{
			try
			{
				// Truncate long messages
				string displayMessage = chatMessage;
				if (displayMessage.Length > 60)
				{
					displayMessage = displayMessage.Substring(0, 57) + "...";
				}

				// Draw semi-transparent background
				using (Font chatFont = new Font("Arial", 12, FontStyle.Bold))
				{
					SizeF textSize = g.MeasureString(displayMessage, chatFont);
					RectangleF backgroundRect = new RectangleF(
						10, 10,
						Math.Min(textSize.Width + 20, width - 20),
						textSize.Height + 10
					);

					using (SolidBrush backgroundBrush = new SolidBrush(Color.FromArgb(180, Color.Black)))
					{
						g.FillRectangle(backgroundBrush, backgroundRect);
					}

					// Draw "CHAT:" label
					using (SolidBrush labelBrush = new SolidBrush(Color.Yellow))
					{
						g.DrawString("CHAT:", chatFont, labelBrush, 15, 15);
					}

					// Draw chat message
					using (SolidBrush textBrush = new SolidBrush(Color.White))
					{
						SizeF labelSize = g.MeasureString("CHAT:", chatFont);
						g.DrawString(displayMessage, new Font("Arial", 10, FontStyle.Regular),
							textBrush, 15 + labelSize.Width + 5, 17);
					}
				}
			}
			catch (Exception ex)
			{
				LogCommand("ERROR", $"Error drawing chat overlay: {ex.Message}");
			}
		}

		private void HandleTogglePause(Dictionary<string, object> command)
		{
			streamPaused = !streamPaused;
			string clientId = GetValueOrDefault(command, "clientId", "Unknown").ToString();
			string timestamp = GetValueOrDefault(command, "timestamp", DateTime.Now.ToString()).ToString();

			LogCommand("TOGGLEPAUSE", $"Stream {(streamPaused ? "PAUSED" : "RESUMED")} by {clientId} at {timestamp}");

			this.Invoke(new Action(() =>
			{
				statusLabel.Text = $"Server running - Stream is {(streamPaused ? "PAUSED" : "PLAYING")} - Video: {VIDEO_PORT}, Commands: {COMMAND_PORT}";
				statusLabel.ForeColor = streamPaused ? Color.Orange : Color.Green;
			}));
		}

		private object GetValueOrDefault(Dictionary<string, object> dict, string key, object defaultValue)
		{
			return dict.ContainsKey(key) ? dict[key] : defaultValue;
		}

		private void DrawLine(Graphics g, Dictionary<string, object> command)
		{
			try
			{
				float x1 = Convert.ToSingle(GetValueOrDefault(command, "x1", 0));
				float y1 = Convert.ToSingle(GetValueOrDefault(command, "y1", 0));
				float x2 = Convert.ToSingle(GetValueOrDefault(command, "x2", 100));
				float y2 = Convert.ToSingle(GetValueOrDefault(command, "y2", 100));
				string colorStr = GetValueOrDefault(command, "color", "Red").ToString();
				float thickness = Convert.ToSingle(GetValueOrDefault(command, "thickness", 2));

				Color color = Color.FromName(colorStr);
				using (Pen pen = new Pen(color, thickness))
				{
					g.DrawLine(pen, x1, y1, x2, y2);
				}
			}
			catch (Exception ex)
			{
				LogCommand("ERROR", $"DrawLine error: {ex.Message}");
			}
		}

		private void DrawRectangle(Graphics g, Dictionary<string, object> command)
		{
			try
			{
				float x = Convert.ToSingle(GetValueOrDefault(command, "x", 10));
				float y = Convert.ToSingle(GetValueOrDefault(command, "y", 10));
				float width = Convert.ToSingle(GetValueOrDefault(command, "width", 50));
				float height = Convert.ToSingle(GetValueOrDefault(command, "height", 50));
				string colorStr = GetValueOrDefault(command, "color", "Blue").ToString();
				bool filled = Convert.ToBoolean(GetValueOrDefault(command, "filled", false));

				Color color = Color.FromName(colorStr);

				if (filled)
				{
					using (SolidBrush brush = new SolidBrush(color))
					{
						g.FillRectangle(brush, x, y, width, height);
					}
				}
				else
				{
					using (Pen pen = new Pen(color, 2))
					{
						g.DrawRectangle(pen, x, y, width, height);
					}
				}
			}
			catch (Exception ex)
			{
				LogCommand("ERROR", $"DrawRectangle error: {ex.Message}");
			}
		}

		private void DrawCircle(Graphics g, Dictionary<string, object> command)
		{
			try
			{
				float x = Convert.ToSingle(GetValueOrDefault(command, "x", 50));
				float y = Convert.ToSingle(GetValueOrDefault(command, "y", 50));
				float radius = Convert.ToSingle(GetValueOrDefault(command, "radius", 25));
				string colorStr = GetValueOrDefault(command, "color", "Green").ToString();
				bool filled = Convert.ToBoolean(GetValueOrDefault(command, "filled", false));

				Color color = Color.FromName(colorStr);
				float diameter = radius * 2;

				if (filled)
				{
					using (SolidBrush brush = new SolidBrush(color))
					{
						g.FillEllipse(brush, x - radius, y - radius, diameter, diameter);
					}
				}
				else
				{
					using (Pen pen = new Pen(color, 2))
					{
						g.DrawEllipse(pen, x - radius, y - radius, diameter, diameter);
					}
				}
			}
			catch (Exception ex)
			{
				LogCommand("ERROR", $"DrawCircle error: {ex.Message}");
			}
		}

		private void DrawText(Graphics g, Dictionary<string, object> command)
		{
			try
			{
				float x = Convert.ToSingle(GetValueOrDefault(command, "x", 10));
				float y = Convert.ToSingle(GetValueOrDefault(command, "y", 10));
				string text = GetValueOrDefault(command, "text", "Sample Text").ToString();
				string colorStr = GetValueOrDefault(command, "color", "Black").ToString();
				float fontSize = Convert.ToSingle(GetValueOrDefault(command, "fontSize", 12));

				Color color = Color.FromName(colorStr);
				using (Font font = new Font("Arial", fontSize))
				using (SolidBrush brush = new SolidBrush(color))
				{
					g.DrawString(text, font, brush, x, y);
				}
			}
			catch (Exception ex)
			{
				LogCommand("ERROR", $"DrawText error: {ex.Message}");
			}
		}

		private void StartVideoListening()
		{
			tcpListener.Start();

			while (isStreaming)
			{
				try
				{
					TcpClient client = tcpListener.AcceptTcpClient();

					// Handle each client in a separate thread
					Thread clientThread = new Thread(async () => await HandleVideoClient(client));
					clientThread.IsBackground = true;
					clientThread.Start();
				}
				catch (ObjectDisposedException)
				{
					// Expected when stopping the server
					break;
				}
				catch (Exception ex)
				{
					if (isStreaming)
					{
						this.Invoke(new Action(() =>
							MessageBox.Show($"Video server error: {ex.Message}", "Error",
								MessageBoxButtons.OK, MessageBoxIcon.Error)));
					}
					break;
				}
			}
		}

		private void StartCommandListening()
		{
			commandListener.Start();

			while (isStreaming)
			{
				try
				{
					TcpClient client = commandListener.AcceptTcpClient();

					// Handle each command client in a separate thread
					Thread clientThread = new Thread(() => HandleCommandClient(client));
					clientThread.IsBackground = true;
					clientThread.Start();
				}
				catch (ObjectDisposedException)
				{
					// Expected when stopping the server
					break;
				}
				catch (Exception ex)
				{
					if (isStreaming)
					{
						this.Invoke(new Action(() =>
							MessageBox.Show($"Command server error: {ex.Message}", "Error",
								MessageBoxButtons.OK, MessageBoxIcon.Error)));
					}
					break;
				}
			}
		}

		private async Task HandleVideoClient(TcpClient client)
		{
			try
			{
				Interlocked.Increment(ref connectedClients);
				this.Invoke(new Action(() => UpdateClientCounts()));

				LogCommand("VIDEO", $"Video client connected from {client.Client.RemoteEndPoint}");
				await StreamToClient(client);
			}
			finally
			{
				Interlocked.Decrement(ref connectedClients);
				this.Invoke(new Action(() => UpdateClientCounts()));
				LogCommand("VIDEO", $"Video client disconnected");
				client?.Close();
			}
		}

		private void HandleCommandClient(TcpClient client)
		{
			try
			{
				// Add client to the list for echoing
				lock (commandClientsLock)
				{
					commandClients.Add(client);
				}

				Interlocked.Increment(ref connectedCommandClients);
				this.Invoke(new Action(() => UpdateClientCounts()));

				LogCommand("COMMAND", $"Command client connected from {client.Client.RemoteEndPoint}");
				ReceiveCommands(client);
			}
			finally
			{
				// Remove client from the list
				lock (commandClientsLock)
				{
					var updatedClients = new ConcurrentBag<TcpClient>();
					while (commandClients.TryTake(out TcpClient existingClient))
					{
						if (existingClient != client && existingClient.Connected)
						{
							updatedClients.Add(existingClient);
						}
					}
					commandClients = updatedClients;
				}

				Interlocked.Decrement(ref connectedCommandClients);
				this.Invoke(new Action(() => UpdateClientCounts()));
				LogCommand("COMMAND", $"Command client disconnected");
				client?.Close();
			}
		}

		private void ReceiveCommands(TcpClient client)
		{
			NetworkStream stream = client.GetStream();
			byte[] buffer = new byte[4096];

			while (isStreaming && client.Connected)
			{
				try
				{
					int bytesRead = stream.Read(buffer, 0, buffer.Length);
					if (bytesRead > 0)
					{
						string commandData = Encoding.UTF8.GetString(buffer, 0, bytesRead);

						// Handle multiple commands if they come in one packet
						string[] commands = commandData.Split('\n');

						foreach (string command in commands)
						{
							if (!string.IsNullOrWhiteSpace(command))
							{
								string trimmedCommand = command.Trim();

								// Add to pending commands for video processing
								lock (commandLock)
								{
									pendingCommands.Enqueue(trimmedCommand);
								}

								// Echo the command back to ALL command clients (with network simulation)
								_ = Task.Run(async () => await EchoCommandToClientsAsync(trimmedCommand, client));

								LogCommand("RECEIVED", trimmedCommand);
							}
						}
					}
					else
					{
						break; // Client disconnected
					}
				}
				catch (Exception ex)
				{
					LogCommand("ERROR", $"Command receive error: {ex.Message}");
					break;
				}
			}
		}

		private async Task EchoCommandToClientsAsync(string command, TcpClient sendingClient)
		{
			// Create echo response with timestamp and sender info
			var echoResponse = new
			{
				type = "command_echo",
				original_command = command,
				timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
				sender = sendingClient.Client.RemoteEndPoint?.ToString() ?? "unknown",
				server_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
			};

			string echoJson = JsonConvert.SerializeObject(echoResponse) + "\n";
			byte[] echoData = Encoding.UTF8.GetBytes(echoJson);

			// Apply network simulation to command echo
			if (!await SimulateNetworkConditions(echoData))
			{
				LogCommand("SIMULATION", "Command echo dropped due to network simulation");
				return;
			}

			lock (commandClientsLock)
			{
				var clientsToRemove = new List<TcpClient>();
				var activeClients = new List<TcpClient>();

				// Get all current clients
				while (commandClients.TryTake(out TcpClient client))
				{
					activeClients.Add(client);
				}

				// Send echo to all connected command clients
				foreach (TcpClient client in activeClients)
				{
					try
					{
						if (client.Connected)
						{
							NetworkStream clientStream = client.GetStream();
							clientStream.Write(echoData, 0, echoData.Length);
							clientStream.Flush();

							// Add back to collection
							commandClients.Add(client);
						}
						else
						{
							clientsToRemove.Add(client);
						}
					}
					catch (Exception ex)
					{
						LogCommand("ERROR", $"Failed to echo command to client {client.Client.RemoteEndPoint}: {ex.Message}");
						clientsToRemove.Add(client);
					}
				}

				// Clean up disconnected clients
				foreach (TcpClient clientToRemove in clientsToRemove)
				{
					try
					{
						clientToRemove.Close();
					}
					catch { }
				}
			}

			LogCommand("ECHO", $"Command echoed to {commandClients.Count} clients");
		}

		private void UpdateClientCounts()
		{
			clientCountLabel.Text = $"Video clients: {connectedClients}";
			commandStatusLabel.Text = $"Command clients: {connectedCommandClients}";
		}

		private void LogCommand(string type, string message)
		{
			if (this.InvokeRequired)
			{
				this.Invoke(new Action(() => LogCommand(type, message)));
				return;
			}

			string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
			string logEntry = $"[{timestamp}] [{type}] {message}\n";

			commandLogTextBox.AppendText(logEntry);
			commandLogTextBox.ScrollToCaret();

			// Keep log size manageable
			if (commandLogTextBox.Lines.Length > 1000)
			{
				string[] lines = commandLogTextBox.Lines;
				string[] keepLines = new string[500];
				Array.Copy(lines, lines.Length - 500, keepLines, 0, 500);
				commandLogTextBox.Lines = keepLines;
			}
		}

		private async Task StreamToClient(TcpClient client)
		{
			NetworkStream stream = client.GetStream();
			byte[] lastSentFrame = null;

			while (isStreaming && client.Connected)
			{
				try
				{
					// If stream is paused, send a pause indicator frame or just wait
					if (streamPaused)
					{
						Thread.Sleep(100);
						continue;
					}

					byte[] frameData = null;

					lock (frameLock)
					{
						if (hasNewFrame && currentFrame != null && currentFrame.Length > 0)
						{
							// Only send if frame is different from last sent frame
							if (lastSentFrame == null || !ArraysEqual(currentFrame, lastSentFrame))
							{
								frameData = new byte[currentFrame.Length];
								Array.Copy(currentFrame, frameData, currentFrame.Length);

								// Update last sent frame
								lastSentFrame = new byte[currentFrame.Length];
								Array.Copy(currentFrame, lastSentFrame, currentFrame.Length);
							}
							hasNewFrame = false;
						}
					}

					if (frameData != null)
					{
						// Apply network simulation to video frames
						if (await SimulateNetworkConditions(frameData))
						{
							// Send frame size first (4 bytes)
							byte[] sizeBytes = BitConverter.GetBytes(frameData.Length);

							// Apply simulation to size header too
							if (await SimulateNetworkConditions(sizeBytes))
							{
								stream.Write(sizeBytes, 0, 4);
								stream.Write(frameData, 0, frameData.Length);
								stream.Flush();
							}
						}
						// If simulation drops the packet, we just skip sending this frame
					}

					// Control frame rate (15 FPS) - but add jitter if network simulation is enabled
					int baseDelay = 67;
					if (networkSimulationEnabled && latencyMs > 0)
					{
						// Add random jitter (±20ms)
						int jitter = networkRandom.Next(-20, 21);
						baseDelay += jitter;
					}

					await Task.Delay(Math.Max(10, baseDelay));
				}
				catch (Exception ex)
				{
					// Client disconnected or other error
					Console.WriteLine($"Stream error: {ex.Message}");
					break;
				}
			}
		}

		private bool ArraysEqual(byte[] array1, byte[] array2)
		{
			if (array1.Length != array2.Length)
				return false;

			for (int i = 0; i < array1.Length; i++)
			{
				if (array1[i] != array2[i])
					return false;
			}
			return true;
		}

		private byte[] BitmapToByteArray(Bitmap bitmap, long quality)
		{
			try
			{
				using (MemoryStream stream = new MemoryStream())
				{
					// Set JPEG encoder parameters for compression
					ImageCodecInfo jpegCodec = GetEncoder(ImageFormat.Jpeg);
					if (jpegCodec != null)
					{
						EncoderParameters encoderParams = new EncoderParameters(1);
						encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

						bitmap.Save(stream, jpegCodec, encoderParams);
						encoderParams.Dispose();
					}
					else
					{
						// Fallback to default JPEG encoding
						bitmap.Save(stream, ImageFormat.Jpeg);
					}

					return stream.ToArray();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Bitmap conversion error: {ex.Message}");
				return new byte[0]; // Return empty array on error
			}
		}

		private ImageCodecInfo GetEncoder(ImageFormat format)
		{
			try
			{
				ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
				foreach (ImageCodecInfo codec in codecs)
				{
					if (codec.FormatID == format.Guid)
					{
						return codec;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Codec error: {ex.Message}");
			}
			return null;
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			isStreaming = false;
			CleanupCamera();

			// Close all command clients
			lock (commandClientsLock)
			{
				while (commandClients.TryTake(out TcpClient client))
				{
					try
					{
						client?.Close();
					}
					catch { }
				}
			}

			tcpListener?.Stop();
			commandListener?.Stop();

			if (serverThread != null && serverThread.IsAlive)
			{
				serverThread.Join(2000);
			}

			if (commandThread != null && commandThread.IsAlive)
			{
				commandThread.Join(2000);
			}

			base.OnFormClosing(e);
		}
	}
}