namespace IzinTakip.API.Models.DTOs
{
    public class CompanyQuotaDto
    {
        public int AdminId { get; set; }
        public int MazeretKota { get; set; }
        public int UcretsizKota { get; set; }
        public int MaxIzinliKota { get; set; }

        // YENİ EKLENEN ALAN:
        public string SirketPolitikasi { get; set; } = string.Empty;
    }
}