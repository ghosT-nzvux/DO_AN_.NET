using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DO_AN_QLKS
{
    public partial class Quanlidichvu : UserControl
    {
        private readonly DatabaseEntities _db = new DatabaseEntities();
        private readonly ObservableCollection<ServiceRow> _items = new ObservableCollection<ServiceRow>();

        public Quanlidichvu()
        {
            InitializeComponent();
            dgServices.ItemsSource = _items;

            Loaded += (s, e) =>
            {
                btnAdd.Click += BtnAdd_Click;
                btnEdit.Click += BtnEdit_Click;
                btnDelete.Click += BtnDelete_Click;
                RefreshGrid();
            };
        }

        private void RefreshGrid()
        {
            _items.Clear();

            var data = _db.Set<DichVu>()
                .OrderBy(d => d.TenDichVu)
                .Select(d => new ServiceRow
                {
                    DichVuId = d.DichVuId,
                    Code = "DV" + d.DichVuId,           
                    Name = d.TenDichVu,
                    Category = d.LaVatTuKho ? "Vật tư kho" : "Dịch vụ",
                    Unit = d.DonViTinh,
                    Price = d.GiaBan,
                    VatRate = 0m,                          
                    IsActive = d.HieuLuc,
                    Notes = d.GhiChu
                })
                .ToList();

            foreach (var r in data) _items.Add(r);
        }

        private ServiceRow GetSelected() => dgServices.SelectedItem as ServiceRow;

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            string ten = SimplePrompt.Ask("Tên dịch vụ:");
            if (string.IsNullOrWhiteSpace(ten)) return;
            ten = ten.Trim();

            if (_db.Set<DichVu>().Any(x => x.TenDichVu == ten))
            {
                MessageBox.Show("Tên dịch vụ đã tồn tại.", "Trùng dữ liệu",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string dvt = SimplePrompt.Ask("Đơn vị tính:");
            if (string.IsNullOrWhiteSpace(dvt)) dvt = null;

            string giaStr = SimplePrompt.Ask("Giá bán:");
            if (!decimal.TryParse(giaStr, out var gia) || gia < 0)
            {
                MessageBox.Show("Giá bán không hợp lệ.", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool laVatTuKho = MessageBox.Show("Đánh dấu là VẬT TƯ KHO?", "Phân loại",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

            var dv = new DichVu
            {
                TenDichVu = ten,
                DonViTinh = dvt?.Trim(),
                GiaBan = gia,
                LaVatTuKho = laVatTuKho,
                GhiChu = null,
                HieuLuc = true
            };

            try
            {
                _db.Set<DichVu>().Add(dv);
                _db.SaveChanges();

                if (dv.LaVatTuKho == true) EnsureVatTuForDichVu(dv);

                RefreshGrid();
                MessageBox.Show("Đã thêm dịch vụ.", "OK",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể thêm dịch vụ.\n" + ExplainDbException(ex),
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelected();
            if (selected == null)
            {
                MessageBox.Show("Hãy chọn một dòng để sửa.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dv = _db.Set<DichVu>().FirstOrDefault(x => x.DichVuId == selected.DichVuId);
            if (dv == null)
            {
                MessageBox.Show("Bản ghi không còn tồn tại.", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                RefreshGrid();
                return;
            }

            bool oldIsStock = dv.LaVatTuKho;

            string ten = SimplePrompt.Ask("Tên dịch vụ:", dv.TenDichVu ?? "");
            if (string.IsNullOrWhiteSpace(ten)) return;
            ten = ten.Trim();

            if (_db.Set<DichVu>().Any(x => x.DichVuId != dv.DichVuId && x.TenDichVu == ten))
            {
                MessageBox.Show("Tên dịch vụ đã tồn tại.", "Trùng dữ liệu",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string dvt = SimplePrompt.Ask("Đơn vị tính:", dv.DonViTinh ?? "");
            if (string.IsNullOrWhiteSpace(dvt)) dvt = null;

            string giaStr = SimplePrompt.Ask("Giá bán:", dv.GiaBan.ToString("0.##"));
            if (!decimal.TryParse(giaStr, out var gia) || gia < 0)
            {
                MessageBox.Show("Giá bán không hợp lệ.", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool laVatTuKho = MessageBox.Show("Đánh dấu là VẬT TƯ KHO?", "Phân loại",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

            dv.TenDichVu = ten;
            dv.DonViTinh = dvt?.Trim();
            dv.GiaBan = gia;
            dv.LaVatTuKho = laVatTuKho;

            try
            {
                _db.SaveChanges();

                if (laVatTuKho) EnsureVatTuForDichVu(dv);
                else if (oldIsStock && !laVatTuKho) DeactivateVatTuByDichVuName(dv.TenDichVu);

                RefreshGrid();
                MessageBox.Show("Đã cập nhật dịch vụ.", "OK",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể cập nhật dịch vụ.\n" + ExplainDbException(ex),
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelected();
            if (selected == null)
            {
                MessageBox.Show("Hãy chọn một dòng để xóa.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"Xóa dịch vụ: {selected.Name} ?", "Xác nhận",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            var dv = _db.Set<DichVu>().FirstOrDefault(x => x.DichVuId == selected.DichVuId);
            if (dv == null)
            {
                MessageBox.Show("Bản ghi không còn tồn tại.", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                RefreshGrid();
                return;
            }

            try
            {
                _db.Set<DichVu>().Remove(dv);
                _db.SaveChanges();
                RefreshGrid();
                MessageBox.Show("Đã xóa dịch vụ.", "OK",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show(
                    "Không thể xóa do đang được sử dụng ở nghiệp vụ (ví dụ: Sử dụng dịch vụ).\n" +
                    "Bạn có thể đặt HieuLuc = 0 thay vì xóa cứng.\n\nChi tiết:\n" + ExplainDbException(ex),
                    "Xóa thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Xóa thất bại.\n" + ExplainDbException(ex),
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EnsureVatTuForDichVu(DichVu dv)
        {
            var name = (dv.TenDichVu ?? "").Trim();
            if (name.Length == 0) return;

            var vt = _db.Set<VatTu>()
                        .FirstOrDefault(x => x.TenVatTu.ToLower() == name.ToLower());

            if (vt == null)
            {
                vt = new VatTu
                {
                    TenVatTu = dv.TenDichVu,
                    DonViTinh = dv.DonViTinh,
                    SoLuongTon = 0, 
                    GhiChu = "Tự tạo từ dịch vụ (LaVatTuKho=true)",
                    HieuLuc = dv.HieuLuc
                };
                _db.Set<VatTu>().Add(vt);
                _db.SaveChanges();
            }
            else
            {
                vt.DonViTinh = dv.DonViTinh;
                vt.HieuLuc = dv.HieuLuc;
                _db.SaveChanges();
            }
        }

        private void DeactivateVatTuByDichVuName(string tenDichVu)
        {
            var vt = _db.Set<VatTu>()
                        .FirstOrDefault(x => x.TenVatTu.ToLower() == (tenDichVu ?? "").ToLower());
            if (vt != null)
            {
                vt.HieuLuc = false;
                _db.SaveChanges();
            }
        }

        public class ServiceRow : INotifyPropertyChanged
        {
            public int DichVuId { get; set; }
            private string _code; public string Code { get => _code; set { _code = value; OnChanged(nameof(Code)); } }
            private string _name; public string Name { get => _name; set { _name = value; OnChanged(nameof(Name)); } }
            private string _category; public string Category { get => _category; set { _category = value; OnChanged(nameof(Category)); } }
            private string _unit; public string Unit { get => _unit; set { _unit = value; OnChanged(nameof(Unit)); } }
            private decimal _price; public decimal Price { get => _price; set { _price = value; OnChanged(nameof(Price)); } }
            private decimal _vat; public decimal VatRate { get => _vat; set { _vat = value; OnChanged(nameof(VatRate)); } }
            private bool _active; public bool IsActive { get => _active; set { _active = value; OnChanged(nameof(IsActive)); } }
            private string _notes; public string Notes { get => _notes; set { _notes = value; OnChanged(nameof(Notes)); } }
            public event PropertyChangedEventHandler PropertyChanged;
            void OnChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        }

        private string ExplainDbException(Exception ex)
        {
            var root = ex;
            while (root.InnerException != null) root = root.InnerException;

            if (root is SqlException sql)
            {
                foreach (SqlError err in sql.Errors)
                {
                    if (err.Number == 2627 || err.Number == 2601) return "Dữ liệu trùng UNIQUE/INDEX (có thể tên dịch vụ đã tồn tại).";
                    if (err.Number == 547) return "Vi phạm ràng buộc (CHECK/FK). Kiểm tra lại dữ liệu.";
                }
                return $"SQL {sql.Number}: {sql.Message}";
            }
            if (root is DbEntityValidationException val)
            {
                var errs = string.Join("; ", val.EntityValidationErrors
                    .SelectMany(v => v.ValidationErrors)
                    .Select(v => $"{v.PropertyName}: {v.ErrorMessage}"));
                return "Lỗi validate entity: " + errs;
            }
            return root.Message;
        }
    }

    internal static class SimplePrompt
    {
        public static string Ask(string message, string defaultValue = "")
        {
            var win = new Window
            {
                Title = "Nhập liệu",
                Width = 420,
                Height = 160,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false
            };
            var grid = new Grid { Margin = new Thickness(12) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var tbMsg = new TextBlock { Text = message, Margin = new Thickness(0, 0, 0, 8) };
            Grid.SetRow(tbMsg, 0);
            var txt = new TextBox { Text = defaultValue, MinWidth = 360 };
            Grid.SetRow(txt, 1);
            var sp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
            var ok = new Button { Content = "OK", Padding = new Thickness(12, 6, 12, 6), IsDefault = true };
            var cancel = new Button { Content = "Hủy", Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(8, 0, 0, 0), IsCancel = true };
            ok.Click += (s, e) => { win.DialogResult = true; };
            sp.Children.Add(ok); sp.Children.Add(cancel);
            Grid.SetRow(sp, 2);

            grid.Children.Add(tbMsg); grid.Children.Add(txt); grid.Children.Add(sp);
            win.Content = grid;

            var owner = Application.Current?.Windows?.OfType<Window>()?.FirstOrDefault(w => w.IsActive);
            if (owner != null) win.Owner = owner;

            if (win.ShowDialog() == true) return txt.Text;
            return null;
        }
    }
}
