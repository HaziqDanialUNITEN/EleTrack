using EleTrack.Services;

namespace EleTrack;

public partial class AnalyticsPage : ContentPage
{
    private LineChartDrawable _chartDrawable;
    private readonly FirebaseService _firebaseService;
    private bool _isWeeklyView = false;

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

    private void OnDailyTapped(object sender, TappedEventArgs e)
    {
        _isWeeklyView = false;

        // Simple UI property changes from user interaction are already on MainThread
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

    private async Task LoadChartData()
    {
        try
        {
            var allReadings = await _firebaseService.GetReadingsAsync();

            // Background thread is fine for calculations
            if (allReadings == null || allReadings.Count == 0) return;

            var readings = allReadings.OrderBy(r => r.Date).ToList();

            List<float> dataPoints = new List<float>();
            List<string> labels = new List<string>();

            if (!_isWeeklyView) // DAILY
            {
                var last7 = readings.TakeLast(7).ToList();
                foreach (var r in last7)
                {
                    dataPoints.Add((float)r.Consumed);
                    labels.Add(r.Date.ToString("ddd"));
                }
            }
            else // WEEKLY
            {
                int weekNum = 1;
                for (int i = 0; i < readings.Count; i += 7)
                {
                    var chunk = readings.Skip(i).Take(7);
                    dataPoints.Add((float)chunk.Sum(c => c.Consumed));
                    labels.Add($"Week {weekNum}");
                    weekNum++;
                }
            }

            // CRITICAL FIX: Push the UI updates back to the Main Thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (dataPoints.Count > 0)
                {
                    _chartDrawable.Data = dataPoints.ToArray();
                    _chartDrawable.Labels = labels.ToArray();

                    float maxVal = dataPoints.Max();
                    _chartDrawable.YAxisMax = maxVal + (maxVal * 0.2f);
                    if (_chartDrawable.YAxisMax < 5) _chartDrawable.YAxisMax = 5;

                    ChartGraphicsView.Invalidate(); // Crucial that this happens on UI thread

                    int peakIndex = dataPoints.IndexOf(maxVal);
                    int lowestIndex = dataPoints.IndexOf(dataPoints.Min());

                    PeakDayLabel.Text = labels[peakIndex];
                    PeakValueLabel.Text = $"RM {dataPoints[peakIndex]:F2}";
                    LowestDayLabel.Text = labels[lowestIndex];
                    LowestValueLabel.Text = $"RM {dataPoints[lowestIndex]:F2}";

                    InsightTextLabel.Text = _isWeeklyView
                        ? $"Your highest consumption was during {labels[peakIndex]}. Monitor appliance usage during this period."
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

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Data.Length == 0) return;

        float paddingLeft = 35;
        float paddingBottom = 30;
        float paddingTop = 10;
        float paddingRight = 15;

        float width = dirtyRect.Width - paddingLeft - paddingRight;
        float height = dirtyRect.Height - paddingTop - paddingBottom;

        Color lineColor = Color.FromArgb("#10B981");
        Color gridColor = Color.FromArgb("#E2E8F0");
        Color axisTextColor = Color.FromArgb("#64748B");

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
            canvas.FontSize = 12;
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
                canvas.FontSize = 12;
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