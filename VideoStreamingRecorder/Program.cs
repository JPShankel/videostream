using VideoStreamRecorder.Forms;

namespace VideoStreamRecorder;

internal static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Configure Windows Forms application
        ApplicationConfiguration.Initialize();
        
        // Set up global exception handling
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += Application_ThreadException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        
        try
        {
            // Create and run the main recorder form
            Application.Run(new RecorderMainForm());
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Application Startup Error", ex);
        }
    }
    
    private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
    {
        ShowErrorDialog("Application Error", e.Exception);
    }
    
    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            ShowErrorDialog("Unhandled Error", ex);
        }
        else
        {
            MessageBox.Show(
                "An unknown error occurred. The application will now exit.",
                "Critical Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
    
    private static void ShowErrorDialog(string title, Exception ex)
    {
        var errorMessage = $"Error: {ex.Message}\n\n" +
                          $"Type: {ex.GetType().Name}\n\n" +
                          $"Stack Trace:\n{ex.StackTrace}";
        
        MessageBox.Show(
            errorMessage,
            title,
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}