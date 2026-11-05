"""Bewohnerbild oder Platzhalter mit Initialen (Regel 2: in den Demo-Daten nur Platzhalter)."""

from __future__ import annotations

from pathlib import Path

from PySide6.QtCore import QRect, Qt
from PySide6.QtGui import QColor, QFont, QPainter, QPixmap

from dokusystem.kern import bilder
from dokusystem.kern.bewohner import Bewohner, initialen

FARBEN = ("#5c6bc0", "#00897b", "#8d6e63", "#7e57c2", "#1e88e5", "#ef6c00", "#546e7a", "#ad1457")


def platzhalter(text: str, groesse: int, farbschluessel: str = "") -> QPixmap:
    bild = QPixmap(groesse, groesse)
    bild.fill(Qt.GlobalColor.transparent)
    maler = QPainter(bild)
    maler.setRenderHint(QPainter.RenderHint.Antialiasing)
    maler.setBrush(QColor(FARBEN[sum(map(ord, farbschluessel or text)) % len(FARBEN)]))
    maler.setPen(Qt.PenStyle.NoPen)
    maler.drawEllipse(0, 0, groesse, groesse)
    schrift = QFont()
    schrift.setPixelSize(int(groesse * 0.38))
    schrift.setBold(True)
    maler.setFont(schrift)
    maler.setPen(QColor("white"))
    maler.drawText(QRect(0, 0, groesse, groesse), Qt.AlignmentFlag.AlignCenter, text)
    maler.end()
    return bild


def aus_datei(pfad: Path | None, groesse: int) -> QPixmap | None:
    """Bilddatei quadratisch zugeschnitten – oder None, wenn sie fehlt oder kein Bild ist."""
    if pfad is None:
        return None
    bild = QPixmap(str(pfad))
    if bild.isNull():
        return None
    bild = bild.scaled(groesse, groesse, Qt.AspectRatioMode.KeepAspectRatioByExpanding,
                       Qt.TransformationMode.SmoothTransformation)
    return bild.copy((bild.width() - groesse) // 2, (bild.height() - groesse) // 2, groesse, groesse)


def bewohner_bild(b: Bewohner, groesse: int) -> QPixmap:
    return aus_datei(bilder.pfad(b.bild), groesse) or platzhalter(initialen(b), groesse, b.nachname)
