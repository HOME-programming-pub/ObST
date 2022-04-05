using ObST.Analyzer.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ObST.Domain.OasAnalyzer
{
    internal class ResponsesAnalyzer
    {
        private readonly ILogger _logger;
        private readonly SchemaAnalyzer _schemaAnalyzer;

        public ResponsesAnalyzer(SchemaAnalyzer schemaAnalyzer, ILogger logger)
        {
            _logger = logger;
            _schemaAnalyzer = schemaAnalyzer;
        }

        public void Analyze(OpenApiResponses responses, OperationType operationType, ResourcePath path)
        {
            var doesCreate = responses.Any(r => r.Key == "201");

            foreach (var r in responses)
            {
                if (doesCreate)
                {
                    if (operationType == OperationType.Post)
                    {
                        if (path.ResourceClass?.Subordinate is null)
                            _logger.LogWarning("POST operation with 201 response on none list resource!");
                    }
                    else if (operationType != OperationType.Put)
                        throw new NotSupportedException($"201 response for {operationType} is not supported!");
                }

                AnalyzeContent(r.Value.Content);
            }
        }

        private void AnalyzeContent(IDictionary<string, OpenApiMediaType> content)
        {
            if (content.Any())
            {
                //analyze schemes
                foreach (var s in content)
                    _schemaAnalyzer.AnalyzeAndMapSchema(s.Value.Schema);
            }
        }
    }
}
