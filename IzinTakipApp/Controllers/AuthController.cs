using System;
using System.Web.Http;
using System.Web.Helpers;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using IzinTakipApp.Data;
using IzinTakipApp.Models;
using IzinTakipApp.Enums;
using IzinTakip.API.Models.DTOs;

namespace IzinTakip.API.Controllers
{
    public class ForgotPasswordDto
    {
        public string Email { get; set; }
        public string Code { get; set; }
        public string NewPassword { get; set; }
    }

    public class BulkRespondLeaveDto
    {
        public List<int> TalepIdleri { get; set; }
        public bool OnaylandiMi { get; set; }
        public bool IzneYansitilsinMi { get; set; } = true;
    }

    [RoutePrefix("api/auth")]
    public class AuthController : ApiController
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly Dictionary<string, string> VerificationCodes = new Dictionary<string, string>();

        [HttpGet]
        [Route("get-personels/{adminId}")]
        public IHttpActionResult GetPersonels(int adminId)
        {
            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    // İstek atan kullanıcı İK ise, bağlı olduğu Yönetici (Admin) ID'sini tespit ediyoruz
                    var reqUser = db.Users.FirstOrDefault(u => u.ID == adminId);
                    int targetManagerId = (reqUser != null && reqUser.Role == "2" && reqUser.ManagerID.HasValue)
                                          ? reqUser.ManagerID.Value
                                          : adminId;

                    // Personel ("0") ve İnsan Kaynakları ("2") rollerindeki tüm çalışanları çekiyoruz
                    var personeller = db.Users.Where(u => (u.Role == "0" || u.Role == "2") && u.ManagerID == targetManagerId).ToList();
                    var liste = new List<object>();

                    foreach (var p in personeller)
                    {
                        int iseGirisYili = p.IseGirisTarihi.Year;
                        int buYil = DateTime.Now.Year;
                        int toplamHakEdilen = 0;

                        for (int y = iseGirisYili; y <= buYil; y++)
                        {
                            int kidem = y - iseGirisYili;
                            toplamHakEdilen += kidem >= 5 ? 20 : kidem >= 1 ? 14 : 5;
                        }

                        string rolAdi = p.Role == "2" ? "İnsan Kaynakları" : "Personel";

                        liste.Add(new
                        {
                            id = p.ID,
                            ad = p.Name,
                            rol = rolAdi,
                            rolId = p.Role,
                            eposta = p.Email,
                            kalanIzin = p.KalanYillikIzin,
                            toplamHakEdilen = toplamHakEdilen,
                            mazeretKota = p.MazeretIzinKotasi,
                            ucretsizKota = p.UcretsizIzinKotasi,
                            iseBaslamaTarihi = p.IseGirisTarihi.ToString("yyyy-MM-dd")
                        });
                    }
                    return Ok(liste);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Personel listesi çekilirken hata oluştu. Yönetici ID: {adminId}");
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
                    var reqUser = db.Users.FirstOrDefault(u => u.ID == adminId);
                    int targetManagerId = (reqUser != null && reqUser.Role == "2" && reqUser.ManagerID.HasValue)
                                          ? reqUser.ManagerID.Value
                                          : adminId;

                    var tumTalepler = db.IzinTalepleri.Where(t => t.Durum == IzinDurumu.Beklemede).ToList();
                    var tumKullanicilar = db.Users.ToList();

                    var benimPersonelIdleri = tumKullanicilar
                                                   .Where(u => u.ManagerID == targetManagerId && (u.Role == "0" || u.Role == "2"))
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
                                aciklama = talep.Aciklama,
                                raporUrl = talep.RaporUrl,
                                raporDosyasi = talep.RaporUrl
                            });
                        }
                    }

                    return Ok(liste);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Onay bekleyen izinler çekilirken hata oluştu. Yönetici ID: {adminId}");
                return BadRequest("Bekleyen izinler çekilemedi: " + ex.Message);
            }
        }

        [HttpGet]
        [Route("get-past-leaves/{adminId}")]
        public IHttpActionResult GetPastLeaves(int adminId)
        {
            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var reqUser = db.Users.FirstOrDefault(u => u.ID == adminId);
                    int targetManagerId = (reqUser != null && reqUser.Role == "2" && reqUser.ManagerID.HasValue)
                                          ? reqUser.ManagerID.Value
                                          : adminId;

                    var tumTalepler = db.IzinTalepleri.Where(t => t.Durum != IzinDurumu.Beklemede).ToList();
                    var tumKullanicilar = db.Users.ToList();

                    var benimPersonelIdleri = tumKullanicilar
                                                   .Where(u => u.ManagerID == targetManagerId && (u.Role == "0" || u.Role == "2"))
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
                                durum = (int)talep.Durum,
                                aciklama = talep.Aciklama,
                                raporUrl = talep.RaporUrl,
                                raporDosyasi = talep.RaporUrl
                            });
                        }
                    }

                    return Ok(liste);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Geçmiş arşiv verileri çekilirken hata oluştu. Yönetici ID: {adminId}");
                return BadRequest("Geçmiş izin arşiv verileri çekilemedi: " + ex.Message);
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
                            aciklama = t.Aciklama,
                            olusturulmaTarihi = t.OlusturulmaTarihi.ToString("yyyy-MM-ddTHH:mm:ss"),
                            raporUrl = t.RaporUrl
                        });
                    }
                    return Ok(liste);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Personel kendi izin geçmişini çekerken sistem hatası oluştu. Personel ID: {userId}");
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
                        UcretsizIzinKotasi = 15,
                        MaxIzinliKota = 3
                    };

                    db.Users.Add(yeniKullanici);
                    db.SaveChanges();

                    Logger.Info($"Yeni şirket yöneticisi kaydı başarılı. E-posta: {dto.Email}, Adı: {yeniKullanici.Name}");
                    return Ok("Şirket yöneticisi kaydı başarılı.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Yönetici register aşamasında teknik hata meydana geldi. E-posta: {dto?.Email}");
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

                    var adminUser = db.Users.FirstOrDefault(u => u.ID == dto.ManagerID);

                    string atanacakRol = string.IsNullOrEmpty(dto.Role) ? "0" : dto.Role;

                    Kullanici yeniPersonel = new Kullanici
                    {
                        Name = dto.AdminName,
                        Email = dto.Email,
                        PasswordHash = Crypto.HashPassword(dto.Password),
                        Role = atanacakRol,
                        IseGirisTarihi = iseGiris,
                        KalanYillikIzin = tanimlananIzinGunu,
                        MazeretIzinKotasi = adminUser != null ? adminUser.MazeretIzinKotasi : 5,
                        UcretsizIzinKotasi = adminUser != null ? adminUser.UcretsizIzinKotasi : 15,
                        ManagerID = dto.ManagerID
                    };

                    db.Users.Add(yeniPersonel);
                    db.SaveChanges();

                    Logger.Info($"Şirkete yeni çalışan tanımlandı. Adı: {dto.AdminName}, E-posta: {dto.Email}, Rol: {atanacakRol}, Yönetici ID: {dto.ManagerID}");
                    return Ok("Personel başarıyla tanımlandı.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Personel tanımlama servisinde hata meydana geldi. Eklenmeye çalışılan: {dto?.Email}");
                return BadRequest("Personel eklenemedi: " + ex.Message);
            }
        }

        [HttpPost]
        [Route("update-personel")]
        public IHttpActionResult UpdatePersonel([FromBody] RegisterDto dto)
        {
            if (dto == null) return BadRequest("Güncelleme verisi eksik.");

            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var personel = db.Users.FirstOrDefault(u => u.ID == dto.id);
                    if (personel == null) return NotFound();

                    personel.Name = dto.AdminName;
                    personel.Email = dto.Email;

                    if (!string.IsNullOrEmpty(dto.Role))
                    {
                        personel.Role = dto.Role;
                    }

                    if (dto.IseBaslamaTarihi.HasValue)
                    {
                        if (personel.IseGirisTarihi.Date != dto.IseBaslamaTarihi.Value.Date)
                        {
                            personel.IseGirisTarihi = dto.IseBaslamaTarihi.Value;
                            int kidemYili = DateTime.Now.Year - dto.IseBaslamaTarihi.Value.Year;
                            personel.KalanYillikIzin = kidemYili >= 5 ? 20 : kidemYili >= 1 ? 14 : 5;
                        }
                    }

                    db.SaveChanges();
                    Logger.Info($"Personel kartı bilgileri güncellendi. Personel ID: {dto.id}, Yeni Adı: {dto.AdminName}");
                    return Ok("Personel başarıyla güncellendi.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Personel kartı güncellenirken hata oluştu. Personel ID: {dto?.id}");
                return BadRequest("Güncelleme hatası: " + ex.Message);
            }
        }

        [HttpPost]
        [Route("update-company-quotas")]
        public IHttpActionResult UpdateCompanyQuotas([FromBody] CompanyQuotaDto dto)
        {
            if (dto == null) return BadRequest("Kota güncelleme verisi eksik.");

            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var adminUser = db.Users.FirstOrDefault(u => u.ID == dto.AdminId);
                    if (adminUser == null) return BadRequest("Yönetici bulunamadı.");

                    adminUser.MazeretIzinKotasi = dto.MazeretKota;
                    adminUser.UcretsizIzinKotasi = dto.UcretsizKota;
                    adminUser.MaxIzinliKota = dto.MaxIzinliKota;
                    adminUser.SirketPolitikasi = dto.SirketPolitikasi ?? string.Empty;

                    var bagliPersoneller = db.Users.Where(u => u.ManagerID == dto.AdminId && (u.Role == "0" || u.Role == "2")).ToList();
                    foreach (var personel in bagliPersoneller)
                    {
                        personel.MazeretIzinKotasi = dto.MazeretKota;
                        personel.UcretsizIzinKotasi = dto.UcretsizKota;
                    }

                    db.SaveChanges();
                    Logger.Info($"Yönetici şirket genel kotalarını ve politikasını güncelledi. Yönetici ID: {dto.AdminId}, Mazeret: {dto.MazeretKota}, Ücretsiz: {dto.UcretsizKota}, Maks Aynı Gün: {dto.MaxIzinliKota}");
                    return Ok("Şirket kotaları ve politikası başarıyla güncellendi.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Şirket kotaları güncellenirken genel hata oluştu.");
                return BadRequest("Kota güncelleme hatası: " + ex.Message);
            }
        }

        [HttpGet]
        [Route("get-company-policy/{managerId}")]
        public IHttpActionResult GetCompanyPolicy(int managerId)
        {
            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var reqUser = db.Users.FirstOrDefault(u => u.ID == managerId);
                    int targetManagerId = (reqUser != null && reqUser.Role == "2" && reqUser.ManagerID.HasValue)
                                          ? reqUser.ManagerID.Value
                                          : managerId;

                    var adminUser = db.Users.FirstOrDefault(u => u.ID == targetManagerId);
                    if (adminUser == null)
                    {
                        return Ok(new { sirketPolitikasi = string.Empty });
                    }

                    return Ok(new { sirketPolitikasi = adminUser.SirketPolitikasi ?? string.Empty });
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Şirket politikası çekilirken hata oluştu. Yönetici ID: {managerId}");
                return BadRequest("Şirket politikası çekilemedi: " + ex.Message);
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

                    double hesaplananGun = (dto.BitisTarihi.Date - dto.BaslangicTarihi.Date).TotalDays + 1;

                    int maxIzinliLimiti = 3;
                    var adminUser = db.Users.FirstOrDefault(u => u.ID == personel.ManagerID);
                    if (adminUser != null && adminUser.MaxIzinliKota > 0)
                    {
                        maxIzinliLimiti = adminUser.MaxIzinliKota;
                    }

                    for (DateTime date = dto.BaslangicTarihi.Date; date <= dto.BitisTarihi.Date; date = date.AddDays(1))
                    {
                        int oGunIzinliOlanlar = db.IzinTalepleri.Count(t =>
                            t.Durum == IzinDurumu.Onaylandi &&
                            t.BaslangicTarihi <= date &&
                            t.BitisTarihi >= date &&
                            db.Users.Any(u => u.ID == t.KullaniciId && u.ManagerID == personel.ManagerID)
                        );

                        if (oGunIzinliOlanlar >= maxIzinliLimiti)
                        {
                            Logger.Warn($"İzin talebi reddedildi (Aynı gün maks personel sınırı aşıldı). Personel ID: {dto.KullaniciId}, Tarih: {date:yyyy-MM-dd}");
                            return BadRequest($"{date.ToString("dd MMMM yyyy")} tarihinde maksimum izinli personel sayısı {maxIzinliLimiti} kişidir.");
                        }
                    }

                    if (dto.Kategori == IzinKategorisi.YillikIzin && hesaplananGun > personel.KalanYillikIzin)
                        return BadRequest("İzin kotanızın üstünde izin talep ettiniz, izin alınamaz.");

                    if (dto.Kategori == IzinKategorisi.MazeretIzni && hesaplananGun > personel.MazeretIzinKotasi)
                        return BadRequest("İzin kotanızın üstünde izin talep ettiniz, izin alınamaz.");

                    if (dto.Kategori == IzinKategorisi.UcretsizIzin && hesaplananGun > personel.UcretsizIzinKotasi)
                        return BadRequest("İzin kotanızın üstünde izin talep ettiniz, izin alınamaz.");

                    IzinTalebi yeniTalep = new IzinTalebi
                    {
                        KullaniciId = dto.KullaniciId,
                        Kategori = dto.Kategori,
                        SureTipi = dto.SureTipi,
                        BaslangicTarihi = dto.BaslangicTarihi.Date,
                        BitisTarihi = dto.BitisTarihi.Date,
                        ToplamGun = hesaplananGun,
                        Durum = IzinDurumu.Beklemede,
                        Aciklama = dto.Aciklama,
                        RaporUrl = dto.RaporUrl,
                        OlusturulmaTarihi = DateTime.Now
                    };

                    db.IzinTalepleri.Add(yeniTalep);
                    db.SaveChanges();

                    Logger.Info($"Yeni izin talebi oluşturuldu. Personel ID: {dto.KullaniciId}, Tür: {dto.Kategori}, Gün: {hesaplananGun}");
                    return Ok("İzin talebi başarıyla iletildi.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"İzin talebi oluşturulurken hata meydana geldi. Personel ID: {dto?.KullaniciId}");
                return BadRequest("Talep oluşturulamadı: " + ex.Message);
            }
        }

        [HttpPost]
        [Route("cancel-leave/{leaveId}")]
        public IHttpActionResult CancelLeave(int leaveId)
        {
            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var talep = db.IzinTalepleri.FirstOrDefault(t => t.ID == leaveId);
                    if (talep == null) return NotFound();

                    if (talep.Durum != IzinDurumu.Beklemede)
                        return BadRequest("Onaylanan veya reddedilen izin talepleri iptal edilemez.");

                    TimeSpan gecenSure = DateTime.Now - talep.OlusturulmaTarihi;
                    if (gecenSure.TotalMinutes > 5)
                        return BadRequest("5 dakikalık yasal iptal süreniz dolduğu için bu işlem gerçekleştirilemez.");

                    talep.Durum = IzinDurumu.IptalEdildi;
                    db.SaveChanges();

                    Logger.Info($"Personel 5 dakikalık geri alma süresi içinde iznini bozdu. Talep ID: {leaveId}, Personel ID: {talep.KullaniciId}");
                    return Ok("İzin talebi başarıyla iptal edildi.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"İzin bozma (CancelLeave) operasyonu esnasında veritabanı hatası oluştu. Talep ID: {leaveId}");
                return BadRequest("İzin bozma işlemi sırasında hata oluştu: " + ex.Message);
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
                        if (dto.IzneYansitilsinMi)
                        {
                            if (talep.Kategori == IzinKategorisi.YillikIzin)
                            {
                                if (personel.KalanYillikIzin < talep.ToplamGun)
                                    return BadRequest($"Personelin kalan yıllık izni yetersiz. (Kalan: {personel.KalanYillikIzin} gün)");
                                personel.KalanYillikIzin -= talep.ToplamGun;
                            }
                            else if (talep.Kategori == IzinKategorisi.MazeretIzni)
                            {
                                if (personel.MazeretIzinKotasi < talep.ToplamGun)
                                    return BadRequest($"Personelin mazeret izin kotası yetersiz. (Kalan: {personel.MazeretIzinKotasi} gün)");
                                personel.MazeretIzinKotasi -= talep.ToplamGun;
                            }
                            else if (talep.Kategori == IzinKategorisi.UcretsizIzin)
                            {
                                if (personel.UcretsizIzinKotasi < talep.ToplamGun)
                                    return BadRequest($"Personelin ücretsiz izin kotası yetersiz. (Kalan: {personel.UcretsizIzinKotasi} gün)");
                                personel.UcretsizIzinKotasi -= talep.ToplamGun;
                            }
                        }

                        talep.Durum = IzinDurumu.Onaylandi;
                    }
                    else
                    {
                        talep.Durum = IzinDurumu.Reddedildi;
                    }

                    db.SaveChanges();

                    if (!string.IsNullOrEmpty(personel.Email))
                    {
                        string durumMetni = dto.OnaylandiMi ? "ONAYLANDI" : "REDDEDİLDİ";
                        string tarihAraligi = $"{talep.BaslangicTarihi:dd.MM.yyyy} - {talep.BitisTarihi:dd.MM.yyyy}";
                        SendLeaveStatusEmail(personel.Email, personel.Name, durumMetni, tarihAraligi, talep.Kategori.ToString());
                    }

                    Logger.Info($"Yönetici izin talebini sonuçlandırdı. Talep ID: {dto.TalepId}, Karar: {(dto.OnaylandiMi ? "Onaylandı" : "Reddedildi")}, İzne Yansıtıldı Mı: {dto.IzneYansitilsinMi}, Personel: {personel.Name}");
                    return Ok("İzin talebi sonuçlandırıldı.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Yönetici izin talebine yanıt verirken (RespondLeave) hata oluştu. Talep ID: {dto?.TalepId}");
                return BadRequest("İşlem başarısız: " + ex.Message);
            }
        }

        [HttpPost]
        [Route("bulk-respond-leave")]
        public IHttpActionResult BulkRespondLeave([FromBody] BulkRespondLeaveDto dto)
        {
            if (dto == null || dto.TalepIdleri == null || !dto.TalepIdleri.Any())
                return BadRequest("Lütfen işlem yapılacak en az bir talep seçiniz.");

            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var talepler = db.IzinTalepleri.Where(t => dto.TalepIdleri.Contains(t.ID)).ToList();
                    if (!talepler.Any()) return BadRequest("Seçilen talepler bulunamadı.");

                    int islenenSayi = 0;
                    var bildirilecekPersoneller = new List<(string Email, string Name, string TarihAraligi, string Kategori)>();

                    foreach (var talep in talepler)
                    {
                        if (talep.Durum != IzinDurumu.Beklemede) continue;

                        var personel = db.Users.FirstOrDefault(u => u.ID == talep.KullaniciId);
                        if (personel == null) continue;

                        if (dto.OnaylandiMi)
                        {
                            if (dto.IzneYansitilsinMi)
                            {
                                if (talep.Kategori == IzinKategorisi.YillikIzin)
                                {
                                    if (personel.KalanYillikIzin < talep.ToplamGun) continue;
                                    personel.KalanYillikIzin -= talep.ToplamGun;
                                }
                                else if (talep.Kategori == IzinKategorisi.MazeretIzni)
                                {
                                    if (personel.MazeretIzinKotasi < talep.ToplamGun) continue;
                                    personel.MazeretIzinKotasi -= talep.ToplamGun;
                                }
                                else if (talep.Kategori == IzinKategorisi.UcretsizIzin)
                                {
                                    if (personel.UcretsizIzinKotasi < talep.ToplamGun) continue;
                                    personel.UcretsizIzinKotasi -= talep.ToplamGun;
                                }
                            }

                            talep.Durum = IzinDurumu.Onaylandi;
                        }
                        else
                        {
                            talep.Durum = IzinDurumu.Reddedildi;
                        }

                        if (!string.IsNullOrEmpty(personel.Email))
                        {
                            string tarihAraligi = $"{talep.BaslangicTarihi:dd.MM.yyyy} - {talep.BitisTarihi:dd.MM.yyyy}";
                            bildirilecekPersoneller.Add((personel.Email, personel.Name, tarihAraligi, talep.Kategori.ToString()));
                        }

                        islenenSayi++;
                    }

                    db.SaveChanges();

                    string durumMetni = dto.OnaylandiMi ? "ONAYLANDI" : "REDDEDİLDİ";
                    foreach (var item in bildirilecekPersoneller)
                    {
                        SendLeaveStatusEmail(item.Email, item.Name, durumMetni, item.TarihAraligi, item.Kategori);
                    }

                    Logger.Info($"Toplu izin işlemi yapıldı. İşlenen Talep Sayısı: {islenenSayi}, Karar: {(dto.OnaylandiMi ? "Onaylandı" : "Reddedildi")}, İzne Yansıtıldı Mı: {dto.IzneYansitilsinMi}");
                    return Ok($"{islenenSayi} adet izin talebi başarıyla sonuçlandırıldı.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Toplu izin yanıtlanırken teknik hata oluştu.");
                return BadRequest("Toplu işlem hatası: " + ex.Message);
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
                    if (kullanici == null)
                    {
                        Logger.Warn($"Hatalı giriş denemesi: E-posta bulunamadı. Girilen E-posta: {dto.Email}");
                        return BadRequest("E-posta adresi bulunamadı.");
                    }

                    bool sifreDogruMu = false;
                    try { sifreDogruMu = Crypto.VerifyHashedPassword(kullanici.PasswordHash, dto.Password); }
                    catch { sifreDogruMu = (kullanici.PasswordHash == dto.Password); }

                    if (!sifreDogruMu)
                    {
                        Logger.Warn($"Hatalı giriş denemesi: Şifre uyuşmadı. Hesap E-posta: {dto.Email}");
                        return BadRequest("Hatalı şifre.");
                    }

                    int userId = kullanici.ID;
                    string userName = kullanici.Name;
                    string userEmail = kullanici.Email;
                    string userRole = kullanici.Role;
                    double kalanIzin = kullanici.KalanYillikIzin;
                    double mazeretKota = kullanici.MazeretIzinKotasi;
                    double ucretsizKota = kullanici.UcretsizIzinKotasi;
                    int maxIzinliKota = kullanici.MaxIzinliKota;
                    DateTime? iseGirisTarihi = kullanici.IseGirisTarihi;
                    bool isFirstLogin = kullanici.IsFirstLogin;
                    int? managerID = kullanici.ManagerID;

                    string rolMetni = userRole == "1" ? "Yönetici" : userRole == "2" ? "İnsan Kaynakları" : "Personel";
                    Logger.Info($"Kullanıcı başarıyla sisteme giriş yaptı. Adı: {userName}, Rol: {rolMetni}");

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
                        ucretsizKota = ucretsizKota,
                        maxIzinliKota = maxIzinliKota,
                        iseGirisTarihi = iseGirisTarihi,
                        isFirstLogin = isFirstLogin,
                        ManagerID = managerID
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Login (Oturum açma) işlemi esnasında ağır sistem hatası.");
                return BadRequest("Sistem hatası: " + ex.Message);
            }
        }

        [HttpPost]
        [Route("complete-onboarding/{userId}")]
        public IHttpActionResult CompleteOnboarding(int userId)
        {
            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var kullanici = db.Users.FirstOrDefault(u => u.ID == userId);
                    if (kullanici == null) return NotFound();

                    kullanici.IsFirstLogin = false;
                    db.SaveChanges();
                    return Ok("Onboarding tamamlandı.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Onboarding tamamlanma kaydı esnasında veritabanı güncelleme hatası. Kullanıcı ID: {userId}");
                return BadRequest("Durum güncellenirken hata oluştu: " + ex.Message);
            }
        }

        [HttpPost]
        [Route("send-code")]
        public IHttpActionResult SendCode([FromBody] ForgotPasswordDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Email)) return BadRequest("E-posta adresi eksik.");

            string email = dto.Email.Trim();

            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var kullanici = db.Users.FirstOrDefault(u => u.Email == email);
                    if (kullanici == null) return BadRequest("Bu e-posta adresine kayıtlı kullanıcı bulunamadı.");

                    Random random = new Random();
                    string code = random.Next(100000, 999999).ToString();
                    VerificationCodes[email] = code;

                    string smtpServer = ConfigurationManager.AppSettings["SmtpServer"];
                    int smtpPort = int.Parse(ConfigurationManager.AppSettings["SmtpPort"]);
                    string senderEmail = ConfigurationManager.AppSettings["SenderEmail"];
                    string senderPassword = ConfigurationManager.AppSettings["SenderPassword"];

                    using (var smtpClient = new SmtpClient(smtpServer, smtpPort))
                    {
                        smtpClient.Credentials = new NetworkCredential(senderEmail, senderPassword);
                        smtpClient.EnableSsl = true;

                        using (var mailMessage = new MailMessage())
                        {
                            mailMessage.From = new MailAddress(senderEmail, "İzinPort Sistem");
                            mailMessage.Subject = "İzinPort - Şifre Sıfırlama Kodu";
                            mailMessage.Body = $"<h3>Şifre Yenileme Talebi</h3><p>İzinPort sistemine kayıtlı e-posta hesabınız için oluşturulan 6 haneli doğrulama kodunuz: <b>{code}</b></p>";
                            mailMessage.IsBodyHtml = true;
                            mailMessage.To.Add(email);

                            smtpClient.Send(mailMessage);
                        }
                    }
                    Logger.Info($"Şifre sıfırlama doğrulama kodu başarıyla mail olarak gönderildi. Alıcı: {email}");
                    return Ok();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Şifre yenileme kodu e-posta olarak gönderilirken SMTP hatası oluştu. Hedef: {email}");
                return BadRequest("E-posta gönderim hatası: " + ex.Message);
            }
        }

        [HttpPost]
        [Route("verify-code")]
        public IHttpActionResult VerifyCode([FromBody] ForgotPasswordDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Code))
                return BadRequest("Doğrulama verileri eksik.");

            string email = dto.Email.Trim();
            string code = dto.Code.Trim();

            if (VerificationCodes.TryGetValue(email, out var savedCode) && savedCode == code)
            {
                Logger.Info($"Kullanıcı 6 haneli şifre sıfırlama kodunu başarıyla doğruladı. Hesap: {email}");
                return Ok();
            }

            Logger.Warn($"Şifre sıfırlama kod doğrulaması başarısız. Girilen kod yanlış veya geçersiz. Hesap: {email}");
            return BadRequest("Girdiğiniz kod hatalı veya süresi dolmuş.");
        }

        [HttpPost]
        [Route("reset-password")]
        public IHttpActionResult ResetPassword([FromBody] ForgotPasswordDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Code) || string.IsNullOrEmpty(dto.NewPassword))
                return BadRequest("Gerekli şifre güncelleme verileri eksik.");

            string email = dto.Email.Trim();
            string code = dto.Code.Trim();

            if (!VerificationCodes.TryGetValue(email, out var savedCode) || savedCode != code)
            {
                return BadRequest("Güvenlik aşaması doğrulanamadı.");
            }

            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var kullanici = db.Users.FirstOrDefault(u => u.Email == email);
                    if (kullanici == null) return NotFound();

                    kullanici.PasswordHash = Crypto.HashPassword(dto.NewPassword);
                    db.SaveChanges();

                    VerificationCodes.Remove(email);
                    Logger.Info($"Kullanıcı şifresini başarıyla sıfırladı ve güncelledi. Hesap: {email}");
                    return Ok();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Veritabanında şifre hashlenip güncellenirken kritik hata oluştu. Hesap: {email}");
                return BadRequest("Veritabanı güncelleme hatası: " + ex.Message);
            }
        }

        private static void SendLeaveStatusEmail(string personelEmail, string personelName, string statusText, string dateRange, string categoryText)
        {
            try
            {
                string smtpServer = ConfigurationManager.AppSettings["SmtpServer"];
                int smtpPort = int.Parse(ConfigurationManager.AppSettings["SmtpPort"]);
                string senderEmail = ConfigurationManager.AppSettings["SenderEmail"];
                string senderPassword = ConfigurationManager.AppSettings["SenderPassword"];

                using (var smtpClient = new SmtpClient(smtpServer, smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(senderEmail, senderPassword);
                    smtpClient.EnableSsl = true;

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(senderEmail, "İzinPort Sistem");
                        mailMessage.Subject = $"İzinPort - İzin Talebiniz {statusText}";
                        mailMessage.Body = $@"
                            <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                                <h3 style='color: #0b1329;'>Merhaba {personelName},</h3>
                                <p><b>{dateRange}</b> tarihleri arasındaki <b>{categoryText}</b> talebiniz yönetici tarafından <b>{statusText}</b>.</p>
                                <p>Detayları görüntülemek için sisteme giriş yapabilirsiniz.</p>
                                <br/>
                                <p style='font-size: 12px; color: #777;'>Bu e-posta otomatik olarak gönderilmiştir, lütfen yanıtlamayınız.</p>
                            </div>";
                        mailMessage.IsBodyHtml = true;
                        mailMessage.To.Add(personelEmail);

                        smtpClient.Send(mailMessage);
                    }
                }
                Logger.Info($"İzin durum maili başarıyla gönderildi. Alıcı: {personelEmail}, Durum: {statusText}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"İzin durum maili gönderilirken hata oluştu. Hedef: {personelEmail}");
            }
        }

        // ------------------ DİREKT METİN AYRIŞTIRICI (REGEX) İLE KESİN LOG OKUMA ------------------
        [HttpGet]
        [Route("get-logs")]
        public IHttpActionResult GetLogs()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                List<string> candidatePaths = new List<string>
                {
                    Path.Combine(baseDir, "App_Data", "Logs", "izintakip_log.xml"),
                    System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/Logs/izintakip_log.xml"),
                    Path.Combine(baseDir, "App_Data", "izintakip_log.xml"),
                    System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/izintakip_log.xml")
                };

                string targetFilePath = candidatePaths.FirstOrDefault(p => !string.IsNullOrEmpty(p) && File.Exists(p));

                if (string.IsNullOrEmpty(targetFilePath))
                {
                    return Ok(new List<object>());
                }

                var logList = new List<object>();

                using (var stream = new FileStream(targetFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var matchTime = Regex.Match(line, @"logTime=""([^""]+)""");
                        var matchLevel = Regex.Match(line, @"level=""([^""]+)""");
                        var matchMessage = Regex.Match(line, @"message=""([^""]+)""");
                        var matchException = Regex.Match(line, @"exception=""([^""]+)""");

                        if (matchMessage.Success || matchTime.Success)
                        {
                            string dateStr = matchTime.Success ? matchTime.Groups[1].Value : DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            string levelStr = matchLevel.Success ? matchLevel.Groups[1].Value.ToUpper() : "INFO";
                            string messageStr = matchMessage.Success ? matchMessage.Groups[1].Value : line.Trim();
                            string exceptionStr = matchException.Success ? matchException.Groups[1].Value : "";

                            logList.Add(new
                            {
                                date = dateStr,
                                level = levelStr,
                                message = messageStr,
                                exception = exceptionStr
                            });
                        }
                    }
                }

                logList.Reverse();
                return Ok(logList);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Log verileri okunurken hata oluştu.");
                return BadRequest("Loglar okunamadı: " + ex.Message);
            }
        }
    }
}