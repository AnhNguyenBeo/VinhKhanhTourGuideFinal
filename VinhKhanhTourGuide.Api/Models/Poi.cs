using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace VinhKhanhTourGuide.Api.Models
{
    public class Poi
    {
        [Key]
        public string Id { get; set; }
        public string Name { get; set; }
        public string ImageName { get; set; }

        [NotMapped]
        public string ImageUrl { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Description_VN { get; set; }
        public int GeofenceRadius { get; set; }
        public int Priority { get; set; }
    }
}