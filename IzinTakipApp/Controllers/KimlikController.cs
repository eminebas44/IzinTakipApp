using System.Web.Http;
using IzinTakipApp.DTOs;
using IzinTakipApp.Services;

namespace IzinTakipApp.Controllers
{
    [RoutePrefix("api/identity")]
    public class KimlikController : ApiController
    {
        private readonly IKimlikService _kimlikService;

        public KimlikController()
        {
            _kimlikService = new KimlikService();
        }

        [HttpPost]
        [Route("register")]
        public IHttpActionResult KayitOl([FromBody] KullaniciKayitDto kayitDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var sonuc = _kimlikService.KayitOl(kayitDto);
            if (!sonuc)
            {
                return BadRequest("E-posta adresi zaten kullanımda.");
            }

            return Ok("Kullanıcı kaydı başarıyla tamamlandı.");
        }

        [HttpPost]
        [Route("login")]
        public IHttpActionResult GirişYap([FromBody] KullaniciGirisDto girisDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var kullanici = _kimlikService.GirişYap(girisDto);
            if (kullanici == null)
            {
                return Unauthorized();
            }

            return Ok(new
            {
                ID = kullanici.ID,
                Name = kullanici.Name,
                Email = kullanici.Email,
                Role = kullanici.Role,
                KalanYillikIzin = kullanici.KalanYillikIzin
            });
        }
    }
}