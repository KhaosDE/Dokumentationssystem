"""Baut die Test-PDF für den HEB-Bogen (tests/daten/heb-test.pdf).

Ausgedacht, kein Bogen eines Kostenträgers: eine Seite mit Formularfeldern, deren Namen den
Programmfeldern entsprechen, dazu ein Feld mit abweichendem Namen ("Feld_Leitziel") für die
Zuordnung. Aufruf: python werkzeuge/heb-test-pdf.py tests/daten/heb-test.pdf
Benötigt reportlab (BSD-Lizenz), nur zum Bauen der Testdatei, nicht im Programm.
"""
import sys
from reportlab.lib.pagesizes import A4
from reportlab.pdfgen import canvas

ziel = sys.argv[1] if len(sys.argv) > 1 else "heb-test.pdf"
c = canvas.Canvas(ziel, pagesize=A4)
c.setTitle("Test-PDF HEB (ausgedacht)")
c.setFont("Helvetica-Bold", 14)
c.drawString(40, 800, "Test-PDF fuer den HEB-Bogen - ausgedacht, kein echter Bogen")
c.setFont("Helvetica", 9)
felder = ["name", "aufnahme", "beschlussende", "von", "bis", "nummer", "planung",
          "1a", "1b", "1d", "1e", "2b", "3b", "hilfe1", "nahziel1"]
y = 770
for name in felder:
    c.drawString(40, y + 4, name)
    c.acroForm.textfield(name=name, x=150, y=y, width=400, height=16, borderWidth=1, fontSize=9)
    y -= 26
c.drawString(40, y + 4, "Feld_Leitziel")
c.acroForm.textfield(name="Feld_Leitziel", x=150, y=y, width=400, height=16, borderWidth=1, fontSize=9)
c.showPage()
c.save()
print("gebaut:", ziel)
