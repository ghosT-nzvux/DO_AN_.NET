using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DO_AN_QLKS
{
    public partial class Quanlitaikhoannhanvien : UserControl
    {
        public Quanlitaikhoannhanvien()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                if (!CurrentSession.IsQuanLy)
                {
                    MessageBox.Show("Bạn không có quyền truy cập chức năng này.");
                    this.IsEnabled = false;
                    return;
                }

                btnAdd.Click += BtnAdd_Click;
                btnEdit.Click += BtnEdit_Click;
                btnDelete.Click += BtnDelete_Click;

                LoadUsers();
            };
        }

        private class UserRow
        {
            public int NguoiDungId { get; set; }
            public string Username { get; set; }   
            public string FullName { get; set; }  
            public string Email { get; set; }
            public string Phone { get; set; }  
            public string RoleName { get; set; }   
            public bool IsActive { get; set; } 
            public DateTime CreatedAt { get; set; }   
        }

        private void LoadUsers()
        {
            try
            {
                using (var db = new DatabaseEntities())
                {
                    var data = (from u in db.NguoiDung
                                join r in db.VaiTro on u.VaiTroId equals r.VaiTroId
                                orderby u.NguoiDungId descending
                                select new UserRow
                                {
                                    NguoiDungId = u.NguoiDungId,
                                    Username = u.TenDangNhap,
                                    FullName = u.HoTen,
                                    Email = u.Email,
                                    Phone = u.DienThoai,
                                    RoleName = r.TenVaiTro,
                                    IsActive = u.HoatDong,
                                    CreatedAt = u.TaoLuc
                                }).ToList();

                    dgUsers.ItemsSource = data;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không tải được danh sách tài khoản.\n" + ex.Message,
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private UserRow GetSelected()
        {
            var row = dgUsers.SelectedItem as UserRow;
            if (row == null)
                MessageBox.Show("Hãy chọn một tài khoản.", "Thông báo",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            return row;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new NguoiDungDialog(null) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
                LoadUsers();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var row = GetSelected();
            if (row == null) return;

            var dlg = new NguoiDungDialog(row.NguoiDungId) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
                LoadUsers();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var row = GetSelected();
            if (row == null) return;

            if (CurrentSession.UserId == row.NguoiDungId)
            {
                MessageBox.Show("Bạn không thể tự xóa tài khoản đang đăng nhập.",
                                "Không thể xóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Xóa tài khoản '{row.Username}'?",
                                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            try
            {
                using (var db = new DatabaseEntities())
                {
                    var ent = db.NguoiDung.FirstOrDefault(x => x.NguoiDungId == row.NguoiDungId);
                    if (ent == null)
                    {
                        MessageBox.Show("Bản ghi không tồn tại.", "Lỗi",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }


                    db.NguoiDung.Remove(ent);
                    db.SaveChanges();
                }

                LoadUsers();
                MessageBox.Show("Đã xóa tài khoản.", "Thành công",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Xóa tài khoản thất bại.\n" + ex.Message,
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
