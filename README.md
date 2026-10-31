# ⚡ FNIRSI DPS-150 Studio Pro

**Created by: PATRYK JANKOWSKI**

![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D6?style=for-the-badge&logo=windows)
![Framework](https://img.shields.io/badge/.NET%20Framework-4.8-512BD4?style=for-the-badge&logo=dotnet)
![Hardware](https://img.shields.io/badge/Hardware-FNIRSI%20DPS--150%20(30V%20%2F%205A%20%2F%20150W)-00E5FF?style=for-the-badge)
![Language](https://img.shields.io/badge/Language-PL%20%7C%20EN-FFD600?style=for-the-badge)

> **🇬🇧 English** | **[🇵🇱 Wersja Polska poniżej](#-fnirsi-dps-150-studio-pro--opis-po-polsku)**

---

## 🇬🇧 Overview (English)

**FNIRSI DPS-150 Studio Pro** is an advanced, standalone Windows desktop control center, real-time oscilloscope, smart battery charger (`1S–7S`), and constant-power (`CP`) controller designed specifically for the **FNIRSI DPS-150** CNC DC programmable laboratory power supply (`0–30V`, `0–5A`, `150W`).

Built as a lightweight, zero-dependency single-file `.exe` (using native Windows `.NET Framework 4.8`), it communicates directly with the power supply over USB Virtual COM (`115200 bps`, `VID_2E3C`) and unlocks features not available on the standalone device screen.

### ✨ Key Features

- **📊 High-Precision Digital Dashboard**:
  - Live telemetry for Output Voltage (`Vout`), Output Current (`Iout`), and Output Power (`Pout`).
  - Instant calculation of **Load Resistance (`Ω`)**, **Input Voltage Headroom (`ΔU = Vin - Vout`)** with automatic low-dropout warning, and **Peak-to-Peak Ripple (`mVpp` / `mApp`)**.
  - Automatic activation of hardware & high-precision software **Capacity (`Ah` / `mAh`)** and **Energy (`Wh` / `mWh`)** counters (`0xD8` register).
- **📈 Real-Time Oscilloscope (`V / I / P`)**:
  - Smooth double-buffered waveform plotter with configurable time windows (`10s`, `30s`, `1m`, `5m`), mouse-wheel zoom, history panning, crosshair inspection, pause (`[P]`), and one-click **CSV Export**.
- **🔋 Smart Battery Charger Mode (`1S – 7S` CC/CV + Auto-Cutoff)**:
  - Supports **Li-Ion / Li-Po 3.7V** (`4.20V/cell`), **Li-Ion HV 3.8V** (`4.35V/cell`), **LiFePO4 3.2V** (`3.65V/cell`), **Lead-Acid Pb / AGM 2.0V** (`2.40V/cell`), and **NiMH / NiCd 1.2V** (`1.45V/cell`).
  - Quick pack selectors (**`1S`, `2S`, `3S`, `4S`, `5S`, `6S`, `7S`**) with automatic target voltage calculation and per-cell voltage telemetry (`V/cell`).
  - **Automatic CV Cutoff**: Automatically turns **OUTPUT OFF** when charging current drops below the configurable cutoff threshold (`I_cutoff`, e.g., `50 mA`) in the CV absorption phase or when the maximum `mAh` capacity limit is reached.
- **⚡ Constant Power Mode (`CP — Constant Power`)**:
  - Closed-loop software power regulator that dynamically adjusts `Vset` based on load current `Iout` to maintain a constant target power (`W`) within safe `Max U` and `Max I` limits.
- **🛡️ Full Hardware Protections & EEPROM Presets (`M1 – M6`)**:
  - Read and write `OVP`, `OCP`, `OPP`, `OTP`, and `LVP` thresholds, display brightness, buzzer volume, and hardware memory groups `M1–M6`.
- **🔄 Automated Sequence & Voltage Ramp Generator**:
  - Programmable multi-step voltage/current sequences with soft-start and pulse templates.
- **🔍 Hardware Diagnostics & HEX / ATEasy Protocol Monitor**:
  - Live readout of `Model (0xDE)`, `HW Version (0xDF)`, `FW Version (0xE0)`,PD/DC input limits `MaxV (0xE1)` / `MaxI (0xE2)`, and live `TX/RX` hexadecimal packet sniffer.
  - Graceful USB disconnect (`0xF1 0xC1 0x00 0x01 0x00 0x01`) that unlocks physical buttons on the DPS-150 upon exit.
- **🌐 Instant Language Switch (`PL ▸ EN`) & Built-in Simulator Mode**:
  - Switch the entire UI between **Polish** and **English** at any time, or test all features without hardware using the built-in virtual load simulator.

---

## 🇵🇱 FNIRSI DPS-150 Studio Pro — Opis po polsku

**FNIRSI DPS-150 Studio Pro** to zaawansowana aplikacja desktopowa dla systemu Windows do pełnego sterowania, diagnostyki i wizualizacji parametrów laboratoryjnego zasilacza regulowanego **FNIRSI DPS-150 (`30V / 5A / 150W`)**.

Program został napisany w języku C# jako lekki, samodzielny plik wykonywalny `FNIRSI_DPS150_Studio.exe` (nie wymaga instalacji żadnych zewnętrznych bibliotek ani środowisk). Automatycznie wykrywa port USB zasilacza (`VID_2E3C`) i odblokowuje funkcje niedostępne z poziomu fizycznego panelu urządzenia.

### 🚀 Główne możliwości programu

1. **📊 Cyfrowy Pulpit Pomiarowy na Żywo**:
   - Precyzyjny odczyt napięcia wyjściowego (`Vout`), prądu (`Iout`) oraz mocy (`Pout`).
   - Automatyczne włączanie sprzętowego i programowego zliczania **Pojemności (`Ah` / `mAh`)** oraz **Energii (`Wh` / `mWh`)** (rejestr `0xD8`).
   - Wyliczanie na żywo **Rezystancji obciążenia (`Ω`)**, **Czasu aktywnej pracy wyjścia (`Czas ON`)** oraz temperatury wewnętrznej (`°C`).
2. **🔋 Inteligentny Tryb Ładowania Akumulatorów (`1S – 7S` CC/CV z Auto-Stopem)**:
   - Obsługa najpopularniejszych chemii ogniw:
     - **Li-Ion / Li-Po 3.7V** (`4.20 V / ogniwo`)
     - **Li-Ion HV 3.8V** (`4.35 V / ogniwo`)
     - **LiFePO4 3.2V** (`3.65 V / ogniwo`)
     - **Kwasowo-ołowiowe Pb / AGM 2.0V** (`2.40 V / ogniwo`)
     - **NiMH / NiCd 1.2V** (`1.45 V / ogniwo`)
   - Szybki wybór pakietów **`1S`, `2S`, `3S`, `4S`, `5S`, `6S`, `7S`** z automatycznym przeliczaniem napięcia docelowego oraz podglądem napięcia pojedynczego ogniwa (`V / ogniwo`).
   - **Automatyczne odcięcie po naładowaniu (`Auto-Cutoff`)**: Gdy w fazie dopełniania napięciowego (`CV`) prąd ładowania spadnie poniżej zadanego progu (np. `0.050 A` = `50 mA`) lub zostanie osiągnięty limit pojemności `mAh`, aplikacja **automatycznie wyłącza wyjście zasilacza (`OUTPUT OFF`)**, chroniąc akumulator przed przeładowaniem.
3. **⚡ Tryb Stałej Mocy Wyjściowej (`CP — Constant Power`)**:
   - Pętla regulacji zamkniętej utrzymująca stałą zadaną moc wyjściową (`W`) poprzez dynamiczne dostosowywanie napięcia `Vset` do zmian prądu obciążenia `Iout` (z zachowaniem bezpiecznego sufitu `Max U` oraz `Max I`).
4. **📉 Pomiar Tętnień (`Ripple mVpp / mApp`) oraz Zapasu Napięcia (`Headroom ΔU`)**:
   - Ciągły pomiar tętnień międzyszczytowych napięcia (`mVpp`) i prądu (`mApp`) wraz z oceną stabilności wyjścia.
   - Monitorowanie spadku/zapasu napięcia wejściowego przetwornicy obniżającej (`ΔU = Vin - Vout`) z alarmem kolorystycznym przy zbyt małym zapasie (`< 1.2 V`).
5. **🔍 Pełna Diagnostyka Sprzętowa i Limity Wejściowe (`0xDE` – `0xE2`)**:
   - Odczyt modelu zasilacza (`0xDE`), wersji płyty głównej (`HW 0xDF`), wersji oprogramowania (`FW 0xE0`) oraz maksymalnych limitów napięcia i prądu wspieranych przez aktualny zasilacz wejściowy USB-PD / DC (`MaxV 0xE1`, `MaxI 0xE2`).
6. **📈 Oscyloskop Cyfrowy (`V / I / P`) z Eksportem CSV**:
   - Wykres przebiegów napięcia, prądu i mocy w czasie rzeczywistym z obsługą przybliżania rolką myszy (Zoom), przesuwania historii (Pan), kursorów pomiarowych i zapisu logów do pliku `.csv`.
7. **🛡️ Zabezpieczenia (`OVP / OCP / OPP / OTP / LVP`), Profile `M1–M6` oraz Generator Sekwencji**:
   - Konfiguracja wszystkich progów zabezpieczeń i profili pamięci EEPROM zasilacza oraz automatyczny generator ramp napięciowych (Soft-Start, schodki napięciowe, testy impulsowe).
8. **🌐 Przełącznik Języka (`PL ▸ EN`) oraz Tryb Symulatora**:
   - Błyskawiczna zmiana języka interfejsu między polskim a angielskim jednym kliknięciem oraz wbudowany symulator wirtualnego obciążenia.

---

## ⌨️ Keyboard Shortcuts / Skróty klawiszowe

| Key / Klawisz | Action (EN) | Akcja (PL) |
| :--- | :--- | :--- |
| **`F5`** | Toggle Output ON / OFF | Włącz / Wyłącz wyjście zasilacza (`OUTPUT`) |
| **`Esc`** | **EMERGENCY STOP** (Output OFF + 0V + Stop Charger/CP/Seq) | **AWARYJNY STOP** (Wyłączenie wyjścia, 0V, stop ładowarki/CP) |
| **`P`** | Pause / Resume Oscilloscope Chart | Pauza / Wznowienie wykresu oscyloskopu |
| **`Alt + 1..6`** | Recall Memory Preset `M1` – `M6` | Szybkie wczytanie profilu pamięci `M1` – `M6` |

---

## 🛠️ Building from Source / Kompilacja ze źródeł

No external SDKs or NuGet packages are required! You can compile the project on any Windows 10/11 machine using the built-in `.NET Framework 4.8` C# compiler (`csc.exe`):

```bat
build.bat
```

Or directly via PowerShell / CMD:
```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:winexe /out:FNIRSI_DPS150_Studio.exe /win32icon:app.ico /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll AssemblyInfo.cs Program.cs Dps150Protocol.cs LiveChartControl.cs MainForm.cs
```

---

## 👤 Author / Autor

**Created by: PATRYK JANKOWSKI**
