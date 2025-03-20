using Azure.Core;
using Final.Services.Vnpay;
using Final.Vnpay;
using Microsoft.AspNetCore.Mvc;

public class PaymentController : Controller
{
    private readonly IVnPayService _vnPayService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IVnPayService vnPayService, ILogger<PaymentController> logger)
    {
        _vnPayService = vnPayService;
        _logger = logger;
    }

    // Tạo URL thanh toán VNPAY và chuyển hướng người dùng
    [HttpPost]
    public IActionResult CreatePaymentUrlVnpay(PaymentInformationModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid payment information model.");
            return BadRequest(ModelState);
        }

        try
        {
            // Gọi service để tạo URL thanh toán từ thông tin trong model
            var url = _vnPayService.CreatePaymentUrl(model, HttpContext);

            // Kiểm tra URL được tạo
            if (string.IsNullOrEmpty(url))
            {
                _logger.LogError("Failed to create VNPAY payment URL.");
                return StatusCode(500, "Failed to create payment URL");
            }

            // Chuyển hướng người dùng đến trang thanh toán VNPAY
            return Redirect(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment URL.");
            return StatusCode(500, "Internal server error");
        }
    }

    // Xử lý phản hồi từ VNPAY
    [HttpGet]
    public IActionResult PaymentCallbackVnpay()
    {
        try
        {
            // Xử lý phản hồi từ VNPAY
            var response = _vnPayService.PaymentExecute(Request.Query);

            // Trả lại kết quả giao dịch cho người dùng
            return Json(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment callback.");
            return StatusCode(500, "Internal server error");
        }
    }
}