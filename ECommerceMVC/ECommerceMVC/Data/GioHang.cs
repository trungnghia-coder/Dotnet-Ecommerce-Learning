namespace ECommerceMVC.Data
{
    public class GioHang
    {
        public int Id { get; set; }
        public string? MaKh { get; set; }  // null n?u ch?a login
        public string SessionId { get; set; } = null!;  // Session ID cho anonymous users
        public int MaHh { get; set; }
        public int SoLuong { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime NgayCapNhat { get; set; }
        
        public virtual KhachHang? KhachHang { get; set; }
        public virtual HangHoa? HangHoa { get; set; }
    }
}
