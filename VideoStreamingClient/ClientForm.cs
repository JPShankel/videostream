using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace VideoStreamingClient
{
	public partial class ClientForm : Form
	{
		private TcpClient tcpClient;
		private TcpClient commandClient;
		private NetworkStream stream;
		private NetworkStream commandStream;
		private Thread receiveThread;
		private bool isConnected = false;
		private bool isCommandConnected = false;
		private bool isPaused = false;

		private TextBox serverIpTextBox;
		private Button connectButton;
		private Button disconnectButton;
		private Button connectCommandButton;
		private Button disconnectCommandButton;
		private Button togglePauseButton;
		private PictureBox videoPictureBox;
		private Label statusLabel;
		private Label commandStatusLabel;
		private Label fpsLabel;
		private Panel controlPanel;
		private Panel commandPanel;

		private int frameCount = 0;
		private DateTime lastFpsUpdate = DateTime.Now;
		private const int VIDEO_PORT = 8080;
		private const int COMMAND_PORT = 8081;

		public ClientForm()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			this.Size = new Size(900, 750);
			this.Text = "Video Streaming Client with Commands";
			this.StartPosition = FormStartPosition.CenterScreen;
			this.MinimumSize = new Size(600, 500);

			// Video control panel at the top
			controlPanel = new Panel
			{
				Location = new Point(0, 0),
				Size = new Size(900, 60),
				BackColor = Color.LightGray,
				Dock = DockStyle.Top
			};

			// Server IP input
			Label ipLabel = new Label
			{
				Text = "Server IP:",
				Location = new Point(10, 20),
				Size = new Size(60, 20),
				Font = new Font("Arial", 9, FontStyle.Regular)
			};

			serverIpTextBox = new TextBox
			{
				Text = "127.0.0.1",
				Location = new Point(80, 17),
				Size = new Size(120, 23),
				Font = new Font("Arial", 9, FontStyle.Regular)
			};

			connectButton = new Button
			{
				Text = "Connect Video",
				Location = new Point(220, 15),
				Size = new Size(100, 27),
				Font = new Font("Arial", 9, FontStyle.Regular),
				BackColor = Color.LightGreen
			};
			connectButton.Click += Connect_Click;

			disconnectButton = new Button
			{
				Text = "Disconnect Video",
				Location = new Point(340, 15),
				Size = new Size(110, 27),
				Font = new Font("Arial", 9, FontStyle.Regular),
				BackColor = Color.LightCoral,
				Enabled = false
			};
			disconnectButton.Click += Disconnect_Click;

			statusLabel = new Label
			{
				Text = "Video: Disconnected",
				Location = new Point(470, 20),
				Size = new Size(150, 20),
				Font = new Font("Arial", 9, FontStyle.Regular),
				ForeColor = Color.Red
			};

			fpsLabel = new Label
			{
				Text = "FPS: 0",
				Location = new Point(630, 20),
				Size = new Size(80, 20),
				Font = new Font("Arial", 9, FontStyle.Regular),
				ForeColor = Color.Blue
			};

			controlPanel.Controls.AddRange(new Control[]
			{
				ipLabel, serverIpTextBox, connectButton,
				disconnectButton, statusLabel, fpsLabel
			});

			// Command control panel
			commandPanel = new Panel
			{
				Location = new Point(0, 60),
				Size = new Size(900, 50),
				BackColor = Color.LightBlue,
				Dock = DockStyle.Top
			};

			connectCommandButton = new Button
			{
				Text = "Connect Commands",
				Location = new Point(20, 12),
				Size = new Size(120, 27),
				Font = new Font("Arial", 9, FontStyle.Regular),
				BackColor = Color.LightGreen
			};
			connectCommandButton.Click += ConnectCommand_Click;

			disconnectCommandButton = new Button
			{
				Text = "Disconnect Commands",
				Location = new Point(160, 12),
				Size = new Size(130, 27),
				Font = new Font("Arial", 9, FontStyle.Regular),
				BackColor = Color.LightCoral,
				Enabled = false
			};
			disconnectCommandButton.Click += DisconnectCommand_Click;

			togglePauseButton = new Button
			{
				Text = "Toggle Pause",
				Location = new Point(310, 12),
				Size = new Size(100, 27),
				Font = new Font("Arial", 9, FontStyle.Bold),
				BackColor = Color.Orange,
				Enabled = false
			};
			togglePauseButton.Click += TogglePause_Click;

			commandStatusLabel = new Label
			{
				Text = "Commands: Disconnected",
				Location = new Point(430, 17),
				Size = new Size(200, 20),
				Font = new Font("Arial", 9, FontStyle.Regular),
				ForeColor = Color.Red
			};

			commandPanel.Controls.AddRange(new Control[]
			{
				connectCommandButton, disconnectCommandButton,
				togglePauseButton, commandStatusLabel
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
			Label placeholderLabel = new Label
			{
				Text = "Video will appear here when connected",
				ForeColor = Color.White,
				BackColor = Color.Transparent,
				Font = new Font("Arial", 12, FontStyle.Italic),
				TextAlign = ContentAlignment.MiddleCenter,
				Dock = DockStyle.Fill
			};
			videoPictureBox.Controls.Add(placeholderLabel);

			this.Controls.AddRange(new Control[] { controlPanel, commandPanel, videoPictureBox });
		}

		private void Connect_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrWhiteSpace(serverIpTextBox.Text))
			{
				MessageBox.Show("Please enter a server IP address.", "Invalid Input",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			try
			{
				statusLabel.Text = "Video: Connecting...";
				statusLabel.ForeColor = Color.Orange;
				Application.DoEvents();

				tcpClient = new TcpClient();
				tcpClient.Connect(serverIpTextBox.Text, VIDEO_PORT);
				stream = tcpClient.GetStream();

				isConnected = true;
				receiveThread = new Thread(ReceiveVideo);
				receiveThread.IsBackground = true;
				receiveThread.Start();

				connectButton.Enabled = false;
				disconnectButton.Enabled = true;
				serverIpTextBox.Enabled = false;
				statusLabel.Text = "Video: Connected";
				statusLabel.ForeColor = Color.Green;

				// Clear placeholder
				videoPictureBox.Controls.Clear();
			}
			catch (Exception ex)
			{
				statusLabel.Text = "Video: Connection failed";
				statusLabel.ForeColor = Color.Red;
				MessageBox.Show($"Video connection failed: {ex.Message}", "Connection Error",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void Disconnect_Click(object sender, EventArgs e)
		{
			DisconnectFromVideoServer();
		}

		private void ConnectCommand_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrWhiteSpace(serverIpTextBox.Text))
			{
				MessageBox.Show("Please enter a server IP address.", "Invalid Input",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			try
			{
				commandStatusLabel.Text = "Commands: Connecting...";
				commandStatusLabel.ForeColor = Color.Orange;
				Application.DoEvents();

				commandClient = new TcpClient();
				commandClient.Connect(serverIpTextBox.Text, COMMAND_PORT);
				commandStream = commandClient.GetStream();

				isCommandConnected = true;
				connectCommandButton.Enabled = false;
				disconnectCommandButton.Enabled = true;
				togglePauseButton.Enabled = true;
				commandStatusLabel.Text = "Commands: Connected";
				commandStatusLabel.ForeColor = Color.Green;
			}
			catch (Exception ex)
			{
				commandStatusLabel.Text = "Commands: Connection failed";
				commandStatusLabel.ForeColor = Color.Red;
				MessageBox.Show($"Command connection failed: {ex.Message}", "Connection Error",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void DisconnectCommand_Click(object sender, EventArgs e)
		{
			DisconnectFromCommandServer();
		}

		private void TogglePause_Click(object sender, EventArgs e)
		{
			if (!isCommandConnected)
			{
				MessageBox.Show("Please connect to command stream first.", "Not Connected",
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

				commandStream.Write(commandBytes, 0, commandBytes.Length);
				commandStream.Flush();

				// Update button text to reflect current state
				isPaused = !isPaused;
				togglePauseButton.Text = isPaused ? "Resume Stream" : "Pause Stream";
				togglePauseButton.BackColor = isPaused ? Color.Red : Color.Orange;

				// Show status message
				this.Text = $"Video Streaming Client - {(isPaused ? "PAUSED" : "PLAYING")}";
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Failed to send command: {ex.Message}", "Command Error",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void SendDrawCommand(string type, object parameters)
		{
			if (!isCommandConnected)
			{
				MessageBox.Show("Please connect to command stream first.", "Not Connected",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			try
			{
				// Create drawing command
				var command = new
				{
					type = type,
					timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
					clientId = Environment.MachineName
				};

				// Merge with parameters
				var commandDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(
					JsonConvert.SerializeObject(command));
				var paramDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(
					JsonConvert.SerializeObject(parameters));

				foreach (var kvp in paramDict)
				{
					commandDict[kvp.Key] = kvp.Value;
				}

				string jsonCommand = JsonConvert.SerializeObject(commandDict) + "\n";
				byte[] commandBytes = Encoding.UTF8.GetBytes(jsonCommand);

				commandStream.Write(commandBytes, 0, commandBytes.Length);
				commandStream.Flush();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Failed to send draw command: {ex.Message}", "Command Error",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void DisconnectFromVideoServer()
		{
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

			connectButton.Enabled = true;
			disconnectButton.Enabled = false;
			serverIpTextBox.Enabled = true;
			statusLabel.Text = "Video: Disconnected";
			statusLabel.ForeColor = Color.Red;
			fpsLabel.Text = "FPS: 0";

			// Clear video display and show placeholder
			if (videoPictureBox.Image != null)
			{
				videoPictureBox.Image.Dispose();
				videoPictureBox.Image = null;
			}

			Label placeholderLabel = new Label
			{
				Text = "Video will appear here when connected",
				ForeColor = Color.White,
				BackColor = Color.Transparent,
				Font = new Font("Arial", 12, FontStyle.Italic),
				TextAlign = ContentAlignment.MiddleCenter,
				Dock = DockStyle.Fill
			};
			videoPictureBox.Controls.Clear();
			videoPictureBox.Controls.Add(placeholderLabel);

			this.Text = "Video Streaming Client with Commands";
		}

		private void DisconnectFromCommandServer()
		{
			isCommandConnected = false;

			try
			{
				commandStream?.Close();
				commandClient?.Close();
			}
			catch { }

			connectCommandButton.Enabled = true;
			disconnectCommandButton.Enabled = false;
			togglePauseButton.Enabled = false;
			togglePauseButton.Text = "Toggle Pause";
			togglePauseButton.BackColor = Color.Orange;
			commandStatusLabel.Text = "Commands: Disconnected";
			commandStatusLabel.ForeColor = Color.Red;

			isPaused = false;
			this.Text = "Video Streaming Client with Commands";
		}

		private void ReceiveVideo()
		{
			byte[] sizeBuffer = new byte[4];

			while (isConnected)
			{
				try
				{
					// Read frame size
					int bytesRead = 0;
					while (bytesRead < 4)
					{
						int read = stream.Read(sizeBuffer, bytesRead, 4 - bytesRead);
						if (read == 0)
						{
							throw new Exception("Connection closed by server");
						}
						bytesRead += read;
					}

					int frameSize = BitConverter.ToInt32(sizeBuffer, 0);

					// Validate frame size to prevent memory issues
					if (frameSize <= 0 || frameSize > 10 * 1024 * 1024) // Max 10MB per frame
					{
						throw new Exception("Invalid frame size received");
					}

					// Read frame data
					byte[] frameBuffer = new byte[frameSize];
					bytesRead = 0;
					while (bytesRead < frameSize)
					{
						int read = stream.Read(frameBuffer, bytesRead, frameSize - bytesRead);
						if (read == 0)
						{
							throw new Exception("Connection closed by server");
						}
						bytesRead += read;
					}

					// Convert to image and display
					using (MemoryStream ms = new MemoryStream(frameBuffer))
					{
						Image frame = Image.FromStream(ms);

						this.Invoke(new Action(() =>
						{
							if (videoPictureBox.Image != null)
							{
								videoPictureBox.Image.Dispose();
							}
							videoPictureBox.Image = frame;

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
					if (isConnected)
					{
						this.Invoke(new Action(() =>
						{
							MessageBox.Show($"Video receive error: {ex.Message}", "Error",
								MessageBoxButtons.OK, MessageBoxIcon.Error);
							DisconnectFromVideoServer();
						}));
					}
					break;
				}
			}
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			DisconnectFromVideoServer();
			DisconnectFromCommandServer();
			base.OnFormClosing(e);
		}

		// Additional helper methods for quick drawing commands
		private void AddDrawingButtons()
		{
			Button drawLineButton = new Button
			{
				Text = "Draw Line",
				Location = new Point(650, 12),
				Size = new Size(80, 27),
				Font = new Font("Arial", 8, FontStyle.Regular),
				BackColor = Color.LightYellow,
				Enabled = false
			};
			drawLineButton.Click += (s, e) => {
				SendDrawCommand("line", new
				{
					x1 = 50,
					y1 = 50,
					x2 = 150,
					y2 = 150,
					color = "Red",
					thickness = 3
				});
			};

			Button drawRectButton = new Button
			{
				Text = "Draw Rect",
				Location = new Point(740, 12),
				Size = new Size(80, 27),
				Font = new Font("Arial", 8, FontStyle.Regular),
				BackColor = Color.LightYellow,
				Enabled = false
			};
			drawRectButton.Click += (s, e) => {
				SendDrawCommand("rectangle", new
				{
					x = 100,
					y = 100,
					width = 80,
					height = 60,
					color = "Blue",
					filled = false
				});
			};

			// Enable/disable drawing buttons with command connection
			EventHandler updateDrawingButtons = (s, e) => {
				drawLineButton.Enabled = isCommandConnected;
				drawRectButton.Enabled = isCommandConnected;
			};

			connectCommandButton.Click += updateDrawingButtons;
			disconnectCommandButton.Click += updateDrawingButtons;

			commandPanel.Controls.AddRange(new Control[] { drawLineButton, drawRectButton });
		}
	}
}