namespace DomainModel;

public enum Role
{
	None = 0,
	Create = 1,
	Fetch = 2,
	Update = 3,
	Delete = 4
}

public interface IUser
{
	Role Role { get; set; }
}

public class User : IUser
{
	public Role Role { get; set; }
}
