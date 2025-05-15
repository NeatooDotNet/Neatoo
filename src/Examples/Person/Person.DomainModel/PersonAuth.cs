using Neatoo.RemoteFactory;

namespace DomainModel;

public interface IPersonAuth
{
	[Authorize(AuthorizeOperation.Read | AuthorizeOperation.Write)]
	public bool HasAccess();
    [Authorize(AuthorizeOperation.Create)]
    bool HasCreate();
    [Authorize(AuthorizeOperation.Fetch)]
    bool HasFetch();
    [Authorize(AuthorizeOperation.Update)]
    bool HasUpdate();
    [Authorize(AuthorizeOperation.Insert)]
    bool HasInsert();
    [Authorize(AuthorizeOperation.Delete)]
    bool HasDelete();
}

internal class PersonAuth : IPersonAuth
{
	public PersonAuth(IUser user)
	{
		this.User = user;
	}

	public IUser User { get; }

	public bool HasAccess()
	{
		if (this.User.Role > Role.None)
		{
			return true;
		}
		return false;
	}
    public bool HasCreate()
    {
        if (this.User.Role >= Role.Create)
        {
            return true;
        }
        return false;
    }

    public bool HasFetch()
    {
        if (this.User.Role >= Role.Fetch)
        {
            return true;
        }
        return false;
    }

    public bool HasInsert()
    {
        if (this.User.Role >= Role.Update)
        {
            return true;
        }
        return false;
    }


    public bool HasUpdate()
    {
        if (this.User.Role >= Role.Update)
        {
            return true;
        }
        return false;
    }

    public bool HasDelete()
    {
        if (this.User.Role >= Role.Delete)
        {
            return true;
        }
        return false;
    }
}
