using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Barberslop.IntegrationTests;

public class BasicPageTests : IClassFixture<BarberWebApplicationFactory>
{
    private readonly HttpClient _client;

    public BasicPageTests(BarberWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task HomePage_Returns200()
    {
        var response = await _client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HomePage_ContainsBarberslop()
    {
        var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Barberslop", content);
    }

    [Fact]
    public async Task LoginPage_Returns200()
    {
        var response = await _client.GetAsync("/Account/Login");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RegisterPage_Returns200()
    {
        var response = await _client.GetAsync("/Account/Register");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedPage_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/Booking/Book");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task BarberOnlyPage_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/Services");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
}
