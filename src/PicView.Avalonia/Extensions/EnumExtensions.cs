using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace PicView.Avalonia.Extensions;

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        var type = value.GetType();
        var name = Enum.GetName(type, value);
        if (name is null) return value.ToString();

        var field = type.GetField(name, BindingFlags.Public | BindingFlags.Static);
        var attr  = field?.GetCustomAttribute<DisplayAttribute>(inherit: false);
        return attr?.GetName() ?? name;
    }
}
