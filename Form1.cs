namespace TunaLoader
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            // call internal loader initialization that sets up UI and mod system
            OnLoadInternal();
        }

        // Compatibility shim: Designer or other code may call OnLoadInternal.
        // Keep this lightweight to avoid running load logic twice; real startup
        // happens in the overridden OnLoad(EventArgs) in the Designer file.
        private void OnLoadInternal()
        {
            // Intentionally left blank.
        }
    }
}
