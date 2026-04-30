using EleTrack.Services;
using System.IO;

namespace EleTrack;

public partial class ReadingsPage : ContentPage
{
    private enum FilterMode { Daily, Weekly, All }

    private readonly FirebaseService _firebaseService;
    private string _currentPhotoPath = null;

    // Caching properties for filters
    private List<MeterReading> _allReadingsCache = new List<MeterReading>();
    private FilterMode _currentFilterMode = FilterMode.Weekly;
    private DateTime _selectedFilterDate = DateTime.Today;

    public ReadingsPage()
    {
        InitializeComponent();
        Shell.SetNavBarIsVisible(this, false);

        _firebaseService = new FirebaseService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Ensure the pickers default to the current local date/time every time the page is opened
        ReadingDatePicker.Date = DateTime.Today;
        ReadingTimePicker.Time = DateTime.Now.TimeOfDay;

        await RefreshReadingsList();
    }

    private async Task RefreshReadingsList()
    {
        try
        {
            var readings = await _firebaseService.GetReadingsAsync();
            _allReadingsCache = readings?.ToList() ?? new List<MeterReading>();

            ApplyFilter();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading readings: {ex.Message}");
        }
    }

    // ==========================================
    // FILTERING LOGIC
    // ==========================================

    private void ApplyFilter()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_currentFilterMode == FilterMode.All)
            {
                DateNavigatorLayout.IsVisible = false;
                ReadingsCollection.ItemsSource = _allReadingsCache;
            }
            else if (_currentFilterMode == FilterMode.Daily)
            {
                DateNavigatorLayout.IsVisible = true;

                // Show "Today", "Yesterday", or the specific date
                if (_selectedFilterDate.Date == DateTime.Today)
                    CurrentFilterDateLabel.Text = "Today";
                else if (_selectedFilterDate.Date == DateTime.Today.AddDays(-1))
                    CurrentFilterDateLabel.Text = "Yesterday";
                else
                    CurrentFilterDateLabel.Text = _selectedFilterDate.ToString("MMM d, yyyy");

                var filtered = _allReadingsCache.Where(r => r.LocalDate.Date == _selectedFilterDate.Date).ToList();
                ReadingsCollection.ItemsSource = filtered;
            }
            else if (_currentFilterMode == FilterMode.Weekly)
            {
                DateNavigatorLayout.IsVisible = true;

                // Calculate week start (Monday) and end (Sunday)
                int diff = (7 + (_selectedFilterDate.DayOfWeek - DayOfWeek.Monday)) % 7;
                DateTime weekStart = _selectedFilterDate.AddDays(-1 * diff).Date;
                DateTime weekEnd = weekStart.AddDays(6).Date;

                CurrentFilterDateLabel.Text = $"{weekStart:MMM d} - {weekEnd:MMM d}";

                var filtered = _allReadingsCache.Where(r => r.LocalDate.Date >= weekStart && r.LocalDate.Date <= weekEnd).ToList();
                ReadingsCollection.ItemsSource = filtered;
            }
        });
    }

    private void OnPreviousDateTapped(object sender, EventArgs e)
    {
        if (_currentFilterMode == FilterMode.Daily)
            _selectedFilterDate = _selectedFilterDate.AddDays(-1);
        else if (_currentFilterMode == FilterMode.Weekly)
            _selectedFilterDate = _selectedFilterDate.AddDays(-7);

        ApplyFilter();
    }

    private void OnNextDateTapped(object sender, EventArgs e)
    {
        if (_currentFilterMode == FilterMode.Daily)
            _selectedFilterDate = _selectedFilterDate.AddDays(1);
        else if (_currentFilterMode == FilterMode.Weekly)
            _selectedFilterDate = _selectedFilterDate.AddDays(7);

        ApplyFilter();
    }

    // ==========================================
    // FILTER TAB UI UPDATES
    // ==========================================

    private void OnDailyFilterTapped(object sender, EventArgs e)
    {
        _currentFilterMode = FilterMode.Daily;
        UpdateFilterTabUI(DailyTabBorder, DailyTabLabel);
        ApplyFilter();
    }

    private void OnWeeklyFilterTapped(object sender, EventArgs e)
    {
        _currentFilterMode = FilterMode.Weekly;
        UpdateFilterTabUI(WeeklyTabBorder, WeeklyTabLabel);
        ApplyFilter();
    }

    private void OnAllFilterTapped(object sender, EventArgs e)
    {
        _currentFilterMode = FilterMode.All;
        UpdateFilterTabUI(AllTabBorder, AllTabLabel);
        ApplyFilter();
    }

    private void UpdateFilterTabUI(Border activeBorder, Label activeLabel)
    {
        // Reset all tabs to inactive styling
        DailyTabBorder.BackgroundColor = Colors.Transparent;
        DailyTabBorder.Shadow = null;
        DailyTabLabel.FontAttributes = FontAttributes.None;
        DailyTabLabel.TextColor = Color.FromArgb("#64748B");

        WeeklyTabBorder.BackgroundColor = Colors.Transparent;
        WeeklyTabBorder.Shadow = null;
        WeeklyTabLabel.FontAttributes = FontAttributes.None;
        WeeklyTabLabel.TextColor = Color.FromArgb("#64748B");

        AllTabBorder.BackgroundColor = Colors.Transparent;
        AllTabBorder.Shadow = null;
        AllTabLabel.FontAttributes = FontAttributes.None;
        AllTabLabel.TextColor = Color.FromArgb("#64748B");

        // Apply active styling to the selected tab
        activeBorder.BackgroundColor = Colors.White;
        activeBorder.Shadow = new Shadow { Brush = Brush.Black, Offset = new Point(0, 2), Radius = 4, Opacity = 0.05f };
        activeLabel.FontAttributes = FontAttributes.Bold;
        activeLabel.TextColor = Color.FromArgb("#1E293B");
    }

    // ==========================================
    // EXISTING PHOTO & SUBMIT LOGIC
    // ==========================================

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
            DateTime selectedDate = (DateTime)ReadingDatePicker.Date;
            TimeSpan selectedTime = (TimeSpan)ReadingTimePicker.Time;

            DateTime selectedLocalTime = selectedDate + selectedTime;
            DateTime utcDateTime = selectedLocalTime.ToUniversalTime();

            double consumed = 0;
            var pastReadings = await _firebaseService.GetReadingsAsync();

            var previousReading = pastReadings.FirstOrDefault(r => r.Date < utcDateTime);

            if (previousReading != null)
            {
                consumed = previousReading.CreditValue - currentCredit;
                if (consumed < 0) consumed = 0;
            }

            var newReading = new MeterReading
            {
                CreditValue = currentCredit,
                Consumed = consumed,
                Date = utcDateTime,
                SubmittedBy = "Me",
                PhotoPath = _currentPhotoPath
            };

            await _firebaseService.AddReadingAsync(newReading);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                CreditEntry.Text = string.Empty;
                ReadingDatePicker.Date = DateTime.Today;
                ReadingTimePicker.Time = DateTime.Now.TimeOfDay;
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