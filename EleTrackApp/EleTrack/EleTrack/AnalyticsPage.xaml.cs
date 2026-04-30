using EleTrack.Services;

namespace EleTrack;

public partial class AnalyticsPage : ContentPage
{
    private LineChartDrawable _chartDrawable;
    private readonly FirebaseService _firebaseService;
    private bool _isWeeklyView = false;

    // NEW: Variables to track the currently selected time window
    private DateTime _currentFilterDate = DateTime.Today;
    private List<MeterReading> _allReadingsCache = new List<MeterReading>();

    public AnalyticsPage()
    {
        InitializeComponent();
        Shell.SetNavBarIsVisible(this, false);

        _firebaseService = new FirebaseService();
        _chartDrawable = new LineChartDrawable();
        ChartGraphicsView.Drawable = _chartDrawable;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadChartData();
    }

    // ==========================================
    // UI NAVIGATION EVENTS
    // ==========================================

    private void OnDailyTapped(object sender, TappedEventArgs e)
    {
        _isWeeklyView = false;
        _currentFilterDate = DateTime.Today; // Reset anchor to today

        DailyTabBorder.BackgroundColor = Colors.White;
        DailyTabLabel.FontAttributes = FontAttributes.Bold;
        DailyTabLabel.TextColor = Color.FromArgb("#1E293B");
        DailyTabBorder.Shadow = new Shadow { Brush = Brush.Black, Offset = new Point(0, 2), Radius = 4, Opacity = 0.05f };

        WeeklyTabBorder.BackgroundColor = Colors.Transparent;
        WeeklyTabLabel.FontAttributes = FontAttributes.None;
        WeeklyTabLabel.TextColor = Color.FromArgb("#64748B");
        WeeklyTabBorder.Shadow = null;

        _ = LoadChartData();
    }

    private void OnWeeklyTapped(object sender, TappedEventArgs e)
    {
        _isWeeklyView = true;
        _currentFilterDate = DateTime.Today; // Reset anchor to today

        WeeklyTabBorder.BackgroundColor = Colors.White;
        WeeklyTabLabel.FontAttributes = FontAttributes.Bold;
        WeeklyTabLabel.TextColor = Color.FromArgb("#1E293B");
        WeeklyTabBorder.Shadow = new Shadow { Brush = Brush.Black, Offset = new Point(0, 2), Radius = 4, Opacity = 0.05f };

        DailyTabBorder.BackgroundColor = Colors.Transparent;
        DailyTabLabel.FontAttributes = FontAttributes.None;
        DailyTabLabel.TextColor = Color.FromArgb("#64748B");
        DailyTabBorder.Shadow = null;

        _ = LoadChartData();
    }

    private void OnPreviousDateTapped(object sender, EventArgs e)
    {
        // Daily moves back 1 week, Weekly moves back 4 weeks
        _currentFilterDate = _currentFilterDate.AddDays(_isWeeklyView ? -28 : -7);
        _ = LoadChartData();
    }

    private void OnNextDateTapped(object sender, EventArgs e)
    {
        // Daily moves forward 1 week, Weekly moves forward 4 weeks
        _currentFilterDate = _currentFilterDate.AddDays(_isWeeklyView ? 28 : 7);
        _ = LoadChartData();
    }

    // ==========================================
    // CHART DATA LOGIC
    // ==========================================

    private async Task LoadChartData()
    {
        try
        {
            var allReadings = await _firebaseService.GetReadingsAsync();
            _allReadingsCache = allReadings?.ToList() ?? new List<MeterReading>();

            if (_allReadingsCache.Count == 0)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    CurrentPeriodLabel.Text = "-";
                    _chartDrawable.Data = Array.Empty<float>();
                    _chartDrawable.Labels = Array.Empty<string>();
                    ChartGraphicsView.Invalidate();

                    PeakDayLabel.Text = "-";
                    PeakValueLabel.Text = "RM 0.00";
                    LowestDayLabel.Text = "-";
                    LowestValueLabel.Text = "RM 0.00";
                    InsightTextLabel.Text = "Keep logging readings to generate insights.";
                });
                return;
            }

            List<float> dataPoints = new List<float>();
            List<string> labels = new List<string>();

            // Find the Monday of the currently selected date
            int diffToMonday = (7 + (_currentFilterDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime currentWeekStart = _currentFilterDate.AddDays(-1 * diffToMonday).Date;

            if (!_isWeeklyView)
            {
                // DAILY MODE: Plot exact 7 days (Mon-Sun)
                DateTime weekEnd = currentWeekStart.AddDays(6).Date;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    CurrentPeriodLabel.Text = $"{currentWeekStart:MMM d} - {weekEnd:MMM d}";
                });

                for (int i = 0; i < 7; i++)
                {
                    DateTime day = currentWeekStart.AddDays(i);
                    float sum = (float)_allReadingsCache.Where(r => r.LocalDate.Date == day).Sum(r => r.Consumed);

                    dataPoints.Add(sum);
                    labels.Add(day.ToString("ddd"));
                }
            }
            else
            {
                // WEEKLY MODE: Plot exact 4 weeks, ending on current week
                DateTime week1Start = currentWeekStart.AddDays(-21); // Go back 3 weeks to get a 4-week window
                DateTime week4End = currentWeekStart.AddDays(6).Date;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    CurrentPeriodLabel.Text = $"{week1Start:MMM d} - {week4End:MMM d}";
                });

                for (int i = 0; i < 4; i++)
                {
                    DateTime startOfWeek = week1Start.AddDays(i * 7);
                    DateTime endOfWeek = startOfWeek.AddDays(6);

                    float sum = (float)_allReadingsCache.Where(r => r.LocalDate.Date >= startOfWeek && r.LocalDate.Date <= endOfWeek).Sum(r => r.Consumed);

                    dataPoints.Add(sum);
                    labels.Add(startOfWeek.ToString("MMM d")); // Display the date the week started rather than "Week X"
                }
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (dataPoints.Count > 0)
                {
                    _chartDrawable.Data = dataPoints.ToArray();
                    _chartDrawable.Labels = labels.ToArray();

                    float maxVal = dataPoints.Max();
                    _chartDrawable.YAxisMax = maxVal + (maxVal * 0.2f);
                    if (_chartDrawable.YAxisMax < 5) _chartDrawable.YAxisMax = 5;

                    ChartGraphicsView.Invalidate();

                    int peakIndex = dataPoints.IndexOf(maxVal);
                    int lowestIndex = dataPoints.IndexOf(dataPoints.Min());

                    PeakDayLabel.Text = labels[peakIndex];
                    PeakValueLabel.Text = $"RM {dataPoints[peakIndex]:F2}";
                    LowestDayLabel.Text = labels[lowestIndex];
                    LowestValueLabel.Text = $"RM {dataPoints[lowestIndex]:F2}";

                    InsightTextLabel.Text = _isWeeklyView
                        ? $"Your highest consumption was during the week of {labels[peakIndex]}. Monitor appliance usage during this period."
                        : $"Your usage peaked on {labels[peakIndex]}. Consider tracking heavy appliances on this day.";
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading chart data: {ex.Message}");
        }
    }
}

