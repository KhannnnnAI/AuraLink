using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace ui_avalonia.Controls;

/// <summary>
/// Vòng tốc kế (Speedometer) dạng cung tròn 270° với thang logarithm,
/// gradient màu teal→cyan→green→lime, kim chỉ tốc độ, và hiển thị giá trị.
/// Scale: 0 → 10 000 Mbps (10 Gbps).
/// </summary>
public class SpeedGaugeControl : Control
{
    // ═══════════════════════ Styled Properties ═══════════════════════

    public static readonly StyledProperty<double> SpeedProperty =
        AvaloniaProperty.Register<SpeedGaugeControl, double>(nameof(Speed), 0);

    public double Speed
    {
        get => GetValue(SpeedProperty);
        set => SetValue(SpeedProperty, value);
    }

    public static readonly StyledProperty<bool> IsUploadProperty =
        AvaloniaProperty.Register<SpeedGaugeControl, bool>(nameof(IsUpload), false);

    public bool IsUpload
    {
        get => GetValue(IsUploadProperty);
        set => SetValue(IsUploadProperty, value);
    }

    static SpeedGaugeControl()
    {
        AffectsRender<SpeedGaugeControl>(SpeedProperty);
        AffectsRender<SpeedGaugeControl>(IsUploadProperty);
    }

    // ═══════════════════════ Constants ═══════════════════════════════

    private const double ArcStart = -135;   // độ, từ 12h, thuận chiều kim đồng hồ
    private const double ArcSweep = 270;
    private const double MaxSpeed = 10_000; // 10 Gbps

    private static readonly (double speed, string label)[] Ticks =
    {
        (0, "0"), (5, "5"), (10, "10"), (50, "50"), (100, "100"),
        (250, "250"), (500, "500"), (1000, "1Gbps"), (2500, "2.5Gbps"),
        (5000, "5Gbps"), (10000, "10Gbps")
    };

    private static readonly (double pos, Color color)[] DownloadColors =
    {
        (0.00, Color.Parse("#00FF87")),   // Neon Green
        (0.50, Color.Parse("#00E5FF")),   // Neon Cyan
        (1.00, Color.Parse("#00A3FF")),   // Deep Neon Blue
    };

    private static readonly (double pos, Color color)[] UploadColors =
    {
        (0.00, Color.Parse("#E040FB")),   // Neon Purple/Fuchsia
        (0.50, Color.Parse("#FF007F")),   // Neon Pink
        (1.00, Color.Parse("#FF5252")),   // Neon Red
    };

    private static readonly SolidColorBrush[] DownloadBrushes = PrecomputeBrushes(false);
    private static readonly SolidColorBrush[] UploadBrushes = PrecomputeBrushes(true);

    private static SolidColorBrush[] PrecomputeBrushes(bool isUpload)
    {
        var brushes = new SolidColorBrush[100];
        for (int i = 0; i < 100; i++)
        {
            double f = (double)i / 99;
            brushes[i] = new SolidColorBrush(ColorAt(f, isUpload));
        }
        return brushes;
    }

    // ═══════════════════════ Math Helpers ════════════════════════════

    private static double Frac(double spd)
        => spd <= 0 ? 0 : Math.Clamp(Math.Log10(Math.Max(spd, 1)) / Math.Log10(MaxSpeed), 0, 1);

    private static double Angle(double spd) => ArcStart + Frac(spd) * ArcSweep;

    private static Point Polar(double deg, double r, Point c)
    {
        double rad = deg * Math.PI / 180.0;
        return new Point(c.X + r * Math.Sin(rad), c.Y - r * Math.Cos(rad));
    }

    private static Color Lerp(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return Color.FromArgb(
            (byte)(a.A + (b.A - a.A) * t), (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t));
    }

    private static Color ColorAt(double f, bool isUpload)
    {
        var colors = isUpload ? UploadColors : DownloadColors;
        f = Math.Clamp(f, 0, 1);
        for (int i = 0; i < colors.Length - 1; i++)
            if (f <= colors[i + 1].pos)
            {
                double t = (f - colors[i].pos) / (colors[i + 1].pos - colors[i].pos);
                return Lerp(colors[i].color, colors[i + 1].color, t);
            }
        return colors[^1].color;
    }

