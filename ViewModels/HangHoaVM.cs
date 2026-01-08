namespace TechShop1.ViewModels
{
    public class HangHoaVM
    {
        public int MaHh { get; set; } 
        public string TenHh { get; set; }
        public string Hinh  { get; set; }
        public double DonGia { get; set; }
        public string MoTaNgan { get; set; }
        public string TenLoai { get; set; }
    }
    public class ChiTietHangHoaVM
    {
        public int MaHh { get; set; }
        public string TenHh { get; set; }
        public string Hinh { get; set; }
        public double DonGia { get; set; }
        public string MoTaNgan { get; set; }
        public string TenLoai { get; set; }
        public string ChiTiet {  get; set; }
        public int DiemDanhGia { get; set; }
        public int SoLuongTon { get; set; }

    }
    public class HangHoaModel
    {
        public int MaHh { get; set; }
        public string TenHh { get; set; } = null!;
        public int MaLoai { get; set; }
        public double? DonGia { get; set; }
        public string? MoTaDonVi { get; set; } // Mô tả ngắn
        public string? MoTa { get; set; }      // Chi tiết
        public IFormFile? Hinh { get; set; }    // Để nhận file ảnh từ Form
    }
}
