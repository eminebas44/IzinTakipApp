using System;
using System.Web.Http;
using System.Web.Helpers;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Mail;
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

    [RoutePrefix("api/auth")]
    public class AuthController : ApiController
    {
        // GÜNCELLEME: Sınıf içerisindeki tüm operasyonları izleyecek statik NLog motoru tanımı
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
                    var personeller = db.Users.Where(u => u.Role == "0" && u.ManagerID == adminId).ToList();
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
                            ucretsizKota = p.UcretsizIzinKotasi,
                            iseBaslamaTarihi = p.IseGirisTarihi.ToString("yyyy-MM-dd")
                        });
                    }
                    return Ok(liste);
                }
            }
            catch (Exception ex)
            {
                // GÜNCELLEME: Teknik hata detayı XML günlüğüne yazılır
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
                    var tumTalepler = db.IzinTalepleri.Where(t => t.Durum != IzinDurumu.Beklemede).ToList();
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
                                durum = (int)talep.Durum,
                                aciklama = talep.Aciklama
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
                            olusturulmaTarihi = t.OlusturulmaTarihi.ToString("yyyy-MM-ddTHH:mm:ss")
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
                        UcretsizIzinKotasi = 15
                    };

                    db.Users.Add(yeniKullanici);
                    db.SaveChanges();

                    // GÜNCELLEME: Başarılı yönetici kaydı XML dosyasına loglanır
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

                    Kullanici yeniPersonel = new Kullanici
                    {
                        Name = dto.AdminName,
                        Email = dto.Email,
                        PasswordHash = Crypto.HashPassword(dto.Password),
                        Role = "0",
                        IseGirisTarihi = iseGiris,
                        KalanYillikIzin = tanimlananIzinGunu,
                        MazeretIzinKotasi = adminUser != null ? adminUser.MazeretIzinKotasi : 5,
                        UcretsizIzinKotasi = adminUser != null ? adminUser.UcretsizIzinKotasi : 15,
                        ManagerID = dto.ManagerID
                    };

                    db.Users.Add(yeniPersonel);
                    db.SaveChanges();

                    // GÜNCELLEME: Başarılı personel ekleme kaydı loglanır
                    Logger.Info($"Şirkete yeni personel tanımlandı. Adı: {dto.AdminName}, E-posta: {dto.Email}, Yönetici ID: {dto.ManagerID}");
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
        public IHttpActionResult UpdateCompanyQuotas([FromBody] Newtonsoft.Json.Linq.JObject data)
        {
            try
            {
                int adminId = data["adminId"] != null ? data["adminId"].ToObject<int>() : 0;
                int mazeretKota = data["mazeretKota"] != null ? data["mazeretKota"].ToObject<int>() : 5;
                int ucretsizKota = data["ucretsizKota"] != null ? data["ucretsizKota"].ToObject<int>() : 15;
                int maxIzinliKota = data["maxIzinliKota"] != null ? data["maxIzinliKota"].ToObject<int>() : 3;

                using (AppDbContext db = new AppDbContext())
                {
                    var adminUser = db.Users.FirstOrDefault(u => u.ID == adminId);
                    if (adminUser == null) return BadRequest("Yönetici bulunamadı.");

                    adminUser.MazeretIzinKotasi = mazeretKota;
                    adminUser.UcretsizIzinKotasi = ucretsizKota;

                    var bagliPersoneller = db.Users.Where(u => u.ManagerID == adminId && u.Role == "0").ToList();
                    foreach (var personel in bagliPersoneller)
                    {
                        personel.MazeretIzinKotasi = mazeretKota;
                        personel.UcretsizIzinKotasi = ucretsizKota;
                    }

                    db.SaveChanges();
                    Logger.Info($"Yönetici şirket genel kotalarını güncelledi. Yönetici ID: {adminId}, Mazeret: {mazeretKota}, Ücretsiz: {ucretsizKota}, Maks Aynı Gün: {maxIzinliKota}");
                    return Ok("Şirket kotaları başarıyla güncellendi.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Şirket kotaları güncellenirken genel hata oluştu.");
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

                    double hesaplananGun = (dto.BitisTarihi.Date - dto.BaslangicTarihi.Date).TotalDays + 1;

                    int maxIzinliLimiti = 3;

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
                        return BadRequest("Yıllık izin limitinizi aşıyorsunuz.");

                    if (dto.Kategori == IzinKategorisi.MazeretIzni && hesaplananGun > personel.MazeretIzinKotasi)
                        return BadRequest("Mazeret izni limitinizi aşıyorsunuz.");

                    if (dto.Kategori == IzinKategorisi.UcretsizIzin && hesaplananGun > personel.UcretsizIzinKotasi)
                        return BadRequest("Ücretsiz izin limitinizi aşıyorsunuz.");

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

                    db.IzinTalepleri.Remove(talep);
                    db.SaveChanges();

                    Logger.Info($"Personel 5 dakikalık geri alma süresi içinde iznini bozdu ve sildi. Talep ID: {leaveId}, İzin Sahibi Personel ID: {talep.KullaniciId}");
                    return Ok("İzin talebi başarıyla geri çekildi ve silindi.");
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

                    Logger.Info($"Yönetici izin talebini sonuçlandırdı. Talep ID: {dto.TalepId}, Karar: {(dto.OnaylandiMi ? "Onaylandı" : "Reddedildi")}, İzin Sahibi Personel: {personel.Name}");
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
                    DateTime? iseGirisTarihi = kullanici.IseGirisTarihi;
                    bool isFirstLogin = kullanici.IsFirstLogin;

                    Logger.Info($"Kullanıcı başarıyla sisteme giriş yaptı. Adı: {userName}, Rol: {(userRole == "1" ? "Yönetici" : "Personel")}");

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
                        iseGirisTarihi = iseGirisTarihi,
                        isFirstLogin = isFirstLogin
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
    }
}