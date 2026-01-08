using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechShop1.Data;
using TechShop1.ViewModels;

namespace TechShop1.Controllers
{
    public class HangHoaController : Controller
    {
        private readonly Hshop2023Context db;
        public HangHoaController(Hshop2023Context context)
        {
            db = context;
        }

        // 1. THÊM LẠI HÀM INDEX (Đây là trang chính khi vào /HangHoa)
        public IActionResult Index(int? loai)
        {
            var hangHoas = db.HangHoas.AsQueryable();

            if (loai.HasValue)
            {
                hangHoas = hangHoas.Where(p => p.MaLoai == loai.Value);
            }

            var result = hangHoas.Select(p => new HangHoaVM
            {
                MaHh = p.MaHh,
                TenHh = p.TenHh,
                DonGia = p.DonGia ?? 0,
                Hinh = p.Hinh ?? "",
                MoTaNgan = p.MoTaDonVi ?? "",
                TenLoai = p.MaLoaiNavigation.TenLoai
            }).ToList(); // BẮT BUỘC có .ToList() để thực thi truy vấn

            return View(result);
        }

        // 2. HÀM LỌC QUA AJAX (Giữ nguyên logic của bạn)
        public IActionResult FilterProduct(int[] loais, string[] hangs, string priceRange, string sortOrder)
        {
            var hangHoas = db.HangHoas.AsQueryable();

            if (loais != null && loais.Length > 0)
            {
                hangHoas = hangHoas.Where(p => loais.Contains(p.MaLoai));
            }

            if (hangs != null && hangs.Length > 0)
            {
                hangHoas = hangHoas.Where(p => hangs.Contains(p.MaNcc));
            }

            if (!string.IsNullOrEmpty(priceRange))
            {
                switch (priceRange)
                {
                    case "0-10": hangHoas = hangHoas.Where(h => h.DonGia < 10000000); break;
                    case "10-20": hangHoas = hangHoas.Where(h => h.DonGia >= 10000000 && h.DonGia <= 20000000); break;
                    case "20-30": hangHoas = hangHoas.Where(h => h.DonGia >= 20000000 && h.DonGia <= 30000000); break;
                    case "30-max": hangHoas = hangHoas.Where(h => h.DonGia > 30000000); break;
                }
            }

            hangHoas = sortOrder switch
            {
                "price_asc" => hangHoas.OrderBy(p => p.DonGia),
                "price_desc" => hangHoas.OrderByDescending(p => p.DonGia),
                "newest" => hangHoas.OrderByDescending(p => p.MaHh),
                _ => hangHoas.OrderBy(p => p.TenHh),
            };

            var result = hangHoas.Select(p => new HangHoaVM
            {
                MaHh = p.MaHh,
                TenHh = p.TenHh,
                DonGia = p.DonGia ?? 0,
                Hinh = p.Hinh ?? "",
                MoTaNgan = p.MoTaDonVi ?? "",
                TenLoai = p.MaLoaiNavigation.TenLoai
            }).ToList();

            return PartialView("_ProductListPartial", result);
        }

        // 3. HÀM TÌM KIẾM
        public IActionResult Search(string? query)
        {
            var hangHoas = db.HangHoas.AsQueryable();

            if (!string.IsNullOrEmpty(query))
            {
                hangHoas = hangHoas.Where(p => p.TenHh.Contains(query));
            }

            var result = hangHoas.Select(p => new HangHoaVM
            {
                MaHh = p.MaHh,
                TenHh = p.TenHh,
                DonGia = p.DonGia ?? 0,
                Hinh = p.Hinh ?? "",
                MoTaNgan = p.MoTaDonVi ?? "",
                TenLoai = p.MaLoaiNavigation.TenLoai
            });

            return View("Index", result);
        }

        // 4. HÀM CHI TIẾT
        [Route("san-pham/{tenHh}-{id}")]
        public IActionResult Detail(int id)
        {
            var data = db.HangHoas
                .Include(p => p.MaLoaiNavigation)
                .SingleOrDefault(p => p.MaHh == id);

            if (data == null)
            {
                TempData["Message"] = $"Không thấy có mã sản phẩm {id}";
                return Redirect("/404");
            }

            var result = new ChiTietHangHoaVM
            {
                MaHh = data.MaHh,
                TenHh = data.TenHh,
                DonGia = data.DonGia ?? 0,
                ChiTiet = data.MoTa ?? string.Empty,
                Hinh = data.Hinh ?? string.Empty,
                MoTaNgan = data.MoTaDonVi ?? string.Empty,
                TenLoai = data.MaLoaiNavigation.TenLoai,
                SoLuongTon = 10,
                DiemDanhGia = 5
            };
            return View(result);
        }
    }
}