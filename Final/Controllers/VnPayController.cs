using Final.Library;
using Final.Models;
using Microsoft.AspNetCore.Mvc;

namespace Final.Controllers
{
    public class VnPayController : Controller
    {
        // Hiển thị form thanh toán
        public IActionResult Checkout()
        {
            return View();
        }

        // Xử lý tạo URL thanh toán
        [HttpPost]
        public IActionResult CreatePayment(PaymentInformationModel model)
        {
            // Tạo mã đơn hàng duy nhất
            string orderId = Guid.NewGuid().ToString();
            string amount = model.Amount.ToString(); // Số tiền thanh toán
            string returnUrl = Url.Action("PaymentResponse", "VnPay", null, protocol: Request.Scheme); // URL nhận kết quả thanh toán

            string paymentUrl = VnPayLibrary.CreatePaymentUrl(orderId, amount, returnUrl);

            return Redirect(paymentUrl); // Chuyển hướng người dùng đến VNPAY
        }

        // Xử lý phản hồi từ VNPAY
        public IActionResult PaymentResponse(string vnp_TxnRef, string vnp_Amount, string vnp_ResponseCode)
        {
            // Xử lý kết quả phản hồi từ VNPAY (thành công hoặc thất bại)
            var paymentResponse = new PaymentResponseModel
            {
                OrderId = vnp_TxnRef,
                Amount = (int)decimal.Parse(vnp_Amount), // Convert string to decimal
                ResponseCode = vnp_ResponseCode
            };

            return View(paymentResponse); // Trả về kết quả thanh toán
        }
    }
}