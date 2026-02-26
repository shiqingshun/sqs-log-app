using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace SqsLogApp;

internal enum AppMenuIconKind
{
    Edit,
    Manage,
    Settings,
    Delete,
    Exit
}

internal static class AppBranding
{
    private static readonly Lazy<Icon> AppIconFactory = new(CreateAppIcon);

    public static Icon AppIcon => AppIconFactory.Value;

    public static Image CreateMenuIcon(AppMenuIconKind kind)
    {
        var image = new Bitmap(16, 16);
        using var graphics = Graphics.FromImage(image);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var backgroundBrush = new SolidBrush(Color.FromArgb(18, 122, 128));
        graphics.FillEllipse(backgroundBrush, 0, 0, 15, 15);

        using var pen = new Pen(Color.White, 1.5F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        switch (kind)
        {
            case AppMenuIconKind.Edit:
                graphics.DrawLine(pen, 4, 11, 10, 5);
                using (var pointBrush = new SolidBrush(Color.White))
                {
                    graphics.FillPolygon(pointBrush, [new PointF(10.5F, 4.5F), new PointF(12.3F, 6.2F), new PointF(9.6F, 6.8F)]);
                }

                break;
            case AppMenuIconKind.Manage:
                graphics.DrawLine(pen, 4, 5, 12, 5);
                graphics.DrawLine(pen, 4, 8, 12, 8);
                graphics.DrawLine(pen, 4, 11, 10, 11);
                break;
            case AppMenuIconKind.Settings:
                graphics.DrawEllipse(pen, 5.2F, 5.2F, 5.6F, 5.6F);
                graphics.DrawLine(pen, 8, 3.2F, 8, 4.5F);
                graphics.DrawLine(pen, 8, 11.5F, 8, 12.8F);
                graphics.DrawLine(pen, 3.2F, 8, 4.5F, 8);
                graphics.DrawLine(pen, 11.5F, 8, 12.8F, 8);
                break;
            case AppMenuIconKind.Delete:
                graphics.DrawLine(pen, 5, 5, 11, 11);
                graphics.DrawLine(pen, 11, 5, 5, 11);
                break;
            case AppMenuIconKind.Exit:
                graphics.DrawLine(pen, 4, 4, 4, 12);
                graphics.DrawLine(pen, 4, 8, 11, 8);
                graphics.DrawLine(pen, 9, 6, 11, 8);
                graphics.DrawLine(pen, 9, 10, 11, 8);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }

        return image;
    }

    private static Icon CreateAppIcon()
    {
        using var bitmap = new Bitmap(64, 64);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        var bounds = new RectangleF(4, 4, 56, 56);
        using var gradientBrush = new LinearGradientBrush(
            bounds,
            Color.FromArgb(34, 163, 157),
            Color.FromArgb(41, 98, 171),
            45F);
        using var backgroundPath = CreateRoundedRectangle(bounds, 14F);
        graphics.FillPath(gradientBrush, backgroundPath);

        using var paperBrush = new SolidBrush(Color.White);
        using var paperPath = CreateRoundedRectangle(new RectangleF(18, 12, 28, 40), 6F);
        graphics.FillPath(paperBrush, paperPath);

        using var linePen = new Pen(Color.FromArgb(65, 133, 202), 2F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        graphics.DrawLine(linePen, 23, 22, 41, 22);
        graphics.DrawLine(linePen, 23, 28, 36, 28);
        graphics.DrawLine(linePen, 23, 34, 33, 34);

        using var checkPen = new Pen(Color.FromArgb(18, 122, 128), 3F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        graphics.DrawLine(checkPen, 25, 41, 30, 46);
        graphics.DrawLine(checkPen, 30, 46, 39, 37);

        var iconHandle = bitmap.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(iconHandle);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(iconHandle);
        }
    }

    private static GraphicsPath CreateRoundedRectangle(RectangleF rect, float radius)
    {
        var diameter = radius * 2F;
        var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
