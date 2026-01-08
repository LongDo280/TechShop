using Microsoft.AspNetCore.Mvc;
using TechShop1.Data;
using TechShop1.ViewModels;
using TechShop1.Helpers;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace TechShop1.Controllers
{
    public class CartController : Controller
    {
        private readonly Hshop2023Context db;
        private readonly IConfiguration _config;
        private readonly TwilioVerifyService _twilioService;

        public CartController(Hshop2023Context context, IConfiguration config, TwilioVerifyService twilioService)
        {
            db = context;
            _config = config;
            _twilioService = twilioService;
        }
        public List<CartItem> Cart => HttpContext.Session.Get<List<CartItem>>(MySetting.CART_KEY) ?? new List<CartItem>();

        public IActionResult Index()
        {
            return View(Cart);
        }
        public IActionResult AddToCart(int id, int quantity = 1)
        {
            var gioHang = Cart;
            var item = gioHang.SingleOrDefault(p => p.MaHh == id);
            if (item == null)
            {
                var hangHoa = db.HangHoas.SingleOrDefault(p => p.MaHh == id);
                if(hangHoa == null)
                {
                    TempData["Message"] = $"Không tìm thấy hàng hóa có mã {id}";
                    return Redirect("/404");
                }
                item = new CartItem
                {
                    MaHh = hangHoa.MaHh,
                    TenHH = hangHoa.TenHh,
                    DonGia = hangHoa.DonGia ?? 0,
                    Hinh = hangHoa.Hinh ?? string.Empty,
                    SoLuong = quantity
                };
                gioHang.Add(item);
            }
            else
            {
                item.SoLuong += quantity;
            }
            HttpContext.Session.Set(MySetting.CART_KEY, gioHang);
            return RedirectToAction("Index");
        }


        public IActionResult RemoveCart(int id)
        {
            var gioHang = Cart;
            var item = gioHang.SingleOrDefault(p => p.MaHh == id);
            if (item != null)
            {
                gioHang.Remove(item);
                HttpContext.Session.Set(MySetting.CART_KEY, gioHang);
            }
            return RedirectToAction("Index");
        }
        [Authorize]
        [HttpGet]
        public IActionResult CheckOut()
        {
            if (Cart.Count == 0)
            {
                return RedirectToAction("Index");
            }

            return View(Cart);
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CheckOut(Checkout model)
        {
            if (Cart.Count == 0) return RedirectToAction("Index");

            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            KhachHang? khachhang = db.KhachHangs.SingleOrDefault(kh => kh.MaKh == customerId);

            if (!model.GiongKhachHang)
            {
                if (string.IsNullOrWhiteSpace(model.HoTen)) ModelState.AddModelError("HoTen", "Vui lòng nhập họ tên");
                if (string.IsNullOrWhiteSpace(model.DiaChi)) ModelState.AddModelError("DiaChi", "Vui lòng nhập địa chỉ");
                if (string.IsNullOrWhiteSpace(model.DienThoai)) ModelState.AddModelError("DienThoai", "Vui lòng nhập điện thoại");

                if (!ModelState.IsValid) return View(Cart);
            }

            // 1. Lưu thông tin Checkout tạm vào Session
            var checkoutData = Newtonsoft.Json.JsonConvert.SerializeObject(model);
            HttpContext.Session.SetString("PendingOrder", checkoutData);

            /// Xử lý xóa khoảng trắng nếu khách hàng lỡ tay nhập
            string rawPhone = (model.GiongKhachHang ? khachhang!.DienThoai : model.DienThoai!).Replace(" ", "");
            string phoneFormatted = rawPhone.StartsWith("0") ? "+84" + rawPhone.Substring(1) : rawPhone;

            try
            {
                // 3. Gửi mã OTP đơn hàng
                
                return RedirectToAction("ConfirmOrderOTP", new { sdt = phoneFormatted });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi gửi OTP: " + ex.Message);
                return View(Cart);
            }
        }
        [HttpGet]
        public IActionResult ConfirmOrderOTP(string sdt)
        {
            ViewBag.Sdt = sdt;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmOrderOTP(string sdt, string otp)
        {
            bool isCorrect = true;

            if (isCorrect)
            {
                var sessionData = HttpContext.Session.GetString("PendingOrder");
                if (sessionData != null && Cart.Count > 0)
                {
                    var model = Newtonsoft.Json.JsonConvert.DeserializeObject<Checkout>(sessionData);
                    var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var khachhang = db.KhachHangs.SingleOrDefault(kh => kh.MaKh == customerId);

                    var hoadon = new HoaDon
                    {
                        MaKh = customerId,
                        HoTen = model!.GiongKhachHang ? khachhang!.HoTen : model.HoTen!,
                        DiaChi = model.GiongKhachHang ? khachhang!.DiaChi : model.DiaChi!,
                        DienThoai = model.GiongKhachHang ? khachhang!.DienThoai : model.DienThoai!,
                        NgayDat = DateTime.Now,
                        CachThanhToan = "COD",
                        CachVanChuyen = "SPX",
                        MaTrangThai = 0,
                        GhiChu = model.GhiChu
                    };

                    using var transaction = db.Database.BeginTransaction();
                    try
                    {
                        db.HoaDons.Add(hoadon);
                        db.SaveChanges();

                        var cthds = Cart.Select(item => new ChiTietHd
                        {
                            MaHd = hoadon.MaHd,
                            MaHh = item.MaHh,
                            SoLuong = item.SoLuong,
                            DonGia = item.DonGia,
                            GiamGia = 0
                        }).ToList();

                        db.ChiTietHds.AddRange(cthds);
                        db.SaveChanges();
                        transaction.Commit();

                        HttpContext.Session.Remove(MySetting.CART_KEY);
                        HttpContext.Session.Remove("PendingOrder");

                        return RedirectToAction("Success");
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        ModelState.AddModelError("", "Lỗi hệ thống khi lưu đơn hàng.");
                    }
                }
            }

            ModelState.AddModelError("", "Mã OTP không chính xác.");
            ViewBag.Sdt = sdt;
            return View();
        }
        [Authorize]
        public IActionResult Success()
        {
            return View();
        }
        [Authorize]
        [HttpPost]
        public IActionResult PayWithVnpay(Checkout model)
        {
            if (Cart.Count == 0)
                return RedirectToAction("Index");

            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var khachhang = db.KhachHangs.SingleOrDefault(kh => kh.MaKh == customerId);
            if (khachhang == null)
                return RedirectToAction("CheckOut");

            // 1️⃣ TẠO HÓA ĐƠN (CHƯA THANH TOÁN)
            var hoadon = new HoaDon
            {
                MaKh = customerId,
                HoTen = model.GiongKhachHang ? khachhang.HoTen : model.HoTen!,
                DiaChi = model.GiongKhachHang ? khachhang.DiaChi : model.DiaChi!,
                DienThoai = model.GiongKhachHang ? khachhang.DienThoai : model.DienThoai!,
                NgayDat = DateTime.Now,
                CachThanhToan = "VNPAY",
                CachVanChuyen = "SPX",
                MaTrangThai = 0, // CHỜ THANH TOÁN
                GhiChu = model.GhiChu
            };

            db.HoaDons.Add(hoadon);
            db.SaveChanges();

            // 2️⃣ LƯU CHI TIẾT
            var cthds = Cart.Select(item => new ChiTietHd
            {
                MaHd = hoadon.MaHd,
                MaHh = item.MaHh,
                SoLuong = item.SoLuong,
                DonGia = item.DonGia,
                GiamGia = 0
            }).ToList();

            db.ChiTietHds.AddRange(cthds);
            db.SaveChanges();

            // 3️⃣ TẠO LINK VNPAY
            long amount = Cart.Sum(x => (long)x.ThanhTien);

            var vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", _config["Vnpay:vnp_TmnCode"]);
            vnpay.AddRequestData("vnp_Amount", (amount * 100).ToString());
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_TxnRef", hoadon.MaHd.ToString());
            vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang #{hoadon.MaHd}");
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_ReturnUrl", _config["Vnpay:vnp_ReturnUrl"]);
            vnpay.AddRequestData("vnp_IpAddr", HttpContext.Connection.RemoteIpAddress!.ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));

            string paymentUrl = vnpay.CreateRequestUrl(
                _config["Vnpay:vnp_Url"],
                _config["Vnpay:vnp_HashSecret"]
            );

            return Redirect(paymentUrl);
        }

        [Authorize]
        public IActionResult VnpayReturn()
        {
            var responseCode = Request.Query["vnp_ResponseCode"];
            var orderId = Request.Query["vnp_TxnRef"];

            if (responseCode == "00")
            {
                int maHd = int.Parse(orderId!);
                var hoadon = db.HoaDons.SingleOrDefault(h => h.MaHd == maHd);

                if (hoadon != null)
                {
                    hoadon.MaTrangThai = 1; // ĐÃ THANH TOÁN
                    db.SaveChanges();
                }

                HttpContext.Session.Remove(MySetting.CART_KEY);
                return RedirectToAction("Success");
            }

            TempData["Message"] = "Thanh toán VNPAY thất bại";
            return RedirectToAction("CheckOut");
        }
        [Authorize]
        [HttpPost]
        public IActionResult PayWithPaypal(Checkout model)
        {
            if (Cart.Count == 0) return RedirectToAction("Index");

            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var khachhang = db.KhachHangs.SingleOrDefault(kh => kh.MaKh == customerId);

            // 1. Tạo Hóa đơn (Trạng thái: Đã thanh toán)
            var hoadon = new HoaDon
            {
                MaKh = customerId,
                HoTen = model.GiongKhachHang ? khachhang!.HoTen : model.HoTen!,
                DiaChi = model.GiongKhachHang ? khachhang!.DiaChi : model.DiaChi!,
                DienThoai = model.GiongKhachHang ? khachhang!.DienThoai : model.DienThoai!,
                NgayDat = DateTime.Now,
                CachThanhToan = "PayPal", // Ghi rõ phương thức thanh toán
                CachVanChuyen = "SPX",
                MaTrangThai = 1, // 1 là ĐÃ THANH TOÁN
                GhiChu = model.GhiChu
            };

            using var transaction = db.Database.BeginTransaction();
            try
            {
                db.HoaDons.Add(hoadon);
                db.SaveChanges();

                // 2. Lưu Chi tiết Hóa đơn
                var cthds = Cart.Select(item => new ChiTietHd
                {
                    MaHd = hoadon.MaHd,
                    MaHh = item.MaHh,
                    SoLuong = item.SoLuong,
                    DonGia = item.DonGia,
                    GiamGia = 0
                }).ToList();

                db.ChiTietHds.AddRange(cthds);
                db.SaveChanges();
                transaction.Commit();

                // 3. Xóa giỏ hàng và chuyển hướng
                HttpContext.Session.Remove(MySetting.CART_KEY);
                return Json(new { success = true }); // Trả về JSON để JavaScript biết đã xong
            }
            catch (Exception)
            {
                transaction.Rollback();
                return BadRequest();
            }
        }
        // Thêm vào CartController.cs
        [HttpPost]
        public IActionResult UpdateQuantity(int id, int quantity)
        {
            var gioHang = Cart;
            var item = gioHang.SingleOrDefault(p => p.MaHh == id);

            if (item != null && quantity > 0)
            {
                item.SoLuong = quantity;
                HttpContext.Session.Set(MySetting.CART_KEY, gioHang);
            }

            // Trả về dữ liệu JSON để JavaScript cập nhật giao diện
            return Json(new
            {
                success = true,
                itemThanhTien = item?.ThanhTien.ToString("#,##0") + " đ",
                cartTotal = gioHang.Sum(p => p.ThanhTien).ToString("#,##0") + " đ"
            });
        }


    }
}
