using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Text;
using System.Diagnostics;


namespace DO_AN_QLKS
{
    public partial class Quanliphong : UserControl
    {
        private readonly DatabaseEntities _db = new DatabaseEntities();

        public Quanliphong()
        {
            InitializeComponent();

            this.Loaded += (s, e) =>
            {
                btnAdd.Click += BtnAdd_Click;
                btnEdit.Click += BtnEdit_Click;

                LoadData();
            };
        }

        public class RoomRow
        {
            public int PhongId { get; set; }
            public string Number { get; set; }
            public string RoomTypeName { get; set; }
            public bool HieuLuc { get; set; }
            public decimal BasePrice { get; set; }
            public int Capacity { get; set; }
            public string Status { get; set; }
            public string Notes { get; set; }
            public int LoaiPhongId { get; set; }
        }

        private void LoadData()
        {
            try
            {
                using (var db = new DatabaseEntities()) 
                {
                    var data = (from p in db.Phong.AsNoTracking()
                                join lp in db.LoaiPhong.AsNoTracking()
                                  on p.LoaiPhongId equals lp.LoaiPhongId into gj
                                from lp in gj.DefaultIfEmpty()
                                orderby p.SoPhong
                                select new RoomRow
                                {
                                    PhongId = p.PhongId,
                                    Number = p.SoPhong,
                                    RoomTypeName = lp != null ? lp.TenLoai : "(chưa gán loại)",
                                    BasePrice = lp != null ? lp.GiaCoBan : 0m,
                                    Capacity = lp != null ? lp.SoNguoiToiDa : 0,
                                    Status = p.TinhTrang,
                                    Notes = p.GhiChu,
                                    LoaiPhongId = p.LoaiPhongId
                                }).ToList();

                    dgRooms.ItemsSource = data;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không tải được danh sách phòng.\n" + ex.Message,
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private RoomRow GetSelected()
        {
            var row = dgRooms.SelectedItem as RoomRow;
            if (row == null)
            {
                MessageBox.Show("Hãy chọn một phòng trong danh sách.", "Thông báo",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            return row;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadData();

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new PhongDialog(null) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
            {
                LoadData();
                MessageBox.Show("Đã thêm phòng.", "Thành công",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var row = GetSelected();
            if (row == null) return;

            var dlg = new PhongDialog(row.PhongId) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
            {
                LoadData();
                MessageBox.Show("Đã cập nhật phòng.", "Thành công",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

       
    }
}
