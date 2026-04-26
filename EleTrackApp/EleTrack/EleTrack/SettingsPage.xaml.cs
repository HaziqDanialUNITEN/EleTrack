namespace EleTrack;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();

        // Hide default navigation bar
        Shell.SetNavBarIsVisible(this, false);
    }

    private async void OnAboutTapped(object sender, TappedEventArgs e)
    {
        // Navigate to the About Page
        await Navigation.PushAsync(new AboutPage());
    }
}