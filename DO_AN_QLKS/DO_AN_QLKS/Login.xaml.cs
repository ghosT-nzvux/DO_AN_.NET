using System;
using System.Linq;
using System.Windows;

namespace DO_AN_QLKS
{
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
            txtPassword.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                    BtnLogin_Click(btnLogin, new RoutedEventArgs());
            };
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = (txtUsername.Text ?? "").Trim();
            string password = txtPassword.Password ?? "";

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Vui lòng nhập tài khoản và mật khẩu.", "Thiếu thông tin",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var db = new DatabaseEntities())
                {
                    var user = db.NguoiDung.FirstOrDefault(u => u.TenDangNhap == username);
                    if (user == null)
                    {
                        MessageBox.Show("Tài khoản không tồn tại.", "Đăng nhập thất bại",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (user.HoatDong == false)
                    {
                        MessageBox.Show("Tài khoản đang bị khóa.", "Đăng nhập thất bại",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // >>> So sánh mật khẩu thuần (không băm) <<<
                    if (!string.Equals(user.MatKhauHash, password))
                    {
                        MessageBox.Show("Mật khẩu không đúng.", "Đăng nhập thất bại",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var role = db.VaiTro.FirstOrDefault(r => r.VaiTroId == user.VaiTroId)?.TenVaiTro ?? "NhanVien";

                    user.LanDangNhapCuoi = DateTime.Now;
                    db.SaveChanges();

                    CurrentSession.UserId = user.NguoiDungId;
                    CurrentSession.Username = user.TenDangNhap;
                    CurrentSession.HoTen = user.HoTen;
                    CurrentSession.Role = role;

                    var main = new MainWindow();
                    main.Show();
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Có lỗi khi đăng nhập: " + ex.Message, "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e) => Close();
    }

    public static class CurrentSession
    {
        public static int UserId { get; set; }
        public static string Username { get; set; }
        public static string HoTen { get; set; }
        public static string Role { get; set; }
        public static bool IsQuanLy =>
            string.Equals(Role, "QuanLy", StringComparison.OrdinalIgnoreCase);
    }
}
