# FNIRSI DPS-150 Studio Pro

**Created by: PATRYK JANKOWSKI**

---

## English Overview

**FNIRSI DPS-150 Studio Pro** is an advanced standalone Windows desktop control center, real-time oscilloscope, smart battery charger (1S to 7S), and constant-power (CP) controller designed for the **FNIRSI DPS-150** CNC DC programmable laboratory power supply (0 to 30V, 0 to 5A, 150W).

Built as a lightweight, zero-dependency single-file executable (using native Windows .NET Framework 4.8), it communicates directly with the power supply over USB Virtual COM (115200 bps, VID_2E3C) and unlocks features not available on the standalone device screen.

### Key Features

- **High-Precision Digital Dashboard**:
  - Live telemetry for Output Voltage (Vout), Output Current (Iout), and Output Power (Pout).
  - Instant calculation of Load Resistance (Ohm), Input Voltage Headroom (Vin minus Vout) with automatic low-dropout warning, and Peak-to-Peak Ripple (mVpp and mApp).
  - Automatic activation of hardware and high-precision software Capacity (Ah and mAh) and Energy (Wh and mWh) counters (0xD8 register).
- **Real-Time Oscilloscope (V / I / P)**:
  - Smooth double-buffered waveform plotter with configurable time windows (10s, 30s, 1m, 5m), mouse-wheel zoom, history panning, crosshair inspection, pause, and one-click CSV Export.
- **Smart Battery Charger Mode (1S to 7S CC/CV + Auto-Cutoff)**:
  - Supports Li-Ion / Li-Po 3.7V (4.20V per cell), Li-Ion HV 3.8V (4.35V per cell), LiFePO4 3.2V (3.65V per cell), Lead-Acid Pb / AGM 2.0V (2.40V per cell), and NiMH / NiCd 1.2V (1.45V per cell).
  - Quick pack selectors (1S, 2S, 3S, 4S, 5S, 6S, 7S) with automatic target voltage calculation and per-cell voltage telemetry (V per cell).
  - Automatic CV Cutoff: Automatically turns OUTPUT OFF when charging current drops below the configurable cutoff threshold (for example 50 mA) in the CV absorption phase or when the maximum mAh capacity limit is reached.
- **Constant Power Mode (CP - Constant Power)**:
  - Closed-loop software power regulator that dynamically adjusts Vset based on load current Iout to maintain a constant target power (W) within safe Max U and Max I limits.
- **Full Hardware Protections and EEPROM Presets (M1 to M6)**:
  - Read and write OVP, OCP, OPP, OTP, and LVP thresholds, display brightness, buzzer volume, and hardware memory groups M1 to M6.
- **Automated Sequence and Voltage Ramp Generator**:
  - Programmable multi-step voltage and current sequences with soft-start and pulse templates.
- **Hardware Diagnostics and HEX / ATEasy Protocol Monitor**:
  - Live readout of Model (0xDE), HW Version (0xDF), FW Version (0xE0), USB-PD / DC input limits MaxV (0xE1) and MaxI (0xE2), and live TX/RX hexadecimal packet monitor.
  - Graceful USB disconnect (0xF1 0xC1 0x00 0x01 0x00 0x01) that unlocks physical buttons on the DPS-150 upon exit.
- **Instant Language Switch (PL / EN) and Built-in Simulator Mode**:
  - Switch the entire user interface between Polish and English at any time, or test all features without hardware using the built-in virtual load simulator.

---

## Opis po polsku (PL)

**FNIRSI DPS-150 Studio Pro** to zaawansowana aplikacja desktopowa dla systemu Windows do pelnego sterowania, diagnostyki i wizualizacji parametrow laboratoryjnego zasilacza regulowanego **FNIRSI DPS-150 (30V / 5A / 150W)**.

Program zostal napisany w jezyku C# jako lekki, samodzielny plik wykonywalny `FNIRSI_DPS150_Studio.exe` (nie wymaga instalacji zadnych zewnetrznych bibliotek ani srodowisk). Automatycznie wykrywa port USB zasilacza (`VID_2E3C`) i odblokowuje funkcje niedostepne z poziomu fizycznego panelu urzadzenia.

### Glowne mozliwosci programu

1. **Cyfrowy Pulpit Pomiarowy na Zywo**:
   - Precyzyjny odczyt napiecia wyjsciowego (Vout), pradu (Iout) oraz mocy (Pout).
   - Automatyczne wlaczanie sprzetowego i programowego zliczania Pojemnosci (Ah / mAh) oraz Energii (Wh / mWh) (rejestr 0xD8).
   - Wyliczanie na zywo Rezystancji obciazenia (Ohm), Czasu aktywnej pracy wyjscia (Czas ON) oraz temperatury wewnetrznej (C).
