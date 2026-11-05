"""Bewohner: Datenmodell, Prüfregeln und Anzeigetexte (ohne Oberfläche).

Sicherheitsangaben haben drei Zustände (Regel 5 in CLAUDE.md): eingetragen, „keine
bekannt“ bzw. „kein Beschluss“, und „nicht erfasst“. Neu angelegte Bewohner stehen immer
auf „nicht erfasst“ – ein leeres Feld sieht nie wie „keine Allergie“ aus.
"""

from __future__ import annotations

import datetime as _dt
import re
from dataclasses import dataclass

NICHT_ERFASST = "nicht_erfasst"

ALLERGIE_EINGETRAGEN = "eingetragen"
ALLERGIE_KEINE = "keine_bekannt"
ALLERGIE_ZUSTAENDE = (ALLERGIE_EINGETRAGEN, ALLERGIE_KEINE, NICHT_ERFASST)

BESCHLUSS_ENDE = "ende"
BESCHLUSS_KEINER = "kein_beschluss"
BESCHLUSS_ZUSTAENDE = (BESCHLUSS_ENDE, BESCHLUSS_KEINER, NICHT_ERFASST)

_DATUM = re.compile(r"^(\d{1,2})\.(\d{1,2})\.(\d{4})$")


class Eingabefehler(Exception):
    """Eingaben passen nicht. ``fehler`` ist eine Liste verständlicher Sätze."""

    def __init__(self, fehler: list[str]) -> None:
        super().__init__(" ".join(fehler))
        self.fehler = fehler


@dataclass
class Bewohner:
    vorname: str
    nachname: str
    zimmer: str
    geburtsdatum: _dt.date
    aufnahmedatum: _dt.date
    allergie: str = NICHT_ERFASST
    allergien: str = ""
    beschluss: str = NICHT_ERFASST
    beschlussende: _dt.date | None = None
    bild: str | None = None
    id: int | None = None

    @property
    def name(self) -> str:
        return f"{self.vorname} {self.nachname}"


# --------------------------------------------------------------------------- Datum

def datum_text(datum: _dt.date | None) -> str:
    """Datum im Format TT.MM.JJJJ (leer, wenn keins)."""
    return datum.strftime("%d.%m.%Y") if datum else ""


def datum_lesen(text: str, feld: str) -> _dt.date:
    """Liest TT.MM.JJJJ (führende Nullen dürfen fehlen). Fehler als verständlicher Satz."""
    text = (text or "").strip()
    if not text:
        raise ValueError(f"{feld} fehlt.")
    treffer = _DATUM.match(text)
    if not treffer:
        raise ValueError(f"{feld}: bitte im Format TT.MM.JJJJ eingeben, z. B. 03.04.1965.")
    tag, monat, jahr = (int(t) for t in treffer.groups())
    try:
        return _dt.date(jahr, monat, tag)
    except ValueError:
        raise ValueError(f"{feld}: Den {text} gibt es nicht.") from None


# --------------------------------------------------------------------------- Prüfregeln

def pruefen(b: Bewohner) -> list[str]:
    """Alle Prüfregeln für einen vollständigen Bewohner. Leere Liste = in Ordnung."""
    fehler = []
    if not b.vorname.strip():
        fehler.append("Vorname fehlt.")
    if not b.nachname.strip():
        fehler.append("Nachname fehlt.")
    if not b.zimmer.strip():
        fehler.append("Zimmernummer fehlt.")
    if b.geburtsdatum and b.aufnahmedatum and b.aufnahmedatum < b.geburtsdatum:
        fehler.append("Das Aufnahmedatum liegt vor dem Geburtsdatum.")

    if b.allergie not in ALLERGIE_ZUSTAENDE:
        fehler.append("Allergien: unbekannte Angabe.")
    elif b.allergie == ALLERGIE_EINGETRAGEN and not b.allergien.strip():
        fehler.append("Allergien: bitte eintragen, welche – oder „keine bekannt“ bzw. "
                      "„nicht erfasst“ wählen.")
    elif b.allergie != ALLERGIE_EINGETRAGEN and b.allergien.strip():
        fehler.append("Allergien: Text nur bei eingetragenen Allergien.")

    if b.beschluss not in BESCHLUSS_ZUSTAENDE:
        fehler.append("Beschluss: unbekannte Angabe.")
    elif b.beschluss == BESCHLUSS_ENDE and b.beschlussende is None:
        fehler.append("Beschlussende fehlt.")
    elif b.beschluss != BESCHLUSS_ENDE and b.beschlussende is not None:
        fehler.append("Beschlussende nur, wenn es einen Beschluss gibt.")
    return fehler


