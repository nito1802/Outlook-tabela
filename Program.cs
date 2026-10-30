using System.Globalization;
using System.Net;
using System.Text;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var osoby = new List<string> { "Anna", "Bartek", "Celina", "Daniel", "Ewa", "Filip" };
        var serwisy = Enumerable.Range(1, 30).Select(i => $"Serwis {i}").ToList();
        var poczatkowePary = new Dictionary<string, string>
        {
            ["Serwis 1"] = "Anna",
            ["Serwis 2"] = "Bartek"
        };

        var (result, seed) = OutlookTable.Generate(poczatkowePary, osoby, serwisy);

        HtmlClipboard.Copy(result);
        Console.WriteLine($"Seed: {seed}");
        Console.WriteLine("Tabela skopiowana. Wklej Ctrl+V w wiadomości HTML w Outlooku (z zachowaniem formatowania źródła).");
    }
}

public static class HtmlClipboard
{
    public static void Copy(string fragment)
    {
        ArgumentNullException.ThrowIfNull(fragment);
        const string template = "Version:1.0\r\nStartHTML:{0:D10}\r\nEndHTML:{1:D10}\r\nStartFragment:{2:D10}\r\nEndFragment:{3:D10}\r\n";
        const string prefix = "<html><head><meta charset=\"utf-8\"></head><body><!--StartFragment-->";
        const string suffix = "<!--EndFragment--></body></html>";

        // CF_HTML wymaga offsetów w bajtach UTF-8, także dla polskich znaków.
        int startHtml = Encoding.UTF8.GetByteCount(string.Format(CultureInfo.InvariantCulture, template, 0, 0, 0, 0));
        int startFragment = startHtml + Encoding.UTF8.GetByteCount(prefix);
        int endFragment = startFragment + Encoding.UTF8.GetByteCount(fragment);
        int endHtml = endFragment + Encoding.UTF8.GetByteCount(suffix);
        string payload = string.Format(CultureInfo.InvariantCulture, template,
            startHtml, endHtml, startFragment, endFragment) + prefix + fragment + suffix;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload + "\0"));
        var data = new DataObject();
        data.SetData(DataFormats.Html, stream);
        // true utrwala zawartość schowka po zakończeniu konsolki.
        Clipboard.SetDataObject(data, true, 10, 100);
    }
}

public static class OutlookTable
{
    /// <summary>
    /// Zwraca tabelę HTML i seed: osoba i jej serwisy. Początkowe pary serwis → osoba
    /// są zachowane, a reszta jest losowo przydzielana najmniej obciążonym osobom.
    /// minRows to minimalna liczba wierszy danych (bez nagłówka).
    /// </summary>
    public static (string Html, int Seed) Generate(
        Dictionary<string, string> initialPairs,
        List<string> people,
        List<string> services,
        int minRows = 20,
        int? seed = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minRows);
        var (assignments, usedSeed) = Distribute(initialPairs, people, services, seed);
        var html = new StringBuilder();
        html.Append("<table width=\"600\" cellpadding=\"0\" cellspacing=\"0\" style=\"width:600px;border-collapse:collapse;font-family:Calibri,Arial,sans-serif;font-size:11pt;color:#222222;\">");
        html.Append("<tr><th width=\"180\" align=\"left\" bgcolor=\"#D9E2F3\" style=\"border:1px solid #A6A6A6;padding:8px;\">Osoba</th><th width=\"420\" align=\"left\" bgcolor=\"#D9E2F3\" style=\"border:1px solid #A6A6A6;padding:8px;\">Serwis</th></tr>");

        for (int i = 0; i < Math.Max(people.Count, minRows); i++)
        {
            string color = i % 2 == 0 ? "#FFFFFF" : "#F0F3F7";
            string person = i < people.Count ? WebUtility.HtmlEncode(people[i]) : "&nbsp;";
            string items = i < people.Count && assignments[people[i]].Count > 0
                ? string.Join("<br>", assignments[people[i]].Select(WebUtility.HtmlEncode))
                : "&nbsp;";
            html.Append($"<tr><td valign=\"top\" bgcolor=\"{color}\" style=\"border:1px solid #A6A6A6;padding:8px;background-color:{color};\">{person}</td><td valign=\"top\" bgcolor=\"{color}\" style=\"border:1px solid #A6A6A6;padding:8px;line-height:22px;background-color:{color};\">{items}</td></tr>");
        }

        return (html.Append("</table>").ToString(), usedSeed);
    }

    public static (Dictionary<string, List<string>> Assignments, int Seed) Distribute(
        Dictionary<string, string> initialPairs,
        List<string> people,
        List<string> services,
        int? seed = null)
    {
        ArgumentNullException.ThrowIfNull(initialPairs);
        ArgumentNullException.ThrowIfNull(people);
        ArgumentNullException.ThrowIfNull(services);
        ValidateNames(people, nameof(people));
        ValidateNames(services, nameof(services));
        if (people.Count == 0)
            throw new ArgumentException("Lista osób nie może być pusta.", nameof(people));

        int usedSeed = seed ?? Random.Shared.Next();
        var random = new Random(usedSeed);
        var result = people.ToDictionary(p => p, _ => new List<string>(), StringComparer.Ordinal);
        var available = services.ToHashSet(StringComparer.Ordinal);
        foreach (var (service, person) in initialPairs.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (person is null || !result.ContainsKey(person))
                throw new ArgumentException($"Nieznana osoba w początkowych parach: {person}", nameof(initialPairs));
            if (service is null || !available.Remove(service))
                throw new ArgumentException($"Serwis nie istnieje lub został przypisany dwukrotnie: {service}", nameof(initialPairs));
            result[person].Add(service);
        }

        var remaining = services.Where(available.Contains).ToArray();
        random.Shuffle(remaining);
        foreach (string service in remaining)
        {
            int min = result.Values.Min(s => s.Count);
            var candidates = people.Where(p => result[p].Count == min).ToArray();
            result[candidates[random.Next(candidates.Length)]].Add(service);
        }
        return (result, usedSeed);
    }

    private static void ValidateNames(List<string> values, string parameter)
    {
        if (values.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Nazwy nie mogą być puste.", parameter);
        if (values.Distinct(StringComparer.Ordinal).Count() != values.Count)
            throw new ArgumentException("Nazwy muszą być unikalne (rozróżniana wielkość liter).", parameter);
    }
}
