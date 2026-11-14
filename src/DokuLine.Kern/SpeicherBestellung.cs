using System.Globalization;

namespace DokuLine.Kern;

// Medikamentenbestellung im Speicher (Bauplan 51): Zählungen und Lieferungen (Bestand, nur wenn eingeschaltet),
// Anforderungen mit Posten und Verlauf (nie geändert, der neueste Stand gilt), Einstellungen für das Haus.
public sealed partial class Speicher
{
    public bool BestandFuehren(string bereich) => Meta($"bestand_fuehren:{bereich}") == "1";

    public int BestellTage() => int.TryParse(Meta("bestell_tage"), NumberStyles.None, CultureInfo.InvariantCulture, out int t) && t is >= 1 and <= 60 ? t : BestellRegeln.TageVorgabe;

    public (string Name, string Email) HeimApotheke() => (Meta("apotheke_name") ?? "", Meta("apotheke_email") ?? "");

    public bool EmailFreigegeben(string empfaenger) => Meta($"email_freigegeben:{empfaenger.Trim().ToLowerInvariant()}") == "1";

    public void BestellEinstellungen(string bereich, bool bestand, int tage, string apotheke, string email, string kuerzel)
    {
        if (Grund(kuerzel, Funktionen.EinstellungenBereich) is string grund) throw new Eingabefehler([grund]);
        if (tage is < 1 or > 60) throw new Eingabefehler(["Tage bitte von 1 bis 60."]);
        if (email.Trim().Length > 0 && !BetreuerRegeln.EmailGueltig(email.Trim())) throw new Eingabefehler(["Die E-Mail-Adresse der Apotheke ist ungültig."]);
        SetzeMeta($"bestand_fuehren:{bereich}", bestand ? "1" : "0", null);
        SetzeMeta("bestell_tage", tage.ToString(CultureInfo.InvariantCulture), null);
        SetzeMeta("apotheke_name", apotheke.Trim(), null);
        SetzeMeta("apotheke_email", email.Trim(), null);
    }

    // „E-Mail an diesen Empfänger ist von der Einrichtung freigegeben“ – danach kein Hinweis bei jeder Bestellung.
    public void EmailFreigeben(string empfaenger, bool frei, string kuerzel)
    {
        if (Grund(kuerzel, Funktionen.EinstellungenBereich) is string grund) throw new Eingabefehler([grund]);
        SetzeMeta($"email_freigegeben:{empfaenger.Trim().ToLowerInvariant()}", frei ? "1" : "0", null);
    }

    public Bestandszaehlung? LetzteZaehlung(long verordnungId)
    {
        using var befehl = Befehl("SELECT verordnung_id, menge, packung, zeit, kuerzel FROM med_bestand WHERE verordnung_id = $v AND art = 'gezaehlt' ORDER BY zeit DESC, id DESC LIMIT 1",
            null, [("$v", verordnungId)]);
        using var leser = befehl.ExecuteReader();
        return leser.Read()
            ? new Bestandszaehlung(leser.GetInt64(0), decimal.Parse(leser.GetString(1), CultureInfo.InvariantCulture), leser.GetInt32(2), AusZeit(leser.GetString(3)), leser.GetString(4))
            : null;
    }

    private List<(DateTime Zeit, decimal Menge)> Lieferungen(long verordnungId)
    {
        var liste = new List<(DateTime, decimal)>();
        using var befehl = Befehl("SELECT zeit, menge FROM med_bestand WHERE verordnung_id = $v AND art = 'geliefert' ORDER BY zeit", null, [("$v", verordnungId)]);
        using var leser = befehl.ExecuteReader();
        while (leser.Read()) liste.Add((AusZeit(leser.GetString(0)), decimal.Parse(leser.GetString(1), CultureInfo.InvariantCulture)));
        return liste;
    }

    public void BestandZaehlen(long verordnungId, decimal stueck, int packung, string kuerzel, DateTime jetzt)
    {
        if (Grund(kuerzel, Funktionen.MedikamenteAusgeben) is string grund) throw new Eingabefehler([grund]);
        if (stueck < 0 || packung < 0) throw new Eingabefehler(["Bitte keine negativen Mengen."]);
        Ausfuehren("INSERT INTO med_bestand (verordnung_id, art, menge, packung, zeit, kuerzel) VALUES ($v, 'gezaehlt', $m, $p, $z, $k)", null,
            ("$v", verordnungId), ("$m", stueck.ToString(CultureInfo.InvariantCulture)), ("$p", packung), ("$z", Zeit(jetzt)), ("$k", kuerzel.Trim()));
    }

