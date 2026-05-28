namespace NovaBrowser;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        try
        {
            Application.Run(new NovaBrowser());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Startup error:\n\n{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}", "Nova Browser - Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}