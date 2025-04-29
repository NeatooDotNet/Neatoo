using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Neatoo.Blazor;

public class NeatooValidator : ComponentBase
{
    [CascadingParameter]
    public EditContext CurrentEditContext { get; set; }

    protected override void OnInitialized()
    {
        if (CurrentEditContext == null)
        {
            throw new ArgumentNullException(nameof(CurrentEditContext), "NeatooValidator must be used inside an EditForm component.");
        }

        CurrentEditContext.AddNeatooValidation();
    }
}
