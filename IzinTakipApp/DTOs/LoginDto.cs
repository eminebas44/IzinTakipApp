using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json; // <-- Bu kütüphaneyi ekledik

namespace IzinTakip.API.Models.DTOs
{
    public class LoginDto
    {
        [Required]
        [JsonProperty("Email")] // Gelen JSON'da "Email" veya "email" aranacağını kesinleştirir
        public string Email { get; set; }

        [Required]
        [JsonProperty("Password")] // Gelen JSON'da "Password" veya "password" aranacağını kesinleştirir
        public string Password { get; set; }
    }
}