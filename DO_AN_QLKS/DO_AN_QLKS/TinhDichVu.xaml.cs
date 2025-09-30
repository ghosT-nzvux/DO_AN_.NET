using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace DO_AN_QLKS
{
    public partial class TinhDichVu : Window
    {
        private readonly DatabaseEntities _db = new DatabaseEntities();

        private List<DichVu> _allServices = new List<DichVu>();
        public ICollectionView ServiceView { get; private set; }

        public ObservableCollection<BillLineVM> Items { get; private set; } = new ObservableCollection<BillLineVM>();

        private readonly int _luuTruId;

        public TinhDichVu(int luuTruId)
        {
            InitializeComponent();
            _luuTruId = luuTruId;
            this.DataContext = this;

            _allServices = _db.Set<DichVu>()
                              .Where(d => d.HieuLuc == true)
                              .OrderBy(d => d.TenDichVu)
                              .ToList();

            ServiceView = CollectionViewSource.GetDefaultView(_allServices);
            ServiceView.Filter = FilterService;

            LoadFromLuuTru();

            dgBillItems.ItemsSource = Items;

            btnAddRow.Click += (s, e) => AddRow();
            btnRemoveRow.Click += (s, e) => RemoveSelectedRow();
            btnOK.Click += BtnOK_Click;
            btnCancel.Click += (s, e) => Close();

            if (btnSearch != null) btnSearch.Click += (s, e) => ServiceView.Refresh();
            if (btnClearSearch != null) btnClearSearch.Click += (s, e) => { if (txtSearch != null) txtSearch.Text = ""; ServiceView.Refresh(); };
            if (txtSearch != null) txtSearch.TextChanged += (s, e) => ServiceView.Refresh();

            Items.CollectionChanged += (s, e) => RecalcTotal();
        }

        private bool FilterService(object obj)
        {
            if (obj == null) return false;
            var dv = (DichVu)obj;
            var key = (txtSearch?.Text ?? "").Trim();
            if (string.IsNullOrEmpty(key)) return true;
            return (dv.TenDichVu ?? "")
                .IndexOf(key, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        private void LoadFromLuuTru()
        {
            Items.Clear();

            var data =
                from s in _db.Set<SuDungDichVu>()
                join d in _db.Set<DichVu>() on s.DichVuId equals d.DichVuId
                where s.LuuTruId == _luuTruId && s.HoaDonId == null
                select new BillLineVM
                {
                    DichVuId = d.DichVuId,
                    Name = d.TenDichVu,
                    Unit = d.DonViTinh,
                    UnitPrice = s.DonGia,    
                    Quantity = s.SoLuong,
                    Notes = s.GhiChu
                };

            foreach (var it in data.ToList())
            {
                it.PropertyChanged += Line_PropertyChanged;
                Items.Add(it);
            }
            RecalcTotal();
        }

        private void AddRow()
        {
            var vm = new BillLineVM
            {
                Quantity = 1,
                UnitPrice = 0
            };
            vm.PropertyChanged += Line_PropertyChanged;
            Items.Add(vm);
            dgBillItems.SelectedItem = vm;
            dgBillItems.ScrollIntoView(vm);
        }

        private void RemoveSelectedRow()
        {
            var sel = dgBillItems.SelectedItem as BillLineVM;
            if (sel == null)
            {
                MessageBox.Show("Chọn một dòng để xóa.", "Thông báo",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            Items.Remove(sel);
            RecalcTotal();
        }

        private void Line_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var line = sender as BillLineVM;
            if (line == null) return;

            if (e.PropertyName == nameof(BillLineVM.DichVuId))
            {
                if (line.DichVuId.HasValue)
                {
                    var dv = _allServices.FirstOrDefault(x => x.DichVuId == line.DichVuId.Value);
                    if (dv != null)
                    {
                        line.Name = dv.TenDichVu;
                        line.Unit = dv.DonViTinh;
                        line.UnitPrice = dv.GiaBan; 
                    }
                }
                RecalcTotal();
            }
            else if (e.PropertyName == nameof(BillLineVM.Quantity)
                  || e.PropertyName == nameof(BillLineVM.UnitPrice))
            {
                RecalcTotal();
            }
        }

        private void RecalcTotal()
        {
            var total = Items.Sum(x => x.Amount);
            txtServicesTotal.Text = total.ToString("0.################");
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var db = new DatabaseEntities())
                using (var tran = db.Database.BeginTransaction())
                {
                    var oldRows = db.Set<SuDungDichVu>()
                                    .Where(x => x.LuuTruId == _luuTruId)
                                    .ToList();

                    var oldQtyByDv = oldRows
                        .GroupBy(x => x.DichVuId)    
                        .ToDictionary(g => g.Key, g => g.Sum(r => r.SoLuong));

                    var newQtyByDv = Items
                        .Where(x => x.DichVuId.HasValue)
                        .GroupBy(x => x.DichVuId.Value)
                        .ToDictionary(g => g.Key, g => g.Sum(xx => xx.Quantity));

                    var dvIds = oldQtyByDv.Keys
                                          .Concat(newQtyByDv.Keys)
                                          .Distinct()
                                          .ToList();

                    foreach (var dvId in dvIds)
                    {
                        decimal oldQty = oldQtyByDv.ContainsKey(dvId) ? oldQtyByDv[dvId] : 0m;
                        decimal newQty = newQtyByDv.ContainsKey(dvId) ? newQtyByDv[dvId] : 0m;
                        decimal delta = newQty - oldQty;

                        if (delta != 0)
                            AdjustStock(db, dvId, delta); 
                    }


                    var newDvSet = new HashSet<int>(newQtyByDv.Keys);
                    var toRemove = oldRows.Where(r => !newDvSet.Contains(r.DichVuId)).ToList();
                    foreach (var r in toRemove)
                        db.Set<SuDungDichVu>().Remove(r);

                    foreach (var g in Items.Where(x => x.DichVuId.HasValue)
                                           .GroupBy(x => x.DichVuId.Value))
                    {
                        int dvId = g.Key;
                        decimal total = g.Sum(x => x.Quantity);
                        decimal unit = g.Last().UnitPrice;
                        string notes = string.Join("; ", g.Where(x => !string.IsNullOrWhiteSpace(x.Notes))
                                                             .Select(x => x.Notes));

                        var any = oldRows.FirstOrDefault(x => x.DichVuId == dvId);
                        if (any == null)
                        {
                            db.Set<SuDungDichVu>().Add(new SuDungDichVu
                            {
                                LuuTruId = _luuTruId,
                                DichVuId = dvId,
                                SoLuong = total,
                                DonGia = unit,
                                GhiChu = notes,
                                TaoLuc = DateTime.Now
                            });
                        }
                        else
                        {
                            any.SoLuong = total;
                            any.DonGia = unit;
                            any.GhiChu = notes;
                        }
                    }

                    db.SaveChanges();
                    tran.Commit();

                    this.DialogResult = true; 
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lưu chi tiết dịch vụ thất bại.\n" + ex.Message,
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private int EnsureVatTuForDichVu(DatabaseEntities db, DichVu dv)
        {
            var name = (dv.TenDichVu ?? "").Trim();
            var vt = db.Set<VatTu>().FirstOrDefault(x => x.TenVatTu.ToLower() == name.ToLower());
            if (vt == null)
            {
                vt = new VatTu
                {
                    TenVatTu = dv.TenDichVu,
                    DonViTinh = dv.DonViTinh,
                    SoLuongTon = 0m,
                    HieuLuc = dv.HieuLuc,
                    GhiChu = "Tự tạo từ dịch vụ (LaVatTuKho=true)"
                };
                db.Set<VatTu>().Add(vt);
                db.SaveChanges();
            }
            else
            {
                vt.DonViTinh = dv.DonViTinh;
                vt.HieuLuc = dv.HieuLuc;
                db.SaveChanges();
            }
            return vt.VatTuId;
        }

        private void AdjustStock(DatabaseEntities db, int dichVuId, decimal delta)
        {
            if (delta == 0) return;

            var dv = db.Set<DichVu>().First(x => x.DichVuId == dichVuId);
            if (!dv.LaVatTuKho) return; 

            var vtId = EnsureVatTuForDichVu(db, dv);
            var vt = db.Set<VatTu>().First(x => x.VatTuId == vtId);

            if (delta > 0)
            {
                if (vt.SoLuongTon < delta)
                    throw new InvalidOperationException(
                        $"Tồn kho '{vt.TenVatTu}' không đủ. Hiện có: {vt.SoLuongTon}, cần: {delta}.");

                vt.SoLuongTon = vt.SoLuongTon - delta;
            }
            else
            {
                vt.SoLuongTon = vt.SoLuongTon + (-delta);
            }
        }
    }

    public class BillLineVM : INotifyPropertyChanged
    {
        private int? _dichVuId;
        public int? DichVuId
        {
            get => _dichVuId;
            set { if (_dichVuId != value) { _dichVuId = value; OnPropertyChanged(nameof(DichVuId)); } }
        }

        private string _name;
        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(nameof(Name)); } }
        }

        private string _unit;
        public string Unit
        {
            get => _unit;
            set { if (_unit != value) { _unit = value; OnPropertyChanged(nameof(Unit)); } }
        }

        private decimal _quantity;
        public decimal Quantity
        {
            get => _quantity;
            set { if (_quantity != value) { _quantity = value; OnPropertyChanged(nameof(Quantity)); OnPropertyChanged(nameof(Amount)); } }
        }

        private decimal _unitPrice;
        public decimal UnitPrice
        {
            get => _unitPrice;
            set { if (_unitPrice != value) { _unitPrice = value; OnPropertyChanged(nameof(UnitPrice)); OnPropertyChanged(nameof(Amount)); } }
        }

        public decimal Amount => Math.Round(Quantity * UnitPrice, 2);

        private string _notes;
        public string Notes
        {
            get => _notes;
            set { if (_notes != value) { _notes = value; OnPropertyChanged(nameof(Notes)); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
