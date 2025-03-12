using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Final.Models;
using System.Linq;
using Microsoft.AspNetCore.Authentication;

namespace Final.Controllers
{
    [Authorize(Roles = "Admin")] // Chỉ Admin mới truy cập được
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
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
                return RedirectToAction("Index"); // Chuyển hướng về trang danh sách sản phẩm
            }
            return View("~/Views/Admin/Product/Create.cshtml", product); // Nếu lỗi, trả về form Create
        }
        // **Hiển thị form sửa sản phẩm**
        public IActionResult Edit(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }
            return View("~/Views/Admin/Product/Edit.cshtml", product);
        }

        // **Xử lý cập nhật sản phẩm**
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

                existingProduct.Name = product.Name;
                existingProduct.Price = product.Price;
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

                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View("~/Views/Admin/Product/Edit.cshtml", product);
        }
        //Xem sản phẩm
        public IActionResult Details(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }
            return View("~/Views/Admin/Product/Details.cshtml", product);
        }
        //xóa sản phẩm
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
    }
}