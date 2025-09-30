using System.Linq;
using System.Windows;

namespace DO_AN_QLKS
{
    public partial class NguoiDungDialog : Window
    {
        private readonly DatabaseEntities _db = new DatabaseEntities();
        public int? NguoiDungId { get; }

        public NguoiDungDialog(int? nguoiDungId = null)
        {
            InitializeComponent();
            NguoiDungId = nguoiDungId;

            // nạp vai trò
            cbRole.ItemsSource = _db.VaiTro
                .Select(r => new { RoleId = r.VaiTroId, RoleName = r.TenVaiTro })
                .OrderBy(x => x.RoleName)
                .ToList();

            if (NguoiDungId.HasValue)
            {
                Title = "Sửa tài khoản";
                var u = _db.NguoiDung.FirstOrDefault(x => x.NguoiDungId == NguoiDungId.Value);
                if (u != null)
                {
                    txtUsername.Text = u.TenDangNhap;
                    txtFullName.Text = u.HoTen;
                    txtEmail.Text = u.Email;
                    txtPhone.Text = u.DienThoai;
                    cbRole.SelectedValue = u.VaiTroId;
                    chkActive.IsChecked = u.HoatDong;
                }
            }
            else
            {
                Title = "Thêm tài khoản";
                chkActive.IsChecked = true;
            }

            btnSave.Click += BtnSave_Click;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var username = (txtUsername.Text ?? "").Trim();
            var fullname = (txtFullName.Text ?? "").Trim();
            var email = (txtEmail.Text ?? "").Trim();
            var phone = (txtPhone.Text ?? "").Trim();
            var pwd = pbPassword.Password ?? "";
            var roleIdObj = cbRole.SelectedValue;
            var active = chkActive.IsChecked == true;

            // ----- Validate cơ bản -----
            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Username là bắt buộc."); return;
            }
            if (string.IsNullOrWhiteSpace(fullname))
            {
                MessageBox.Show("Họ tên là bắt buộc."); return;
            }
            if (roleIdObj == null)
            {
                MessageBox.Show("Vui lòng chọn vai trò."); return;
            }
            int roleId = (int)roleIdObj;

            // kiểm tra trùng username
            bool dup = _db.NguoiDung.Any(u => u.TenDangNhap == username && u.NguoiDungId != (NguoiDungId ?? 0));
            if (dup)
            {
                MessageBox.Show("Username đã tồn tại."); return;
            }

            if (NguoiDungId.HasValue)
            {
                // ----- Sửa -----
                var u = _db.NguoiDung.First(x => x.NguoiDungId == NguoiDungId.Value);
                u.TenDangNhap = username;
                u.HoTen = fullname;
                u.Email = string.IsNullOrWhiteSpace(email) ? null : email;
                u.DienThoai = string.IsNullOrWhiteSpace(phone) ? null : phone;
                u.VaiTroId = roleId;
                u.HoatDong = active;

                // nếu có nhập mật khẩu mới → cập nhật (plaintext theo yêu cầu)
                if (!string.IsNullOrEmpty(pwd))
                    u.MatKhauHash = pwd;

                _db.SaveChanges();
            }
            else
            {
                // ----- Thêm -----
                if (string.IsNullOrEmpty(pwd))
                {
                    MessageBox.Show("Vui lòng nhập mật khẩu cho tài khoản mới.");
                    return;
                }

                var u = new NguoiDung
                {
                    TenDangNhap = username,
                    MatKhauHash = pwd,      // lưu thuần theo yêu cầu
                    HoTen = fullname,
                    Email = string.IsNullOrWhiteSpace(email) ? null : email,
                    DienThoai = string.IsNullOrWhiteSpace(phone) ? null : phone,
                    VaiTroId = roleId,
                    HoatDong = active,
                    TaoLuc = System.DateTime.Now
                };
                _db.NguoiDung.Add(u);
                _db.SaveChanges();
            }

            DialogResult = true;
            Close();
        }
    }
}
