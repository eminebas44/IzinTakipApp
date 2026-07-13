using System;
using System.ComponentModel.DataAnnotations;
using IzinTakipApp.Enums;

namespace IzinTakipApp.DTOs
{
    public class IzinTalebiDto
    {
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

        [StringLength(500)]
        public string Aciklama { get; set; }
    }
}