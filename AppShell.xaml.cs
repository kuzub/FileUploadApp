namespace FileUploadApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            
            Routing.RegisterRoute("UploadResultsPage", typeof(Views.UploadResultsPage));
        }
    }
}
