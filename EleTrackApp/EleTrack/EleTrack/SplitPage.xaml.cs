using System.IO;

namespace EleTrack;

public partial class SplitPage : ContentPage
{
    public SplitPage()
    {
        InitializeComponent();

        // Hide the default top navigation bar for a cleaner look
        Shell.SetNavBarIsVisible(this, false);
    }

    private async void OnShareBillSummaryClicked(object sender, EventArgs e)
    {
        try
        {
            if (Screenshot.Default.IsCaptureSupported)
            {
                // 1. Temporarily hide the bottom TabBar to exclude it from the screenshot
                Shell.SetTabBarIsVisible(this, false);

                // Wait a tiny bit for the UI layout to update and actually hide the bar
                await Task.Delay(100);

                // 2. Capture the screen
                IScreenshotResult screenshot = await Screenshot.Default.CaptureAsync();

                // 3. Bring the bottom TabBar back immediately after capture
                Shell.SetTabBarIsVisible(this, true);

                // 4. Save screenshot to a temporary file
                string cacheDir = FileSystem.CacheDirectory;
                string filePath = Path.Combine(cacheDir, "BillSummary.png");

                using (Stream stream = await screenshot.OpenReadAsync())
                using (FileStream fileStream = File.OpenWrite(filePath))
                {
                    await stream.CopyToAsync(fileStream);
                }

                // 5. Open the native OS Share dialog (WhatsApp, Telegram, etc.)
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Share Bill Summary",
                    File = new ShareFile(filePath)
                });
            }
            else
            {
                await DisplayAlert("Error", "Screenshot capture is not supported on this device.", "OK");
            }
        }
        catch (Exception ex)
        {
            // Restore TabBar just in case an error occurs during capture
            Shell.SetTabBarIsVisible(this, true);
            await DisplayAlert("Error", $"Failed to share bill summary: {ex.Message}", "OK");
        }
    }
}