    // Die Liste der Seite „Bestellung“: mit Bestand alles, was in den nächsten n Tagen ausgeht; ohne Bestand alle regelmäßigen
    // Verordnungen der Bewohner (Auswahl von Hand).
    public List<BestellPosten> BestellListe(IEnumerable<Bewohner> bewohner, string bereich, DateOnly heute)
    {
        bool bestand = BestandFuehren(bereich);
        int tage = BestellTage();
        var (apotheke, apoEmail) = HeimApotheke();
        var personen = Personen().ToDictionary(p => p.Id);
        var ausgaben = bestand ? AusgabeEintraege(heute.AddDays(-120), heute) : [];
        var liste = new List<BestellPosten>();
        foreach (var b in bewohner)
        {
            var weg = Angaben(b.Id!.Value, heute).GetValueOrDefault(Angabe.Bestellweg)?.Lesen<BestellwegAngabe>();
            var person = weg?.PersonId is long pid && personen.TryGetValue(pid, out var p) ? p : null;
            foreach (var s in Verordnungen(b.Id!.Value).Where(v => !v.Bedarf).Select(v => MedikationRegeln.Stand(v, heute)).Where(s => s.Zustand is Zustand.Gilt or Zustand.Stufenplan))
            {
                var v = s.Verordnung;
                decimal? vorrat = bestand ? BestellRegeln.Vorrat(LetzteZaehlung(v.Id), Lieferungen(v.Id), ausgaben, v) : null;
                DateOnly? reicht = vorrat is decimal m ? BestellRegeln.ReichtBis(m, BestellRegeln.ProTag(s.Dosis), heute) : null;
                if (bestand && reicht is DateOnly r && r > heute.AddDays(tage)) continue;
                string empfaenger = BestellRegeln.Empfaenger(weg?.Weg ?? Bestellweg.Hausarzt, person?.Name, v, apotheke);
                string email = empfaenger == apotheke || empfaenger == BestellRegeln.Apotheke ? apoEmail : person?.Email ?? "";
                liste.Add(new BestellPosten(b, v, vorrat, reicht, empfaenger, email));
            }
        }
        return [.. liste.OrderBy(x => x.Empfaenger, StringComparer.CurrentCulture).ThenBy(x => x.ReichtBis ?? DateOnly.MaxValue).ThenBy(x => x.Bewohner.Nachname)];
    }

    // „Anforderung erstellen …“: je Empfänger eine Anforderung mit ihren Posten.
    public List<Anforderung> AnforderungenErstellen(IEnumerable<(BestellPosten Posten, string Menge)> auswahl, string kuerzel, DateTime jetzt)
    {
        if (Grund(kuerzel, Funktionen.MedikamenteAusgeben) is string grund) throw new Eingabefehler([grund]);
        var gruppen = auswahl.GroupBy(x => (x.Posten.Empfaenger, x.Posten.Email)).ToList();
        if (gruppen.Count == 0) throw new Eingabefehler(["Bitte zuerst Medikamente ankreuzen."]);
        var ids = new List<long>();
        using (var transaktion = db.BeginTransaction())
        {
            foreach (var g in gruppen)
            {
                Ausfuehren("INSERT INTO med_anforderungen (empfaenger, email, erstellt, kuerzel) VALUES ($e, $m, $z, $k)", transaktion,
                    ("$e", g.Key.Empfaenger), ("$m", g.Key.Email), ("$z", Zeit(jetzt)), ("$k", kuerzel.Trim()));
                long id = Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
                ids.Add(id);
                foreach (var (p, menge) in g)
                    Ausfuehren("INSERT INTO med_anforderung_posten (anforderung_id, verordnung_id, bewohner_id, text, menge) VALUES ($a, $v, $b, $t, $m)", transaktion,
                        ("$a", id), ("$v", p.Verordnung.Id), ("$b", p.Bewohner.Id), ("$t", BestellRegeln.PostenText(p.Bewohner, p.Verordnung)),
                        ("$m", menge.Trim().Length == 0 ? "1 Packung" : menge.Trim()));
            }
            transaktion.Commit();
        }
        return [.. Anforderungen().Where(a => ids.Contains(a.Id))];
    }

