using EleTrack.Services;

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

            // CRITICAL FIX: Ensure CollectionView ItemsSource updates on the Main Thread
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

    private async void OnAboutTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new AboutPage());
    }
}