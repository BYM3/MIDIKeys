namespace MIDIKeys.Core.Tests;

public class ChordNamerRundlaufTests
{
    /// <summary>
    /// Vollständiger Rundlauf: Jede Akkordform auf jedem Grundton mit jedem
    /// Akkordton im Bass muss einen Namen liefern, der — zurück in Töne
    /// übersetzt — genau die gespielten Töne ergibt und den Bass richtig nennt.
    /// In beiden Sprachen — Englisch unterscheidet sich nur im Notennamen
    /// (B statt H) und im Wortlaut der Umkehrung.
    /// </summary>
    [Theory]
    [InlineData(Sprache.Deutsch)]
    [InlineData(Sprache.English)]
    public void jederNameBeschreibtGenauDieGespieltenToene(Sprache sprache)
    {
        var noten = Notes.Names(sprache);
        var geprueft = 0;
        foreach (var form in ChordNamer.Akkordformen)
        {
            for (var grundton = 0; grundton < 12; grundton++)
            {
                var toene = form.Intervalle.Select(i => (grundton + i) % 12).ToHashSet();
                foreach (var bass in toene)
                {
                    var name = ChordNamer.Name(toene, bass, sprache);
                    Assert.True(name is not null, $"kein Name für {noten[grundton]} {form.Name} über {noten[bass]}");

                    // "C maj7/E · 1. Umk." -> Grundton "C", Form "maj7", Bass "E"
                    var haupt = name!.Split(" · ")[0];
                    var teile = haupt.Split(' ', 2);
                    var formName = teile[1];
                    var genannterBass = teile[0];
                    // Der Slash-Bass ist der letzte "/Note"-Teil ("6/9" enthält selbst einen Slash).
                    var slash = formName.LastIndexOf('/');
                    if (slash >= 0 && noten.Contains(formName[(slash + 1)..]))
                    {
                        genannterBass = formName[(slash + 1)..];
                        formName = formName[..slash];
                    }
                    var g = Array.IndexOf(noten, teile[0]);
                    var f = ChordNamer.Akkordformen.FirstOrDefault(a => a.Name == formName);
                    Assert.True(g >= 0 && f is not null, $"unlesbarer Name: {name}");

                    Assert.True(f!.Intervalle.Select(i => (g + i) % 12).ToHashSet().SetEquals(toene),
                        $"{name} passt nicht zu den Tönen");
                    Assert.True(genannterBass == noten[bass], $"{name}: falscher Bass, erwartet {noten[bass]}");
                    // Liegt der eigene Grundton im Bass, gewinnt eine Lesart ohne Slash.
                    if (bass == grundton) Assert.False(name.Contains(" · "), $"{name}: Grundstellung mit Umkehrung");
                    geprueft++;
                }
            }
        }
        Assert.Equal(ChordNamer.Akkordformen.Sum(a => a.Intervalle.Length) * 12, geprueft);
    }

    /// <summary>
    /// Jedes der 11 Intervalle auf jedem Ton, in beiden Sprachen: Die Töne
    /// stehen in der Reihenfolge unten–oben, und der Intervallname passt.
    /// </summary>
    [Theory]
    [InlineData(Sprache.Deutsch)]
    [InlineData(Sprache.English)]
    public void jedesIntervallWirdRichtigBenannt(Sprache sprache)
    {
        var noten = Notes.Names(sprache);
        string[] deutsch = ["kleine Sekunde", "große Sekunde", "kleine Terz", "große Terz", "Quarte", "Tritonus",
                            "Quinte", "kleine Sexte", "große Sexte", "kleine Septime", "große Septime"];
        string[] english = ["minor second", "major second", "minor third", "major third", "perfect fourth", "tritone",
                            "perfect fifth", "minor sixth", "major sixth", "minor seventh", "major seventh"];
        var namen = sprache == Sprache.Deutsch ? deutsch : english;
        for (var unten = 0; unten < 12; unten++)
        {
            for (var abstand = 1; abstand <= 11; abstand++)
            {
                var oben = (unten + abstand) % 12;
                Assert.Equal($"{noten[unten]}–{noten[oben]} · {namen[abstand - 1]}",
                    ChordNamer.Name(new HashSet<int> { unten, oben }, unten, sprache));
            }
        }
    }
}
