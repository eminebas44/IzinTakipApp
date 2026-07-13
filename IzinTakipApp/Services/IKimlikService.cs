using IzinTakipApp.DTOs;
using IzinTakipApp.Models;

namespace IzinTakipApp.Services
{
    public interface IKimlikService
    {
        bool KayitOl(KullaniciKayitDto kayitDto);
        Kullanici GirişYap(KullaniciGirisDto girisDto);
    }
}