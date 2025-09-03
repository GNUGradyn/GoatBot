using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
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
        var respsonse = AuthenticatedRequest(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post,
                new Uri("js-custom_form_services/form_save/permit/new.json"));


            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("account[0][nid]", _config["LonestarAPI:LonestarAccountNuber"]),
                new KeyValuePair<string, string>("apartment_number[0][value]",
                    _config["LonestarAPI:LonestarApartmentNumber"]),
                new KeyValuePair<string, string>("date_first_issued[0][value][date]",
                    DateTime.Now.ToString("MM/dd/yyyy")),
                new KeyValuePair<string, string>("date_first_issued[0][value][date]",
                    DateTime.Now.ToString("hh:mm tt")),
                new KeyValuePair<string, string>("description[0][value]", string.Empty),
                new KeyValuePair<string, string>("expiration_date[0][value][date]", string.Empty),
                new KeyValuePair<string, string>("expiration_date[0][value][time]", string.Empty),
                new KeyValuePair<string, string>("form_part[0][value][time]", _config["LonestarAPI:FormPart"]),
                new KeyValuePair<string, string>("license_plate[0][plate]", permitRequest.PlateNumber),
                new KeyValuePair<string, string>("license_plate[0][state]", permitRequest.PlateStateCode),
                new KeyValuePair<string, string>("permit_contact_email[0][email]", permitRequest.Email),
                new KeyValuePair<string, string>("permit_contact_phone[0][value]", String.Empty),
                new KeyValuePair<string, string>("permit_length_days[0][value]", permitRequest.PermitDays.ToString()),
                new KeyValuePair<string, string>("permit_length_hours[0][value]", String.Empty),
                new KeyValuePair<string, string>("permit_type[0][value]", _config["LonestarAPI:PermitType"]),
                new KeyValuePair<string, string>("vehicle[0][make]", permitRequest.VehicleMake),
                new KeyValuePair<string, string>("vehicle[0][model]", permitRequest.VehicleModel),
                new KeyValuePair<string, string>("vehicle_color[0][tid]", permitRequest.VehicleColorCode.ToString()),
                new KeyValuePair<string, string>("vin_0[0][value]", String.Empty)
            });
            return request;
        });
    }
}