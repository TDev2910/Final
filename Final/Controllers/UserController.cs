using Final.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Final.Controllers
{
    [Authorize(Roles = "User")] // Chỉ User truy cập được
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hiển thị danh sách sản phẩm trên trang Dashboard
        [Authorize(Roles = "User")]
        public IActionResult Dashboard()
        {
            return View();
        }

        // Hiển thị product - trang chủ với lọc và tìm kiếm
        public IActionResult Product(string searchTerm = null, string priceRange = null, string os = null)
        {
            // Lấy danh sách hệ điều hành từ database
            ViewBag.OSOptions = _context.Products.Select(p => p.OS).Distinct().ToList();
            ViewBag.SelectedOS = os;
            ViewBag.SelectedPriceRange = priceRange;
            ViewBag.SearchTerm = searchTerm;

            // Lấy danh sách sản phẩm
            var products = _context.Products.AsQueryable();

            // Lọc theo hệ điều hành nếu có
            if (!string.IsNullOrEmpty(os))
            {
                products = products.Where(p => p.OS.ToLower() == os.ToLower());
            }

            // Lọc theo khoảng giá
            if (!string.IsNullOrEmpty(priceRange))
            {
                switch (priceRange)
                {
                    case "10-15":
                        products = products.Where(p => p.Price >= 10000000 && p.Price <= 15000000);
                        break;
                    case "15-30":
                        products = products.Where(p => p.Price >= 15000000 && p.Price <= 30000000);
                        break;
                    case "30-50":
                        products = products.Where(p => p.Price >= 30000000 && p.Price <= 50000000);
                        break;
                }
            }

            // Tìm kiếm theo tên sản phẩm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                products = products.Where(p => p.Name.ToLower().Contains(searchTerm.ToLower()));
            }

            // Trả về danh sách sản phẩm sau khi lọc
            return View(products.ToList());
        }
    }
}