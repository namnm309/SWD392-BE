using System;
using System.ComponentModel.DataAnnotations.Schema;
using DomainLayer.Enum;

namespace DomainLayer.Entities
{
    [Table("tbl_reports")]
    public class Report : BaseEntity
    {
        public string Title { get; set; } = null!;
        
        public string Description { get; set; } = null!;
        
        public ReportType Type { get; set; } // Lab, Equipment
        
        public string? ImageUrl { get; set; }
        
        public DateTime ReportedDate { get; set; }
        
        public ReportStatus Status { get; set; } = ReportStatus.Open;
        
        public Guid ReporterId { get; set; }
        
        [ForeignKey(nameof(ReporterId))]
        public Users Reporter { get; set; } = null!;
        
        public string? AdminResponse { get; set; }
        
        public DateTime? ResolvedAt { get; set; }
        
        public Guid? ResolvedBy { get; set; }
        
        [ForeignKey(nameof(ResolvedBy))]
        public Users? ResolvedByUser { get; set; }
    }
}
