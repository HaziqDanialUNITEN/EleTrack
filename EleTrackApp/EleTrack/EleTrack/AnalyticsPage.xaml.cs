namespace EleTrack;

public partial class AnalyticsPage : ContentPage
{
    private LineChartDrawable _chartDrawable;

    // Fake Data for Daily (Past 7 days)
    private readonly float[] _dailyData = { 2.4f, 3.2f, 2.1f, 4.5f, 3.8f, 5.2f, 4.1f };
    private readonly string[] _dailyLabels = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

    // Fake Data for Weekly (4 weeks)
    private readonly float[] _weeklyData = { 18.5f, 15.2f, 24.1f, 22.4f };
    private readonly string[] _weeklyLabels = { "Week 1", "Week 2", "Week 3", "Week 4" };

    public AnalyticsPage()
    {
        InitializeComponent();
        Shell.SetNavBarIsVisible(this, false);

        // Initialize and bind the chart drawable
        _chartDrawable = new LineChartDrawable();
        ChartGraphicsView.Drawable = _chartDrawable;

        // Load Daily data by default
        LoadDailyData();
    }

    private void OnDailyTapped(object sender, TappedEventArgs e)
    {
        // Update UI Tabs
        DailyTabBorder.BackgroundColor = Colors.White;
        DailyTabLabel.FontAttributes = FontAttributes.Bold;
        DailyTabLabel.TextColor = Color.FromArgb("#1E293B");
        DailyTabBorder.Shadow = new Shadow { Brush = Brush.Black, Offset = new Point(0, 2), Radius = 4, Opacity = 0.05f };

        WeeklyTabBorder.BackgroundColor = Colors.Transparent;
        WeeklyTabLabel.FontAttributes = FontAttributes.None;
        WeeklyTabLabel.TextColor = Color.FromArgb("#64748B");
        WeeklyTabBorder.Shadow = null;

        LoadDailyData();
    }

    private void OnWeeklyTapped(object sender, TappedEventArgs e)
    {
        // Update UI Tabs
        WeeklyTabBorder.BackgroundColor = Colors.White;
        WeeklyTabLabel.FontAttributes = FontAttributes.Bold;
        WeeklyTabLabel.TextColor = Color.FromArgb("#1E293B");
        WeeklyTabBorder.Shadow = new Shadow { Brush = Brush.Black, Offset = new Point(0, 2), Radius = 4, Opacity = 0.05f };

        DailyTabBorder.BackgroundColor = Colors.Transparent;
        DailyTabLabel.FontAttributes = FontAttributes.None;
        DailyTabLabel.TextColor = Color.FromArgb("#64748B");
        DailyTabBorder.Shadow = null;

        LoadWeeklyData();
    }

    private void LoadDailyData()
    {
        // Update Chart
        _chartDrawable.Data = _dailyData;
        _chartDrawable.Labels = _dailyLabels;
        _chartDrawable.YAxisMax = 8f; // Set max height for Y axis
        ChartGraphicsView.Invalidate(); // Forces the chart to redraw

        // Update Stats
        PeakDayLabel.Text = "Saturday";
        PeakValueLabel.Text = "RM 5.20";
        LowestDayLabel.Text = "Wednesday";
        LowestValueLabel.Text = "RM 2.10";
        InsightTextLabel.Text = "Your usage spikes on weekends. Consider monitoring air conditioning and gaming device usage during this time.";
    }

    private void LoadWeeklyData()
    {
        // Update Chart
        _chartDrawable.Data = _weeklyData;
        _chartDrawable.Labels = _weeklyLabels;
        _chartDrawable.YAxisMax = 30f; // Increase max height for weekly sums
        ChartGraphicsView.Invalidate(); // Forces the chart to redraw

        // Update Stats
        PeakDayLabel.Text = "Week 3";
        PeakValueLabel.Text = "RM 24.10";
        LowestDayLabel.Text = "Week 2";
        LowestValueLabel.Text = "RM 15.20";
        InsightTextLabel.Text = "Your usage has been relatively stable this month, but Week 3 showed an unusual surge in consumption.";
    }
}

/// <summary>
/// A native graphics drawing class to render the line chart cleanly without any 3rd party NuGets.
/// </summary>
public class LineChartDrawable : IDrawable
{
    public float[] Data { get; set; } = Array.Empty<float>();
    public string[] Labels { get; set; } = Array.Empty<string>();
    public float YAxisMax { get; set; } = 10f;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Data.Length == 0) return;

        // Chart configuration
        float paddingLeft = 35;
        float paddingBottom = 25;
        float paddingTop = 10;
        float paddingRight = 15;

        float width = dirtyRect.Width - paddingLeft - paddingRight;
        float height = dirtyRect.Height - paddingTop - paddingBottom;

        // Colors matching your mockup
        Color lineColor = Color.FromArgb("#10B981"); // Emerald Green
        Color gridColor = Color.FromArgb("#E2E8F0");
        Color axisTextColor = Color.FromArgb("#64748B");

        // 1. Draw horizontal grid lines and Y-axis text
        int yAxisSteps = 4;
        for (int i = 0; i <= yAxisSteps; i++)
        {
            float yRatio = (float)i / yAxisSteps;
            float yPos = dirtyRect.Height - paddingBottom - (height * yRatio);

            // Draw dotted grid line
            canvas.StrokeColor = gridColor;
            canvas.StrokeSize = 1;
            canvas.StrokeDashPattern = new float[] { 4, 4 };
            canvas.DrawLine(paddingLeft, yPos, dirtyRect.Width - paddingRight, yPos);

            // Draw Y-axis text
            float val = YAxisMax * yRatio;
            canvas.FontColor = axisTextColor;
            canvas.FontSize = 12;
            canvas.DrawString(val.ToString("0"), 0, yPos - 8, paddingLeft - 5, 16, HorizontalAlignment.Right, VerticalAlignment.Center);
        }

        // Remove dash pattern for main lines
        canvas.StrokeDashPattern = null;

        // Draw solid X and Y axes boundaries
        canvas.StrokeColor = gridColor;
        canvas.DrawLine(paddingLeft, dirtyRect.Height - paddingBottom, dirtyRect.Width - paddingRight, dirtyRect.Height - paddingBottom);
        canvas.DrawLine(paddingLeft, paddingTop, paddingLeft, dirtyRect.Height - paddingBottom);

        // Calculate points for the graph line
        float xStep = width / (Data.Length - 1);
        PointF[] points = new PointF[Data.Length];

        for (int i = 0; i < Data.Length; i++)
        {
            float xPos = paddingLeft + (i * xStep);
            float yRatio = Data[i] / YAxisMax;
            float yPos = dirtyRect.Height - paddingBottom - (height * yRatio);

            points[i] = new PointF(xPos, yPos);

            // Draw X-axis label
            if (i < Labels.Length)
            {
                canvas.FontColor = axisTextColor;
                canvas.FontSize = 12;
                canvas.DrawString(Labels[i], xPos - 15, dirtyRect.Height - paddingBottom + 5, 30, 20, HorizontalAlignment.Center, VerticalAlignment.Top);
            }
        }

        // 2. Draw the connected line path
        PathF path = new PathF();
        path.MoveTo(points[0]);
        for (int i = 1; i < points.Length; i++)
        {
            path.LineTo(points[i]);
        }

        canvas.StrokeColor = lineColor;
        canvas.StrokeSize = 3;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.DrawPath(path);

        // 3. Draw the data points (filled circles) over the line
        canvas.FillColor = lineColor;
        foreach (var point in points)
        {
            canvas.FillCircle(point.X, point.Y, 5);
        }
    }
}