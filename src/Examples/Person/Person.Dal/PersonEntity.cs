using System.ComponentModel.DataAnnotations;

namespace Person.Dal;

public class PersonEntity
{
    [Key]
    public Guid? Id { get; set; }

    [Required]
	public string FirstName { get; set; } = null!;
    [Required]
	public string LastName { get; set; } = null!;
    public string? Email { get; set; }
	public string? Phone { get; set; }
	public string? Notes { get; set; }
    public virtual ICollection<PersonPhoneEntity> Phones { get; } = [];
}
