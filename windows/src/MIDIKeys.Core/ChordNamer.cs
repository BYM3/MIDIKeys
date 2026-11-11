namespace MIDIKeys.Core;

/// <summary>
/// Erkennt Akkorde und Intervalle aus einer Menge gehaltener Tonhöhenklassen
/// (0–11). 1:1-Port von <c>ChordNamer.swift</c> — Formen, Reihenfolge und
/// Grundton-Regeln sind identisch; Quellen siehe dort.
/// </summary>
public static class ChordNamer
{
    internal sealed record Akkordform(string Name, int[] Intervalle, Dictionary<int, int> Umkehrung);

    private static Dictionary<int, int> Dur => new() { [4] = 1, [7] = 2 };
    private static Dictionary<int, int> Moll => new() { [3] = 1, [7] = 2 };
    private static Dictionary<int, int> Dom7 => new() { [4] = 1, [7] = 2, [10] = 3 };
    private static Dictionary<int, int> Moll7 => new() { [3] = 1, [7] = 2, [10] = 3 };
    private static Dictionary<int, int> U(params (int Intervall, int Umkehrung)[] e) =>
        e.ToDictionary(x => x.Intervall, x => x.Umkehrung);

    /// <summary>Die Reihenfolge entscheidet bei Gleichstand: Terzschichtungen vor add-Akkorden.</summary>
    internal static readonly Akkordform[] Akkordformen =
    [
        // Dreiklänge
        new("maj", [0, 4, 7], Dur),
        new("min", [0, 3, 7], Moll),
        new("dim", [0, 3, 6], U((3, 1), (6, 2))),
        new("aug", [0, 4, 8], U((4, 1), (8, 2))),
        new("sus2", [0, 2, 7], U((7, 2))),
        new("sus4", [0, 5, 7], U((7, 2))),
        // Septakkorde ohne Quinte
        new("7(no5)", [0, 4, 10], U((4, 1), (10, 3))),
        new("maj7(no5)", [0, 4, 11], U((4, 1), (11, 3))),
        new("m7(no5)", [0, 3, 10], U((3, 1), (10, 3))),
        // Septakkorde
        new("7", [0, 4, 7, 10], Dom7),
        new("maj7", [0, 4, 7, 11], U((4, 1), (7, 2), (11, 3))),
        new("m7", [0, 3, 7, 10], Moll7),
        new("m7b5", [0, 3, 6, 10], U((3, 1), (6, 2), (10, 3))),
        new("dim7", [0, 3, 6, 9], U((3, 1), (6, 2), (9, 3))),
        new("mMaj7", [0, 3, 7, 11], U((3, 1), (7, 2), (11, 3))),
        new("aug7", [0, 4, 8, 10], U((4, 1), (8, 2), (10, 3))),
        new("maj7#5", [0, 4, 8, 11], U((4, 1), (8, 2), (11, 3))),
        new("7b5", [0, 4, 6, 10], U((4, 1), (6, 2), (10, 3))),
        new("7sus4", [0, 5, 7, 10], U((7, 2), (10, 3))),
        // add-Akkorde (bewusst ohne 6 und m6 — siehe Swift-Original)
        new("add9", [0, 4, 7, 2], Dur),
        new("madd9", [0, 3, 7, 2], Moll),
        new("add11", [0, 4, 7, 5], Dur),
        // None-Akkorde
        new("9", [0, 4, 7, 10, 2], Dom7),
        new("maj9", [0, 4, 7, 11, 2], U((4, 1), (7, 2), (11, 3))),
        new("m9", [0, 3, 7, 10, 2], Moll7),
        new("7b9", [0, 4, 7, 10, 1], Dom7),
        new("7#9", [0, 4, 7, 10, 3], Dom7),
        new("9sus4", [0, 5, 7, 10, 2], U((7, 2), (10, 3))),
        new("6/9", [0, 4, 7, 9, 2], Dur),
        // Undezimen und Tredezime
        new("11", [0, 4, 7, 10, 2, 5], Dom7),
        new("m11", [0, 3, 7, 10, 2, 5], Moll7),
        new("13", [0, 4, 10, 2, 9], U((4, 1), (10, 3))),
    ];

