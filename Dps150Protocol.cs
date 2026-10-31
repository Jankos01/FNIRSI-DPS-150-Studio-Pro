using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace FnirsiDps150Studio
{
    public enum ProtectionStatus : byte
    {
        Normal = 0,
        OVP = 1,
        OCP = 2,
        OPP = 3,
        OTP = 4,
        LVP = 5,
        ShortCircuit = 6,
        ReversePolarity = 7
    }

    public struct MemoryPreset
    {
        public float Voltage;
        public float Current;
    }

    public class Dps150State
    {
        public float InputVoltage;
        public float SetVoltage = 5.00f;
        public float SetCurrent = 1.000f;
        public float OutputVoltage;
        public float OutputCurrent;
        public float OutputPower;
        public float Temperature = 25.0f;

        public MemoryPreset[] Presets = new MemoryPreset[]
        {
            new MemoryPreset { Voltage = 3.30f, Current = 1.000f },
            new MemoryPreset { Voltage = 5.00f, Current = 2.000f },
            new MemoryPreset { Voltage = 9.00f, Current = 2.000f },
            new MemoryPreset { Voltage = 12.00f, Current = 3.000f },
            new MemoryPreset { Voltage = 19.00f, Current = 4.000f },
            new MemoryPreset { Voltage = 24.00f, Current = 5.000f }
        };

        public float OvpThreshold = 30.50f;
        public float OcpThreshold = 5.100f;
        public float OppThreshold = 152.0f;
        public float OtpThreshold = 85.0f;
        public float LvpThreshold = 4.50f;

        public byte Brightness = 10;
        public byte Volume = 5;
        public bool MeteringEnabled = true;

        public float CapacityAh;
        public float EnergyWh;

        public bool OutputEnabled;
        public ProtectionStatus Protection = ProtectionStatus.Normal;
        public bool IsConstantVoltage = true; // true = CV, false = CC

        public string ModelName = "DPS-150";
        public string HardwareVersion = "V1.0";
        public string FirmwareVersion = "V1.2";

        public float MaxVoltageLimit = 30.0f;
        public float MaxCurrentLimit = 5.0f;

        public float RippleVppMv = 0.0f;
        public float RippleIppMa = 0.0f;
        public double OutputOnSeconds = 0.0;

        public DateTime LastUpdated = DateTime.Now;

        public Dps150State Clone()
        {
            var copy = (Dps150State)this.MemberwiseClone();
            copy.Presets = new MemoryPreset[6];
            Array.Copy(this.Presets, copy.Presets, 6);
            return copy;
        }
    }

    public class TelemetrySample
    {
        public DateTime Timestamp;
        public double ElapsedSeconds;
        public float Voltage;
        public float Current;
        public float Power;
        public float SetVoltage;
        public float SetCurrent;
        public bool IsCv;
        public bool OutputOn;
    }

    public class PortInfo
    {
        public string PortName { get; set; }
        public string Description { get; set; }
        public bool IsDps150Likely { get; set; }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Description) ? PortName : string.Format("{0} ({1})", PortName, Description);
        }
    }

    public class Dps150Driver : IDisposable
    {
        // Protocol Constants
        public const byte HEADER_OUT = 0xF1;
        public const byte HEADER_IN = 0xF0;

        public const byte CMD_GET = 0xA1;
        public const byte CMD_SET = 0xB1;
        public const byte CMD_CONN = 0xC1;

        public const byte TYPE_VIN = 0xC0;        // 192
        public const byte TYPE_VSET = 0xC1;       // 193
        public const byte TYPE_ISET = 0xC2;       // 194
        public const byte TYPE_VIP_OUT = 0xC3;    // 195 (12 bytes: Vout, Iout, Pout)
        public const byte TYPE_TEMP = 0xC4;       // 196
        public const byte TYPE_M1_V = 0xC5;       // 197 .. 208 (M1_V..M6_I)
        public const byte TYPE_OVP = 0xD1;        // 209
        public const byte TYPE_OCP = 0xD2;        // 210
        public const byte TYPE_OPP = 0xD3;        // 211
        public const byte TYPE_OTP = 0xD4;        // 212
        public const byte TYPE_LVP = 0xD5;        // 213
        public const byte TYPE_BRIGHTNESS = 0xD6; // 214
        public const byte TYPE_VOLUME = 0xD7;     // 215
        public const byte TYPE_METERING = 0xD8;   // 216
        public const byte TYPE_CAP_AH = 0xD9;     // 217
        public const byte TYPE_ENE_WH = 0xDA;     // 218
        public const byte TYPE_OUT_EN = 0xDB;     // 219
        public const byte TYPE_PROT = 0xDC;       // 220
        public const byte TYPE_MODE = 0xDD;       // 221
        public const byte TYPE_MODEL = 0xDE;      // 222
        public const byte TYPE_HW_VER = 0xDF;     // 223
        public const byte TYPE_FW_VER = 0xE0;     // 224
        public const byte TYPE_MAX_V = 0xE1;      // 225
        public const byte TYPE_MAX_I = 0xE2;      // 226
        public const byte TYPE_ALL = 0xFF;        // 255

        private SerialPort _serialPort;
        private readonly object _stateLock = new object();
        private readonly Dps150State _state = new Dps150State();
        private readonly List<byte> _rxBuffer = new List<byte>(2048);
        private readonly ConcurrentQueue<byte[]> _txQueue = new ConcurrentQueue<byte[]>();

        private Thread _workerThread;
        private volatile bool _running;
        private volatile bool _isConnected;
        private volatile bool _isSimulated;
        private float _simulatedLoadOhms = 10.0f; // Virtual load resistance in simulator mode
        private DateTime _startTime = DateTime.Now;
        private DateTime _lastSimTick = DateTime.Now;
        private DateTime _lastHwTelemetryTick = DateTime.Now;
        private float _swAccumulatedAh = 0f;
        private float _swAccumulatedWh = 0f;
        private readonly List<float> _recentVoltages = new List<float>(24);
        private readonly List<float> _recentCurrents = new List<float>(24);

        public event Action<Dps150State, TelemetrySample> OnTelemetryUpdated;
        public event Action<string, bool> OnPacketLog; // (hexString, isTx)
        public event Action<string> OnConnectionError;
        public event Action<bool, string> OnConnectionChanged;

        public bool IsConnected { get { return _isConnected; } }
        public bool IsSimulated { get { return _isSimulated; } }
        public float SimulatedLoadOhms
        {
            get { return _simulatedLoadOhms; }
            set { _simulatedLoadOhms = Math.Max(0.1f, value); }
        }

        public Dps150State GetStateSnapshot()
        {
            lock (_stateLock)
            {
                return _state.Clone();
            }
        }

        public static List<PortInfo> DiscoverPorts()
        {
            var result = new List<PortInfo>();
            var dpsPorts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var usbSerPorts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // 1. Check VID_2E3C (AT32 Virtual COM Port used by FNIRSI DPS-150)
                using (var usbKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB"))
                {
                    if (usbKey != null)
                    {
                        foreach (var subName in usbKey.GetSubKeyNames())
                        {
                            if (subName.IndexOf("VID_2E3C", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                using (var devKey = usbKey.OpenSubKey(subName))
                                {
                                    if (devKey == null) continue;
                                    foreach (var instName in devKey.GetSubKeyNames())
                                    {
                                        using (var instKey = devKey.OpenSubKey(instName))
                                        using (var paramKey = instKey != null ? instKey.OpenSubKey("Device Parameters") : null)
                                        {
                                            if (paramKey != null)
                                            {
                                                var portName = paramKey.GetValue("PortName") as string;
                                                if (!string.IsNullOrEmpty(portName))
                                                    dpsPorts.Add(portName);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // 2. Check SERIALCOMM for USBSER devices
                using (var commKey = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM"))
                {
                    if (commKey != null)
                    {
                        foreach (var valName in commKey.GetValueNames())
                        {
                            var portVal = commKey.GetValue(valName) as string;
                            if (!string.IsNullOrEmpty(portVal) && valName.IndexOf("USBSER", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                usbSerPorts.Add(portVal);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore registry permission issues
            }

            string[] names = SerialPort.GetPortNames();
            Array.Sort(names, StringComparer.OrdinalIgnoreCase);

            foreach (var name in names)
            {
                bool isDps = dpsPorts.Contains(name);
                bool isUsb = usbSerPorts.Contains(name);
                string desc = isDps ? "FNIRSI DPS-150 USB" : (isUsb ? "USB Serial" : "Port COM");
                result.Add(new PortInfo
                {
                    PortName = name,
                    Description = desc,
                    IsDps150Likely = isDps || isUsb
                });
            }

            // Sort so likely DPS-150 ports appear first
            result.Sort((a, b) =>
            {
                if (a.IsDps150Likely != b.IsDps150Likely)
                    return b.IsDps150Likely.CompareTo(a.IsDps150Likely);
                return string.Compare(a.PortName, b.PortName, StringComparison.OrdinalIgnoreCase);
            });

            return result;
        }

        public bool Connect(string portName, int baudRate = 115200)
        {
            Disconnect();

            try
            {
                _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = 200,
                    WriteTimeout = 500,
                    RtsEnable = true,
                    DtrEnable = true,
                    ReceivedBytesThreshold = 1
                };

                _serialPort.Open();
                _rxBuffer.Clear();
                byte[] dummy;
                while (_txQueue.TryDequeue(out dummy)) { }

                _isSimulated = false;
                _isConnected = true;
                _running = true;
                _startTime = DateTime.Now;
                _lastHwTelemetryTick = DateTime.Now;

                _workerThread = new Thread(WorkerLoop)
                {
                    IsBackground = true,
                    Name = "DPS150_SerialWorker"
                };
                _workerThread.Start();

                // Handshake sequence:
                // 1. Enable session streaming (0xC1, type 0x00, data [0x01])
                EnqueuePacket(BuildPacket(CMD_CONN, 0x00, new byte[] { 0x01 }));
                // 2. Query Model, HW, FW, Input Limits (MaxV/MaxI) and Full State (0xFF)
                EnqueuePacket(BuildPacket(CMD_GET, TYPE_MODEL, new byte[] { 0x00 }));
                EnqueuePacket(BuildPacket(CMD_GET, TYPE_HW_VER, new byte[] { 0x00 }));
                EnqueuePacket(BuildPacket(CMD_GET, TYPE_FW_VER, new byte[] { 0x00 }));
                EnqueuePacket(BuildPacket(CMD_GET, TYPE_MAX_V, new byte[] { 0x00 }));
                EnqueuePacket(BuildPacket(CMD_GET, TYPE_MAX_I, new byte[] { 0x00 }));
                EnqueuePacket(BuildPacket(CMD_GET, TYPE_ALL, new byte[] { 0x00 }));
                // 3. Automatically enable hardware Ah/Wh metering (0xD8 = 1) on DPS-150!
                EnqueuePacket(BuildPacket(CMD_SET, TYPE_METERING, new byte[] { 0x01 }));

                var handler = OnConnectionChanged;
                if (handler != null) handler(true, string.Format("Połączono z {0} ({1} bps)", portName, baudRate));
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                string msg = string.Format(
                    "Odmowa dostępu do portu {0}.\n\nUpewnij się, że oryginalny program FNIRSI (lub inny terminal) został zamknięty i nie blokuje portu {0}.",
                    portName);
                var err = OnConnectionError;
                if (err != null) err(msg);
                return false;
            }
            catch (Exception ex)
            {
                string msg = string.Format("Nie udało się otworzyć portu {0}: {1}", portName, ex.Message);
                var err = OnConnectionError;
                if (err != null) err(msg);
                return false;
            }
        }

        public void StartSimulation()
        {
            Disconnect();
            lock (_stateLock)
            {
                _state.InputVoltage = 20.05f; // USB-PD 20V
                _state.SetVoltage = 5.00f;
                _state.SetCurrent = 1.500f;
                _state.MaxVoltageLimit = 30.0f;
                _state.MaxCurrentLimit = 5.0f;
                _state.ModelName = "DPS-150 (SIM)";
                _state.HardwareVersion = "V1.0";
                _state.FirmwareVersion = "V1.2";
                _state.Protection = ProtectionStatus.Normal;
            }

            _isSimulated = true;
            _isConnected = true;
            _running = true;
            _startTime = DateTime.Now;
            _lastSimTick = DateTime.Now;

            _workerThread = new Thread(SimulatorLoop)
            {
                IsBackground = true,
                Name = "DPS150_SimWorker"
            };
            _workerThread.Start();

            var handler = OnConnectionChanged;
            if (handler != null) handler(true, "Tryb Symulatora DPS-150 (Aktywny)");
        }

        public void Disconnect()
        {
            _running = false;

            if (_serialPort != null)
            {
                try
                {
                    if (_serialPort.IsOpen)
                    {
                        // Gracefully unlock the physical panel on DPS-150!
                        byte[] unlockFrame = BuildPacket(CMD_CONN, 0x00, new byte[] { 0x00 });
                        _serialPort.Write(unlockFrame, 0, unlockFrame.Length);
                        LogPacket(unlockFrame, true);
                        Thread.Sleep(40);
                        _serialPort.Close();
                    }
                }
                catch
                {
                    // Ignore disconnect errors
                }
                finally
                {
                    try { _serialPort.Dispose(); } catch { }
                    _serialPort = null;
                }
            }

            if (_workerThread != null && _workerThread.IsAlive)
            {
                _workerThread.Join(300);
                _workerThread = null;
            }

            bool wasConnected = _isConnected;
            _isConnected = false;
            _isSimulated = false;

            if (wasConnected)
            {
                var handler = OnConnectionChanged;
                if (handler != null) handler(false, "Rozłączono (panel zasilacza odblokowany)");
            }
        }

        // --- High-Level Commands ---

        public void SetVoltage(float volts)
        {
            volts = Clamp(volts, 0f, 30.0f);
            lock (_stateLock) { _state.SetVoltage = volts; }
            if (!_isSimulated)
                EnqueuePacket(BuildFloatPacket(CMD_SET, TYPE_VSET, volts));
        }

        public void SetCurrent(float amps)
        {
            amps = Clamp(amps, 0f, 5.0f);
            lock (_stateLock) { _state.SetCurrent = amps; }
            if (!_isSimulated)
                EnqueuePacket(BuildFloatPacket(CMD_SET, TYPE_ISET, amps));
        }

        public void SetOutputEnabled(bool enabled)
        {
            lock (_stateLock)
            {
                _state.OutputEnabled = enabled;
                if (enabled)
                {
                    _lastHwTelemetryTick = DateTime.Now;
                    _state.MeteringEnabled = true;
                    if (_state.Protection != ProtectionStatus.Normal)
                        _state.Protection = ProtectionStatus.Normal;
                }
            }
            if (!_isSimulated)
            {
                EnqueuePacket(BuildPacket(CMD_SET, TYPE_OUT_EN, new byte[] { enabled ? (byte)1 : (byte)0 }));
                if (enabled)
                {
                    // Ensure hardware Wh/Ah counter (0xD8) is running when output turns ON
                    EnqueuePacket(BuildPacket(CMD_SET, TYPE_METERING, new byte[] { 0x01 }));
                }
            }
        }

        public void SetProtections(float ovp, float ocp, float opp, float otp, float lvp)
        {
            ovp = Clamp(ovp, 0.1f, 32.0f);
            ocp = Clamp(ocp, 0.01f, 5.5f);
            opp = Clamp(opp, 0.5f, 160.0f);
            otp = Clamp(otp, 30.0f, 99.0f);
            lvp = Clamp(lvp, 3.0f, 30.0f);

            lock (_stateLock)
            {
                _state.OvpThreshold = ovp;
                _state.OcpThreshold = ocp;
                _state.OppThreshold = opp;
                _state.OtpThreshold = otp;
                _state.LvpThreshold = lvp;
            }

            if (!_isSimulated)
            {
                EnqueuePacket(BuildFloatPacket(CMD_SET, TYPE_OVP, ovp));
                EnqueuePacket(BuildFloatPacket(CMD_SET, TYPE_OCP, ocp));
                EnqueuePacket(BuildFloatPacket(CMD_SET, TYPE_OPP, opp));
                EnqueuePacket(BuildFloatPacket(CMD_SET, TYPE_OTP, otp));
                EnqueuePacket(BuildFloatPacket(CMD_SET, TYPE_LVP, lvp));
            }
        }

        public void SaveMemoryPreset(int groupIndex1To6, float voltage, float current)
        {
            if (groupIndex1To6 < 1 || groupIndex1To6 > 6) return;
            voltage = Clamp(voltage, 0f, 30.0f);
            current = Clamp(current, 0f, 5.0f);

            lock (_stateLock)
            {
                _state.Presets[groupIndex1To6 - 1] = new MemoryPreset { Voltage = voltage, Current = current };
            }

            if (!_isSimulated)
            {
                byte vReg = (byte)(TYPE_M1_V + (groupIndex1To6 - 1) * 2);
                byte iReg = (byte)(vReg + 1);
                EnqueuePacket(BuildFloatPacket(CMD_SET, vReg, voltage));
                EnqueuePacket(BuildFloatPacket(CMD_SET, iReg, current));
            }
        }

        public void SetBrightness(byte level)
        {
            level = (byte)Math.Min((int)level, 14);
            lock (_stateLock) { _state.Brightness = level; }
            if (!_isSimulated)
                EnqueuePacket(BuildPacket(CMD_SET, TYPE_BRIGHTNESS, new byte[] { level }));
        }

        public void SetVolume(byte level)
        {
            level = (byte)Math.Min((int)level, 10);
            lock (_stateLock) { _state.Volume = level; }
            if (!_isSimulated)
                EnqueuePacket(BuildPacket(CMD_SET, TYPE_VOLUME, new byte[] { level }));
        }

        public void SetMeteringEnabled(bool enabled)
        {
            lock (_stateLock) { _state.MeteringEnabled = enabled; }
            if (!_isSimulated)
                EnqueuePacket(BuildPacket(CMD_SET, TYPE_METERING, new byte[] { enabled ? (byte)1 : (byte)0 }));
        }

        public void ResetLocalCounters()
        {
            lock (_stateLock)
            {
                _state.CapacityAh = 0f;
                _state.EnergyWh = 0f;
                _state.OutputOnSeconds = 0.0;
                _swAccumulatedAh = 0f;
                _swAccumulatedWh = 0f;
                _recentVoltages.Clear();
                _recentCurrents.Clear();
                _state.RippleVppMv = 0f;
                _state.RippleIppMa = 0f;
                _lastHwTelemetryTick = DateTime.Now;
            }
            if (!_isSimulated)
            {
                // Toggle metering off and on to reset on hardware if supported
                EnqueuePacket(BuildPacket(CMD_SET, TYPE_METERING, new byte[] { 0x00 }));
                EnqueuePacket(BuildPacket(CMD_SET, TYPE_METERING, new byte[] { 0x01 }));
            }
        }

        public void RefreshAllState()
        {
            if (!_isSimulated && _isConnected)
            {
                EnqueuePacket(BuildPacket(CMD_GET, TYPE_MODEL, new byte[] { 0x00 }));
                EnqueuePacket(BuildPacket(CMD_GET, TYPE_HW_VER, new byte[] { 0x00 }));
                EnqueuePacket(BuildPacket(CMD_GET, TYPE_FW_VER, new byte[] { 0x00 }));
                EnqueuePacket(BuildPacket(CMD_GET, TYPE_MAX_V, new byte[] { 0x00 }));
                EnqueuePacket(BuildPacket(CMD_GET, TYPE_MAX_I, new byte[] { 0x00 }));
                EnqueuePacket(BuildPacket(CMD_GET, TYPE_ALL, new byte[] { 0x00 }));
            }
        }

        public void SendRawCustomPacket(byte cmd, byte type, byte[] payload)
        {
            if (!_isSimulated && _isConnected)
            {
                EnqueuePacket(BuildPacket(cmd, type, payload ?? new byte[0]));
            }
        }

        // --- Packet Construction & Parsing ---

        public static byte[] BuildFloatPacket(byte cmd, byte type, float value)
        {
            byte[] floatBytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(floatBytes);
            return BuildPacket(cmd, type, floatBytes);
        }

        public static byte[] BuildPacket(byte cmd, byte type, byte[] data)
        {
            if (data == null) data = new byte[0];
            byte len = (byte)data.Length;
            int sum = type + len;
            for (int i = 0; i < len; i++)
                sum += data[i];
            byte checksum = (byte)(sum & 0xFF);

            byte[] frame = new byte[4 + len + 1];
            frame[0] = HEADER_OUT;
            frame[1] = cmd;
            frame[2] = type;
            frame[3] = len;
            if (len > 0)
                Buffer.BlockCopy(data, 0, frame, 4, len);
            frame[4 + len] = checksum;
            return frame;
        }

        private void EnqueuePacket(byte[] packet)
        {
            _txQueue.Enqueue(packet);
        }

        private void WorkerLoop()
        {
            byte[] readChunk = new byte[512];
            DateTime lastKeepAlive = DateTime.Now;

            while (_running && _serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    // 1. Send queued outgoing packets
                    byte[] pkt;
                    while (_txQueue.TryDequeue(out pkt))
                    {
                        _serialPort.Write(pkt, 0, pkt.Length);
                        LogPacket(pkt, true);
                        Thread.Sleep(15); // Brief spacing so DPS-150 MCU processes every command cleanly
                    }

                    // 2. Read incoming telemetry bytes
                    int avail = _serialPort.BytesToRead;
                    if (avail > 0)
                    {
                        int toRead = Math.Min(avail, readChunk.Length);
                        int read = _serialPort.Read(readChunk, 0, toRead);
                        if (read > 0)
                        {
                            for (int i = 0; i < read; i++)
                                _rxBuffer.Add(readChunk[i]);
                            ProcessRxBuffer();
                        }
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }

                    // Periodic full state sync every 5 seconds if idle
                    if ((DateTime.Now - lastKeepAlive).TotalSeconds >= 5.0)
                    {
                        lastKeepAlive = DateTime.Now;
                        if (_txQueue.IsEmpty)
                        {
                            EnqueuePacket(BuildPacket(CMD_GET, TYPE_ALL, new byte[] { 0x00 }));
                        }
                    }
                }
                catch (TimeoutException)
                {
                    // Normal read timeout
                }
                catch (Exception)
                {
                    if (_running)
                        Thread.Sleep(50);
                }
            }
        }

        private void ProcessRxBuffer()
        {
            while (_rxBuffer.Count >= 5)
            {
                // Find start byte 0xF0
                int startIdx = _rxBuffer.IndexOf(HEADER_IN);
                if (startIdx < 0)
                {
                    _rxBuffer.Clear();
                    return;
                }
                if (startIdx > 0)
                {
                    _rxBuffer.RemoveRange(0, startIdx);
                    if (_rxBuffer.Count < 5)
                        return;
                }

                byte cmd = _rxBuffer[1];
                byte type = _rxBuffer[2];
                byte len = _rxBuffer[3];
                int fullFrameLen = 4 + len + 1;

                if (_rxBuffer.Count < fullFrameLen)
                {
                    // Wait for remaining bytes of this packet
                    return;
                }

                // Verify checksum
                int sum = type + len;
                for (int i = 0; i < len; i++)
                    sum += _rxBuffer[4 + i];
                byte expectedChecksum = (byte)(sum & 0xFF);
                byte actualChecksum = _rxBuffer[4 + len];

                if (expectedChecksum != actualChecksum)
                {
                    // Corrupted byte stream; skip this 0xF0 byte and resync
                    _rxBuffer.RemoveAt(0);
                    continue;
                }

                byte[] frame = new byte[fullFrameLen];
                _rxBuffer.CopyTo(0, frame, 0, fullFrameLen);
                _rxBuffer.RemoveRange(0, fullFrameLen);

                LogPacket(frame, false);
                ParseIncomingFrame(cmd, type, frame, 4, len);
            }
        }

        private void ParseIncomingFrame(byte cmd, byte type, byte[] buf, int offset, int len)
        {
            bool triggerTelemetryEvent = false;

            lock (_stateLock)
            {
                _state.LastUpdated = DateTime.Now;

                switch (type)
                {
                    case TYPE_VIN: // 192
                        if (len >= 4) _state.InputVoltage = ReadFloat(buf, offset);
                        break;

                    case TYPE_VSET: // 193
                        if (len >= 4) _state.SetVoltage = ReadFloat(buf, offset);
                        break;

                    case TYPE_ISET: // 194
                        if (len >= 4) _state.SetCurrent = ReadFloat(buf, offset);
                        break;

                    case TYPE_VIP_OUT: // 195 (12 bytes: Vout, Iout, Pout)
                        if (len >= 12)
                        {
                            DateTime now = DateTime.Now;
                            double dtHours = (now - _lastHwTelemetryTick).TotalHours;
                            _lastHwTelemetryTick = now;

                            _state.OutputVoltage = ReadFloat(buf, offset);
                            _state.OutputCurrent = ReadFloat(buf, offset + 4);
                            _state.OutputPower = ReadFloat(buf, offset + 8);

                            // High-precision real-time software integration alongside hardware metering
                            if (_state.OutputEnabled && _state.MeteringEnabled && dtHours > 0.0 && dtHours < 0.01)
                            {
                                _swAccumulatedAh += (float)(_state.OutputCurrent * dtHours);
                                _swAccumulatedWh += (float)(_state.OutputPower * dtHours);
                                if (_swAccumulatedAh > _state.CapacityAh)
                                    _state.CapacityAh = _swAccumulatedAh;
                                if (_swAccumulatedWh > _state.EnergyWh)
                                    _state.EnergyWh = _swAccumulatedWh;
                            }

                            UpdateRippleAndRuntimeLocked(dtHours);

                            triggerTelemetryEvent = true;
                        }
                        break;

                    case TYPE_TEMP: // 196
                        if (len >= 4) _state.Temperature = ReadFloat(buf, offset);
                        break;

                    case 197:
                    case 198:
                    case 199:
                    case 200:
                    case 201:
                    case 202:
                    case 203:
                    case 204:
                    case 205:
                    case 206:
                    case 207:
                    case 208:
                        if (len >= 4)
                        {
                            int idx = type - TYPE_M1_V;
                            int group = idx / 2;
                            bool isCurrent = (idx % 2) == 1;
                            float val = ReadFloat(buf, offset);
                            if (isCurrent)
                                _state.Presets[group].Current = val;
                            else
                                _state.Presets[group].Voltage = val;
                        }
                        break;

                    case TYPE_OVP: // 209
                        if (len >= 4) _state.OvpThreshold = ReadFloat(buf, offset);
                        break;
                    case TYPE_OCP: // 210
                        if (len >= 4) _state.OcpThreshold = ReadFloat(buf, offset);
                        break;
                    case TYPE_OPP: // 211
                        if (len >= 4) _state.OppThreshold = ReadFloat(buf, offset);
                        break;
                    case TYPE_OTP: // 212
                        if (len >= 4) _state.OtpThreshold = ReadFloat(buf, offset);
                        break;
                    case TYPE_LVP: // 213
                        if (len >= 4) _state.LvpThreshold = ReadFloat(buf, offset);
                        break;

                    case TYPE_BRIGHTNESS: // 214
                        if (len >= 1) _state.Brightness = buf[offset];
                        break;
                    case TYPE_VOLUME: // 215
                        if (len >= 1) _state.Volume = buf[offset];
                        break;
                    case TYPE_METERING: // 216
                        if (len >= 1) _state.MeteringEnabled = buf[offset] != 0;
                        break;

                    case TYPE_CAP_AH: // 217
                        if (len >= 4)
                        {
                            float hwAh = ReadFloat(buf, offset);
                            if (hwAh > 0f)
                            {
                                _state.CapacityAh = Math.Max(hwAh, _swAccumulatedAh);
                                _swAccumulatedAh = _state.CapacityAh;
                            }
                        }
                        break;
                    case TYPE_ENE_WH: // 218
                        if (len >= 4)
                        {
                            float hwWh = ReadFloat(buf, offset);
                            if (hwWh > 0f)
                            {
                                _state.EnergyWh = Math.Max(hwWh, _swAccumulatedWh);
                                _swAccumulatedWh = _state.EnergyWh;
                            }
                        }
                        break;

                    case TYPE_OUT_EN: // 219
                        if (len >= 1) _state.OutputEnabled = buf[offset] != 0;
                        break;
                    case TYPE_PROT: // 220
                        if (len >= 1) _state.Protection = (ProtectionStatus)buf[offset];
                        break;
                    case TYPE_MODE: // 221
                        if (len >= 1) _state.IsConstantVoltage = buf[offset] != 0;
                        break;

                    case TYPE_MODEL: // 222
                        _state.ModelName = ReadAsciiString(buf, offset, len);
                        break;
                    case TYPE_HW_VER: // 223
                        _state.HardwareVersion = ReadAsciiString(buf, offset, len);
                        break;
                    case TYPE_FW_VER: // 224
                        _state.FirmwareVersion = ReadAsciiString(buf, offset, len);
                        break;

                    case TYPE_MAX_V: // 225
                        if (len >= 4) _state.MaxVoltageLimit = ReadFloat(buf, offset);
                        break;
                    case TYPE_MAX_I: // 226
                        if (len >= 4) _state.MaxCurrentLimit = ReadFloat(buf, offset);
                        break;

                    case TYPE_ALL: // 255 (Full 139-byte state block)
                        ParseAllBlock(buf, offset, len);
                        triggerTelemetryEvent = true;
                        break;
                }
            }

            if (triggerTelemetryEvent)
            {
                EmitTelemetrySample();
            }
        }

        private void ParseAllBlock(byte[] buf, int o, int len)
        {
            if (len < 110) return;

            _state.InputVoltage = ReadFloat(buf, o + 0);
            _state.SetVoltage = ReadFloat(buf, o + 4);
            _state.SetCurrent = ReadFloat(buf, o + 8);
            _state.OutputVoltage = ReadFloat(buf, o + 12);
            _state.OutputCurrent = ReadFloat(buf, o + 16);
            _state.OutputPower = ReadFloat(buf, o + 20);
            _state.Temperature = ReadFloat(buf, o + 24);

            for (int g = 0; g < 6; g++)
            {
                int baseOff = o + 28 + (g * 8);
                _state.Presets[g] = new MemoryPreset
                {
                    Voltage = ReadFloat(buf, baseOff),
                    Current = ReadFloat(buf, baseOff + 4)
                };
            }

            _state.OvpThreshold = ReadFloat(buf, o + 76);
            _state.OcpThreshold = ReadFloat(buf, o + 80);
            _state.OppThreshold = ReadFloat(buf, o + 84);
            _state.OtpThreshold = ReadFloat(buf, o + 88);
            _state.LvpThreshold = ReadFloat(buf, o + 92);

            _state.Brightness = buf[o + 96];
            _state.Volume = buf[o + 97];
            _state.MeteringEnabled = buf[o + 98] != 0;

            float allAh = ReadFloat(buf, o + 99);
            float allWh = ReadFloat(buf, o + 103);
            if (allAh > _state.CapacityAh) _state.CapacityAh = allAh;
            if (allWh > _state.EnergyWh) _state.EnergyWh = allWh;

            _state.OutputEnabled = buf[o + 107] != 0;
            _state.Protection = (ProtectionStatus)buf[o + 108];
            _state.IsConstantVoltage = buf[o + 109] != 0;

            if (len >= 139)
            {
                _state.MaxVoltageLimit = ReadFloat(buf, o + 131);
                _state.MaxCurrentLimit = ReadFloat(buf, o + 135);
            }
        }

        private void SimulatorLoop()
        {
            Random rng = new Random();
            while (_running && _isSimulated)
            {
                DateTime now = DateTime.Now;
                double dtHours = (now - _lastSimTick).TotalHours;
                _lastSimTick = now;

                lock (_stateLock)
                {
                    _state.InputVoltage = 24.02f + (float)(rng.NextDouble() - 0.5) * 0.03f;

                    if (_state.OutputEnabled)
                    {
                        float rLoad = Math.Max(0.1f, _simulatedLoadOhms);
                        float idealCurrent = _state.SetVoltage / rLoad;

                        if (idealCurrent <= _state.SetCurrent)
                        {
                            _state.IsConstantVoltage = true;
                            _state.OutputVoltage = _state.SetVoltage + (float)(rng.NextDouble() - 0.5) * 0.004f;
                            _state.OutputCurrent = (_state.OutputVoltage / rLoad) + (float)(rng.NextDouble() - 0.5) * 0.002f;
                        }
                        else
                        {
                            _state.IsConstantVoltage = false; // CC mode
                            _state.OutputCurrent = _state.SetCurrent + (float)(rng.NextDouble() - 0.5) * 0.002f;
                            _state.OutputVoltage = _state.OutputCurrent * rLoad;
                        }

                        _state.OutputVoltage = Math.Max(0f, _state.OutputVoltage);
                        _state.OutputCurrent = Math.Max(0f, _state.OutputCurrent);
                        _state.OutputPower = _state.OutputVoltage * _state.OutputCurrent;

                        // Check protections
                        if (_state.OutputVoltage > _state.OvpThreshold)
                        {
                            _state.Protection = ProtectionStatus.OVP;
                            _state.OutputEnabled = false;
                        }
                        else if (_state.OutputCurrent > _state.OcpThreshold)
                        {
                            _state.Protection = ProtectionStatus.OCP;
                            _state.OutputEnabled = false;
                        }
                        else if (_state.OutputPower > _state.OppThreshold)
                        {
                            _state.Protection = ProtectionStatus.OPP;
                            _state.OutputEnabled = false;
                        }

                        if (_state.MeteringEnabled && dtHours > 0)
                        {
                            _state.CapacityAh += (float)(_state.OutputCurrent * dtHours);
                            _state.EnergyWh += (float)(_state.OutputPower * dtHours);
                        }

                        float targetTemp = 27.0f + (_state.OutputPower * 0.22f);
                        _state.Temperature += (targetTemp - _state.Temperature) * 0.05f;
                    }
                    else
                    {
                        _state.OutputVoltage = 0.000f;
                        _state.OutputCurrent = 0.000f;
                        _state.OutputPower = 0.000f;
                        _state.IsConstantVoltage = true;
                        _state.Temperature += (26.5f - _state.Temperature) * 0.05f;
                    }
                    UpdateRippleAndRuntimeLocked(dtHours);
                    _state.LastUpdated = now;
                }

                EmitTelemetrySample();
                Thread.Sleep(100); // 10 Hz update rate
            }
        }

        private void UpdateRippleAndRuntimeLocked(double dtHours)
        {
            if (_state.OutputEnabled)
            {
                if (dtHours > 0.0 && dtHours < 0.01)
                {
                    _state.OutputOnSeconds += dtHours * 3600.0;
                }

                _recentVoltages.Add(_state.OutputVoltage);
                if (_recentVoltages.Count > 20)
                    _recentVoltages.RemoveAt(0);

                _recentCurrents.Add(_state.OutputCurrent);
                if (_recentCurrents.Count > 20)
                    _recentCurrents.RemoveAt(0);

                if (_recentVoltages.Count >= 2)
                {
                    float minV = _recentVoltages[0], maxV = _recentVoltages[0];
                    for (int i = 1; i < _recentVoltages.Count; i++)
                    {
                        float v = _recentVoltages[i];
                        if (v < minV) minV = v;
                        if (v > maxV) maxV = v;
                    }
                    _state.RippleVppMv = Math.Max(0f, (maxV - minV) * 1000f);
                }

                if (_recentCurrents.Count >= 2)
                {
                    float minI = _recentCurrents[0], maxI = _recentCurrents[0];
                    for (int i = 1; i < _recentCurrents.Count; i++)
                    {
                        float c = _recentCurrents[i];
                        if (c < minI) minI = c;
                        if (c > maxI) maxI = c;
                    }
                    _state.RippleIppMa = Math.Max(0f, (maxI - minI) * 1000f);
                }
            }
            else
            {
                _recentVoltages.Clear();
                _recentCurrents.Clear();
                _state.RippleVppMv = 0f;
                _state.RippleIppMa = 0f;
            }
        }

        private void EmitTelemetrySample()
        {
            Dps150State snap;
            TelemetrySample sample;
            lock (_stateLock)
            {
                snap = _state.Clone();
                DateTime now = DateTime.Now;
                sample = new TelemetrySample
                {
                    Timestamp = now,
                    ElapsedSeconds = (now - _startTime).TotalSeconds,
                    Voltage = snap.OutputVoltage,
                    Current = snap.OutputCurrent,
                    Power = snap.OutputPower,
                    SetVoltage = snap.SetVoltage,
                    SetCurrent = snap.SetCurrent,
                    IsCv = snap.IsConstantVoltage,
                    OutputOn = snap.OutputEnabled
                };
            }

            var handler = OnTelemetryUpdated;
            if (handler != null)
            {
                handler(snap, sample);
            }
        }

        private void LogPacket(byte[] pkt, bool isTx)
        {
            var handler = OnPacketLog;
            if (handler == null) return;

            StringBuilder sb = new StringBuilder(pkt.Length * 3);
            for (int i = 0; i < pkt.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(pkt[i].ToString("X2"));
            }
            handler(sb.ToString(), isTx);
        }

        private static float ReadFloat(byte[] buf, int offset)
        {
            if (offset + 4 > buf.Length) return 0f;
            float val = BitConverter.ToSingle(buf, offset);
            if (float.IsNaN(val) || float.IsInfinity(val)) return 0f;
            return val;
        }

        private static string ReadAsciiString(byte[] buf, int offset, int len)
        {
            if (len <= 0 || offset + len > buf.Length) return string.Empty;
            int actualLen = 0;
            while (actualLen < len && buf[offset + actualLen] != 0)
                actualLen++;
            return Encoding.ASCII.GetString(buf, offset, actualLen).Trim();
        }

        private static float Clamp(float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