    private IBrush GetThemeBrush(string key, IBrush defaultBrush)
    {
        if (this.TryFindResource(key, out var res))
        {
            if (res is IBrush brush)
                return brush;
        }
        return defaultBrush;
    }

    // ═══════════════════════ Render ══════════════════════════════════

    public override void Render(DrawingContext dc)
    {
        base.Render(dc);

        double w = Bounds.Width, h = Bounds.Height;
        double sz = Math.Min(w, h);
        if (sz < 80) return;

        var ctr = new Point(w / 2, h / 2);
        double arcR = (sz - 76) / 2;      // bán kính cung chính
        double stroke = sz * 0.052;         // độ dày cung

        // 1 ── Track nền (cung xám mờ hoặc sáng hơn ở darkmode để nổi bật)
        bool isDark = this.ActualThemeVariant == ThemeVariant.Dark;
        var trackBrush = isDark
            ? new SolidColorBrush(Color.FromArgb(35, 255, 255, 255)) // Nổi bật hơn ở Darkmode
            : new SolidColorBrush(Color.FromArgb(20, 0, 0, 0));      // Lightmode
        
        DrawArc(dc, ctr, arcR, ArcStart, ArcSweep,
            new Pen(trackBrush, stroke + 6, lineCap: PenLineCap.Round));

        // 2 ── Cung giá trị (gradient)
        double frac = Frac(Speed);
        double sweep = frac * ArcSweep;
        if (Speed > 0 && sweep > 0.3)
        {
            // Lớp glow (mờ, rộng hơn)
            DrawGradientArc(dc, ctr, arcR, sweep, stroke + 18, 0.08, IsUpload);
            // Lớp chính
            DrawGradientArc(dc, ctr, arcR, sweep, stroke, 1.0, IsUpload);
        }

        // 3 ── Vạch chia & nhãn
        DrawTicks(dc, ctr, arcR, stroke, sz);

        // 4 ── Kim chỉ tốc độ (luôn vẽ kim để đồng hồ trông sinh động hơn)
        DrawNeedle(dc, ctr, arcR, stroke);

        // 5 ── Giá trị tốc độ ở giữa
        DrawSpeedValue(dc, ctr, arcR, sz);
    }

    // ═══════ Vẽ cung tròn ═══════

    private static void DrawArc(DrawingContext dc, Point ctr, double r,
        double startDeg, double sweepDeg, Pen pen)
    {
        if (sweepDeg < 0.1) return;
        var p1 = Polar(startDeg, r, ctr);
        var p2 = Polar(startDeg + sweepDeg, r, ctr);
        var geo = new StreamGeometry();
        using (var g = geo.Open())
        {
            g.BeginFigure(p1, false);
            g.ArcTo(p2, new Size(r, r), 0, sweepDeg > 180, SweepDirection.Clockwise);
        }
        dc.DrawGeometry(null, pen, geo);
    }

    private void DrawGradientArc(DrawingContext dc, Point ctr, double r,
        double sweepDeg, double stroke, double opacity, bool isUpload)
    {
        // Tối ưu hóa số lượng segment vẽ để đảm bảo 60fps mượt mà
        int segs = Math.Clamp((int)(sweepDeg / 6.0), 10, 35);
        double segA = sweepDeg / segs;

        for (int i = 0; i < segs; i++)
        {
            double f = (double)i / segs * (sweepDeg / ArcSweep);
            
            // Sử dụng brush đã được tính toán sẵn từ cache thay vì tạo mới liên tục
            int brushIdx = Math.Clamp((int)(f * 99), 0, 99);
            var baseBrush = isUpload ? UploadBrushes[brushIdx] : DownloadBrushes[brushIdx];
            var brush = baseBrush;

            if (opacity < 1)
            {
                var c = baseBrush.Color;
                brush = new SolidColorBrush(Color.FromArgb((byte)(255 * opacity), c.R, c.G, c.B));
            }

            DrawArc(dc, ctr, r, ArcStart + i * segA, segA + 0.5,
                new Pen(brush, stroke));
        }
    }

    // ═══════ Vạch chia ═══════

