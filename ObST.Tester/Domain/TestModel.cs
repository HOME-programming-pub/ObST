using ObST.Tester.Core.Models;
using System.Text;

namespace ObST.Tester.Domain;

class TestModel
{

    private readonly Dictionary<string, ISet<IdModel>> _state;

    public TestModel()
    {
        _state = new Dictionary<string, ISet<IdModel>>();
    }

    public TestModel(TestModel model)
    {
        _state = new Dictionary<string, ISet<IdModel>>();

        //copy, keep old references
        var ids = model._state.Values
            .SelectMany(ids => ids.Select(id => new IdModel(id)))
            .ToList();

        foreach (var id in ids)
        {
            foreach (var idModel in id.Children.SelectMany(c => c.Value).ToList())
            {
                id.Children[idModel.Mapping].Remove(idModel);

                var newModel = ids.Single(model => model.Id == idModel.Id);

                id.Children[idModel.Mapping].Add(newModel);
            }

            foreach (var idModel in id.Parents.Values.ToList())
            {
                var newModel = ids.Single(model => model.Id == idModel.Id);

                id.Parents[idModel.Mapping] = newModel;
            }

            if (!_state.TryGetValue(id.Mapping, out var list))
            {
                list = new HashSet<IdModel>();
                _state.Add(id.Mapping, list);
            }

            list.Add(id);
        }
    }

    public virtual void Add(IEnumerable<(string key, string value)> values)
    {
        var newId = ToIdModel(values);

        while (newId != null)
        {
            IdModel? model = null;

            if (!_state.TryGetValue(newId.Mapping, out var ids))
            {
                ids = new HashSet<IdModel>();
                _state.Add(newId.Mapping, ids);
            }
            else
                model = ids.SingleOrDefault(id => newId.HasSameValueAndParent(id));

            var parent = newId.Parents.FirstOrDefault().Value;

            if (model == null)
            {
                model = newId;
                ids.Add(model);
            }
            else
            {
                if (newId.Children.Any())
                {
                    var child = newId.Children.First().Value.First();
                    child.RemoveParent(newId.Mapping);
                    model.LinkChild(child);
                }

                if (parent != null)
                {
                    newId.RemoveParent(parent.Mapping);
                    parent.LinkChild(model);
                }
            }

            newId = parent;
        }
    }

    private IdModel? ToIdModel(IEnumerable<(string key, string value)> values)
    {
        IdModel? last = null;

        foreach (var (key, value) in values)
        {
            var model = new IdModel(Guid.NewGuid().ToString(), key, value);

            if (last != null)
                last.LinkChild(model);

            last = model;
        }

        return last;
    }

    public void Delete(IEnumerable<(string key, string value)> values)
    {
        IdModel? lastModel = null;

        foreach (var (key, value) in values)
        {
            IdModel? model = null;

            if (lastModel != null)
                model = lastModel.Children[key].SingleOrDefault(i => i.Value == value);
            else if (_state.TryGetValue(key, out var ids))
            {
                model = ids.SingleOrDefault(id => id.Value == value);
            }

            //id not found
            if (model == null)
                return;

            lastModel = model;
        }

        if (lastModel == null)
            return;

        lastModel.IsDeleted = true;
        lastModel.DeleteChildren();
    }

    public IEnumerable<object> Get(IEnumerable<(string key, string value)> path, string key, bool includeDeleted = false)
    {
        var keys = GetKeys(path, key);

        if (includeDeleted)
            return keys.Select(k => k.value);
        else
            return keys.Where(k => !k.deleted).Select(k => k.value);
    }

    public IEnumerable<string> GetDeleted(IEnumerable<(string key, string value)> path, string key)
    {
        var keys = GetKeys(path, key);

        return keys.Where(k => k.deleted).Select(k => k.value);
    }

    private IEnumerable<(string value, bool deleted)> GetKeys(IEnumerable<(string key, string value)> path, string key)
    {
        IdModel? lastModel = null;

        foreach (var (k, v) in path)
        {
            IdModel? model = null;

            if (lastModel != null)
                model = lastModel.Children[key].SingleOrDefault(i => i.Value == v);
            else if (_state.TryGetValue(k, out var ids))
            {
                model = ids.SingleOrDefault(id => id.Value == v);
            }

            if (model == null)
                return Enumerable.Empty<(string, bool)>();

            lastModel = model;
        }

        if (lastModel == null)
        {
            if (_state.TryGetValue(key, out var ids))
                return ids.Select(id => (id.Value, id.IsDeleted));
            else
                return Enumerable.Empty<(string, bool)>();
        }

        return lastModel.Children[key]
            .Select(i => (i.Value, i.IsDeleted));
    }

    public IList<Dictionary<string, (string? value, bool deleted)>> GetMatchingIdSubset<T>(IList<T> constraints) where T : PropertyConstraint<T>
    {
        var dicts = new List<List<Dictionary<string, (string?, bool)>>>();

        foreach (var c in constraints)
        {
            if (dicts.Any(combination => combination.Any(d => d.ContainsKey(c.Mapping))))
                continue;

            if (!_state.TryGetValue(c.Mapping, out var ids))
            {
                if (c.IsRequired)
                    return new List<Dictionary<string, (string?, bool)>>();
                else
                {
                    dicts.Add(new List<Dictionary<string, (string?, bool)>>
                        {
                            new Dictionary<string, (string?, bool)>
                            {
                                {c.Mapping, (null, false) }
                            }
                        });
                    continue;
                }
            }

            var cRes = GetAllIdCombinations(c, ids, Enumerable.Empty<string>()).ToList();

            if (!cRes.Any())
                return cRes;

            dicts.Add(cRes);
        }

        var permutations = CartesianProduct(dicts);

        var res = new List<Dictionary<string, (string? value, bool deleted)>>();

        foreach (var p in permutations)
            if (TryMergeDictionaries(p, out var d))
                res.Add(d);

        return res;
    }

