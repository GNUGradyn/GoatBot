using System.Net;
using Goatbot.Models;
using Microsoft.Extensions.Configuration;

namespace Goatbot.Services;

public class LonestarAPIClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly CookieContainer _cookieContainer;

    public LonestarAPIClient(IConfiguration config)
    {
        _cookieContainer = new CookieContainer();
        _config = config;
        _httpClient = new HttpClient(new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = _cookieContainer
        })
        {
            BaseAddress = new Uri(_config["LonestarAPI:BaseAddress"])
        };
    }

    private async Task Authenticate()
    {
        var payload = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("form_build_id", _config["LonestarAPI:LonestarFormBuildId"]),
            new KeyValuePair<string, string>("form_id", _config["LonestarAPI:LonestarFormId"]),
            new KeyValuePair<string, string>("name", _config["LonestarAPI:Username"]),
            new KeyValuePair<string, string>("op", _config["LonestarAPI:LonestarOp"]),
            new KeyValuePair<string, string>("pass", _config["LonestarAPI:Password"])
        });

        var response = await _httpClient.PostAsync("user", payload);
        response.EnsureSuccessStatusCode();
    }

    // request factory used for safe retry - request messages are single use
    private async Task<HttpResponseMessage> AuthenticatedRequest(Func<HttpRequestMessage> requestFactory, bool ensureSuccess = true)
    {
        if (!_cookieContainer.GetAllCookies().Any(x => x.Name == "jwt")) await Authenticate();
        
        var request = requestFactory();
        var response = await _httpClient.SendAsync(request);
        
        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized) await Authenticate();
        
        request = requestFactory();
        response = await _httpClient.SendAsync(request);
        if (ensureSuccess) response.EnsureSuccessStatusCode();
        
        return response;
    }

    public async Task IssuePermit(PermitRequest permitRequest)
    {
        var repsonse = AuthenticatedRequest(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post,
                new Uri("js-custom_form_services/form_save/permit/new.json"));


            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("account[0][nid]", _config["LonestarAPI:LonestarAccountNuber"]),
                new KeyValuePair<string, string>("apartment_number[0][value]", _config["LonestarAPI:LonestarApartmentNumber"]),
                new KeyValuePair<string, string>("date_first_issued[0][value][date]", DateTime.Now.ToString("MM/dd/yyyy")),
                new KeyValuePair<string, string>("date_first_issued[0][value][date]", DateTime.Now.ToString("hh:mm tt")),
                new KeyValuePair<string, string>("description[0][value]", string.Empty),
                new KeyValuePair<string, string>("expiration_date[0][value][date]", string.Empty),
                new KeyValuePair<string, string>("expiration_date[0][value][time]", string.Empty),

            });
            return request;
        }
    }


}