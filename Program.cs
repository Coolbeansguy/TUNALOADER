namespace TunaLoader
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                // To customize application configuration such as set high DPI settings or default font,
                // see https://aka.ms/applicationconfiguration.
                ApplicationConfiguration.Initialize();
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                // Log and show unexpected startup errors so the user knows why the EXE didn't start.
                try
                {
                    string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TunaLoader.log");
                    System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:O}] Fatal: {ex}\r\n");
                }
                catch { }

                MessageBox.Show($"TunaLoader failed to start:\n{ex.Message}", "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}