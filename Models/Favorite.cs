using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Propertease.Models;

public class Favorite
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int PropertyId { get; set; }
    public DateTime DateAdded { get; set; }

    // Navigation properties
    public virtual User User { get; set; }
    public virtual Properties Property { get; set; }
}