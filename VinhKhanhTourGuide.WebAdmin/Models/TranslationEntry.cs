using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhTourGuide.WebAdmin.Models
{
    [Table("TranslationEntry")]
    public class TranslationEntry
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string PoiId { get; set; }

        [Required]
        [StringLength(20)]
        public string LanguageCode { get; set; }

        [Required]
        public string TranslatedText { get; set; }

        [Required]
        [StringLength(20)]
        public string Source { get; set; } = "Manual";

        public bool IsApproved { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}