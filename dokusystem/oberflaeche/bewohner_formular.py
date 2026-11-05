"""Formular: Bewohner anlegen oder bearbeiten. Prüfen und Speichern macht der Kern."""

from __future__ import annotations

from pathlib import Path

from PySide6.QtGui import QImage
from PySide6.QtWidgets import (
    QButtonGroup,
    QDialog,
    QDialogButtonBox,
    QFileDialog,
    QFormLayout,
    QGridLayout,
    QHBoxLayout,
    QLabel,
    QLineEdit,
    QPushButton,
    QRadioButton,
    QVBoxLayout,
    QWidget,
)

from dokusystem.kern import bewohner as bw
from dokusystem.kern import bilder
from dokusystem.kern.speicher import Speicher

from . import bildchen
from .stil import DEMO_HINWEIS

BILDGROESSE = 96


class Dreiwahl(QWidget):
    """Drei Zustände (Regel 5): eingetragen (mit Eingabefeld) / zweite Möglichkeit / nicht erfasst."""

    def __init__(self, zustaende: tuple[str, str, str], beschriftungen: tuple[str, str, str],
                 platzhalter: str) -> None:
        super().__init__()
        self.zustaende = zustaende
        self.gruppe = QButtonGroup(self)
        self.knoepfe = [QRadioButton(text) for text in beschriftungen]
        self.feld = QLineEdit()
        self.feld.setPlaceholderText(platzhalter)
        layout = QGridLayout(self)
        layout.setContentsMargins(0, 0, 0, 0)
        for i, knopf in enumerate(self.knoepfe):
            self.gruppe.addButton(knopf, i)
            layout.addWidget(knopf, i, 0)
        layout.addWidget(self.feld, 0, 1)
        self.gruppe.idToggled.connect(lambda *_: self.feld.setEnabled(self.knoepfe[0].isChecked()))
        self.setze(zustaende[2], "")

    def setze(self, zustand: str, text: str) -> None:
        self.knoepfe[self.zustaende.index(zustand)].setChecked(True)
        self.feld.setText(text)
        self.feld.setEnabled(zustand == self.zustaende[0])

    def zustand(self) -> str:
        return self.zustaende[self.gruppe.checkedId()]


