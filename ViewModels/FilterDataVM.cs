namespace TechShop1.ViewModels
{
    public class FilterDataVM
    {
        public List<NhaCungCapVM> NhaCungCaps { get; set; }
        public List<LoaiVM> Loais { get; set; }
    }

    public class NhaCungCapVM
    {
        public string MaNCC { get; set; }
        public string TenCongTy { get; set; }
        public int SoLuong { get; set; }
    }

    public class LoaiVM
    {
        public int MaLoai { get; set; }
        public string TenLoai { get; set; }
        public int SoLuong { get; set; }
    }
}