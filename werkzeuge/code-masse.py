"""Maße des Codes nach docs/code-regeln.md: Dateien über 400 Zeilen, Methoden über 40 Zeilen, Zeilen über
120 Zeichen, Kommentare mit Verlauf. Nur ein Hinweis im Build, kein Fehler. Aufruf: python werkzeuge/code-masse.py
"""
import pathlib
import re
import sys

WURZEL = pathlib.Path(__file__).resolve().parent.parent
DATEI, METHODE, ZEILE = 400, 40, 120
VERLAUF = re.compile(r"//.*(Schritt \d|Bauplan|\b0\.\d{1,2}\.\d|\(0\.\d{2}|seit 0\.|vorher )")
# Kopf einer Methode oder eines Konstruktors (grob, für eine Übersicht genügt das).
KOPF = re.compile(r"^\s*(public|private|internal|protected)[^=;]*\)\s*(\{|$)")


def dateien():
    for ordner in ("src",):   # Programmcode; Tests zählen nicht mit (wie die Ausgangswerte in docs/code-regeln.md)
        for pfad in sorted((WURZEL / ordner).rglob("*.cs")):
            if "obj" not in pfad.parts and "bin" not in pfad.parts:
                yield pfad


def methoden(zeilen):
    """(Name, Länge) jeder Methode: vom Kopf bis zur schließenden Klammer auf gleicher Tiefe."""
    i = 0
    while i < len(zeilen):
        if KOPF.match(zeilen[i]) and "=>" not in zeilen[i]:
            tiefe, start, j = 0, i, i
            gesehen = False
            while j < len(zeilen):
                tiefe += zeilen[j].count("{") - zeilen[j].count("}")
                gesehen = gesehen or "{" in zeilen[j]
                if gesehen and tiefe <= 0:
                    break
                j += 1
            name = zeilen[start].split("(")[0].split()[-1]
            yield name, j - start + 1
            i = j + 1
        else:
            i += 1


def main():
    gesamt = kommentare = verlauf = 0
    grosse, lange_methoden, lange_zeilen = [], [], 0
    for pfad in dateien():
        text = pfad.read_text(encoding="utf-8-sig")
        zeilen = text.split("\n")
        rel = pfad.relative_to(WURZEL).as_posix()
        gesamt += len(zeilen)
        kommentare += sum(1 for z in zeilen if z.strip().startswith("//"))
        verlauf += sum(1 for z in zeilen if VERLAUF.search(z))
        lange_zeilen += sum(1 for z in zeilen if len(z) > ZEILE)
        if len(zeilen) > DATEI:
            grosse.append((len(zeilen), rel))
        for name, laenge in methoden(zeilen):
            if laenge > METHODE:
                lange_methoden.append((laenge, f"{rel}: {name}"))
    print(f"Zeilen gesamt: {gesamt}")
    print(f"Kommentarzeilen: {kommentare} (davon mit Verlauf: {verlauf})")
    print(f"Dateien über {DATEI} Zeilen: {len(grosse)}")
    for laenge, rel in sorted(grosse, reverse=True):
        print(f"  {laenge:5}  {rel}")
    print(f"Methoden über {METHODE} Zeilen: {len(lange_methoden)}")
    for laenge, name in sorted(lange_methoden, reverse=True)[:25]:
        print(f"  {laenge:5}  {name}")
    print(f"Zeilen über {ZEILE} Zeichen: {lange_zeilen}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