2. **Inteligentny Tryb Ladowania Akumulatorow (1S do 7S CC/CV z Auto-Stopem)**:
   - Obsluga najpopularniejszych chemii ogniw:
     - Li-Ion / Li-Po 3.7V (4.20 V na ogniwo)
     - Li-Ion HV 3.8V (4.35 V na ogniwo)
     - LiFePO4 3.2V (3.65 V na ogniwo)
     - Kwasowo-olowiowe Pb / AGM 2.0V (2.40 V na ogniwo)
     - NiMH / NiCd 1.2V (1.45 V na ogniwo)
   - Szybki wybor pakietow 1S, 2S, 3S, 4S, 5S, 6S, 7S z automatycznym przeliczaniem napiecia docelowego oraz podgladem napiecia pojedynczego ogniwa.
   - Automatyczne odciecie po naladowaniu (Auto-Cutoff): Gdy w fazie dopelniania napieciowego (CV) prad ladowania spadnie ponizej zadanego progu (np. 0.050 A = 50 mA) lub zostanie osiagniety limit pojemnosci mAh, aplikacja automatycznie wylacza wyjscie zasilacza (OUTPUT OFF), chroniac akumulator przed przeladowaniem.
3. **Tryb Stalej Mocy Wyjsciowej (CP - Constant Power)**:
   - Petla regulacji zamknietej utrzymujaca stala zadana moc wyjsciowa (W) poprzez dynamiczne dostosowywanie napiecia Vset do zmian pradu obciazenia Iout (z zachowaniem bezpiecznego limitu Max U oraz Max I).
4. **Pomiar Tetnien (Ripple mVpp / mApp) oraz Zapasu Napiecia (Headroom Vin - Vout)**:
   - Ciagly pomiar tetnien miedzyszczytowych napiecia (mVpp) i pradu (mApp) wraz z ocena stabilnosci wyjscia.
   - Monitorowanie spadku i zapasu napiecia wejsciowego przetwornicy obnizajacej (Vin minus Vout) z ostrzezeniem przy zbyt malym zapasie (ponizej 1.2 V).
5. **Pelna Diagnostyka Sprzetowa i Limity Wejsciowe (0xDE - 0xE2)**:
   - Odczyt modelu zasilacza (0xDE), wersji plyty glownej (HW 0xDF), wersji oprogramowania (FW 0xE0) oraz maksymalnych limitow napiecia i pradu wspieranych przez aktualny zasilacz wejsciowy USB-PD / DC (MaxV 0xE1, MaxI 0xE2).
6. **Oscyloskop Cyfrowy (V / I / P) z Eksportem CSV**:
   - Wykres przebiegow napiecia, pradu i mocy w czasie rzeczywistym z obsluga przyblizania rolka myszy (Zoom), przesuwania historii (Pan), kursorow pomiarowych i zapisu logow do pliku CSV.
7. **Zabezpieczenia (OVP / OCP / OPP / OTP / LVP), Profile M1-M6 oraz Generator Sekwencji**:
   - Konfiguracja wszystkich progow zabezpieczen i profili pamieci EEPROM zasilacza oraz automatyczny generator ramp napieciowych (Soft-Start, schodki napieciowe, testy impulsowe).
8. **Przelacznik Jezyka (PL / EN) oraz Tryb Symulatora**:
   - Zmiana jezyka interfejsu miedzy polskim a angielskim jednym kliknieciem oraz wbudowany symulator wirtualnego obciazenia.

---

## Keyboard Shortcuts / Skroty klawiszowe

| Key / Klawisz | Action (EN) | Akcja (PL) |
| :--- | :--- | :--- |
| **F5** | Toggle Output ON / OFF | Wlacz / Wylacz wyjscie zasilacza (OUTPUT) |
| **Esc** | EMERGENCY STOP (Output OFF + 0V + Stop Charger/CP/Seq) | AWARYJNY STOP (Wylaczenie wyjscia, 0V, stop ladowarki/CP) |
| **P** | Pause / Resume Oscilloscope Chart | Pauza / Wznowienie wykresu oscyloskopu |
| **Alt + 1..6** | Recall Memory Preset M1 - M6 | Szybkie wczytanie profilu pamieci M1 - M6 |

---

## Building from Source / Kompilacja ze zrodel

No external SDKs or NuGet packages are required. You can compile the project on any Windows 10 or Windows 11 machine using the built-in .NET Framework 4.8 C# compiler (`csc.exe`):

```bat
build.bat
```

Or directly via PowerShell / CMD:
```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:winexe /out:FNIRSI_DPS150_Studio.exe /win32icon:app.ico /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll AssemblyInfo.cs Program.cs Dps150Protocol.cs LiveChartControl.cs MainForm.cs
```

---

## Author / Autor

**Created by: PATRYK JANKOWSKI**
