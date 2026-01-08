using System.Text;
namespace TechShop1.Helpers
{
    public class MyUtil // Đổi tên thành MyUtil để tránh trùng với các thư viện hệ thống
    {
        public static string UploadHinh(IFormFile Hinh, string folder)
        {
            try
            {
                // Tạo tên file duy nhất bằng cách thêm chuỗi ngẫu nhiên (hoặc dùng Guid)
                var fileName = Guid.NewGuid().ToString() + "_" + Hinh.FileName;
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Hinh", folder, fileName);

                using (var myfile = new FileStream(fullPath, FileMode.Create)) // Dùng Create để ghi đè hoặc tạo mới an toàn
                {
                    Hinh.CopyTo(myfile);
                }
                return fileName;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static string GenerateRandomKey(int length = 5)
        {
            var pattern = @"qawesdkjfnkdvksASKDJSDKJNQOIEFDLKMBKL!@#$%^&";
            var sb = new StringBuilder();
            var rd = new Random();
            for (int i = 0; i < length; i++)
            {
                sb.Append(pattern[rd.Next(0, pattern.Length)]);
            }
            return sb.ToString();
        }
    }
}