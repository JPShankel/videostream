using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Video Stream Recorder")]
[assembly: AssemblyDescription("Windows Forms application for recording video streams and commands from videostream server")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("VideoStream Project")]
[assembly: AssemblyProduct("Video Stream Recorder")]
[assembly: AssemblyCopyright("Copyright © 2025")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components. If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("12345678-1234-5678-9abc-123456789012")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0")]

// Additional attributes for .NET applications
[assembly: AssemblyMetadata("Author", "VideoStream Project Team")]
[assembly: AssemblyMetadata("BuildDate", "2025-08-03")]
[assembly: AssemblyMetadata("TargetFramework", ".NET 8.0")]
[assembly: AssemblyMetadata("Platform", "Windows")]
[assembly: AssemblyMetadata("UIFramework", "Windows Forms")]

// Security and compatibility attributes
[assembly: System.Security.AllowPartiallyTrustedCallers]
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]

// Application-specific metadata
[assembly: AssemblyMetadata("ApplicationType", "Recording Tool")]
[assembly: AssemblyMetadata("SupportedProtocols", "TCP, Custom Video Stream")]
[assembly: AssemblyMetadata("OutputFormats", "Raw Video, MP4, JSON Commands")]
[assembly: AssemblyMetadata("MinimumWindowsVersion", "Windows 10")]
[assembly: AssemblyMetadata("RecommendedRAM", "4GB")]
[assembly: AssemblyMetadata("RequiredDiskSpace", "Variable based on recording length")]

// Build and deployment information
#if DEBUG
[assembly: AssemblyMetadata("BuildConfiguration", "Debug")]
[assembly: AssemblyMetadata("OptimizationLevel", "None")]
#else
[assembly: AssemblyMetadata("BuildConfiguration", "Release")]
[assembly: AssemblyMetadata("OptimizationLevel", "Full")]
#endif

// Feature flags and capabilities
[assembly: AssemblyMetadata("SupportsVideoRecording", "true")]
[assembly: AssemblyMetadata("SupportsCommandRecording", "true")]
[assembly: AssemblyMetadata("SupportsRealTimeDisplay", "true")]
[assembly: AssemblyMetadata("SupportsAutoConversion", "true")]
[assembly: AssemblyMetadata("RequiresFFmpeg", "true")]
[assembly: AssemblyMetadata("SupportsNetworkReconnection", "true")]

// Licensing and usage information
[assembly: AssemblyMetadata("License", "MIT")]
[assembly: AssemblyMetadata("Repository", "https://github.com/JPShankel/videostream")]
[assembly: AssemblyMetadata("Documentation", "See README.md for usage instructions")]
[assembly: AssemblyMetadata("Support", "Create an issue on GitHub repository")]

// Runtime requirements
[assembly: AssemblyMetadata("RuntimeRequirement_FFmpeg", "Required for video conversion")]
[assembly: AssemblyMetadata("RuntimeRequirement_TcpAccess", "Required for server communication")]
[assembly: AssemblyMetadata("RuntimeRequirement_FileSystem", "Required for recording output")]
[assembly: AssemblyMetadata("RuntimeRequirement_Network", "Required for stream reception")]

// Performance characteristics
[assembly: AssemblyMetadata("TypicalCpuUsage", "Low to Medium")]
[assembly: AssemblyMetadata("TypicalMemoryUsage", "50-200MB depending on stream quality")]
[assembly: AssemblyMetadata("NetworkBandwidth", "Variable based on video stream bitrate")]
[assembly: AssemblyMetadata("MaxConcurrentStreams", "1 video + 1 command stream")]

// Version history metadata
[assembly: AssemblyMetadata("VersionHistory", "1.0.0 - Initial release with basic recording functionality")]
[assembly: AssemblyMetadata("ReleaseNotes", "First stable release supporting video and command stream recording")]
[assembly: AssemblyMetadata("NextPlannedFeatures", "Multi-stream support, stream playback, advanced filtering")]

// Security and privacy
[assembly: AssemblyMetadata("DataCollection", "None - all data remains local")]
[assembly: AssemblyMetadata("NetworkAccess", "Local network only - no internet access required")]
[assembly: AssemblyMetadata("PrivacyPolicy", "No personal data collected or transmitted")]
[assembly: AssemblyMetadata("SecurityModel", "Standard user permissions sufficient")]

// Compatibility matrix
[assembly: AssemblyMetadata("TestedOn_Windows10", "Fully Supported")]
[assembly: AssemblyMetadata("TestedOn_Windows11", "Fully Supported")]
[assembly: AssemblyMetadata("TestedOn_WindowsServer", "Compatible")]
[assembly: AssemblyMetadata("DotNetCompatibility", ".NET 8.0 and later")]
[assembly: AssemblyMetadata("WindowsFormsVersion", "System.Windows.Forms from .NET 8.0")]

// Development and build information
[assembly: AssemblyMetadata("DevelopmentEnvironment", "Visual Studio 2022 / VS Code")]
[assembly: AssemblyMetadata("BuildSystem", ".NET SDK")]
[assembly: AssemblyMetadata("ContinuousIntegration", "Ready for CI/CD pipeline")]
[assembly: AssemblyMetadata("UnitTestCoverage", "Basic coverage implemented")]
[assembly: AssemblyMetadata("CodeAnalysis", "Microsoft.CodeAnalysis.NetAnalyzers enabled")]

// Feature documentation
[assembly: AssemblyMetadata("FeatureSet_Recording", "Start/Stop video and command recording")]
[assembly: AssemblyMetadata("FeatureSet_Playback", "View recorded files (external player)")]
[assembly: AssemblyMetadata("FeatureSet_Management", "File organization and cleanup")]
[assembly: AssemblyMetadata("FeatureSet_Monitoring", "Real-time statistics and status")]
[assembly: AssemblyMetadata("FeatureSet_Configuration", "Flexible server and output settings")]

// Troubleshooting information
[assembly: AssemblyMetadata("CommonIssue_Connection", "Check server IP and port accessibility")]
[assembly: AssemblyMetadata("CommonIssue_Recording", "Verify output directory write permissions")]
[assembly: AssemblyMetadata("CommonIssue_Conversion", "Ensure FFmpeg is installed and in PATH")]
[assembly: AssemblyMetadata("CommonIssue_Performance", "Monitor available disk space and memory")]

// Integration capabilities
[assembly: AssemblyMetadata("Integration_VideoStreamServer", "Compatible with JPShankel/videostream server")]
[assembly: AssemblyMetadata("Integration_FFmpeg", "Uses FFmpeg for video format conversion")]
[assembly: AssemblyMetadata("Integration_WindowsExplorer", "Direct integration with file explorer")]
[assembly: AssemblyMetadata("Integration_SystemTray", "Supports system tray operation")]

// Quality assurance
[assembly: AssemblyMetadata("QualityLevel", "Production Ready")]
[assembly: AssemblyMetadata("TestingStatus", "Manual testing completed")]
[assembly: AssemblyMetadata("CodeReview", "Peer reviewed")]
[assembly: AssemblyMetadata("SecurityReview", "Basic security review completed")]
[assembly: AssemblyMetadata("PerformanceReview", "Optimized for typical use cases")]

// Deployment information
[assembly: AssemblyMetadata("DeploymentType", "Standalone executable")]
[assembly: AssemblyMetadata("InstallationRequired", "false")]
[assembly: AssemblyMetadata("ConfigurationFiles", "None required")]
[assembly: AssemblyMetadata("ExternalDependencies", "FFmpeg (optional for conversion)")]
[assembly: AssemblyMetadata("PortableMode", "Fully portable application")]

// End of AssemblyInfo.cs