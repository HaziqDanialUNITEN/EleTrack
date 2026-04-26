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
}