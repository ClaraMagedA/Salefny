using SalefnyMVC.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalefnyMVC.Models.Entities
{
    public class ItemImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ImagePath { get; set; }

        [Range(1, 6)]
        public int AngleIndex { get; set; } // 1 to 6 for the Cubical 6-Angle Inspection System

        [Required]
        public int ItemId { get; set; }

        [ForeignKey("ItemId")]
        public virtual Item Item { get; set; }
    }
}