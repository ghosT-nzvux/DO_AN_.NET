using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DO_AN_QLKS
{
    public partial class Traphongvathanhtoan : UserControl
    {
        private readonly DatabaseEntities _db = new DatabaseEntities();
        private readonly ObservableCollection<OccupiedRoomRow> _rooms = new ObservableCollection<OccupiedRoomRow>();

        private int? _currentLuuTruId;

        public Traphongvathanhtoan()
        {
            InitializeComponent();

            dgOccupiedRooms.ItemsSource = _rooms;

            this.Loaded += (s, e) => RefreshOccupiedRooms();

            dgOccupiedRooms.SelectionChanged += DgOccupiedRooms_SelectionChanged;
            txtCashGiven.TextChanged += (s, e) => RecalcChange();

            btnConfirmPay.Click += BtnConfirmPay_Click;
        }

        private void RefreshOccupiedRooms()
        {
            _rooms.Clear();

            var query =
                from lt in _db.LuuTru
                join p in _db.Phong on lt.PhongId equals p.PhongId
                join kh in _db.KhachHang on lt.KhachHangId equals kh.KhachHangId
                join lp in _db.LoaiPhong on p.LoaiPhongId equals lp.LoaiPhongId
                where lt.TrangThai == "DaDat"
                select new
                {
                    lt.LuuTruId,
                    p.SoPhong,
                    kh.HoTen,
                    lt.CheckInThucTe,
                    lt.CheckInDuKien,
                    lt.CheckOutDuKien,
                    GiaPhong = lp.GiaCoBan
                };

            var list = query.ToList();

            foreach (var x in list)
            {
                var checkIn = x.CheckInThucTe ?? x.CheckInDuKien;
                var checkOutPlan = x.CheckOutDuKien;

                var roomCharge = CalcRoomCharge(x.GiaPhong, checkIn, checkOutPlan);

                var serviceTotal = _db.SuDungDichVu
                    .Where(s => s.LuuTruId == x.LuuTruId)
                    .Select(s => (decimal?)(s.SoLuong * s.DonGia))
                    .DefaultIfEmpty(0)
                    .Sum() ?? 0;

                _rooms.Add(new OccupiedRoomRow
                {
                    LuuTruId = x.LuuTruId,
                    RoomNumber = x.SoPhong,
                    CustomerName = x.HoTen,
                    CheckIn = checkIn,
                    PlanCheckout = checkOutPlan,
                    EstimatedTotal = roomCharge + serviceTotal
                });
            }
        }

        private static decimal CalcRoomCharge(decimal giaCoBan, DateTime checkIn, DateTime planCheckout)
        {
            var days = (planCheckout - checkIn).TotalDays;
            var nights = Math.Max(1, (int)Math.Ceiling(days));
            return giaCoBan * nights;
        }

        private void DgOccupiedRooms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var row = dgOccupiedRooms.SelectedItem as OccupiedRoomRow;
            if (row == null)
            {
                _currentLuuTruId = null;
                ClearRightPanel();
                return;
            }

            _currentLuuTruId = row.LuuTruId;
            txtRoom.Text = row.RoomNumber;
            txtGuest.Text = row.CustomerName;
            txtCheckIn.Text = row.CheckIn.ToString("dd/MM/yyyy HH:mm");
            txtPlanCheckout.Text = row.PlanCheckout.ToString("dd/MM/yyyy HH:mm");

            txtAmountDue.Text = GetCurrentAmountDue(row.LuuTruId).ToString("#,##0.##");
            RecalcChange();
        }

        private void ClearRightPanel()
        {
            txtRoom.Text = "";
            txtGuest.Text = "";
            txtCheckIn.Text = "";
            txtPlanCheckout.Text = "";
            txtAmountDue.Text = "0";
            txtCashGiven.Text = "";
            txtChange.Text = "0";
        }

        private decimal GetCurrentAmountDue(int luuTruId)
        {
            var lt = _db.LuuTru.FirstOrDefault(x => x.LuuTruId == luuTruId);
            if (lt == null) return 0;

            var phong = _db.Phong.First(p => p.PhongId == lt.PhongId);
            var lp = _db.LoaiPhong.First(x => x.LoaiPhongId == phong.LoaiPhongId);

            var checkIn = lt.CheckInThucTe ?? lt.CheckInDuKien;
            var checkOut = lt.CheckOutThucTe ?? lt.CheckOutDuKien;

            var room = CalcRoomCharge(lp.GiaCoBan, checkIn, checkOut);

            var service = _db.SuDungDichVu
                .Where(s => s.LuuTruId == luuTruId)
                .Select(s => (decimal?)(s.SoLuong * s.DonGia))
                .DefaultIfEmpty(0)
                .Sum() ?? 0;

            return room + service; 
        }

        private void RecalcChange()
        {
            decimal amountDue, cashGiven;

            if (!decimal.TryParse(txtAmountDue.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out amountDue))
                amountDue = 0;

            if (!decimal.TryParse(txtCashGiven.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out cashGiven))
                cashGiven = 0;

            var change = cashGiven - amountDue;
            txtChange.Text = (change < 0 ? 0 : change).ToString("#,##0.##");
        }

        private void BtnConfirmPay_Click(object sender, RoutedEventArgs e)
        {
            if (_currentLuuTruId == null)
            {
                MessageBox.Show("Hãy chọn một phòng đang ở.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var luuTruId = _currentLuuTruId.Value;

            decimal amountDue;
            if (!decimal.TryParse(txtAmountDue.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out amountDue))
            {
                MessageBox.Show("Tổng cần trả không hợp lệ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal cash;
            if (!decimal.TryParse(txtCashGiven.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out cash))
            {
                MessageBox.Show("Số tiền khách đưa không hợp lệ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cash < amountDue)
            {
                if (MessageBox.Show("Khách đưa ít hơn tổng cần trả. Vẫn tiếp tục thanh toán?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;
            }

            try
            {
                var lt = _db.LuuTru.First(x => x.LuuTruId == luuTruId);
                var phong = _db.Phong.First(p => p.PhongId == lt.PhongId);
                var lp = _db.LoaiPhong.First(x => x.LoaiPhongId == phong.LoaiPhongId);

                var checkIn = lt.CheckInThucTe ?? lt.CheckInDuKien;
                var checkOut = DateTime.Now; // trả thực tế bây giờ
                var tienPhong = CalcRoomCharge(lp.GiaCoBan, checkIn, checkOut);

                var tienDv = _db.SuDungDichVu
                    .Where(s => s.LuuTruId == luuTruId)
                    .Select(s => (decimal?)(s.SoLuong * s.DonGia))
                    .DefaultIfEmpty(0)
                    .Sum() ?? 0;

                var giamGia = 0m;
                var thue = 0m;  

                var hd = new HoaDon
                {
                    LuuTruId = luuTruId,
                    NgayLap = DateTime.Now,
                    TienPhong = tienPhong,
                    TienDichVu = tienDv,
                    GiamGia = giamGia,
                    Thue = thue,
                    DaThanhToan = true,
                    HinhThucThanhToan = "Tiền mặt",
                    GhiChu = null
                };

                _db.HoaDon.Add(hd);

                lt.CheckOutThucTe = checkOut;
                lt.TrangThai = "DaTra";

                var Phong = _db.Phong.First(p => p.PhongId == lt.PhongId);
                phong.TinhTrang = "Trong";

                _db.SaveChanges();

                MessageBox.Show("Thanh toán & trả phòng thành công.", "Thành công",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                _currentLuuTruId = null;
                ClearRightPanel();
                RefreshOccupiedRooms();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Thanh toán thất bại.\n" + ex.Message, "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBillDetails_Click(object sender, RoutedEventArgs e)
        {
            var row = dgOccupiedRooms.SelectedItem as OccupiedRoomRow;
            if (row == null) { MessageBox.Show("Hãy chọn một phòng."); return; }

            var dlg = new TinhDichVu(row.LuuTruId) { Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) };
            if (dlg.ShowDialog() == true)
            {
                txtAmountDue.Text = GetCurrentAmountDue(row.LuuTruId).ToString("#,##0.##");
                row.EstimatedTotal = decimal.Parse(txtAmountDue.Text);
                dgOccupiedRooms.Items.Refresh();
                RecalcChange();
            }
        }

    }

    public class OccupiedRoomRow : INotifyPropertyChanged
    {
        public int LuuTruId { get; set; }
        public string RoomNumber { get; set; }
        public string CustomerName { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime PlanCheckout { get; set; }

        private decimal _estimatedTotal;
        public decimal EstimatedTotal
        {
            get { return _estimatedTotal; }
            set { if (_estimatedTotal != value) { _estimatedTotal = value; OnPropertyChanged("EstimatedTotal"); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
