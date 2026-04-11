using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhTourGuide.WebAdmin.Models
{
    [Table("AudioAsset")]
    public class AudioAsset
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
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; }

        public int DurationSeconds { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}