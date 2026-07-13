using System;
using System.Web.Http;
using System.Web.Helpers;
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
                    var personeller = System.Linq.Queryable.Where(db.Users, u => u.Role == "0");
                    var liste = new System.Collections.Generic.List<object>();

                    foreach (var p in personeller)
                    {
                        liste.Add(new
                        {
                            ad = p.Name,
                            rol = "Personel",
                            eposta = p.Email,
                            kalanIzin = p.KalanYillikIzin + " Gün"
                        });
                    }

                    return Ok(liste);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("PERSONEL LISTESI HATASI: " + ex.ToString());
                return BadRequest("Veritabanından personeller çekilemedi: " + ex.Message);
            }
        }

        // FİLTRELİ YENİ SÜRÜM: Sadece bu yöneticiye bağlı bekleyen izinleri getirir
        [HttpGet]
        [Route("get-pending-leaves/{adminId}")]
        public IHttpActionResult GetPendingLeaves(int adminId)
        {
            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var talepler = System.Linq.Queryable.Where(db.IzinTalepleri,
                        t => t.Durum == IzinDurumu.Beklemede && t.Kullanici.ManagerID == adminId);

                    var liste = new System.Collections.Generic.List<object>();

                    foreach (var t in talepler)
                    {
                        liste.Add(new
                        {
                            id = t.ID,
                            personelId = t.KullaniciID,
                            ad = t.Kullanici != null ? t.Kullanici.Name : "İsimsiz Personel",
                            kategori = ((int)t.Kategori).ToString(),
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
                System.Diagnostics.Debug.WriteLine("IZIN LISTESI HATASI: " + ex.ToString());
                return BadRequest("Bekleyen izin talepleri çekilemedi: " + ex.Message);
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
                    var talepler = System.Linq.Queryable.Where(db.IzinTalepleri, t => t.KullaniciID == userId);
                    var liste = new System.Collections.Generic.List<object>();

                    foreach (var t in talepler)
                    {
                        liste.Add(new
                        {
                            id = t.ID,
                            personelId = t.KullaniciID,
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
                return BadRequest("Kullanıcı izinleri çekilemedi: " + ex.Message);
            }
        }

        [HttpPost]
        [Route("register")]
        public IHttpActionResult Register([FromBody] RegisterDto dto)
        {
            if (dto == null)
                return BadRequest("Backend Hatası: Veri çözülemedi.");

            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var varOlanKullanici = System.Linq.Queryable.FirstOrDefault(db.Users, u => u.Email == dto.Email);

                    if (varOlanKullanici != null)
                        return BadRequest("Bu e-posta adresi zaten sisteme kayıtlı.");

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
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Veritabanı hatası: " + ex.Message);
            }

            return Ok("Şirket kurulumu başarıyla tamamlandı!");
        }

        [HttpPost]
        [Route("register-personel")]
        public IHttpActionResult RegisterPersonel([FromBody] RegisterDto dto)
        {
            if (dto == null)
                return BadRequest("Backend Hatası: Veri boş.");

            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var varOlanKullanici = System.Linq.Queryable.FirstOrDefault(db.Users, u => u.Email == dto.Email);

                    if (varOlanKullanici != null)
                        return BadRequest("Bu e-posta adresiyle kayıtlı bir personel zaten var.");

                    DateTime iseGiris = dto.IseBaslamaTarihi ?? DateTime.Now;
                    DateTime bugun = DateTime.Now;

                    int kidemYili = bugun.Year - iseGiris.Year;
                    if (iseGiris > bugun.AddYears(-kidemYili)) kidemYili--;

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
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Veritabanı hatası: " + ex.Message);
            }

            return Ok("Personel başarıyla veritabanına kaydedildi!");
        }

        [HttpPost]
        [Route("update-company-quotas")]
        public IHttpActionResult UpdateCompanyQuotas([FromBody] CompanyQuotaDto dto)
        {
            if (dto == null) return BadRequest("Geçersiz kota verisi.");

            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var adminUser = System.Linq.Queryable.FirstOrDefault(db.Users, u => u.ID == dto.AdminId);
                    if (adminUser == null) return BadRequest("Yönetici bulunamadı.");

                    adminUser.MazeretIzinKotasi = dto.MazeretKota;
                    adminUser.UcretsizIzinKotasi = dto.UcretsizKota;

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Veritabanı hatası: " + ex.Message);
            }

            return Ok("Şirket izin kotaları başarıyla güncellendi!");
        }

        [HttpPost]
        [Route("request-leave")]
        public IHttpActionResult RequestLeave([FromBody] RequestLeaveDto dto)
        {
            if (dto == null) return BadRequest("Geçersiz talep verisi.");

            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var personel = System.Linq.Queryable.FirstOrDefault(db.Users, u => u.ID == dto.KullaniciId);
                    if (personel == null) return BadRequest("Personel bulunamadı.");

                    IzinTalebi yeniTalep = new IzinTalebi
                    {
                        KullaniciID = dto.KullaniciId,
                        Kategori = dto.Kategori,
                        SureTipi = dto.SureTipi,
                        BaslangicTarihi = dto.BaslangicTarihi,
                        BitisTarihi = dto.BitisTarihi,
                        ToplamGun = dto.ToplamGun,
                        Durum = IzinDurumu.Beklemede,
                        Aciklama = dto.Aciklama,
                        OlusturulmaTarihi = DateTime.Now
                    };

                    db.IzinTalepleri.Add(yeniTalep);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                return BadRequest("İzin talebi oluşturulurken hata: " + ex.Message);
            }

            return Ok("İzin talebiniz başarıyla yöneticiye iletildi!");
        }

        [HttpPost]
        [Route("respond-leave")]
        public IHttpActionResult RespondLeave([FromBody] RespondLeaveDto dto)
        {
            if (dto == null) return BadRequest("Geçersiz işlem verisi.");

            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var talep = System.Linq.Queryable.FirstOrDefault(db.IzinTalepleri, t => t.ID == dto.TalepId);
                    if (talep == null) return BadRequest("İzin talebi bulunamadı.");

                    var personel = System.Linq.Queryable.FirstOrDefault(db.Users, u => u.ID == talep.KullaniciID);
                    if (personel == null) return BadRequest("İlgili personel bulunamadı.");

                    if (dto.OnaylandiMi)
                    {
                        talep.Durum = IzinDurumu.Onaylandi;

                        if (talep.Kategori == IzinKategorisi.YillikIzin)
                            personel.KalanYillikIzin -= (int)talep.ToplamGun;
                        else if (talep.Kategori == IzinKategorisi.MazeretIzni)
                            personel.MazeretIzinKotasi -= (int)talep.ToplamGun;
                        else if (talep.Kategori == IzinKategorisi.UcretsizIzin)
                            personel.UcretsizIzinKotasi -= (int)talep.ToplamGun;
                    }
                    else
                    {
                        talep.Durum = IzinDurumu.Reddedildi;
                    }

                    db.SaveChanges();
                    return Ok("İzin talebi başarıyla sonuçlandırıldı!");
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Veritabanı hatası: " + ex.Message);
            }
        }

        [HttpPost]
        [Route("login")]
        public IHttpActionResult Login([FromBody] LoginDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
            {
                return BadRequest("Lütfen tüm alanları doldurun.");
            }

            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var kullanici = System.Linq.Queryable.FirstOrDefault(db.Users, u => u.Email == dto.Email);
                    if (kullanici == null) return BadRequest("E-posta adresi sistemde bulunamadı.");

                    bool sifreDogruMu = false;
                    try { sifreDogruMu = Crypto.VerifyHashedPassword(kullanici.PasswordHash, dto.Password); }
                    catch { sifreDogruMu = (kullanici.PasswordHash == dto.Password); }

                    if (!sifreDogruMu) return BadRequest("Girdiğiniz şifre hatalı.");

                    return Ok(new
                    {
                        Message = "Giriş başarılı!",
                        Id = kullanici.ID,
                        Name = kullanici.Name,
                        Email = kullanici.Email,
                        Role = kullanici.Role,
                        KalanIzin = kullanici.KalanYillikIzin,
                        MazeretKota = kullanici.MazeretIzinKotasi,
                        UcretsizKota = kullanici.UcretsizIzinKotasi
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Sistem Hatası: " + ex.Message);
            }
        }
    }
}