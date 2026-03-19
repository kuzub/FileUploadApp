using FileUploadApp.Models;

namespace FileUploadApp.Services;

public class MockAuthenticationService : IAuthenticationService
{
    private const string TokenKey = "auth_token";
    private const string ExpirationKey = "token_expiration";

    public async Task<TokenResult?> AuthenticateUserAsync(string username, string password)
    {
        await Task.Delay(2000); // Simulate network delay

        // Mock authentication logic - accepts any non-empty credentials
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await ClearTokenAsync();
            return null;
        }

        // Simulate successful authentication
        var token = new TokenResult
        {
            AccessToken = $"mock_token_{Guid.NewGuid():N}",
            Expiration = DateTime.UtcNow.AddHours(1)
        };

        // Store token securely
        await StoreTokenAsync(token);

        return token;
    }

    public async Task<bool> IsUserAuthenticatedAsync()
    {
        await Task.Delay(2000); // Simulate network delay

        try
        {
            // Retrieve token from secure storage
            var token = await RetrieveTokenAsync();

            if (token == null)
            {
                return false;
            }

            // Check if token has expired
            if (token.Expiration <= DateTime.UtcNow)
            {
                await ClearTokenAsync();
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task StoreTokenAsync(TokenResult token)
    {
        try
        {
            await SecureStorage.SetAsync(TokenKey, token.AccessToken ?? string.Empty);
            await SecureStorage.SetAsync(ExpirationKey, token.Expiration.ToString("O"));
        }
        catch (Exception ex)
        {
            // Handle storage exceptions (e.g., platform not supported)
            System.Diagnostics.Debug.WriteLine($"Failed to store token: {ex.Message}");
        }
    }

    public async Task<TokenResult?> RetrieveTokenAsync()
    {
        await Task.Delay(2000); // Simulate network delay

        try
        {
            var accessToken = await SecureStorage.GetAsync(TokenKey);
            var expirationString = await SecureStorage.GetAsync(ExpirationKey);

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(expirationString))
            {
                return null;
            }

            if (DateTime.TryParse(expirationString, out var expiration))
            {
                return new TokenResult
                {
                    AccessToken = accessToken,
                    Expiration = expiration
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to retrieve token: {ex.Message}");
            return null;
        }
    }

    private async Task ClearTokenAsync()
    {
        try
        {
            SecureStorage.Remove(TokenKey);
            SecureStorage.Remove(ExpirationKey);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to clear token: {ex.Message}");
        }
    }
}