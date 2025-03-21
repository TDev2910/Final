namespace Final.Models
{
    public class PaymentResponseModel
    {
        public string OrderId { get; set; }
        public decimal Amount { get; set; }  // Change to decimal for consistency
        public string ResponseCode { get; set; }
        public bool Success { get; set; }
        public string VnPayResponseCode { get; set; }
    }
}

//namespace Final.Vnpay
//{
//    public class PaymentResponseModel
//    {
//        public string OrderDescription { get; set; }
//        public string TransactionId { get; set; }
//        public string OrderId { get; set; }
//        public string PaymentMethod { get; set; }
//        public string PaymentId { get; set; }
//        public bool Success { get; set; }
//        public string Token { get; set; }
//        public string VnPayResponseCode { get; set; }
//    }
//}
