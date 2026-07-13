using System.ComponentModel.DataAnnotations;

namespace IzinTakipApp.DTOs
{
    public class KullaniciGirisDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}