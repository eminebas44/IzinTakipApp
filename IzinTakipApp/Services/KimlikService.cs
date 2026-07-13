using System;
using System.Linq;
using IzinTakipApp.Data;
using IzinTakipApp.DTOs;
using IzinTakipApp.Helpers;
using IzinTakipApp.Models;

namespace IzinTakipApp.Services
{
    public class KimlikService : IKimlikService
    {
        private readonly AppDbContext _context;

        public KimlikService()
        {
            _context = new AppDbContext();
        }

        public bool KayitOl(KullaniciKayitDto kayitDto)
        {
            // Aynı e-posta adresiyle başka bir kullanıcı var mı kontrol et
            var kullaniciVarMi = _context.Users.Any(u => u.Email == kayitDto.Email);
            if (kullaniciVarMi) return false;

            // DTO'dan gelen verileri ana Kullanici modelimize eşliyoruz
            var yeniKullanici = new Kullanici
            {
                Name = kayitDto.Name,
                Email = kayitDto.Email,
                PasswordHash = HashingHelper.CreatePasswordHash(kayitDto.Password), // Şifreyi hashleyerek koruyoruz
                Role = kayitDto.Role,
                IseGirisTarihi = kayitDto.IseGirisTarihi == default ? DateTime.Now : kayitDto.IseGirisTarihi,
                KalanYillikIzin = 15, // Yeni başlayan personele varsayılan 15 gün izin tanımlıyoruz
                ManagerID = kayitDto.ManagerID
            };

            _context.Users.Add(yeniKullanici);
            _context.SaveChanges();
            return true;
        }

        public Kullanici GirişYap(KullaniciGirisDto girisDto)
        {
            // Kullanıcıyı e-posta adresine göre bul
            var kullanici = _context.Users.FirstOrDefault(u => u.Email == girisDto.Email);
            if (kullanici == null) return null;

            // Girdiği şifreyi veritabanındaki hash ile doğrula
            var sifreDogruMu = HashingHelper.VerifyPasswordHash(girisDto.Password, kullanici.PasswordHash);
            if (!sifreDogruMu) return null;

            // Giriş başarılıysa kullanıcının tüm profilini (Rolü dahil) geri dön
            return kullanici;
        }
    }
}