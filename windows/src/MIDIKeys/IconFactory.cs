using System.Drawing.Drawing2D;

namespace MIDIKeys;

/// <summary>
/// Die beiden Tray-Symbole, programmatisch gezeichnet — wie unter macOS eine
/// Tastenkappe mit "A" (inaktiv) oder Klaviatur (aktiv). Einfarbig, passend zur
/// hellen oder dunklen Taskleiste.
/// </summary>
internal static class IconFactory
{
    public static Bitmap Render(bool active, int size, Color farbe)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        var s = size / 18f; // Entwurf in 18-pt-Einheiten wie das Original

        var kappe = new RectangleF(1.5f * s, 1.0f * s, 15f * s, 16f * s);
        using (var pen = new Pen(farbe, Math.Max(1f, 1.3f * s)))
        using (var path = RoundedRect(kappe, 3.5f * s))
            g.DrawPath(pen, path);

        using var brush = new SolidBrush(farbe);
        if (active)
        {
            var r = RectangleF.Inflate(kappe, -2.2f * s, -3.4f * s);
            var weisseBreite = r.Width / 3;
            var schwarzHoehe = r.Height * 0.52f;
            var schwarzBreite = weisseBreite * 0.66f;
            var trenner = Math.Max(1f, 0.8f * s);
            for (var i = 1; i < 3; i++)
            {
                var mitte = r.Left + weisseBreite * i;
                g.FillRectangle(brush, mitte - trenner / 2, r.Top + schwarzHoehe, trenner, r.Height - schwarzHoehe);
                g.FillRectangle(brush, mitte - schwarzBreite / 2, r.Top, schwarzBreite, schwarzHoehe);
            }
        }
        else
        {
            var r = RectangleF.Inflate(kappe, -3f * s, -3f * s);
            using var font = new Font("Segoe UI Semibold", r.Height * 0.85f, GraphicsUnit.Pixel);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.DrawString("A", font, brush, new RectangleF(r.X, r.Y + 0.5f * s, r.Width, r.Height), sf);
        }
        return bmp;
    }

    public static Icon TrayIcon(bool active, int size)
    {
        var farbe = Theme.TaskbarDark ? Color.White : Color.Black;
        using var bmp = Render(active, size, farbe);
        var h = bmp.GetHicon();
        var icon = (Icon)Icon.FromHandle(h).Clone();
        Native.DestroyIcon(h);
        return icon;
    }

    public static GraphicsPath RoundedRect(RectangleF r, float radius)
    {
        var path = new GraphicsPath();
        var d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        if (d <= 0) { path.AddRectangle(r); return path; }
        path.AddArc(r.Left, r.Top, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
