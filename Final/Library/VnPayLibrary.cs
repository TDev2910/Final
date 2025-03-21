using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace Final.Library
{
    public class VnPayLibrary
    {
        private static string VnPayUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        private static string VnPayHashSecret = "MWZTXZ5M4M5IGR38KZZARJICM5B5RZ8L";
        private static string VnPayMerchantCode = "X5Y2YK6N";

        public static string CreatePaymentUrl(string orderId, string amount, string returnUrl)
        {
            var vnpayData = new Dictionary<string, string>
    {
        { "vnp_Version", "2.1.0" },
        { "vnp_Command", "pay" },
        { "vnp_TmnCode", VnPayMerchantCode }, // Your merchant code
        { "vnp_Amount", (Convert.ToDecimal(amount) * 100).ToString() }, // Multiply by 100 for VND
        { "vnp_CurrCode", "VND" }, // Currency is VND
        { "vnp_TxnRef", orderId }, // Unique transaction reference (Order ID)
        { "vnp_OrderInfo", "Thanh toán đơn hàng " + orderId }, // Order description
        { "vnp_Locale", "vn" }, // Locale for VNPAY (Vietnam)
        { "vnp_ReturnUrl", returnUrl }, // Return URL after payment
        { "vnp_IpAddr", GetIpAddress() } // IP address of the user
    };

            string queryString = BuildQueryString(vnpayData);
            string vnpSecureHash = GetSecureHash(queryString); // Generate checksum
            queryString += "&vnp_SecureHash=" + vnpSecureHash; // Add secure hash to the query string

            return VnPayUrl + "?" + queryString; // Return the complete URL for VNPAY
        }

        private static string BuildQueryString(Dictionary<string, string> data)
        {
            List<string> listQuery = new List<string>();
            foreach (var item in data)
            {
                listQuery.Add(item.Key + "=" + item.Value);
            }
            return string.Join("&", listQuery);
        }

        private static string GetSecureHash(string queryString)
        {
            string hashData = queryString + "&vnp_HashSecret=" + VnPayHashSecret;
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashData));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        private static string GetIpAddress()
        {
            string ipAddress = "127.0.0.1";
            return ipAddress;
        }
    }
}

//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.Linq;
//using System.Net;
//using System.Security.Cryptography;
//using System.Text;
//using Final.Vnpay;
//using Microsoft.AspNetCore.Http;

//namespace Final.Library
//{
//    public class VnPayLibrary
//    {
//        private readonly SortedList<string, string> _requestData = new SortedList<string, string>(new VnPayCompare());
//        private readonly SortedList<string, string> _responseData = new SortedList<string, string>(new VnPayCompare());

//        public void AddRequestData(string key, string value)
//        {
//            if (!string.IsNullOrEmpty(value))
//            {
//                _requestData[key] = value;
//            }
//        }

//        public void AddResponseData(string key, string value)
//        {
//            if (!string.IsNullOrEmpty(value))
//            {
//                _responseData[key] = value;
//            }
//        }

//        public string GetResponseData(string key)
//        {
//            return _responseData.TryGetValue(key, out var retValue) ? retValue : string.Empty;
//        }

//        public string CreateRequestUrl(string baseUrl, string vnpHashSecret)
//        {
//            var data = new StringBuilder();

//            foreach (var (key, value) in _requestData.Where(kv => !string.IsNullOrEmpty(kv.Value)))
//            {
//                data.Append(Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value) + "&");
//            }

//            var querystring = data.ToString().TrimEnd('&');
//            var vnpSecureHash = HmacSha512(vnpHashSecret, querystring);
//            baseUrl += "?" + querystring + "&vnp_SecureHash=" + vnpSecureHash;

//            return baseUrl;
//        }

//        private string HmacSha512(string key, string inputData)
//        {
//            var hash = new StringBuilder();
//            var keyBytes = Encoding.UTF8.GetBytes(key);
//            var inputBytes = Encoding.UTF8.GetBytes(inputData);

//            using (var hmac = new HMACSHA512(keyBytes))
//            {
//                var hashValue = hmac.ComputeHash(inputBytes);
//                foreach (var theByte in hashValue)
//                {
//                    hash.Append(theByte.ToString("x2"));
//                }
//            }

//            return hash.ToString();
//        }

//        public bool ValidateSignature(string inputHash, string secretKey)
//        {
//            var rspRaw = GetResponseData();
//            var myChecksum = HmacSha512(secretKey, rspRaw);
//            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
//        }

//        private string GetResponseData()
//        {
//            var data = new StringBuilder();
//            if (_responseData.ContainsKey("vnp_SecureHashType"))
//            {
//                _responseData.Remove("vnp_SecureHashType");
//            }

//            if (_responseData.ContainsKey("vnp_SecureHash"))
//            {
//                _responseData.Remove("vnp_SecureHash");
//            }

//            foreach (var (key, value) in _responseData.Where(kv => !string.IsNullOrEmpty(kv.Value)))
//            {
//                data.Append(WebUtility.UrlEncode(key) + "=" + WebUtility.UrlEncode(value) + "&");
//            }

//            if (data.Length > 0)
//            {
//                data.Remove(data.Length - 1, 1);
//            }

//            return data.ToString();
//        }

//        public PaymentResponseModel GetFullResponseData(IQueryCollection collection, string hashSecret)
//        {
//            var vnPay = new VnPayLibrary();
//            foreach (var (key, value) in collection)
//            {
//                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
//                {
//                    vnPay.AddResponseData(key, value);
//                }
//            }

//            var orderId = Convert.ToInt64(vnPay.GetResponseData("vnp_TxnRef"));
//            var vnPayTranId = Convert.ToInt64(vnPay.GetResponseData("vnp_TransactionNo"));
//            var vnpResponseCode = vnPay.GetResponseData("vnp_ResponseCode");
//            var vnpSecureHash = collection.FirstOrDefault(k => k.Key == "vnp_SecureHash").Value;
//            var orderInfo = vnPay.GetResponseData("vnp_OrderInfo");
//            var checkSignature = vnPay.ValidateSignature(vnpSecureHash, hashSecret);

//            if (!checkSignature)
//            {
//                return new PaymentResponseModel
//                {
//                    Success = false
//                };
//            }

//            return new PaymentResponseModel
//            {
//                Success = true,
//                PaymentMethod = "VnPay",
//                OrderDescription = orderInfo,
//                OrderId = orderId.ToString(),
//                PaymentId = vnPayTranId.ToString(),
//                TransactionId = vnPayTranId.ToString(),
//                Token = vnpSecureHash,
//                VnPayResponseCode = vnpResponseCode
//            };
//        }

//        // Thêm phương thức GetIpAddress
//        public string GetIpAddress(HttpContext context)
//        {
//            string ipAddress = context.Connection.RemoteIpAddress?.ToString();

//            if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1" || ipAddress == "127.0.0.1")
//            {
//                // Nếu không lấy được IP từ client (ví dụ chạy local), trả về IP mặc định
//                ipAddress = "127.0.0.1";
//            }

//            return ipAddress;
//        }
//    }

//    public class VnPayCompare : IComparer<string>
//    {
//        public int Compare(string x, string y)
//        {
//            if (x == y) return 0;
//            if (x == null) return -1;
//            if (y == null) return 1;
//            var vnpCompare = CompareInfo.GetCompareInfo("en-US");
//            return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
//        }
//    }
//}