using System;
using IzinTakipApp.Enums;

namespace IzinTakip.API.Models.DTOs
{
    public class RequestLeaveDto
    {
        public int KullaniciId { get; set; }
        public IzinKategorisi Kategori { get; set; }
        public IzinSureTipi SureTipi { get; set; }
        public DateTime BaslangicTarihi { get; set; }
        public DateTime BitisTarihi { get; set; }
        public double ToplamGun { get; set; }
        public string Aciklama { get; set; }
    }
}