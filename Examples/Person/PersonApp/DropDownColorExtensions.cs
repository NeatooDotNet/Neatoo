using BlazorBootstrap;

namespace PersonApp
{
    public static class DropDownColorExtensions
    {
        public static DropdownColor ToDropdownColor(this bool value)
        {
            return value ? DropdownColor.Primary : DropdownColor.Warning;
        }

    }
}
