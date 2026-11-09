using DokuLine.Kern;

namespace DokuLine;

// Die Seitenleiste: Einträge der Module, ein- und ausklappen, Anordnung.
public sealed partial class Hauptfenster
{
    // Seitenleiste und Seiten der Module: Eingeschaltete Module bekommen einen Eintrag vor
    // „Einstellungen“ und einen Reiter in der Akte; ausgeschaltete verschwinden ganz (die Daten bleiben).
    public void ModuleAnzeigen()
    {
        foreach (var (name, seite) in modulSeiten)
        {
            seiten.Remove(name);
            bereichsFlaeche.Controls.Remove(seite);
            seite.Dispose();
        }
        foreach (var eintrag in Navigation.Where(e => modulSeiten.ContainsKey(e.Text)).ToList())
        {
            Navigation.Remove(eintrag);
            leiste.Controls.Remove(eintrag);
            eintrag.Dispose();
        }
        modulSeiten.Clear();
        int vor = Navigation.FindIndex(e => e.Text == "Einstellungen");
        foreach (var m in Modulverwaltung.Eingeschaltet(speicher))
        {
            string name = m.Name;
            var seite = m.SeiteBauen(speicher);
            seite.Dock = DockStyle.Fill;
            seite.Visible = false;
            seite.Titel = name;
            seite.Geaendert += () => { if (AktiverBereich == name) Kopf.TexteSetzen(seite.KopfText, name); };
            seiten[name] = seite;
            modulSeiten[name] = seite;
            bereichsFlaeche.Controls.Add(seite);
            var eintrag = new SeitenleistenEintrag(name) { Symbol = Symbole.Fuer(name), Eingeklappt = MenueEingeklappt };
            eintrag.Click += (_, _) => BereichWaehlen(name);
            hinweis.SetToolTip(eintrag, MenueEingeklappt ? name : null);
            Navigation.Insert(vor++, eintrag);
            leiste.Controls.Add(eintrag);
        }
        Bewohner.Akte.ModuleAktualisieren();
        leiste.PerformLayout();
        if (AktiverBereich.Length == 0) return;
        if (!seiten.ContainsKey(Schluessel(AktiverBereich))) BereichWaehlen("Dokumentation");
        else if (AktiverBereich == "Dokumentation" && !Bewohner.AkteSichtbar) Bewohner.UebersichtZeigen();   // Marken der Module
        else if (modulSeiten.ContainsKey(AktiverBereich) || AktiverBereich == "Übersicht") BereichWaehlen(AktiverBereich);
    }

    // Seitenmenü ein- oder ausklappen; der Zustand bleibt in den Einstellungen.
    public void MenueUmschalten() => MenueSetzen(!MenueEingeklappt, merken: true);

    private void MenueSetzen(bool eingeklappt, bool merken)
    {
        MenueEingeklappt = eingeklappt;
        if (merken) Einstellungen.Aendern(w => w with { MenueEingeklappt = eingeklappt });
        MenueKnopf.Text = eingeklappt ? "Menü ausklappen" : "Menü einklappen";
        MenueKnopf.AccessibleName = MenueKnopf.Text;
        hinweis.SetToolTip(MenueKnopf, MenueKnopf.Text);
        foreach (var eintrag in Navigation)
        {
            eintrag.Eingeklappt = eingeklappt;
            hinweis.SetToolTip(eintrag, eingeklappt ? eintrag.Text : null);
        }
        AbmeldenEintrag.Eingeklappt = eingeklappt;
        programmName.Visible = LeittextAnzeige.Visible = Arbeitsplatz.Visible = VersionText.Visible = !eingeklappt;
        InfoSymbol.Visible = eingeklappt;
        InfoAktualisieren();
        leiste.Width = S(eingeklappt ? LeisteSchmal : LeisteBreite);   // die Inhalte nutzen die Fläche sofort (Dock)
        leiste.PerformLayout();
        leiste.Invalidate();
    }

    // Eingeklappt: Version und Bereich als Hinweis am Info-Symbol.
    private void InfoAktualisieren()
    {
        InfoSymbol.Text = $"{VersionText.Text}\n{Arbeitsplatz.Text}\n{Programmversion.Name} – {Programmversion.Leittext}";
        InfoSymbol.AccessibleName = InfoSymbol.Text.Replace("\n", " · ");
        hinweis.SetToolTip(InfoSymbol, InfoSymbol.Text);
    }

    private void LeisteAnordnen()
    {
        int x = leiste.Padding.Left, breite = leiste.ClientSize.Width - leiste.Padding.Horizontal;
        int y = leiste.Padding.Top;
        MenueKnopf.Bounds = new Rectangle(x, y, MenueEingeklappt ? breite : S(40), S(36));
        // Name und darunter der Leittext, zusammen mittig neben dem Menüknopf.
        int namensblock = programmName.PreferredHeight + LeittextAnzeige.PreferredHeight;
        programmName.Location = new Point(MenueKnopf.Right + S(Stil.AbstandKlein), y + Math.Max(0, (S(36) - namensblock) / 2));
        LeittextAnzeige.Location = new Point(programmName.Left, programmName.Bottom);
        y = MenueKnopf.Bottom + S(Stil.AbstandGross);
        foreach (var eintrag in Navigation.Where(e => e.Text != "Einstellungen" && !OhneRechtAusgeblendet.Contains(e.Text)))
        {
            eintrag.Bounds = new Rectangle(x, y, breite, S(36));
            y += eintrag.Height + S(Stil.AbstandMini);
        }

        // Unten fest: „Einstellungen“, darunter Arbeitsplatz und Version bzw. das Info-Symbol.
        int unten = leiste.ClientSize.Height - leiste.Padding.Bottom;
        var einstellungen = Navigation.Single(e => e.Text == "Einstellungen");
        if (MenueEingeklappt)
        {
            InfoSymbol.Bounds = new Rectangle(x, unten - S(36), breite, S(36));
            AbmeldenEintrag.Bounds = new Rectangle(x, InfoSymbol.Top - S(Stil.AbstandMini) - S(36), breite, S(36));
            unten = AbmeldenEintrag.Top - S(Stil.AbstandMini);
        }
        else
        {
            VersionText.Location = new Point(x + S(Stil.AbstandMini), unten - VersionText.PreferredHeight);
            Arbeitsplatz.MaximumSize = new Size(breite - S(Stil.AbstandMini), 0);
            Arbeitsplatz.Location = new Point(x + S(Stil.AbstandMini), VersionText.Top - S(Stil.AbstandKlein) - Arbeitsplatz.PreferredHeight);
            AbmeldenEintrag.Bounds = new Rectangle(x, Arbeitsplatz.Top - S(Stil.AbstandKlein) - S(36), breite, S(36));
            unten = AbmeldenEintrag.Top - S(Stil.AbstandMini);
        }
        einstellungen.Bounds = new Rectangle(x, Math.Max(y + S(Stil.AbstandMittel), unten - S(36)), breite, S(36));
    }
}