def aus_eingabe(eingabe: dict[str, str], id: int | None = None, bild: str | None = None) -> Bewohner:
    """Baut einen Bewohner aus Texteingaben (Formular oder Demo-Datei).

    Schlüssel: vorname, nachname, zimmer, geburtsdatum, aufnahmedatum (TT.MM.JJJJ),
    allergie + allergien, beschluss + beschlussende. Fehlt ein Zustand, gilt „nicht erfasst“.
    Wirft ``Eingabefehler`` mit allen Fehlern auf einmal.
    """
    def wert(schluessel: str) -> str:
        return str(eingabe.get(schluessel) or "").strip()

    fehler: list[str] = []
    unlesbar: set[str] = set()  # Datumsfelder, zu denen schon ein Fehler notiert ist

    def datum(schluessel: str, feld: str) -> _dt.date | None:
        try:
            return datum_lesen(wert(schluessel), feld)
        except ValueError as e:
            fehler.append(str(e))
            unlesbar.add(feld)
            return None

    geburt = datum("geburtsdatum", "Geburtsdatum")
    aufnahme = datum("aufnahmedatum", "Aufnahmedatum")
    allergie = wert("allergie") or NICHT_ERFASST
    beschluss = wert("beschluss") or NICHT_ERFASST
    ende = datum("beschlussende", "Beschlussende") if beschluss == BESCHLUSS_ENDE else None

    b = Bewohner(
        vorname=wert("vorname"),
        nachname=wert("nachname"),
        zimmer=wert("zimmer"),
        geburtsdatum=geburt,  # type: ignore[arg-type]  # None nur, wenn schon ein Fehler notiert ist
        aufnahmedatum=aufnahme,  # type: ignore[arg-type]
        allergie=allergie,
        allergien=wert("allergien") if allergie == ALLERGIE_EINGETRAGEN else "",
        beschluss=beschluss,
        beschlussende=ende,
        bild=bild,
        id=id,
    )
    for f in pruefen(b):
        if f not in fehler and not any(f.startswith(feld) for feld in unlesbar):
            fehler.append(f)
    if fehler:
        raise Eingabefehler(fehler)
    return b


def als_eingabe(b: Bewohner) -> dict[str, str]:
    """Gegenstück zu ``aus_eingabe``: Texte zum Vorbefüllen des Formulars."""
    return {
        "vorname": b.vorname, "nachname": b.nachname, "zimmer": b.zimmer,
        "geburtsdatum": datum_text(b.geburtsdatum), "aufnahmedatum": datum_text(b.aufnahmedatum),
        "allergie": b.allergie, "allergien": b.allergien,
        "beschluss": b.beschluss, "beschlussende": datum_text(b.beschlussende),
    }


# --------------------------------------------------------------------------- Anzeige

def allergie_anzeige(b: Bewohner) -> str:
    if b.allergie == ALLERGIE_EINGETRAGEN:
        return f"Allergien: {b.allergien}"
    if b.allergie == ALLERGIE_KEINE:
        return "Allergien: keine bekannt"
    return "Allergien: nicht erfasst – bitte klären"


def beschluss_anzeige(b: Bewohner) -> str:
    if b.beschluss == BESCHLUSS_ENDE:
        return datum_text(b.beschlussende)
    if b.beschluss == BESCHLUSS_KEINER:
        return "kein Beschluss"
    return "nicht erfasst"


def initialen(b: Bewohner) -> str:
    return "".join(teil.strip()[:1].upper() for teil in (b.vorname, b.nachname) if teil.strip()) or "?"


def sortierschluessel(b: Bewohner) -> tuple[str, str]:
    return b.nachname.casefold(), b.vorname.casefold()
