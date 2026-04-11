using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhTourGuide.Api.Models
{
    [Table("TourPoi")]
    public class TourPoi
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TourId { get; set; }

        [Required]
        [StringLength(450)]
        public string PoiId { get; set; }

        [Required]
        public int SortOrder { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }
    }
}