namespace Final.Models
{
    public class PaymentInformationModel
    {
        public string OrderType { get; set; }
        public decimal Amount { get; set; } // Amount in decimal for calculations
        public string OrderDescription { get; set; }
        public string Name { get; set; }
        public string OrderId { get; set; }
        public string ReturnUrl { get; set; }

        // Convert Amount to VND (multiply by 100 for payment)
        public long GetAmountInVND()
        {
            return (long)(Amount * 100); // VNPAY expects the amount in "cents"
        }
    }
}
//namespace Final.Vnpay
//{
//    public class PaymentInformationModel
//    {
//        public string OrderType { get; set; }
//        public double Amount { get; set; }
//        public string OrderDescription { get; set; }
//        public string Name { get; set; }
//    }
//}
