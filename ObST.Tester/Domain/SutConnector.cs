using ObST.Tester.Core.Interfaces;
using ObST.Tester.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System.Text;

namespace ObST.Tester.Domain;
class SutConnector : ISutConnector, IDisposable
{

    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly ICoverageTracker _tracker;
    private readonly ITestConfigurationProvider _testConfigurationProvider;

    public SutConnector(ICoverageTracker tracker, ITestConfigurationProvider testConfigurationProvider, ILogger<SutConnector> logger)
    {

        var handler = new HttpClientHandler
        {
            //this allows to manually add cookies as header
            UseCookies = false
        };

        _httpClient = new HttpClient(handler);
        _logger = logger;
        _tracker = tracker;
        _testConfigurationProvider = testConfigurationProvider;
    }

    public async Task<(HttpResponseMessage response, string body)> RunAsync(SutOperation operation, SutOperationValues parameters, string correlationId)
    {
        _logger.LogDebug("[{correlationId}] Running {operationId}", correlationId, operation.OperationId);

        var request = BuildRequestMessage(operation, parameters);

        _logger.LogTrace("[{correlationId}] Request: {request}, Body: {@requestBody}", correlationId, request, parameters.Body?.Content);

        var res = await _httpClient.SendAsync(request);

        var trackerTask = _tracker.AddTrackingAsync(res, operation);

        var content = await res.Content.ReadAsStringAsync();

        _logger.LogTrace("[{correlationId}] Response: {response}, Content: {responseContent}", correlationId, res, content);

        await trackerTask;

        return (res, content);
    }

    public async Task<bool> ResetAsync()
    {

        var resetUri = _testConfigurationProvider.TestConfiguration?.Setup?.ResetUri;

        if (resetUri is null)
        {
            _logger.LogWarning("ResetUri not set. Unable to reset SUT");
            return false;
        }
        else
        {
            _logger.LogDebug("Reseting SUT");
            var res = await _httpClient.PostAsync(resetUri, null);

            return res.IsSuccessStatusCode;
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private HttpRequestMessage BuildRequestMessage(SutOperation operation, SutOperationValues parameters)
    {
        var server = operation.ServerUrls.First().TrimEnd('/');

        var path = operation.Path;

        //apply path parameters
        foreach (var value in parameters.Path)
        {
            var index = path.IndexOf("{?}");
            path = path[..index] + value + path[(index + 3)..];
        }

        var uri = new UriBuilder(server + path);

        if (parameters.Query.Any())
            uri.Query = string.Join("&", parameters.Query.Select(p => p.Key + "=" + p.Value));

        var request = new HttpRequestMessage(GetHttpMethod(operation.OperationType), uri.Uri);

        switch (operation.OperationType)
        {
            case OperationType.Put:
            case OperationType.Post:
            case OperationType.Patch:
                if (parameters.Body != null)
                    request.Content = BuildHttpContent(parameters.Body);
                break;
            default:
                if (parameters.Body != null)
                    throw new InvalidOperationException($"Body is not supported for {operation.OperationType} operations!");
                break;
        }

        foreach (var p in parameters.Header)
            request.Headers.Add(p.Key, p.Value);

        if (parameters.Cookie.Any())
        {
            var cookieHeader = string.Join("; ", parameters.Cookie.Select(p => p.Key + "=" + p.Value));

            request.Headers.Add("Cookie", cookieHeader);
        }

        return request;
    }

    private HttpContent BuildHttpContent(BodyValue body)
    {
        switch (body.ContentType)
        {
            case "application/json":
            case "text/json":
            case "application/*+json":
                var json = body.Content != null ? JsonConvert.SerializeObject(body.Content) : string.Empty;

                return new StringContent(json, Encoding.UTF8, body.ContentType);
            default:
                throw new NotImplementedException($"ContenType: '{body.ContentType}' is not implemented!");
        }
    }

    private HttpMethod GetHttpMethod(OperationType operationType)
    {
        return operationType switch
        {
            OperationType.Get => HttpMethod.Get,
            OperationType.Put => HttpMethod.Put,
            OperationType.Post => HttpMethod.Post,
            OperationType.Delete => HttpMethod.Delete,
            OperationType.Options => HttpMethod.Options,
            OperationType.Head => HttpMethod.Head,
            OperationType.Patch => HttpMethod.Patch,
            OperationType.Trace => HttpMethod.Trace,
            _ => throw new NotImplementedException($"OperationType: {operationType} is not implemented!")
        };
    }
}
