using MusicApp.Shared.Models;

namespace MusicApp.Client.Interfaces;

public interface ISpotifyClientService
{
    Task<SpotifyUserProfile> GetUserProfileAsync();
    Task<List<SpotifyPlaylistDto>> GetUserPlaylistsAsync();
    Task<List<SpotifyTrackDto>> GetPlaylistRecommendationsAsync(string playlistId);
}