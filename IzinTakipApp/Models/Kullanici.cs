using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IzinTakipApp.Models
{
    [Table("Users")]
    public class Kullanici
    {
        // ROL SABİTLERİ (İş mantığında ve kontrollerde kullanmak için)
        public const string RolePersonel = "0";
        public const string RoleAdmin = "1";
        public const string RoleIK = "2";

        [Key]
        public int ID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; }

        /// <summary>
        /// Kullanıcı Rolü:
        /// "0" = Personel
        /// "1" = Şirket Yöneticisi (Admin)
        /// "2" = İnsan Kaynakları (HR)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Role { get; set; }

        /// <summary>
        /// Şirket / Kurum Adı
        /// </summary>
        [StringLength(150)]
        public string SirketAdi { get; set; }

        public DateTime IseGirisTarihi { get; set; }

        public double KalanYillikIzin { get; set; }

        public double MazeretIzinKotasi { get; set; }

        public double UcretsizIzinKotasi { get; set; }

        // Aynı gün izinli olabilecek maksimum personel kotası
        public int MaxIzinliKota { get; set; } = 3;

        // Şirket İzin Politikası & Duyurular Metni
        public string SirketPolitikasi { get; set; } = string.Empty;

        public bool IsFirstLogin { get; set; } = true;

        public int? ManagerID { get; set; }

        [ForeignKey("ManagerID")]
        public virtual Kullanici Manager { get; set; }
    }
}