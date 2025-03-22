using Final.Infrastructure;
using Final.Models;
using Final.Services;
using Final.Services.Vnpay;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Final.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IVnPayService _vnPayService;
        private readonly EmailService _emailService;

        public OrderController(ApplicationDbContext context, IConfiguration configuration, IVnPayService vnPayService, EmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _vnPayService = vnPayService;
            _emailService = emailService;
        }

        // Confirm order and process payment
        [HttpPost]
        public async Task<IActionResult> ConfirmOrder(OrderModel model)
        {
            // Ensure cart items exist
            var cartItems = HttpContext.Session.GetJson<List<CartItem>>("Cart");
            if (cartItems == null || !cartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng trống hoặc có lỗi! Vui lòng kiểm tra lại.";
                return View("~/Views/Cart/Checkout.cshtml", model);
            }

            model.CartItems = cartItems;

            // Create new order in database
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
                TotalPrice = model.CartItems.Sum(i => i.Price * i.Quantity),
                OrderDate = DateTime.Now,
                Status = "Đang xử lý",
                OrderItems = model.CartItems.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.Price,
                    Product = _context.Products.FirstOrDefault(p => p.Id == i.ProductId) // Ensure Product is included
                }).ToList()
            };

            _context.Orders.Add(newOrder);
            _context.SaveChanges(); // Save the new order

            // Gửi email xác nhận đơn hàng
            var emailBody = GenerateOrderConfirmationEmailBody(newOrder);
            await _emailService.SendEmailAsync(newOrder.Email, "Xác nhận đơn hàng", emailBody);

            // If VNPAY payment method is selected
            var vnPayModel = new PaymentInformationModel
            {
                Amount = (double)newOrder.TotalPrice,  // Total amount in decimal
                OrderDescription = $"Thanh toán đơn hàng {newOrder.Id}",
                Name = $"{model.FirstName} {model.LastName}",
            };
            if (model.PaymentMethod == "VNPAY")
            {
                return RedirectToAction("ConfirmPayment", vnPayModel);
            }
            else
            {
                return RedirectToAction("OrderSuccess", new { id = newOrder.Id });
            }
        }

        private string GenerateOrderConfirmationEmailBody(Order order)
        {
            var body = new StringBuilder();
            body.AppendLine("<h2>Thông tin đơn hàng</h2>");
            body.AppendLine($"<p><strong>Mã đơn hàng:</strong> {order.Id}</p>");
            body.AppendLine($"<p><strong>Họ:</strong> {order.FirstName}</p>");
            body.AppendLine($"<p><strong>Tên:</strong> {order.LastName}</p>");
            body.AppendLine($"<p><strong>Số điện thoại:</strong> {order.Phone}</p>");
            body.AppendLine($"<p><strong>Email:</strong> {order.Email}</p>");
            body.AppendLine($"<p><strong>Địa chỉ:</strong> {order.Address}</p>");
            body.AppendLine($"<p><strong>Phương thức vận chuyển:</strong> {order.ShippingMethod}</p>");
            body.AppendLine($"<p><strong>Phương thức thanh toán:</strong> {order.PaymentMethod}</p>");
            body.AppendLine($"<p><strong>Tổng giá trị:</strong> {order.TotalPrice}</p>");
            body.AppendLine($"<p><strong>Trạng thái:</strong> {order.Status}</p>");
            body.AppendLine("<h4>Sản phẩm trong đơn hàng</h4>");
            body.AppendLine("<ul>");
            foreach (var item in order.OrderItems)
            {
                body.AppendLine($"<li>{item.Product.Name} - Số lượng: {item.Quantity} - Giá: {item.Price}</li>");
            }
            body.AppendLine("</ul>");
            return body.ToString();
        }

        public IActionResult OrderSuccess(int id)
        {
            var order = _context.Orders.FirstOrDefault(o => o.Id == id);

            if (order == null)
                return RedirectToAction("Index", "Home");

            return View(order);
        }

        //thanh toán vnpay
        public IActionResult CreatePaymentUrl(PaymentInformationModel model)
        {
            var url = _vnPayService.CreatePaymentUrl(model, HttpContext);

            return Redirect(url);
        }

        [HttpGet]
        [Route("Order/PaymentCallback")]
        public IActionResult PaymentCallback()
        {
            return View();
        }

        public IActionResult ConfirmPayment(PaymentInformationModel model)
        {
            return View(model);
        }
    }
}