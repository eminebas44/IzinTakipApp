using System;

namespace IzinTakip.API.Models.DTOs
{
    public class RegisterDto
    {
        // Güncelleme işlemlerinde personelin benzersiz kimliğini taşır
        public int? id { get; set; }

        // Aynı gün içinde max kaç personelin izinli olacağı bilgisi
        public int? MaxIzinliKota { get; set; }

        public string CompanyName { get; set; }

        public string AdminName { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }

        public string TelefonNo { get; set; }

        /// <summary>
        /// Kullanıcı Rolü:
        /// "0" = Personel
        /// "1" = Admin / Yönetici
        /// "2" = İnsan Kaynakları (İK)
        /// </summary>
        public string Role { get; set; } = "0";

        public DateTime? IseBaslamaTarihi { get; set; }

        public int? ManagerID { get; set; }
    }
}