namespace VinhKhanhTourGuide
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Nếu sau này bạn muốn dùng hệ thống Navigation bằng đường dẫn (URI) 
            // của Shell thay cho PushAsync, bạn có thể đăng ký các trang con ở đây:
            // Routing.RegisterRoute(nameof(Views.EateryDetailPage), typeof(Views.EateryDetailPage));
        }
    }
}