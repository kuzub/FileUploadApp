using FileUploadApp.Models;

namespace FileUploadApp.Services;

public interface IAuthenticationService
{
    Task<TokenResult?> AuthenticateUserAsync(string username, string password);

    Task<TokenResult?> RetrieveTokenAsync();

    Task<bool> IsUserAuthenticatedAsync();

    //Task LogoutUserAsync();

}
