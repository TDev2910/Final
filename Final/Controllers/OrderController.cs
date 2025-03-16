using Final.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Linq;

namespace Final.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Checkout()
        {
            var model = new OrderModel();

            // Lấy giỏ hàng từ session
            var cart = HttpContext.Session.GetString("Cart");

            Console.WriteLine("Session Cart: " + cart); // In ra session giỏ hàng để kiểm tra

            // Deserialize giỏ hàng nếu có dữ liệu
            model.CartItems = string.IsNullOrEmpty(cart)
                ? new List<CartItem>()
                : JsonConvert.DeserializeObject<List<CartItem>>(cart);

            Console.WriteLine("Cart Items Count: " + model.CartItems.Count); // In ra số lượng sản phẩm trong giỏ hàng

            // Tính tổng giá trị đơn hàng
            var totalPrice = model.CartItems.Sum(i => i.Price * i.Quantity);
            // Truyền tổng giá trị đơn hàng vào ViewData
            ViewData["TotalPrice"] = totalPrice;

            return View(model);
        }

        [HttpPost]
        public IActionResult ConfirmOrder(OrderModel model)
        {
            var cart = HttpContext.Session.GetString("Cart");

            if (string.IsNullOrEmpty(cart))
            {
                TempData["Error"] = "Giỏ hàng trống hoặc có lỗi! Vui lòng kiểm tra lại.";
                return View("~/Views/Cart/Checkout.cshtml", model);
            }

            model.CartItems = JsonConvert.DeserializeObject<List<CartItem>>(cart);

            if (model.CartItems == null || !model.CartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng trống hoặc có lỗi! Vui lòng kiểm tra lại.";
                return View("~/Views/Cart/Checkout.cshtml", model);
            }

            // Kiểm tra các sản phẩm trong giỏ hàng
            var productIds = model.CartItems.Select(i => i.ProductId).ToList();
            var validProducts = _context.Products.Where(p => productIds.Contains(p.Id)).Select(p => p.Id).ToList();

            foreach (var item in model.CartItems)
            {
                if (!validProducts.Contains(item.ProductId))
                {
                    TempData["Error"] = $"Sản phẩm với ID {item.ProductId} không hợp lệ!";
                    return View("~/Views/Cart/Checkout.cshtml", model);
                }
            }

            var newOrder = new Order
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Phone = model.Phone,
                Email = model.Email,
                Address = model.Address,
                ShippingMethod = model.ShippingMethod,
                PaymentMethod = model.PaymentMethod,
                City = model.City,
                Store = model.Store,
                Notes = model.Notes,
                TotalPrice = model.CartItems.Sum(i => i.Price * i.Quantity), // Tính tổng giá trị đơn hàng
                OrderDate = DateTime.Now,
                Status = "Đang xử lý",
                OrderItems = model.CartItems.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList()
            };

            try
            {
                _context.Orders.Add(newOrder);
                _context.SaveChanges();  // Sau khi lưu, chúng ta có ID của đơn hàng
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Đã xảy ra lỗi khi xử lý đơn hàng. Vui lòng thử lại.";
                Console.WriteLine(ex.Message);  // In chi tiết lỗi ra Console để kiểm tra
                return View("~/Views/Cart/Checkout.cshtml", model);
            }

            // Sau khi tạo đơn hàng thành công, điều hướng sang trang OrderSuccess
            return RedirectToAction("OrderSuccess", new { id = newOrder.Id });
        }

        public IActionResult OrderSuccess(int id)
        {
            var order = _context.Orders
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
                return RedirectToAction("Index", "Home");

            return View(order);
        }
    }
}