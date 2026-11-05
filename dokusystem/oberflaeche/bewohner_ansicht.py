"""Bereich „Bewohner“: Übersicht als Kacheln, Klick öffnet die Akte."""

from __future__ import annotations

from PySide6.QtCore import QSize, Qt, Signal
from PySide6.QtGui import QIcon
from PySide6.QtWidgets import (
    QFormLayout,
    QHBoxLayout,
    QLabel,
    QListView,
    QListWidget,
    QListWidgetItem,
    QPushButton,
    QStackedWidget,
    QVBoxLayout,
    QWidget,
)

from dokusystem.kern import bewohner as bw
from dokusystem.kern.speicher import Speicher

from . import bildchen
from .bewohner_formular import BewohnerFormular

KACHELBILD = 96
AKTENBILD = 128

# Allergiehinweis oben in der Akte: drei klar verschiedene Darstellungen (Regel 5)
ALLERGIE_STIL = {
    bw.ALLERGIE_EINGETRAGEN: "background: #c62828; color: white; font-weight: bold;",
    bw.ALLERGIE_KEINE: "background: #e8f5e9; color: #1b5e20; border: 1px solid #a5d6a7;",
    bw.NICHT_ERFASST: "background: #fff3e0; color: #8a4b00; border: 2px dashed #ef6c00; font-weight: bold;",
}
NICHT_ERFASST_STIL = "color: #8a4b00; font-style: italic;"


class Uebersicht(QWidget):
    akte_oeffnen = Signal(int)
    neu = Signal()

    def __init__(self) -> None:
        super().__init__()
        titel = QLabel("Bewohner")
        titel.setObjectName("seitenTitel")
        self.anzahl = QLabel()
        neu = QPushButton("Neuer Bewohner …")
        neu.clicked.connect(self.neu)
        kopf = QHBoxLayout()
        kopf.addWidget(titel)
        kopf.addWidget(self.anzahl)
        kopf.addStretch()
        kopf.addWidget(neu)

        self.liste = QListWidget()
        self.liste.setViewMode(QListView.ViewMode.IconMode)
        self.liste.setIconSize(QSize(KACHELBILD, KACHELBILD))
        self.liste.setGridSize(QSize(190, 170))
        self.liste.setResizeMode(QListView.ResizeMode.Adjust)
        self.liste.setMovement(QListView.Movement.Static)
        self.liste.setWordWrap(True)
        self.liste.setObjectName("kacheln")
        self.liste.itemClicked.connect(lambda item: self.akte_oeffnen.emit(item.data(Qt.ItemDataRole.UserRole)))

        layout = QVBoxLayout(self)
        layout.setContentsMargins(0, 0, 0, 0)
        layout.addLayout(kopf)
        layout.addWidget(self.liste, 1)

    def zeigen(self, alle: list[bw.Bewohner]) -> None:
        self.liste.clear()
        for b in alle:
            item = QListWidgetItem(QIcon(bildchen.bewohner_bild(b, KACHELBILD)),
                                   f"{b.nachname}, {b.vorname}\nZimmer {b.zimmer}")
            item.setData(Qt.ItemDataRole.UserRole, b.id)
            item.setToolTip(f"Akte von {b.name} öffnen")
            self.liste.addItem(item)
        self.anzahl.setText(f"({len(alle)})")


