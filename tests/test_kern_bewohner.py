"""Bewohner: Datumsformat, Prüfregeln, drei Zustände bei Allergien und Beschluss."""

import datetime as dt
import unittest

from dokusystem.kern import bewohner as bw


def eingabe(**aenderungen):
    werte = {
        "vorname": "Anton", "nachname": "Ahorn", "zimmer": "101",
        "geburtsdatum": "14.03.1962", "aufnahmedatum": "01.09.2019",
    }
    werte.update(aenderungen)
    return werte


def fehler_von(werte):
    try:
        bw.aus_eingabe(werte)
    except bw.Eingabefehler as e:
        return e.fehler
    raise AssertionError("Eingabefehler erwartet")


class DatumTest(unittest.TestCase):
    def test_format_tt_mm_jjjj(self):
        self.assertEqual(bw.datum_lesen("03.04.1965", "Geburtsdatum"), dt.date(1965, 4, 3))
        self.assertEqual(bw.datum_lesen(" 3.4.1965 ", "Geburtsdatum"), dt.date(1965, 4, 3))
        self.assertEqual(bw.datum_text(dt.date(1965, 4, 3)), "03.04.1965")
        self.assertEqual(bw.datum_text(None), "")

    def test_falsche_daten(self):
        for text in ("1965-04-03", "03.04.65", "03/04/1965", "irgendwann"):
            with self.assertRaisesRegex(ValueError, "TT.MM.JJJJ"):
                bw.datum_lesen(text, "Geburtsdatum")
        with self.assertRaisesRegex(ValueError, "gibt es nicht"):
            bw.datum_lesen("31.02.1965", "Geburtsdatum")
        with self.assertRaisesRegex(ValueError, "fehlt"):
            bw.datum_lesen("", "Geburtsdatum")


class PruefregelnTest(unittest.TestCase):
    def test_gueltige_eingabe(self):
        b = bw.aus_eingabe(eingabe(), id=7, bild="a.png")
        self.assertEqual((b.name, b.zimmer, b.id, b.bild), ("Anton Ahorn", "101", 7, "a.png"))
        self.assertEqual(b.geburtsdatum, dt.date(1962, 3, 14))

    def test_pflichtfelder(self):
        fehler = fehler_von(eingabe(vorname=" ", nachname="", zimmer="", geburtsdatum="", aufnahmedatum=""))
        self.assertEqual(fehler, ["Vorname fehlt.", "Nachname fehlt.", "Zimmernummer fehlt.",
                                  "Geburtsdatum fehlt.", "Aufnahmedatum fehlt."])

    def test_aufnahme_nicht_vor_geburt(self):
        self.assertEqual(fehler_von(eingabe(geburtsdatum="14.03.1962", aufnahmedatum="13.03.1962")),
                         ["Das Aufnahmedatum liegt vor dem Geburtsdatum."])
        bw.aus_eingabe(eingabe(geburtsdatum="14.03.1962", aufnahmedatum="14.03.1962"))  # gleicher Tag geht

    def test_beschluss(self):
        b = bw.aus_eingabe(eingabe(beschluss="ende", beschlussende="30.06.2027"))
        self.assertEqual(bw.beschluss_anzeige(b), "30.06.2027")
        self.assertEqual(fehler_von(eingabe(beschluss="ende", beschlussende="")), ["Beschlussende fehlt."])
        self.assertEqual(len(fehler_von(eingabe(beschluss="ende", beschlussende="30.6.27"))), 1)
        b = bw.aus_eingabe(eingabe(beschluss="kein_beschluss", beschlussende="30.06.2027"))
        self.assertIsNone(b.beschlussende)  # Datum ohne Beschluss wird verworfen
        self.assertEqual(bw.beschluss_anzeige(b), "kein Beschluss")
        self.assertEqual(fehler_von(eingabe(beschluss="vielleicht")), ["Beschluss: unbekannte Angabe."])


class DreiZustaendeTest(unittest.TestCase):
    """Regel 5: Leere Felder sind nicht „nichts“."""

    def test_ohne_angabe_gilt_nicht_erfasst(self):
        b = bw.aus_eingabe(eingabe())
        self.assertEqual(b.allergie, bw.NICHT_ERFASST)
        self.assertEqual(b.beschluss, bw.NICHT_ERFASST)
        self.assertEqual(bw.allergie_anzeige(b), "Allergien: nicht erfasst – bitte klären")
        self.assertEqual(bw.beschluss_anzeige(b), "nicht erfasst")
        neu = bw.Bewohner("A", "B", "1", dt.date(1960, 1, 1), dt.date(2020, 1, 1))
        self.assertEqual((neu.allergie, neu.beschluss), (bw.NICHT_ERFASST, bw.NICHT_ERFASST))

    def test_drei_verschiedene_anzeigen(self):
        anzeigen = {
            bw.allergie_anzeige(bw.aus_eingabe(eingabe(allergie="eingetragen", allergien="Haselnüsse"))),
            bw.allergie_anzeige(bw.aus_eingabe(eingabe(allergie="keine_bekannt"))),
            bw.allergie_anzeige(bw.aus_eingabe(eingabe(allergie="nicht_erfasst"))),
        }
        self.assertEqual(anzeigen, {"Allergien: Haselnüsse", "Allergien: keine bekannt",
                                    "Allergien: nicht erfasst – bitte klären"})

    def test_eingetragen_ohne_text_geht_nicht(self):
        fehler = fehler_von(eingabe(allergie="eingetragen", allergien="  "))
        self.assertEqual(len(fehler), 1)
        self.assertIn("bitte eintragen", fehler[0])

    def test_text_bei_keine_bekannt_wird_verworfen(self):
        b = bw.aus_eingabe(eingabe(allergie="keine_bekannt", allergien="Haselnüsse"))
        self.assertEqual(b.allergien, "")
        b.allergien = "Haselnüsse"  # direkt gesetzt (z. B. Programmfehler): Prüfung schlägt an
        self.assertEqual(bw.pruefen(b), ["Allergien: Text nur bei eingetragenen Allergien."])


class HinUndZurueckTest(unittest.TestCase):
    def test_als_eingabe_ergibt_denselben_bewohner(self):
        b = bw.aus_eingabe(eingabe(allergie="eingetragen", allergien="Erdbeeren",
                                   beschluss="ende", beschlussende="1.2.2027"))
        self.assertEqual(bw.aus_eingabe(bw.als_eingabe(b)), b)
        self.assertEqual(bw.als_eingabe(b)["beschlussende"], "01.02.2027")


class AnzeigeTest(unittest.TestCase):
    def test_initialen_und_sortierung(self):
        a = bw.aus_eingabe(eingabe(vorname="emil", nachname="Eiche"))
        b = bw.aus_eingabe(eingabe(vorname="Anton", nachname="ahorn"))
        self.assertEqual(bw.initialen(a), "EE")
        self.assertEqual(sorted([a, b], key=bw.sortierschluessel), [b, a])


if __name__ == "__main__":
    unittest.main()
