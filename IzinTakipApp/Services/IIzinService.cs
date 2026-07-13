using System.Collections.Generic;
using IzinTakipApp.DTOs;

namespace IzinTakipApp.Services
{
    public interface IIzinService
    {
        bool IzinTalepEt(IzinTalebiDto talepDto);

        List<IzinListeleDto> PersonelIzinleriniGetir(int kullaniciId);

        List<IzinListeleDto> YoneticiOnayListesiniGetir(int yoneticiId);

        bool IzinDurumunuGuncelle(IzinOnayDto onayDto);
    }
}