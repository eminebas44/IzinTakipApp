using System.Web.Http;
using IzinTakipApp.DTOs;
using IzinTakipApp.Services;

namespace IzinTakipApp.Controllers
{
    [RoutePrefix("api/izinler")]
    public class IzinTalebiController : ApiController
    {
        private readonly IIzinService _izinService;

        public IzinTalebiController()
        {
            _izinService = new IzinService();
        }

        [HttpPost]
        [Route("talep")]
        public IHttpActionResult IzinTalepEt([FromBody] IzinTalebiDto talepDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var sonuc = _izinService.IzinTalepEt(talepDto);
            if (!sonuc)
            {
                return BadRequest("İzin talebi oluşturulamadı. Yıllık izin bakiye yetersizliği veya geçersiz kullanıcı.");
            }

            return Ok("İzin talebi başarıyla alındı, onay bekliyor.");
        }

        [HttpGet]
        [Route("personel/{kullaniciId}")]
        public IHttpActionResult PersonelIzinleriniGetir(int kullaniciId)
        {
            var izinler = _izinService.PersonelIzinleriniGetir(kullaniciId);
            return Ok(izinler);
        }

        [HttpGet]
        [Route("yonetici/{yoneticiId}/onay-listesi")]
        public IHttpActionResult YoneticiOnayListesiniGetir(int yoneticiId)
        {
            var onayListesi = _izinService.YoneticiOnayListesiniGetir(yoneticiId);
            return Ok(onayListesi);
        }

        [HttpPost]
        [Route("onayla-reddet")]
        public IHttpActionResult IzinDurumunuGuncelle([FromBody] IzinOnayDto onayDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var sonuc = _izinService.IzinDurumunuGuncelle(onayDto);
            if (!sonuc)
            {
                return BadRequest("İşlem gerçekleştirilemedi. Talep bulunamadı veya zaten işleme alınmış.");
            }

            return Ok("İzin durumu başarıyla güncellendi.");
        }
    }
}