class Akte(QWidget):
    zurueck = Signal()
    bearbeiten = Signal(int)

    def __init__(self) -> None:
        super().__init__()
        self.bewohner_id: int | None = None
        zurueck = QPushButton("← Alle Bewohner")
        zurueck.clicked.connect(self.zurueck)
        bearbeiten = QPushButton("Bearbeiten …")
        bearbeiten.clicked.connect(lambda: self.bearbeiten.emit(self.bewohner_id))
        leiste = QHBoxLayout()
        leiste.addWidget(zurueck)
        leiste.addStretch()
        leiste.addWidget(bearbeiten)

        self.bild = QLabel()
        self.bild.setFixedSize(AKTENBILD, AKTENBILD)
        self.name = QLabel()
        self.name.setObjectName("seitenTitel")
        self.zimmer = QLabel()
        self.zimmer.setObjectName("unterTitel")
        namensblock = QWidget()
        namen_layout = QVBoxLayout(namensblock)
        namen_layout.setContentsMargins(0, 0, 0, 0)
        namen_layout.addWidget(self.name)
        namen_layout.addWidget(self.zimmer)
        kopf = QHBoxLayout()
        kopf.addWidget(self.bild)
        kopf.addSpacing(16)
        kopf.addWidget(namensblock, 1, Qt.AlignmentFlag.AlignVCenter)

        self.allergie = QLabel()
        self.allergie.setWordWrap(True)
        self.allergie.setContentsMargins(12, 10, 12, 10)

        self.geburtsdatum = QLabel()
        self.aufnahmedatum = QLabel()
        self.beschlussende = QLabel()
        angaben = QFormLayout()
        angaben.addRow("Geburtsdatum:", self.geburtsdatum)
        angaben.addRow("Aufnahmedatum:", self.aufnahmedatum)
        angaben.addRow("Beschlussende:", self.beschlussende)

        layout = QVBoxLayout(self)
        layout.setContentsMargins(0, 0, 0, 0)
        layout.addLayout(leiste)
        layout.addSpacing(8)
        layout.addLayout(kopf)
        layout.addSpacing(8)
        layout.addWidget(self.allergie)
        layout.addSpacing(8)
        layout.addLayout(angaben)
        layout.addStretch(1)

    def zeigen(self, b: bw.Bewohner) -> None:
        self.bewohner_id = b.id
        self.bild.setPixmap(bildchen.bewohner_bild(b, AKTENBILD))
        self.name.setText(b.name)
        self.zimmer.setText(f"Zimmer {b.zimmer}")
        self.allergie.setText(bw.allergie_anzeige(b))
        self.allergie.setStyleSheet(ALLERGIE_STIL[b.allergie])
        self.geburtsdatum.setText(bw.datum_text(b.geburtsdatum))
        self.aufnahmedatum.setText(bw.datum_text(b.aufnahmedatum))
        self.beschlussende.setText(bw.beschluss_anzeige(b))
        self.beschlussende.setStyleSheet(NICHT_ERFASST_STIL if b.beschluss == bw.NICHT_ERFASST else "")


class BewohnerBereich(QStackedWidget):
    def __init__(self, speicher: Speicher) -> None:
        super().__init__()
        self.speicher = speicher
        self.uebersicht = Uebersicht()
        self.akte = Akte()
        self.addWidget(self.uebersicht)
        self.addWidget(self.akte)
        self.uebersicht.akte_oeffnen.connect(self.akte_zeigen)
        self.uebersicht.neu.connect(lambda: self._formular(None))
        self.akte.zurueck.connect(self.uebersicht_zeigen)
        self.akte.bearbeiten.connect(lambda id: self._formular(self.speicher.bewohner(id)))
        self.uebersicht_zeigen()

    def uebersicht_zeigen(self) -> None:
        self.uebersicht.zeigen(self.speicher.alle_bewohner())
        self.setCurrentWidget(self.uebersicht)

    def akte_zeigen(self, id: int) -> None:
        b = self.speicher.bewohner(id)
        if b is None:
            self.uebersicht_zeigen()
            return
        self.akte.zeigen(b)
        self.setCurrentWidget(self.akte)

    def formular(self, b: bw.Bewohner | None) -> BewohnerFormular:
        return BewohnerFormular(self.speicher, b, self)

    def _formular(self, b: bw.Bewohner | None) -> None:
        formular = self.formular(b)
        gespeichert = formular.gespeichert if formular.exec() else None
        formular.deleteLater()  # sonst sammeln sich geschlossene Formulare an
        if gespeichert:
            self.akte_zeigen(gespeichert.id)
