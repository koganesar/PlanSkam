using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Planscam.MobileApi.Tests;

public class AuthTests : TestBase
{
    public AuthTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task Login()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/Auth/Login");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            {"grant_type", "password"},
            {"username", "qwe"},
            {"password", "qweQWE123!"}
        });
        var response = await Client.SendAsync(request);
        await WriteResponseToOutput(response);
        response.StatusCodeIsOk();
    }

    [Fact]
    public async Task Register()
    {
        var name = string.Empty;
        for (var i = 0; i < 5; ++i) 
            name += (char) ('a' + Random.Shared.Next(22));
        var request = new HttpRequestMessage(HttpMethod.Post, "Auth/Register");
        request.Content = new StringContent(JsonConvert.SerializeObject(new
        {
            userName = name,
            email = $"{name}@test.com",
            password = "tyuTYU123!",
            passwordConfirm = "tyuTYU123!"
        }), Encoding.UTF8, "application/json");
        var response = await Client.SendAsync(request);
        await WriteResponseToOutput(response);
        response.StatusCodeIsOk();
    }
}
