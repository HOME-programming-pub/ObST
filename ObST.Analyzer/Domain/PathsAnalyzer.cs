using ObST.Analyzer.Core.Models;
using ObST.Analyzer.Domain;
using ObST.Analyzer.Domain.Util;
using KellermanSoftware.CompareNetObjects;
using Microsoft.OpenApi.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ObST.Domain.OasAnalyzer;

internal sealed class PathsAnalyzer
{
    public static IList<AnalyzedPath> ExtractPaths(OpenApiDocument document)
    {
        var paths = document.Paths
            .Select(item => AnalyzePathItem(item.Key, item.Value, document))
            .ToList();

        foreach (var path in paths)
        {
            var toResolve = path.Parameters.Where(param => param.Mapping == null).ToList();

            var config = new ComparisonConfig();
            config.IgnoreProperty<OpenApiSchema>(s => s.Title);
            config.IgnoreProperty<OpenApiParameter>(s => s.Name);
            config.IgnoreProperty<OpenApiParameter>(s => s.Description);

            foreach (var param in toResolve)
            {
                var matchingPath = paths.FirstOrDefault(x => x.Path.Path == param.PartialPath);

                if (matchingPath == null)
                {
                    param.Mapping = "UNKNOWN!";
                    continue;
                    //throw new ArgumentException($"No matching path for partial path '{param.PartialPath}' found!");
                }

                var matchingParameter = matchingPath.Parameters.Last();

                //ensure parameters are matching, ignore title
                matchingParameter.Parameter.ShouldCompare(param.Parameter, compareConfig: config);

                param.Mapping = matchingParameter.Mapping;
                param.Parameter!.Schema = matchingParameter.Parameter!.Schema;
            }

            path.Path.Parameters = path.Parameters.Select(p => (p.Name, p.Mapping)).ToList();
        }

        return paths;
    }

    private static AnalyzedPath AnalyzePathItem(string path, OpenApiPathItem item, OpenApiDocument document)
    {
        var (pathParameters, lastIsParameter, strippedPath) = GetPathParameters(path);

        //override global servers
        var pathServers = document.Servers;

        if (item.Servers?.Any() == true)
            pathServers = item.Servers;

        var resourcePath = new ResourcePath
        {
            Path = strippedPath,
            PathDescription = item.Description,
            PathSummery = item.Summary,
            PathExtensions = item.Extensions,
            Servers = pathServers,
            KnownMethods = item.Operations.Keys.ToList()
        };

        ResourceClass? resourceClass = null;

        foreach (var o in item.Operations)
        {
            //Analyze the resource class
            if (TryFindResourceClassName(o.Key, o.Value, path, out var res))
            {
                resourceClass = res;
            }

            //override path parameters
            var opParameters = AnalyzerUtil.MergePathAndOperationParameters(item.Parameters, o.Value.Parameters);

            ApplyAndValidatePathParameterScheme(pathParameters, opParameters, path);
        }

        resourcePath.ResourceClass = resourceClass ?? new ResourceClass { Name = "UNKNOWN!" }; // throw new ArgumentException($"Unable to determin ResourceClass for: {path}");

        if (lastIsParameter)
        {
            var lastParam = pathParameters.Last();
            lastParam.Mapping = resourcePath.ResourceClass.AsIdMapping();
            lastParam.Parameter!.Schema.Title = resourcePath.ResourceClass.AsIdMapping();
        }

        return new AnalyzedPath(resourcePath, pathParameters, item, lastIsParameter);
    }

    private static void ApplyAndValidatePathParameterScheme(IList<ResourcePathParameter> pathParameters, IList<OpenApiParameter> opParameters, string path)
    {
        foreach (var p in pathParameters)
        {
            var match = opParameters.SingleOrDefault(param => param.Name == p.Name && param.In == ParameterLocation.Path);

            if (match == null)
                throw new InvalidOperationException($"No matching parameter found for path parameter '{p.Name}' in {path}");

            //ensure schemas match across all operations of this path
            if (p.Parameter is not null)
            {
                p.Parameter.Schema.ShouldCompare(match.Schema);

                if (p.Parameter.Style != match.Style)
                    throw new InvalidOperationException($"Styles of path parameter '{p.Name}' do not match across all operations of path {path}");
            }
            else
                p.Parameter = match;
        }
    }

    private static bool TryFindResourceClassName(OperationType type, OpenApiOperation operation, string path, [NotNullWhen(true)] out ResourceClass? res)
    {

        if (type == OperationType.Get)
        {
            var successResponse = operation.Responses.FirstOrDefault(r => r.Key == "200" || r.Key == "2XX");

            if (successResponse.Value == null || !successResponse.Value.Content.Any())
            {
                //204 GET /gists/{gist_id}/star
                res = new ResourceClass
                {
                    Name = "UNKNOWN!",
                };
                return true;
                //throw new ArgumentException($"GET operation without 200 or 2XX response specified: {path}");
            }

            var resourceSchema = successResponse.Value.Content.First().Value.Schema;

            if (resourceSchema == null)
                throw new ArgumentException($"GET operation is missing schema for {successResponse.Key}: {path}");

            res = new ResourceClass();

            var c = res;

            while (resourceSchema.Type == "array")
            {
                var newClass = new ResourceClass();
                c.Subordinate = newClass;
                c = newClass;
                resourceSchema = resourceSchema.Items;
            }

            if (resourceSchema.Reference != null)
                c.Name = resourceSchema.Reference.Id;
            else
                c.Name = resourceSchema.Title;

            return true;
        }
        //TODO add other indicators

        res = null;
        return false;
    }

    private static (List<ResourcePathParameter> parameter, bool lastIsParameter, string cleanPath) GetPathParameters(string path)
    {
        var parameters = new List<ResourcePathParameter>();

        var lastIsParameter = false;
        var partialPath = new StringBuilder();

        foreach (var segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            partialPath.Append('/');

            if (segment.StartsWith('{'))
            {
                if (!segment.EndsWith('}'))
                    throw new ArgumentException($"Invalid path segment '{segment}'!");

                partialPath.Append("{?}");

                parameters.Add(new ResourcePathParameter(segment[1..^1], partialPath.ToString()));

                lastIsParameter = true;
            }
            else
            {
                partialPath.Append(segment);
                lastIsParameter = false;
            }
        }

        return (parameters, lastIsParameter, partialPath.ToString());
    }
}
