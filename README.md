The system consists of three applications: The Video Streaming Server, The Video Streaming Client and the Video Streaming Recorder

**Video Streaming Server**
The Video Streaming Server sets up two sockets using TcpListener: a video socket for the streaming imagery and a command socket for communication between client and server
On startup, the server enumerates available camera devices using AForge's DirectShow interface
When the server is started via the UI, the selected camera is activated (InitializeCamera) and the two streams are set up to send and receive data
VideoSource_NewFrame is added to the initialized camera to handle frame events. 
This function gets the frame data and sets a flag responded to in StreamToClient which sends the frame data down the client stream
SimulateNetworkConditions allows the user to set a number of artificial network issues, including delays, limited bandwidth, dropped packets and dropped connections
In addition to the video streaming data, the server can receive commands via the command socket. When the server receives a command, it forwards it to all command
clients (which include the Streaming Client and the Recording Client)

**Video Streaming Client**
The Video Streaming Client sets up two communication sockets (video data and command) and two streams for receiving data.
Video data is handled by ReceiveVideo and command data is handled by ReceiveCommand. 
There are currently no commands the client responds to, but the handler is there and will log commands.
The client may pause the server's playback by hitting 'Pause.' This sends a pause command to the server which suspends transfers from the camera
The video streaming client responds to unstable network by reporting the issue and attempting reconnection

**Video Streaming Recorder**
The Video Streaming Recorder sets up communications similarly to the client. 
When recording is initiated, video data received from the server is saved to an AVI file
Commands sent from the Streaming Client to the Streaming Server are also received when the Streaming Server echoes the messages out. 
The Streaming Recorder stores a record of received commands in a json file
The Streaming Recorder responds to unstable network conditions by reporting and attempting reconnection
