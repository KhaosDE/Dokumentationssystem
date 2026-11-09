using System.Text.Json;
using DokuLine.Kern;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace DokuLine;

// Nachschlagen beim BfArM im Formular „Medikament anlegen/ändern“ – die einzige
// eingebettete Webansicht im Programm (CLAUDE.md, Regel 6). Gesucht wird nur mit dem Medikamentennamen.
// Jede Navigation wird geprüft: Nur Adressen aus ExterneAdressen.Nachschlagebereiche bleiben in der
// Ansicht, alles andere öffnet im normalen Browser. Verbunden wird erst auf „Nachschlagen“ – das
// Öffnen des Formulars lädt nichts. Ausgelesen wird nichts von selbst: Markierten Text übernimmt erst
// „In Feld übernehmen“. Fehlen WebView2 oder die Verbindung, steht ein Hinweis da; das Formular bleibt
// voll nutzbar. In den Einstellungen abschaltbar (dann nur „Im Browser öffnen“).
// Abgesichert (Bauplan 29p): Anfragen an andere Adressen werden blockiert und im Protokoll gezählt, privates Fenster
// (Cookies und Verlauf nur bis zum Schließen), SmartScreen-Prüfung aus, kein automatisches Ausfüllen.
public sealed class Nachschlageansicht : Panel
{
    public const string StartHinweis =
        "„Nachschlagen“ öffnet die Arzneimittelsuche des BfArM – erst dann verbindet sich die Ansicht. Gesucht wird nur mit dem Namen " +
        "des Medikaments. Angaben bitte selbst lesen und übertragen: Text markieren, Feld in der Maske anklicken, „In Feld übernehmen“.";
    public const string NichtVerfuegbarText =
        "Die Nachschlage-Ansicht lässt sich auf diesem Rechner nicht starten (Microsoft Edge WebView2 fehlt oder ist gesperrt). " +
        "Das Formular bleibt voll nutzbar; „Im Browser öffnen“ geht weiterhin. Abschalten: Einstellungen → Medikamente.";

    private readonly Panel flaeche = new() { Dock = DockStyle.Fill, BackColor = Stil.Flaeche, BorderStyle = BorderStyle.FixedSingle };
    private WebView2? web;

