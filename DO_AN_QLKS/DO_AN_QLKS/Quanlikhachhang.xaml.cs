using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.ComponentModel;
using System.Data.Entity;   // EF6

namespace DO_AN_QLKS
{
    public partial class Quanlikhachhang : UserControl
    {
        private readonly DatabaseEntities _db = new DatabaseEntities();
        private ICollectionView _view;

        public Quanlikhachhang()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                _db.KhachHang.Load(); 
                _view = CollectionViewSource.GetDefaultView(_db.KhachHang.Local);
                dgCustomers.ItemsSource = _view;

                btnSearch.Click += (s2, e2) => ApplyFilter();
                txtKeyword.TextChanged += (s3, e3) => ApplyFilter();

                btnEdit.Click += (s4, e4) => dgCustomers.BeginEdit();

            };
        }

        private void ApplyFilter()
        {
            string kw = (txtKeyword.Text ?? "").Trim();
            if (string.IsNullOrEmpty(kw))
            {
                _view.Filter = null;
            }
            else
            {
                _view.Filter = o =>
                {
                    var k = o as KhachHang;
                    if (k == null) return false;
                    return (k.HoTen != null && k.HoTen.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                        || (k.DienThoai != null && k.DienThoai.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0);
                };
            }
            _view.Refresh();
        }

        private void dgCustomers_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var kh = e.Row.Item as KhachHang;
                if (kh == null) return;

                if (string.IsNullOrWhiteSpace(kh.HoTen))
                {
                    MessageBox.Show("Họ tên là bắt buộc.");
                    _db.Entry(kh).Reload(); 
                    return;
                }
                if (string.IsNullOrWhiteSpace(kh.DienThoai))
                {
                    MessageBox.Show("Điện thoại là bắt buộc.");
                    _db.Entry(kh).Reload();
                    return;
                }

                bool phoneDup = _db.KhachHang.Any(x => x.DienThoai == kh.DienThoai && x.KhachHangId != kh.KhachHangId);
                if (phoneDup)
                {
                    MessageBox.Show("Số điện thoại đã tồn tại.");
                    _db.Entry(kh).Reload();
                    return;
                }

                if (kh.KhachHangId == 0) kh.TaoLuc = DateTime.Now;

                try
                {
                    _db.SaveChanges();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lưu khách hàng thất bại.\n" + ex.Message, "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    _db.Entry(kh).Reload(); 
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void dgCustomers_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Delete && !dgCustomers.IsReadOnly)
            {
                e.Handled = true;
            }
        }
    }
}
