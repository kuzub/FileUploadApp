namespace FileUploadApp.Services;

public class NotificationService : INotificationService
{
    public async Task ShowAlertAsync(string title, string message, string cancel = "OK")
    {
        if (Application.Current?.MainPage != null)
        {
            await Application.Current.MainPage.DisplayAlert(title, message, cancel);
        }
    }

    public async Task<bool> ShowConfirmationAsync(string title, string message, string accept = "OK", string cancel = "Cancel")
    {
        if (Application.Current?.MainPage != null)
        {
            return await Application.Current.MainPage.DisplayAlert(title, message, accept, cancel);
        }

        return false;
    }
}