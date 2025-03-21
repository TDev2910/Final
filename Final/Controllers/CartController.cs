using Final.Models;
using Microsoft.AspNetCore.Mvc;
using Final.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Final.Services.Vnpay;

namespace Final.Controllers
{
    [Authorize(Roles = "User")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartController> _logger;

        public CartController(ApplicationDbContext context, ILogger<CartController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IActionResult Index()
        {
            var cartItems = GetCartItems();

            var cart = new Cart();
            foreach (var item in cartItems)
            {
                cart.AddItem(item.Product, item.Quantity);
            }

            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var cartItems = GetCartItems();
            var existingItem = cartItems.FirstOrDefault(p => p.Product.Id == productId);

            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                cartItems.Add(new CartItem
                {
                    Product = product,
                    ProductId = product.Id,
                    Quantity = 1,
                    Price = product.Price
                });
            }

            SaveCart(cartItems);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult IncreaseQuantity(int productId)
        {
            UpdateCartItemQuantity(productId, 1);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult DecreaseQuantity(int productId)
        {
            UpdateCartItemQuantity(productId, -1);
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
                TempData["Error"] = "Your cart is empty!";
                return RedirectToAction("Index");
            }

            ModelState.Clear();

            var orderModel = new OrderModel
            {
                CartItems = cartItems,
                City = "",
                Province = "",
            };

            var totalPrice = cartItems.Sum(item => item.Price * item.Quantity);
            ViewData["TotalPrice"] = totalPrice;

            return View(orderModel);
        }

        private void SaveCart(List<CartItem> cartItems)
        {
            HttpContext.Session.SetJson("Cart", cartItems);
            _logger.LogInformation("Cart saved to session: {CartItems}", JsonConvert.SerializeObject(cartItems));
        }

        private List<CartItem> GetCartItems()
        {
            return HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();
        }

        private void UpdateCartItemQuantity(int productId, int quantityChange)
        {
            var cartItems = GetCartItems();
            var item = cartItems.FirstOrDefault(p => p.Product.Id == productId);

            if (item != null)
            {
                item.Quantity += quantityChange;
                if (item.Quantity <= 0)
                {
                    cartItems.Remove(item);
                }
                SaveCart(cartItems);
            }
        }

        ////Mới thêm
        //public IActionResult PaymentCallback()
        //{
        //    // Add detailed logging
        //    _logger.LogInformation("Payment callback received: {QueryString}",
        //        JsonConvert.SerializeObject(Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString())));

        //    if (!Request.Query.TryGetValue("vnp_TxnRef", out var txnRefValues))
        //    {
        //        _logger.LogWarning("Payment callback missing transaction reference");
        //        TempData["Error"] = "Không tìm thấy thông tin đơn hàng";
        //        return RedirectToAction("Index", "Home");
        //    }

        //    int orderId;
        //    if (!int.TryParse(txnRefValues.FirstOrDefault(), out orderId))
        //    {
        //        _logger.LogWarning("Invalid order ID format: {OrderId}", txnRefValues.FirstOrDefault());
        //        TempData["Error"] = "Mã đơn hàng không hợp lệ";
        //        return RedirectToAction("Index", "Home");
        //    }

        //    var order = _context.Orders.FirstOrDefault(o => o.Id == orderId);
        //    if (order == null)
        //    {
        //        _logger.LogWarning("Order not found: {OrderId}", orderId);
        //        TempData["Error"] = "Không tìm thấy đơn hàng";
        //        return RedirectToAction("Index", "Home");
        //    }

        //    var vnPayService = HttpContext.RequestServices.GetService<IVnPayService>();
        //    var response = vnPayService.PaymentExecute(Request.Query);

        //    _logger.LogInformation("Payment response for order {OrderId}: Success={Success}, Code={Code}",
        //        orderId, response.Success, response.VnPayResponseCode);

        //    if (response.Success)
        //    {
        //        if (response.VnPayResponseCode == "00")
        //        {
        //            order.Status = "Đã thanh toán";
        //            _context.SaveChanges();
        //            HttpContext.Session.Remove("Cart");
        //            TempData["Success"] = "Thanh toán thành công!";
        //            return RedirectToAction("OrderSuccess", "Order", new { id = orderId });
        //        }
        //        else
        //        {
        //            order.Status = "Thanh toán thất bại";
        //            _context.SaveChanges();
        //            TempData["Error"] = $"Thanh toán thất bại! Mã lỗi: {response.VnPayResponseCode}";
        //            return RedirectToAction("Index");
        //        }
        //    }
        //    else
        //    {
        //        _logger.LogWarning("Invalid payment data for order {OrderId}", orderId);
        //        TempData["Error"] = "Dữ liệu thanh toán không hợp lệ!";
        //        return RedirectToAction("Index");
        //    }
        //}
    }
}