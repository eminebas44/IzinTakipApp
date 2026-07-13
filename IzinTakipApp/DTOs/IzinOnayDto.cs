using System.ComponentModel.DataAnnotations;
using IzinTakipApp.Enums;

namespace IzinTakipApp.DTOs
{
    public class IzinOnayDto
    {
        [Required]
        public int IzinID { get; set; }

        [Required]
        public IzinDurumu Durum { get; set; }
    }
}