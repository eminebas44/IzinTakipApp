using System;
using System.Web.Http;
using System.Web.Helpers;
using System.Linq;
using System.Collections.Generic;
using IzinTakipApp.Data;
using IzinTakipApp.Models;
using IzinTakipApp.Enums;
using IzinTakip.API.Models.DTOs;

namespace IzinTakip.API.Controllers
{
    [RoutePrefix("api/auth")]
    public class AuthController : ApiController
    {
        [HttpGet]
        [Route("get-personels")]
        public IHttpActionResult GetPersonels()
        {
            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var personeller = db.Users.Where(u => u.Role == "0").ToList();
                    var liste = new List<object>();

                    foreach (var p in personeller)
                    {
                        liste.Add(new
                        {
                            id = p.ID,
                            ad = p.Name,
                            rol = "Personel",
                            eposta = p.Email,
                            kalanIzin = p.KalanYillikIzin,
                            mazeretKota = p.MazeretIzinKotasi,
                            ucretsizKota = p.UcretsizIzinKotasi
                        });
                    }
                    return Ok(liste);
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Personeller çekilemedi: " + ex.Message);
            }
        }

        [HttpGet]
        [Route("get-pending-leaves/{adminId}")]
        public IHttpActionResult GetPendingLeaves(int adminId)
        {
            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var tumTalepler = db.IzinTalepleri.Where(t => t.Durum == IzinDurumu.Beklemede).ToList();
                    var tumKullanicilar = db.Users.ToList();

                    var benimPersonelIdleri = tumKullanicilar
                                                 .Where(u => u.ManagerID == adminId && u.Role == "0")
                                                 .Select(u => u.ID)
                                                 .ToList();

                    var liste = new List<object>();

                    foreach (var talep in tumTalepler)
                    {
                        if (benimPersonelIdleri.Contains(talep.KullaniciId))
                        {
                            var personel = tumKullanicilar.FirstOrDefault(u => u.ID == talep.KullaniciId);
                            string personelAdi = personel != null ? personel.Name : "Bilinmeyen Personel";

                            liste.Add(new
                            {
                                id = talep.ID,
                                personelId = talep.KullaniciId,
                                ad = personelAdi,
                                kategori = (int)talep.Kategori,
                                baslangic = talep.BaslangicTarihi.ToString("yyyy-MM-dd"),
                                bitis = talep.BitisTarihi.ToString("yyyy-MM-dd"),
                                gun = talep.ToplamGun,
                                aciklama = talep.Aciklama
                            });
                        }
                    }

                    return Ok(liste);
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Bekleyen izinler çekilemedi: " + ex.Message);
            }
        }

        [HttpGet]
        [Route("get-personel-leaves/{userId}")]
        public IHttpActionResult GetPersonelLeaves(int userId)
        {
            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var talepler = db.IzinTalepleri.Where(t => t.KullaniciId == userId).ToList();
                    var liste = new List<object>();

                    foreach (var t in talepler)
                    {
                        liste.Add(new
                        {
                            id = t.ID,
                            durum = (int)t.Durum,
                            kategori = (int)t.Kategori,
                            baslangic = t.BaslangicTarihi.ToString("yyyy-MM-dd"),
                            bitis = t.BitisTarihi.ToString("yyyy-MM-dd"),
                            gun = t.ToplamGun,
                            aciklama = t.Aciklama
                        });
                    }
                    return Ok(liste);
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Personel izin geçmişi çekilemedi: " + ex.Message);
            }
        }

        [HttpPost]
        [Route("register")]
        public IHttpActionResult Register([FromBody] RegisterDto dto)
        {
            if (dto == null) return BadRequest("Geçersiz kayıt verisi.");

            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var varOlan = db.Users.FirstOrDefault(u => u.Email == dto.Email);
                    if (varOlan != null) return BadRequest("Bu e-posta adresi zaten kayıtlı.");

                    Kullanici yeniKullanici = new Kullanici
                    {
                        Name = dto.AdminName ?? "Yeni Yönetici",
                        Email = dto.Email,
                        PasswordHash = Crypto.HashPassword(dto.Password),
                        Role = "1",
                        IseGirisTarihi = DateTime.Now,
                        KalanYillikIzin = 30,
                        MazeretIzinKotasi = 5,
                        UcretsizIzinKotasi = 15
                    };

                    db.Users.Add(yeniKullanici);
                    db.SaveChanges();
                    return Ok("Şirket yöneticisi kaydı başarılı.");
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Kayıt hatası: " + ex.Message);
            }
        }

        [HttpPost]
        [Route("register-personel")]
        public IHttpActionResult RegisterPersonel([FromBody] RegisterDto dto)
        {
            if (dto == null) return BadRequest("Personel verisi boş.");

            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var varOlan = db.Users.FirstOrDefault(u => u.Email == dto.Email);
                    if (varOlan != null) return BadRequest("Bu personel zaten kayıtlı.");

                    DateTime iseGiris = dto.IseBaslamaTarihi ?? DateTime.Now;
                    int kidemYili = DateTime.Now.Year - iseGiris.Year;
                    int tanimlananIzinGunu = kidemYili >= 5 ? 20 : kidemYili >= 1 ? 14 : 5;

                    Kullanici yeniPersonel = new Kullanici
                    {
                        Name = dto.AdminName,
                        Email = dto.Email,
                        PasswordHash = Crypto.HashPassword(dto.Password),
                        Role = "0",
                        IseGirisTarihi = iseGiris,
                        KalanYillikIzin = tanimlananIzinGunu,
                        MazeretIzinKotasi = 5,
                        UcretsizIzinKotasi = 15,
                        ManagerID = dto.ManagerID
                    };

                    db.Users.Add(yeniPersonel);
                    db.SaveChanges();
                    return Ok("Personel başarıyla tanımlandı.");
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Personel eklenemedi: " + ex.Message);
            }
        }

        [HttpPost]
        [Route("update-company-quotas")]
        public IHttpActionResult UpdateCompanyQuotas([FromBody] CompanyQuotaDto dto)
        {
            if (dto == null) return BadRequest("Kota verisi eksik.");

            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var adminUser = db.Users.FirstOrDefault(u => u.ID == dto.AdminId);
                    if (adminUser == null) return BadRequest("Yönetici bulunamadı.");

                    adminUser.MazeretIzinKotasi = dto.MazeretKota;
                    adminUser.UcretsizIzinKotasi = dto.UcretsizKota;

                    db.SaveChanges();
                    return Ok("Şirket kotaları başarıyla güncellendi.");
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Kota güncelleme hatası: " + ex.Message);
            }
        }

        [HttpPost]
        [Route("request-leave")]
        public IHttpActionResult RequestLeave([FromBody] RequestLeaveDto dto)
        {
            if (dto == null) return BadRequest("Talep verisi eksik.");

            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var personel = db.Users.FirstOrDefault(u => u.ID == dto.KullaniciId);
                    if (personel == null) return BadRequest("Personel bulunamadı.");

                    if (dto.Kategori == IzinKategorisi.YillikIzin && dto.ToplamGun > personel.KalanYillikIzin)
                        return BadRequest("Yıllık izin limitinizi aşıyorsunuz.");

                    IzinTalebi yeniTalep = new IzinTalebi
                    {
                        KullaniciId = dto.KullaniciId,
                        Kategori = dto.Kategori,
                        SureTipi = dto.SureTipi,
                        BaslangicTarihi = dto.BaslangicTarihi,
                        BitisTarihi = dto.BitisTarihi,
                        ToplamGun = (int)dto.ToplamGun,
                        Durum = IzinDurumu.Beklemede,
                        Aciklama = dto.Aciklama,
                        OlusturulmaTarihi = DateTime.Now
                    };

                    db.IzinTalepleri.Add(yeniTalep);
                    db.SaveChanges();
                    return Ok("İzin talebi başarıyla iletildi.");
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Talep oluşturulamadı: " + ex.Message);
            }
        }

        [HttpPost]
        [Route("respond-leave")]
        public IHttpActionResult RespondLeave([FromBody] RespondLeaveDto dto)
        {
            if (dto == null) return BadRequest("İşlem verisi eksik.");

            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var talep = db.IzinTalepleri.FirstOrDefault(t => t.ID == dto.TalepId);
                    if (talep == null) return BadRequest("Talep bulunamadı.");

                    var personel = db.Users.FirstOrDefault(u => u.ID == talep.KullaniciId);
                    if (personel == null) return BadRequest("Personel bulunamadı.");

                    if (dto.OnaylandiMi)
                    {
                        talep.Durum = IzinDurumu.Onaylandi;
                        if (talep.Kategori == IzinKategorisi.YillikIzin) personel.KalanYillikIzin -= talep.ToplamGun;
                        if (talep.Kategori == IzinKategorisi.MazeretIzni) personel.MazeretIzinKotasi -= talep.ToplamGun;
                        if (talep.Kategori == IzinKategorisi.UcretsizIzin) personel.UcretsizIzinKotasi -= talep.ToplamGun;
                    }
                    else
                    {
                        talep.Durum = IzinDurumu.Reddedildi;
                    }

                    db.SaveChanges();
                    return Ok("İzin talebi sonuçlandırıldı.");
                }
            }
            catch (Exception ex)
            {
                return BadRequest("İşlem başarısız: " + ex.Message);
            }
        }

        [HttpPost]
        [Route("login")]
        public IHttpActionResult Login([FromBody] LoginDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
                return BadRequest("Lütfen tüm alanları doldurun.");

            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var kullanici = db.Users.FirstOrDefault(u => u.Email == dto.Email);
                    if (kullanici == null) return BadRequest("E-posta adresi bulunamadı.");

                    bool sifreDogruMu = false;
                    try { sifreDogruMu = Crypto.VerifyHashedPassword(kullanici.PasswordHash, dto.Password); }
                    catch { sifreDogruMu = (kullanici.PasswordHash == dto.Password); }

                    if (!sifreDogruMu) return BadRequest("Hatalı şifre.");

                    int userId = kullanici.ID;
                    string userName = kullanici.Name;
                    string userEmail = kullanici.Email;
                    string userRole = kullanici.Role;
                    double kalanIzin = kullanici.KalanYillikIzin;
                    double mazeretKota = kullanici.MazeretIzinKotasi;
                    double ucretsizKota = kullanici.UcretsizIzinKotasi;

                    return Ok(new
                    {
                        message = "Giriş başarılı!",
                        UserID = userId,
                        ID = userId,
                        name = userName,
                        email = userEmail,
                        role = userRole,
                        kalanIzin = kalanIzin,
                        mazeretKota = mazeretKota,
                        ucretsizKota = ucretsizKota
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Sistem hatası: " + ex.Message);
            }
        }
    }
}