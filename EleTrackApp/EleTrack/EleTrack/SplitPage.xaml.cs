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
            var housemates = await _firebaseService.GetHousematesAsync();
            var readings = await _firebaseService.GetReadingsAsync();

            if (housemates == null || housemates.Count == 0)
            {
                // FIX: Explicitly reset the UI when housemates are wiped
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TotalBillLabel.Text = "RM 0.00";
                    SplitAmongLabel.Text = "Split among 0 housemates";
                    PaymentProgressTextLabel.Text = "RM 0.00 / RM 0.00";
                    PaidCountLabel.Text = "0 of 0 paid";
                    PaymentProgressBar.Progress = 0;
                    BindableLayout.SetItemsSource(SplitCollection, null);
                });
                return;
            }

            double totalConsumed = readings?.Sum(r => r.Consumed) ?? 0;
            double totalModifiers = housemates.Sum(h => h.Modifier);
            double baseShare = totalConsumed / (totalModifiers > 0 ? totalModifiers : 1);

            int paidCount = 0;
            double totalPaid = 0;

            foreach (var mate in housemates)
            {
                mate.AmountDue = baseShare * mate.Modifier;

                if (mate.Status == "Paid")
                {
                    paidCount++;
                    totalPaid += mate.AmountDue;
                }
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                TotalBillLabel.Text = $"RM {totalConsumed:F2}";
                SplitAmongLabel.Text = $"Split among {housemates.Count} housemates";

                PaymentProgressTextLabel.Text = $"RM {totalPaid:F2} / RM {totalConsumed:F2}";
                PaidCountLabel.Text = $"{paidCount} of {housemates.Count} paid";

                if (totalConsumed > 0)
                {
                    PaymentProgressBar.Progress = totalPaid / totalConsumed;
                }
                else
                {
                    PaymentProgressBar.Progress = 0;
                }

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
            ShareButton.IsVisible = false;
            await Task.Delay(100);

            IScreenshotResult screenshot = await MainContent.CaptureAsync();

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
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ShareButton.IsVisible = true;
            });
        }
    }

    // NEW: Toggles the Paid/Pending status and saves it to Firebase
    private async void OnTogglePaidStatusTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is Housemate housemate)
        {
            // Toggle the status string
            housemate.Status = housemate.Status == "Paid" ? "Pending" : "Paid";

            try
            {
                // Update specific housemate in database
                await _firebaseService.UpdateHousemateAsync(housemate);

                // Refresh the whole page to update math and progress bars
                await LoadSplitData();
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("Error", $"Could not update status: {ex.Message}", "OK");
                });
            }
        }
    }
}