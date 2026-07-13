using System;
using System.ComponentModel.DataAnnotations;

namespace IzinTakipApp.DTOs
{
    public class KullaniciKayitDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [Required]
        public string Role { get; set; }

        public DateTime IseGirisTarihi { get; set; }

        public int? ManagerID { get; set; }
    }
}