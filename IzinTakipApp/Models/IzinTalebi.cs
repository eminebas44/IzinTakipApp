using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IzinTakipApp.Enums;

namespace IzinTakipApp.Models
{
    [Table("IzinTalepleri")]
    public class IzinTalebi
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public int KullaniciID { get; set; }

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

        public DateTime OlusturulmaTarihi { get; set; }

        [ForeignKey("KullaniciID")]
        public virtual Kullanici Kullanici { get; set; }
    }
}