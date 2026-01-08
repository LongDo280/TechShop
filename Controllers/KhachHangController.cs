using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TechShop1.Data;
using TechShop1.Helpers;
using TechShop1.ViewModels;
using TechShop1.Helpers;
namespace TechShop1.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly Hshop2023Context db;
        private readonly IMapper _mapper;
        private readonly TwilioVerifyService _twilioService;

        public KhachHangController(Hshop2023Context context, IMapper mapper, TwilioVerifyService twilioService)
        {
            db = context;
            _mapper = mapper;
            _twilioService = twilioService;
        }

        #region Đăng Ký
        [HttpGet]
        public IActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> DangKy(RegisterVM model, IFormFile? Hinh)
        {
            if (ModelState.IsValid)
            {
                // 1. Kiểm tra trùng tên đăng nhập
                if (db.KhachHangs.Any(kh => kh.MaKh == model.MaKh))
                {
                    ModelState.AddModelError("MaKh", "Tên đăng nhập này đã được sử dụng.");
                    return View(model);
                }

                try
                {
                    // Xử lý ảnh trước nếu có (để lưu tên file vào Session)
                    if (Hinh != null)
                    {
                        model.Hinh = MyUtil.UploadHinh(Hinh, "KhachHang");
                    }

                    // Lưu thông tin đăng ký vào Session (dùng Newtonsoft.Json)
                    var sessionData = Newtonsoft.Json.JsonConvert.SerializeObject(model);
                    HttpContext.Session.SetString("PendingUser", sessionData);

                    // Chuyển đổi số điện thoại 0898675884 -> +84898675884
                    string phone = model.DienThoai;
                    if (phone.StartsWith("0"))
                    {
                        phone = "+84" + phone.Substring(1);
                    }

                    // Gửi mã OTP qua Twilio
                    await _twilioService.SendVerificationCode(phone);

                    // Chuyển sang trang nhập mã OTP
                    return RedirectToAction("XacNhanOTP", new { sdt = phone });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                }
            }
            return View(model);
        }
        #endregion
        [HttpGet]
        public IActionResult XacNhanOTP(string sdt)
        {
            ViewBag.Sdt = sdt;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> XacNhanOTP(string sdt, string otp)
        {
            bool isCorrect = await _twilioService.CheckVerificationCode(sdt, otp);

            if (isCorrect)
            {
                // Lấy dữ liệu từ Session ra để lưu vào DB
                var sessionData = HttpContext.Session.GetString("PendingUser");
                if (sessionData != null)
                {
                    var model = Newtonsoft.Json.JsonConvert.DeserializeObject<RegisterVM>(sessionData);
                    var key = MyUtil.GenerateRandomKey();

                    var khachHang = new KhachHang
                    {
                        MaKh = model.MaKh,
                        HoTen = model.HoTen,
                        MatKhau = model.MatKhau.ToMd5Hash(key),
                        RandomKey = key,
                        Email = model.Email,
                        DienThoai = model.DienThoai,
                        DiaChi = model.DiaChi,
                        NgaySinh = model.NgaySinh ?? DateTime.Now,
                        GioiTinh = model.GioiTinh,
                        Hinh = model.Hinh, // Tên file đã lưu ở bước trên
                        HieuLuc = true
                    };

                    db.Add(khachHang);
                    db.SaveChanges();

                    HttpContext.Session.Remove("PendingUser"); // Xóa session sau khi dùng xong
                    TempData["Message"] = "Đăng ký thành công!";
                    return RedirectToAction("DangNhap");
                }
            }

            ModelState.AddModelError("", "Mã OTP không chính xác hoặc đã hết hạn.");
            ViewBag.Sdt = sdt;
            return View();
        }
        #region Đăng Nhập
        [AllowAnonymous]
        [HttpGet]
        public IActionResult DangNhap(string? ReturnUrl)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "HangHoa");
            }

            ViewBag.ReturnUrl = ReturnUrl;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> DangNhap(LoginVM model, string? ReturnUrl)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = ReturnUrl;
                return View(model);
            }

            /* 1️⃣ LOGIN ADMIN (NhanVien) */
            /* 1️⃣ LOGIN ADMIN (NhanVien) */
            // Chỉnh sửa để kiểm tra cả quyền hạn hoặc trạng thái hoạt động của nhân viên
            var admin = db.NhanViens.SingleOrDefault(nv => nv.MaNv == model.LoginName || nv.Email == model.LoginName);
            if (admin != null)
            {
                // Giả sử Admin không dùng RandomKey, băm trực tiếp
                var adminHash = model.Password.ToMd5Hash("");
                if (admin.MatKhau == adminHash)
                {
                    var adminClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, admin.MaNv),
            new Claim(ClaimTypes.Name, admin.HoTen),
            new Claim(ClaimTypes.Email, admin.Email),
            new Claim(ClaimTypes.Role, "Admin") // Role rất quan trọng cho [Authorize(Roles="Admin")]
        };

                    await CreateAuthenticationCookie(adminClaims);
                    return RedirectToAction("Index", "Admin");
                }
            }

            /* 2️⃣ LOGIN KHÁCH HÀNG */
            var kh = db.KhachHangs.SingleOrDefault(k => k.MaKh == model.LoginName || k.Email == model.LoginName);
            if (kh != null)
            {
                if (!kh.HieuLuc)
                {
                    ModelState.AddModelError("", "Tài khoản của bạn đang bị khóa.");
                    return View(model);
                }

                // Kiểm tra mật khẩu băm với RandomKey
                var khHash = model.Password.ToMd5Hash(kh.RandomKey);
                if (kh.MatKhau == khHash)
                {
                    var customerClaims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, kh.MaKh),
                        new Claim(ClaimTypes.Name, kh.HoTen),
                        new Claim(ClaimTypes.Email, kh.Email),
                        new Claim(ClaimTypes.Role, "Customer")
                    };

                    await CreateAuthenticationCookie(customerClaims);

                    if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                        return Redirect(ReturnUrl);

                    return RedirectToAction("Index", "HangHoa");
                }
            }

            ModelState.AddModelError("", "Thông tin đăng nhập không chính xác.");
            return View(model);
        }

        // Hàm phụ để giảm lặp code đăng nhập
        private async Task CreateAuthenticationCookie(List<Claim> claims)
        {
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var properties = new AuthenticationProperties { IsPersistent = true };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
        }
        #endregion

        [Authorize]
        public IActionResult Profile()
        {
            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Lấy thông tin khách hàng kèm danh sách hóa đơn và chi tiết hóa đơn
            var khachHang = db.KhachHangs
                .Include(kh => kh.HoaDons)
                    .ThenInclude(hd => hd.ChiTietHds)
                .SingleOrDefault(kh => kh.MaKh == customerId);

            if (khachHang == null) return NotFound();

            return View(khachHang);
        }
        [Authorize]
        public IActionResult OrderDetail(int id)
        {
            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var hoaDon = db.HoaDons
                .Include(h => h.MaTrangThaiNavigation)
                .Include(h => h.ChiTietHds)
                    .ThenInclude(ct => ct.MaHhNavigation)
                .SingleOrDefault(h => h.MaHd == id && h.MaKh == customerId);

            if (hoaDon == null)
            {
                return NotFound();
            }

            return View(hoaDon);
        }
        [Authorize]
        [HttpGet]
        public IActionResult EditProfile()
        {
            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var kh = db.KhachHangs.SingleOrDefault(k => k.MaKh == customerId);

            if (kh == null) return NotFound();

            var model = new EditProfileVM
            {
                HoTen = kh.HoTen,
                DienThoai = kh.DienThoai,
                Email = kh.Email,
                DiaChi = kh.DiaChi
            };
            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditProfile(EditProfileVM model)
        {
            if (ModelState.IsValid)
            {
                var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var kh = db.KhachHangs.SingleOrDefault(k => k.MaKh == customerId);

                if (kh != null)
                {
                    kh.HoTen = model.HoTen;
                    kh.DienThoai = model.DienThoai;
                    kh.Email = model.Email;
                    kh.DiaChi = model.DiaChi;

                    db.Update(kh);
                    await db.SaveChangesAsync();

                    TempData["Message"] = "Cập nhật hồ sơ thành công!";
                    return RedirectToAction("Profile");
                }
            }
            return View(model);
        }
        [Authorize]
        public async Task<IActionResult> DangXuat()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

    }
}