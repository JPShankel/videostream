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
		private Thread serverThread;
		private bool isStreaming = false;
		private Button startButton;
		private Button stopButton;
		private Label statusLabel;
		private Label clientCountLabel;
		private ComboBox cameraComboBox;
		private int connectedClients = 0;
		private const int PORT = 8080;

		// AForge.NET objects (alternative to DirectShow)
		private FilterInfoCollection videoDevices;
		private VideoCaptureDevice videoSource;
		private readonly object frameLock = new object();
		private byte[] currentFrame;
		private bool hasNewFrame = false;

		public ServerForm()
		{
			InitializeComponent();
			LoadCameraDevices();
		}

		private void InitializeComponent()
		{
			this.Size = new Size(450, 300);
			this.Text = "Camera Video Streaming Server";
			this.StartPosition = FormStartPosition.CenterScreen;
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;

			// Title label
			Label titleLabel = new Label
			{
				Text = "Camera Video Streaming Server",
				Font = new Font("Arial", 14, FontStyle.Bold),
				Location = new Point(50, 20),
				Size = new Size(350, 25),
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
				Size = new Size(300, 20),
				Font = new Font("Arial", 10, FontStyle.Regular)
			};

			clientCountLabel = new Label
			{
				Text = "Connected clients: 0",
				Location = new Point(50, 190),
				Size = new Size(300, 20),
				Font = new Font("Arial", 10, FontStyle.Regular)
			};

			// Port info label
			Label portLabel = new Label
			{
				Text = $"Listening on port: {PORT}",
				Location = new Point(50, 220),
				Size = new Size(300, 20),
				Font = new Font("Arial", 9, FontStyle.Italic),
				ForeColor = Color.Gray
			};

			this.Controls.AddRange(new Control[]
			{
				titleLabel, cameraLabel, cameraComboBox, startButton, stopButton,
				statusLabel, clientCountLabel, portLabel
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

				// Start TCP server
				tcpListener = new TcpListener(IPAddress.Any, PORT);
				serverThread = new Thread(StartListening);
				serverThread.IsBackground = true;
				serverThread.Start();

				isStreaming = true;
				startButton.Enabled = false;
				stopButton.Enabled = true;
				cameraComboBox.Enabled = false;
				statusLabel.Text = $"Server started on port {PORT} with camera";
				statusLabel.ForeColor = Color.Green;
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

			// Stop TCP server
			tcpListener?.Stop();

			if (serverThread != null && serverThread.IsAlive)
			{
				serverThread.Join(2000);
			}

			startButton.Enabled = true;
			stopButton.Enabled = false;
			cameraComboBox.Enabled = true;
			statusLabel.Text = "Server stopped";
			statusLabel.ForeColor = Color.Red;
			connectedClients = 0;
			UpdateClientCount();
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
						int newWidth = Math.Min(640, originalFrame.Width / 2);
						int newHeight = Math.Min(480, originalFrame.Height / 2);

						// Ensure minimum size
						if (newWidth < 160) newWidth = 160;
						if (newHeight < 120) newHeight = 120;

						using (Bitmap scaledBitmap = new Bitmap(newWidth, newHeight, PixelFormat.Format24bppRgb))
						{
							using (Graphics g = Graphics.FromImage(scaledBitmap))
							{
								// Create pen.
								Pen blackPen = new Pen(Color.Black, 3);

								// Create coordinates of points that define line.
								float x1 = 100.0F;
								float y1 = 100.0F;
								float x2 = 500.0F;
								float y2 = 100.0F;

								// Draw line to screen.
								g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
								g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
								g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
								g.DrawImage(originalFrame, 0, 0, newWidth, newHeight);
								g.DrawLine(blackPen, x1, y1, x2, y2);
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

		private void StartListening()
		{
			tcpListener.Start();

			while (isStreaming)
			{
				try
				{
					TcpClient client = tcpListener.AcceptTcpClient();

					// Handle each client in a separate thread
					Thread clientThread = new Thread(() => HandleClient(client));
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
							MessageBox.Show($"Server error: {ex.Message}", "Error",
								MessageBoxButtons.OK, MessageBoxIcon.Error)));
					}
					break;
				}
			}
		}

		private void HandleClient(TcpClient client)
		{
			try
			{
				Interlocked.Increment(ref connectedClients);
				this.Invoke(new Action(() => UpdateClientCount()));

				StreamToClient(client);
			}
			finally
			{
				Interlocked.Decrement(ref connectedClients);
				this.Invoke(new Action(() => UpdateClientCount()));
				client?.Close();
			}
		}

		private void UpdateClientCount()
		{
			clientCountLabel.Text = $"Connected clients: {connectedClients}";
		}

		private void StreamToClient(TcpClient client)
		{
			NetworkStream stream = client.GetStream();
			byte[] lastSentFrame = null;

			while (isStreaming && client.Connected)
			{
				try
				{
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
						encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);

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
			tcpListener?.Stop();

			if (serverThread != null && serverThread.IsAlive)
			{
				serverThread.Join(2000);
			}

			base.OnFormClosing(e);
		}
	}
}