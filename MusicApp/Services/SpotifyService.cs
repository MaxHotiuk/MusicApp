using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using MusicApp.Shared.Models;
using MusicApp.Interfaces;

namespace MusicApp.Services
{
    public class SpotifyService : ISpotifyService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string? _clientId;
        private readonly string? _clientSecret;
        private readonly string _authorizationEndpoint = "https://accounts.spotify.com/authorize";
        private readonly string _tokenEndpoint = "https://accounts.spotify.com/api/token";
        private readonly string _apiBaseUrl = "https://api.spotify.com/v1";

        public SpotifyService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _clientId = configuration["Spotify:ClientId"];
            _clientSecret = configuration["Spotify:ClientSecret"];
        }

        public string GetAuthorizationUrl(string state)
        {
            var scopes = new[] 
            {
                "user-read-private",
                "user-read-email",
                "playlist-read-private",
                "playlist-read-collaborative"
            };

            var queryParams = new Dictionary<string, string>
            {
                { "client_id", _clientId! },
                { "response_type", "code" },
                { "redirect_uri", _configuration["Spotify:RedirectUri"]! },
                { "scope", string.Join(" ", scopes) },
                { "state", state }
            };

            var queryString = string.Join("&", queryParams.Select(p => $"{p.Key}={HttpUtility.UrlEncode(p.Value)}"));
            return $"{_authorizationEndpoint}?{queryString}";
        }

        public async Task<SpotifyTokenResponse> ExchangeCodeForTokenAsync(string code, string redirectUri)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint);
            
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", redirectUri },
                { "client_id", _clientId! },
                { "client_secret", _clientSecret! }
            });
            
            request.Content = content;
            
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<SpotifyTokenResponse>(responseContent, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            }) ?? throw new InvalidOperationException("Failed to deserialize Spotify token response.");
            return tokenResponse;
        }

        public async Task<SpotifyTokenResponse> RefreshTokenAsync(string refreshToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint);
            
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken },
                { "client_id", _clientId! },
                { "client_secret", _clientSecret! }
            });
            
            request.Content = content;
            
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<SpotifyTokenResponse>(responseContent, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            }) ?? throw new InvalidOperationException("Failed to deserialize Spotify token response.");
            return tokenResponse;
        }

        public async Task<SpotifyUserProfile> GetUserProfileAsync(string accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var userProfile = JsonSerializer.Deserialize<SpotifyUserProfile>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            }) ?? throw new InvalidOperationException("Failed to deserialize Spotify user profile.");
            
            return userProfile;
        }

        public async Task<List<SpotifyPlaylistDto>> GetUserPlaylistsAsync(string accessToken)
        {
            var playlists = new List<SpotifyPlaylistDto>();
            string nextUrl = $"{_apiBaseUrl}/me/playlists?limit=50";
            
            while (!string.IsNullOrEmpty(nextUrl))
            {
                var request = new HttpRequestMessage(HttpMethod.Get, nextUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var playlistResponse = JsonSerializer.Deserialize<SpotifyPaginatedResponse<SpotifyPlaylistDto>>(
                    content, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
                
                playlists.AddRange(playlistResponse!.Items!);
                nextUrl = playlistResponse.Next!;
            }
            
            return playlists;
        }
    }
}