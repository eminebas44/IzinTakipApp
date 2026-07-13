using System;
using IzinTakipApp.Enums;

namespace IzinTakipApp.DTOs
{
    public class IzinListeleDto
    {
        public int ID { get; set; }
        public int KullaniciID { get; set; }
        public string PersonelAdi { get; set; }
        public IzinKategorisi Kategori { get; set; }
        public IzinSureTipi SureTipi { get; set; }
        public DateTime BaslangicTarihi { get; set; }
        public DateTime BitisTarihi { get; set; }
        public double ToplamGun { get; set; }
        public IzinDurumu Durum { get; set; } 
        public string Aciklama { get; set; }
        public DateTime OlusturulmaTarihi { get; set; }
    }
}