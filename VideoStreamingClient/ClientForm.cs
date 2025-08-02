using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace VideoStreamingClient
{
	public partial class ClientForm : Form
	{
		private TcpClient tcpClient;
		private NetworkStream stream;
		private Thread receiveThread;
		private bool isConnected = false;

		private TextBox serverIpTextBox;
		private Button connectButton;
		private Button disconnectButton;
		private PictureBox videoPictureBox;
		private Label statusLabel;
		private Label fpsLabel;
		private Panel controlPanel;

		private int frameCount = 0;
		private DateTime lastFpsUpdate = DateTime.Now;
		private const int PORT = 8080;

		public ClientForm()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			this.Size = new Size(900, 700);
			this.Text = "Video Streaming Client";
			this.StartPosition = FormStartPosition.CenterScreen;
			this.MinimumSize = new Size(600, 400);

			// Control panel at the top
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
				Text = "Connect",
				Location = new Point(220, 15),
				Size = new Size(80, 27),
				Font = new Font("Arial", 9, FontStyle.Regular),
				BackColor = Color.LightGreen
			};
			connectButton.Click += Connect_Click;

			disconnectButton = new Button
			{
				Text = "Disconnect",
				Location = new Point(320, 15),
				Size = new Size(80, 27),
				Font = new Font("Arial", 9, FontStyle.Regular),
				BackColor = Color.LightCoral,
				Enabled = false
			};
			disconnectButton.Click += Disconnect_Click;

			statusLabel = new Label
			{
				Text = "Disconnected",
				Location = new Point(420, 20),
				Size = new Size(150, 20),
				Font = new Font("Arial", 9, FontStyle.Regular),
				ForeColor = Color.Red
			};

			fpsLabel = new Label
			{
				Text = "FPS: 0",
				Location = new Point(580, 20),
				Size = new Size(80, 20),
				Font = new Font("Arial", 9, FontStyle.Regular),
				ForeColor = Color.Blue
			};

			controlPanel.Controls.AddRange(new Control[]
			{
				ipLabel, serverIpTextBox, connectButton,
				disconnectButton, statusLabel, fpsLabel
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

			this.Controls.AddRange(new Control[] { controlPanel, videoPictureBox });
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
				statusLabel.Text = "Connecting...";
				statusLabel.ForeColor = Color.Orange;
				Application.DoEvents();

				tcpClient = new TcpClient();
				tcpClient.Connect(serverIpTextBox.Text, PORT);
				stream = tcpClient.GetStream();

				isConnected = true;
				receiveThread = new Thread(ReceiveVideo);
				receiveThread.IsBackground = true;
				receiveThread.Start();

				connectButton.Enabled = false;
				disconnectButton.Enabled = true;
				serverIpTextBox.Enabled = false;
				statusLabel.Text = "Connected";
				statusLabel.ForeColor = Color.Green;

				// Clear placeholder
				videoPictureBox.Controls.Clear();
			}
			catch (Exception ex)
			{
				statusLabel.Text = "Connection failed";
				statusLabel.ForeColor = Color.Red;
				MessageBox.Show($"Connection failed: {ex.Message}", "Connection Error",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void Disconnect_Click(object sender, EventArgs e)
		{
			DisconnectFromServer();
		}

		private void DisconnectFromServer()
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
			statusLabel.Text = "Disconnected";
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
							MessageBox.Show($"Receive error: {ex.Message}", "Error",
								MessageBoxButtons.OK, MessageBoxIcon.Error);
							DisconnectFromServer();
						}));
					}
					break;
				}
			}
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			DisconnectFromServer();
			base.OnFormClosing(e);
		}
	}
}