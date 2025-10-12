using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Goatbot.Models;
using Microsoft.Extensions.Configuration;

namespace Goatbot.Services;

public class LonestarAPIClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly CookieContainer _cookieContainer;
    private string _csrfToken;

    public LonestarAPIClient(IConfiguration config)
    {
        _cookieContainer = new CookieContainer();
        _cookieContainer.Add(new Cookie("has_js", "1", "/", "lonestartows.omadi.com") // CHALLENGE BYPASS: pretend JS is working
        {
            HttpOnly = false,
            Secure = false
        });
        _config = config;
        _httpClient = new HttpClient(new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = _cookieContainer,
        })
        {
            BaseAddress = new Uri(_config["LonestarAPI:BaseAddress"]),
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:141.0) Gecko/20100101 Firefox/141.0"); // CHALLENGE BYPASS: pretend this is happening in firefox
    }

    private async Task Authenticate()
    {
        var payload = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("form_build_id", _config.GetValue<string>("LonestarAPI:LonestarFormBuildId")),
            new KeyValuePair<string, string>("form_id", _config.GetValue<string>("LonestarAPI:LonestarFormId")),
            new KeyValuePair<string, string>("name", _config.GetValue<string>("LonestarAPI:Username")),
            new KeyValuePair<string, string>("op", _config.GetValue<string>("LonestarAPI:LonestarOp")),
            new KeyValuePair<string, string>("pass", _config.GetValue<string>("LonestarAPI:Password"))
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

    // CHALLENGE BYPASS: obtain the cross-site request forgery token by pretending to load the new permit page and extracting it with regex
    private async Task<string> ObtainCSRFToken()
    {
        var response = await AuthenticatedRequest(() => new HttpRequestMessage(HttpMethod.Get,"permit/new/?destination=node/20887"));
        var rawResponsePayload = await response.Content.ReadAsStringAsync();
        var regex = new Regex("\"csrfToken\":\"([^\"]+)\"");
        var match = regex.Match(rawResponsePayload);
        if (!match.Success) throw new Exception("HTTP GET success but no CSRF token");
        if (match.Groups.Count != 2) throw new Exception("CSRF token not understood - too many or too few REGEX match groups");
        return match.Groups[1].Value;
    }
    
    public async Task IssuePermit(PermitRequest permitRequest)
    {
        var csrfToken = await ObtainCSRFToken();
        
        var response = await AuthenticatedRequest(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Put,"js-custom_form_services/form_save/permit/new.json");

            request.Headers.Add("X-CSRF-Token", csrfToken);
            
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("account[0][nid]", _config.GetValue<string>("LonestarAPI:LonestarAccountNumber")),
                new KeyValuePair<string, string>("apartment_number[0][value]",
                    _config["LonestarAPI:LonestarApartmentNumber"]),
                new KeyValuePair<string, string>("date_first_issued[0][value][date]",
                    DateTime.Now.ToString("MM/dd/yyyy")),
                new KeyValuePair<string, string>("date_first_issued[0][value][time]",
                    DateTime.Now.ToString("hh:mm tt")),
                new KeyValuePair<string, string>("description[0][value]", string.Empty),
                new KeyValuePair<string, string>("expiration_date[0][value][date]", string.Empty),
                new KeyValuePair<string, string>("expiration_date[0][value][time]", string.Empty),
                new KeyValuePair<string, string>("form_part", _config.GetValue<string>("LonestarAPI:FormPart")),
                new KeyValuePair<string, string>("license_plate[0][plate]", permitRequest.PlateNumber),
                new KeyValuePair<string, string>("license_plate[0][state]", "us-" + permitRequest.PlateStateCode),
                new KeyValuePair<string, string>("permit_contact_email[0][email]", permitRequest.Email),
                new KeyValuePair<string, string>("permit_contact_phone[0][value]", String.Empty),
                new KeyValuePair<string, string>("name_0[0][value]", permitRequest.Name),
                new KeyValuePair<string, string>("permit_length_days[0][value]",
                    (permitRequest.PermitDays + 1)
                    .ToString()), // +1 to account for how 1 day will end at the end of the day, not 1 night
                new KeyValuePair<string, string>("permit_length_hours[0][value]", String.Empty),
                new KeyValuePair<string, string>("permit_number_text[0][value]", String.Empty),
                new KeyValuePair<string, string>("permit_type[0][value]", _config.GetValue<string>("LonestarAPI:PermitType")),
                new KeyValuePair<string, string>("vehicle[0][make]", permitRequest.VehicleMake),
                new KeyValuePair<string, string>("vehicle[0][model]", permitRequest.VehicleModel),
                new KeyValuePair<string, string>("vehicle_color[0][tid]", permitRequest.VehicleColorCode.ToString()),
                new KeyValuePair<string, string>("vin_0[0][value]", String.Empty)
            });
             return request;
        });
        
        var rawResponsePayload = await response.Content.ReadAsStringAsync();
        
        var responsePayload = JsonSerializer.Deserialize<PermitResponse>(rawResponsePayload);

        if (responsePayload == null) throw new Exception("Response could not be deserialized into a PermitResponse. Some gibberish to help grandon: " + rawResponsePayload);
        
        if (!responsePayload.Successful) throw new Exception("Lonestar returned a non-success payload. Some gibberish to help grandon: " + rawResponsePayload);
    }
}