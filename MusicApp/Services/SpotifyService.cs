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
            // Get tracks from the playlist
            var playlistTracks = await GetPlaylistTracksAsync(accessToken, playlistId);
            
            // Sample tracks if the playlist is large
            var sampledTracks = playlistTracks.Count > 100 
                ? playlistTracks.OrderBy(_ => Guid.NewGuid()).Take(100).ToList() 
                : playlistTracks;
            
            // Extract unique artist IDs
            var uniqueArtistIds = new HashSet<string>();
            foreach (var track in sampledTracks)
            {
                if (track.Artists != null)
                {
                    foreach (var artist in track.Artists)
                    {
                        if (artist != null && !string.IsNullOrEmpty(artist.Id))
                        {
                            uniqueArtistIds.Add(artist.Id);
                        }
                    }
                }
            }
            
            // Get artist details in batches of 50 (Spotify's limit for Get Several Artists endpoint)
            var genreCounts = new Dictionary<string, int>();
            var artistIds = uniqueArtistIds.ToList();
            
            for (int i = 0; i < artistIds.Count; i += 50)
            {
                var batchIds = artistIds.Skip(i).Take(50);
                var idsParam = string.Join(",", batchIds);
                
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/artists?ids={idsParam}");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    
                    var response = await _httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    
                    var content = await response.Content.ReadAsStringAsync();
                    var artistsResponse = JsonSerializer.Deserialize<SpotifyMultipleArtistsResponse>(
                        content, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    
                    if (artistsResponse?.Artists != null)
                    {
                        foreach (var artist in artistsResponse.Artists)
                        {
                            if (artist?.Genres != null)
                            {
                                foreach (var genre in artist.Genres)
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
                    }
                }
                catch
                {
                    // Continue with next batch if we can't get details for one batch
                    continue;
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
            
            // For each genre, find artists and get their top tracks
            foreach (var genre in topGenres)
            {
                var artists = await GetArtistsByGenreAsync(accessToken, genre, 3);
                
                // Find artists we haven't processed yet
                foreach (var artist in artists)
                {
                    if (artist != null && !string.IsNullOrEmpty(artist.Id) && !processedArtists.Contains(artist.Id))
                    {
                        processedArtists.Add(artist.Id);
                        var topTracks = await GetArtistTopTracksAsync(accessToken, artist.Id);
                        
                        // Add top tracks to recommendations
                        recommendations.AddRange(topTracks.Take(2));
                        
                        // Break after processing one artist per genre to limit API calls
                        break;
                    }
                }
            }
            
            // Randomize and limit recommendations
            var finalRecommendations = recommendations
                .OrderBy(_ => Guid.NewGuid())
                .Take(15)
                .ToList();
                
            // Make sure all tracks have album artwork data
            await EnsureTrackAlbumImagesAsync(accessToken, finalRecommendations);
                
            return finalRecommendations;
        }
        
        // New method to ensure tracks have album images
        private async Task EnsureTrackAlbumImagesAsync(string accessToken, List<SpotifyTrackDto> tracks)
        {
            var tracksMissingAlbumImages = tracks
                .Where(t => t.Album?.Images == null || !t.Album.Images.Any())
                .ToList();
                
            if (!tracksMissingAlbumImages.Any())
                return; // All tracks have images, nothing to do
                
            // Get album IDs that need image data
            var albumIds = tracksMissingAlbumImages
                .Where(t => t.Album != null && !string.IsNullOrEmpty(t.Album.Id))
                .Select(t => t.Album!.Id!)
                .Distinct()
                .ToList();
                
            if (!albumIds.Any())
                return; // No valid album IDs to fetch
                
            // Process in batches of 20 (Spotify's limit for Get Multiple Albums endpoint)
            for (int i = 0; i < albumIds.Count; i += 20)
            {
                var batchIds = albumIds.Skip(i).Take(20);
                var idsParam = string.Join(",", batchIds);
                
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/albums?ids={idsParam}");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    
                    var response = await _httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    
                    var content = await response.Content.ReadAsStringAsync();
                    var albumsResponse = JsonSerializer.Deserialize<SpotifyMultipleAlbumsResponse>(
                        content, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    
                    if (albumsResponse?.Albums != null)
                    {
                        // Create lookup for faster access
                        var albumLookup = albumsResponse.Albums
                            .Where(a => a != null && !string.IsNullOrEmpty(a.Id))
                            .ToDictionary(a => a.Id!);
                            
                        // Update tracks with album data
                        foreach (var track in tracksMissingAlbumImages)
                        {
                            if (track.Album != null && !string.IsNullOrEmpty(track.Album.Id) && 
                                albumLookup.TryGetValue(track.Album.Id, out var albumDetails))
                            {
                                track.Album.Images = albumDetails.Images;
                                track.Album.Name = albumDetails.Name;
                                track.Album.ReleaseDate = albumDetails.ReleaseDate;
                            }
                            else if (track.Album != null && (track.Album.Images == null || !track.Album.Images.Any()))
                            {
                                // Provide default image if we couldn't get album data
                                track.Album.Images = new List<SpotifyImage> {
                                    new SpotifyImage { 
                                        Url = "/images/default-album.png",
                                        Height = 300,
                                        Width = 300
                                    }
                                };
                            }
                        }
                    }
                }
                catch
                {
                    // Continue with next batch if we can't get details for one batch
                    continue;
                }
            }
            
            // Ensure all tracks have at least a default image
            foreach (var track in tracks)
            {
                if (track.Album == null)
                {
                    track.Album = new SpotifyAlbumDto { 
                        Name = "Unknown Album",
                        Images = new List<SpotifyImage> {
                            new SpotifyImage { 
                                Url = "/images/default-album.png",
                                Height = 300,
                                Width = 300
                            }
                        }
                    };
                }
                else if (track.Album.Images == null || !track.Album.Images.Any())
                {
                    track.Album.Images = new List<SpotifyImage> {
                        new SpotifyImage { 
                            Url = "/images/default-album.png",
                            Height = 300,
                            Width = 300
                        }
                    };
                }
                
                // Ensure track has artists data
                if (track.Artists == null || !track.Artists.Any())
                {
                    track.Artists = new List<SpotifyArtistDto> {
                        new SpotifyArtistDto { Name = "Unknown Artist" }
                    };
                }
            }
        }

        // Add this class to support the new batched artist request
        public class SpotifyMultipleArtistsResponse
        {
            public List<SpotifyArtistDto>? Artists { get; set; }
        }
        
        // Add this class to support the multiple albums request
        public class SpotifyMultipleAlbumsResponse
        {
            public List<SpotifyAlbumDto>? Albums { get; set; }
        }
    }
}