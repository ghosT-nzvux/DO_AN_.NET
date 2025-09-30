using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace DO_AN_QLKS
{
    public partial class Quanlidatphong : UserControl
    {
        private int? _selectedPhongId = null;   
        public Quanlidatphong()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                btnConfirm.Click += BtnConfirm_Click;

                if (!dpCheckIn.SelectedDate.HasValue) dpCheckIn.SelectedDate = DateTime.Today;
                if (!dpCheckOut.SelectedDate.HasValue) dpCheckOut.SelectedDate = DateTime.Today.AddDays(1);

                LoadAvailableRooms();
                LoadReservations();
            };
        }

        private class AvailableRoomRow
        {
            public int PhongId { get; set; }
            public string RoomNumber { get; set; } 
            public string RoomType { get; set; }   
            public decimal Price { get; set; }   
            public int Capacity { get; set; }   
            public string Notes { get; set; }   
        }

        private class ReservationRow
        {
            public int LuuTruId { get; set; }
            public string BookingCode { get; set; }
            public string CustomerName { get; set; }
            public string RoomNumber { get; set; }
            public DateTime CheckIn { get; set; }   
            public DateTime CheckOut { get; set; }   
            public string Status { get; set; }
            public decimal Total { get; set; }   
            public string Notes { get; set; }
        }


        private void LoadAvailableRooms()
        {
            _selectedPhongId = null;

            if (!ValidateDates(out DateTime ci, out DateTime co))
                return;

            try
            {
                using (var db = new DatabaseEntities())
                {
                    var phongTrong = db.Phong.Where(p => p.TinhTrang == "Trong");

                    var idsBiGiu = (from lt in db.LuuTru
                                    where (lt.TrangThai == "DaDat" || lt.TrangThai == "DaNhan")
                                       && ci < lt.CheckOutDuKien && co > lt.CheckInDuKien
                                    select lt.PhongId).Distinct().ToList();

                    var list = (from p in phongTrong
                                where !idsBiGiu.Contains(p.PhongId)
                                orderby p.SoPhong
                                select new AvailableRoomRow
                                {
                                    PhongId = p.PhongId,
                                    RoomNumber = p.SoPhong,
                                    RoomType = p.LoaiPhong.TenLoai,
                                    Price = p.LoaiPhong.GiaCoBan,
                                    Capacity = p.LoaiPhong.SoNguoiToiDa,
                                    Notes = p.GhiChu
                                }).ToList();

                    dgAvailableRooms.ItemsSource = list;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không tải được danh sách phòng trống.\n" + ex.Message,
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadReservations()
        {
            try
            {
                using (var db = new DatabaseEntities())
                {
                    var rows = (from lt in db.LuuTru
                                orderby lt.LuuTruId descending
                                select new ReservationRow
                                {
                                    LuuTruId = lt.LuuTruId,
                                    BookingCode = "LT" + lt.LuuTruId,
                                    CustomerName = lt.KhachHang.HoTen,
                                    RoomNumber = lt.Phong.SoPhong,
                                    CheckIn = lt.CheckInDuKien,   
                                    CheckOut = lt.CheckOutDuKien, 
                                    Status = lt.TrangThai,
                                    Total = 0m,
                                    Notes = lt.GhiChu
                                }).ToList();

                    dgReservations.ItemsSource = rows;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không tải được danh sách đặt phòng.\n" + ex.Message,
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void PickRoom_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var row = btn?.Tag as AvailableRoomRow;
            if (row == null) return;

            _selectedPhongId = row.PhongId;
            MessageBox.Show($"Đã chọn phòng {row.RoomNumber}.", "Thông báo",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDates(out DateTime ci, out DateTime co)) return;

            if (_selectedPhongId == null)
            {
                MessageBox.Show("Vui lòng chọn phòng trong tab 'Phòng trống'.");
                return;
            }

            var ten = (txtCusName.Text ?? "").Trim();
            var sdt = (txtCusPhone.Text ?? "").Trim();
            var mail = (txtCusEmail.Text ?? "").Trim();
            var cccd = (txtCusIdCard.Text ?? "").Trim();
            var dia = (txtCusAddress.Text ?? "").Trim();
            var note = (txtCusNote.Text ?? "").Trim();

            if (string.IsNullOrWhiteSpace(ten) || string.IsNullOrWhiteSpace(sdt))
            {
                MessageBox.Show("Vui lòng nhập đủ Họ tên và Điện thoại khách hàng.");
                return;
            }

            try
            {
                using (var db = new DatabaseEntities())
                {
                    int phongId = _selectedPhongId.Value;

                    bool biGiu = db.LuuTru.Any(x =>
                        (x.TrangThai == "DaDat" || x.TrangThai == "DaNhan") &&
                        x.PhongId == phongId &&
                        ci < x.CheckOutDuKien && co > x.CheckInDuKien);

                    var phong = db.Phong.FirstOrDefault(p => p.PhongId == phongId);

                    if (phong == null || phong.TinhTrang != "Trong" || biGiu)
                    {
                        MessageBox.Show("Phòng không còn khả dụng, vui lòng chọn phòng khác.");
                        LoadAvailableRooms();
                        return;
                    }

                    var kh = db.KhachHang.FirstOrDefault(k =>
                        (!string.IsNullOrEmpty(sdt) && k.DienThoai == sdt) ||
                        (!string.IsNullOrEmpty(cccd) && k.CCCD == cccd));

                    if (kh == null)
                    {
                        kh = new KhachHang
                        {
                            HoTen = ten,
                            DienThoai = sdt,
                            Email = string.IsNullOrWhiteSpace(mail) ? null : mail,
                            CCCD = string.IsNullOrWhiteSpace(cccd) ? null : cccd,
                            DiaChi = string.IsNullOrWhiteSpace(dia) ? null : dia,
                            GhiChu = string.IsNullOrWhiteSpace(note) ? null : note,
                            TaoLuc = DateTime.Now
                        };
                        db.KhachHang.Add(kh);
                        db.SaveChanges();
                    }
                    else
                    {
                        kh.HoTen = ten;
                        if (!string.IsNullOrWhiteSpace(mail)) kh.Email = mail;
                        if (!string.IsNullOrWhiteSpace(dia)) kh.DiaChi = dia;
                        if (!string.IsNullOrWhiteSpace(note)) kh.GhiChu = note;
                        db.SaveChanges();
                    }

                    var luuTru = new LuuTru
                    {
                        PhongId = phongId,
                        KhachHangId = kh.KhachHangId,
                        CheckInDuKien = ci,
                        CheckOutDuKien = co,
                        TrangThai = "DaDat",
                        TienCoc = 0,
                        SoNguoiLon = 1,
                        SoTreEm = 0,
                        TaoBoiNguoiDungId = CurrentSession.UserId > 0 ? (int?)CurrentSession.UserId : null,
                        TaoLuc = DateTime.Now,
                        GhiChu = note
                    };
                    db.LuuTru.Add(luuTru);

                    phong.TinhTrang = "DaDat";

                    db.SaveChanges();
                }

                MessageBox.Show("Đặt phòng thành công!", "Thành công",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                _selectedPhongId = null;
                LoadAvailableRooms();
                LoadReservations();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Xác nhận đặt phòng thất bại.\n" + ex.Message,
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateDates(out DateTime ci, out DateTime co)
        {
            ci = DateTime.MinValue; co = DateTime.MinValue;

            if (!dpCheckIn.SelectedDate.HasValue || !dpCheckOut.SelectedDate.HasValue)
            {
                MessageBox.Show("Vui lòng chọn Ngày đến và Ngày đi.");
                return false;
            }

            ci = dpCheckIn.SelectedDate.Value.Date;
            co = dpCheckOut.SelectedDate.Value.Date;

            if (co <= ci)
            {
                MessageBox.Show("Ngày đi phải sau Ngày đến.");
                return false;
            }
            return true;
        }

        private void dpCheckIn_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadAvailableRooms();
        }
        private void dpCheckOut_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadAvailableRooms();
        }
    }
}
