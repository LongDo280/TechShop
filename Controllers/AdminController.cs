using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TechShop1.Data;
using TechShop1.ViewModels;

namespace TechShop1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly Hshop2023Context db;

        public AdminController(Hshop2023Context context)
        {
            db = context;
        }

        public IActionResult Index()
        {
            var model = new AdminDashboardVM
            {
                TotalProducts = db.HangHoas.Count(),
                NewOrders = db.HoaDons.Count(hd => hd.MaTrangThai == 0),
                MonthlyRevenue = db.ChiTietHds
                    .Where(ct => ct.MaHdNavigation.MaTrangThai == 1 && ct.MaHdNavigation.NgayDat.Month == DateTime.Now.Month)
                    .Sum(ct => (double)ct.SoLuong * ct.DonGia),
                TotalCustomers = db.KhachHangs.Count()
            };

            return View(model);
        }

        public IActionResult HangHoa()
        {
            var data = db.HangHoas
                .Include(h => h.MaLoaiNavigation)
                .OrderByDescending(h => h.MaHh)
                .ToList();
            return View(data);
        }

        public IActionResult DonHang()
        {
            var data = db.HoaDons
                .Include(h => h.ChiTietHds)
                .OrderByDescending(h => h.NgayDat)
                .ToList();
            return View(data);
        }
        [HttpGet]
        public IActionResult CreateProduct()
        {
            // Lấy danh sách loại để đổ vào DropdownList trong View
            ViewBag.MaLoai = new SelectList(db.Loais, "MaLoai", "TenLoai");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct(HangHoaModel model, IFormFile Hinh)
        {
            if (ModelState.IsValid)
            {
                var hangHoa = new HangHoa
                {
                    TenHh = model.TenHh,
                    MaLoai = model.MaLoai,
                    DonGia = model.DonGia,
                    MoTaDonVi = model.MoTaDonVi,
                    MoTa = model.MoTa,
                    NgaySx = DateTime.Now
                };

                // Xử lý Upload hình ảnh
                if (Hinh != null)
                {
                    // Đặt tên file và đường dẫn lưu trữ
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(Hinh.FileName);
                    string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Hinh", "HangHoa", fileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await Hinh.CopyToAsync(stream);
                    }
                    hangHoa.Hinh = fileName; // Lưu tên file vào Database
                }

                db.Add(hangHoa);
                await db.SaveChangesAsync();
                return RedirectToAction("HangHoa");
            }

            ViewBag.MaLoai = new SelectList(db.Loais, "MaLoai", "TenLoai", model.MaLoai);
            return View(model);
        }
        public IActionResult OrderDetail(int id)
        {
            var hoaDon = db.HoaDons
                .Include(h => h.MaKhNavigation)        // Thông tin khách hàng
                .Include(h => h.MaTrangThaiNavigation) // Tên trạng thái (đã thanh toán/chờ xử lý)
                .Include(h => h.ChiTietHds)           // Danh sách món hàng
                    .ThenInclude(ct => ct.MaHhNavigation) // Thông tin từng món hàng (tên, hình)
                .SingleOrDefault(h => h.MaHd == id);

            if (hoaDon == null)
            {
                return NotFound();
            }

            return View(hoaDon);
        }
    }
}