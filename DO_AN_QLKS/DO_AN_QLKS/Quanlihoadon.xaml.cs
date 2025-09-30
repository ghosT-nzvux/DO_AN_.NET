using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DO_AN_QLKS
{
    public partial class Quanlihoadon : UserControl
    {
        private readonly DatabaseEntities _db = new DatabaseEntities();
        private readonly ObservableCollection<InvoiceRow> _items = new ObservableCollection<InvoiceRow>();

        public Quanlihoadon()
        {
            InitializeComponent();
            dgInvoices.ItemsSource = _items;

            this.Loaded += (s, e) => RefreshGrid();
        }

        private void RefreshGrid()
        {
            _items.Clear();

            var data =
                from hd in _db.HoaDon
                join lt in _db.LuuTru on hd.LuuTruId equals lt.LuuTruId
                join p in _db.Phong on lt.PhongId equals p.PhongId
                join kh in _db.KhachHang on lt.KhachHangId equals kh.KhachHangId
                orderby hd.NgayLap descending, hd.HoaDonId descending
                select new
                {
                    hd.HoaDonId,
                    hd.NgayLap,
                    hd.TongTien,
                    hd.HinhThucThanhToan,
                    hd.DaThanhToan,
                    CheckIn = (DateTime?)(lt.CheckInThucTe ?? lt.CheckInDuKien),
                    CheckOut = (DateTime?)(lt.CheckOutThucTe ?? lt.CheckOutDuKien),
                    RoomNumber = p.SoPhong,
                    CustomerName = kh.HoTen
                };

            var list = data.ToList();
            foreach (var x in list)
            {
                _items.Add(new InvoiceRow
                {
                    InvoiceCode = "HD" + x.HoaDonId,
                    CustomerName = x.CustomerName,
                    RoomNumber = x.RoomNumber,
                    CheckIn = x.CheckIn.HasValue ? x.CheckIn.Value.ToString("dd/MM/yyyy HH:mm") : "",
                    CheckOut = x.CheckOut.HasValue ? x.CheckOut.Value.ToString("dd/MM/yyyy HH:mm") : "",
                    Total = (x.TongTien ?? 0m).ToString("#,##0.##", CultureInfo.CurrentCulture),
                    PaymentMethod = string.IsNullOrWhiteSpace(x.HinhThucThanhToan) ? "—" : x.HinhThucThanhToan,
                    Status = x.DaThanhToan ? "Đã thanh toán" : "Chưa thanh toán",
                    CreatedAt = x.NgayLap.ToString("dd/MM/yyyy HH:mm")
                });
            }
        }
    }

    public class InvoiceRow : INotifyPropertyChanged
    {
        private string _invoiceCode;
        public string InvoiceCode { get { return _invoiceCode; } set { _invoiceCode = value; OnPropertyChanged("InvoiceCode"); } }

        private string _customerName;
        public string CustomerName { get { return _customerName; } set { _customerName = value; OnPropertyChanged("CustomerName"); } }

        private string _roomNumber;
        public string RoomNumber { get { return _roomNumber; } set { _roomNumber = value; OnPropertyChanged("RoomNumber"); } }

        private string _checkIn;
        public string CheckIn { get { return _checkIn; } set { _checkIn = value; OnPropertyChanged("CheckIn"); } }

        private string _checkOut;
        public string CheckOut { get { return _checkOut; } set { _checkOut = value; OnPropertyChanged("CheckOut"); } }

        private string _total;
        public string Total { get { return _total; } set { _total = value; OnPropertyChanged("Total"); } }

        private string _paymentMethod;
        public string PaymentMethod { get { return _paymentMethod; } set { _paymentMethod = value; OnPropertyChanged("PaymentMethod"); } }

        private string _status;
        public string Status { get { return _status; } set { _status = value; OnPropertyChanged("Status"); } }

        private string _createdAt;
        public string CreatedAt { get { return _createdAt; } set { _createdAt = value; OnPropertyChanged("CreatedAt"); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string n)
        {
            var h = PropertyChanged;
            if (h != null) h(this, new PropertyChangedEventArgs(n));
        }
    }
}
