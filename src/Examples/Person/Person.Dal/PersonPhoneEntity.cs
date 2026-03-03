using System.ComponentModel.DataAnnotations;

namespace Person.Dal;

public class PersonPhoneEntity
{
	[Key]
	public Guid? Id { get; set; }

    [Required]
    public string PhoneNumber { get; set; } = null!;

    [Required]
    public int PhoneType { get; set; }

    public virtual PersonEntity PersonEntity { get; set; }
}