    private static readonly Dictionary<int, string> Intervallnamen = new()
    {
        [1] = "kleine Sekunde", [2] = "große Sekunde", [3] = "kleine Terz", [4] = "große Terz",
        [5] = "Quarte", [6] = "Tritonus", [7] = "Quinte", [8] = "kleine Sexte",
        [9] = "große Sexte", [10] = "kleine Septime", [11] = "große Septime",
    };

    private static readonly Dictionary<int, string> IntervalNames = new()
    {
        [1] = "minor second", [2] = "major second", [3] = "minor third", [4] = "major third",
        [5] = "perfect fourth", [6] = "tritone", [7] = "perfect fifth", [8] = "minor sixth",
        [9] = "major sixth", [10] = "minor seventh", [11] = "major seventh",
    };

    private static readonly Dictionary<int, string> Ordinals = new() { [1] = "1st", [2] = "2nd", [3] = "3rd" };

    /// <summary>
    /// Gibt z. B. "C maj", "C maj/E · 1. Umk." oder bei zwei Tönen
    /// "C–E · große Terz" zurück, oder null.
    /// </summary>
    public static string? Name(IReadOnlySet<int> pitchClasses, int? bass = null, Sprache sprache = Sprache.Deutsch)
    {
        var notennamen = Notes.Names(sprache);
        if (pitchClasses.Count == 2) return Intervall(pitchClasses, bass, sprache);
        var treffer = Finde(pitchClasses, bass);
        if (treffer is null) return null;
        var (grundton, form) = treffer.Value;
        var basisname = $"{notennamen[grundton]} {form.Name}";
        if (bass is null || bass == grundton) return basisname;
        var slash = $"{basisname}/{notennamen[bass.Value]}";
        var abstand = (bass.Value - grundton + 12) % 12;
        if (!form.Umkehrung.TryGetValue(abstand, out var umkehrung)) return slash;
        return sprache == Sprache.Deutsch
            ? $"{slash} · {umkehrung}. Umk."
            : $"{slash} · {(Ordinals.TryGetValue(umkehrung, out var o) ? o : $"{umkehrung}th")} inv.";
    }

    private static string? Intervall(IReadOnlySet<int> pitchClasses, int? bass, Sprache sprache)
    {
        var notennamen = Notes.Names(sprache);
        var namen = sprache == Sprache.Deutsch ? Intervallnamen : IntervalNames;
        var unten = bass ?? pitchClasses.Min();
        var obenKandidaten = pitchClasses.Where(p => p != unten).ToList();
        if (obenKandidaten.Count == 0) return null;
        var oben = obenKandidaten[0];
        if (!namen.TryGetValue((oben - unten + 12) % 12, out var name)) return null;
        return $"{notennamen[unten]}–{notennamen[oben]} · {name}";
    }

    private static (int Grundton, Akkordform Form)? Finde(IReadOnlySet<int> pitchClasses, int? bass)
    {
        if (pitchClasses.Count is < 3 or > 6) return null;

        var kandidaten = new List<(int Grundton, int FormIndex)>();
        for (var grundton = 0; grundton < 12; grundton++)
        {
            for (var index = 0; index < Akkordformen.Length; index++)
            {
                var form = Akkordformen[index];
                if (form.Intervalle.Length != pitchClasses.Count) continue;
                var geformt = form.Intervalle.Select(i => (grundton + i) % 12).ToHashSet();
                if (geformt.SetEquals(pitchClasses)) kandidaten.Add((grundton, index));
            }
        }
        if (kandidaten.Count == 0) return null;
        if (bass is null)
        {
            var k = kandidaten[0];
            return (k.Grundton, Akkordformen[k.FormIndex]);
        }

        int Rang((int Grundton, int FormIndex) k)
        {
            if (k.Grundton == bass) return 0;
            var abstand = (bass.Value - k.Grundton + 12) % 12;
            return Akkordformen[k.FormIndex].Umkehrung.ContainsKey(abstand) ? 1 : 2;
        }

        var bester = kandidaten
            .OrderBy(Rang).ThenBy(k => k.FormIndex).ThenBy(k => k.Grundton)
            .First();
        return (bester.Grundton, Akkordformen[bester.FormIndex]);
    }
}
