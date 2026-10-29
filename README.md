# Tabela do Outlooka

Cały kod znajduje się w Program.cs. Edytuj osoby, serwisy i początkowe pary w Main.
Wynik HTML pozostaje w zmiennej result. Na końcu Main jest kopiowany do schowka jako CF_HTML.
Program nie zapisuje wyniku na dysk.

Uruchom: dotnet run
Następnie w Outlooku wklej Ctrl+V do wiadomości w formacie HTML, zachowując formatowanie źródła.
Wymaga Windows. Projekt używa net9.0-windows i UseWindowsForms, a Main ma atrybut STAThread.

Domyślnie minimum 20 wierszy danych (brakujące puste). Parametr minRows: 0 daje tylko wiersze osób.
Początkowe pary są zachowane; pozostałe serwisy są rozdzielane losowo i równo.