    private void DrawTicks(DrawingContext dc, Point ctr, double r, double stroke, double sz)
    {
        double inR = r - stroke / 2 - 6;
        double outR = r + stroke / 2 + 6;
        double lblR = r + stroke / 2 + 24;

        bool isDark = this.ActualThemeVariant == ThemeVariant.Dark;
        
        // Tăng độ tương phản của ticks và labels trong darkmode để làm đồng hồ nổi lên
        var tickBrush = isDark 
            ? new SolidColorBrush(Color.FromArgb(140, 255, 255, 255))
            : GetThemeBrush("SemiColorText2", new SolidColorBrush(Color.FromArgb(110, 0, 0, 0)));
            
        var tickPen = new Pen(tickBrush, 1.5);
        var tf = new Typeface("Segoe UI");
        
        var lblBrush = isDark
            ? new SolidColorBrush(Color.FromArgb(200, 255, 255, 255))
            : GetThemeBrush("SemiColorText1", new SolidColorBrush(Color.FromArgb(155, 0, 0, 0)));
            
        double fontSize = Math.Max(sz * 0.033, 9);

        foreach (var (spd, lbl) in Ticks)
        {
            double a = Angle(spd);
            dc.DrawLine(tickPen, Polar(a, inR, ctr), Polar(a, outR, ctr));

            var ft = new FormattedText(lbl, CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, tf, fontSize, lblBrush);

            var lp = Polar(a, lblR, ctr);
            dc.DrawText(ft, new Point(
                lp.X - ft.Width / 2,
                lp.Y - ft.Height / 2));
        }
    }

    // ═══════ Kim chỉ ═══════

    private void DrawNeedle(DrawingContext dc, Point ctr, double r, double stroke)
    {
        double a = Angle(Speed);
        double tipR = r - stroke / 2 - 12;
        double backR = 20;
        double halfW = 4.5;

        var tip = Polar(a, tipR, ctr);
        var bL = Polar(a - 90, halfW, ctr);
        var bR = Polar(a + 90, halfW, ctr);
        var back = Polar(a + 180, backR, ctr);

        var needleColor = ColorAt(Frac(Speed), IsUpload);
        var needleBrush = new SolidColorBrush(needleColor);

        // Thân kim
        var geo = new StreamGeometry();
        using (var g = geo.Open())
        {
            g.BeginFigure(tip, true);
            g.LineTo(bL);
            g.LineTo(back);
            g.LineTo(bR);
            g.EndFigure(true);
        }
        dc.DrawGeometry(
            needleBrush,
            new Pen(new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)), 0.5),
            geo);

        // Trục giữa (hub)
        var hubGeo = new EllipseGeometry(
            new Rect(ctr.X - 10, ctr.Y - 10, 20, 20));
        var hubBgBrush = GetThemeBrush("SemiColorBackground2", new SolidColorBrush(Color.FromRgb(40, 40, 48)));
        dc.DrawGeometry(
            hubBgBrush,
            new Pen(needleBrush, 2.5),
            hubGeo);
    }

    // ═══════ Hiển thị giá trị ═══════

    private void DrawSpeedValue(DrawingContext dc, Point ctr, double r, double sz)
    {
        // Giá trị lớn
        string val = Speed <= 0
            ? "—"
            : Speed >= 1000
                ? Speed.ToString("F1", CultureInfo.InvariantCulture)
                : Speed.ToString("F2", CultureInfo.InvariantCulture);

        double bigSize = Math.Max(sz * 0.115, 18);
        
        // Buộc văn bản chữ số tốc độ là màu trắng khi ở darkmode
        bool isDark = this.ActualThemeVariant == ThemeVariant.Dark;
        var textBrush = isDark ? Brushes.White : Brushes.Black;
        
        var bigFt = new FormattedText(val, CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI", FontStyle.Normal, FontWeight.Bold),
            bigSize, textBrush);

        double yCenter = ctr.Y + r * 0.30;
        dc.DrawText(bigFt, new Point(
            ctr.X - bigFt.Width / 2,
            yCenter - bigFt.Height / 2));

        // Đơn vị "Mbps" (màu trắng mờ ở darkmode)
        double unitSize = Math.Max(sz * 0.038, 10);
        var unitBrush = isDark
            ? new SolidColorBrush(Color.FromArgb(150, 255, 255, 255))
            : new SolidColorBrush(Color.FromArgb(150, 0, 0, 0));
            
        var unitFt = new FormattedText("Mbps", CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            unitSize,
            unitBrush);

        dc.DrawText(unitFt, new Point(
            ctr.X - unitFt.Width / 2,
            yCenter + bigFt.Height / 2 + 2));
    }
}