public class LineChartDrawable : IDrawable
{
    public float[] Data { get; set; } = Array.Empty<float>();
    public string[] Labels { get; set; } = Array.Empty<string>();
    public float YAxisMax { get; set; } = 10f;

    public string YAxisLabel { get; set; } = "Credit Usage (RM)";

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Data.Length == 0) return;

        float paddingLeft = 40;
        float paddingBottom = 30;
        float paddingTop = 30;
        float paddingRight = 45;

        float width = dirtyRect.Width - paddingLeft - paddingRight;
        float height = dirtyRect.Height - paddingTop - paddingBottom;

        Color lineColor = Color.FromArgb("#10B981");
        Color gridColor = Color.FromArgb("#E2E8F0");
        Color axisTextColor = Color.FromArgb("#64748B");
        Color axisLabelColor = Color.FromArgb("#334155");

        canvas.FontColor = axisLabelColor;
        canvas.FontSize = 12;
        canvas.DrawString(YAxisLabel, paddingLeft - 10, paddingTop - 25, 150, 20, HorizontalAlignment.Left, VerticalAlignment.Center);

        int yAxisSteps = 4;
        for (int i = 0; i <= yAxisSteps; i++)
        {
            float yRatio = (float)i / yAxisSteps;
            float yPos = dirtyRect.Height - paddingBottom - (height * yRatio);

            canvas.StrokeColor = gridColor;
            canvas.StrokeSize = 1;
            canvas.StrokeDashPattern = new float[] { 4, 4 };
            canvas.DrawLine(paddingLeft, yPos, dirtyRect.Width - paddingRight, yPos);

            float val = YAxisMax * yRatio;
            canvas.FontColor = axisTextColor;
            canvas.FontSize = 11;
            canvas.DrawString(val.ToString("0"), 0, yPos - 8, paddingLeft - 5, 16, HorizontalAlignment.Right, VerticalAlignment.Center);
        }

        canvas.StrokeDashPattern = null;
        canvas.StrokeColor = gridColor;
        canvas.DrawLine(paddingLeft, dirtyRect.Height - paddingBottom, dirtyRect.Width - paddingRight, dirtyRect.Height - paddingBottom);
        canvas.DrawLine(paddingLeft, paddingTop, paddingLeft, dirtyRect.Height - paddingBottom);

        float xStep = width / (Data.Length - 1 > 0 ? Data.Length - 1 : 1);
        PointF[] points = new PointF[Data.Length];

        for (int i = 0; i < Data.Length; i++)
        {
            float xPos = paddingLeft + (i * xStep);
            float yRatio = Data[i] / YAxisMax;
            float yPos = dirtyRect.Height - paddingBottom - (height * yRatio);

            points[i] = new PointF(xPos, yPos);

            if (i < Labels.Length)
            {
                canvas.FontColor = axisTextColor;
                canvas.FontSize = 11;
                canvas.DrawString(Labels[i], xPos - 30, dirtyRect.Height - paddingBottom + 8, 60, 20, HorizontalAlignment.Center, VerticalAlignment.Top);
            }
        }

        if (points.Length > 1)
        {
            PathF path = new PathF();
            path.MoveTo(points[0]);
            for (int i = 1; i < points.Length; i++) path.LineTo(points[i]);

            canvas.StrokeColor = lineColor;
            canvas.StrokeSize = 3;
            canvas.StrokeLineJoin = LineJoin.Round;
            canvas.DrawPath(path);
        }

        canvas.FillColor = lineColor;
        foreach (var point in points) canvas.FillCircle(point.X, point.Y, 5);
    }
}