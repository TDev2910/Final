using Final.Infrastructure;
using Final.Models;
using Final.Services.Vnpay;
using Final.Vnpay;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Final.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IVnPayService _vnPayService;

        public OrderController(ApplicationDbContext context, IConfiguration configuration, IVnPayService vnPayService)
        {
            _context = context;
            _configuration = configuration;
            _vnPayService = vnPayService;
        }

        // Các phương thức hiện tại giữ nguyên...

        [HttpPost]
        public IActionResult ConfirmOrder(OrderModel model)
        {
            var cartItems = HttpContext.Session.GetJson<List<CartItem>>("Cart");
            if (cartItems == null || !cartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng trống hoặc có lỗi! Vui lòng kiểm tra lại.";
                return View("~/Views/Cart/Checkout.cshtml", model);
            }
            model.CartItems = cartItems;

            if (model.CartItems == null || !model.CartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng trống hoặc có lỗi! Vui lòng kiểm tra lại.";
                return View("~/Views/Cart/Checkout.cshtml", model);
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
                TotalPrice = model.CartItems.Sum(i => i.Price * i.Quantity),
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
                Console.WriteLine(ex.Message);
                return View("~/Views/Cart/Checkout.cshtml", model);
            }

            // Chuyển sang thanh toán VNPAY
            if (model.PaymentMethod == "VNPAY")
            {
                var vnPayModel = new PaymentInformationModel
                {
                    OrderType = "other",
                    Amount = newOrder.TotalPrice,
                    OrderDescription = $"Thanh toán đơn hàng {newOrder.Id}",
                    Name = $"{model.FirstName} {model.LastName}",
                    OrderId = newOrder.Id.ToString()
                };

                string paymentUrl = _vnPayService.CreatePaymentUrl(vnPayModel, HttpContext);
                return Redirect(paymentUrl);
            }

            return RedirectToAction("OrderSuccess", new { id = newOrder.Id });
        }

        public IActionResult PaymentReturn()
        {
            // Lấy orderId từ vnp_TxnRef
            if (!Request.Query.TryGetValue("vnp_TxnRef", out var txnRefValues))
            {
                return RedirectToAction("Index", "Home");
            }

            int orderId;
            if (!int.TryParse(txnRefValues.FirstOrDefault(), out orderId))
            {
                return RedirectToAction("Index", "Home");
            }

            var order = _context.Orders.FirstOrDefault(o => o.Id == orderId);

            if (order == null)
                return RedirectToAction("Index", "Home");

            // Xử lý kết quả thanh toán từ VNPAY
            var response = _vnPayService.PaymentExecute(Request.Query);

            if (response.Success)
            {
                if (response.VnPayResponseCode == "00")
                {
                    order.Status = "Đã thanh toán";
                    _context.SaveChanges();
                    TempData["Success"] = "Thanh toán thành công!";
                }
                else
                {
                    order.Status = "Thanh toán thất bại";
                    _context.SaveChanges();
                    TempData["Error"] = "Thanh toán thất bại!";
                }
            }
            else
            {
                TempData["Error"] = "Dữ liệu không hợp lệ!";
            }

            return View(order);
        }

        public IActionResult OrderSuccess(int id)
        {
            var order = _context.Orders.FirstOrDefault(o => o.Id == id);

            if (order == null)
                return RedirectToAction("Index", "Home");

            return View(order);
        }
    }
}