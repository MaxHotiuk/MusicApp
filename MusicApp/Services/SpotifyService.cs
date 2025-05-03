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

        public async Task<List<SpotifyTrackDto>> GetPlaylistTracksAsync(string accessToken, string playlistId)
        {
            var tracks = new List<SpotifyTrackDto>();
            string nextUrl = $"{_apiBaseUrl}/playlists/{playlistId}/tracks?limit=100";
            
            while (!string.IsNullOrEmpty(nextUrl))
            {
                var request = new HttpRequestMessage(HttpMethod.Get, nextUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var tracksResponse = JsonSerializer.Deserialize<SpotifyPaginatedResponse<SpotifyPlaylistTrackDto>>(
                    content, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
                
                // Extract track information from playlist track objects
                if (tracksResponse?.Items != null)
                {
                    tracks.AddRange(tracksResponse.Items
                        .Where(item => item.Track != null)
                        .Select(item => item.Track!));
                }
                
                nextUrl = tracksResponse?.Next ?? string.Empty;
            }
            
            return tracks;
        }

        public async Task<List<SpotifyArtistDto>> GetArtistsByGenreAsync(string accessToken, string genre, int limit = 5)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/search?q=genre:{HttpUtility.UrlEncode(genre)}&type=artist&limit={limit}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var searchResponse = JsonSerializer.Deserialize<SpotifySearchResponse>(
                content, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
            
            return searchResponse?.Artists?.Items ?? new List<SpotifyArtistDto>();
        }

        public async Task<List<SpotifyTrackDto>> GetArtistTopTracksAsync(string accessToken, string artistId, string market = "US")
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/artists/{artistId}/top-tracks?market={market}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var topTracksResponse = JsonSerializer.Deserialize<SpotifyArtistTopTracksResponse>(
                content, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
            
            return topTracksResponse?.Tracks ?? new List<SpotifyTrackDto>();
        }

        public async Task<List<SpotifyTrackDto>> GetPlaylistRecommendationsAsync(string accessToken, string playlistId)
        {
            // Get all tracks from the playlist
            var playlistTracks = await GetPlaylistTracksAsync(accessToken, playlistId);
            
            // Extract all genres from the artists
            var genreCounts = new Dictionary<string, int>();
            
            foreach (var track in playlistTracks)
            {
                if (track.Artists != null)
                {
                    foreach (var artist in track.Artists)
                    {
                        if (artist == null || string.IsNullOrEmpty(artist.Id)) continue;
                        
                        // Get artist details to get genres
                        try
                        {
                            var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/artists/{artist.Id}");
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                            
                            var response = await _httpClient.SendAsync(request);
                            response.EnsureSuccessStatusCode();
                            
                            var content = await response.Content.ReadAsStringAsync();
                            var artistDetails = JsonSerializer.Deserialize<SpotifyArtistDto>(
                                content, 
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                            );
                            
                            if (artistDetails?.Genres != null)
                            {
                                foreach (var genre in artistDetails.Genres)
                                {
                                    if (!string.IsNullOrEmpty(genre))
                                    {
                                        if (genreCounts.ContainsKey(genre))
                                            genreCounts[genre]++;
                                        else
                                            genreCounts[genre] = 1;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Continue with next artist if we can't get details for one
                            continue;
                        }
                    }
                }
            }
            
            // Get top 5 most common genres
            var topGenres = genreCounts
                .OrderByDescending(g => g.Value)
                .Take(5)
                .Select(g => g.Key)
                .ToList();
            
            // Create recommendations list
            var recommendations = new List<SpotifyTrackDto>();
            var processedArtists = new HashSet<string>();
            
            // For each genre, find one artist and get their top tracks
            foreach (var genre in topGenres)
            {
                var artists = await GetArtistsByGenreAsync(accessToken, genre, 5);
                
                // Find an artist we haven't processed yet
                var artist = artists.FirstOrDefault(a => !processedArtists.Contains(a.Id!));
                if (artist != null && !string.IsNullOrEmpty(artist.Id))
                {
                    processedArtists.Add(artist.Id);
                    var topTracks = await GetArtistTopTracksAsync(accessToken, artist.Id);
                    
                    // Add top 3 tracks to recommendations
                    recommendations.AddRange(topTracks.Take(3));
                }
            }
            
            // Randomize and limit recommendations
            return recommendations
                .OrderBy(_ => Guid.NewGuid())
                .Take(15)
                .ToList();
        }
    }
}