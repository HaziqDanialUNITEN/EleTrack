namespace EleTrack;

public partial class AboutPage : ContentPage
{
    public AboutPage()
    {
        InitializeComponent();

        // Hide default navigation bar because we are using a custom header with a back arrow
        Shell.SetNavBarIsVisible(this, false);

        // Hide the bottom TabBar on the About page since it's a sub-page
        Shell.SetTabBarIsVisible(this, false);
    }

    private async void OnBackTapped(object sender, TappedEventArgs e)
    {
        // Navigate back to the previous page (SettingsPage)
        await Navigation.PopAsync();
    }

    private async void OnEmailTapped(object sender, TappedEventArgs e)
    {
        try
        {
            if (Email.Default.IsComposeSupported)
            {
                string subject = "EleTrack Support Request";
                string body = "Hello EleTrack Support,\n\nI need help with...";
                string[] recipients = new[] { "support@eletrack.app" };

                var message = new EmailMessage
                {
                    Subject = subject,
                    Body = body,
                    To = new List<string>(recipients)
                };

                await Email.Default.ComposeAsync(message);
            }
            else
            {
                // Fallback if the user hasn't set up an email app on their phone
                await Launcher.Default.OpenAsync("mailto:support@eletrack.app");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not open email app: {ex.Message}", "OK");
        }
    }
}