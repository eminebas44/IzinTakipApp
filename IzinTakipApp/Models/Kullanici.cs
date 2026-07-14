using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IzinTakipApp.Models
{
    [Table("Users")]
    public class Kullanici
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        [StringLength(50)]
        public string Role { get; set; }

        public DateTime IseGirisTarihi { get; set; }

        public double KalanYillikIzin { get; set; }

        public double MazeretIzinKotasi { get; set; }

        public double UcretsizIzinKotasi { get; set; }

        public bool IsFirstLogin { get; set; } = true;

        public int? ManagerID { get; set; }

        [ForeignKey("ManagerID")]
        public virtual Kullanici Manager { get; set; }
    }
}