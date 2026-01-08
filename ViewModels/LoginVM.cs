using System.ComponentModel.DataAnnotations;

namespace TechShop1.ViewModels
{
    public class LoginVM
    {
        [Display(Name ="Tên đăng nhập")]
        [Required(ErrorMessage ="*")]
        [MaxLength(20, ErrorMessage ="Tối đa 20 ký tự.")]
        public string LoginName { get; set; }
        [Display(Name = "Mật Khẩu")]
        [Required(ErrorMessage ="*")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
