using System.Linq;
using System.Windows;

namespace DO_AN_QLKS
{
    public partial class PhongDialog : Window
    {
        private readonly DatabaseEntities _db = new DatabaseEntities();

        public int? PhongId { get; }
        public Phong DialogResultPhong { get; private set; }

        private static readonly string[] TinhTrangList = { "Trong"};
        public PhongDialog(int? phongId = null)
        {
            InitializeComponent();
            PhongId = phongId;

            cboLoaiPhong.ItemsSource = _db.LoaiPhong.OrderBy(x => x.TenLoai).ToList();
            cboLoaiPhong.DisplayMemberPath = "TenLoai";
            cboLoaiPhong.SelectedValuePath = "LoaiPhongId";

            cboTinhTrang.ItemsSource = TinhTrangList;

            if (PhongId.HasValue)
            {
                Title = "Sửa phòng";
                var p = _db.Phong.FirstOrDefault(x => x.PhongId == PhongId.Value);
                if (p != null)
                {
                    txtSoPhong.Text = p.SoPhong;
                    cboLoaiPhong.SelectedValue = p.LoaiPhongId;
                    cboTinhTrang.SelectedValue = string.IsNullOrWhiteSpace(p.TinhTrang) ? "Trong" : p.TinhTrang;
                    txtGhiChu.Text = p.GhiChu;
                }
            }
            else
            {
                Title = "Thêm phòng";
                cboTinhTrang.SelectedIndex = 0;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string soPhong = (txtSoPhong.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(soPhong))
            {
                MessageBox.Show("Vui lòng nhập Số phòng."); return;
            }
            if (cboLoaiPhong.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn Loại phòng."); return;
            }
            if (cboTinhTrang.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn Tình trạng."); return;
            }

            int loaiPhongId = (int)cboLoaiPhong.SelectedValue;
            string tinhTrang = (string)cboTinhTrang.SelectedValue;   // phải là 1 trong TinhTrangList
            string ghiChu = txtGhiChu.Text?.Trim();

            bool soPhongTrung = _db.Phong.Any(x => x.SoPhong == soPhong && x.PhongId != (PhongId ?? 0));
            if (soPhongTrung)
            {
                MessageBox.Show("Số phòng đã tồn tại."); return;
            }

            if (PhongId.HasValue)
            {
                var p = _db.Phong.First(x => x.PhongId == PhongId.Value);
                p.SoPhong = soPhong;
                p.LoaiPhongId = loaiPhongId;
                p.TinhTrang = tinhTrang;          
                p.GhiChu = string.IsNullOrWhiteSpace(ghiChu) ? null : ghiChu;
                _db.SaveChanges();
                DialogResultPhong = p;
            }
            else
            {
                var p = new Phong
                {
                    SoPhong = soPhong,
                    LoaiPhongId = loaiPhongId,
                    TinhTrang = tinhTrang,       
                    GhiChu = string.IsNullOrWhiteSpace(ghiChu) ? null : ghiChu
                };
                _db.Phong.Add(p);
                _db.SaveChanges();
                DialogResultPhong = p;
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
