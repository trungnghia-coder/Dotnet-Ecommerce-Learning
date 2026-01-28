namespace ECommerceMVC.ViewModels
{
    public class PayPalCaptureRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}
