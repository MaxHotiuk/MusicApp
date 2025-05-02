using MusicApp.Shared.Models;

namespace MusicApp.Interfaces
{
    public interface ISpotifyService
    {
        string GetAuthorizationUrl(string state);
        Task<SpotifyTokenResponse> ExchangeCodeForTokenAsync(string code, string redirectUri);
        Task<SpotifyTokenResponse> RefreshTokenAsync(string refreshToken);
        Task<SpotifyUserProfile> GetUserProfileAsync(string accessToken);
        Task<List<SpotifyPlaylistDto>> GetUserPlaylistsAsync(string accessToken);
    }
}