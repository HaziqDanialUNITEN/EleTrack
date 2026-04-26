using EleTrack.Services;

namespace EleTrack;

public partial class MainPage : ContentPage
{
    private readonly FirebaseService _firebaseService;

    public MainPage()
    {
        InitializeComponent();
        Shell.SetNavBarIsVisible(this, false);
        _firebaseService = new FirebaseService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDashboardData();
    }

    private async Task LoadDashboardData()
    {
        try
        {
            var readings = await _firebaseService.GetReadingsAsync();
            var currentBill = await _firebaseService.GetCurrentBillAsync();

            // CRITICAL FIX: Ensure UI is updated on the Main Thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (readings != null && readings.Count > 0)
                {
                    var latestReading = readings.First(); // Newest reading
                    var oldestReading = readings.Last();  // Oldest reading for baseline calculation

                    CurrentCreditLabel.Text = $"RM {latestReading.CreditValue:F2}";
                    LastUpdatedLabel.Text = $"Last updated: {currentBill.LastUpdated:MMM d, yyyy}";

                    TotalConsumedLabel.Text = $"RM {currentBill.TotalConsumed:F2}";

                    double startedCredit = oldestReading.CreditValue + currentBill.TotalConsumed; // Rough estimate of top-up
                    StartedCreditLabel.Text = $"Started: RM {startedCredit:F2}";
                    RemainingCreditLabel.Text = $"Remaining: RM {latestReading.CreditValue:F2}";

                    // Progress bar logic
                    if (startedCredit > 0)
                    {
                        double percentage = currentBill.TotalConsumed / startedCredit;
                        UsageProgressBar.Progress = percentage;
                        UsagePercentageLabel.Text = $"{(percentage * 100):F1}%";
                    }

                    // Averages
                    double daysTracked = (DateTime.UtcNow - oldestReading.Date).TotalDays;
                    if (daysTracked < 1) daysTracked = 1;

                    DaysTrackedLabel.Text = Math.Round(daysTracked).ToString();
                    DailyAverageLabel.Text = $"RM {(currentBill.TotalConsumed / daysTracked):F2}";
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading main page data: {ex.Message}");
        }
    }
}