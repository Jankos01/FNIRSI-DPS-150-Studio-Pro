using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace FnirsiDps150Studio
{
    public class MainForm : Form
    {
        private readonly Dps150Driver _driver = new Dps150Driver();
        private bool _isEnglish = false;

        // Top Connection Bar
        private Label _lblPortTitle;
        private ComboBox _cboPort;
        private ComboBox _cboBaud;
        private Button _btnRefreshPorts;
        private Button _btnConnect;
        private Button _btnSimulate;
        private Button _btnLangToggle;
        private NumericUpDown _nudSimLoad;
        private Label _lblSimLoadTitle;
        private Label _lblConnectionBadge;
        private Label _lblDeviceInfo;

        // Big Readouts & Titles
        private Label _lblCardTitleV;
        private Label _lblCardTitleI;
        private Label _lblCardTitleP;
        private Label _lblVoutBig;
        private Label _lblIoutBig;
        private Label _lblPoutBig;
        private Label _lblVsetSub;
        private Label _lblIsetSub;
        private Label _lblPmaxSub;
        private ProgressBar _barV;
        private ProgressBar _barI;
        private ProgressBar _barP;

        // Secondary Status Indicators & Titles
        private Label _lblStatTitleMode;
        private Label _lblStatTitleProt;
        private Label _lblStatTitleVin;
        private Label _lblStatTitleRes;
        private Label _lblStatTitleCap;
        private Label _lblStatTitleEne;
        private Label _lblStatTitleTemp;
        private Label _lblModeBadge;
        private Label _lblProtBadge;
        private Label _lblVinVal;
        private Label _lblTempVal;
        private Label _lblResistanceVal;
        private Label _lblCapacityVal;
        private Label _lblEnergyVal;
        private Button _btnResetCounters;

        // Fast Setpoint Controls
        private Button _btnOutputToggle;
        private Button _btnEmergencyOff;
        private Label _lblVHeader;
        private Label _lblIHeader;
        private Label _lblMQuick;
        private NumericUpDown _nudVset;
        private TrackBar _trkVset;
        private NumericUpDown _nudIset;
        private TrackBar _trkIset;
        private CheckBox _chkLiveSliders;
        private bool _suppressSliderEvents;

        // Tabs
        private TabPage _tabChart;
        private TabPage _tabChargerAndCp;
        private TabPage _tabProtAndPresets;
        private TabPage _tabSequence;
        private TabPage _tabProtocol;

        // Battery Charger (1S-7S) & Constant Power (CP) & Diagnostics Tab Controls
        private Label _lblChgSectionTitle;
        private Label _lblChgChemTitle;
        private ComboBox _cboBattChem;
        private Label _lblChgCellsTitle;
        private readonly Button[] _btnCells = new Button[7];
        private int _selectedCellCount = 3; // Default 3S (e.g. 11.1V / 12.60V Li-Ion)
        private Label _lblChgVTitle;
        private Label _lblChgITitle;
        private Label _lblChgCutoffTitle;
        private Label _lblChgMaxMahTitle;
        private NumericUpDown _nudChgVoltage;
        private NumericUpDown _nudChgCurrent;
        private NumericUpDown _nudChgCutoffCurrent;
        private NumericUpDown _nudChgMaxMah;
        private Button _btnChgStart;
        private Button _btnChgStop;
        private Label _lblChgStatusBanner;
        private Label _lblChgLiveMetrics;
        private bool _chargerActive = false;
        private int _chgCutoffConfirmTicks = 0;
        private float _chgStartAh = 0f;
        private float _chgStartWh = 0f;
        private DateTime _chgStartTime = DateTime.Now;

        // Constant Power (CP) & Hardware Diagnostics Controls
        private Label _lblCpSectionTitle;
        private Label _lblCpWattsTitle;
        private Label _lblCpMaxVTitle;
        private Label _lblCpMaxITitle;
        private NumericUpDown _nudCpWatts;
        private NumericUpDown _nudCpMaxV;
        private NumericUpDown _nudCpMaxI;
        private Button _btnCpStart;
        private Button _btnCpStop;
        private Label _lblCpStatus;
        private bool _cpModeActive = false;
        private System.Windows.Forms.Timer _chgAndCpTimer;

        private Label _lblDiagSectionTitle;
        private Button _btnDiagRefresh;
        private Label _lblDiagDetails;

        // Chart Tab Controls
        private LiveChartControl _chart;
        private Button _btnPauseChart;
        private Button _btnClearChart;
        private Button _btnExportCsv;
        private CheckBox _chkShowV;
        private CheckBox _chkShowI;
        private CheckBox _chkShowP;
        private CheckBox _chkAutoScale;

        // Memory Presets M1-M6 & Protections Tab
        private Label _lblPresetSectionTitle;
        private Label _lblProtSectionTitle;
        private readonly Label[] _lblPresetM = new Label[6];
        private readonly Button[] _btnPresetUse = new Button[6];
        private readonly Button[] _btnPresetSave = new Button[6];
        private readonly NumericUpDown[] _nudPresetV = new NumericUpDown[6];
        private readonly NumericUpDown[] _nudPresetI = new NumericUpDown[6];
        private Label _lblOvpRow;
        private Label _lblOcpRow;
        private Label _lblOppRow;
        private Label _lblOtpRow;
        private Label _lblLvpRow;
        private NumericUpDown _nudOvp;
        private NumericUpDown _nudOcp;
        private NumericUpDown _nudOpp;
        private NumericUpDown _nudOtp;
        private NumericUpDown _nudLvp;
        private Button _btnApplyProt;
        private Button _btnSyncFromDevice;
        private Label _lblBriTitle;
        private Label _lblVolTitle;
        private TrackBar _trkBrightness;
        private Label _lblBrightnessVal;
        private TrackBar _trkVolume;
        private Label _lblVolumeVal;
        private bool _initialSettingsSynced;

        // Sequence / Ramp Generator Tab
        private DataGridView _gridSeq;
        private Label _lblSeqTitle;
        private Button _btnTpl1;
        private Button _btnTpl2;
        private Button _btnTpl3;
        private CheckBox _chkSeqLoop;
        private Button _btnSeqStart;
        private Button _btnSeqStop;
        private Label _lblSeqStatus;
        private System.Windows.Forms.Timer _seqTimer;
        private int _seqCurrentStep = -1;

        // Protocol Log Tab
        private Label _lblLogHeader;
        private Button _btnClearLog;
        private ListBox _lstPacketLog;
        private CheckBox _chkLogRx;
        private Panel _footerPanel;

        // Colors
        private static readonly Color BgDark = Color.FromArgb(15, 20, 28);
        private static readonly Color PanelDark = Color.FromArgb(22, 29, 41);
        private static readonly Color CardDark = Color.FromArgb(29, 38, 54);
        private static readonly Color AccentCyan = Color.FromArgb(0, 229, 255);
        private static readonly Color AccentAmber = Color.FromArgb(255, 214, 0);
        private static readonly Color AccentMagenta = Color.FromArgb(255, 64, 129);
        private static readonly Color AccentGreen = Color.FromArgb(16, 185, 129);
        private static readonly Color AccentRed = Color.FromArgb(239, 68, 68);
        private static readonly Color TextLight = Color.FromArgb(241, 245, 249);
        private static readonly Color TextMuted = Color.FromArgb(148, 163, 184);

        public MainForm()
        {
            this.Text = "FNIRSI DPS-150 Studio Pro — Created by: PATRYK JANKOWSKI";
            this.Size = new Size(1380, 910);
            this.MinimumSize = new Size(1180, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = BgDark;
            this.ForeColor = TextLight;
            this.Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);
            this.KeyPreview = true;
            this.Icon = CreateCustomLogoIcon();

            BuildUi();
            WireEvents();
            RefreshPortList();
            ApplyLanguage();
        }

        private void BuildUi()
        {
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = BgDark,
                Padding = new Padding(8, 8, 8, 4)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));  // Top bar with Logo & Lang switch
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 345f)); // Readouts + Fast Controls
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Tabs
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));  // Author Footer Bar

            mainLayout.Controls.Add(BuildTopConnectionBar(), 0, 0);
            mainLayout.Controls.Add(BuildUpperDashboard(), 0, 1);
            mainLayout.Controls.Add(BuildLowerTabs(), 0, 2);
            mainLayout.Controls.Add(BuildAuthorFooterBar(), 0, 3);

            this.Controls.Add(mainLayout);
        }

        private Control BuildTopConnectionBar()
        {
            var topTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = PanelDark,
                Padding = new Padding(8, 5, 10, 5)
            };
            topTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75f));
            topTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            var leftFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = PanelDark
            };

            leftFlow.Controls.Add(CreateHeaderLogoBadge());

            _lblPortTitle = new Label
            {
                Text = "Port USB:",
                AutoSize = true,
                Margin = new Padding(2, 7, 4, 0),
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            leftFlow.Controls.Add(_lblPortTitle);

            _cboPort = new ComboBox
            {
                Width = 205,
                Margin = new Padding(0, 3, 6, 0),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = CardDark,
                ForeColor = TextLight,
                FlatStyle = FlatStyle.Flat
            };
            leftFlow.Controls.Add(_cboPort);

            _btnRefreshPorts = CreateFlatButton("⟳ Odśwież", new Size(86, 28), CardDark);
            _btnRefreshPorts.Margin = new Padding(0, 2, 8, 0);
            leftFlow.Controls.Add(_btnRefreshPorts);

            var lblBaud = new Label
            {
                Text = "Baud:",
                AutoSize = true,
                Margin = new Padding(0, 7, 4, 0),
                ForeColor = TextMuted
            };
            leftFlow.Controls.Add(lblBaud);

            _cboBaud = new ComboBox
            {
                Width = 80,
                Margin = new Padding(0, 3, 8, 0),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = CardDark,
                ForeColor = TextLight,
                FlatStyle = FlatStyle.Flat
            };
            _cboBaud.Items.AddRange(new object[] { "115200", "9600", "57600", "38400" });
            _cboBaud.SelectedIndex = 0;
            leftFlow.Controls.Add(_cboBaud);

            _btnConnect = CreateFlatButton("🔌 Połącz z DPS-150", new Size(154, 30), Color.FromArgb(2, 132, 199));
            _btnConnect.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            _btnConnect.Margin = new Padding(0, 1, 6, 0);
            leftFlow.Controls.Add(_btnConnect);

            _btnSimulate = CreateFlatButton("🧪 Tryb Symulatora", new Size(144, 30), Color.FromArgb(71, 85, 105));
            _btnSimulate.Margin = new Padding(0, 1, 6, 0);
            leftFlow.Controls.Add(_btnSimulate);

            _lblSimLoadTitle = new Label
            {
                Text = "Obciążenie [Ω]:",
                AutoSize = true,
                Margin = new Padding(4, 7, 4, 0),
                ForeColor = AccentAmber,
                Visible = false
            };
            leftFlow.Controls.Add(_lblSimLoadTitle);

            _nudSimLoad = new NumericUpDown
            {
                Width = 65,
                Margin = new Padding(0, 4, 0, 0),
                DecimalPlaces = 1,
                Minimum = 0.2m,
                Maximum = 1000m,
                Increment = 0.5m,
                Value = 10.0m,
                BackColor = CardDark,
                ForeColor = AccentAmber,
                Visible = false
            };
            leftFlow.Controls.Add(_nudSimLoad);

            var rightFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = PanelDark
            };

            // Small PL / EN language switch button on the far right
            _btnLangToggle = CreateFlatButton("🌐 PL ▸ EN", new Size(78, 28), Color.FromArgb(30, 58, 138));
            _btnLangToggle.Font = new Font("Consolas", 8.5f, FontStyle.Bold);
            _btnLangToggle.ForeColor = AccentCyan;
            _btnLangToggle.Margin = new Padding(4, 2, 0, 0);
            rightFlow.Controls.Add(_btnLangToggle);

            _lblConnectionBadge = new Label
            {
                Text = "● ROZŁĄCZONO",
                AutoSize = true,
                Margin = new Padding(4, 7, 6, 0),
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            rightFlow.Controls.Add(_lblConnectionBadge);

            _lblDeviceInfo = new Label
            {
                Text = "COM4",
                AutoSize = true,
                Margin = new Padding(4, 7, 4, 0),
                ForeColor = TextMuted
            };
            rightFlow.Controls.Add(_lblDeviceInfo);

            topTable.Controls.Add(leftFlow, 0, 0);
            topTable.Controls.Add(rightFlow, 1, 0);

            return topTable;
        }

        private Control BuildUpperDashboard()
        {
            var split = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 6, 0, 6)
            };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 51f));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 49f));

            split.Controls.Add(BuildDigitalReadoutsPanel(), 0, 0);
            split.Controls.Add(BuildFastControlsPanel(), 1, 0);

            return split;
        }

        private Control BuildDigitalReadoutsPanel()
        {
            var container = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PanelDark,
                Margin = new Padding(0, 0, 6, 0),
                Padding = new Padding(8)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 3
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 146f)); // 3 Big Meter Cards
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82f));  // Mode, Protection, Vin, R_load
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82f));  // Energy / Capacity Ah, Wh, Temp

            // Card 1: VOLTAGE
            layout.Controls.Add(CreateMeterCard(
                "NAPIĘCIE WYJŚCIOWE",
                "00.000 V",
                "Vset: 5.00 V (OVP: 30.5V)",
                AccentCyan,
                out _lblCardTitleV,
                out _lblVoutBig,
                out _lblVsetSub,
                out _barV,
                3000), 0, 0);

            // Card 2: CURRENT
            layout.Controls.Add(CreateMeterCard(
                "PRĄD WYJŚCIOWY",
                "0.000 A",
                "Iset: 1.000 A (OCP: 5.10A)",
                AccentAmber,
                out _lblCardTitleI,
                out _lblIoutBig,
                out _lblIsetSub,
                out _barI,
                5000), 1, 0);

            // Card 3: POWER
            layout.Controls.Add(CreateMeterCard(
                "MOC WYJŚCIOWA",
                "00.000 W",
                "OPP: 152.0 W (Max 30V/5A)",
                AccentMagenta,
                out _lblCardTitleP,
                out _lblPoutBig,
                out _lblPmaxSub,
                out _barP,
                1500), 2, 0);

            // Row 1: Status Strip (Span 3 columns)
            var statusRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0, 6, 0, 4)
            };
            for (int i = 0; i < 4; i++)
                statusRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            statusRow.Controls.Add(CreateMiniStatBox("TRYB PRACY", "STANDBY", AccentGreen, out _lblStatTitleMode, out _lblModeBadge), 0, 0);
            statusRow.Controls.Add(CreateMiniStatBox("ZABEZPIECZENIE", "OK (Normal)", AccentGreen, out _lblStatTitleProt, out _lblProtBadge), 1, 0);
            statusRow.Controls.Add(CreateMiniStatBox("WEJŚCIE (Vin)", "0.00 V", AccentCyan, out _lblStatTitleVin, out _lblVinVal), 2, 0);
            statusRow.Controls.Add(CreateMiniStatBox("REZYSTANCJA OBC.", "∞ Ω", TextLight, out _lblStatTitleRes, out _lblResistanceVal), 3, 0);

            layout.Controls.Add(statusRow, 0, 1);
            layout.SetColumnSpan(statusRow, 3);

            // Row 2: Energy / Capacity / Temp Strip (Span 3 columns)
            var energyRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0, 4, 0, 0)
            };
            energyRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29f));
            energyRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29f));
            energyRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            energyRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f));

            energyRow.Controls.Add(CreateMiniStatBox("POJEMNOŚĆ (Ah)", "0.000 Ah (0 mAh)", AccentAmber, out _lblStatTitleCap, out _lblCapacityVal), 0, 0);
            energyRow.Controls.Add(CreateMiniStatBox("ENERGIA (Wh)", "0.000 Wh (0 mWh)", AccentMagenta, out _lblStatTitleEne, out _lblEnergyVal), 1, 0);
            energyRow.Controls.Add(CreateMiniStatBox("TEMPERATURA", "25.0 °C", TextLight, out _lblStatTitleTemp, out _lblTempVal), 2, 0);

            _btnResetCounters = CreateFlatButton("↺ Zeruj Ah/Wh", new Size(100, 40), CardDark);
            _btnResetCounters.Dock = DockStyle.Fill;
            _btnResetCounters.Margin = new Padding(4, 4, 4, 4);
            energyRow.Controls.Add(_btnResetCounters, 3, 0);

            layout.Controls.Add(energyRow, 0, 2);
            layout.SetColumnSpan(energyRow, 3);

            container.Controls.Add(layout);
            return container;
        }

        private Control CreateMeterCard(
            string title,
            string initialBig,
            string initialSub,
            Color accent,
            out Label lblTitle,
            out Label lblBig,
            out Label lblSub,
            out ProgressBar bar,
            int maxBar)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardDark,
                Margin = new Padding(4),
                BorderStyle = BorderStyle.FixedSingle
            };

            lblTitle = new Label
            {
                Text = title,
                Location = new Point(10, 8),
                AutoSize = true,
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };

            lblBig = new Label
            {
                Text = initialBig,
                Location = new Point(8, 30),
                AutoSize = true,
                ForeColor = accent,
                Font = new Font("Consolas", 22f, FontStyle.Bold)
            };

            lblSub = new Label
            {
                Text = initialSub,
                Location = new Point(10, 78),
                AutoSize = true,
                ForeColor = TextLight,
                Font = new Font("Consolas", 8.5f, FontStyle.Bold)
            };

            bar = new ProgressBar
            {
                Location = new Point(10, 108),
                Size = new Size(180, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Maximum = maxBar,
                Value = 0,
                Style = ProgressBarStyle.Continuous
            };

            ProgressBar localBar = bar;
            card.Resize += (s, e) =>
            {
                localBar.Width = Math.Max(40, card.ClientSize.Width - 20);
            };

            card.Controls.Add(lblTitle);
            card.Controls.Add(lblBig);
            card.Controls.Add(lblSub);
            card.Controls.Add(bar);
            return card;
        }

        private Control CreateMiniStatBox(string title, string initialVal, Color valColor, out Label lblT, out Label lblVal)
        {
            var box = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardDark,
                Margin = new Padding(4),
                BorderStyle = BorderStyle.FixedSingle
            };

            lblT = new Label
            {
                Text = title,
                Location = new Point(10, 8),
                AutoSize = true,
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold)
            };

            lblVal = new Label
            {
                Text = initialVal,
                Location = new Point(10, 30),
                AutoSize = true,
                ForeColor = valColor,
                Font = new Font("Consolas", 10.5f, FontStyle.Bold)
            };

            box.Controls.Add(lblT);
            box.Controls.Add(lblVal);
            return box;
        }

        private Control BuildFastControlsPanel()
        {
            var container = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PanelDark,
                Margin = new Padding(6, 0, 0, 0),
                Padding = new Padding(10, 8, 10, 8)
            };

            var rows = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 8
            };
            rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f)); // Row 0: Output ON/OFF + Emergency STOP
            rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f)); // Row 1: Vset header + live checkbox
            rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f)); // Row 2: Vset Numeric + TrackBar + Step buttons
            rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f)); // Row 3: Vset Quick Presets (1.8V..30V)
            rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f)); // Row 4: Iset header
            rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f)); // Row 5: Iset Numeric + TrackBar + Step buttons
            rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f)); // Row 6: Iset Quick Presets (0.05A..5.0A)
            rows.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Row 7: M1..M6 Recall strip

            // Row 0: Output Switch + Emergency Off
            var row0 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };
            row0.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72f));
            row0.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));

            _btnOutputToggle = new Button
            {
                Text = "⚪ WYJŚCIE WYŁĄCZONE (OFF) — Kliknij [F5]",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 6, 4),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = TextLight,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnOutputToggle.FlatAppearance.BorderColor = Color.FromArgb(100, 116, 139);
            row0.Controls.Add(_btnOutputToggle, 0, 0);

            _btnEmergencyOff = new Button
            {
                Text = "🛑 STOP 0V [Esc]",
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 0, 0, 4),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(127, 29, 29),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnEmergencyOff.FlatAppearance.BorderColor = AccentRed;
            row0.Controls.Add(_btnEmergencyOff, 1, 0);
            rows.Controls.Add(row0, 0, 0);

            // Row 1: Voltage Header + Live Sliders Checkbox
            var row1 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };
            row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));

            _lblVHeader = new Label
            {
                Text = "NASTAWA NAPIĘCIA (Vset) [0 – 30 V]:",
                Dock = DockStyle.Fill,
                ForeColor = AccentCyan,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft
            };
            row1.Controls.Add(_lblVHeader, 0, 0);

            _chkLiveSliders = new CheckBox
            {
                Text = "Aktualizuj na żywo z suwaków",
                Dock = DockStyle.Fill,
                Checked = true,
                ForeColor = TextMuted,
                CheckAlign = ContentAlignment.MiddleRight,
                TextAlign = ContentAlignment.MiddleRight
            };
            row1.Controls.Add(_chkLiveSliders, 1, 0);
            rows.Controls.Add(row1, 0, 1);

            // Row 2: Vset Numeric + TrackBar + Step buttons
            var row2 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0)
            };
            row2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112f));
            row2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            row2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 224f));

            _nudVset = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 6, 4),
                DecimalPlaces = 2,
                Minimum = 0.00m,
                Maximum = 30.00m,
                Increment = 0.10m,
                Value = 5.00m,
                Font = new Font("Consolas", 13f, FontStyle.Bold),
                BackColor = CardDark,
                ForeColor = AccentCyan
            };
            row2.Controls.Add(_nudVset, 0, 0);

            _trkVset = new TrackBar
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                Height = 28,
                Margin = new Padding(2, 4, 4, 4),
                Minimum = 0,
                Maximum = 3000,
                TickStyle = TickStyle.None,
                Value = 500,
                BackColor = PanelDark
            };
            row2.Controls.Add(_trkVset, 1, 0);

            var vStepFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 3, 0, 0)
            };
            float[] vSteps = new float[] { -1.0f, -0.1f, +0.1f, +1.0f };
            string[] vStepLabels = new string[] { "-1V", "-0.1", "+0.1", "+1V" };
            for (int i = 0; i < vSteps.Length; i++)
            {
                float delta = vSteps[i];
                var btnStep = CreateFlatButton(vStepLabels[i], new Size(52, 28), CardDark);
                btnStep.Margin = new Padding(2, 0, 2, 0);
                btnStep.Font = new Font("Consolas", 8.5f, FontStyle.Bold);
                btnStep.Click += (s, e) => AdjustVoltageBy(delta);
                vStepFlow.Controls.Add(btnStep);
            }
            row2.Controls.Add(vStepFlow, 2, 0);
            rows.Controls.Add(row2, 0, 2);

            // Row 3: Quick Voltage Presets
            var row3 = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 2, 0, 2)
            };
            float[] vPresets = new float[] { 1.8f, 3.3f, 5.0f, 9.0f, 12.0f, 15.0f, 19.0f, 24.0f, 30.0f };
            foreach (float vp in vPresets)
            {
                float targetV = vp;
                var btnVp = CreateFlatButton(vp.ToString("0.0", CultureInfo.InvariantCulture) + "V", new Size(58, 27), CardDark);
                btnVp.Margin = new Padding(0, 1, 5, 1);
                btnVp.ForeColor = AccentCyan;
                btnVp.Font = new Font("Consolas", 8.5f, FontStyle.Bold);
                btnVp.Click += (s, e) => ApplyVoltageTarget(targetV);
                row3.Controls.Add(btnVp);
            }
            rows.Controls.Add(row3, 0, 3);

            // Row 4: Current Header
            _lblIHeader = new Label
            {
                Text = "LIMIT PRĄDU (Iset) [0.000 – 5.000 A]:",
                Dock = DockStyle.Fill,
                ForeColor = AccentAmber,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft
            };
            rows.Controls.Add(_lblIHeader, 0, 4);

            // Row 5: Iset Numeric + TrackBar + Step buttons
            var row5 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0)
            };
            row5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112f));
            row5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            row5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 224f));

            _nudIset = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 6, 4),
                DecimalPlaces = 3,
                Minimum = 0.000m,
                Maximum = 5.000m,
                Increment = 0.050m,
                Value = 1.000m,
                Font = new Font("Consolas", 13f, FontStyle.Bold),
                BackColor = CardDark,
                ForeColor = AccentAmber
            };
            row5.Controls.Add(_nudIset, 0, 0);

            _trkIset = new TrackBar
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                Height = 28,
                Margin = new Padding(2, 4, 4, 4),
                Minimum = 0,
                Maximum = 5000,
                TickStyle = TickStyle.None,
                Value = 1000,
                BackColor = PanelDark
            };
            row5.Controls.Add(_trkIset, 1, 0);

            var iStepFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 3, 0, 0)
            };
            float[] iSteps = new float[] { -0.5f, -0.1f, +0.1f, +0.5f };
            string[] iStepLabels = new string[] { "-0.5", "-0.1", "+0.1", "+0.5" };
            for (int i = 0; i < iSteps.Length; i++)
            {
                float delta = iSteps[i];
                var btnStep = CreateFlatButton(iStepLabels[i], new Size(52, 28), CardDark);
                btnStep.Margin = new Padding(2, 0, 2, 0);
                btnStep.Font = new Font("Consolas", 8.5f, FontStyle.Bold);
                btnStep.Click += (s, e) => AdjustCurrentBy(delta);
                iStepFlow.Controls.Add(btnStep);
            }
            row5.Controls.Add(iStepFlow, 2, 0);
            rows.Controls.Add(row5, 0, 5);

            // Row 6: Quick Current Presets
            var row6 = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 2, 0, 2)
            };
            float[] iPresets = new float[] { 0.05f, 0.10f, 0.25f, 0.50f, 1.00f, 1.50f, 2.00f, 3.00f, 5.00f };
            foreach (float ip in iPresets)
            {
                float targetI = ip;
                string label = ip < 1.0f
                    ? ip.ToString("0.00", CultureInfo.InvariantCulture) + "A"
                    : ip.ToString("0.0", CultureInfo.InvariantCulture) + "A";
                var btnIp = CreateFlatButton(label, new Size(58, 27), CardDark);
                btnIp.Margin = new Padding(0, 1, 5, 1);
                btnIp.ForeColor = AccentAmber;
                btnIp.Font = new Font("Consolas", 8.5f, FontStyle.Bold);
                btnIp.Click += (s, e) => ApplyCurrentTarget(targetI);
                row6.Controls.Add(btnIp);
            }
            rows.Controls.Add(row6, 0, 6);

            // Row 7: Quick Memory Recall M1-M6
            var row7 = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 4, 0, 0)
            };

            _lblMQuick = new Label
            {
                Text = "Profile pamięci M1–M6 (Alt+1..6):",
                AutoSize = true,
                Margin = new Padding(0, 6, 8, 0),
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };
            row7.Controls.Add(_lblMQuick);

            for (int m = 1; m <= 6; m++)
            {
                int groupIdx = m;
                var btnM = CreateFlatButton("M" + m, new Size(52, 26), Color.FromArgb(30, 58, 138));
                btnM.Margin = new Padding(0, 1, 5, 0);
                btnM.Font = new Font("Consolas", 9f, FontStyle.Bold);
                btnM.Click += (s, e) => RecallPresetGroup(groupIdx);
                row7.Controls.Add(btnM);
            }
            rows.Controls.Add(row7, 0, 7);

            container.Controls.Add(rows);
            return container;
        }

        private Control BuildLowerTabs()
        {
            var tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold)
            };

            _tabChart = new TabPage("📈 Oscyloskop (V / I / P)")
            {
                BackColor = PanelDark,
                Padding = new Padding(6)
            };
            _tabChart.Controls.Add(BuildChartTabContent());

            _tabChargerAndCp = new TabPage("🔋 Ładowarka Aku (1S–7S) / Stała Moc (CP) / Diagnostyka")
            {
                BackColor = PanelDark,
                Padding = new Padding(8)
            };
            _tabChargerAndCp.Controls.Add(BuildChargerAndCpTabContent());

            _tabProtAndPresets = new TabPage("🛡️ Zabezpieczenia i Profile M1–M6")
            {
                BackColor = PanelDark,
                Padding = new Padding(8)
            };
            _tabProtAndPresets.Controls.Add(BuildProtectionsAndPresetsTabContent());

            _tabSequence = new TabPage("🔄 Sekwencje / Rampa")
            {
                BackColor = PanelDark,
                Padding = new Padding(8)
            };
            _tabSequence.Controls.Add(BuildSequenceTabContent());

            _tabProtocol = new TabPage("🔌 Monitor HEX / ATEasy")
            {
                BackColor = PanelDark,
                Padding = new Padding(8)
            };
            _tabProtocol.Controls.Add(BuildProtocolTabContent());

            tabs.TabPages.Add(_tabChart);
            tabs.TabPages.Add(_tabChargerAndCp);
            tabs.TabPages.Add(_tabProtAndPresets);
            tabs.TabPages.Add(_tabSequence);
            tabs.TabPages.Add(_tabProtocol);

            return tabs;
        }

        private Control BuildChargerAndCpTabContent()
        {
            var split = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));

            // ================= LEFT: BATTERY CHARGER (1S - 7S) =================
            var leftCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardDark,
                Margin = new Padding(0, 0, 6, 0),
                Padding = new Padding(12, 10, 12, 10),
                AutoScroll = true
            };

            var chgLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2
            };
            chgLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 225f));
            chgLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            _lblChgSectionTitle = new Label
            {
                Text = "🔋 ŁADOWARKA AKUMULATORÓW (1S–7S CC/CV + AUTO-STOP)",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8),
                ForeColor = AccentCyan,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            chgLayout.Controls.Add(_lblChgSectionTitle, 0, 0);
            chgLayout.SetColumnSpan(_lblChgSectionTitle, 2);

            _lblChgChemTitle = new Label
            {
                Text = "Typ ogniwa (Chemia):",
                AutoSize = true,
                Margin = new Padding(0, 7, 6, 4),
                ForeColor = TextLight
            };
            chgLayout.Controls.Add(_lblChgChemTitle, 0, 1);

            _cboBattChem = new ComboBox
            {
                Width = 340,
                Margin = new Padding(0, 3, 0, 4),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = PanelDark,
                ForeColor = AccentCyan,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            _cboBattChem.Items.AddRange(new object[]
            {
                "Li-Ion / Li-Po 3.7V (Ładowanie 4.20V / ogniwo)",
                "Li-Ion HV 3.8V (Ładowanie 4.35V / ogniwo)",
                "LiFePO4 3.2V (Ładowanie 3.65V / ogniwo)",
                "Kwasowy Pb / AGM 2.0V (Ładowanie 2.40V / ogniwo)",
                "NiMH / NiCd 1.2V (Ładowanie 1.45V / ogniwo)"
            });
            _cboBattChem.SelectedIndex = 0;
            _cboBattChem.SelectedIndexChanged += (s, e) => RecalculateBatteryTargetVoltage();
            chgLayout.Controls.Add(_cboBattChem, 1, 1);

            _lblChgCellsTitle = new Label
            {
                Text = "Liczba ogniw szeregowo (S):",
                AutoSize = true,
                Margin = new Padding(0, 9, 6, 4),
                ForeColor = TextLight
            };
            chgLayout.Controls.Add(_lblChgCellsTitle, 0, 2);

            var cellsFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 3, 0, 4)
            };
            for (int c = 1; c <= 7; c++)
            {
                int cellCount = c;
                var btnC = CreateFlatButton(c + "S", new Size(45, 28), PanelDark);
                btnC.Margin = new Padding(0, 0, 5, 0);
                btnC.Font = new Font("Consolas", 9.5f, FontStyle.Bold);
                btnC.Click += (s, e) => SelectBatteryCellCount(cellCount);
                _btnCells[c - 1] = btnC;
                cellsFlow.Controls.Add(btnC);
            }
            chgLayout.Controls.Add(cellsFlow, 1, 2);

            // Params grid (2x2 for compact vertical fit)
            var paramsGrid = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 4,
                Margin = new Padding(0, 4, 0, 4)
            };
            paramsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 185f));
            paramsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100f));
            paramsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 185f));
            paramsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100f));

            _lblChgVTitle = new Label
            {
                Text = "Napięcie końcowe [V]:",
                AutoSize = true,
                Margin = new Padding(0, 7, 4, 4),
                ForeColor = TextLight
            };
            paramsGrid.Controls.Add(_lblChgVTitle, 0, 0);

            _nudChgVoltage = new NumericUpDown
            {
                Width = 90,
                DecimalPlaces = 2,
                Minimum = 1.00m,
                Maximum = 30.00m,
                Increment = 0.05m,
                Value = 12.60m,
                BackColor = PanelDark,
                ForeColor = AccentCyan,
                Font = new Font("Consolas", 10f, FontStyle.Bold)
            };
            paramsGrid.Controls.Add(_nudChgVoltage, 1, 0);

            _lblChgITitle = new Label
            {
                Text = "Prąd ładowania CC [A]:",
                AutoSize = true,
                Margin = new Padding(8, 7, 4, 4),
                ForeColor = TextLight
            };
            paramsGrid.Controls.Add(_lblChgITitle, 2, 0);

            _nudChgCurrent = new NumericUpDown
            {
                Width = 90,
                DecimalPlaces = 3,
                Minimum = 0.050m,
                Maximum = 5.000m,
                Increment = 0.100m,
                Value = 1.000m,
                BackColor = PanelDark,
                ForeColor = AccentAmber,
                Font = new Font("Consolas", 10f, FontStyle.Bold)
            };
            paramsGrid.Controls.Add(_nudChgCurrent, 3, 0);

            _lblChgCutoffTitle = new Label
            {
                Text = "Prąd odcięcia CV [A]:",
                AutoSize = true,
                Margin = new Padding(0, 7, 4, 4),
                ForeColor = TextLight
            };
            paramsGrid.Controls.Add(_lblChgCutoffTitle, 0, 1);

            _nudChgCutoffCurrent = new NumericUpDown
            {
                Width = 90,
                DecimalPlaces = 3,
                Minimum = 0.010m,
                Maximum = 2.000m,
                Increment = 0.010m,
                Value = 0.050m,
                BackColor = PanelDark,
                ForeColor = AccentGreen,
                Font = new Font("Consolas", 10f, FontStyle.Bold)
            };
            paramsGrid.Controls.Add(_nudChgCutoffCurrent, 1, 1);

            _lblChgMaxMahTitle = new Label
            {
                Text = "Limit mAh (0=Brak):",
                AutoSize = true,
                Margin = new Padding(8, 7, 4, 4),
                ForeColor = TextLight
            };
            paramsGrid.Controls.Add(_lblChgMaxMahTitle, 2, 1);

            _nudChgMaxMah = new NumericUpDown
            {
                Width = 90,
                DecimalPlaces = 0,
                Minimum = 0m,
                Maximum = 50000m,
                Increment = 100m,
                Value = 0m,
                BackColor = PanelDark,
                ForeColor = TextLight,
                Font = new Font("Consolas", 10f, FontStyle.Bold)
            };
            paramsGrid.Controls.Add(_nudChgMaxMah, 3, 1);

            chgLayout.Controls.Add(paramsGrid, 0, 3);
            chgLayout.SetColumnSpan(paramsGrid, 2);

            var chgBtnFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 4, 0, 6)
            };

            _btnChgStart = CreateFlatButton("▶ START ŁADOWANIA (CC/CV)", new Size(235, 32), AccentGreen);
            _btnChgStart.Margin = new Padding(0, 0, 8, 0);
            _btnChgStart.Click += (s, e) => StartBatteryCharger();
            chgBtnFlow.Controls.Add(_btnChgStart);

            _btnChgStop = CreateFlatButton("⏹ ZATRZYMAJ ŁADOWANIE", new Size(195, 32), Color.FromArgb(127, 29, 29));
            _btnChgStop.Enabled = false;
            _btnChgStop.Click += (s, e) => StopBatteryCharger(false, "");
            chgBtnFlow.Controls.Add(_btnChgStop);

            chgLayout.Controls.Add(chgBtnFlow, 0, 4);
            chgLayout.SetColumnSpan(chgBtnFlow, 2);

            _lblChgStatusBanner = new Label
            {
                Text = "Status ładowarki: GOTOWY (Wybierz 1S–7S i kliknij START)",
                AutoSize = true,
                Margin = new Padding(0, 2, 0, 4),
                ForeColor = AccentAmber,
                Font = new Font("Consolas", 9.5f, FontStyle.Bold)
            };
            chgLayout.Controls.Add(_lblChgStatusBanner, 0, 5);
            chgLayout.SetColumnSpan(_lblChgStatusBanner, 2);

            _lblChgLiveMetrics = new Label
            {
                Text = "Ogniwo: 0.000 V/ogn. | Wtłoczono: 0 mAh (0.000 Wh) | Czas: 00:00",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 2),
                ForeColor = TextLight,
                Font = new Font("Consolas", 9.0f, FontStyle.Regular)
            };
            chgLayout.Controls.Add(_lblChgLiveMetrics, 0, 6);
            chgLayout.SetColumnSpan(_lblChgLiveMetrics, 2);

            leftCard.Controls.Add(chgLayout);

            // ================= RIGHT: CONSTANT POWER (CP) & FULL DIAGNOSTICS =================
            var rightCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardDark,
                Margin = new Padding(6, 0, 0, 0),
                Padding = new Padding(12, 10, 12, 10),
                AutoScroll = true
            };

            var rightLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1
            };

            _lblCpSectionTitle = new Label
            {
                Text = "⚡ TRYB STAŁEJ MOCY WYJŚCIOWEJ (CP — CONSTANT POWER)",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6),
                ForeColor = AccentMagenta,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            rightLayout.Controls.Add(_lblCpSectionTitle, 0, 0);

            var cpGrid = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 6,
                Margin = new Padding(0, 0, 0, 4)
            };

            _lblCpWattsTitle = new Label
            {
                Text = "Moc CP [W]:",
                AutoSize = true,
                Margin = new Padding(0, 6, 4, 2),
                ForeColor = TextLight
            };
            cpGrid.Controls.Add(_lblCpWattsTitle, 0, 0);

            _nudCpWatts = new NumericUpDown
            {
                Width = 78,
                DecimalPlaces = 1,
                Minimum = 0.5m,
                Maximum = 150.0m,
                Increment = 1.0m,
                Value = 15.0m,
                BackColor = PanelDark,
                ForeColor = AccentMagenta,
                Font = new Font("Consolas", 9.5f, FontStyle.Bold)
            };
            cpGrid.Controls.Add(_nudCpWatts, 1, 0);

            _lblCpMaxVTitle = new Label
            {
                Text = "Max U [V]:",
                AutoSize = true,
                Margin = new Padding(8, 6, 4, 2),
                ForeColor = TextLight
            };
            cpGrid.Controls.Add(_lblCpMaxVTitle, 2, 0);

            _nudCpMaxV = new NumericUpDown
            {
                Width = 74,
                DecimalPlaces = 2,
                Minimum = 1.00m,
                Maximum = 30.00m,
                Increment = 0.50m,
                Value = 24.00m,
                BackColor = PanelDark,
                ForeColor = AccentCyan,
                Font = new Font("Consolas", 9.5f, FontStyle.Bold)
            };
            cpGrid.Controls.Add(_nudCpMaxV, 3, 0);

            _lblCpMaxITitle = new Label
            {
                Text = "Max I [A]:",
                AutoSize = true,
                Margin = new Padding(8, 6, 4, 2),
                ForeColor = TextLight
            };
            cpGrid.Controls.Add(_lblCpMaxITitle, 4, 0);

            _nudCpMaxI = new NumericUpDown
            {
                Width = 74,
                DecimalPlaces = 3,
                Minimum = 0.100m,
                Maximum = 5.000m,
                Increment = 0.100m,
                Value = 3.000m,
                BackColor = PanelDark,
                ForeColor = AccentAmber,
                Font = new Font("Consolas", 9.5f, FontStyle.Bold)
            };
            cpGrid.Controls.Add(_nudCpMaxI, 5, 0);

            rightLayout.Controls.Add(cpGrid, 0, 1);

            var cpBtnFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 2, 0, 4)
            };

            _btnCpStart = CreateFlatButton("▶ Włącz Stałą Moc (CP)", new Size(195, 29), Color.FromArgb(190, 24, 93));
            _btnCpStart.Margin = new Padding(0, 0, 8, 0);
            _btnCpStart.Click += (s, e) => StartConstantPowerMode();
            cpBtnFlow.Controls.Add(_btnCpStart);

            _btnCpStop = CreateFlatButton("⏹ Wyłącz CP", new Size(125, 29), Color.FromArgb(127, 29, 29));
            _btnCpStop.Enabled = false;
            _btnCpStop.Click += (s, e) => StopConstantPowerMode();
            cpBtnFlow.Controls.Add(_btnCpStop);

            _lblCpStatus = new Label
            {
                Text = "CP: Wyłączony",
                AutoSize = true,
                Margin = new Padding(8, 6, 0, 0),
                ForeColor = TextMuted,
                Font = new Font("Consolas", 9f, FontStyle.Bold)
            };
            cpBtnFlow.Controls.Add(_lblCpStatus);

            rightLayout.Controls.Add(cpBtnFlow, 0, 2);

            // Hardware Diagnostics & Ripple / Headroom Section
            var diagHeaderFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 8, 0, 4)
            };

            _lblDiagSectionTitle = new Label
            {
                Text = "🔍 PEŁNA DIAGNOSTYKA SPRZĘTOWA, TĘTNIENIA I ZAPAS ΔU:",
                AutoSize = true,
                Margin = new Padding(0, 5, 10, 0),
                ForeColor = AccentCyan,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            diagHeaderFlow.Controls.Add(_lblDiagSectionTitle);

            _btnDiagRefresh = CreateFlatButton("🔄 Odśwież", new Size(92, 25), PanelDark);
            _btnDiagRefresh.Click += (s, e) => _driver.RefreshAllState();
            diagHeaderFlow.Controls.Add(_btnDiagRefresh);

            rightLayout.Controls.Add(diagHeaderFlow, 0, 3);

            var diagBox = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                BackColor = BgDark,
                Padding = new Padding(10, 8, 10, 8),
                BorderStyle = BorderStyle.FixedSingle
            };

            _lblDiagDetails = new Label
            {
                AutoSize = true,
                ForeColor = TextLight,
                Font = new Font("Consolas", 9.0f, FontStyle.Regular),
                Text =
                    "• Identyfikacja (0xDE–0xE0) : DPS-150 | HW: V1.0 | FW: V1.2\r\n" +
                    "• Limity wejściowe (0xE1/E2): Max U = 30.00 V | Max I = 5.000 A\r\n" +
                    "• Zasilanie wejściowe (Vin) : 0.00 V (Próg odcięcia LVP: 4.50 V)\r\n" +
                    "• Spadek / Zapas (Vin-Vout) : ΔU = +0.00 V (Oczekiwanie)\r\n" +
                    "• Tętnienia wyjścia (Ripple): U_pp = 0.0 mVpp | I_pp = 0.0 mApp\r\n" +
                    "• Licznik energii (0xD8)    : AKTYWNY (ON) | Czas pracy: 00:00:00"
            };
            diagBox.Controls.Add(_lblDiagDetails);
            rightLayout.Controls.Add(diagBox, 0, 4);

            rightCard.Controls.Add(rightLayout);

            split.Controls.Add(leftCard, 0, 0);
            split.Controls.Add(rightCard, 1, 0);

            _chgAndCpTimer = new System.Windows.Forms.Timer();
            _chgAndCpTimer.Interval = 250;
            _chgAndCpTimer.Tick += OnChargerAndCpTimerTick;
            _chgAndCpTimer.Start();

            SelectBatteryCellCount(3); // Highlight 3S (12.60V Li-Ion) by default

            return split;
        }

        private Control BuildChartTabContent()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = CardDark,
                Padding = new Padding(8, 4, 8, 4)
            };

            _btnPauseChart = CreateFlatButton("⏸ ZATRZYMAJ PRZEBIEG (PAUZA) [P]", new Size(235, 28), Color.FromArgb(180, 83, 9));
            _btnPauseChart.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            _btnPauseChart.FlatAppearance.BorderColor = AccentAmber;
            _btnPauseChart.Margin = new Padding(0, 1, 10, 0);
            toolbar.Controls.Add(_btnPauseChart);

            _chkShowV = new CheckBox
            {
                Text = "Napięcie Vout",
                Checked = true,
                AutoSize = true,
                Margin = new Padding(4, 5, 10, 0),
                ForeColor = AccentCyan
            };
            toolbar.Controls.Add(_chkShowV);

            _chkShowI = new CheckBox
            {
                Text = "Prąd Iout",
                Checked = true,
                AutoSize = true,
                Margin = new Padding(4, 5, 10, 0),
                ForeColor = AccentAmber
            };
            toolbar.Controls.Add(_chkShowI);

            _chkShowP = new CheckBox
            {
                Text = "Moc Pout",
                Checked = true,
                AutoSize = true,
                Margin = new Padding(4, 5, 10, 0),
                ForeColor = AccentMagenta
            };
            toolbar.Controls.Add(_chkShowP);

            _chkAutoScale = new CheckBox
            {
                Text = "Auto-skala Y",
                Checked = true,
                AutoSize = true,
                Margin = new Padding(4, 5, 10, 0),
                ForeColor = TextLight
            };
            toolbar.Controls.Add(_chkAutoScale);

            int[] windows = new int[] { 10, 30, 60, 300 };
            string[] winLabels = new string[] { "10s", "30s", "1m", "5m" };
            for (int i = 0; i < windows.Length; i++)
            {
                int sec = windows[i];
                var btnWin = CreateFlatButton(winLabels[i], new Size(46, 27), PanelDark);
                btnWin.Margin = new Padding(2, 1, 2, 0);
                btnWin.Click += (s, e) => { _chart.TimeWindowSeconds = sec; };
                toolbar.Controls.Add(btnWin);
            }

            _btnClearChart = CreateFlatButton("🗑 Wyczyść", new Size(92, 27), PanelDark);
            _btnClearChart.Margin = new Padding(8, 1, 4, 0);
            _btnClearChart.Click += (s, e) => _chart.ClearSamples();
            toolbar.Controls.Add(_btnClearChart);

            _btnExportCsv = CreateFlatButton("💾 Eksportuj CSV...", new Size(138, 27), Color.FromArgb(2, 132, 199));
            _btnExportCsv.Margin = new Padding(2, 1, 4, 0);
            _btnExportCsv.Click += (s, e) => ExportChartToCsv();
            toolbar.Controls.Add(_btnExportCsv);

            _chart = new LiveChartControl
            {
                Dock = DockStyle.Fill
            };
            _chart.OnViewportChanged += () => UpdatePauseButtonUi();

            layout.Controls.Add(toolbar, 0, 0);
            layout.Controls.Add(_chart, 0, 1);
            return layout;
        }

        private Control BuildProtectionsAndPresetsTabContent()
        {
            var split = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 53f));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 47f));

            // Left: Memory Presets M1-M6
            var grpPresets = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardDark,
                Margin = new Padding(0, 0, 6, 0),
                Padding = new Padding(12),
                AutoScroll = true
            };

            var presetTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 5,
                RowCount = 8
            };
            presetTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
            presetTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118f));
            presetTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118f));
            presetTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120f));
            presetTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120f));

            _lblPresetSectionTitle = new Label
            {
                Text = "PROFILE PAMIĘCI ZASILACZA (M1 – M6) W EEPROM",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8),
                ForeColor = AccentCyan,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            presetTable.Controls.Add(_lblPresetSectionTitle, 0, 0);
            presetTable.SetColumnSpan(_lblPresetSectionTitle, 5);

            float[] defaultV = { 3.30f, 5.00f, 9.00f, 12.00f, 19.00f, 24.00f };
            float[] defaultI = { 1.000f, 2.000f, 2.000f, 3.000f, 4.000f, 5.000f };

            for (int i = 0; i < 6; i++)
            {
                int rowIdx = i + 1;
                int groupNum = i + 1;

                _lblPresetM[i] = new Label
                {
                    Text = string.Format("Profil M{0}:", groupNum),
                    AutoSize = true,
                    Margin = new Padding(0, 8, 4, 4),
                    ForeColor = TextLight,
                    Font = new Font("Consolas", 10f, FontStyle.Bold)
                };
                presetTable.Controls.Add(_lblPresetM[i], 0, rowIdx);

                _nudPresetV[i] = new NumericUpDown
                {
                    Width = 104,
                    Margin = new Padding(2, 4, 6, 4),
                    DecimalPlaces = 2,
                    Minimum = 0m,
                    Maximum = 30m,
                    Increment = 0.1m,
                    Value = (decimal)defaultV[i],
                    BackColor = PanelDark,
                    ForeColor = AccentCyan,
                    Font = new Font("Consolas", 10f, FontStyle.Bold)
                };
                presetTable.Controls.Add(_nudPresetV[i], 1, rowIdx);

                _nudPresetI[i] = new NumericUpDown
                {
                    Width = 104,
                    Margin = new Padding(2, 4, 6, 4),
                    DecimalPlaces = 3,
                    Minimum = 0m,
                    Maximum = 5m,
                    Increment = 0.05m,
                    Value = (decimal)defaultI[i],
                    BackColor = PanelDark,
                    ForeColor = AccentAmber,
                    Font = new Font("Consolas", 10f, FontStyle.Bold)
                };
                presetTable.Controls.Add(_nudPresetI[i], 2, rowIdx);

                int idxCapture = i;
                _btnPresetUse[i] = CreateFlatButton("⚡ Użyj [V/A]", new Size(112, 28), Color.FromArgb(2, 132, 199));
                _btnPresetUse[i].Margin = new Padding(2, 3, 4, 3);
                _btnPresetUse[i].Click += (s, e) =>
                {
                    ApplyVoltageTarget((float)_nudPresetV[idxCapture].Value);
                    ApplyCurrentTarget((float)_nudPresetI[idxCapture].Value);
                };
                presetTable.Controls.Add(_btnPresetUse[i], 3, rowIdx);

                _btnPresetSave[i] = CreateFlatButton("💾 Zapisz M" + groupNum, new Size(112, 28), PanelDark);
                _btnPresetSave[i].Margin = new Padding(2, 3, 2, 3);
                _btnPresetSave[i].Click += (s, e) =>
                {
                    _driver.SaveMemoryPreset(
                        idxCapture + 1,
                        (float)_nudPresetV[idxCapture].Value,
                        (float)_nudPresetI[idxCapture].Value);
                };
                presetTable.Controls.Add(_btnPresetSave[i], 4, rowIdx);
            }

            grpPresets.Controls.Add(presetTable);

            // Right: Hardware Protections (OVP, OCP, OPP, OTP, LVP) + Brightness/Volume
            var grpProt = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardDark,
                Margin = new Padding(6, 0, 0, 0),
                Padding = new Padding(12),
                AutoScroll = true
            };

            var protTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3
            };
            protTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210f));
            protTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135f));
            protTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _lblProtSectionTitle = new Label
            {
                Text = "PROGI ZABEZPIECZEŃ (OVP / OCP / OPP / OTP / LVP)",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8),
                ForeColor = AccentAmber,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            protTable.Controls.Add(_lblProtSectionTitle, 0, 0);
            protTable.SetColumnSpan(_lblProtSectionTitle, 3);

            _nudOvp = AddProtectionRowTable(protTable, 1, "OVP (Max Napięcie Wyj.):", "V", 0.1m, 32.0m, 30.50m, 2, out _lblOvpRow);
            _nudOcp = AddProtectionRowTable(protTable, 2, "OCP (Max Prąd Wyj.):", "A", 0.01m, 5.50m, 5.100m, 3, out _lblOcpRow);
            _nudOpp = AddProtectionRowTable(protTable, 3, "OPP (Max Moc Wyj.):", "W", 0.5m, 160.0m, 152.0m, 1, out _lblOppRow);
            _nudOtp = AddProtectionRowTable(protTable, 4, "OTP (Max Temperatura):", "°C", 30.0m, 99.0m, 85.0m, 1, out _lblOtpRow);
            _nudLvp = AddProtectionRowTable(protTable, 5, "LVP (Min Napięcie Wej.):", "V", 3.0m, 30.0m, 4.50m, 2, out _lblLvpRow);

            var btnRowFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 6, 0, 6)
            };

            _btnApplyProt = CreateFlatButton("🛡️ Wyślij zabezpieczenia do DPS-150", new Size(265, 30), AccentGreen);
            _btnApplyProt.Margin = new Padding(0, 0, 8, 0);
            _btnApplyProt.Click += (s, e) =>
            {
                _driver.SetProtections(
                    (float)_nudOvp.Value,
                    (float)_nudOcp.Value,
                    (float)_nudOpp.Value,
                    (float)_nudOtp.Value,
                    (float)_nudLvp.Value);
            };
            btnRowFlow.Controls.Add(_btnApplyProt);

            _btnSyncFromDevice = CreateFlatButton("🔄 Odczytaj z zasilacza", new Size(175, 30), PanelDark);
            _btnSyncFromDevice.Click += (s, e) =>
            {
                _initialSettingsSynced = false;
                _driver.RefreshAllState();
            };
            btnRowFlow.Controls.Add(_btnSyncFromDevice);

            protTable.Controls.Add(btnRowFlow, 0, 6);
            protTable.SetColumnSpan(btnRowFlow, 3);

            // Brightness row
            _lblBriTitle = new Label
            {
                Text = "Jasność ekranu DPS-150:",
                AutoSize = true,
                Margin = new Padding(0, 6, 0, 0),
                ForeColor = TextMuted
            };
            protTable.Controls.Add(_lblBriTitle, 0, 7);

            _trkBrightness = new TrackBar
            {
                Width = 125,
                AutoSize = false,
                Height = 24,
                Minimum = 1,
                Maximum = 14,
                Value = 10,
                TickStyle = TickStyle.None,
                BackColor = CardDark
            };
            protTable.Controls.Add(_trkBrightness, 1, 7);

            _lblBrightnessVal = new Label
            {
                Text = "10 / 14",
                AutoSize = true,
                Margin = new Padding(4, 6, 0, 0),
                ForeColor = TextLight
            };
            protTable.Controls.Add(_lblBrightnessVal, 2, 7);

            // Volume row
            _lblVolTitle = new Label
            {
                Text = "Głośność dźwięku (Buzzer):",
                AutoSize = true,
                Margin = new Padding(0, 6, 0, 0),
                ForeColor = TextMuted
            };
            protTable.Controls.Add(_lblVolTitle, 0, 8);

            _trkVolume = new TrackBar
            {
                Width = 125,
                AutoSize = false,
                Height = 24,
                Minimum = 0,
                Maximum = 10,
                Value = 5,
                TickStyle = TickStyle.None,
                BackColor = CardDark
            };
            protTable.Controls.Add(_trkVolume, 1, 8);

            _lblVolumeVal = new Label
            {
                Text = "5 / 10",
                AutoSize = true,
                Margin = new Padding(4, 6, 0, 0),
                ForeColor = TextLight
            };
            protTable.Controls.Add(_lblVolumeVal, 2, 8);

            grpProt.Controls.Add(protTable);

            split.Controls.Add(grpPresets, 0, 0);
            split.Controls.Add(grpProt, 1, 0);
            return split;
        }

        private NumericUpDown AddProtectionRowTable(
            TableLayoutPanel table, int rowIdx, string label, string unit,
            decimal min, decimal max, decimal val, int decimals, out Label lblOut)
        {
            lblOut = new Label
            {
                Text = label,
                AutoSize = true,
                Margin = new Padding(0, 7, 4, 4),
                ForeColor = TextLight
            };
            table.Controls.Add(lblOut, 0, rowIdx);

            var nud = new NumericUpDown
            {
                Width = 115,
                Margin = new Padding(2, 3, 4, 3),
                DecimalPlaces = decimals,
                Minimum = min,
                Maximum = max,
                Value = val,
                Increment = decimals >= 2 ? 0.1m : 1.0m,
                BackColor = PanelDark,
                ForeColor = AccentAmber,
                Font = new Font("Consolas", 10f, FontStyle.Bold)
            };
            table.Controls.Add(nud, 1, rowIdx);

            var lblU = new Label
            {
                Text = unit,
                AutoSize = true,
                Margin = new Padding(2, 7, 0, 4),
                ForeColor = TextMuted
            };
            table.Controls.Add(lblU, 2, rowIdx);

            return nud;
        }

        private Control BuildSequenceTabContent()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));

            _gridSeq = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = CardDark,
                ForeColor = Color.Black,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToResizeRows = false
            };
            _gridSeq.Columns.Add("StepVoltage", "Napięcie docelowe [V]");
            _gridSeq.Columns.Add("StepCurrent", "Limit prądu [A]");
            _gridSeq.Columns.Add("StepDurationMs", "Czas kroku [ms]");

            LoadDefaultSequenceTemplate(1);

            var rightPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = CardDark,
                Margin = new Padding(8, 0, 0, 0),
                Padding = new Padding(12),
                AutoScroll = true
            };

            _lblSeqTitle = new Label
            {
                Text = "GENERATOR SEKWENCJI / RAMP",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8),
                ForeColor = AccentCyan,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            rightPanel.Controls.Add(_lblSeqTitle);

            _btnTpl1 = CreateFlatButton("📋 Szablon 1: Schodki 3.3V → 5V → 9V → 12V", new Size(340, 30), PanelDark);
            _btnTpl1.Margin = new Padding(0, 2, 0, 4);
            _btnTpl1.Click += (s, e) => LoadDefaultSequenceTemplate(1);
            rightPanel.Controls.Add(_btnTpl1);

            _btnTpl2 = CreateFlatButton("📈 Szablon 2: Soft-Start 0V → 12V (10 kroków)", new Size(340, 30), PanelDark);
            _btnTpl2.Margin = new Padding(0, 2, 0, 4);
            _btnTpl2.Click += (s, e) => LoadDefaultSequenceTemplate(2);
            rightPanel.Controls.Add(_btnTpl2);

            _btnTpl3 = CreateFlatButton("🔋 Szablon 3: Impulsy obciążeniowe 5V / 12V", new Size(340, 30), PanelDark);
            _btnTpl3.Margin = new Padding(0, 2, 0, 8);
            _btnTpl3.Click += (s, e) => LoadDefaultSequenceTemplate(3);
            rightPanel.Controls.Add(_btnTpl3);

            _chkSeqLoop = new CheckBox
            {
                Text = "Powtarzaj sekwencję w pętli (Loop)",
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 8),
                Checked = false,
                ForeColor = TextLight
            };
            rightPanel.Controls.Add(_chkSeqLoop);

            var actionFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 8)
            };

            _btnSeqStart = CreateFlatButton("▶ Uruchom Sekwencję", new Size(165, 34), AccentGreen);
            _btnSeqStart.Margin = new Padding(0, 0, 8, 0);
            _btnSeqStart.Click += (s, e) => StartSequence();
            actionFlow.Controls.Add(_btnSeqStart);

            _btnSeqStop = CreateFlatButton("⏹ Zatrzymaj", new Size(165, 34), Color.FromArgb(127, 29, 29));
            _btnSeqStop.Enabled = false;
            _btnSeqStop.Click += (s, e) => StopSequence();
            actionFlow.Controls.Add(_btnSeqStop);

            rightPanel.Controls.Add(actionFlow);

            _lblSeqStatus = new Label
            {
                Text = "Status sekwencji: Gotowy",
                AutoSize = true,
                ForeColor = AccentAmber,
                Font = new Font("Consolas", 9.5f, FontStyle.Bold)
            };
            rightPanel.Controls.Add(_lblSeqStatus);

            _seqTimer = new System.Windows.Forms.Timer();
            _seqTimer.Tick += OnSequenceTimerTick;

            layout.Controls.Add(_gridSeq, 0, 0);
            layout.Controls.Add(rightPanel, 1, 0);
            return layout;
        }

        private Control BuildProtocolTabContent()
        {
            var split = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));

            var left = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = CardDark,
                Margin = new Padding(0, 0, 6, 0),
                Padding = new Padding(8)
            };
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var logHeaderFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            _lblLogHeader = new Label
            {
                Text = "MONITOR RAMEK SZEREGOWYCH (TX / RX HEX):",
                AutoSize = true,
                Margin = new Padding(2, 6, 12, 0),
                ForeColor = AccentCyan,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            logHeaderFlow.Controls.Add(_lblLogHeader);

            _chkLogRx = new CheckBox
            {
                Text = "Loguj przychodzące RX",
                AutoSize = true,
                Margin = new Padding(4, 5, 12, 0),
                Checked = false,
                ForeColor = TextMuted
            };
            logHeaderFlow.Controls.Add(_chkLogRx);

            _btnClearLog = CreateFlatButton("Wyczyść", new Size(80, 26), PanelDark);
            logHeaderFlow.Controls.Add(_btnClearLog);

            _lstPacketLog = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = BgDark,
                ForeColor = Color.FromArgb(134, 239, 172),
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.FixedSingle
            };
            _btnClearLog.Click += (s, e) => _lstPacketLog.Items.Clear();

            left.Controls.Add(logHeaderFlow, 0, 0);
            left.Controls.Add(_lstPacketLog, 0, 1);

            var right = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardDark,
                Margin = new Padding(6, 0, 0, 0),
                Padding = new Padding(10)
            };

            var txtAteasyGuide = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                BackColor = BgDark,
                ForeColor = TextLight,
                Font = new Font("Consolas", 9f),
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                Text =
                    "=== FNIRSI DPS-150 PROTOCOL SPEC (ATEASY / SCPI / SERIAL COM) ===\r\n" +
                    "Port: 115200 bps, 8N1, RTS=ON, DTR=ON\r\n" +
                    "Outgoing Frame (PC -> DPS-150):\r\n" +
                    "  [0xF1] [CMD] [TYPE] [LEN] [DATA...] [CHECKSUM]\r\n" +
                    "  Checksum = (TYPE + LEN + Sum(DATA)) MOD 256\r\n\r\n" +
                    "1. Session Connect (Enable USB stream):\r\n" +
                    "   TX: F1 C1 00 01 01 02\r\n" +
                    "2. Session Disconnect (Unlock physical buttons):\r\n" +
                    "   TX: F1 C1 00 01 00 01\r\n" +
                    "3. Output Enable / Disable (0xDB):\r\n" +
                    "   ON : F1 B1 DB 01 01 DD\r\n" +
                    "   OFF: F1 B1 DB 01 00 DC\r\n" +
                    "4. Set Voltage Vset (0xC1, Float32 Little-Endian):\r\n" +
                    "   5.00V (00 00 A0 40): F1 B1 C1 04 00 00 A0 40 A5\r\n" +
                    "   12.0V (00 00 40 41): F1 B1 C1 04 00 00 40 41 46\r\n" +
                    "5. Set Current Iset (0xC2, Float32 Little-Endian):\r\n" +
                    "   1.00A (00 00 80 3F): F1 B1 C2 04 00 00 80 3F 85\r\n" +
                    "6. Enable Hardware Wh/Ah Metering (0xD8):\r\n" +
                    "   ON : F1 B1 D8 01 01 DA\r\n"
            };
            right.Controls.Add(txtAteasyGuide);

            split.Controls.Add(left, 0, 0);
            split.Controls.Add(right, 1, 0);
            return split;
        }

        private void WireEvents()
        {
            _btnRefreshPorts.Click += (s, e) => RefreshPortList();
            _btnConnect.Click += (s, e) => ToggleConnection();
            _btnSimulate.Click += (s, e) => ToggleSimulation();
            _btnLangToggle.Click += (s, e) =>
            {
                _isEnglish = !_isEnglish;
                ApplyLanguage();
            };
            _nudSimLoad.ValueChanged += (s, e) => { _driver.SimulatedLoadOhms = (float)_nudSimLoad.Value; };

            _btnOutputToggle.Click += (s, e) =>
            {
                var snap = _driver.GetStateSnapshot();
                _driver.SetOutputEnabled(!snap.OutputEnabled);
            };

            _btnEmergencyOff.Click += (s, e) =>
            {
                StopSequence();
                StopBatteryCharger(false, "");
                StopConstantPowerMode();
                _driver.SetOutputEnabled(false);
                ApplyVoltageTarget(0.0f);
            };

            _btnResetCounters.Click += (s, e) => _driver.ResetLocalCounters();

            _nudVset.ValueChanged += (s, e) =>
            {
                if (_suppressSliderEvents) return;
                _suppressSliderEvents = true;
                float val = (float)_nudVset.Value;
                _trkVset.Value = Math.Max(0, Math.Min(3000, (int)Math.Round(val * 100f)));
                _suppressSliderEvents = false;
                _driver.SetVoltage(val);
            };

            _trkVset.Scroll += (s, e) =>
            {
                if (_suppressSliderEvents) return;
                float val = _trkVset.Value / 100f;
                _suppressSliderEvents = true;
                _nudVset.Value = (decimal)val;
                _suppressSliderEvents = false;
                if (_chkLiveSliders.Checked)
                {
                    _driver.SetVoltage(val);
                }
            };
            _trkVset.MouseUp += (s, e) =>
            {
                if (!_chkLiveSliders.Checked)
                {
                    _driver.SetVoltage(_trkVset.Value / 100f);
                }
            };

            _nudIset.ValueChanged += (s, e) =>
            {
                if (_suppressSliderEvents) return;
                _suppressSliderEvents = true;
                float val = (float)_nudIset.Value;
                _trkIset.Value = Math.Max(0, Math.Min(5000, (int)Math.Round(val * 1000f)));
                _suppressSliderEvents = false;
                _driver.SetCurrent(val);
            };

            _trkIset.Scroll += (s, e) =>
            {
                if (_suppressSliderEvents) return;
                float val = _trkIset.Value / 1000f;
                _suppressSliderEvents = true;
                _nudIset.Value = (decimal)val;
                _suppressSliderEvents = false;
                if (_chkLiveSliders.Checked)
                {
                    _driver.SetCurrent(val);
                }
            };
            _trkIset.MouseUp += (s, e) =>
            {
                if (!_chkLiveSliders.Checked)
                {
                    _driver.SetCurrent(_trkIset.Value / 1000f);
                }
            };

            _chkShowV.CheckedChanged += (s, e) => { _chart.ShowVoltage = _chkShowV.Checked; _chart.Invalidate(); };
            _chkShowI.CheckedChanged += (s, e) => { _chart.ShowCurrent = _chkShowI.Checked; _chart.Invalidate(); };
            _chkShowP.CheckedChanged += (s, e) => { _chart.ShowPower = _chkShowP.Checked; _chart.Invalidate(); };
            _chkAutoScale.CheckedChanged += (s, e) => { _chart.AutoScaleY = _chkAutoScale.Checked; _chart.Invalidate(); };

            _btnPauseChart.Click += (s, e) => ToggleChartPause();

            _trkBrightness.Scroll += (s, e) =>
            {
                byte b = (byte)_trkBrightness.Value;
                _lblBrightnessVal.Text = b + " / 14";
                _driver.SetBrightness(b);
            };
            _trkVolume.Scroll += (s, e) =>
            {
                byte v = (byte)_trkVolume.Value;
                _lblVolumeVal.Text = v + " / 10";
                _driver.SetVolume(v);
            };

            _driver.OnTelemetryUpdated += OnDriverTelemetry;
            _driver.OnConnectionChanged += OnDriverConnectionChanged;
            _driver.OnConnectionError += (msg) =>
            {
                if (this.IsHandleCreated)
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        MessageBox.Show(this, msg, _isEnglish ? "DPS-150 Connection Error" : "Błąd połączenia z DPS-150", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    });
                }
            };
            _driver.OnPacketLog += (hex, isTx) =>
            {
                if (!isTx && (_chkLogRx == null || !_chkLogRx.Checked)) return;
                if (this.IsHandleCreated)
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        string line = string.Format("[{0:HH:mm:ss.fff}] {1}: {2}", DateTime.Now, isTx ? "TX ->" : "RX <-", hex);
                        _lstPacketLog.Items.Add(line);
                        if (_lstPacketLog.Items.Count > 300)
                            _lstPacketLog.Items.RemoveAt(0);
                        _lstPacketLog.TopIndex = _lstPacketLog.Items.Count - 1;
                    });
                }
            };
        }

        private void ApplyLanguage()
        {
            bool en = _isEnglish;
            _btnLangToggle.Text = en ? "🌐 EN ▸ PL" : "🌐 PL ▸ EN";

            _lblPortTitle.Text = en ? "USB Port:" : "Port USB:";
            _btnRefreshPorts.Text = en ? "⟳ Refresh" : "⟳ Odśwież";
            _lblSimLoadTitle.Text = en ? "Sim Load [Ω]:" : "Obciążenie [Ω]:";

            _lblCardTitleV.Text = en ? "OUTPUT VOLTAGE" : "NAPIĘCIE WYJŚCIOWE";
            _lblCardTitleI.Text = en ? "OUTPUT CURRENT" : "PRĄD WYJŚCIOWY";
            _lblCardTitleP.Text = en ? "OUTPUT POWER" : "MOC WYJŚCIOWA";

            _lblStatTitleMode.Text = en ? "OPERATING MODE" : "TRYB PRACY";
            _lblStatTitleProt.Text = en ? "PROT. / LIMITS" : "ZABEZP. / LIMITY";
            _lblStatTitleVin.Text = en ? "VIN / HEADROOM ΔU" : "WEJŚCIE Vin / ΔU";
            _lblStatTitleRes.Text = en ? "LOAD R / RIPPLE" : "OBC. Ω / TĘTNIENIA";
            _lblStatTitleCap.Text = en ? "CAPACITY (Ah)" : "POJEMNOŚĆ (Ah)";
            _lblStatTitleEne.Text = en ? "ENERGY (Wh)" : "ENERGIA (Wh)";
            _lblStatTitleTemp.Text = en ? "TEMP / RUNTIME" : "TEMP. / CZAS ON";
            _btnResetCounters.Text = en ? "↺ Reset Ah/Wh" : "↺ Zeruj Ah/Wh";

            _lblVHeader.Text = en ? "VOLTAGE SETPOINT (Vset) [0 – 30 V]:" : "NASTAWA NAPIĘCIA (Vset) [0 – 30 V]:";
            _chkLiveSliders.Text = en ? "Live update from sliders" : "Aktualizuj na żywo z suwaków";
            _lblIHeader.Text = en ? "CURRENT LIMIT (Iset) [0.000 – 5.000 A]:" : "LIMIT PRĄDU (Iset) [0.000 – 5.000 A]:";
            _lblMQuick.Text = en ? "Memory Presets M1–M6 (Alt+1..6):" : "Profile pamięci M1–M6 (Alt+1..6):";

            _tabChart.Text = en ? "📈 Live Oscilloscope (V / I / P)" : "📈 Oscyloskop (V / I / P)";
            _tabChargerAndCp.Text = en
                ? "🔋 Battery Charger (1S–7S) / Const Power (CP) / Diagnostics"
                : "🔋 Ładowarka Aku (1S–7S) / Stała Moc (CP) / Diagnostyka";
            _tabProtAndPresets.Text = en ? "🛡️ Protections & Presets M1–M6" : "🛡️ Zabezpieczenia i Profile M1–M6";
            _tabSequence.Text = en ? "🔄 Sequences / Voltage Ramp" : "🔄 Sekwencje / Rampa";
            _tabProtocol.Text = en ? "🔌 HEX Monitor & ATEasy" : "🔌 Monitor HEX / ATEasy";

            if (_lblChgSectionTitle != null)
            {
                _lblChgSectionTitle.Text = en
                    ? "🔋 BATTERY CHARGER (1S–7S CC/CV + AUTO-CUTOFF)"
                    : "🔋 ŁADOWARKA AKUMULATORÓW (1S–7S CC/CV + AUTO-STOP)";
                _lblChgChemTitle.Text = en ? "Battery Chemistry:" : "Typ ogniwa (Chemia):";
                int selChem = _cboBattChem.SelectedIndex;
                _cboBattChem.Items[0] = en ? "Li-Ion / Li-Po 3.7V (Charge 4.20V / cell)" : "Li-Ion / Li-Po 3.7V (Ładowanie 4.20V / ogniwo)";
                _cboBattChem.Items[1] = en ? "Li-Ion HV 3.8V (Charge 4.35V / cell)" : "Li-Ion HV 3.8V (Ładowanie 4.35V / ogniwo)";
                _cboBattChem.Items[2] = en ? "LiFePO4 3.2V (Charge 3.65V / cell)" : "LiFePO4 3.2V (Ładowanie 3.65V / ogniwo)";
                _cboBattChem.Items[3] = en ? "Lead-Acid Pb / AGM 2.0V (2.40V / cell)" : "Kwasowy Pb / AGM 2.0V (Ładowanie 2.40V / ogniwo)";
                _cboBattChem.Items[4] = en ? "NiMH / NiCd 1.2V (Charge 1.45V / cell)" : "NiMH / NiCd 1.2V (Ładowanie 1.45V / ogniwo)";
                if (selChem >= 0) _cboBattChem.SelectedIndex = selChem;

                _lblChgCellsTitle.Text = en ? "Series Cell Count (1S–7S):" : "Liczba ogniw szeregowo (S):";
                _lblChgVTitle.Text = en ? "Charge Voltage [V]:" : "Napięcie końcowe [V]:";
                _lblChgITitle.Text = en ? "Charge Current CC [A]:" : "Prąd ładowania CC [A]:";
                _lblChgCutoffTitle.Text = en ? "CV Cutoff Current [A]:" : "Prąd odcięcia CV [A]:";
                _lblChgMaxMahTitle.Text = en ? "Max Capacity [mAh] (0=Off):" : "Limit mAh (0=Brak):";
                _btnChgStart.Text = en ? "▶ START CHARGING (CC/CV)" : "▶ START ŁADOWANIA (CC/CV)";
                _btnChgStop.Text = en ? "⏹ STOP CHARGING" : "⏹ ZATRZYMAJ ŁADOWANIE";
                if (!_chargerActive)
                {
                    _lblChgStatusBanner.Text = en
                        ? "Charger status: READY (Select 1S–7S and click START)"
                        : "Status ładowarki: GOTOWY (Wybierz 1S–7S i kliknij START)";
                }

                _lblCpSectionTitle.Text = en
                    ? "⚡ CONSTANT POWER MODE (CP — CLOSED-LOOP REGULATION)"
                    : "⚡ TRYB STAŁEJ MOCY WYJŚCIOWEJ (CP — CONSTANT POWER)";
                _lblCpWattsTitle.Text = en ? "Target P [W]:" : "Moc CP [W]:";
                _lblCpMaxVTitle.Text = "Max U [V]:";
                _lblCpMaxITitle.Text = "Max I [A]:";
                _btnCpStart.Text = en ? "▶ Enable Constant Power (CP)" : "▶ Włącz Stałą Moc (CP)";
                _btnCpStop.Text = en ? "⏹ Disable CP" : "⏹ Wyłącz CP";
                if (!_cpModeActive)
                {
                    _lblCpStatus.Text = en ? "CP: Disabled" : "CP: Wyłączony";
                }

                _lblDiagSectionTitle.Text = en
                    ? "🔍 HARDWARE DIAGNOSTICS, RIPPLE & HEADROOM ΔU:"
                    : "🔍 PEŁNA DIAGNOSTYKA SPRZĘTOWA, TĘTNIENIA I ZAPAS ΔU:";
                _btnDiagRefresh.Text = en ? "🔄 Refresh" : "🔄 Odśwież";
            }

            _chkShowV.Text = en ? "Voltage Vout" : "Napięcie Vout";
            _chkShowI.Text = en ? "Current Iout" : "Prąd Iout";
            _chkShowP.Text = en ? "Power Pout" : "Moc Pout";
            _chkAutoScale.Text = en ? "Auto-scale Y" : "Auto-skala Y";
            UpdatePauseButtonUi();
            _btnClearChart.Text = en ? "🗑 Clear" : "🗑 Wyczyść";
            _btnExportCsv.Text = en ? "💾 Export CSV..." : "💾 Eksportuj CSV...";

            _lblPresetSectionTitle.Text = en ? "DEVICE MEMORY PRESETS (M1 – M6) IN EEPROM" : "PROFILE PAMIĘCI ZASILACZA (M1 – M6) W EEPROM";
            for (int i = 0; i < 6; i++)
            {
                int g = i + 1;
                _lblPresetM[i].Text = en ? ("Preset M" + g + ":") : ("Profil M" + g + ":");
                _btnPresetUse[i].Text = en ? "⚡ Load [V/A]" : "⚡ Użyj [V/A]";
                _btnPresetSave[i].Text = en ? ("💾 Save M" + g) : ("💾 Zapisz M" + g);
            }

            _lblProtSectionTitle.Text = en ? "HARDWARE PROTECTION THRESHOLDS (OVP / OCP / OPP / OTP / LVP)" : "PROGI ZABEZPIECZEŃ (OVP / OCP / OPP / OTP / LVP)";
            _lblOvpRow.Text = en ? "OVP (Max Output Voltage):" : "OVP (Max Napięcie Wyj.):";
            _lblOcpRow.Text = en ? "OCP (Max Output Current):" : "OCP (Max Prąd Wyj.):";
            _lblOppRow.Text = en ? "OPP (Max Output Power):" : "OPP (Max Moc Wyj.):";
            _lblOtpRow.Text = en ? "OTP (Max Temperature):" : "OTP (Max Temperatura):";
            _lblLvpRow.Text = en ? "LVP (Min Input Voltage):" : "LVP (Min Napięcie Wej.):";
            _btnApplyProt.Text = en ? "🛡️ Send Protections to DPS-150" : "🛡️ Wyślij zabezpieczenia do DPS-150";
            _btnSyncFromDevice.Text = en ? "🔄 Read from Device" : "🔄 Odczytaj z zasilacza";
            _lblBriTitle.Text = en ? "DPS-150 Display Brightness:" : "Jasność ekranu DPS-150:";
            _lblVolTitle.Text = en ? "Buzzer Sound Volume:" : "Głośność dźwięku (Buzzer):";

            if (_gridSeq.Columns.Count >= 3)
            {
                _gridSeq.Columns[0].HeaderText = en ? "Target Voltage [V]" : "Napięcie docelowe [V]";
                _gridSeq.Columns[1].HeaderText = en ? "Current Limit [A]" : "Limit prądu [A]";
                _gridSeq.Columns[2].HeaderText = en ? "Step Duration [ms]" : "Czas kroku [ms]";
            }
            _lblSeqTitle.Text = en ? "SEQUENCE / RAMP GENERATOR" : "GENERATOR SEKWENCJI / RAMP";
            _btnTpl1.Text = en ? "📋 Template 1: Steps 3.3V → 5V → 9V → 12V" : "📋 Szablon 1: Schodki 3.3V → 5V → 9V → 12V";
            _btnTpl2.Text = en ? "📈 Template 2: Soft-Start 0V → 12V (10 steps)" : "📈 Szablon 2: Soft-Start 0V → 12V (10 kroków)";
            _btnTpl3.Text = en ? "🔋 Template 3: Load Pulses 5V / 12V" : "🔋 Szablon 3: Impulsy obciążeniowe 5V / 12V";
            _chkSeqLoop.Text = en ? "Repeat sequence in loop" : "Powtarzaj sekwencję w pętli (Loop)";
            _btnSeqStart.Text = en ? "▶ Start Sequence" : "▶ Uruchom Sekwencję";
            _btnSeqStop.Text = en ? "⏹ Stop" : "⏹ Zatrzymaj";
            if (_seqCurrentStep < 0)
                _lblSeqStatus.Text = en ? "Sequence status: Ready" : "Status sekwencji: Gotowy";

            _lblLogHeader.Text = en ? "SERIAL PACKET MONITOR (TX / RX HEX):" : "MONITOR RAMEK SZEREGOWYCH (TX / RX HEX):";
            _chkLogRx.Text = en ? "Log incoming RX packets" : "Loguj przychodzące RX";
            _btnClearLog.Text = en ? "Clear" : "Wyczyść";

            _chart.IsEnglish = en;
            _chart.Invalidate();
            if (_footerPanel != null) _footerPanel.Invalidate();

            // Refresh connection and dashboard labels
            OnDriverConnectionChanged(_driver.IsConnected, _lblDeviceInfo.Text);
            UpdateDashboardUi(_driver.GetStateSnapshot());
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Escape)
            {
                StopSequence();
                StopBatteryCharger(false, "");
                StopConstantPowerMode();
                _driver.SetOutputEnabled(false);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F5)
            {
                var snap = _driver.GetStateSnapshot();
                _driver.SetOutputEnabled(!snap.OutputEnabled);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.P && !(this.ActiveControl is TextBox) && !(this.ActiveControl is NumericUpDown))
            {
                ToggleChartPause();
                e.Handled = true;
            }
            else if (e.Alt && e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D6)
            {
                int group = (int)(e.KeyCode - Keys.D1) + 1;
                RecallPresetGroup(group);
                e.Handled = true;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopSequence();
            StopBatteryCharger(false, "");
            StopConstantPowerMode();
            if (_chgAndCpTimer != null) _chgAndCpTimer.Stop();
            _driver.Dispose();
            base.OnFormClosing(e);
        }

        private void RefreshPortList()
        {
            _cboPort.Items.Clear();
            var ports = Dps150Driver.DiscoverPorts();
            foreach (var p in ports)
            {
                _cboPort.Items.Add(p);
            }
            if (_cboPort.Items.Count > 0)
            {
                _cboPort.SelectedIndex = 0;
            }
        }

        private void ToggleConnection()
        {
            if (_driver.IsConnected && !_driver.IsSimulated)
            {
                _driver.Disconnect();
                return;
            }

            var selected = _cboPort.SelectedItem as PortInfo;
            if (selected == null)
            {
                MessageBox.Show(
                    this,
                    _isEnglish ? "Please select a COM port from the list." : "Wybierz port COM z listy.",
                    _isEnglish ? "No COM Port" : "Brak portu COM",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            int baud = 115200;
            int.TryParse(_cboBaud.Text, out baud);
            _initialSettingsSynced = false;
            _driver.Connect(selected.PortName, baud);
        }

        private void ToggleSimulation()
        {
            if (_driver.IsConnected && _driver.IsSimulated)
            {
                _driver.Disconnect();
            }
            else
            {
                _initialSettingsSynced = false;
                _driver.StartSimulation();
            }
        }

        private void OnDriverConnectionChanged(bool connected, string statusText)
        {
            if (!this.IsHandleCreated) return;
            this.BeginInvoke((MethodInvoker)delegate
            {
                bool en = _isEnglish;
                if (connected)
                {
                    _lblConnectionBadge.Text = _driver.IsSimulated
                        ? (en ? "● SIMULATOR" : "● SYMULATOR")
                        : (en ? "● CONNECTED" : "● POŁĄCZONO");
                    _lblConnectionBadge.ForeColor = _driver.IsSimulated ? AccentAmber : AccentGreen;
                    _btnConnect.Text = _driver.IsSimulated
                        ? (en ? "🔌 Connect DPS-150" : "🔌 Połącz z DPS-150")
                        : (en ? "🔌 Disconnect" : "🔌 Rozłącz DPS-150");
                    _btnConnect.BackColor = _driver.IsSimulated ? Color.FromArgb(2, 132, 199) : AccentRed;
                    _btnSimulate.Text = _driver.IsSimulated
                        ? (en ? "⏹ Stop Simulator" : "⏹ Wyłącz Symulator")
                        : (en ? "🧪 Simulator Mode" : "🧪 Tryb Symulatora");
                    _lblSimLoadTitle.Visible = _driver.IsSimulated;
                    _nudSimLoad.Visible = _driver.IsSimulated;
                }
                else
                {
                    _lblConnectionBadge.Text = en ? "● DISCONNECTED" : "● ROZŁĄCZONO";
                    _lblConnectionBadge.ForeColor = TextMuted;
                    _btnConnect.Text = en ? "🔌 Connect DPS-150" : "🔌 Połącz z DPS-150";
                    _btnConnect.BackColor = Color.FromArgb(2, 132, 199);
                    _btnSimulate.Text = en ? "🧪 Simulator Mode" : "🧪 Tryb Symulatora";
                    _lblSimLoadTitle.Visible = false;
                    _nudSimLoad.Visible = false;
                }
            });
        }

        private void OnDriverTelemetry(Dps150State state, TelemetrySample sample)
        {
            _chart.AddSample(sample);

            if (!this.IsHandleCreated) return;
            this.BeginInvoke((MethodInvoker)delegate
            {
                UpdateDashboardUi(state);
            });
        }

        private void UpdateDashboardUi(Dps150State s)
        {
            bool en = _isEnglish;
            _lblVoutBig.Text = string.Format(CultureInfo.InvariantCulture, "{0:00.000} V", s.OutputVoltage);
            _lblIoutBig.Text = string.Format(CultureInfo.InvariantCulture, "{0:0.000} A", s.OutputCurrent);
            _lblPoutBig.Text = string.Format(CultureInfo.InvariantCulture, "{0:00.000} W", s.OutputPower);

            _lblVsetSub.Text = string.Format(CultureInfo.InvariantCulture, "Vset: {0:0.00} V (OVP: {1:0.0}V)", s.SetVoltage, s.OvpThreshold);
            _lblIsetSub.Text = string.Format(CultureInfo.InvariantCulture, "Iset: {0:0.000} A (OCP: {1:0.00}A)", s.SetCurrent, s.OcpThreshold);
            _lblPmaxSub.Text = string.Format(CultureInfo.InvariantCulture, "OPP: {0:0.0} W (Max {1:0}V/{2:0}A)", s.OppThreshold, s.MaxVoltageLimit, s.MaxCurrentLimit);

            _barV.Value = Math.Max(0, Math.Min(_barV.Maximum, (int)(s.OutputVoltage * 100f)));
            _barI.Value = Math.Max(0, Math.Min(_barI.Maximum, (int)(s.OutputCurrent * 1000f)));
            _barP.Value = Math.Max(0, Math.Min(_barP.Maximum, (int)(s.OutputPower * 10f)));

            if (!s.OutputEnabled)
            {
                _lblModeBadge.Text = en ? "STANDBY (Off)" : "STANDBY (Wył.)";
                _lblModeBadge.ForeColor = TextMuted;
            }
            else if (_cpModeActive)
            {
                _lblModeBadge.Text = en ? "⚡ CP (Const P)" : "⚡ CP (Stała Moc)";
                _lblModeBadge.ForeColor = AccentMagenta;
            }
            else if (_chargerActive)
            {
                _lblModeBadge.Text = s.IsConstantVoltage
                    ? (en ? "🔋 CHG (CV)" : "🔋 ŁAD. (CV)")
                    : (en ? "🔋 CHG (CC)" : "🔋 ŁAD. (CC)");
                _lblModeBadge.ForeColor = AccentCyan;
            }
            else if (s.IsConstantVoltage)
            {
                _lblModeBadge.Text = en ? "CV (Const V)" : "CV (Stałe U)";
                _lblModeBadge.ForeColor = AccentGreen;
            }
            else
            {
                _lblModeBadge.Text = en ? "CC (Const I!)" : "CC (Stały I!)";
                _lblModeBadge.ForeColor = AccentAmber;
            }

            if (s.Protection == ProtectionStatus.Normal)
            {
                _lblProtBadge.Text = string.Format(CultureInfo.InvariantCulture, "OK ({0:0}V/{1:0}A)", s.MaxVoltageLimit, s.MaxCurrentLimit);
                _lblProtBadge.ForeColor = AccentGreen;
            }
            else
            {
                _lblProtBadge.Text = "ALARM: " + s.Protection.ToString();
                _lblProtBadge.ForeColor = AccentRed;
            }

            float headroom = s.InputVoltage - s.OutputVoltage;
            _lblVinVal.Text = string.Format(CultureInfo.InvariantCulture, "{0:0.0}V (Δ{1:+0.0;-0.0;0.0}V)", s.InputVoltage, headroom);
            if (s.OutputEnabled && s.InputVoltage > 1.0f && headroom < 1.2f)
            {
                _lblVinVal.ForeColor = AccentRed;
            }
            else
            {
                _lblVinVal.ForeColor = AccentCyan;
            }

            int runMins = (int)(s.OutputOnSeconds / 60.0);
            int runSecs = (int)(s.OutputOnSeconds % 60.0);
            _lblTempVal.Text = string.Format(CultureInfo.InvariantCulture, "{0:0.0}°C | {1:00}:{2:00}", s.Temperature, runMins, runSecs);

            if (s.OutputEnabled && s.OutputCurrent > 0.0015f)
            {
                float r = s.OutputVoltage / s.OutputCurrent;
                string rStr = r >= 1000f
                    ? string.Format(CultureInfo.InvariantCulture, "{0:0.0}kΩ", r / 1000f)
                    : string.Format(CultureInfo.InvariantCulture, "{0:0.0}Ω", r);
                _lblResistanceVal.Text = string.Format(CultureInfo.InvariantCulture, "{0} | {1:0.0}mV", rStr, s.RippleVppMv);
            }
            else
            {
                _lblResistanceVal.Text = string.Format(CultureInfo.InvariantCulture, "∞ Ω | {0:0.0}mV", s.RippleVppMv);
            }

            _lblCapacityVal.Text = string.Format(CultureInfo.InvariantCulture, "{0:0.000} Ah ({1:0} mAh)", s.CapacityAh, s.CapacityAh * 1000f);
            _lblEnergyVal.Text = string.Format(CultureInfo.InvariantCulture, "{0:0.000} Wh ({1:0} mWh)", s.EnergyWh, s.EnergyWh * 1000f);

            UpdateDiagnosticsPanelText(s, headroom);

            if (s.OutputEnabled)
            {
                _btnOutputToggle.Text = en
                    ? "🟢 OUTPUT ACTIVE (ON) — Click [F5]"
                    : "🟢 WYJŚCIE AKTYWNE (ON) — Kliknij [F5]";
                _btnOutputToggle.BackColor = Color.FromArgb(5, 150, 105);
                _btnOutputToggle.FlatAppearance.BorderColor = AccentGreen;
            }
            else
            {
                _btnOutputToggle.Text = en
                    ? "⚪ OUTPUT DISABLED (OFF) — Click [F5]"
                    : "⚪ WYJŚCIE WYŁĄCZONE (OFF) — Kliknij [F5]";
                _btnOutputToggle.BackColor = Color.FromArgb(51, 65, 85);
                _btnOutputToggle.FlatAppearance.BorderColor = Color.FromArgb(100, 116, 139);
            }

            if (!_initialSettingsSynced && s.InputVoltage > 0.5f)
            {
                _initialSettingsSynced = true;
                _suppressSliderEvents = true;
                try
                {
                    _nudVset.Value = ClampDec((decimal)s.SetVoltage, _nudVset.Minimum, _nudVset.Maximum);
                    _trkVset.Value = Math.Max(0, Math.Min(3000, (int)Math.Round(s.SetVoltage * 100f)));

                    _nudIset.Value = ClampDec((decimal)s.SetCurrent, _nudIset.Minimum, _nudIset.Maximum);
                    _trkIset.Value = Math.Max(0, Math.Min(5000, (int)Math.Round(s.SetCurrent * 1000f)));

                    _nudOvp.Value = ClampDec((decimal)s.OvpThreshold, _nudOvp.Minimum, _nudOvp.Maximum);
                    _nudOcp.Value = ClampDec((decimal)s.OcpThreshold, _nudOcp.Minimum, _nudOcp.Maximum);
                    _nudOpp.Value = ClampDec((decimal)s.OppThreshold, _nudOpp.Minimum, _nudOpp.Maximum);
                    _nudOtp.Value = ClampDec((decimal)s.OtpThreshold, _nudOtp.Minimum, _nudOtp.Maximum);
                    _nudLvp.Value = ClampDec((decimal)s.LvpThreshold, _nudLvp.Minimum, _nudLvp.Maximum);

                    for (int i = 0; i < 6; i++)
                    {
                        if (s.Presets[i].Voltage > 0.01f || s.Presets[i].Current > 0.001f)
                        {
                            _nudPresetV[i].Value = ClampDec((decimal)s.Presets[i].Voltage, 0m, 30m);
                            _nudPresetI[i].Value = ClampDec((decimal)s.Presets[i].Current, 0m, 5m);
                        }
                    }

                    _trkBrightness.Value = Math.Max(_trkBrightness.Minimum, Math.Min(_trkBrightness.Maximum, (int)s.Brightness));
                    _lblBrightnessVal.Text = _trkBrightness.Value + " / 14";

                    _trkVolume.Value = Math.Max(_trkVolume.Minimum, Math.Min(_trkVolume.Maximum, (int)s.Volume));
                    _lblVolumeVal.Text = _trkVolume.Value + " / 10";

                    _lblDeviceInfo.Text = string.Format("{0} | FW:{1}", s.ModelName, s.FirmwareVersion);
                }
                finally
                {
                    _suppressSliderEvents = false;
                }
            }
        }

        private void UpdateDiagnosticsPanelText(Dps150State s, float headroom)
        {
            if (_lblDiagDetails == null) return;
            bool en = _isEnglish;

            string headroomStatus;
            if (!s.OutputEnabled)
                headroomStatus = en ? "Standby (Output OFF)" : "Tryb czuwania (Wyjście OFF)";
            else if (headroom < 0.8f)
                headroomStatus = en ? "⚠️ CRITICAL (<0.8V! Buck dropout)" : "⚠️ KRYTYCZNY (<0.8V! Brak zapasu Buck)";
            else if (headroom < 1.5f)
                headroomStatus = en ? "⚠️ LOW (<1.5V)" : "⚠️ NISKI (<1.5V)";
            else
                headroomStatus = en ? "OK (Stable Buck Headroom)" : "OK (Zapas przetwornicy Buck stabilny)";

            string rippleQuality;
            if (!s.OutputEnabled)
                rippleQuality = en ? "Output OFF" : "Wyjście wyłączone";
            else if (s.RippleVppMv < 15.0f)
                rippleQuality = en ? "EXCELLENT (<15 mVpp)" : "WZOROWA (<15 mVpp)";
            else if (s.RippleVppMv < 50.0f)
                rippleQuality = en ? "NORMAL (<50 mVpp)" : "DOBRA (<50 mVpp)";
            else
                rippleQuality = en ? "HIGH RIPPLE / DYNAMIC LOAD" : "PODWYŻSZONE (Obciążenie dynamiczne)";

            int h = (int)(s.OutputOnSeconds / 3600.0);
            int m = (int)((s.OutputOnSeconds % 3600.0) / 60.0);
            int sec = (int)(s.OutputOnSeconds % 60.0);
            string runtimeStr = string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}", h, m, sec);

            if (en)
            {
                _lblDiagDetails.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    "• Device ID (0xDE–0xE0)   : {0} | HW: {1} | FW: {2}\r\n" +
                    "• Input Limits (0xE1/0xE2): Max U = {3:0.00} V | Max I = {4:0.000} A ({5:0.0} W)\r\n" +
                    "• Input Supply (Vin 0xC0) : {6:0.00} V (LVP Cutoff: {7:0.00} V)\r\n" +
                    "• Voltage Headroom (ΔU)   : ΔU = Vin - Vout = {8:+0.00;-0.00;0.00} V [{9}]\r\n" +
                    "• Output Ripple (Vpp/Ipp) : U_pp = {10:0.1} mVpp | I_pp = {11:0.1} mApp [{12}]\r\n" +
                    "• Energy Metering (0xD8)  : {13} | Active Runtime: {14}",
                    s.ModelName, s.HardwareVersion, s.FirmwareVersion,
                    s.MaxVoltageLimit, s.MaxCurrentLimit, s.MaxVoltageLimit * s.MaxCurrentLimit,
                    s.InputVoltage, s.LvpThreshold,
                    headroom, headroomStatus,
                    s.RippleVppMv, s.RippleIppMa, rippleQuality,
                    s.MeteringEnabled ? "ENABLED (ON)" : "DISABLED", runtimeStr);
            }
            else
            {
                _lblDiagDetails.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    "• Identyfikacja (0xDE–0xE0) : {0} | HW: {1} | FW: {2}\r\n" +
                    "• Limity wejściowe (0xE1/E2): Max U = {3:0.00} V | Max I = {4:0.000} A ({5:0.0} W)\r\n" +
                    "• Zasilanie wejściowe (Vin) : {6:0.00} V (Próg odcięcia LVP: {7:0.00} V)\r\n" +
                    "• Spadek / Zapas (Vin-Vout) : ΔU = {8:+0.00;-0.00;0.00} V [{9}]\r\n" +
                    "• Tętnienia wyjścia (Ripple): U_pp = {10:0.1} mVpp | I_pp = {11:0.1} mApp [{12}]\r\n" +
                    "• Licznik energii (0xD8)    : {13} | Czas pracy ON: {14}",
                    s.ModelName, s.HardwareVersion, s.FirmwareVersion,
                    s.MaxVoltageLimit, s.MaxCurrentLimit, s.MaxVoltageLimit * s.MaxCurrentLimit,
                    s.InputVoltage, s.LvpThreshold,
                    headroom, headroomStatus,
                    s.RippleVppMv, s.RippleIppMa, rippleQuality,
                    s.MeteringEnabled ? "AKTYWNY (ON)" : "WYŁĄCZONY", runtimeStr);
            }
        }

        private void SelectBatteryCellCount(int cells1To7)
        {
            _selectedCellCount = Math.Max(1, Math.Min(7, cells1To7));
            for (int i = 0; i < 7; i++)
            {
                if (_btnCells[i] != null)
                {
                    bool active = (i + 1) == _selectedCellCount;
                    _btnCells[i].BackColor = active ? Color.FromArgb(2, 132, 199) : PanelDark;
                    _btnCells[i].ForeColor = active ? Color.White : TextLight;
                }
            }
            RecalculateBatteryTargetVoltage();
        }

        private void RecalculateBatteryTargetVoltage()
        {
            if (_cboBattChem == null || _nudChgVoltage == null) return;
            float vPerCell = 4.20f;
            float nomPerCell = 3.70f;
            switch (_cboBattChem.SelectedIndex)
            {
                case 0: vPerCell = 4.20f; nomPerCell = 3.70f; break; // Li-Ion / Li-Po 3.7V
                case 1: vPerCell = 4.35f; nomPerCell = 3.80f; break; // Li-Ion HV 3.8V
                case 2: vPerCell = 3.65f; nomPerCell = 3.20f; break; // LiFePO4 3.2V
                case 3: vPerCell = 2.40f; nomPerCell = 2.00f; break; // Lead-Acid Pb 2.0V
                case 4: vPerCell = 1.45f; nomPerCell = 1.20f; break; // NiMH 1.2V
            }

            float targetV = _selectedCellCount * vPerCell;
            if (targetV > 30.00f) targetV = 30.00f;
            _nudChgVoltage.Value = ClampDec((decimal)targetV, _nudChgVoltage.Minimum, _nudChgVoltage.Maximum);

            if (!_chargerActive && _lblChgStatusBanner != null)
            {
                float nomPack = _selectedCellCount * nomPerCell;
                _lblChgStatusBanner.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    _isEnglish
                        ? "Ready: Pack {0}S ({1:0.0}V nom. → Charge to {2:0.00}V, CC={3:0.00}A, Cutoff={4:0.000}A)"
                        : "Gotowy: Pakiet {0}S ({1:0.0}V nom. → Ładowanie do {2:0.00}V, CC={3:0.00}A, Stop={4:0.000}A)",
                    _selectedCellCount, nomPack, targetV, _nudChgCurrent.Value, _nudChgCutoffCurrent.Value);
                _lblChgStatusBanner.ForeColor = AccentAmber;
            }
        }

        private void StartBatteryCharger()
        {
            StopSequence();
            StopConstantPowerMode();

            float chargeV = (float)_nudChgVoltage.Value;
            float chargeI = (float)_nudChgCurrent.Value;
            var snap = _driver.GetStateSnapshot();

            if (snap.InputVoltage > 1.0f && snap.InputVoltage < chargeV + 0.4f)
            {
                MessageBox.Show(
                    this,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        _isEnglish
                            ? "Input voltage Vin ({0:0.00} V) is lower than required charging voltage ({1:0.00} V)!\n\nFNIRSI DPS-150 is a Buck (step-down) supply — please use a higher voltage USB-PD / DC source."
                            : "Napięcie wejściowe Vin ({0:0.00} V) jest niższe niż docelowe napięcie ładowania pakietu ({1:0.00} V)!\n\nZasilacz DPS-150 pracuje jako przetwornica obniżająca (Buck) — podłącz wyższe napięcie zasilania wejściowego.",
                        snap.InputVoltage, chargeV),
                    _isEnglish ? "Low Input Voltage (Vin)" : "Za niskie napięcie wejściowe (Vin)",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            _chargerActive = true;
            _chgCutoffConfirmTicks = 0;
            _chgStartAh = snap.CapacityAh;
            _chgStartWh = snap.EnergyWh;
            _chgStartTime = DateTime.Now;

            _btnChgStart.Enabled = false;
            _btnChgStop.Enabled = true;

            ApplyVoltageTarget(chargeV);
            ApplyCurrentTarget(chargeI);
            _driver.SetOutputEnabled(true);

            _lblChgStatusBanner.Text = string.Format(
                CultureInfo.InvariantCulture,
                _isEnglish
                    ? "🔋 CHARGING STARTED ({0}S): Target {1:0.00}V @ {2:0.000}A (Auto-Cutoff <= {3:0.000}A)"
                    : "🔋 ŁADOWANIE AKTYWNE ({0}S): Cel {1:0.00}V @ {2:0.000}A (Auto-Stop <= {3:0.000}A)",
                _selectedCellCount, chargeV, chargeI, _nudChgCutoffCurrent.Value);
            _lblChgStatusBanner.ForeColor = AccentCyan;
        }

        private void StopBatteryCharger(bool turnOffOutput, string customMessage)
        {
            _chargerActive = false;
            _chgCutoffConfirmTicks = 0;
            if (_btnChgStart != null) _btnChgStart.Enabled = true;
            if (_btnChgStop != null) _btnChgStop.Enabled = false;

            if (turnOffOutput)
            {
                _driver.SetOutputEnabled(false);
            }

            if (_lblChgStatusBanner != null)
            {
                if (!string.IsNullOrEmpty(customMessage))
                {
                    _lblChgStatusBanner.Text = customMessage;
                    _lblChgStatusBanner.ForeColor = AccentGreen;
                }
                else
                {
                    _lblChgStatusBanner.Text = _isEnglish
                        ? "Charger status: STOPPED"
                        : "Status ładowarki: ZATRZYMANO";
                    _lblChgStatusBanner.ForeColor = TextMuted;
                }
            }
        }

        private void StartConstantPowerMode()
        {
            StopSequence();
            StopBatteryCharger(false, "");

            _cpModeActive = true;
            _btnCpStart.Enabled = false;
            _btnCpStop.Enabled = true;

            float maxI = (float)_nudCpMaxI.Value;
            ApplyCurrentTarget(maxI);

            var snap = _driver.GetStateSnapshot();
            if (!snap.OutputEnabled)
            {
                _driver.SetOutputEnabled(true);
            }

            _lblCpStatus.Text = string.Format(
                CultureInfo.InvariantCulture,
                _isEnglish ? "⚡ CP Active: {0:0.0}W" : "⚡ CP Aktywne: {0:0.0}W",
                _nudCpWatts.Value);
            _lblCpStatus.ForeColor = AccentMagenta;
        }

        private void StopConstantPowerMode()
        {
            _cpModeActive = false;
            if (_btnCpStart != null) _btnCpStart.Enabled = true;
            if (_btnCpStop != null) _btnCpStop.Enabled = false;
            if (_lblCpStatus != null)
            {
                _lblCpStatus.Text = _isEnglish ? "CP: Disabled" : "CP: Wyłączony";
                _lblCpStatus.ForeColor = TextMuted;
            }
        }

        private void OnChargerAndCpTimerTick(object sender, EventArgs e)
        {
            var s = _driver.GetStateSnapshot();
            bool en = _isEnglish;

            // 1. Constant Power (CP) Closed-Loop Controller
            if (_cpModeActive)
            {
                if (!s.OutputEnabled)
                {
                    _lblCpStatus.Text = en ? "⚡ CP Waiting (Output OFF)" : "⚡ CP Czeka (Wyjście OFF)";
                }
                else
                {
                    float targetW = (float)_nudCpWatts.Value;
                    float maxV = (float)_nudCpMaxV.Value;
                    float maxI = (float)_nudCpMaxI.Value;

                    if (Math.Abs(s.SetCurrent - maxI) > 0.01f)
                    {
                        ApplyCurrentTarget(maxI);
                    }

                    if (s.OutputCurrent > 0.015f)
                    {
                        float idealV = targetW / s.OutputCurrent;
                        if (idealV > maxV) idealV = maxV;
                        if (idealV < 0.50f) idealV = 0.50f;

                        // Smooth proportional step to avoid oscillation
                        float nextV = s.SetVoltage + (idealV - s.SetVoltage) * 0.45f;
                        nextV = Math.Max(0.50f, Math.Min(maxV, nextV));

                        if (Math.Abs(nextV - s.SetVoltage) >= 0.01f)
                        {
                            ApplyVoltageTarget(nextV);
                        }
                    }

                    _lblCpStatus.Text = string.Format(
                        CultureInfo.InvariantCulture,
                        en ? "⚡ CP: {0:0.00}W / {1:0.0}W (U={2:0.00}V)" : "⚡ CP: {0:0.00}W / {1:0.0}W (U={2:0.00}V)",
                        s.OutputPower, targetW, s.OutputVoltage);
                    _lblCpStatus.ForeColor = AccentMagenta;
                }
            }

            // 2. Battery Charger (1S - 7S CC/CV + Auto-Cutoff)
            if (_chargerActive)
            {
                float targetV = (float)_nudChgVoltage.Value;
                float cutoffI = (float)_nudChgCutoffCurrent.Value;
                float maxMah = (float)_nudChgMaxMah.Value;

                float chargedMah = Math.Max(0f, (s.CapacityAh - _chgStartAh) * 1000f);
                float chargedWh = Math.Max(0f, s.EnergyWh - _chgStartWh);
                TimeSpan elapsed = DateTime.Now - _chgStartTime;
                float cellV = s.OutputVoltage / Math.Max(1, _selectedCellCount);

                _lblChgLiveMetrics.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    en
                        ? "Cell ({0}S): {1:0.000} V/cell | Charged: {2:0} mAh ({3:0.000} Wh) | Time: {4:00}:{5:00}"
                        : "Ogniwo ({0}S): {1:0.000} V/ogn. | Wtłoczono: {2:0} mAh ({3:0.000} Wh) | Czas: {4:00}:{5:00}",
                    _selectedCellCount, cellV, chargedMah, chargedWh, (int)elapsed.TotalMinutes, elapsed.Seconds);

                if (!s.OutputEnabled && elapsed.TotalSeconds > 2.0)
                {
                    StopBatteryCharger(false, en
                        ? "⚠️ Charger paused: Output was turned OFF"
                        : "⚠️ Ładowanie przerwane: Wyjście zasilacza zostało wyłączone");
                    _lblChgStatusBanner.ForeColor = AccentRed;
                    return;
                }

                // Check Max mAh safety cutoff
                if (maxMah > 0f && chargedMah >= maxMah)
                {
                    StopBatteryCharger(true, string.Format(
                        CultureInfo.InvariantCulture,
                        en
                            ? "✅ CHARGING COMPLETE: Reached capacity limit {0:0} mAh (Output turned OFF!)"
                            : "✅ KONIEC ŁADOWANIA: Osiągnięto limit pojemności {0:0} mAh (Wyjście wyłączone!)",
                        chargedMah));
                    return;
                }

                // Check CV phase & current cutoff (5 seconds confirmation = 20 ticks * 250ms)
                bool nearTargetVoltage = s.OutputVoltage >= (targetV - 0.08f);
                if (!s.IsConstantVoltage && !nearTargetVoltage)
                {
                    _chgCutoffConfirmTicks = 0;
                    _lblChgStatusBanner.Text = string.Format(
                        CultureInfo.InvariantCulture,
                        en
                            ? "⚡ PHASE 1 — CC (Constant Current Bulk): {0:0.000}A @ {1:0.00}V ({2:0.00}V/cell)"
                            : "⚡ FAZA 1 — CC (Stały Prąd): {0:0.000}A @ {1:0.00}V ({2:0.00}V/ogniwo)",
                        s.OutputCurrent, s.OutputVoltage, cellV);
                    _lblChgStatusBanner.ForeColor = AccentAmber;
                }
                else
                {
                    if (s.OutputCurrent <= cutoffI && elapsed.TotalSeconds > 3.0)
                    {
                        _chgCutoffConfirmTicks++;
                        int remSec = Math.Max(0, 5 - (_chgCutoffConfirmTicks / 4));
                        _lblChgStatusBanner.Text = string.Format(
                            CultureInfo.InvariantCulture,
                            en
                                ? "💧 CV Cutoff threshold reached ({0:0.000}A <= {1:0.000}A) — Finishing in {2}s..."
                                : "💧 Prąd spadł poniżej odcięcia ({0:0.000}A <= {1:0.000}A) — Auto-wyłączenie za {2}s...",
                            s.OutputCurrent, cutoffI, remSec);
                        _lblChgStatusBanner.ForeColor = AccentGreen;

                        if (_chgCutoffConfirmTicks >= 20)
                        {
                            StopBatteryCharger(true, string.Format(
                                CultureInfo.InvariantCulture,
                                en
                                    ? "✅ BATTERY 100% CHARGED ({0}S = {1:0.00}V, +{2:0} mAh) — Auto-Cutoff Turned Output OFF!"
                                    : "✅ AKUMULATOR NAŁADOWANY 100% ({0}S = {1:0.00}V, +{2:0} mAh) — Auto-Stop Wyłączył Wyjście!",
                                _selectedCellCount, s.OutputVoltage, chargedMah));
                        }
                    }
                    else
                    {
                        _chgCutoffConfirmTicks = 0;
                        _lblChgStatusBanner.Text = string.Format(
                            CultureInfo.InvariantCulture,
                            en
                                ? "💧 PHASE 2 — CV (Constant Voltage Absorption {0:0.00}V): I={1:0.000}A -> Cutoff at {2:0.000}A"
                                : "💧 FAZA 2 — CV (Dopełnianie napięciowe {0:0.00}V): I={1:0.000}A -> Odcięcie przy {2:0.000}A",
                            s.OutputVoltage, s.OutputCurrent, cutoffI);
                        _lblChgStatusBanner.ForeColor = AccentCyan;
                    }
                }
            }
        }

        private void ApplyVoltageTarget(float v)
        {
            v = Math.Max(0f, Math.Min(30.0f, v));
            _suppressSliderEvents = true;
            _nudVset.Value = (decimal)v;
            _trkVset.Value = (int)Math.Round(v * 100f);
            _suppressSliderEvents = false;
            _driver.SetVoltage(v);
        }

        private void AdjustVoltageBy(float delta)
        {
            ApplyVoltageTarget((float)_nudVset.Value + delta);
        }

        private void ApplyCurrentTarget(float a)
        {
            a = Math.Max(0f, Math.Min(5.0f, a));
            _suppressSliderEvents = true;
            _nudIset.Value = (decimal)a;
            _trkIset.Value = (int)Math.Round(a * 1000f);
            _suppressSliderEvents = false;
            _driver.SetCurrent(a);
        }

        private void AdjustCurrentBy(float delta)
        {
            ApplyCurrentTarget((float)_nudIset.Value + delta);
        }

        private void RecallPresetGroup(int group1To6)
        {
            int idx = group1To6 - 1;
            if (idx < 0 || idx >= 6) return;
            ApplyVoltageTarget((float)_nudPresetV[idx].Value);
            ApplyCurrentTarget((float)_nudPresetI[idx].Value);
        }

        private void ToggleChartPause()
        {
            _chart.IsPaused = !_chart.IsPaused;
            UpdatePauseButtonUi();
        }

        private void UpdatePauseButtonUi()
        {
            if (_btnPauseChart == null || _chart == null) return;
            if (_chart.IsPaused)
            {
                _btnPauseChart.Text = _isEnglish
                    ? "▶ RESUME WAVEFORM (LIVE) [P]"
                    : "▶ WZNÓW PRZEBIEG (NA ŻYWO) [P]";
                _btnPauseChart.BackColor = Color.FromArgb(5, 150, 105);
                _btnPauseChart.FlatAppearance.BorderColor = AccentGreen;
            }
            else
            {
                _btnPauseChart.Text = _isEnglish
                    ? "⏸ PAUSE WAVEFORM (HOLD) [P]"
                    : "⏸ ZATRZYMAJ PRZEBIEG (PAUZA) [P]";
                _btnPauseChart.BackColor = Color.FromArgb(180, 83, 9);
                _btnPauseChart.FlatAppearance.BorderColor = AccentAmber;
            }
            _chart.Invalidate();
        }

        private void ExportChartToCsv()
        {
            var samples = _chart.GetSamplesSnapshot();
            if (samples.Count == 0)
            {
                MessageBox.Show(
                    this,
                    _isEnglish ? "No chart samples to export." : "Brak próbek na wykresie do wyeksportowania.",
                    "CSV Export",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV Files (*.csv)|*.csv";
                sfd.FileName = string.Format("DPS150_Log_{0:yyyyMMdd_HHmmss}.csv", DateTime.Now);
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Timestamp,ElapsedSeconds,Vout_V,Iout_A,Pout_W,Vset_V,Iset_A,Mode,OutputOn");
                    foreach (var s in samples)
                    {
                        sb.AppendFormat(
                            CultureInfo.InvariantCulture,
                            "{0:yyyy-MM-dd HH:mm:ss.fff},{1:0.000},{2:0.000},{3:0.000},{4:0.000},{5:0.00},{6:0.000},{7},{8}\r\n",
                            s.Timestamp, s.ElapsedSeconds, s.Voltage, s.Current, s.Power, s.SetVoltage, s.SetCurrent,
                            s.IsCv ? "CV" : "CC", s.OutputOn ? 1 : 0);
                    }
                    File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show(
                        this,
                        (_isEnglish ? "Saved " : "Zapisano ") + samples.Count + (_isEnglish ? " samples to:\n" : " próbek do pliku:\n") + sfd.FileName,
                        "CSV Export",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
        }

        private void LoadDefaultSequenceTemplate(int tpl)
        {
            _gridSeq.Rows.Clear();
            if (tpl == 1)
            {
                _gridSeq.Rows.Add("3.30", "1.000", "2000");
                _gridSeq.Rows.Add("5.00", "1.500", "2000");
                _gridSeq.Rows.Add("9.00", "2.000", "2000");
                _gridSeq.Rows.Add("12.00", "2.000", "3000");
                _gridSeq.Rows.Add("5.00", "1.000", "1500");
            }
            else if (tpl == 2)
            {
                for (int i = 1; i <= 10; i++)
                {
                    float v = i * 1.2f;
                    _gridSeq.Rows.Add(v.ToString("0.00", CultureInfo.InvariantCulture), "2.000", "600");
                }
            }
            else
            {
                _gridSeq.Rows.Add("5.00", "2.000", "1000");
                _gridSeq.Rows.Add("12.00", "3.000", "1000");
                _gridSeq.Rows.Add("3.30", "1.000", "1000");
                _gridSeq.Rows.Add("15.00", "3.000", "1000");
            }
        }

        private void StartSequence()
        {
            if (_gridSeq.Rows.Count == 0) return;
            _seqCurrentStep = -1;
            _btnSeqStart.Enabled = false;
            _btnSeqStop.Enabled = true;
            _driver.SetOutputEnabled(true);
            AdvanceSequenceStep();
        }

        private void StopSequence()
        {
            if (_seqTimer != null) _seqTimer.Stop();
            _seqCurrentStep = -1;
            if (_btnSeqStart != null) _btnSeqStart.Enabled = true;
            if (_btnSeqStop != null) _btnSeqStop.Enabled = false;
            if (_lblSeqStatus != null)
                _lblSeqStatus.Text = _isEnglish ? "Sequence status: Stopped" : "Status sekwencji: Zatrzymano";
        }

        private void OnSequenceTimerTick(object sender, EventArgs e)
        {
            _seqTimer.Stop();
            AdvanceSequenceStep();
        }

        private void AdvanceSequenceStep()
        {
            _seqCurrentStep++;
            int validRows = _gridSeq.AllowUserToAddRows ? _gridSeq.Rows.Count - 1 : _gridSeq.Rows.Count;

            if (_seqCurrentStep >= validRows)
            {
                if (_chkSeqLoop.Checked && validRows > 0)
                {
                    _seqCurrentStep = 0;
                }
                else
                {
                    StopSequence();
                    _lblSeqStatus.Text = _isEnglish ? "Sequence status: Completed" : "Status sekwencji: Zakończono pomyślnie";
                    return;
                }
            }

            var row = _gridSeq.Rows[_seqCurrentStep];
            float v = 5.0f, a = 1.0f;
            int ms = 1000;

            float.TryParse(Convert.ToString(row.Cells[0].Value), NumberStyles.Float, CultureInfo.InvariantCulture, out v);
            float.TryParse(Convert.ToString(row.Cells[1].Value), NumberStyles.Float, CultureInfo.InvariantCulture, out a);
            int.TryParse(Convert.ToString(row.Cells[2].Value), out ms);
            ms = Math.Max(150, ms);

            _gridSeq.ClearSelection();
            row.Selected = true;

            ApplyVoltageTarget(v);
            ApplyCurrentTarget(a);

            _lblSeqStatus.Text = string.Format(
                _isEnglish ? "Step {0}/{1}: U={2:0.00}V, I={3:0.000}A ({4} ms)" : "Krok {0}/{1}: U={2:0.00}V, I={3:0.000}A ({4} ms)",
                _seqCurrentStep + 1, validRows, v, a, ms);
            _seqTimer.Interval = ms;
            _seqTimer.Start();
        }

        private static Control CreateHeaderLogoBadge()
        {
            var badge = new Panel
            {
                Size = new Size(158, 34),
                Margin = new Padding(0, 0, 10, 0),
                BackColor = CardDark
            };

            badge.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                using (Pen pBorder = new Pen(Color.FromArgb(0, 180, 216), 1.2f))
                {
                    g.DrawRectangle(pBorder, 0, 0, badge.Width - 1, badge.Height - 1);
                }

                DrawVectorLogoEmblem(g, new Rectangle(3, 2, 30, 30));

                using (Font fTop = new Font("Consolas", 9.5f, FontStyle.Bold))
                using (Font fBot = new Font("Segoe UI", 7.0f, FontStyle.Bold))
                using (Brush bCyan = new SolidBrush(AccentCyan))
                using (Brush bAmber = new SolidBrush(AccentAmber))
                {
                    g.DrawString("DPS-150 PRO", fTop, bCyan, 36, 3);
                    g.DrawString("BY PATRYK JANKOWSKI", fBot, bAmber, 36, 18);
                }
            };

            return badge;
        }

        private Control BuildAuthorFooterBar()
        {
            _footerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PanelDark,
                Margin = new Padding(0, 4, 0, 0)
            };

            _footerPanel.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                using (Pen borderPen = new Pen(Color.FromArgb(45, 58, 82), 1f))
                {
                    g.DrawRectangle(borderPen, 0, 0, _footerPanel.Width - 1, _footerPanel.Height - 1);
                }

                DrawVectorLogoEmblem(g, new Rectangle(10, 3, 26, 26));

                string leftSub = _isEnglish
                    ? "FNIRSI DPS-150 STUDIO PRO • PRECISION CNC DC POWER SUPPLY CONTROL & TELEMETRY"
                    : "FNIRSI DPS-150 STUDIO PRO • SYSTEM STEROWANIA I TELEMETRII ZASILACZA CNC DC";

                using (Font fLeft = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                using (Brush bMuted = new SolidBrush(TextMuted))
                {
                    g.DrawString(leftSub, fLeft, bMuted, 44, 8);
                }

                // English signature badge as requested: "CREATED BY: PATRYK JANKOWSKI"
                string sigText = "⚡ CREATED BY: PATRYK JANKOWSKI";
                using (Font fSig = new Font("Consolas", 10.5f, FontStyle.Bold))
                {
                    SizeF sz = g.MeasureString(sigText, fSig);
                    float boxW = sz.Width + 24;
                    float boxH = 24;
                    float bx = _footerPanel.Width - boxW - 10;
                    float by = (_footerPanel.Height - boxH) / 2f;

                    using (Brush bBg = new SolidBrush(Color.FromArgb(24, 34, 52)))
                    using (Pen pAccent = new Pen(AccentCyan, 1.2f))
                    using (Brush bText = new SolidBrush(AccentAmber))
                    {
                        g.FillRectangle(bBg, bx, by, boxW, boxH);
                        g.DrawRectangle(pAccent, bx, by, boxW, boxH);
                        g.DrawString(sigText, fSig, bText, bx + 12, by + 4);
                    }
                }
            };

            _footerPanel.Resize += (s, e) => _footerPanel.Invalidate();
            return _footerPanel;
        }

        private static void DrawVectorLogoEmblem(Graphics g, Rectangle r)
        {
            using (Brush bgBrush = new SolidBrush(Color.FromArgb(12, 22, 38)))
            using (Pen ringPen = new Pen(AccentCyan, 1.8f))
            {
                g.FillEllipse(bgBrush, r);
                g.DrawEllipse(ringPen, r);
            }

            PointF[] wave = new PointF[]
            {
                new PointF(r.Left + r.Width * 0.15f, r.Top + r.Height * 0.55f),
                new PointF(r.Left + r.Width * 0.35f, r.Top + r.Height * 0.30f),
                new PointF(r.Left + r.Width * 0.65f, r.Top + r.Height * 0.78f),
                new PointF(r.Left + r.Width * 0.85f, r.Top + r.Height * 0.50f)
            };
            using (Pen wavePen = new Pen(Color.FromArgb(180, AccentCyan), 1.6f))
            {
                g.DrawLines(wavePen, wave);
            }

            PointF[] bolt = new PointF[]
            {
                new PointF(r.Left + r.Width * 0.56f, r.Top + r.Height * 0.14f),
                new PointF(r.Left + r.Width * 0.32f, r.Top + r.Height * 0.52f),
                new PointF(r.Left + r.Width * 0.50f, r.Top + r.Height * 0.52f),
                new PointF(r.Left + r.Width * 0.42f, r.Top + r.Height * 0.86f),
                new PointF(r.Left + r.Width * 0.70f, r.Top + r.Height * 0.44f),
                new PointF(r.Left + r.Width * 0.52f, r.Top + r.Height * 0.44f)
            };
            using (Brush boltBrush = new SolidBrush(AccentAmber))
            {
                g.FillPolygon(boltBrush, bolt);
            }
        }

        private static Icon CreateCustomLogoIcon()
        {
            try
            {
                using (Bitmap bmp = new Bitmap(32, 32))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);
                    DrawVectorLogoEmblem(g, new Rectangle(1, 1, 30, 30));
                    return Icon.FromHandle(bmp.GetHicon());
                }
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        private static Button CreateFlatButton(string text, Size size, Color backColor)
        {
            var btn = new Button
            {
                Text = text,
                Size = size,
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = TextLight,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(71, 85, 105);
            return btn;
        }

        private static decimal ClampDec(decimal val, decimal min, decimal max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }
    }
}
