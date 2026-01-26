namespace ECommerceMVC.ViewModels
{
    public class MerchandiseDetailVM
    {
        public int MerchandiseId { get; set; }
        public string Merchandisename { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public double Price { get; set; }
        public string Description { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? DetailDescription { get; set; }
        public int? Discount { get; set; }
        public DateTime? DateCreated { get; set; }
        public int ViewCount { get; set; }
    }
}
