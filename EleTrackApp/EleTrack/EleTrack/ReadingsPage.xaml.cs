using EleTrack.Services;
using System.IO;

namespace EleTrack;

public partial class ReadingsPage : ContentPage
{
    private readonly FirebaseService _firebaseService;
    private string _currentPhotoPath = null;

    public ReadingsPage()
    {
        InitializeComponent();
        Shell.SetNavBarIsVisible(this, false);

        _firebaseService = new FirebaseService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshReadingsList();
    }

    private async Task RefreshReadingsList()
    {
        try
        {
            var readings = await _firebaseService.GetReadingsAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                ReadingsCollection.ItemsSource = readings;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading readings: {ex.Message}");
        }
    }

    private async void OnAttachPhotoTapped(object sender, TappedEventArgs e)
    {
        try
        {
            string action = await DisplayActionSheet("Attach Meter Photo", "Cancel", null, "Take Photo with Camera", "Choose from Gallery");

            FileResult photo = null;

            if (action == "Take Photo with Camera")
            {
                if (MediaPicker.Default.IsCaptureSupported)
                    photo = await MediaPicker.Default.CapturePhotoAsync();
                else
                    await DisplayAlert("Not Supported", "Your device does not support camera capture.", "OK");
            }
            else if (action == "Choose from Gallery")
            {
                photo = await MediaPicker.Default.PickPhotoAsync();
            }

            if (photo != null)
            {
                string localFilePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);

                using Stream sourceStream = await photo.OpenReadAsync();
                using FileStream localFileStream = File.OpenWrite(localFilePath);
                await sourceStream.CopyToAsync(localFileStream);

                _currentPhotoPath = localFilePath;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    PhotoPreviewImage.Source = ImageSource.FromFile(localFilePath);
                    PhotoPreviewContainer.IsVisible = true;
                    AttachPhotoLabel.Text = "Change Photo";
                });
            }
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Error", $"Could not attach photo: {ex.Message}", "OK");
            });
        }
    }

    private void OnRemovePhotoTapped(object sender, TappedEventArgs e)
    {
        _currentPhotoPath = null;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            PhotoPreviewImage.Source = null;
            PhotoPreviewContainer.IsVisible = false;
            AttachPhotoLabel.Text = "Attach Photo";
        });
    }

    // NEW: Handles tapping the square thumbnail in the history list
    private void OnViewPhotoTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is string photoPath && !string.IsNullOrEmpty(photoPath))
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                FullScreenImage.Source = ImageSource.FromFile(photoPath);
                FullScreenPhotoOverlay.IsVisible = true;
            });
        }
    }

    // NEW: Closes the full-screen photo overlay
    private void OnCloseFullScreenPhotoTapped(object sender, TappedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            FullScreenPhotoOverlay.IsVisible = false;
            FullScreenImage.Source = null; // Clear memory
        });
    }

    private async void OnSubmitReadingClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CreditEntry.Text) || !double.TryParse(CreditEntry.Text, out double currentCredit))
        {
            await DisplayAlert("Invalid Input", "Please enter a valid credit value.", "OK");
            return;
        }

        try
        {
            double consumed = 0;
            var pastReadings = await _firebaseService.GetReadingsAsync();

            if (pastReadings != null && pastReadings.Count > 0)
            {
                var lastReading = pastReadings[0];
                consumed = lastReading.CreditValue - currentCredit;
                if (consumed < 0) consumed = 0;
            }

            var newReading = new MeterReading
            {
                CreditValue = currentCredit,
                Consumed = consumed,
                Date = DateTime.UtcNow,
                SubmittedBy = "Me",
                PhotoPath = _currentPhotoPath
            };

            await _firebaseService.AddReadingAsync(newReading);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                CreditEntry.Text = string.Empty;
                _currentPhotoPath = null;
                PhotoPreviewImage.Source = null;
                PhotoPreviewContainer.IsVisible = false;
                AttachPhotoLabel.Text = "Attach Photo";
            });

            await RefreshReadingsList();

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Success", "Meter reading submitted and saved to cloud!", "OK");
            });
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Error", $"Failed to submit reading: {ex.Message}", "OK");
            });
        }
    }
}