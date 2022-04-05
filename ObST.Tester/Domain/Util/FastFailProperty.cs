using FsCheck;

namespace ObST.Tester.Domain.Util;

sealed class FastFailProperty
{
    public Property Property { get; }
    public bool IsSuccess { get; }
    public bool IsFail => !IsSuccess;
    internal FastFailProperty(bool isSuccess, Property property)
    {
        Property = property;
        IsSuccess = isSuccess;
    }
}
