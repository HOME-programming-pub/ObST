namespace ObST.Tester.Core.Models;

abstract class PropertyConstraint<T> where T : PropertyConstraint<T>
{
    public string Mapping { get; }

    public bool IsRequired { get; set; }

    /// <summary>
    /// One parent per mapping
    /// </summary>
    public Dictionary<string, T> Parents { get; set; } = new Dictionary<string, T>();
    public Dictionary<string, HashSet<T>> Children { get; set; } = new Dictionary<string, HashSet<T>>();

    public PropertyConstraint(string mapping)
    {
        Mapping = mapping;
    }

    /// <summary>
    /// Adds the parent and child relation to both objects
    /// </summary>
    /// <param name="child">The child</param>
    public void LinkChild(T child)
    {
        if (!Children.TryGetValue(child.Mapping, out var children))
        {
            children = new HashSet<T>();
            Children.Add(child.Mapping, children);
        }

        children.Add(child);
        child.Parents[Mapping] = (T)this;
    }

    /// <summary>
    /// Removes the parent and child realtion from both objects
    /// </summary>
    public void RemoveParent(string mapping)
    {
        Parents[mapping]?.Children[Mapping].Remove((T)this);
        Parents.Remove(mapping);
    }

    /// <summary>
    /// Removes the parent and child realtion from both objects
    /// </summary>
    public void RemoveAllRelations()
    {
        foreach (var p in Parents.Keys)
            RemoveParent(p);

        foreach (var kvp in Children)
            foreach (var c in kvp.Value)
                c.RemoveParent(kvp.Key);
    }
}

class BareConstraint : PropertyConstraint<BareConstraint>
{
    /// <summary>
    /// Create a copy of the constraints
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="constraints"></param>
    /// <returns></returns>
    public static IList<BareConstraint> CreateCopy<T>(IList<T> constraints) where T : PropertyConstraint<T>
    {
        var dict = constraints.ToDictionary(origin => origin.Mapping, origin => (origin, bare: new BareConstraint(origin.Mapping)));

        var res = new List<BareConstraint>();

        foreach (var (mapping, (origin, bare)) in dict)
        {
            foreach (var p in origin.Parents)
                dict[p.Key].bare.LinkChild(bare);

            res.Add(bare);
        }

        return res;
    }

    private BareConstraint(string mapping) : base(mapping)
    {
    }
}
