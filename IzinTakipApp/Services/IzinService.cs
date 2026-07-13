using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using IzinTakipApp.Data;
using IzinTakipApp.DTOs;
using IzinTakipApp.Enums;
using IzinTakipApp.Models;

namespace IzinTakipApp.Services
{
    public class IzinService : IIzinService
    {
        private readonly AppDbContext _context;

        public IzinService()
        {
            _context = new AppDbContext();
        }

        public bool IzinTalepEt(IzinTalebiDto talepDto)
        {
            var kullanici = _context.Users.Find(talepDto.KullaniciID);
            if (kullanici == null) return false;

            double hesaplananGun = 0;

            if (talepDto.SureTipi == IzinSureTipi.KisaSureli)
            {
                hesaplananGun = 0.25;
            }
            else if (talepDto.SureTipi == IzinSureTipi.YarimGun)
            {
                hesaplananGun = 0.5;
            }
            else
            {
                var resmiTatiller = new List<DateTime>
                {
                    new DateTime(DateTime.Now.Year, 1, 1),
                    new DateTime(DateTime.Now.Year, 4, 23),
                    new DateTime(DateTime.Now.Year, 5, 1),
                    new DateTime(DateTime.Now.Year, 5, 19),
                    new DateTime(DateTime.Now.Year, 7, 15),
                    new DateTime(DateTime.Now.Year, 8, 30),
                    new DateTime(DateTime.Now.Year, 10, 29)
                };

                double netIzinGunu = 0;
                for (DateTime tarih = talepDto.BaslangicTarihi.Date; tarih <= talepDto.BitisTarihi.Date; tarih = tarih.AddDays(1))
                {
                    if (tarih.DayOfWeek == DayOfWeek.Saturday || tarih.DayOfWeek == DayOfWeek.Sunday)
                    {
                        continue;
                    }

                    if (resmiTatiller.Any(rt => rt.Month == tarih.Month && rt.Day == tarih.Day))
                    {
                        continue;
                    }

                    netIzinGunu++;
                }
                hesaplananGun = netIzinGunu;
            }

            // --- EKLEME: Tüm Kategoriler İçin Kota Limit Kontrolü ---
            if (talepDto.Kategori == IzinKategorisi.YillikIzin && kullanici.KalanYillikIzin < hesaplananGun)
            {
                return false;
            }
            if (talepDto.Kategori == IzinKategorisi.MazeretIzni && kullanici.MazeretIzinKotasi < hesaplananGun)
            {
                return false;
            }
            if (talepDto.Kategori == IzinKategorisi.UcretsizIzin && kullanici.UcretsizIzinKotasi < hesaplananGun)
            {
                return false;
            }

            var yeniTalep = new IzinTalebi
            {
                KullaniciID = talepDto.KullaniciID,
                Kategori = talepDto.Kategori,
                SureTipi = talepDto.SureTipi,
                BaslangicTarihi = talepDto.BaslangicTarihi,
                BitisTarihi = talepDto.BitisTarihi,
                ToplamGun = hesaplananGun,
                Durum = IzinDurumu.Beklemede,
                Aciklama = talepDto.Aciklama,
                OlusturulmaTarihi = DateTime.Now
            };

            _context.IzinTalepleri.Add(yeniTalep);
            _context.SaveChanges();
            return true;
        }

        public List<IzinListeleDto> PersonelIzinleriniGetir(int kullaniciId)
        {
            var list = _context.IzinTalepleri
                .Include(i => i.Kullanici)
                .Where(i => i.KullaniciID == kullaniciId)
                .AsEnumerable();

            return list.Select(i => new IzinListeleDto
            {
                ID = i.ID,
                KullaniciID = i.KullaniciID,
                PersonelAdi = i.Kullanici?.Name ?? "Bilinmeyen Personel",
                Kategori = i.Kategori,
                SureTipi = i.SureTipi,
                BaslangicTarihi = i.BaslangicTarihi,
                BitisTarihi = i.BitisTarihi,
                ToplamGun = i.ToplamGun,
                Durum = i.Durum,
                Aciklama = i.Aciklama,
                OlusturulmaTarihi = i.OlusturulmaTarihi
            }).ToList();
        }

        public List<IzinListeleDto> YoneticiOnayListesiniGetir(int yoneticiId)
        {
            var list = _context.IzinTalepleri
                .Include(i => i.Kullanici)
                .Where(i => i.Kullanici.ManagerID == yoneticiId && i.Durum == IzinDurumu.Beklemede)
                .AsEnumerable();

            return list.Select(i => new IzinListeleDto
            {
                ID = i.ID,
                KullaniciID = i.KullaniciID,
                PersonelAdi = i.Kullanici?.Name ?? "Bilinmeyen Personel",
                Kategori = i.Kategori,
                SureTipi = i.SureTipi,
                BaslangicTarihi = i.BaslangicTarihi,
                BitisTarihi = i.BitisTarihi,
                ToplamGun = i.ToplamGun,
                Durum = i.Durum,
                Aciklama = i.Aciklama,
                OlusturulmaTarihi = i.OlusturulmaTarihi
            }).ToList();
        }

        public bool IzinDurumunuGuncelle(IzinOnayDto onayDto)
        {
            var talep = _context.IzinTalepleri.Include(i => i.Kullanici).FirstOrDefault(i => i.ID == onayDto.IzinID);
            if (talep == null || talep.Durum != IzinDurumu.Beklemede) return false;

            talep.Durum = onayDto.Durum;

            // --- GÜNCELLEME: Onay Durumunda İlgili Kotadan Düşüş Yapılması ---
            if (onayDto.Durum == IzinDurumu.Onaylandi)
            {
                // Yarım/Kısa günlerin int bakiyeden eksilmemesi riskine karşı Math.Ceiling (Yukarı yuvarlama) kullanıldı.
                // Eğer sisteminizde kotaların da double olması gerekirse veri tabanı katmanında veri tipi değişmelidir.
                int dusulecekGun = (int)Math.Ceiling(talep.ToplamGun);

                if (talep.Kategori == IzinKategorisi.YillikIzin)
                {
                    talep.Kullanici.KalanYillikIzin -= dusulecekGun;
                }
                else if (talep.Kategori == IzinKategorisi.MazeretIzni)
                {
                    talep.Kullanici.MazeretIzinKotasi -= dusulecekGun;
                }
                else if (talep.Kategori == IzinKategorisi.UcretsizIzin)
                {
                    talep.Kullanici.UcretsizIzinKotasi -= dusulecekGun;
                }
            }

            _context.SaveChanges();
            return true;
        }
    }
}