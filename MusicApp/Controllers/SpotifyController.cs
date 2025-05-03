using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MusicApp.Data;
using MusicApp.Interfaces;
using Microsoft.AspNetCore.Authorization;


namespace MusicApp.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class SpotifyController : ControllerBase
    {
        private readonly ISpotifyService _spotifyService;
        private readonly ApplicationDbContext _context;

        public SpotifyController(
            ISpotifyService spotifyService,
            ApplicationDbContext context)
        {
            _spotifyService = spotifyService;
            _context = context;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetUserProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userGuid);
            if (user == null)
                return NotFound("User not found");

            // Check if token is expired, refresh if needed
            if (user.SpotifyTokenExpiry <= DateTime.UtcNow)
            {
                try
                {
                    var tokenResponse = await _spotifyService.RefreshTokenAsync(user.SpotifyRefreshToken!);
                    user.SpotifyAccessToken = tokenResponse.AccessToken;
                    if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
                        user.SpotifyRefreshToken = tokenResponse.RefreshToken;
                    user.SpotifyTokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    return Unauthorized($"Failed to refresh token: {ex.Message}");
                }
            }

            try
            {
                var profile = await _spotifyService.GetUserProfileAsync(user.SpotifyAccessToken!);
                return Ok(profile);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving Spotify profile: {ex.Message}");
            }
        }

        [HttpGet("playlists")]
        public async Task<IActionResult> GetUserPlaylists()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userGuid);
            if (user == null)
                return NotFound("User not found");

            // Check if token is expired, refresh if needed
            if (user.SpotifyTokenExpiry <= DateTime.UtcNow)
            {
                try
                {
                    var tokenResponse = await _spotifyService.RefreshTokenAsync(user.SpotifyRefreshToken!);
                    user.SpotifyAccessToken = tokenResponse.AccessToken;
                    if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
                        user.SpotifyRefreshToken = tokenResponse.RefreshToken;
                    user.SpotifyTokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    return Unauthorized($"Failed to refresh token: {ex.Message}");
                }
            }

            try
            {
                var playlists = await _spotifyService.GetUserPlaylistsAsync(user.SpotifyAccessToken!);
                return Ok(playlists);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving Spotify playlists: {ex.Message}");
            }
        }

        [HttpGet("playlists/{playlistId}/recommendations")]
        public async Task<IActionResult> GetPlaylistRecommendations(string playlistId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userGuid);
            if (user == null)
                return NotFound("User not found");

            // Check if token is expired, refresh if needed
            if (user.SpotifyTokenExpiry <= DateTime.UtcNow)
            {
                try
                {
                    var tokenResponse = await _spotifyService.RefreshTokenAsync(user.SpotifyRefreshToken!);
                    user.SpotifyAccessToken = tokenResponse.AccessToken;
                    if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
                        user.SpotifyRefreshToken = tokenResponse.RefreshToken;
                    user.SpotifyTokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    return Unauthorized($"Failed to refresh token: {ex.Message}");
                }
            }

            try
            {
                var recommendations = await _spotifyService.GetPlaylistRecommendationsAsync(user.SpotifyAccessToken!, playlistId);
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving recommendations: {ex.Message}");
            }
        }
    }
}