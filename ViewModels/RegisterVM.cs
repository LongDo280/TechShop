using System.ComponentModel.DataAnnotations;

namespace TechShop1.ViewModels
{
    public class RegisterVM
    {
        [Display(Name = "Tên đăng nhập")]
        [Required(ErrorMessage ="*")]
        [MaxLength(20, ErrorMessage ="Tối đa 20 kí tự!")]
        public string MaKh { get; set; }
        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "*")]
        [DataType(DataType.Password)]
        public string? MatKhau { get; set; }
        [Display(Name = "Họ và Tên")]
        [Required(ErrorMessage = "*")]
        [MaxLength(50, ErrorMessage = "Tối đa 50 kí tự!")]
        public string HoTen { get; set; }
        [Display(Name = "Nam/Nữ")]
        public bool GioiTinh { get; set; } = true;
        [Display(Name ="Ngày Sinh")]
        [DataType(DataType.Date)]
        public DateTime? NgaySinh { get; set; }
        [Display(Name ="Địa chỉ")]
        [MaxLength(60, ErrorMessage = "Tối đa 60 kí tự!")]
        public string DiaChi { get; set; }
        [Display(Name = "Điện Thoại")]
        [MaxLength(24, ErrorMessage = "Tối đa 24 kí tự!")]
        [RegularExpression(@"0[9845]\d{8}", ErrorMessage ="Chưa đúng định dạng số di động.")]
        public string DienThoai { get; set; }
        [EmailAddress(ErrorMessage ="Chưa đúng định dạng email")]
        public string Email { get; set; }

        public string? Hinh { get; set; } = string.Empty;
    }
}
