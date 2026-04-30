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

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (readings != null && readings.Count > 0)
                {
                    var latestReading = readings.First();
                    var oldestReading = readings.Last();

                    double totalConsumed = readings.Sum(r => r.Consumed);
                    double startedCredit = latestReading.CreditValue + totalConsumed;

                    CurrentCreditLabel.Text = $"RM {latestReading.CreditValue:F2}";
                    LastUpdatedLabel.Text = $"Last updated: {latestReading.LocalDate:MMM d, yyyy}";

                    TotalConsumedLabel.Text = $"RM {totalConsumed:F2}";
                    StartedCreditLabel.Text = $"Started: RM {startedCredit:F2}";
                    RemainingCreditLabel.Text = $"Remaining: RM {latestReading.CreditValue:F2}";

                    if (startedCredit > 0)
                    {
                        double percentage = totalConsumed / startedCredit;
                        UsageProgressBar.Progress = percentage;
                        UsagePercentageLabel.Text = $"{(percentage * 100):F1}%";
                    }
                    else
                    {
                        UsageProgressBar.Progress = 0;
                        UsagePercentageLabel.Text = "0%";
                    }

                    double daysTracked = (DateTime.UtcNow - oldestReading.Date).TotalDays;
                    if (daysTracked < 1) daysTracked = 1;

                    DaysTrackedLabel.Text = Math.Round(daysTracked).ToString();
                    DailyAverageLabel.Text = $"RM {(totalConsumed / daysTracked):F2}";
                }
                else
                {
                    // FIX: Explicitly reset the UI when the database is wiped
                    CurrentCreditLabel.Text = "RM 0.00";
                    LastUpdatedLabel.Text = "Last updated: -";
                    TotalConsumedLabel.Text = "RM 0.00";
                    StartedCreditLabel.Text = "Started: RM 0.00";
                    RemainingCreditLabel.Text = "Remaining: RM 0.00";
                    UsageProgressBar.Progress = 0;
                    UsagePercentageLabel.Text = "0%";
                    DaysTrackedLabel.Text = "0";
                    DailyAverageLabel.Text = "RM 0.00";
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading main page data: {ex.Message}");
        }
    }
}