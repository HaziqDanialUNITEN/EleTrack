using EleTrack.Services;
using System.IO;

namespace EleTrack;

public partial class SettingsPage : ContentPage
{
    private readonly FirebaseService _firebaseService;

    public SettingsPage()
    {
        InitializeComponent();
        Shell.SetNavBarIsVisible(this, false);

        _firebaseService = new FirebaseService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshHousematesList();
    }

    private async Task RefreshHousematesList()
    {
        try
        {
            var housemates = await _firebaseService.GetHousematesAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                HousematesCollection.ItemsSource = housemates;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading housemates: {ex.Message}");
        }
    }

    private async void OnAddHousemateTapped(object sender, TappedEventArgs e)
    {
        string name = await DisplayPromptAsync("New Housemate", "Enter housemate's name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        string modifierStr = await DisplayPromptAsync("Modifier", "Enter usage modifier (e.g., 1.0, 1.5):", initialValue: "1.0", keyboard: Keyboard.Numeric);
        if (!double.TryParse(modifierStr, out double modifier))
        {
            await DisplayAlert("Invalid Input", "Please enter a valid number for the modifier.", "OK");
            return;
        }

        var newHousemate = new Housemate
        {
            Name = name,
            Modifier = modifier,
            Status = "Pending",
            AmountDue = 0
        };

        await _firebaseService.AddHousemateAsync(newHousemate);
        await RefreshHousematesList();
    }

    private async void OnEditHousemateTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is Housemate housemate)
        {
            string newModifierStr = await DisplayPromptAsync("Edit Modifier", $"Update modifier for {housemate.Name}:", initialValue: housemate.Modifier.ToString(), keyboard: Keyboard.Numeric);

            if (double.TryParse(newModifierStr, out double newModifier) && newModifier != housemate.Modifier)
            {
                await _firebaseService.DeleteHousemateAsync(housemate.Id);
                housemate.Modifier = newModifier;
                await _firebaseService.AddHousemateAsync(housemate);
                await RefreshHousematesList();
            }
        }
    }

    private async void OnDeleteHousemateTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is Housemate housemate)
        {
            bool confirm = await DisplayAlert("Remove Housemate", $"Are you sure you want to remove {housemate.Name}?", "Yes", "Cancel");
            if (confirm)
            {
                await _firebaseService.DeleteHousemateAsync(housemate.Id);
                await RefreshHousematesList();
            }
        }
    }

    private async void OnSetInitialCreditTapped(object sender, TappedEventArgs e)
    {
        string creditStr = await DisplayPromptAsync("Initial Credit", "Enter the initial meter credit balance (e.g., after a fresh top-up):", keyboard: Keyboard.Numeric);
        if (string.IsNullOrWhiteSpace(creditStr)) return;

        if (double.TryParse(creditStr, out double initialCredit))
        {
            var newReading = new MeterReading
            {
                CreditValue = initialCredit,
                Consumed = 0, // Baseline reading has 0 consumption
                Date = DateTime.UtcNow,
                SubmittedBy = "System (Initial)",
                PhotoPath = null
            };

            await _firebaseService.AddReadingAsync(newReading);

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Success", $"Initial baseline credit set to RM {initialCredit:F2}.", "OK");
            });
        }
        else
        {
            await DisplayAlert("Invalid Input", "Please enter a valid numeric amount.", "OK");
        }
    }

    private async void OnResetDataTapped(object sender, TappedEventArgs e)
    {
        bool confirm = await DisplayAlert("⚠️ WARNING", "This will permanently delete ALL housemates, meter readings, bills, and local photos. This action cannot be undone. Are you absolutely sure?", "Yes, Wipe Everything", "Cancel");

        if (confirm)
        {
            try
            {
                // 1. Wipe Firebase Database
                await _firebaseService.ResetAllDataAsync();

                // 2. Wipe Local Cached Photos
                string cacheDir = FileSystem.CacheDirectory;
                var files = Directory.GetFiles(cacheDir);

                foreach (var file in files)
                {
                    // Only delete image files so we don't accidentally break MAUI's internal caches
                    if (file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                        file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                    {
                        try { File.Delete(file); } catch { /* Ignore if file is locked */ }
                    }
                }

                // 3. Refresh UI
                await RefreshHousematesList();

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("Reset Complete", "All app data and photos have been successfully wiped.", "OK");
                });
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("Error", $"Could not reset data: {ex.Message}", "OK");
                });
            }
        }
    }

    private async void OnAboutTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new AboutPage());
    }
}