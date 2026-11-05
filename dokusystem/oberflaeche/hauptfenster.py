"""Hauptfenster: Demo-Hinweis oben, Navigation links, Bereiche rechts, Version unten links."""

from __future__ import annotations

from PySide6.QtCore import Qt
from PySide6.QtWidgets import (
    QCheckBox,
    QHBoxLayout,
    QLabel,
    QListWidget,
    QMainWindow,
    QMessageBox,
    QPushButton,
    QStackedWidget,
    QVBoxLayout,
    QWidget,
)

from dokusystem._build import BUILD
from dokusystem._version import VERSION
from dokusystem.kern import einstellungen
from dokusystem.kern.speicher import Speicher

from .bewohner_ansicht import BewohnerBereich
from .stil import DEMO_HINWEIS


def versions_text() -> str:
    build = f"Build {BUILD}" if BUILD else "Entwicklerversion"
    return f"Version {VERSION} · {build}"


class Hauptfenster(QMainWindow):
    def __init__(self, speicher: Speicher) -> None:
        super().__init__()
        self.speicher = speicher
        self.setWindowTitle(f"Dokumentationssystem – {DEMO_HINWEIS}")
        self.resize(1100, 720)

        self.demo_hinweis = QLabel(f"{DEMO_HINWEIS} · nicht für echte Bewohnerdaten")
        self.demo_hinweis.setObjectName("demoHinweis")
        self.demo_hinweis.setAlignment(Qt.AlignmentFlag.AlignCenter)

        self.bereiche = QStackedWidget()
        self.navigation = QListWidget()
        self.navigation.currentRowChanged.connect(self.bereiche.setCurrentIndex)
        self.bewohner = BewohnerBereich(speicher)
        self._bereich_hinzufuegen("Bewohner", self.bewohner)
        self.navigation.itemClicked.connect(self._bereich_neu_zeigen)

        self.version = QLabel(versions_text())
        self.version.setObjectName("version")
        self.auto_update = QCheckBox("Beim Start nach Updates suchen")
        self.auto_update.setObjectName("autoUpdate")
        self.auto_update.setChecked(einstellungen.lade()["auto_update"])
        self.auto_update.toggled.connect(self._auto_update_umschalten)
        zuruecksetzen = QPushButton("Demo-Daten zurücksetzen …")
        zuruecksetzen.clicked.connect(lambda: self.demo_zuruecksetzen())

        leiste = QWidget()
        leiste.setObjectName("navigation")
        leiste.setFixedWidth(240)
        leisten_layout = QVBoxLayout(leiste)
        leisten_layout.addWidget(self.navigation, 1)
        leisten_layout.addWidget(zuruecksetzen)
        leisten_layout.addWidget(self.auto_update)
        leisten_layout.addWidget(self.version)

        mitte = QHBoxLayout()
        mitte.setContentsMargins(0, 0, 0, 0)
        mitte.setSpacing(0)
        mitte.addWidget(leiste)
        rechts = QWidget()
        rechts_layout = QVBoxLayout(rechts)
        rechts_layout.setContentsMargins(16, 12, 16, 12)
        rechts_layout.addWidget(self.bereiche)
        mitte.addWidget(rechts, 1)

        ganz = QWidget()
        ganz_layout = QVBoxLayout(ganz)
        ganz_layout.setContentsMargins(0, 0, 0, 0)
        ganz_layout.setSpacing(0)
        ganz_layout.addWidget(self.demo_hinweis)
        ganz_layout.addLayout(mitte, 1)
        self.setCentralWidget(ganz)
        self.navigation.setCurrentRow(0)

    def _bereich_hinzufuegen(self, name: str, seite: QWidget) -> None:
        self.navigation.addItem(name)
        self.bereiche.addWidget(seite)

    def _auto_update_umschalten(self, an: bool) -> None:
        werte = einstellungen.lade()
        werte["auto_update"] = an
        einstellungen.speichere(werte)

    def _bereich_neu_zeigen(self) -> None:
        """Klick auf „Bewohner“ in der Navigation führt immer zur Übersicht zurück."""
        if self.bereiche.currentWidget() is self.bewohner:
            self.bewohner.uebersicht_zeigen()

    def demo_zuruecksetzen(self, nachfragen: bool = True) -> bool:
        if nachfragen:
            antwort = QMessageBox.question(
                self, "Demo-Daten zurücksetzen",
                "Alle Änderungen und hinterlegten Bilder werden gelöscht und die ausgedachten "
                "Demo-Bewohner neu geladen.\n\nFortfahren?",
                QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No,
                QMessageBox.StandardButton.No)
            if antwort != QMessageBox.StandardButton.Yes:
                return False
        self.speicher.demo_zuruecksetzen()
        self.navigation.setCurrentRow(0)
        self.bewohner.uebersicht_zeigen()
        return True
