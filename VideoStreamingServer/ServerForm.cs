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
			this.Size = new Size(600, 500);
			this.Text = "Camera Video Streaming Server with Commands";
			this.StartPosition = FormStartPosition.CenterScreen;
			this.FormBorderStyle = FormBorderStyle.Sizable;
			this.MinimumSize = new Size(600, 500);

			// Title label
			Label titleLabel = new Label
			{
				Text = "Camera Video Streaming Server",
				Font = new Font("Arial", 14, FontStyle.Bold),
				Location = new Point(50, 20),
				Size = new Size(500, 25),
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

			// Command log section
			Label commandLogLabel = new Label
			{
				Text = "Command Log:",
				Location = new Point(50, 245),
				Size = new Size(100, 20),
				Font = new Font("Arial", 10, FontStyle.Bold)
			};

			commandLogTextBox = new RichTextBox
			{
				Location = new Point(50, 270),
				Size = new Size(500, 180),
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
				videoPortLabel, commandPortLabel, commandLogLabel, commandLogTextBox
			});
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
					}
				}
			}
			catch (Exception ex)
			{
				LogCommand("ERROR", $"Failed to execute command: {ex.Message}");
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
					Thread clientThread = new Thread(() => HandleVideoClient(client));
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

		private void HandleVideoClient(TcpClient client)
		{
			try
			{
				Interlocked.Increment(ref connectedClients);
				this.Invoke(new Action(() => UpdateClientCounts()));

				LogCommand("VIDEO", $"Video client connected from {client.Client.RemoteEndPoint}");
				StreamToClient(client);
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

								// Echo the command back to ALL command clients
								EchoCommandToClients(trimmedCommand, client);

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

		private void EchoCommandToClients(string command, TcpClient sendingClient)
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

		private void StreamToClient(TcpClient client)
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
						// Send frame size first (4 bytes)
						byte[] sizeBytes = BitConverter.GetBytes(frameData.Length);
						stream.Write(sizeBytes, 0, 4);

						// Send frame data
						stream.Write(frameData, 0, frameData.Length);
						stream.Flush();
					}

					// Control frame rate (15 FPS)
					Thread.Sleep(67);
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