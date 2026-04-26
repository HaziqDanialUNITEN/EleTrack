namespace EleTrack;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        // Hide the default navigation bar so our custom header looks clean
        Shell.SetNavBarIsVisible(this, false);
    }
}