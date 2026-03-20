namespace FileUploadApp.Services;

public interface INotificationService
{
    /// <summary>
    /// Displays an alert dialog with a message and an OK button
    /// </summary>
    Task ShowAlertAsync(string title, string message, string cancel = "OK");

    /// <summary>
    /// Displays a confirmation dialog with Accept and Cancel buttons
    /// </summary>
    Task<bool> ShowConfirmationAsync(string title, string message, string accept = "OK", string cancel = "Cancel");
}