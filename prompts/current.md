Diese app ist aktuell ein mehr oder weniger leeres Gerüst für eine Fragebogen-Anwendung. Jetzt wollen wir ein erstes Feature hinzufügen:

* Verwaltung von Fragebögen:
  * Je Fragebogen:
    * Titel (gibt es schon)
    * Beschreibung (gibt es schon)
    * Komma-separierte Liste von Tags (neu)
    * List von Fragen (neu). Je Frage:
      * Text (max. 500 Zeichen)
      * Antworttyp (Text, Ja/Nein, Likert-Skala 1-5)
      * Kennzeichen, ob Frage optional oder verpflichtend ist
  * UI:
    * Fragebogen erstellen
      * Alles wird auf einmal eingegeben, inkl. Fragen
      * Am Ende wird alles in einer Transaktion gespeichert
    * Fragebogen bearbeiten
      * Titel, Beschreibung und Tags können bearbeitet werden
      * Fragen können hinzugefügt werden.
      * Fragen können bearbeitet werden.
      * Fragen können NICHT gelöscht werden.
      * Gesamter Fragebogen wird geändert und am Ende in einer Transaktion gespeichert
    * Fragebögen können NICHT gelöscht werden.
* Beantwortung:
  * Auswahl eines Fragebogens
  * Beantwortung der Fragen
  * Speichern der Antworten (alles wird in einer Transaktion gespeichert, kein Zwischenspeichern)
  * Die Prüfung der Eingaben (z.B. Pflichtfragen) erfolgt erst beim Speichern, nicht während der Eingabe
  * Ungültige Eingaben werden zurückgewiesen, Prüfung im Backend, nicht (nur) im Frontend
* Auswertung:
  * Auswahl eines Fragebogens
  * Anzeige aller Antworten zu diesem Fragebogen
    * Bei Textfragen: Alle Antworten werden angezeigt
    * Bei Ja/Nein-Fragen: Anzahl der Ja- und Nein-Antworten (mit %Wert)
    * Bei Likert-Skala-Fragen: Durchschnittswert und Verteilung der Antworten (z.B. 20% 1, 30% 2, 25% 3, 15% 4, 10% 5)

QS:

* Prüfung der Eingaben muss in der Data Layer als isolierte Funktion bereitstehen
* Es müssen Unit-Tests für die Prüfungsfunktion existieren (positive Tests, negative Tests)
* Die Prüflogik muss so gestaltet sein, dass sie komplett ohne DB testbar ist
