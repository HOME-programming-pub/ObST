using ObST.Analyzer.Core.Models;
using ObST.Analyzer.Domain.Util;
using ObST.Core.Util;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ObST.Domain.OasAnalyzer
{
    internal class SchemaAnalyzer
    {
        private readonly HashSet<OpenApiSchema> _analyzedSchemas = new HashSet<OpenApiSchema>();

        private int UnknownCounter = 0;
        private readonly Regex _idPattern;
        private readonly Regex _primaryResourceIdPattern;
        private readonly ILogger _logger;

        public readonly Dictionary<ResourceClass, OpenApiSchema> ResourceClasses = new();

        public SchemaAnalyzer(string idPattern, string primaryResourceIdPattern, OpenApiComponents components, ILogger logger)
        {
            _idPattern = new Regex(idPattern);
            _primaryResourceIdPattern = new Regex(primaryResourceIdPattern);

            _logger = logger;

            //Add names first
            foreach (var schema in components.Schemas)
                if (schema.Value.Type == "object")
                    ResourceClasses.Add(new ResourceClass { Name = schema.Key }, schema.Value);

            foreach (var schema in components.Schemas)
                if (schema.Value.Type == "object")
                    AnalyzeAndMapSchema(schema.Key, null, schema.Value);
        }

        public void AnalyzeAndMapSchema(OpenApiSchema schema)
        {
            AnalyzeAndMapSchema(schema.Title, null, schema);
        }

        private void AnalyzeAndMapSchema(string mapping, string? propertyKey, OpenApiSchema schema)
        {
            if (schema.Type == "object")
            {
                if (_analyzedSchemas.Contains(schema))
                    return;
                else
                {
                    if (schema.Reference is not null)
                        mapping = schema.Reference.Id;
                    else if (mapping is null)
                        mapping = "UNKNOWN_OBJECT_" + UnknownCounter++;
                    else if (propertyKey is not null)
                        mapping = propertyKey;

                    foreach (var prop in schema.Properties)
                    {
                        AnalyzeAndMapSchema("", prop.Key, prop.Value);
                    }

                    _analyzedSchemas.Add(schema);

                    if (!ResourceClasses.ContainsKey(new ResourceClass { Name = mapping }))
                        ResourceClasses.Add(new ResourceClass { Name = mapping }, schema);
                }
            }
            else if (schema.Type == "array")
            {
                AnalyzeAndMapSchema(mapping, propertyKey, schema.Items);

                mapping = schema.Items.Title;
            }
            else
            {
                propertyKey ??= "Unknown_Property_" + UnknownCounter++;

                if (_primaryResourceIdPattern.IsMatch(propertyKey))
                {
                    //this is the primary resource id
                    mapping = mapping.AddIdMapping();
                }
                else if (_idPattern.IsMatch(propertyKey))
                {
                    //find out which is the matching reference!
                    var name = _idPattern.Split(propertyKey).Single(s => s != string.Empty);

                    var primaryMapping = mapping;
                    mapping = ResourceClasses.Select(r => r.Key.ToString()).SingleOrDefault(r => r?.ToLower() == name.ToLower()) ?? "Unkown_Mapping";

                    if (mapping == null)
                    {
                        _logger.LogWarning($"No resource class found for {primaryMapping}:{propertyKey}");
                        mapping += ":" + propertyKey;
                    }
                    else
                        mapping = mapping.AddIdMapping();
                }
                else
                {
                    mapping += ":" + propertyKey;
                }
            }

            schema.Title = mapping;
        }

        public ResourceClass? GetResourceClassBySchema(OpenApiSchema schema)
        {
            var resourceClass = new ResourceClass();
            var current = resourceClass;

            while (schema.Type == "array")
            {
                var subordinate = new ResourceClass();
                current.Subordinate = subordinate;
                current = subordinate;
                schema = schema.Items;
            }

            if (schema.Type == "object")
            {
                if (schema.Reference == null)
                    current.Name = AddAndAnalyzeInlineSchema(schema);
                else
                    current.Name = schema.Reference.Id;
            }
            else
                return null;

            return resourceClass;
        }

        private string AddAndAnalyzeInlineSchema(OpenApiSchema schema)
        {
            if (string.IsNullOrWhiteSpace(schema.Title))
                throw new ArgumentException("Title of inline schema is not set!");

            var resourceClass = new ResourceClass
            {
                Name = schema.Title
            };

            if (ResourceClasses.ContainsKey(resourceClass))
                throw new ArgumentException($"Inline schema {resourceClass} was allready defined!");
            else
                ResourceClasses.Add(resourceClass, schema);

            return resourceClass.Name;
        }
    }
}
