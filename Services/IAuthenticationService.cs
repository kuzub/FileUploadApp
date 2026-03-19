using FileUploadApp.Models;

namespace FileUploadApp.Services;

public interface IAuthenticationService
{
    Task<TokenResult?> AuthenticateUserAsync(string username, string password);

    Task<bool> IsUserAuthenticatedAsync();
    //Task LogoutUserAsync();

}
