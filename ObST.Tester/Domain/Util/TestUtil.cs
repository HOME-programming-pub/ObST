using ObST.Core.Models;
using ObST.Core.Util;
using ObST.Tester.Domain.Operation;
using FsCheck;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace ObST.Tester.Domain.Util;

static class TestUtil
{
    public static TestModel SearchForIds(this TestModel model, JToken obj, NJsonSchema.JsonSchema objSchema, IEnumerable<(string mapping, string value)> ids)
    {
        if (obj.Type == JTokenType.Array)
            foreach (var entry in obj)
            {
                model.SearchForIds(entry, objSchema.Item, ids);
            }
        else if (obj.Type == JTokenType.Object)
        {

            var title = objSchema.Title.StartsWith('>') || objSchema.Title.StartsWith('<') ? objSchema.Title[1..] : objSchema.Title;

            //get id of current object
            var primaryObjectIdentifier = title.AddIdMapping();
            var idPropertyKey = objSchema.Properties.FirstOrDefault(p => p.Value.Title == primaryObjectIdentifier).Key;

            string? id;

            if (idPropertyKey != null && ((JObject)obj).TryGetValue(idPropertyKey, out var idToken))
                id = Convert.ToString(idToken, CultureInfo.InvariantCulture);
            else
                id = null;

            switch (objSchema.Title[0])
            {
                case '>':
                    if (id != null)
                    {
                        var newIds = Enumerable.Repeat((primaryObjectIdentifier, id), 1);

                        if (ids.Any())
                            model.Add(newIds.Append(ids.Last()));
                        else
                            model.Add(ids);

                        ids = newIds;
                    }
                    else
                        ids = Enumerable.Empty<(string, string)>();
                    break;
                case '<':
                default:
                    if (id != null)
                    {
                        if (ids.Any() && ids.Last().mapping == primaryObjectIdentifier && ids.Last().value == id)
                            ids = ids.SkipLast(1);

                        ids = ids.Append((primaryObjectIdentifier, id));
                        model.Add(ids);
                    }
                    break;
            }


            foreach (var prop in (JObject)obj)
            {
                if (prop.Key == idPropertyKey)
                    continue;

                if (objSchema.AllowAdditionalProperties)
                {
                    if (objSchema.Properties.ContainsKey(prop.Key))
                        model.SearchForIds(prop.Value, objSchema.Properties[prop.Key], ids);
                    //else we have no information to continue
                }
                else
                    model.SearchForIds(prop.Value, objSchema.Properties[prop.Key], ids);
            }
        }
        else if (obj.Type != JTokenType.Null)
        {
            var mapping = objSchema.Title;

            //protected mapping suffix to indicate ids
            if (mapping.IsIdMapping())
            {
                var id = Convert.ToString(obj, CultureInfo.InvariantCulture)!;

                switch (mapping[0])
                {
                    case '>':
                        if (ids.Any())
                            ids = ids.TakeLast(1);
                        model.Add(ids.Prepend((mapping[1..], id)));
                        break;
                    case '<':
                        model.Add(ids.Append((mapping[1..], id)));
                        break;
                    default:
                        ids = Enumerable.Empty<(string, string)>();
                        model.Add(ids.Append((mapping, id)));
                        break;
                }
            }
        }

        return model;
    }

    public static TestModel ExtractIdsFromUri(this TestModel model, Uri uri, TestConfiguration config)
    {
        var values = config.GetIdsOfPath(uri.AbsolutePath, false);

        model.Add(values);

        return model;
    }

    public static void AddOperation(this IList<(int, Gen<TestOperation>, TestOperation)> list, int weight, Func<TestOperation> gen)
    {
        list.Add((weight, Gen.Fresh(gen), gen()));
    }
}