    private IEnumerable<Dictionary<string, (string? value, bool isDeleted)>> GetAllIdCombinations<T>(PropertyConstraint<T> constraint, ISet<IdModel> ids, IEnumerable<string> analyzed) where T : PropertyConstraint<T>
    {
        analyzed = analyzed.Append(constraint.Mapping);

        foreach (var id in ids)
        {
            var didNotMatch = false;

            var childIds = new List<List<Dictionary<string, (string?, bool)>>>();

            //has all constrains
            foreach (var child in constraint.Children.SelectMany(c => c.Value))
            {

                //allready mapped
                if (analyzed.Contains(child.Mapping))
                    continue;

                if (!id.Children.TryGetValue(child.Mapping, out var matches))
                {
                    if (child.IsRequired)
                    {
                        didNotMatch = true;
                        break;
                    }
                    else
                    {
                        childIds.Add(new List<Dictionary<string, (string?, bool)>>
                            {
                                new Dictionary<string, (string?, bool)>
                                {
                                    {child.Mapping, (null, false) }
                                }
                            });
                        continue;
                    }

                }

                childIds.Add(GetAllIdCombinations(child, matches, analyzed).ToList());
            }

            if (didNotMatch)
                continue;

            var permutations = CartesianProduct(childIds);

            var self = new Dictionary<string, (string? value, bool isDeleted)>
                {
                    { id.Mapping, (id.Value, id.IsDeleted) }
                };

            foreach (var p in permutations)
                if (TryMergeDictionaries(p.Append(self), out var res))
                    yield return res;
        }

        if (!constraint.IsRequired && constraint.Children.SelectMany(c => c.Value).All(c => !c.IsRequired))
            yield return new Dictionary<string, (string? value, bool isDeleted)>
                {
                    {constraint.Mapping, (null, false) }
                };
    }

    private bool TryMergeDictionaries(
        IEnumerable<Dictionary<string, (string? value, bool isDeleted)>> dicts,
        out Dictionary<string, (string? value, bool isDeleted)> res)
    {
        res = new Dictionary<string, (string? value, bool isDeleted)>();

        foreach (var dict in dicts)
        {
            foreach (var e in dict)
            {
                if (res.TryGetValue(e.Key, out var entry))
                {
                    if (entry != e.Value)
                        return false;
                }
                else
                    res.Add(e.Key, e.Value);
            }
        }

        return true;
    }

    private List<List<T>> CartesianProduct<T>(List<List<T>> list)
    {
        var count = list.Aggregate(1, (i, next) => i * next.Count);

        var res = new List<List<T>>();

        for (int i = 0; i < count; ++i)
        {
            var tmp = new List<T>();

            int j = 1;
            list.ForEach(inner =>
            {
                j *= inner.Count;
                tmp.Add(inner[i / (count / j) % inner.Count]);
            });
            res.Add(tmp);
        }
        return res;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.Append("Testmodel: ");

        foreach (var kvp in _state)
        {
            if (sb.Length > 0)
                sb.Append(", ");

            sb.Append("{");

            sb.Append(kvp.Key);
            sb.Append(":");
            sb.Append("{");

            var existing = kvp.Value.Where(v => !v.IsDeleted).Select(v => v.Value);
            var deleted = kvp.Value.Where(v => v.IsDeleted).Select(v => v.Value);

            sb.Append("E: ");
            sb.Append(string.Join(", ", existing));
            sb.Append(", D: ");
            sb.Append(string.Join(", ", deleted));

            sb.Append("}");
            sb.Append("}");
        }

        return sb.ToString();
    }

    private class IdModel : PropertyConstraint<IdModel>
    {
        /// <summary>
        /// Id is requiered to resolve references for deep copies
        /// </summary>
        public string Id { get; }
        public bool IsDeleted { get; set; }
        public string Value { get; }

        public IdModel(string id, string mapping, string value) : base(mapping)
        {
            Id = id;
            Value = value;
        }

        public IdModel(IdModel source) : base(source.Mapping)
        {
            Id = source.Id;
            IsDeleted = source.IsDeleted;
            Value = source.Value;
            Parents = new Dictionary<string, IdModel>(source.Parents);
            Children = source.Children.ToDictionary(c => c.Key, c => new HashSet<IdModel>(c.Value));
        }

        public void DeleteChildren()
        {
            foreach (var child in Children.SelectMany(c => c.Value))
            {
                child.IsDeleted = true;
                //NOTE: We must ensure that children dependencies can never be circular
                child.DeleteChildren();
            }
        }

        public bool HasSameValueAndParent(IdModel other)
        {
            if (Value != other.Value || Mapping != other.Mapping)
                return false;

            if (!Parents.Any())
                return true;

            var parent = Parents.First().Value;

            if (!other.Parents.TryGetValue(parent.Mapping, out var oParent))
                return true;
            else
                return parent.HasSameValueAndParent(oParent);
        }
    }

}
