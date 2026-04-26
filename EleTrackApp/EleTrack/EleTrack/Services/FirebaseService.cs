using Firebase.Database;
using Firebase.Database.Query;
using System.Collections.ObjectModel;

namespace EleTrack.Services;

// ==========================================
// 1. DATA MODELS
// ==========================================

public class Housemate
{
    public string Id { get; set; }
    public string Name { get; set; }
    public double Modifier { get; set; }
    public string Status { get; set; }
    public double AmountDue { get; set; }
}

public class MeterReading
{
    public string Id { get; set; }
    public DateTime Date { get; set; }

    // FIX: Automatically converts the UTC database time to the phone's local timezone
    public DateTime LocalDate => Date.ToLocalTime();

    public double CreditValue { get; set; }
    public double Consumed { get; set; }
    public string SubmittedBy { get; set; }

    // ADDED: This allows the app to store the local photo path in the database
    public string PhotoPath { get; set; }

    // HELPER: Used by XAML to easily check if a photo exists without needing a complex ValueConverter
    public bool HasPhoto => !string.IsNullOrEmpty(PhotoPath);
}

public class CurrentBill
{
    public double TotalConsumed { get; set; }
    public DateTime LastUpdated { get; set; }
}

// ==========================================
// 2. THE SERVICE CLASS
// ==========================================

public class FirebaseService
{
    // TODO: Replace this URL with your actual Firebase Realtime Database URL
    // Make sure it ends with a forward slash (/)
    private readonly string _firebaseUrl = "https://eletrack-e0ba8-default-rtdb.asia-southeast1.firebasedatabase.app/";
    private readonly FirebaseClient _firebaseClient;

    public FirebaseService()
    {
        _firebaseClient = new FirebaseClient(_firebaseUrl);
    }

    // --- HOUSEMATES ---

    public async Task<ObservableCollection<Housemate>> GetHousematesAsync()
    {
        var housemates = await _firebaseClient
            .Child("housemates")
            .OnceAsync<Housemate>();

        var list = new ObservableCollection<Housemate>();
        if (housemates != null)
        {
            foreach (var item in housemates)
            {
                item.Object.Id = item.Key; // Save the Firebase unique key
                list.Add(item.Object);
            }
        }
        return list;
    }

    public async Task AddHousemateAsync(Housemate housemate)
    {
        await _firebaseClient
            .Child("housemates")
            .PostAsync(housemate);
    }

    public async Task DeleteHousemateAsync(string id)
    {
        await _firebaseClient
            .Child("housemates")
            .Child(id)
            .DeleteAsync();
    }

    // --- READINGS ---

    public async Task<ObservableCollection<MeterReading>> GetReadingsAsync()
    {
        var readings = await _firebaseClient
            .Child("readings")
            .OrderByKey()
            .OnceAsync<MeterReading>();

        var list = new ObservableCollection<MeterReading>();
        if (readings != null)
        {
            // Reverse to show the latest reading at the top of the list
            foreach (var item in readings.Reverse())
            {
                item.Object.Id = item.Key;
                list.Add(item.Object);
            }
        }
        return list;
    }

    public async Task AddReadingAsync(MeterReading reading)
    {
        await _firebaseClient
            .Child("readings")
            .PostAsync(reading);
    }

    // --- CURRENT BILL ---

    public async Task<CurrentBill> GetCurrentBillAsync()
    {
        var bill = await _firebaseClient
            .Child("currentBill")
            .OnceSingleAsync<CurrentBill>();

        // If there's no bill data yet, return a new empty one
        return bill ?? new CurrentBill { TotalConsumed = 0, LastUpdated = DateTime.UtcNow };
    }

    public async Task UpdateCurrentBillAsync(CurrentBill bill)
    {
        // PutAsync will overwrite the existing current bill with the updated values
        await _firebaseClient
            .Child("currentBill")
            .PutAsync(bill);
    }
}