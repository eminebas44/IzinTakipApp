using System;

namespace IzinTakip.API.Models.DTOs
{
    public class RegisterDto
    {
        public string CompanyName { get; set; }
        public string AdminName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string TelefonNo { get; set; }
        public string Role { get; set; }
        public DateTime? IseBaslamaTarihi { get; set; }
        public int? ManagerID { get; set; }
    }
}