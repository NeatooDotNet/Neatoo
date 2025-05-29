using Neatoo.RemoteFactory;

namespace DomainModel;

public interface IPersonAuth
{
	[AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
	public bool HasAccess();
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool HasCreate();
    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool HasFetch();
    [AuthorizeFactory(AuthorizeFactoryOperation.Update)]
    bool HasUpdate();
    [AuthorizeFactory(AuthorizeFactoryOperation.Insert)]
    bool HasInsert();
    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
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
