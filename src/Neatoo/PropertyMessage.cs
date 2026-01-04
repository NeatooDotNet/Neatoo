
namespace Neatoo;

public interface IPropertyMessage
{
    IValidateProperty Property { get; set; }
    string Message { get; set; }
}

public record PropertyMessage : IPropertyMessage
{
    public IValidateProperty Property { get; set; }
    public string Message { get; set; }
    public PropertyMessage(IValidateProperty property, string message)
    {
        this.Property = property;
        this.Message = message;
    }
}


