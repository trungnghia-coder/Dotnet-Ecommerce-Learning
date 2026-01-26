namespace ECommerceMVC.ViewModels
{
    public class CartItemVM
    {
        public int MerchandiseId { get; set; }
        public string Merchandisename { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public double Price { get; set; }
        public int Quantity { get; set; }
        public double Total => Price * Quantity;
    }
}