    public Button NachschlagenKnopf { get; } = Stil.NebenKnopf("Nachschlagen");
    public Button BrowserKnopf { get; } = Stil.NebenKnopf("Im Browser öffnen");
    public Button UebernehmenKnopf { get; } = Stil.NebenKnopf("In Feld übernehmen");
    public Button QuelleKnopf { get; } = Stil.NebenKnopf("Adresse als Quelle übernehmen");
    public Label Ziel { get; } = new() { AutoSize = true, ForeColor = Stil.TextMittel, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandGross, 0) };
    public Label Hinweis { get; } = new() { Text = StartHinweis, Dock = DockStyle.Fill, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche, Padding = new Padding(Stil.AbstandGross) };
    public Label Status { get; } = new() { AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextLeise, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) };

    // Die gerade gezeigte Adresse (nur zulässige), sonst null.
    public Uri? Adresse { get; private set; }

    // Blockierte Anfragen dieser Ansicht (für das Protokoll beim Schließen).
    public List<string> Blockiert { get; } = [];

    // Liefert den Namen aus der Maske (Wirkstoff, sonst Handelsname) – sonst nichts.
    public Func<string> Suchname { get; set; } = () => "";
    public event Action<string>? TextUebernehmen;
    public event Action<string>? QuelleUebernehmen;

    // Zum Testen ersetzbar: Laden einer Adresse, markierten Text lesen, Zwischenablage.
    internal static Func<Nachschlageansicht, Uri, Task>? LadenErsatz { get; set; }
    internal static Func<Nachschlageansicht, Task<string>>? MarkierungErsatz { get; set; }
    internal static Action<string> Zwischenablage { get; set; } = text => Clipboard.SetText(text);

    public Nachschlageansicht()
    {
        BackColor = Stil.Fensterhintergrund;
        var oben = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Padding = new Padding(0, 0, 0, Stil.AbstandKlein) };
        var titel = new Label { Text = "Nachschlagen beim BfArM", AutoSize = true, Font = Stil.Fett, ForeColor = Stil.TextMittel, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandGross, 0) };
        foreach (var k in new[] { NachschlagenKnopf, BrowserKnopf, UebernehmenKnopf, QuelleKnopf })
        {
            k.AutoSize = true;
            k.Margin = new Padding(0, 0, Stil.AbstandKlein, 0);
        }
        oben.Controls.AddRange([titel, NachschlagenKnopf, BrowserKnopf]);
        var unten = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, WrapContents = true, Padding = new Padding(0, Stil.AbstandKlein, 0, 0) };
        unten.Controls.AddRange([UebernehmenKnopf, Ziel, QuelleKnopf, Status]);
        flaeche.Controls.Add(Hinweis);
        Controls.Add(flaeche);
        Controls.Add(unten);
        Controls.Add(oben);
        NachschlagenKnopf.Click += async (_, _) => await NachschlagenAsync();
        BrowserKnopf.Click += (_, _) => ImBrowserOeffnen();
        UebernehmenKnopf.Click += async (_, _) => Uebernehmen(await (MarkierungErsatz ?? MarkierungLesen)(this));
        QuelleKnopf.Click += (_, _) => AlsQuelleUebernehmen();
        KnoepfeSetzen();
    }

    public void ZielSetzen(string feld) => Ziel.Text = $"→ {feld}";

    private void KnoepfeSetzen() => UebernehmenKnopf.Enabled = QuelleKnopf.Enabled = Adresse is not null;

    private void HinweisZeigen(string text)
    {
        if (web is not null) web.Visible = false;
        Hinweis.Text = text;
        Hinweis.Visible = true;
        Adresse = null;
        KnoepfeSetzen();
    }

    // Legt den Namen aus der Maske in die Zwischenablage (die BfArM-Suche nimmt ihn nicht in der
    // Adresse an) und öffnet die Suche in der Ansicht.
    public async Task NachschlagenAsync()
    {
        NameBereitlegen();
        try
        {
            await (LadenErsatz ?? Laden)(this, Nachschlagen.Start);
            Adresse = Nachschlagen.Start;
            Hinweis.Visible = false;
            KnoepfeSetzen();
        }
        catch (Exception x) when (x is not OutOfMemoryException)
        {
            HinweisZeigen(NichtVerfuegbarText);
        }
    }

    private void NameBereitlegen()
    {
        string begriff = Nachschlagen.Suchbegriff(Suchname());
        if (begriff.Length == 0)
        {
            Status.Text = "Tipp: Erst den Wirkstoff eintragen – dann liegt er zum Einfügen bereit.";
            return;
        }
        try
        {
            Zwischenablage(begriff);
            Status.Text = $"„{begriff}“ liegt in der Zwischenablage – im Suchfeld des BfArM mit Strg+V einfügen.";
        }
        catch (System.Runtime.InteropServices.ExternalException) { Status.Text = $"Im Suchfeld des BfArM „{begriff}“ eingeben."; }
    }

    // Markierten Text in das gewählte Feld der Maske (bereinigt, eine Zeile).
    public bool Uebernehmen(string markiert)
    {
        string text = Nachschlagen.Bereinigen(markiert);
        if (text.Length == 0)
        {
            Status.Text = "Bitte zuerst in der Ansicht Text markieren.";
            return false;
        }
        TextUebernehmen?.Invoke(text);
        Status.Text = $"Übernommen: „{(text.Length > 40 ? text[..40] + " …" : text)}“";
        return true;
    }

    public bool AlsQuelleUebernehmen()
    {
        if (!Nachschlagen.Zulaessig(Adresse)) return false;
        QuelleUebernehmen?.Invoke(Adresse!.AbsoluteUri);
        return true;
    }

    // Übergibt die gezeigte (oder die Start-)Adresse an den normalen Browser.
    public void ImBrowserOeffnen()
    {
        NameBereitlegen();
        Extern.Oeffnen((Nachschlagen.Zulaessig(Adresse) ? Adresse! : Nachschlagen.Start).AbsoluteUri);
    }

    // ------------------------------------------------------------------ WebView2

    // Beim Start geprüft (Program.cs) und in den Einstellungen gezeigt: Ist WebView2 vorhanden?
    private static string? laufzeit;
    public static string Laufzeit => laufzeit ??= LaufzeitPruefen();

    // Für den Selbsttest der .exe: Ist der WebView2-Lader (eigene DLL im Paket) mitgeliefert?
    public static bool LaderVorhanden()
    {
        if (!System.Runtime.InteropServices.NativeLibrary.TryLoad("WebView2Loader.dll", typeof(CoreWebView2Environment).Assembly, null, out var lader))
            return false;
        System.Runtime.InteropServices.NativeLibrary.Free(lader);
        return true;
    }

    private static string LaufzeitPruefen()
    {
        try
        {
            string version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            return string.IsNullOrWhiteSpace(version) ? "fehlt" : $"vorhanden (Version {version})";
        }
        catch (Exception x) when (x is not OutOfMemoryException)
        {
            return "fehlt";
        }
    }

    private static async Task Laden(Nachschlageansicht ansicht, Uri adresse)
    {
        if (ansicht.web is null) await ansicht.WebAnlegen();
        ansicht.web!.Visible = true;
        ansicht.web.BringToFront();
        ansicht.web.CoreWebView2.Navigate(adresse.AbsoluteUri);
    }

    private async Task WebAnlegen()
    {
        var neu = new WebView2 { Dock = DockStyle.Fill, Visible = false };
        flaeche.Controls.Add(neu);
        web = neu;
        // Daten der Ansicht (Cookies, Zwischenspeicher) im Datenordner, nicht neben der .exe.
        var umgebung = await CoreWebView2Environment.CreateAsync(null, Pfade.ImBenutzerordner("webansicht"));   // je Benutzer, nicht im gemeinsamen Ordner
        var optionen = umgebung.CreateCoreWebView2ControllerOptions();
        optionen.IsInPrivateModeEnabled = true;   // Cookies, Verlauf, Zwischenspeicher nur bis zum Schließen
        await neu.EnsureCoreWebView2Async(umgebung, optionen);
        var core = neu.CoreWebView2;
        core.Settings.IsReputationCheckingRequired = false;   // SmartScreen: sonst gingen die Adressen an Microsoft
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.IsPasswordAutosaveEnabled = false;
        core.Settings.IsGeneralAutofillEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
        core.WebResourceRequested += (_, e) =>
        {
            if (Nachschlagen.RessourceZulaessig(e.Request.Uri)) return;
            e.Response = core.Environment.CreateWebResourceResponse(null, 403, "Blockiert", "");
            Blockiert.Add(e.Request.Uri);
            Status.Text = $"{Blockiert.Count} Anfragen an andere Adressen blockiert (z. B. Schriften oder Statistik).";
        };
        core.NavigationStarting += (_, e) =>
        {
            if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var ziel) && Nachschlagen.Zulaessig(ziel)) return;
            e.Cancel = true;   // fremde Adresse: nicht in der Ansicht, sondern im normalen Browser
            if (ziel is not null && (ziel.Scheme == Uri.UriSchemeHttps || ziel.Scheme == Uri.UriSchemeHttp)) Extern.Oeffnen(ziel.AbsoluteUri);
        };
        core.NewWindowRequested += (_, e) =>
        {
            e.Handled = true;   // keine neuen Fenster: zulässige Adressen in der Ansicht, andere im Browser
            if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var ziel)) return;
            if (Nachschlagen.Zulaessig(ziel)) core.Navigate(ziel.AbsoluteUri);
            else if (ziel.Scheme == Uri.UriSchemeHttps || ziel.Scheme == Uri.UriSchemeHttp) Extern.Oeffnen(ziel.AbsoluteUri);
        };
        core.DownloadStarting += (_, e) =>
        {
            e.Cancel = true;   // nichts still speichern; Dateien öffnet der normale Browser
            if (Uri.TryCreate(e.DownloadOperation.Uri, UriKind.Absolute, out var ziel) && Nachschlagen.Zulaessig(ziel)) Extern.Oeffnen(ziel.AbsoluteUri);
        };
        core.SourceChanged += (_, _) =>
        {
            Adresse = Nachschlagen.Zulaessig(neu.Source) ? neu.Source : null;
            KnoepfeSetzen();
        };
        core.NavigationCompleted += (_, e) =>
        {
            if (e.IsSuccess || e.WebErrorStatus == CoreWebView2WebErrorStatus.OperationCanceled) return;
            HinweisZeigen("Das BfArM ist gerade nicht erreichbar (keine Verbindung?). Das Formular bleibt voll nutzbar.");
        };
    }

    // Beim Schließen: die blockierten Anfragen einmal ins Protokoll im Datenordner (nur Rechnernamen).
    protected override void Dispose(bool disposing)
    {
        if (disposing && Blockiert.Count > 0)
        {
            try { File.AppendAllText(Pfade.ImDatenordner(Nachschlagen.Protokoll), Nachschlagen.ProtokollZeile(DateTime.Now, Blockiert) + Environment.NewLine); }
            catch (Exception x) when (x is IOException or UnauthorizedAccessException) { }   // ohne Protokoll geht es auch
            Blockiert.Clear();
        }
        base.Dispose(disposing);
    }

    private async Task<string> MarkierungLesen(Nachschlageansicht _)
    {
        if (web?.CoreWebView2 is null) return "";
        string json = await web.CoreWebView2.ExecuteScriptAsync("window.getSelection().toString()");
        return JsonSerializer.Deserialize<string>(json) ?? "";
    }
}
