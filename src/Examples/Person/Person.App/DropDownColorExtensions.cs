using BlazorBootstrap;

namespace Person.App
{
    public static class DropDownColorExtensions
    {
        public static DropdownColor ToDropdownColor(this bool value)
        {
            return value ? DropdownColor.Primary : DropdownColor.Warning;
        }

    }
}
