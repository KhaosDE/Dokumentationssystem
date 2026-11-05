# IDEEN — Dokumentationssystem

Aufträge aus den Chats. Gebaut wird nur, was `Status: freigegeben` hat
(Regel: `CLAUDE.md`, „Arbeitsweise“).

Aufbau eines Eintrags:

```
## <Titel>
Status: offen | freigegeben | erledigt (<Datum>)
Datum: <Datum>
Was: <ein bis drei Sätze>
Warum: <wofür Tobi es braucht>
Umfang: <was dazu gehört, was ausdrücklich nicht>
```

## Bewohnerakte
Status: offen (wartet auf die Entscheidung zur Technik)
Datum: 05.11.2026
Was: Eine Akte pro Bewohner mit den wichtigsten Angaben auf einen Blick, dazu eine
Übersicht aller Bewohner des Wohnbereichs.
Warum: Kern der Demo. In DANlife stehen wichtige Angaben (z. B. Beschluss) an
pflegebezogenen Stellen, die im Wohnbereich ohne Pflege kaum jemand findet.
Umfang:
- Felder laut Tobi: Name, Zimmernummer, Geburtsdatum, Aufnahmedatum, Beschlussende,
  Hinweis auf Allergien, Platz für ein Bewohnerbild.
- Allergiehinweis mit drei Zuständen (`CLAUDE.md`, Regel 5), gut sichtbar oben in der Akte.
- Bewohnerbild: Platzhalter mit Initialen, wenn kein Bild hinterlegt ist.
- Demo-Daten: ausgedachte Bewohner, Anzahl legt Tobi fest.
- **Noch nicht entschieden (Vorschläge, nicht bauen):** Warnung vor Beschlussende,
  Angabe „offen geführt ja/nein“, Alter aus dem Geburtsdatum berechnet.
- Ausdrücklich nicht: Tagesdokumentation, Medikation, Ausgang (eigene Einträge).
