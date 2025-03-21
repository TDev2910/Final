using Final.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Final.Controllers
{
    [Authorize(Roles = "Admin")] // Chỉ Admin mới truy cập được
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const string SecurityCode = "2910"; // Mã bảo mật mặc định

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Trang Dashboard Admin
        public IActionResult Dashboard()
        {
            return View();
        }

        // Trang quản lý sản phẩm
        public IActionResult Index()
        {
            var products = _context.Products.ToList();
            return View("~/Views/Admin/Product/Index.cshtml", products);
        }

        // Hiển thị form thêm sản phẩm
        public IActionResult Create()
        {
            return View("~/Views/Admin/Product/Create.cshtml"); // Đường dẫn đúng của view
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Products.Add(product);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View("~/Views/Admin/Product/Create.cshtml", product);
        }

        // Hiển thị form sửa sản phẩm
        public IActionResult Edit(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }
            return View("~/Views/Admin/Product/Edit.cshtml", product);
        }

        // Xử lý cập nhật sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Product product)
        {
            if (ModelState.IsValid)
            {
                var existingProduct = _context.Products.FirstOrDefault(p => p.Id == product.Id);
                if (existingProduct == null)
                {
                    return NotFound();
                }

                // Loại bỏ các ký tự không hợp lệ từ giá trị nhập vào (nếu có dấu phân cách hoặc ký tự không phải số)
                existingProduct.Price = Convert.ToDecimal(product.Price.ToString().Replace(",", "").Replace(".", ""));
                existingProduct.DiscountPrice = product.DiscountPrice.HasValue
                    ? Convert.ToDecimal(product.DiscountPrice.Value.ToString().Replace(",", "").Replace(".", ""))
                    : (decimal?)null;

                // Cập nhật các trường khác
                existingProduct.Name = product.Name;
                existingProduct.Stock = product.Stock;
                existingProduct.TotalStock = product.TotalStock;
                existingProduct.Image = product.Image;
                existingProduct.Description = product.Description;
                existingProduct.Screen = product.Screen;
                existingProduct.OS = product.OS;
                existingProduct.Camera = product.Camera;
                existingProduct.RAM = product.RAM;
                existingProduct.Storage = product.Storage;
                existingProduct.Warranty = product.Warranty;

                // Lưu thay đổi vào cơ sở dữ liệu
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            return View("~/Views/Admin/Product/Edit.cshtml", product);
        }

        // Xem sản phẩm
        public IActionResult Details(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }
            return View("~/Views/Admin/Product/Details.cshtml", product);
        }

        // Xóa sản phẩm
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }
            return View("~/Views/Admin/Product/Delete.cshtml", product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var product = _context.Products.Find(id);
            if (product != null)
            {
                _context.Products.Remove(product);  // Xóa sản phẩm
                _context.SaveChanges();  // Lưu thay đổi
                TempData["SuccessMessage"] = "Sản phẩm đã được xóa thành công!";  // Thông báo thành công
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm để xóa.";
            }
            return RedirectToAction("Index");  // Quay lại trang danh sách sản phẩm
        }

        // Hiển thị form nhập mã bảo mật
        public IActionResult EnterSecurityCode()
        {
            return View("~/Views/Admin/UserManagement/EnterSecurityCode.cshtml", new SecurityCodeViewModel());
        }

        // Xử lý mã bảo mật và hiển thị danh sách người dùng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidateSecurityCode(SecurityCodeViewModel model)
        {
            string enteredCode = $"{model.Code1}{model.Code2}{model.Code3}{model.Code4}";
            if (enteredCode != SecurityCode)
            {
                TempData["ErrorMessage"] = "Mã code không hợp lệ.";
                return RedirectToAction("EnterSecurityCode");
            }

            return RedirectToAction("UserList");
        }

        // Hiển thị danh sách người dùng
        [HttpGet]
        public async Task<IActionResult> UserList(string searchQuery)
        {
            var users = await _userManager.Users.ToListAsync();
            if (!string.IsNullOrEmpty(searchQuery))
            {
                users = users.Where(u => u.UserName.Contains(searchQuery)).ToList();
            }

            var userList = new List<UserViewModel>();

            foreach (var user in users)
            {
                userList.Add(new UserViewModel
                {
                    UserName = user.UserName,
                    Email = user.Email,
                    IsActive = IsUserActive(user.UserName)
                });
            }

            return View("~/Views/Admin/UserManagement/UserList.cshtml", userList);
        }

        private bool IsUserActive(string userName)
        {
            // Logic để kiểm tra trạng thái hoạt động của người dùng
            var user = _userManager.Users.FirstOrDefault(u => u.UserName == userName);
            if (user != null)
            {
                // Giả sử bạn có một thuộc tính LastActivityDate trong ApplicationUser
                // return (DateTime.Now - user.LastActivityDate).TotalMinutes < 5;
                return true; // Giả sử tất cả người dùng đều đang hoạt động
            }
            return false;
        }

        // Hiển thị danh sách đơn hàng
        public async Task<IActionResult> OrderList()
        {
            var orders = await _context.Orders.Include(o => o.OrderItems).ThenInclude(oi => oi.Product).ToListAsync();
            return View("~/Views/Admin/OrderServices/OrderList.cshtml", orders);
        }

        // Hiển thị chi tiết đơn hàng
        public async Task<IActionResult> OrderDetails(int orderId)
        {
            var order = await _context.Orders.Include(o => o.OrderItems).ThenInclude(oi => oi.Product).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
            {
                return NotFound();
            }
            return View("~/Views/Admin/OrderServices/OrderDetails.cshtml", order);
        }

        // Cập nhật trạng thái đơn hàng
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus([FromForm] UpdateOrderStatusModel model)
        {
            var order = await _context.Orders.FindAsync(model.OrderId);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = model.Status;
            await _context.SaveChangesAsync();

            return RedirectToAction("OrderList");
        }
    }

    public class UserViewModel
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
    }

    public class SecurityCodeViewModel
    {
        [Required]
        public string Code1 { get; set; }
        [Required]
        public string Code2 { get; set; }
        [Required]
        public string Code3 { get; set; }
        [Required]
        public string Code4 { get; set; }
    }

    public class UpdateOrderStatusModel
    {
        public int OrderId { get; set; }
        public string Status { get; set; }
    }
}