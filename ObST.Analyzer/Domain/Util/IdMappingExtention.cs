using ObST.Analyzer.Core.Models;

namespace ObST.Analyzer.Domain.Util;

public static class IdMappingExtention
{
    public static string AsIdMapping(this ResourceClass r)
    {
        return r + ObST.Core.Util.IdMappingExtention.ID_MAPPING;
    }
}