    public List<Anforderung> Anforderungen(bool auchErledigte = true)
    {
        var posten = new Dictionary<long, List<AnforderungPosten>>();
        using (var befehl = Befehl("SELECT anforderung_id, verordnung_id, bewohner_id, text, menge FROM med_anforderung_posten ORDER BY id", null, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
            {
                long a = leser.GetInt64(0);
                if (!posten.TryGetValue(a, out var l)) posten[a] = l = [];
                l.Add(new AnforderungPosten(leser.GetInt64(1), leser.GetInt64(2), leser.GetString(3), leser.GetString(4)));
            }
        var liste = new List<Anforderung>();
        using (var befehl = Befehl("""
            SELECT a.id, a.empfaenger, a.email, a.erstellt, a.kuerzel,
                   (SELECT s.stand FROM med_anforderung_stand s WHERE s.anforderung_id = a.id ORDER BY s.zeit DESC, s.id DESC LIMIT 1),
                   (SELECT s.zeit FROM med_anforderung_stand s WHERE s.anforderung_id = a.id ORDER BY s.zeit DESC, s.id DESC LIMIT 1)
            FROM med_anforderungen a ORDER BY a.erstellt DESC, a.id DESC
            """, null, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
            {
                var stand = leser.IsDBNull(5) ? AnforderungStand.Angefordert : Enum.Parse<AnforderungStand>(leser.GetString(5), true);
                var erstellt = AusZeit(leser.GetString(3));
                liste.Add(new Anforderung(leser.GetInt64(0), leser.GetString(1), leser.GetString(2), erstellt, leser.GetString(4),
                    posten.GetValueOrDefault(leser.GetInt64(0)) ?? [], stand, leser.IsDBNull(6) ? erstellt : AusZeit(leser.GetString(6))));
            }
        return auchErledigte ? liste : [.. liste.Where(a => a.Stand != AnforderungStand.Erledigt)];
    }

    // Weiter im Verlauf; „geliefert“ bucht bei eingeschaltetem Bestand die Mengen ein (je Posten, Stück).
    public void AnforderungWeiter(long id, AnforderungStand stand, IReadOnlyDictionary<long, decimal>? geliefert, string kuerzel, DateTime jetzt)
    {
        if (Grund(kuerzel, Funktionen.MedikamenteAusgeben) is string grund) throw new Eingabefehler([grund]);
        var a = Anforderungen().FirstOrDefault(x => x.Id == id) ?? throw new Eingabefehler(["Diese Anforderung gibt es nicht mehr."]);
        if (stand <= a.Stand) throw new Eingabefehler([$"Die Anforderung ist schon „{BestellRegeln.StandText(a.Stand)}“."]);
        using var transaktion = db.BeginTransaction();
        Ausfuehren("INSERT INTO med_anforderung_stand (anforderung_id, stand, zeit, kuerzel) VALUES ($a, $s, $z, $k)", transaktion,
            ("$a", id), ("$s", stand.ToString().ToLowerInvariant()), ("$z", Zeit(jetzt)), ("$k", kuerzel.Trim()));
        foreach (var (verordnung, menge) in geliefert ?? new Dictionary<long, decimal>())
            if (menge > 0)
                Ausfuehren("INSERT INTO med_bestand (verordnung_id, art, menge, packung, zeit, kuerzel) VALUES ($v, 'geliefert', $m, 0, $z, $k)", transaktion,
                    ("$v", verordnung), ("$m", menge.ToString(CultureInfo.InvariantCulture)), ("$z", Zeit(jetzt)), ("$k", kuerzel.Trim()));
        transaktion.Commit();
    }

    // Erinnerung als Aufgabe des Bereichs, wenn nach n Tagen nichts geliefert wurde – einmal je Anforderung.
    public int BestellErinnerungen(DateTime jetzt)
    {
        int neu = 0;
        foreach (var a in Anforderungen(auchErledigte: false).Where(a => BestellRegeln.ErinnernNoetig(a, jetzt)))
        {
            if (Meta($"bestell_erinnert:{a.Id}") is not null || a.Posten.Count == 0 || Bewohner(a.Posten[0].BewohnerId) is not Bewohner b) continue;
            SetzeMeta($"bestell_erinnert:{a.Id}", "1", null);
            AufgabeSpeichern(new Aufgabe
            {
                Bereich = b.Wohnbereich, Text = $"Bestellung bei {a.Empfaenger} vom {a.Erstellt:dd.MM.} ist noch nicht geliefert – bitte nachfragen.",
                Fuer = AufgabeFuer.WerZeitHat, Erstellt = jetzt, Verfasser = "AUTO",
            });
            neu++;
        }
        return neu;
    }
}
