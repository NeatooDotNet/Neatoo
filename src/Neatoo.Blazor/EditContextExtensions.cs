using Microsoft.AspNetCore.Components.Forms;

namespace Neatoo.Blazor
{
    public static class EditContextExtensions
    {
        public static EditContext AddNeatooValidation(this EditContext editContext)
        {
            if (editContext == null)
            {
                throw new ArgumentNullException(nameof(editContext));
            }

            var messages = new ValidationMessageStore(editContext);

            editContext.OnValidationRequested +=
                (sender, _) => ValidateModel((EditContext)sender, messages);

            editContext.OnFieldChanged +=
                (_, eventArgs) => ValidateField(editContext, messages, eventArgs.FieldIdentifier);

            return editContext;
        }

        private static void ValidateModel(EditContext editContext, ValidationMessageStore messages)
        {
            if (editContext.Model is IEntityBase model)
            {
                // Transfer broken rules of severity Error to the ValidationMessageStore
                messages.Clear();
                foreach (var ruleMessage in model.PropertyMessages)
                {
                    messages.Add(new FieldIdentifier(ruleMessage.Property, "StringValue"), ruleMessage.Message);
                }
            }

            editContext.NotifyValidationStateChanged();
        }

        private static void ValidateField(EditContext editContext, ValidationMessageStore messages, in FieldIdentifier fieldIdentifier)
        {
            if (fieldIdentifier.Model is IEntityProperty model)
            {
                messages.Clear(fieldIdentifier);
                foreach (var ruleMessage in model.PropertyMessages.Select(rm => rm.Message).Distinct())
                {
                    messages.Add(fieldIdentifier, ruleMessage);
                }
            }

            editContext.NotifyValidationStateChanged();
        }
    }
}
