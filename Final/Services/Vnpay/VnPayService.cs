using Final.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Newtonsoft.Json;

namespace Final.Services.Vnpay
{
    public class VnPayService : IVnPayService
    {
        private static string VnPayUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        private static string VnPayHashSecret = "MWZTXZ5M4M5IGR38KZZARJICM5B5RZ8L";  // Lấy từ thông tin bạn cung cấp
        private static string VnPayMerchantCode = "X5Y2YK6N";  // Lấy từ thông tin bạn cung cấp

        public string CreatePaymentUrl(PaymentInformationModel model, HttpContext context)
        {
            var vnpayData = new Dictionary<string, string>
            {
            { "vnp_Version", "2.1.0" },
            { "vnp_Command", "pay" },
            { "vnp_TmnCode", VnPayMerchantCode },
            { "vnp_Amount", (Convert.ToDecimal(model.Amount) * 100).ToString() },
            { "vnp_CurrCode", "VND" },
            { "vnp_TxnRef", model.OrderId },
            { "vnp_OrderInfo", Uri.EscapeDataString(model.OrderDescription) },  // Encoding the description to ensure no special chars
            { "vnp_Locale", "vn" },
            { "vnp_ReturnUrl", model.ReturnUrl },
            { "vnp_IpAddr", GetIpAddress(context) }
        };

            // Build query string and create secure hash
            string queryString = BuildQueryString(vnpayData);
            string vnpSecureHash = GetSecureHash(queryString);
            queryString += "&vnp_SecureHash=" + vnpSecureHash;

            return VnPayUrl + "?" + queryString;
        }

        private static string BuildQueryString(Dictionary<string, string> data)
        {
            var listQuery = data.Select(item => $"{item.Key}={item.Value}");
            return string.Join("&", listQuery);
        }

        private static string GetSecureHash(string queryString)
        {
            string hashData = queryString + "&vnp_HashSecret=" + VnPayHashSecret;
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashData));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        private static string GetIpAddress(HttpContext context)
        {
            // Lấy địa chỉ IP của người dùng
            return context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        }

        public PaymentResponseModel PaymentExecute(IQueryCollection collections)
        {
            var paymentResponse = new PaymentResponseModel();

            // Lấy mã giao dịch và các tham số từ phản hồi của VNPAY
            string vnpResponseCode = collections["vnp_ResponseCode"];
            string vnpTxnRef = collections["vnp_TxnRef"];

            paymentResponse.Success = (vnpResponseCode == "00");
            paymentResponse.VnPayResponseCode = vnpResponseCode;
            paymentResponse.OrderId = vnpTxnRef;

            return paymentResponse;
        }
    }
}