using Microsoft.JSInterop;
using MusicApp.Client.Interfaces;
using MusicApp.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace MusicApp.Client.Services;

public class SpotifyClientService : ISpotifyClientService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;

    public SpotifyClientService(
        HttpClient httpClient,
        IAuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    public async Task<SpotifyUserProfile> GetUserProfileAsync()
    {
        var token = await _authService.GetToken();
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        return await _httpClient.GetFromJsonAsync<SpotifyUserProfile>("api/spotify/profile") ?? throw new InvalidOperationException("Failed to retrieve user profile.");
    }

    public async Task<List<SpotifyPlaylistDto>> GetUserPlaylistsAsync()
    {
        var token = await _authService.GetToken();
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        return await _httpClient.GetFromJsonAsync<List<SpotifyPlaylistDto>>("api/spotify/playlists") ?? throw new InvalidOperationException("Failed to retrieve user playlists.");
    }
}