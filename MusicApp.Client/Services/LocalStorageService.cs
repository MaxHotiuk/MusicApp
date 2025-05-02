using Microsoft.JSInterop;
using MusicApp.Client.Interfaces;
using System.Text.Json;

namespace MusicApp.Client.Services;

public class LocalStorageService : ILocalStorageService
{
    private readonly IJSRuntime _jsRuntime;

    public LocalStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<T> GetItemAsync<T>(string key)
    {
        // Check if we're running in a context where JS interop is available
        if (_jsRuntime is IJSInProcessRuntime)
        {
            try
            {
                var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
                if (string.IsNullOrEmpty(json))
                    return default!;

                return JsonSerializer.Deserialize<T>(json) ?? default!;
            }
            catch (InvalidOperationException)
            {
                // JS interop not available (static rendering)
                return default!;
            }
        }
        
        // Return default if JS interop isn't available
        return default!;
    }

    public async Task SetItemAsync<T>(string key, T value)
    {
        // Only attempt JS interop if it's available
        if (_jsRuntime is IJSInProcessRuntime)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
            }
            catch (InvalidOperationException)
            {
                // JS interop not available, silently ignore
            }
        }
    }

    public async Task RemoveItemAsync(string key)
    {
        // Only attempt JS interop if it's available
        if (_jsRuntime is IJSInProcessRuntime)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
            }
            catch (InvalidOperationException)
            {
                // JS interop not available, silently ignore
            }
        }
    }
}
