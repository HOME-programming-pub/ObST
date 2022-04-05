namespace ObST.Core.Util;

public static class IdMappingExtention
{
    public const string ID_MAPPING = ":@id";

    public static string AddIdMapping(this string r)
    {
        return r + ID_MAPPING;
    }

    public static bool IsIdMapping(this string s)
    {
        return s.EndsWith(ID_MAPPING);
    }
}

