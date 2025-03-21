using Final.Infrastructure;
using Final.Models;
using Final.Services.Vnpay;
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

        // Confirm order and process payment
        [HttpPost]
        public IActionResult ConfirmOrder(OrderModel model)
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
                    Price = i.Price
                }).ToList()
            };

            _context.Orders.Add(newOrder);
            _context.SaveChanges(); // Save the new order

            // If VNPAY payment method is selected
            var vnPayModel = new PaymentInformationModel
            {
                //OrderType = "other",
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

        public IActionResult PaymentCallback()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            return Json(response);
        }

        //xac nhan thanh toan vnpay
        public IActionResult ConfirmPayment(PaymentInformationModel model)
        {

            return View(model);
        }


        // Handle VNPAY response after payment
        //public IActionResult PaymentReturn()
        //{
        //    // Get the transaction reference from the query string
        //    if (!Request.Query.TryGetValue("vnp_TxnRef", out var txnRefValues))
        //    {
        //        return RedirectToAction("Index", "Home");
        //    }

        //    int orderId;
        //    if (!int.TryParse(txnRefValues.FirstOrDefault(), out orderId))
        //    {
        //        return RedirectToAction("Index", "Home");
        //    }

        //    var order = _context.Orders.FirstOrDefault(o => o.Id == orderId);

        //    if (order == null)
        //        return RedirectToAction("Index", "Home");

        //    // Process the payment result from VNPAY
        //    var response = _vnPayService.PaymentExecute(Request.Query);

        //    // Check if the transaction was successful
        //    if (response.Success)
        //    {
        //        if (response.VnPayResponseCode == "00")
        //        {
        //            order.Status = "Đã thanh toán";
        //            _context.SaveChanges(); // Update order status
        //            TempData["Success"] = "Thanh toán thành công!";
        //        }
        //        else
        //        {
        //            order.Status = "Thanh toán thất bại";
        //            _context.SaveChanges();
        //            TempData["Error"] = "Thanh toán thất bại!";
        //        }
        //    }
        //    else
        //    {
        //        TempData["Error"] = "Dữ liệu không hợp lệ!";
        //    }

        //    return View(order); // Return to the payment response page
        //}

        // Show order success page after payment        
    }
}