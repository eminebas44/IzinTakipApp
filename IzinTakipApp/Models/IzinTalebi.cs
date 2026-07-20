using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IzinTakipApp.Enums;

namespace IzinTakipApp.Models
{
    [Table("IzinTalepleri")]
    public class IzinTalebi
    {
        // Geri alma (Undo) mekanizmasında nesne ilk üretildiğinde oluşturulma anını otomatik atayan yapıcı metot
        public IzinTalebi()
        {
            OlusturulmaTarihi = DateTime.Now;
        }

        [Key]
        public int ID { get; set; }

        [Required]
        public int KullaniciId { get; set; }

        [Required]
        public IzinKategorisi Kategori { get; set; }

        [Required]
        public IzinSureTipi SureTipi { get; set; }

        [Required]
        public DateTime BaslangicTarihi { get; set; }

        [Required]
        public DateTime BitisTarihi { get; set; }

        [Required]
        public double ToplamGun { get; set; }

        [Required]
        public IzinDurumu Durum { get; set; }

        [StringLength(500)]
        public string Aciklama { get; set; }

        [Required]
        public DateTime OlusturulmaTarihi { get; set; }

        [ForeignKey("KullaniciId")]
        public virtual Kullanici Kullanici { get; set; }
    }
}