using MIDIKeys.Core;

namespace MIDIKeys;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        // Entwicklerhilfe: Tray-Symbole als PNG rendern und beenden.
        var renderIdx = Array.IndexOf(args, "--render-icons");
        if (renderIdx >= 0)
        {
            var dir = renderIdx + 1 < args.Length ? args[renderIdx + 1] : Path.GetTempPath();
            Directory.CreateDirectory(dir);
            foreach (var (name, aktiv) in new[] { ("inaktiv", false), ("aktiv", true) })
            foreach (var (variante, farbe) in new[] { ("hell", Color.Black), ("dunkel", Color.White) })
            {
                using var bmp = IconFactory.Render(aktiv, 144, farbe);
                var pfad = Path.Combine(dir, $"midikeys-{name}-{variante}.png");
                bmp.Save(pfad, System.Drawing.Imaging.ImageFormat.Png);
                Console.WriteLine(pfad);
            }
            return 0;
        }

        using var mutex = new Mutex(true, @"Local\MIDIKeys.SingleInstance", out var erste);
        if (!erste) return 0; // läuft schon — eine zweite Instanz hätte einen zweiten Hook

        ApplicationConfiguration.Initialize();
        using var app = new TrayApp(showPopup: args.Contains("--show"));
        Application.ApplicationExit += (_, _) => GC.KeepAlive(mutex);
        Application.Run(app);
        return 0;
    }
}
