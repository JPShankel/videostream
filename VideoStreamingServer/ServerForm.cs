using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

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
        private int connectedClients = 0;
        private const int PORT = 8080;

        public ServerForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(450, 250);
            this.Text = "Video Streaming Server";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Title label
            Label titleLabel = new Label
            {
                Text = "Video Streaming Server",
                Font = new Font("Arial", 14, FontStyle.Bold),
                Location = new Point(50, 20),
                Size = new Size(300, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };

            startButton = new Button
            {
                Text = "Start Server",
                Location = new Point(50, 70),
                Size = new Size(120, 35),
                Font = new Font("Arial", 10, FontStyle.Regular),
                BackColor = Color.LightGreen
            };
            startButton.Click += StartServer_Click;

            stopButton = new Button
            {
                Text = "Stop Server",
                Location = new Point(200, 70),
                Size = new Size(120, 35),
                Font = new Font("Arial", 10, FontStyle.Regular),
                BackColor = Color.LightCoral,
                Enabled = false
            };
            stopButton.Click += StopServer_Click;

            statusLabel = new Label
            {
                Text = "Server stopped",
                Location = new Point(50, 130),
                Size = new Size(300, 20),
                Font = new Font("Arial", 10, FontStyle.Regular)
            };

            clientCountLabel = new Label
            {
                Text = "Connected clients: 0",
                Location = new Point(50, 160),
                Size = new Size(300, 20),
                Font = new Font("Arial", 10, FontStyle.Regular)
            };

            // Port info label
            Label portLabel = new Label
            {
                Text = $"Listening on port: {PORT}",
                Location = new Point(50, 190),
                Size = new Size(300, 20),
                Font = new Font("Arial", 9, FontStyle.Italic),
                ForeColor = Color.Gray
            };

            this.Controls.AddRange(new Control[] 
            { 
                titleLabel, startButton, stopButton, 
                statusLabel, clientCountLabel, portLabel 
            });
        }

        private void StartServer_Click(object sender, EventArgs e)
        {
            try
            {
                tcpListener = new TcpListener(IPAddress.Any, PORT);
                serverThread = new Thread(StartListening);
                serverThread.IsBackground = true;
                serverThread.Start();

                isStreaming = true;
                startButton.Enabled = false;
                stopButton.Enabled = true;
                statusLabel.Text = $"Server started on port {PORT}";
                statusLabel.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting server: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopServer_Click(object sender, EventArgs e)
        {
            isStreaming = false;
            tcpListener?.Stop();
            
            if (serverThread != null && serverThread.IsAlive)
            {
                serverThread.Join(2000);
            }

            startButton.Enabled = true;
            stopButton.Enabled = false;
            statusLabel.Text = "Server stopped";
            statusLabel.ForeColor = Color.Red;
            connectedClients = 0;
            UpdateClientCount();
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
            
            while (isStreaming && client.Connected)
            {
                try
                {
                    // Capture screen
                    Bitmap screenshot = CaptureScreen();
                    
                    // Convert to JPEG with compression
                    byte[] imageData = BitmapToByteArray(screenshot, 75L); // 75% quality
                    
                    // Send frame size first
                    byte[] sizeBytes = BitConverter.GetBytes(imageData.Length);
                    stream.Write(sizeBytes, 0, 4);
                    
                    // Send frame data
                    stream.Write(imageData, 0, imageData.Length);
                    stream.Flush();
                    
                    screenshot.Dispose();
                    
                    // Control frame rate (15 FPS)
                    Thread.Sleep(67); // ~15 FPS
                }
                catch (Exception)
                {
                    // Client disconnected or other error
                    break;
                }
            }
        }

        private Bitmap CaptureScreen()
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            // Scale down for better performance (25% of original size)
            int width = bounds.Width / 4;
            int height = bounds.Height / 4;
            
            Bitmap screenshot = new Bitmap(width, height);
            
            using (Graphics g = Graphics.FromImage(screenshot))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                g.DrawImage(CaptureFullScreen(), 0, 0, width, height);
            }
            
            return screenshot;
        }

        private Bitmap CaptureFullScreen()
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            Bitmap fullScreen = new Bitmap(bounds.Width, bounds.Height);
            
            using (Graphics g = Graphics.FromImage(fullScreen))
            {
                g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size);
            }
            
            return fullScreen;
        }

        private byte[] BitmapToByteArray(Bitmap bitmap, long quality)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                // Set JPEG encoder parameters for compression
                ImageCodecInfo jpegCodec = GetEncoder(ImageFormat.Jpeg);
                EncoderParameters encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);
                
                bitmap.Save(stream, jpegCodec, encoderParams);
                return stream.ToArray();
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            isStreaming = false;
            tcpListener?.Stop();
            
            if (serverThread != null && serverThread.IsAlive)
            {
                serverThread.Join(2000);
            }
            
            base.OnFormClosing(e);
        }
    }
}