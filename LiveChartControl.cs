using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FnirsiDps150Studio
{
    public class LiveChartControl : Control
    {
        private readonly List<TelemetrySample> _samples = new List<TelemetrySample>(36000);
        private readonly object _sync = new object();

        public bool ShowVoltage = true;
        public bool ShowCurrent = true;
        public bool ShowPower = true;
        public bool ShowSetpoints = true;
        public bool AutoScaleY = true;
        public bool IsEnglish = false;

        private bool _isPaused = false;
        private double _pausedAtTime = -1.0;
        public bool IsPaused
        {
            get { return _isPaused; }
            set
            {
                if (_isPaused != value)
                {
                    _isPaused = value;
                    lock (_sync)
                    {
                        if (_isPaused && _samples.Count > 0)
                        {
                            _pausedAtTime = _samples[_samples.Count - 1].ElapsedSeconds;
                        }
                        else
                        {
                            _pausedAtTime = -1.0;
                            _panOffsetSeconds = 0.0;
                        }
                    }
                    var handler = OnViewportChanged;
                    if (handler != null) handler();
                    Invalidate();
                }
            }
        }

        // Time window in seconds (zoom level)
        private double _timeWindowSeconds = 30.0;
        public double TimeWindowSeconds
        {
            get { return _timeWindowSeconds; }
            set
            {
                _timeWindowSeconds = Math.Max(5.0, Math.Min(3600.0, value));
                Invalidate();
            }
        }

        // Pan offset from latest sample in seconds (0 = live right edge)
        private double _panOffsetSeconds = 0.0;
        public double PanOffsetSeconds
        {
            get { return _panOffsetSeconds; }
            set
            {
                _panOffsetSeconds = Math.Max(0.0, value);
                Invalidate();
            }
        }

        private bool _isDragging;
        private Point _dragStartPoint;
        private double _dragStartOffset;
        private Point _mousePos = new Point(-1, -1);

        // Colors matching dark bench oscilloscope aesthetic
        private static readonly Color BgColor = Color.FromArgb(14, 18, 26);
        private static readonly Color GridMajorColor = Color.FromArgb(35, 45, 62);
        private static readonly Color GridMinorColor = Color.FromArgb(22, 29, 41);
        private static readonly Color BorderColor = Color.FromArgb(55, 70, 95);
        private static readonly Color VoltageColor = Color.FromArgb(0, 229, 255);   // Cyan
        private static readonly Color CurrentColor = Color.FromArgb(255, 214, 0);   // Amber/Yellow
        private static readonly Color PowerColor = Color.FromArgb(255, 64, 129);    // Magenta/Coral
        private static readonly Color TextMutedColor = Color.FromArgb(148, 163, 184);

        public event Action OnViewportChanged;

        public LiveChartControl()
        {
            this.SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw,
                true);
            this.BackColor = BgColor;
            this.Cursor = Cursors.Cross;
        }

        public void AddSample(TelemetrySample sample)
        {
            lock (_sync)
            {
                _samples.Add(sample);
                // Keep up to 36,000 samples (~1 hour at 10Hz)
                if (_samples.Count > 36000)
                {
                    _samples.RemoveRange(0, 1000);
                }
            }

            if (!IsPaused)
            {
                if (this.IsHandleCreated)
                {
                    this.BeginInvoke((MethodInvoker)delegate { this.Invalidate(); });
                }
            }
        }

        public void ClearSamples()
        {
            lock (_sync)
            {
                _samples.Clear();
            }
            _panOffsetSeconds = 0;
            Invalidate();
        }

        public List<TelemetrySample> GetSamplesSnapshot()
        {
            lock (_sync)
            {
                return new List<TelemetrySample>(_samples);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            double factor = e.Delta > 0 ? 0.75 : 1.333;
            TimeWindowSeconds = _timeWindowSeconds * factor;
            var handler = OnViewportChanged;
            if (handler != null) handler();
        }

        protected override void OnDoubleClick(EventArgs e)
        {
            base.OnDoubleClick(e);
            IsPaused = !IsPaused;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _dragStartPoint = e.Location;
                _dragStartOffset = _panOffsetSeconds;
            }
            else if (e.Button == MouseButtons.Middle)
            {
                IsPaused = !IsPaused;
            }
            else if (e.Button == MouseButtons.Right)
            {
                // Right-click resets pan to live edge and resumes
                _panOffsetSeconds = 0;
                IsPaused = false;
                var handler = OnViewportChanged;
                if (handler != null) handler();
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            _mousePos = e.Location;

            if (_isDragging)
            {
                int plotWidth = Math.Max(100, this.Width - 110);
                int dx = e.X - _dragStartPoint.X;
                if (Math.Abs(dx) > 5 && !_isPaused)
                {
                    IsPaused = true;
                }
                double deltaSec = (dx / (double)plotWidth) * _timeWindowSeconds;
                PanOffsetSeconds = _dragStartOffset + deltaSec;
                var handler = OnViewportChanged;
                if (handler != null) handler();
            }
            else
            {
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isDragging = false;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _mousePos = new Point(-1, -1);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            g.Clear(BgColor);

            Rectangle plotRect = new Rectangle(68, 30, Math.Max(50, this.Width - 136), Math.Max(40, this.Height - 62));

            // 1. Draw Oscilloscope Grid
            DrawGrid(g, plotRect);

            List<TelemetrySample> visible = new List<TelemetrySample>();
            double latestTime = 0;
            double minV = 0, maxV = 0, sumV = 0;
            double minI = 0, maxI = 0, sumI = 0;
            double maxP = 0, sumP = 0;

            lock (_sync)
            {
                if (_samples.Count > 0)
                {
                    if (_isPaused && _pausedAtTime >= 0)
                    {
                        latestTime = _pausedAtTime;
                    }
                    else
                    {
                        latestTime = _samples[_samples.Count - 1].ElapsedSeconds;
                    }
                    double tEnd = latestTime - _panOffsetSeconds;
                    double tStart = tEnd - _timeWindowSeconds;

                    for (int i = 0; i < _samples.Count; i++)
                    {
                        var s = _samples[i];
                        if (s.ElapsedSeconds >= tStart && s.ElapsedSeconds <= tEnd)
                        {
                            visible.Add(s);
                        }
                    }
                }
            }

            // Compute scale bounds
            float scaleMaxV = 30.0f;
            float scaleMaxI = 5.0f;
            float scaleMaxP = 150.0f;

            if (visible.Count > 0)
            {
                minV = visible[0].Voltage;
                maxV = visible[0].Voltage;
                minI = visible[0].Current;
                maxI = visible[0].Current;
                maxP = visible[0].Power;

                for (int i = 0; i < visible.Count; i++)
                {
                    var s = visible[i];
                    if (s.Voltage < minV) minV = s.Voltage;
                    if (s.Voltage > maxV) maxV = s.Voltage;
                    if (s.Current < minI) minI = s.Current;
                    if (s.Current > maxI) maxI = s.Current;
                    if (s.Power > maxP) maxP = s.Power;

                    sumV += s.Voltage;
                    sumI += s.Current;
                    sumP += s.Power;
                }

                if (AutoScaleY)
                {
                    float topV = Math.Max((float)maxV, visible[visible.Count - 1].SetVoltage);
                    float topI = Math.Max((float)maxI, visible[visible.Count - 1].SetCurrent);
                    scaleMaxV = Math.Max(1.0f, (float)Math.Ceiling(topV * 1.15f));
                    scaleMaxI = Math.Max(0.2f, (float)Math.Ceiling(topI * 1.2f * 5f) / 5f);
                    scaleMaxP = Math.Max(2.0f, (float)Math.Ceiling(maxP * 1.25f));
                }
            }

            // 2. Draw Y-Axis Scale Labels
            DrawYAxisLabels(g, plotRect, scaleMaxV, scaleMaxI);

            double tWindowEnd = latestTime - _panOffsetSeconds;
            double tWindowStart = tWindowEnd - _timeWindowSeconds;

            // 3. Draw X-Axis Time Labels
            DrawXAxisLabels(g, plotRect, tWindowStart, tWindowEnd);

            // 4. Draw Top Statistics Bar
            DrawStatsBar(g, plotRect, visible.Count, minV, maxV, sumV, minI, maxI, sumI, maxP, sumP);

            if (visible.Count >= 2)
            {
                g.SetClip(plotRect);

                // Optional setpoint reference lines
                if (ShowSetpoints)
                {
                    var last = visible[visible.Count - 1];
                    if (ShowVoltage)
                    {
                        float yVset = ValueToY(last.SetVoltage, 0, scaleMaxV, plotRect);
                        using (Pen pSetV = new Pen(Color.FromArgb(90, VoltageColor), 1f) { DashStyle = DashStyle.Dash })
                        {
                            g.DrawLine(pSetV, plotRect.Left, yVset, plotRect.Right, yVset);
                        }
                    }
                    if (ShowCurrent)
                    {
                        float yIset = ValueToY(last.SetCurrent, 0, scaleMaxI, plotRect);
                        using (Pen pSetI = new Pen(Color.FromArgb(90, CurrentColor), 1f) { DashStyle = DashStyle.Dash })
                        {
                            g.DrawLine(pSetI, plotRect.Left, yIset, plotRect.Right, yIset);
                        }
                    }
                }

                // Draw Power trace (with subtle fill)
                if (ShowPower)
                {
                    DrawTrace(g, visible, tWindowStart, _timeWindowSeconds, plotRect, s => s.Power, scaleMaxP, PowerColor, 1.6f, true);
                }

                // Draw Current trace
                if (ShowCurrent)
                {
                    DrawTrace(g, visible, tWindowStart, _timeWindowSeconds, plotRect, s => s.Current, scaleMaxI, CurrentColor, 2.0f, false);
                }

                // Draw Voltage trace
                if (ShowVoltage)
                {
                    DrawTrace(g, visible, tWindowStart, _timeWindowSeconds, plotRect, s => s.Voltage, scaleMaxV, VoltageColor, 2.2f, false);
                }

                g.ResetClip();

                // 5. Draw Interactive Crosshair & Readout Tooltip
                if (plotRect.Contains(_mousePos))
                {
                    DrawCrosshairAndTooltip(g, plotRect, visible, tWindowStart, _timeWindowSeconds, scaleMaxV, scaleMaxI, scaleMaxP);
                }
            }
            else
            {
                using (Font fEmpty = new Font("Segoe UI", 10f, FontStyle.Regular))
                using (Brush bEmpty = new SolidBrush(TextMutedColor))
                {
                    string msg = IsEnglish
                        ? "Waiting for telemetry data (Connect to DPS-150 or enable Simulator Mode)..."
                        : "Oczekiwanie na dane telemetryczne (Połącz z zasilaczem DPS-150 lub włącz Tryb Symulatora)...";
                    SizeF sz = g.MeasureString(msg, fEmpty);
                    g.DrawString(msg, fEmpty, bEmpty,
                        plotRect.Left + (plotRect.Width - sz.Width) / 2f,
                        plotRect.Top + (plotRect.Height - sz.Height) / 2f);
                }
            }

            // Border & Subtle Oscilloscope Watermark
            using (Font fWm = new Font("Consolas", 8.5f, FontStyle.Bold))
            using (Brush bWm = new SolidBrush(Color.FromArgb(75, 0, 229, 255)))
            {
                string wm = "⚡ CREATED BY PATRYK JANKOWSKI • DPS-150 STUDIO";
                SizeF wmSz = g.MeasureString(wm, fWm);
                g.DrawString(wm, fWm, bWm, plotRect.Right - wmSz.Width - 10, plotRect.Top + 6);
            }

            if (_isPaused)
            {
                using (Font fPause = new Font("Consolas", 9.5f, FontStyle.Bold))
                {
                    string pText = IsEnglish
                        ? "⏸ PAUSED (HOLD) — Move cursor to inspect | Scroll=Zoom | Drag=Pan | Double-click/Space to Resume"
                        : "⏸ PAUZA (HOLD) — Najedz kursorem aby sprawdzic przebieg | Rolka=Zoom | LPM=Przesun | Spacja/2xKlik=Wznow";
                    SizeF pSz = g.MeasureString(pText, fPause);
                    RectangleF badgeRect = new RectangleF(plotRect.Left + 8, plotRect.Top + 6, pSz.Width + 14, pSz.Height + 6);
                    using (Brush bBg = new SolidBrush(Color.FromArgb(230, 180, 83, 9)))
                    using (Pen pBrd = new Pen(Color.FromArgb(255, 214, 0), 1.2f))
                    using (Brush bTxt = new SolidBrush(Color.White))
                    {
                        g.FillRectangle(bBg, badgeRect);
                        g.DrawRectangle(pBrd, badgeRect.X, badgeRect.Y, badgeRect.Width, badgeRect.Height);
                        g.DrawString(pText, fPause, bTxt, badgeRect.X + 7, badgeRect.Y + 3);
                    }
                }
            }

            using (Pen borderPen = new Pen(BorderColor, 1f))
            {
                g.DrawRectangle(borderPen, plotRect);
            }
        }

        private void DrawGrid(Graphics g, Rectangle r)
        {
            int hDivs = 10;
            int vDivs = 5;

            using (Pen minorPen = new Pen(GridMinorColor, 1f) { DashStyle = DashStyle.Dot })
            using (Pen majorPen = new Pen(GridMajorColor, 1f))
            {
                for (int i = 1; i < hDivs * 2; i++)
                {
                    float x = r.Left + (r.Width * i) / (float)(hDivs * 2);
                    g.DrawLine((i % 2 == 0) ? majorPen : minorPen, x, r.Top, x, r.Bottom);
                }

                for (int j = 1; j < vDivs * 2; j++)
                {
                    float y = r.Top + (r.Height * j) / (float)(vDivs * 2);
                    g.DrawLine((j % 2 == 0) ? majorPen : minorPen, r.Left, y, r.Right, y);
                }
            }
        }

        private void DrawYAxisLabels(Graphics g, Rectangle r, float maxV, float maxI)
        {
            using (Font f = new Font("Consolas", 8.5f, FontStyle.Bold))
            using (Brush bV = new SolidBrush(VoltageColor))
            using (Brush bI = new SolidBrush(CurrentColor))
            {
                int steps = 5;
                for (int i = 0; i <= steps; i++)
                {
                    float frac = 1.0f - (i / (float)steps);
                    float y = r.Top + (r.Height * i) / (float)steps - 6;

                    if (ShowVoltage)
                    {
                        float valV = maxV * frac;
                        string sV = valV >= 10f ? valV.ToString("0.0") + "V" : valV.ToString("0.00") + "V";
                        g.DrawString(sV, f, bV, 4, y);
                    }

                    if (ShowCurrent)
                    {
                        float valI = maxI * frac;
                        string sI = valI.ToString("0.00") + "A";
                        g.DrawString(sI, f, bI, r.Right + 5, y);
                    }
                }
            }
        }

        private void DrawXAxisLabels(Graphics g, Rectangle r, double tStart, double tEnd)
        {
            using (Font f = new Font("Consolas", 8.5f))
            using (Brush b = new SolidBrush(TextMutedColor))
            {
                int steps = 5;
                for (int i = 0; i <= steps; i++)
                {
                    double t = tStart + ((tEnd - tStart) * i) / steps;
                    float x = r.Left + (r.Width * i) / (float)steps;
                    string label = string.Format("{0:0.0}s", Math.Max(0, t));
                    SizeF sz = g.MeasureString(label, f);
                    g.DrawString(label, f, b, x - sz.Width / 2f, r.Bottom + 6);
                }
            }
        }

        private void DrawStatsBar(
            Graphics g, Rectangle r, int count,
            double minV, double maxV, double sumV,
            double minI, double maxI, double sumI,
            double maxP, double sumP)
        {
            using (Font f = new Font("Consolas", 8.5f, FontStyle.Bold))
            {
                double avgV = count > 0 ? sumV / count : 0;
                double avgI = count > 0 ? sumI / count : 0;
                double avgP = count > 0 ? sumP / count : 0;

                float x = r.Left;
                float y = 6;

                using (Brush bV = new SolidBrush(VoltageColor))
                {
                    string vStat = string.Format(IsEnglish ? "V: avg {0:0.000}V ({1:0.00}..{2:0.00}V)" : "V: śr {0:0.000}V ({1:0.00}..{2:0.00}V)", avgV, minV, maxV);
                    g.DrawString(vStat, f, bV, x, y);
                    x += g.MeasureString(vStat, f).Width + 16;
                }

                using (Brush bI = new SolidBrush(CurrentColor))
                {
                    string iStat = string.Format(IsEnglish ? "I: avg {0:0.000}A (max {1:0.000}A)" : "I: śr {0:0.000}A (max {1:0.000}A)", avgI, maxI);
                    g.DrawString(iStat, f, bI, x, y);
                    x += g.MeasureString(iStat, f).Width + 16;
                }

                using (Brush bP = new SolidBrush(PowerColor))
                {
                    string pStat = string.Format(IsEnglish ? "P: avg {0:0.00}W (max {1:0.00}W)" : "P: śr {0:0.00}W (max {1:0.00}W)", avgP, maxP);
                    g.DrawString(pStat, f, bP, x, y);
                }

                using (Brush bHint = new SolidBrush(TextMutedColor))
                {
                    string hint;
                    if (IsEnglish)
                    {
                        hint = IsPaused
                            ? "[PAUSED] Scroll: Zoom | LMB: Pan | RMB: Reset"
                            : "Window: " + _timeWindowSeconds.ToString("0") + "s (Scroll: Zoom | RMB: Reset)";
                    }
                    else
                    {
                        hint = IsPaused
                            ? "[PAUZA] Scroll: Zoom | LPM: Przesuń | PPM: Reset"
                            : "Okno: " + _timeWindowSeconds.ToString("0") + "s (Scroll: Zoom | PPM: Reset)";
                    }
                    SizeF hSz = g.MeasureString(hint, f);
                    if (r.Right - hSz.Width > x + 120)
                    {
                        g.DrawString(hint, f, bHint, r.Right - hSz.Width, y);
                    }
                }
            }
        }

        private void DrawTrace(
            Graphics g,
            List<TelemetrySample> samples,
            double tStart,
            double tSpan,
            Rectangle r,
            Func<TelemetrySample, float> selector,
            float maxVal,
            Color color,
            float penWidth,
            bool fillUnder)
        {
            if (samples.Count < 2) return;

            // Decimate if very dense so rendering stays silky smooth
            int step = Math.Max(1, samples.Count / Math.Max(400, r.Width));
            List<PointF> pts = new List<PointF>((samples.Count / step) + 2);

            for (int i = 0; i < samples.Count; i += step)
            {
                var s = samples[i];
                float x = TimeToX(s.ElapsedSeconds, tStart, tSpan, r);
                float y = ValueToY(selector(s), 0, maxVal, r);
                pts.Add(new PointF(x, y));
            }

            // Always include very last point
            var last = samples[samples.Count - 1];
            pts.Add(new PointF(
                TimeToX(last.ElapsedSeconds, tStart, tSpan, r),
                ValueToY(selector(last), 0, maxVal, r)));

            if (pts.Count < 2) return;

            PointF[] arr = pts.ToArray();

            if (fillUnder)
            {
                PointF[] poly = new PointF[arr.Length + 2];
                Array.Copy(arr, 0, poly, 0, arr.Length);
                poly[arr.Length] = new PointF(arr[arr.Length - 1].X, r.Bottom);
                poly[arr.Length + 1] = new PointF(arr[0].X, r.Bottom);

                using (Brush fillBrush = new SolidBrush(Color.FromArgb(24, color)))
                {
                    g.FillPolygon(fillBrush, poly);
                }
            }

            using (Pen tracePen = new Pen(color, penWidth))
            {
                tracePen.LineJoin = LineJoin.Round;
                g.DrawLines(tracePen, arr);
            }
        }

        private void DrawCrosshairAndTooltip(
            Graphics g,
            Rectangle r,
            List<TelemetrySample> visible,
            double tStart,
            double tSpan,
            float maxV,
            float maxI,
            float maxP)
        {
            double targetTime = tStart + ((_mousePos.X - r.Left) / (double)r.Width) * tSpan;

            // Find closest sample
            TelemetrySample closest = visible[0];
            double bestDist = Math.Abs(closest.ElapsedSeconds - targetTime);
            for (int i = 1; i < visible.Count; i++)
            {
                double d = Math.Abs(visible[i].ElapsedSeconds - targetTime);
                if (d < bestDist)
                {
                    bestDist = d;
                    closest = visible[i];
                }
            }

            float cx = TimeToX(closest.ElapsedSeconds, tStart, tSpan, r);
            using (Pen cursorPen = new Pen(Color.FromArgb(140, 226, 232, 240), 1f) { DashStyle = DashStyle.Dash })
            {
                g.DrawLine(cursorPen, cx, r.Top, cx, r.Bottom);
            }

            // Draw dots on active traces
            if (ShowVoltage)
            {
                float yv = ValueToY(closest.Voltage, 0, maxV, r);
                DrawMarkerDot(g, cx, yv, VoltageColor);
            }
            if (ShowCurrent)
            {
                float yi = ValueToY(closest.Current, 0, maxI, r);
                DrawMarkerDot(g, cx, yi, CurrentColor);
            }
            if (ShowPower)
            {
                float yp = ValueToY(closest.Power, 0, maxP, r);
                DrawMarkerDot(g, cx, yp, PowerColor);
            }

            // Build readout tooltip
            string rStr = closest.Current > 0.001f
                ? string.Format("{0:0.00} Ω", closest.Voltage / closest.Current)
                : "∞ Ω";

            string[] lines = IsEnglish
                ? new string[]
                {
                    string.Format("Time:  {0:0.00} s ({1:HH:mm:ss.f})", closest.ElapsedSeconds, closest.Timestamp),
                    string.Format("Vout:  {0:0.000} V  (Vset: {1:0.00}V)", closest.Voltage, closest.SetVoltage),
                    string.Format("Iout:  {0:0.000} A  (Iset: {1:0.000}A)", closest.Current, closest.SetCurrent),
                    string.Format("Pout:  {0:0.000} W  | R_load: {1}", closest.Power, rStr),
                    string.Format("Mode:  {0} ({1})", closest.IsCv ? "CV - Constant Voltage" : "CC - Constant Current", closest.OutputOn ? "ON" : "OFF")
                }
                : new string[]
                {
                    string.Format("Czas:  {0:0.00} s ({1:HH:mm:ss.f})", closest.ElapsedSeconds, closest.Timestamp),
                    string.Format("Vout:  {0:0.000} V  (Vset: {1:0.00}V)", closest.Voltage, closest.SetVoltage),
                    string.Format("Iout:  {0:0.000} A  (Iset: {1:0.000}A)", closest.Current, closest.SetCurrent),
                    string.Format("Pout:  {0:0.000} W  | R_obc: {1}", closest.Power, rStr),
                    string.Format("Tryb:  {0} ({1})", closest.IsCv ? "CV - Stałe napięcie" : "CC - Stały prąd", closest.OutputOn ? "ON" : "OFF")
                };

            using (Font fTip = new Font("Consolas", 8.5f, FontStyle.Bold))
            {
                float boxW = 250;
                float boxH = lines.Length * 16 + 12;
                float bx = cx + 14;
                if (bx + boxW > r.Right - 6)
                    bx = cx - boxW - 14;
                float by = Math.Max(r.Top + 8, Math.Min(_mousePos.Y - boxH / 2f, r.Bottom - boxH - 8));

                RectangleF tipRect = new RectangleF(bx, by, boxW, boxH);
                using (Brush bg = new SolidBrush(Color.FromArgb(235, 20, 27, 40)))
                using (Pen border = new Pen(Color.FromArgb(90, 115, 150), 1f))
                {
                    g.FillRectangle(bg, tipRect);
                    g.DrawRectangle(border, bx, by, boxW, boxH);
                }

                Color[] lineColors = new Color[]
                {
                    Color.FromArgb(203, 213, 225),
                    VoltageColor,
                    CurrentColor,
                    PowerColor,
                    closest.IsCv ? Color.FromArgb(52, 211, 153) : Color.FromArgb(251, 146, 60)
                };

                for (int i = 0; i < lines.Length; i++)
                {
                    using (Brush bLine = new SolidBrush(lineColors[i]))
                    {
                        g.DrawString(lines[i], fTip, bLine, bx + 8, by + 6 + i * 16);
                    }
                }
            }
        }

        private static void DrawMarkerDot(Graphics g, float x, float y, Color c)
        {
            using (Brush bOuter = new SolidBrush(Color.FromArgb(80, c)))
            using (Brush bInner = new SolidBrush(c))
            {
                g.FillEllipse(bOuter, x - 6, y - 6, 12, 12);
                g.FillEllipse(bInner, x - 3.5f, y - 3.5f, 7, 7);
            }
        }

        private static float TimeToX(double t, double tStart, double tSpan, Rectangle r)
        {
            double frac = (t - tStart) / Math.Max(0.001, tSpan);
            return (float)(r.Left + frac * r.Width);
        }

        private static float ValueToY(float val, float minVal, float maxVal, Rectangle r)
        {
            float span = Math.Max(0.001f, maxVal - minVal);
            float frac = (val - minVal) / span;
            frac = Math.Max(0f, Math.Min(1f, frac));
            return r.Bottom - (frac * r.Height);
        }
    }
}
