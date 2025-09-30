    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Data.Entity.Infrastructure;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;

    namespace DO_AN_QLKS
    {
        public partial class Quanlikho : UserControl
        {
            private readonly DatabaseEntities _db = new DatabaseEntities();
            private readonly ObservableCollection<InventoryRow> _items = new ObservableCollection<InventoryRow>();

            public Quanlikho()
            {
                InitializeComponent();
                dgInventory.ItemsSource = _items;

            Loaded += (s, e) =>
            {
                var btnExportRef = this.FindName("btnExport") as Button;
                if (btnExportRef != null) btnExportRef.Visibility = Visibility.Collapsed;
                btnImport.Click += BtnImport_Click;
                RefreshGrid();
            };

        }

        private void RefreshGrid()
            {
                _items.Clear();

                var data = _db.Set<VatTu>()
                              .Where(v => v.HieuLuc == true) 
                              .OrderBy(v => v.TenVatTu)
                              .Select(v => new InventoryRow
                              {
                                  VatTuId = v.VatTuId,
                                  Code = "VT" + v.VatTuId,
                                  Name = v.TenVatTu,
                                  Category = "Vật tư",           
                                  Unit = v.DonViTinh,
                                  OnHand = v.SoLuongTon,
                                  ReorderLevel = 0m,            
                                  IsActive = v.HieuLuc,
                                  Notes = v.GhiChu
                              })
                              .ToList();

                foreach (var r in data) _items.Add(r);
            }

            private void BtnImport_Click(object sender, RoutedEventArgs e)
            {
                var dlg = new ImportWindow(_db);
                if (dlg.ShowDialog() == true)
                {
                    try
                    {
                        var vt = _db.Set<VatTu>().First(v => v.VatTuId == dlg.SelectedVatTuId);

                        var pn = new PhieuNhap
                        {
                            NgayNhap = DateTime.Now,
                            NguoiDungId = null,  
                            GhiChu = dlg.Notes
                        };
                        _db.Set<PhieuNhap>().Add(pn);
                        _db.SaveChanges();

                        var ct = new PhieuNhapChiTiet
                        {
                            PhieuNhapId = pn.PhieuNhapId,
                            VatTuId = vt.VatTuId,
                            SoLuong = dlg.Quantity,
                            DonGia = dlg.UnitCost
                        };
                        _db.Set<PhieuNhapChiTiet>().Add(ct);

                    vt.SoLuongTon = vt.SoLuongTon + dlg.Quantity;
                    _db.SaveChanges();
                        RefreshGrid();
                        MessageBox.Show("Nhập kho thành công.", "OK",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Nhập kho thất bại.\n" + ExplainDbException(ex),
                            "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            public class InventoryRow : INotifyPropertyChanged
            {
                public int VatTuId { get; set; }
                private string _code; public string Code { get => _code; set { _code = value; OnChanged(nameof(Code)); } }
                private string _name; public string Name { get => _name; set { _name = value; OnChanged(nameof(Name)); } }
                private string _category; public string Category { get => _category; set { _category = value; OnChanged(nameof(Category)); } }
                private string _unit; public string Unit { get => _unit; set { _unit = value; OnChanged(nameof(Unit)); } }
                private decimal? _onHand; public decimal? OnHand { get => _onHand; set { _onHand = value; OnChanged(nameof(OnHand)); } }
                private decimal _reorderLevel; public decimal ReorderLevel { get => _reorderLevel; set { _reorderLevel = value; OnChanged(nameof(ReorderLevel)); } }
                private bool _isActive; public bool IsActive { get => _isActive; set { _isActive = value; OnChanged(nameof(IsActive)); } }
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
                        if (err.Number == 2627 || err.Number == 2601) return "Dữ liệu trùng UNIQUE/INDEX.";
                        if (err.Number == 547) return "Vi phạm ràng buộc (CHECK/FK). Kiểm tra số lượng hoặc liên kết.";
                    }
                    return $"SQL {sql.Number}: {sql.Message}";
                }
                return root.Message;
            }
        }

    internal class ImportWindow : Window
    {
        private readonly DatabaseEntities _db;
        private ComboBox _cbItem;
        private TextBox _tbQty,_tbNotes;

        public int SelectedVatTuId { get; private set; }
        public decimal Quantity { get; private set; }
        public decimal UnitCost { get; private set; }
        public string Notes => _tbNotes.Text;

        public ImportWindow(DatabaseEntities db)
        {
            _db = db;
            Title = "Nhập kho";
            Width = 420; Height = 260; WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize; ShowInTaskbar = false;

            var grid = new Grid { Margin = new Thickness(12) };
            for (int i = 0; i < 5; i++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            AddLabel(grid, "Vật tư:", 0, 0);
            _cbItem = new ComboBox { IsEditable = true, MinWidth = 220 };

            var src = _db.Set<DichVu>()
                         .Where(d => d.LaVatTuKho == true && d.HieuLuc == true)
                         .OrderBy(d => d.TenDichVu)
                         .ToList();

            _cbItem.ItemsSource = src;
            _cbItem.DisplayMemberPath = "TenDichVu";
            _cbItem.SelectedValuePath = "DichVuId";

            Grid.SetRow(_cbItem, 0); Grid.SetColumn(_cbItem, 1); grid.Children.Add(_cbItem);

            AddLabel(grid, "Số lượng:", 1, 0);
            _tbQty = new TextBox { Text = "" };
            Grid.SetRow(_tbQty, 1); Grid.SetColumn(_tbQty, 1); grid.Children.Add(_tbQty);

            AddLabel(grid, "Ghi chú:", 3, 0);
            _tbNotes = new TextBox();
            Grid.SetRow(_tbNotes, 3); Grid.SetColumn(_tbNotes, 1); grid.Children.Add(_tbNotes);

            var sp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            var ok = new Button { Content = "OK", Padding = new Thickness(12, 6, 12, 6), IsDefault = true };
            var cancel = new Button { Content = "Hủy", Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(8, 0, 0, 0), IsCancel = true };
            ok.Click += Ok_Click;
            sp.Children.Add(ok); sp.Children.Add(cancel);
            Grid.SetRow(sp, 4); Grid.SetColumnSpan(sp, 2); grid.Children.Add(sp);

            Content = grid;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (_cbItem.SelectedValue == null) { MessageBox.Show("Chọn vật tư (dịch vụ)."); return; }
            if (!decimal.TryParse(_tbQty.Text, out var qty) || qty <= 0) { MessageBox.Show("Số lượng phải > 0."); return; }

            int dvId = (int)_cbItem.SelectedValue;
            var dv = _db.Set<DichVu>().First(x => x.DichVuId == dvId);

            SelectedVatTuId = EnsureVatTuForDichVu(dv);

            Quantity = qty;
            DialogResult = true;
        }

        private int EnsureVatTuForDichVu(DichVu dv)
        {
            string name = (dv.TenDichVu ?? "").Trim();
            var vt = _db.Set<VatTu>().FirstOrDefault(x => x.TenVatTu.ToLower() == name.ToLower());
            if (vt == null)
            {
                vt = new VatTu
                {
                    TenVatTu = dv.TenDichVu,
                    DonViTinh = dv.DonViTinh,
                    SoLuongTon = 0m,
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
            return vt.VatTuId;
        }

        private static void AddLabel(Grid g, string text, int row, int col)
        {
            var lb = new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 8) };
            Grid.SetRow(lb, row); Grid.SetColumn(lb, col); g.Children.Add(lb);
        }
    }

}
