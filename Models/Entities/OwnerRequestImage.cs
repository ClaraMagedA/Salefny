using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalefnyMVC.Models.Entities
{
    public class OwnerRequestImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ImagePath { get; set; } = string.Empty;

        [Range(1, 6)]
        public int AngleIndex { get; set; }

        [Required]
        public int OwnerRequestId { get; set; }

        [ForeignKey("OwnerRequestId")]
        public virtual OwnerRequest OwnerRequest { get; set; } = null!;
    }
}
