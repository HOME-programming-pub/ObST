using ObST.Core.Models;
using ObST.Tester.Core.Interfaces;
using ObST.Tester.Core.Models;
using ObST.Tester.Domain.Util;
using FsCheck;
using FsCheck.Experimental;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;

namespace ObST.Tester.Domain.Operation;

abstract class TestOperation : Operation<ISutConnector, TestModel>
{

    public SutOperation Operation { get; }
    protected SutOperationValues? values;
    protected readonly TestConfiguration _config;
    private readonly IIdentityConnector _identityConnector;

    protected TestOperation(SutOperation operation, TestConfiguration config, IIdentityConnector identityConnector)
    {
        Operation = operation;
        _config = config;
        _identityConnector = identityConnector;
    }

    public abstract IList<TestParameterGeneratorOption> GetGeneratorOptions(TestModel model);

    public void SetParameters(SutOperationValues values)
    {
        this.values = values;
    }

    public override Property Check(ISutConnector actual, TestModel model)
    {
        return CheckWithFastFail(actual, model).Property;
    }

    public FastFailProperty CheckWithFastFail(ISutConnector actual, TestModel model)
    {
        var res = RunActual(actual);

        //Default properties

        var noServerError = (res.Response.StatusCode < HttpStatusCode.InternalServerError).FastFailLabel("Server Error");
        var notDocumentedStatusCode = (res.Documentation is not null).FastFailLabel("StatusCode is not documented");
        var badRequestWithValidParameters = (res.Response.StatusCode != HttpStatusCode.BadRequest && res.Response.StatusCode != HttpStatusCode.UnprocessableEntity)
                                                .FastFailLabel("BadRequest/UnprocessableEntity with valid parameter values");

        var notDocumentedHeader = HeadersMatchDoc(res.Response.Headers, res.Response.Content.Headers, res.Documentation)
                                    .FastFailWhen(res.Documentation is not null)
                                    .Label("Headers do not match documentation");

        var notDocumentedBodySchema = res.BodyMatchesSchema
                                        .FastFailWhen(res.DocumentedSchema is not null)
                                        .Label("Body does not match documented schema")
                                        .Label($"Actual Body: {res.Body}");

        var property = noServerError;

        if (_config.Setup?.Properties?.NoBadRequestWhenValidDataIsProvided == true)
            property = property.And(badRequestWithValidParameters);

        if (_config.Setup?.Properties?.ResponseDocumentation == true)
            property = property.And(notDocumentedStatusCode);

        //Add label for all status code related errors
        property = property.Label($"Actual StatusCode: {res.Response.StatusCode}");

        if (_config.Setup?.Properties?.ResponseDocumentation == true)
        {
            property = property
                .And(notDocumentedHeader)
                .And(notDocumentedBodySchema);
        }

        //currentIdentity is permitted
        if (values!.IdentityHasAllPermissions)
            return property.And(CheckResult(res, model));
        else
            return property.And(
                (res.Response.StatusCode == HttpStatusCode.Forbidden || res.Response.StatusCode == HttpStatusCode.NotFound)
                .FastFailLabel("Expected Forbidden or NotFound when sending request with missing permissions")
                .Label($"Actual StatusCode: {res.Response.StatusCode}")
                );
    }

    public override TestModel Run(TestModel model)
    {
        return new TestModel(model);
    }

    public override bool Pre(TestModel model)
    {
        var options = GetGeneratorOptions(model);

        if (!options.Any())
            return false;

        if (values != null && !options.Any(o => o.Equals(values.UsedGeneratorOptions)))
            return false;

        return base.Pre(model);
    }

    public override string ToString()
    {
        return $"{Operation.OperationId}: {JsonConvert.SerializeObject(values)}";
    }

    protected abstract FastFailProperty CheckResult(RunActualResult res, TestModel model);

    protected IEnumerable<(string mapping, string value)> GetPathValues()
    {
        return Operation.Parameters.Where(p => p.Location == ParameterLocation.Path).Select((p, i) => (p.Mapping, values!.Path[i]));
    }