class BewohnerFormular(QDialog):
    def __init__(self, speicher: Speicher, bewohner: bw.Bewohner | None = None, parent=None) -> None:
        super().__init__(parent)
        self.speicher = speicher
        self.bewohner = bewohner
        self.gespeichert: bw.Bewohner | None = None
        self.neues_bild: Path | None = None
        self.bild_entfernen = False
        titel = f"Akte bearbeiten – {bewohner.name}" if bewohner else "Neuer Bewohner"
        self.setWindowTitle(f"{titel} ({DEMO_HINWEIS})")
        self.setMinimumWidth(560)

        hinweis = QLabel(DEMO_HINWEIS)
        hinweis.setObjectName("demoHinweis")

        self.vorname = QLineEdit()
        self.nachname = QLineEdit()
        self.zimmer = QLineEdit()
        self.geburtsdatum = QLineEdit()
        self.aufnahmedatum = QLineEdit()
        for feld in (self.geburtsdatum, self.aufnahmedatum):
            feld.setPlaceholderText("TT.MM.JJJJ")
        self.beschluss = Dreiwahl(bw.BESCHLUSS_ZUSTAENDE, ("endet am", "kein Beschluss", "nicht erfasst"),
                                  "TT.MM.JJJJ")
        self.allergie = Dreiwahl(bw.ALLERGIE_ZUSTAENDE, ("eingetragen:", "keine bekannt", "nicht erfasst"),
                                 "z. B. Haselnüsse")

        self.bild = QLabel()
        self.bild.setFixedSize(BILDGROESSE, BILDGROESSE)
        bild_waehlen = QPushButton("Bild wählen …")
        bild_waehlen.clicked.connect(self._bild_waehlen)
        self.bild_weg = QPushButton("Bild entfernen")
        self.bild_weg.clicked.connect(self._bild_entfernen)
        bild_knoepfe = QVBoxLayout()
        bild_knoepfe.addWidget(bild_waehlen)
        bild_knoepfe.addWidget(self.bild_weg)
        bild_hinweis = QLabel("Demo: bitte keine Fotos echter Personen.")
        bild_hinweis.setObjectName("klein")
        bild_knoepfe.addWidget(bild_hinweis)
        bild_knoepfe.addStretch()
        bild_zeile = QHBoxLayout()
        bild_zeile.addWidget(self.bild)
        bild_zeile.addLayout(bild_knoepfe, 1)

        formular = QFormLayout()
        formular.addRow("Vorname", self.vorname)
        formular.addRow("Nachname", self.nachname)
        formular.addRow("Zimmer", self.zimmer)
        formular.addRow("Geburtsdatum", self.geburtsdatum)
        formular.addRow("Aufnahmedatum", self.aufnahmedatum)
        formular.addRow("Beschluss", self.beschluss)
        formular.addRow("Allergien", self.allergie)
        formular.addRow("Bild", bild_zeile)

        self.fehler = QLabel()
        self.fehler.setObjectName("fehler")
        self.fehler.setWordWrap(True)
        self.fehler.hide()

        knoepfe = QDialogButtonBox(QDialogButtonBox.StandardButton.Save | QDialogButtonBox.StandardButton.Cancel)
        knoepfe.button(QDialogButtonBox.StandardButton.Save).setText("Speichern")
        knoepfe.button(QDialogButtonBox.StandardButton.Cancel).setText("Abbrechen")
        knoepfe.accepted.connect(self.speichern)
        knoepfe.rejected.connect(self.reject)

        layout = QVBoxLayout(self)
        layout.addWidget(hinweis)
        layout.addLayout(formular)
        layout.addWidget(self.fehler)
        layout.addWidget(knoepfe)

        if bewohner:
            werte = bw.als_eingabe(bewohner)
            for name in ("vorname", "nachname", "zimmer", "geburtsdatum", "aufnahmedatum"):
                getattr(self, name).setText(werte[name])
            self.beschluss.setze(bewohner.beschluss, werte["beschlussende"])
            self.allergie.setze(bewohner.allergie, bewohner.allergien)
        self._bild_zeigen()

    # ------------------------------------------------------------------ Bild

    def _bild_zeigen(self) -> None:
        if self.neues_bild:
            bild = bildchen.aus_datei(self.neues_bild, BILDGROESSE)
        elif self.bewohner and not self.bild_entfernen:
            bild = bildchen.aus_datei(bilder.pfad(self.bewohner.bild), BILDGROESSE)
        else:
            bild = None
        self.bild_weg.setEnabled(bild is not None)
        if bild is None:
            name = f"{self.vorname.text().strip()[:1]}{self.nachname.text().strip()[:1]}".upper() or "?"
            bild = bildchen.platzhalter(name, BILDGROESSE, self.nachname.text())
        self.bild.setPixmap(bild)

    def bild_setzen(self, pfad: Path) -> bool:
        if QImage(str(pfad)).isNull() or pfad.suffix.lower() not in bilder.ENDUNGEN:
            self._fehler_zeigen([f"„{pfad.name}“ ist kein lesbares Bild ({', '.join(bilder.ENDUNGEN)})."])
            return False
        self.neues_bild = pfad
        self.bild_entfernen = False
        self.fehler.hide()
        self._bild_zeigen()
        return True

    def _bild_waehlen(self) -> None:
        muster = " ".join(f"*{e}" for e in bilder.ENDUNGEN)
        datei, _ = QFileDialog.getOpenFileName(self, "Bild wählen", "", f"Bilder ({muster})")
        if datei:
            self.bild_setzen(Path(datei))

    def _bild_entfernen(self) -> None:
        self.neues_bild = None
        self.bild_entfernen = True
        self._bild_zeigen()

    # ------------------------------------------------------------------ Speichern

    def eingabe(self) -> dict[str, str]:
        return {
            "vorname": self.vorname.text(), "nachname": self.nachname.text(), "zimmer": self.zimmer.text(),
            "geburtsdatum": self.geburtsdatum.text(), "aufnahmedatum": self.aufnahmedatum.text(),
            "beschluss": self.beschluss.zustand(), "beschlussende": self.beschluss.feld.text(),
            "allergie": self.allergie.zustand(), "allergien": self.allergie.feld.text(),
        }

    def _fehler_zeigen(self, fehler: list[str]) -> None:
        self.fehler.setText("Bitte prüfen:\n• " + "\n• ".join(fehler))
        self.fehler.show()

    def speichern(self) -> bool:
        try:
            b = bw.aus_eingabe(self.eingabe(), id=self.bewohner.id if self.bewohner else None)
            self.gespeichert = self.speicher.bewohner_speichern(
                b, neues_bild=self.neues_bild, bild_entfernen=self.bild_entfernen)
        except bw.Eingabefehler as e:
            self._fehler_zeigen(e.fehler)
            return False
        except OSError as e:
            self._fehler_zeigen([f"Das Bild ließ sich nicht übernehmen: {e}"])
            return False
        self.accept()
        return True
