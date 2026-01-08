using Microsoft.AspNetCore.Mvc;
using TechShop1.Data;
using TechShop1.ViewModels;

namespace TechShop1.ViewComponents
{
    // Tên lớp PHẢI kết thúc bằng cụm từ "ViewComponent" 
    // để hệ thống tự hiểu tên gọi là "FilterMenu"
    public class FilterMenuViewComponent : ViewComponent
    {
        private readonly Hshop2023Context db;
        public FilterMenuViewComponent(Hshop2023Context context) => db = context;

        public IViewComponentResult Invoke()
        {
            // Lấy dữ liệu động từ SQL Server
            var model = new FilterDataVM
            {
                NhaCungCaps = db.NhaCungCaps.Select(ncc => new NhaCungCapVM
                {
                    MaNCC = ncc.MaNcc,
                    TenCongTy = ncc.TenCongTy
                }).ToList(),

                Loais = db.Loais.Select(l => new LoaiVM
                {
                    MaLoai = l.MaLoai,
                    TenLoai = l.TenLoai
                }).ToList()
            };

            return View(model); // Sẽ tìm đến file Default.cshtml bạn đã tạo
        }
    }
}