    private RunActualResult RunActual(ISutConnector actual)
    {
        var correlationId = Guid.NewGuid().ToString();

        if (values!.Identity != SutIdentity.NULL_IDENTITY)
            GetIdentityAndApplyToParameters(values.Identity);

        var (res, body) = actual.RunAsync(Operation, values, correlationId).GetAwaiter().GetResult();
        var (contentLength, contentType) = ExtractHeaders(res.Content.Headers);

        var documentation = GetResponseDoc(res.StatusCode);

        JContainer? bodyObj = null;
        var bodyMatchesSchema = false;

        if (documentation?.Schema is not null &&
            documentation.ContentType is not null &&
            documentation.ContentType == contentType?.MediaType &&
            contentLength > 0)
        {
            bodyMatchesSchema = ParseBody(body, documentation.Schema, contentType, out bodyObj);
        }

        return new RunActualResult(
            res,
            body,
            bodyObj,
            bodyMatchesSchema,
            contentLength,
            contentType,
            documentation,
            bodyMatchesSchema ? documentation!.Schema : null
            );
    }

    private void GetIdentityAndApplyToParameters(SutIdentity identity)
    {
        var token = _identityConnector.GetIdentityInformationAsync(identity).GetAwaiter().GetResult();

        switch (identity.SecurityScheme.Type)
        {
            case SecuritySchemeType.ApiKey:
                switch (identity.SecurityScheme.In)
                {
                    case "Query":
                        values!.Query[identity.SecurityScheme.Name!] = token;
                        break;
                    case "Header":
                        values!.Header[identity.SecurityScheme.Name!] = token;
                        break;
                    case "Cookie":
                        values!.Cookie[identity.SecurityScheme.Name!] = token;
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported location {identity.SecurityScheme.In} for ApiKey");
                }
                break;
            case SecuritySchemeType.OpenIdConnect:
                values!.Header["Authorization"] = "Bearer " + token;
                break;
            default:
                throw new NotSupportedException($"Unsupported security scheme {identity.SecurityScheme.Type}");
        }
    }

    private SutResponse? GetResponseDoc(HttpStatusCode statusCode)
    {
        if (!Operation.Responses.TryGetValue(((int)statusCode).ToString(), out var response))
        {
            //Check for e.g. 4XX code 
            var statusCodeClass = (int)statusCode / 100 + "XX";
            Operation.Responses.TryGetValue(statusCodeClass, out response);
        }

        return response;
    }

    private bool HeadersMatchDoc(HttpResponseHeaders responseHeaders, HttpContentHeaders contentHeaders, SutResponse? responseDoc)
    {
        return true;
    }

    private bool BodyMatchesDoc(JToken body, JsonSchema expectedJsonSchema)
    {
        if (expectedJsonSchema == null || body == null)
            return false;

        var validationErros = expectedJsonSchema.Validate(body);

        return !validationErros.Any();
    }

    private (long? contentLength, System.Net.Http.Headers.MediaTypeHeaderValue? contentType) ExtractHeaders(HttpContentHeaders headers)
    {
        return (headers.ContentLength, headers.ContentType);
    }

    private bool ParseBody(string body, JsonSchema expectedJsonSchema, System.Net.Http.Headers.MediaTypeHeaderValue contentType, [NotNullWhen(true)][MaybeNullWhen(false)] out JContainer? bodyObj)
    {
        if (contentType.ToString() != "application/json; charset=utf-8" && contentType.ToString() != "application/json")
            throw new NotImplementedException($"Unsupported {HeaderNames.ContentType}: {contentType}!");

        try
        {
            bodyObj = expectedJsonSchema.ActualSchema.Type switch
            {
                JsonObjectType.Object => JObject.Parse(body),
                JsonObjectType.Array => JArray.Parse(body),
                _ => throw new NotImplementedException($"Parsing body of schema type '{expectedJsonSchema.Type}' failed!"),
            };

            if (BodyMatchesDoc(bodyObj, expectedJsonSchema))
                return true;
            else
            {
                return false;
            }
        }
        catch (JsonReaderException)
        {
            bodyObj = null;
            return false;
        }
    }

    protected record RunActualResult
    {
        public HttpResponseMessage Response { get; }
        public string Body { get; }
        public JToken? BodyObject { get; }
        public bool BodyMatchesSchema { get; }
        public long? ContentLength { get; }
        public System.Net.Http.Headers.MediaTypeHeaderValue? ContentType { get; }
        public SutResponse? Documentation { get; }
        public JsonSchema? DocumentedSchema { get; }

        public RunActualResult(HttpResponseMessage response, string body, JToken? bodyObject, bool bodyMatchesSchema,
            long? contentLength, System.Net.Http.Headers.MediaTypeHeaderValue? contentType, SutResponse? documentation,
            JsonSchema? documentedSchema)
        {
            Response = response;
            Body = body;
            BodyObject = bodyObject;
            BodyMatchesSchema = bodyMatchesSchema;
            ContentLength = contentLength;
            ContentType = contentType;
            Documentation = documentation;
            DocumentedSchema = documentedSchema;

        }
    }
}
