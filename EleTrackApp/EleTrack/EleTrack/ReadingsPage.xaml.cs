namespace EleTrack;

public partial class ReadingsPage : ContentPage
{
    public ReadingsPage()
    {
        InitializeComponent();

        // Hide the default navigation bar for a cleaner look
        Shell.SetNavBarIsVisible(this, false);
    }

    private async void OnAttachPhotoTapped(object sender, TappedEventArgs e)
    {
        // Simple mock action for now. 
        // In a real app, you would use MediaPicker to open the camera or gallery.
        await DisplayAlert("Attach Photo", "Camera / Gallery picker will open here.", "OK");
    }

    private async void OnSubmitReadingClicked(object sender, EventArgs e)
    {
        // Simple mock action for now.
        // In a real app, you would validate the Entry field and save to your database/backend.
        await DisplayAlert("Success", "Reading submitted successfully!", "OK");
    }
}