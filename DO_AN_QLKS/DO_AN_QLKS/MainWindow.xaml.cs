using System.Windows;

namespace DO_AN_QLKS
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            txtCurrentUser.Text = CurrentSession.HoTen != null
                ? $"{CurrentSession.HoTen} ({CurrentSession.Role})"
                : "(chưa đăng nhập)";

            ApplyPermissions();
        }

        private void ApplyPermissions()
        {
            if (btnUsers != null)
                btnUsers.Visibility = CurrentSession.IsQuanLy ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnUsers_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentSession.IsQuanLy)
            {
                MessageBox.Show("Bạn không có quyền truy cập chức năng này.");
                return;
            }
            ShowPage("Quản lý tài khoản nhân viên", new Quanlitaikhoannhanvien());
        }

        private void BtnCustomers_Click(object sender, RoutedEventArgs e)
        {
            ShowPage("Quản lý khách hàng", new Quanlikhachhang());
        }

        private void BtnServices_Click(object sender, RoutedEventArgs e)
        {
            ShowPage("Quản lý dịch vụ", new Quanlidichvu());
        }

        private void BtnReservation_Click(object sender, RoutedEventArgs e)
        {
            ShowPage("Đặt phòng", new Quanlidatphong());
        }


        private void BtnCheckout_Click(object sender, RoutedEventArgs e)
        {
            ShowPage("Trả phòng & Thanh toán", new Traphongvathanhtoan());
        }

        private void BtnInvoices_Click(object sender, RoutedEventArgs e)
        {
            ShowPage("Quản lý hóa đơn", new Quanlihoadon());
        }

        private void BtnInventory_Click(object sender, RoutedEventArgs e)
        {
            ShowPage("Quản lý kho", new Quanlikho());
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Bạn chắc chắn muốn đăng xuất?", "Xác nhận",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                CurrentSession.UserId = 0;
                CurrentSession.Username = null;
                CurrentSession.HoTen = null;
                CurrentSession.Role = null;

                var login = new Login();
                login.Show();
                this.Close();
            }
        }

        private void ShowPage(string title, object view)
        {
            txtPageTitle.Text = title;
            ContentHost.Content = view;
        }

        private void BtnRooms_Click(object sender, RoutedEventArgs e)
        {
            ShowPage("Quản lý phòng", new Quanliphong());
        }

    }
}
