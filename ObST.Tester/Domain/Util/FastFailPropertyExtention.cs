using ObST.Tester.Domain.Util;
using FsCheck;

namespace ObST.Tester.Domain.Util;

static class FastFailPropertyExtention
{
    public static FastFailProperty ToFastFailProperty(this bool property)
    {
        return new FastFailProperty(property, property.ToProperty());
    }

    public static FastFailProperty FastFailWhen(this bool property, bool condition)
    {
        return new FastFailProperty(property || !condition, property.When(condition));
    }

    public static FastFailProperty And(this FastFailProperty left, FastFailProperty right)
    {
        return new FastFailProperty(left.IsSuccess && right.IsSuccess, left.Property.And(right.Property));
    }
    public static FastFailProperty Or(this FastFailProperty left, FastFailProperty right)
    {
        return new FastFailProperty(left.IsSuccess || right.IsSuccess, left.Property.Or(right.Property));
    }

    public static FastFailProperty Label(this FastFailProperty property, string label)
    {
        return new FastFailProperty(property.IsSuccess, property.Property.Label(label));
    }

    public static FastFailProperty FastFailLabel(this bool property, string label)
    {
        return new FastFailProperty(property, property.Label(label));
    }

}