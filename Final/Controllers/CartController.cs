using Final.Models;
using Microsoft.AspNetCore.Mvc;
using Final.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Final.Controllers
{
    [Authorize(Roles = "User")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IActionResult Index()
        {
            var cartItems = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            var cart = new Cart();
            foreach (var item in cartItems)
            {
                cart.AddItem(item.Product, item.Quantity);
            }

            return View(cart);
        }

        [HttpPost]
        public IActionResult AddToCart(int productId)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null) return NotFound();

            var cartItems = this.GetCartItems();

            var existingItem = cartItems.FirstOrDefault(p => p.Product.Id == productId);

            if (existingItem != null)
            {
                // Nếu sản phẩm đã có trong giỏ, tăng số lượng lên 1
                existingItem.Quantity++;
            }
            else
            {
                // Thêm sản phẩm mới vào giỏ hàng và đảm bảo ProductId được gán đúng
                cartItems.Add(new CartItem
                {
                    Product = product,
                    ProductId = product.Id,  // Đảm bảo gán ProductId đúng
                    Quantity = 1
                });
            }

            // Lưu giỏ hàng vào session
            SaveCart(cartItems);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult IncreaseQuantity(int productId)
        {
            var cartItems = GetCartItems();
            var item = cartItems.FirstOrDefault(p => p.Product.Id == productId);

            if (item != null) item.Quantity++;

            SaveCart(cartItems);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult DecreaseQuantity(int productId)
        {
            var cartItems = GetCartItems();
            var item = cartItems.FirstOrDefault(p => p.Product.Id == productId);

            if (item != null)
            {
                if (item.Quantity > 1)
                {
                    item.Quantity--;
                }
                else
                {
                    cartItems.Remove(item);
                }
            }

            SaveCart(cartItems);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult RemoveItem(int productId)
        {
            var cartItems = GetCartItems();
            var itemToRemove = cartItems.FirstOrDefault(p => p.Product.Id == productId);

            if (itemToRemove != null)
            {
                cartItems.Remove(itemToRemove);
                SaveCart(cartItems);
            }

            return RedirectToAction("Index");
        }

        public IActionResult Checkout()
        {
            var cartItems = GetCartItems();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn trống!";
                return RedirectToAction("Index");
            }

            ModelState.Clear(); // Xóa lỗi ModelState cũ

            var orderModel = new OrderModel
            {
                CartItems = cartItems,  // Đảm bảo CartItems được gán đúng
                City = "",  // Để tránh lỗi NULL City
                Province = "",  // Đảm bảo không có lỗi NULL Province
            };

            // Tính toán TotalPrice cho giỏ hàng
            var totalPrice = cartItems.Sum(item => item.Price * item.Quantity);
            Console.WriteLine("Tổng giá trị giỏ hàng: " + totalPrice);

            // Truyền giá trị totalPrice vào view thông qua ViewData
            ViewData["TotalPrice"] = totalPrice;

            return View(orderModel);
        }
        private void SaveCart(List<CartItem> cartItems)
        {
            // Lưu giỏ hàng vào session và kiểm tra dữ liệu trong session
            HttpContext.Session.SetJson("Cart", cartItems);
            Console.WriteLine("Giỏ hàng sau khi lưu vào session: " + JsonConvert.SerializeObject(cartItems));
        }

        private List<CartItem> GetCartItems()
        {
            return HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();  // Lấy giỏ hàng từ session
        }
    }
}