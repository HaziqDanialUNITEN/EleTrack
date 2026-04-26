using System.IO;
using EleTrack.Services;

namespace EleTrack;

public partial class SplitPage : ContentPage
{
    private readonly FirebaseService _firebaseService;

    public SplitPage()
    {
        InitializeComponent();
        Shell.SetNavBarIsVisible(this, false);
        _firebaseService = new FirebaseService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadSplitData();
    }

    private async Task LoadSplitData()
    {
        try
        {
            var bill = await _firebaseService.GetCurrentBillAsync();
            var housemates = await _firebaseService.GetHousematesAsync();

            if (housemates == null || housemates.Count == 0) return;

            // 1. Calculate the base mathematical values
            double totalModifiers = housemates.Sum(h => h.Modifier);
            double baseShare = (bill?.TotalConsumed ?? 0) / (totalModifiers > 0 ? totalModifiers : 1);

            int paidCount = 0;
            double totalPaid = 0;

            // 2. Assign dynamic amounts to each housemate
            foreach (var mate in housemates)
            {
                mate.AmountDue = baseShare * mate.Modifier;

                if (mate.Status == "Paid")
                {
                    paidCount++;
                    totalPaid += mate.AmountDue;
                }
            }

            // 3. Update the UI on the Main Thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TotalBillLabel.Text = $"RM {bill?.TotalConsumed ?? 0:F2}";
                SplitAmongLabel.Text = $"Split among {housemates.Count} housemates";

                PaymentProgressTextLabel.Text = $"RM {totalPaid:F2} / RM {bill?.TotalConsumed ?? 0:F2}";
                PaidCountLabel.Text = $"{paidCount} of {housemates.Count} paid";

                if (bill != null && bill.TotalConsumed > 0)
                {
                    PaymentProgressBar.Progress = totalPaid / bill.TotalConsumed;
                }
                else
                {
                    PaymentProgressBar.Progress = 0;
                }

                // Bind the list to the VerticalStackLayout using BindableLayout
                BindableLayout.SetItemsSource(SplitCollection, housemates);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading split data: {ex.Message}");
        }
    }

    private async void OnShareBillSummaryClicked(object sender, EventArgs e)
    {
        try
        {
            // Briefly hide the Share button so it isn't included in the final screenshot
            ShareButton.IsVisible = false;

            // Wait a tiny bit for the UI layout to update and hide the button
            await Task.Delay(100);

            // Capture the specific layout container (MainContent) instead of the entire screen
            // This captures the full scrollable height and naturally ignores the Navigation/Tab bars!
            IScreenshotResult screenshot = await MainContent.CaptureAsync();

            // Bring the button back immediately
            ShareButton.IsVisible = true;

            if (screenshot != null)
            {
                string cacheDir = FileSystem.CacheDirectory;
                string filePath = Path.Combine(cacheDir, "BillSummary.png");

                using (Stream stream = await screenshot.OpenReadAsync())
                using (FileStream fileStream = File.OpenWrite(filePath))
                {
                    await stream.CopyToAsync(fileStream);
                }

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Share Bill Summary",
                    File = new ShareFile(filePath)
                });
            }
        }
        catch (Exception)
        {
            // Restore button just in case an error occurs
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ShareButton.IsVisible = true;
            });
        }
    }
}