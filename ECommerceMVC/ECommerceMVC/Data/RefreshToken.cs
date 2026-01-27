namespace ECommerceMVC.Data
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string MaKh { get; set; } = null!;
        public string Token { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRevoked { get; set; }
        
        public virtual KhachHang? KhachHang { get; set; }
    }
}
