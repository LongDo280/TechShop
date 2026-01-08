using Microsoft.AspNetCore.Mvc;
using TechShop1.Data;

namespace TechShop1.Controllers
{
    public class HomeController : Controller
    {
        private readonly Hshop2023Context _db;

        public HomeController(Hshop2023Context context)
        {
            _db = context;
        }

        public IActionResult Index(string? query)
        {
            var hangHoas = _db.HangHoas.AsQueryable();

            if (!string.IsNullOrEmpty(query))
            {
                // Lọc sản phẩm theo từ khóa tìm kiếm
                hangHoas = hangHoas.Where(p => p.TenHh.Contains(query));
            }

            var result = hangHoas.ToList();
            return View(result);
        }
        public IActionResult Contact()
        {
            return View();
        }
        public IActionResult PrivacyPolicy() => View();
        public IActionResult TermsOfUse() => View();
        public IActionResult SalesRefunds() => View();
        [Route("sitemap.xml")]
        public IActionResult Sitemap()
        {
            var baseUrl = "https://techshop.com.vn"; // Thay bằng domain của bạn
            var items = new List<string>();

            // Link các trang tĩnh
            items.Add($"{baseUrl}/");
            items.Add($"{baseUrl}/HangHoa");
            items.Add($"{baseUrl}/Home/Contact");

            // Lấy link sản phẩm từ Database (Ví dụ dùng db của bạn)
            // var products = _context.HangHoas.ToList();
            // foreach(var p in products) {
            //    items.Add($"{baseUrl}/HangHoa/Detail/{p.MaHh}");
            // }

            var xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";
            xml += "<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">";
            foreach (var item in items)
            {
                xml += $"<url><loc>{item}</loc><lastmod>{DateTime.Now:yyyy-MM-dd}</lastmod><priority>0.8</priority></url>";
            }
            xml += "</urlset>";

            return Content(xml, "text/xml");
        }
    }
}
