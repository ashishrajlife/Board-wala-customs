using System.ComponentModel.DataAnnotations;

namespace ValousWorld.Web.Models.Entities;

public class Announcement
{
    [Key]
    public int AnnouncementId { get; set; }

    [Required, StringLength(300)]
    public string Text { get; set; } = string.Empty;

    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}