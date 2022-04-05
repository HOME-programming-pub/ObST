namespace ObST.Core.Models;
public class QuickCheckConfiguration
{
    /// <summary>
    /// Do not try to shrink failed test sequences
    /// </summary>
    public bool DoNotShrink { get; set; }
    public int MaxNbOfTest { get; set; }
    public int StartSize { get; set; }
    public int EndSize { get; set; }

    public Seed? Seed { get; set; }
}

public class Seed
{
    public ulong Seed1 { get; set; }
    public ulong Seed2 { get; set; }
    public int Size { get; set; }
}
