
namespace Neatoo;

public interface IPropertyMessage
{
    IProperty Property { get; set; }
    string Message { get; set; }
}

public record PropertyMessage : IPropertyMessage
{
    public IProperty Property { get; set; }
    public string Message { get; set; }
    public PropertyMessage(IProperty property, string message)
    {
        Property = property;
        Message = message;
    }
}


