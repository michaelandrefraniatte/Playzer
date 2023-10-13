using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using WebView2 = Microsoft.Web.WebView2.WinForms.WebView2;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Threading.Tasks;
using WinformsVisualization.Visualization;
using System.Text;
using CSCore;

namespace Playzer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        [DllImport("user32.dll")]
        internal static extern bool SendMessage(IntPtr hWnd, Int32 msg, Int32 wParam, Int32 lParam);
        static Int32 WM_SYSCOMMAND = 0x0112;
        static Int32 SC_RESTORE = 0xF120;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("USER32.DLL")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);
        [DllImport("user32.dll")]
        static extern bool DrawMenuBar(IntPtr hWnd);
        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        public static extern uint TimeBeginPeriod(uint ms);
        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        public static extern uint TimeEndPeriod(uint ms);
        [DllImport("ntdll.dll", EntryPoint = "NtSetTimerResolution")]
        public static extern void NtSetTimerResolution(uint DesiredResolution, bool SetResolution, ref uint CurrentResolution);
        public static uint CurrentResolution = 0;
        private static string WINDOW_NAME = "";
        private const int GWL_STYLE = -16;
        private const uint WS_BORDER = 0x00800000;
        private const uint WS_CAPTION = 0x00C00000;
        private const uint WS_SYSMENU = 0x00080000;
        private const uint WS_MINIMIZEBOX = 0x00020000;
        private const uint WS_MAXIMIZEBOX = 0x00010000;
        private const uint WS_OVERLAPPED = 0x00000000;
        private const uint WS_POPUP = 0x80000000;
        private const uint WS_TABSTOP = 0x00010000;
        private const uint WS_VISIBLE = 0x10000000;
        private static int width, height;
        private static bool f11switch = false;
        public int numBars = 100;
        public float[] barData = new float[100];
        public int minFreq = 0;
        public int maxFreq = 23000;
        public int barSpacing = 0;
        public bool logScale = true;
        public bool isAverage = false;
        public float highScaleAverage = 1000f;
        public float highScaleNotAverage = 1000f;
        public LineSpectrum lineSpectrum;
        public CSCore.SoundIn.WasapiCapture capture;
        public CSCore.DSP.FftSize fftSize;
        public float[] fftBuffer;
        public BasicSpectrumProvider spectrumProvider;
        public CSCore.IWaveSource finalSource;
        public string backgroundcolor = "";
        public string frequencystickscolor = "";
        public string tempsavepath = System.Reflection.Assembly.GetEntryAssembly().Location.Replace(@"file:\", "").Replace(Process.GetCurrentProcess().ProcessName + ".exe", "").Replace(@"\", "/").Replace(@"//", "") + "tempsave";
        public string savedstorage = "[]";
        private static bool closeonicon = false, handletopreviousnexttrack = false;
        private static int x, y, cx, cy;
        public WebView2 webView21 = new WebView2();
        public static bool echoboostenable = false;
        public KeyboardHook keyboardHook = new KeyboardHook();
        public static int vkCode, scanCode;
        public static bool KeyboardHookButtonDown, KeyboardHookButtonUp;
        public static bool starting = true;
        public static int[] wd = { 2, 2, 2, 2 };
        public static int[] wu = { 2, 2, 2, 2 };
        public static void valchanged(int n, bool val)
        {
            if (val)
            {
                if (wd[n] <= 1)
                {
                    wd[n] = wd[n] + 1;
                }
                wu[n] = 0;
            }
            else
            {
                if (wu[n] <= 1)
                {
                    wu[n] = wu[n] + 1;
                }
                wd[n] = 0;
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            keyboardHook.Hook += new KeyboardHook.KeyboardHookCallback(KeyboardHook_Hook);
            keyboardHook.Install();
            using (StreamReader file = new StreamReader("params.txt"))
            {
                file.ReadLine();
                handletopreviousnexttrack = bool.Parse(file.ReadLine());
                file.ReadLine();
                echoboostenable = bool.Parse(file.ReadLine());
            }
            if (echoboostenable)
                Process.Start("EchoBoost.exe");
            Task.Run(() => GetAudioByteArray());
        }
        private async void Form1_Shown(object sender, EventArgs e)
        {
            TimeBeginPeriod(1);
            NtSetTimerResolution(1, true, ref CurrentResolution);
            TrayMenuContext();
            x = this.Location.X;
            y = this.Location.Y;
            cx = this.Size.Width;
            cy = this.Size.Height;
            this.label1.Location = new Point(cx / 2 - this.label1.Size.Width / 2, cy / 2 - this.label1.Height / 2 - this.label2.Height);
            this.label2.Location = new Point(cx / 2 - this.label2.Size.Width / 2, cy / 2 - this.label2.Height / 2 + this.label2.Height);
            CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions("--disable-gpu", "--disable-gpu-compositing");
            CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, null, options);
            await webView21.EnsureCoreWebView2Async(environment); 
            webView21.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            webView21.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            webView21.CoreWebView2.Settings.AreDevToolsEnabled = true;
            using (StreamReader file = new StreamReader("playzer.txt"))
            {
                webView21.Source = new Uri(file.ReadLine());
            }
            webView21.Dock = DockStyle.Fill;
            webView21.NavigationCompleted += WebView21_NavigationCompleted;
            webView21.DefaultBackgroundColor = Color.Black;
            webView21.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
            webView21.CoreWebView2.ContextMenuRequested += CoreWebView2_ContextMenuRequested;
            this.Controls.Add(webView21);
            using (StreamReader file = new StreamReader("colors.txt"))
            {
                file.ReadLine();
                backgroundcolor = file.ReadLine();
                file.ReadLine();
                frequencystickscolor = file.ReadLine();
                file.Close();
            }
            using (System.IO.StreamWriter createdfile = new System.IO.StreamWriter(Application.StartupPath + @"\temphandle"))
            {
                createdfile.WriteLine(Process.GetCurrentProcess().MainWindowHandle);
            }
        }
        private void WebView21_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (starting)
            {
                this.Controls.Remove(label1);
                this.Controls.Remove(label2);
                this.Controls.Remove(label3);
                starting = false;
            }
        }
        private void CoreWebView2_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            CoreWebView2HttpRequestHeaders requestHeaders = e.Request.Headers;
            requestHeaders.SetHeader("Access-Control-Max-Age", "0");
            requestHeaders.SetHeader("Cache-Control", "max-age=0, public");
            requestHeaders.SetHeader("Access-Control-Allow-Origin", "*");
            requestHeaders.SetHeader("Access-Control-Allow-Methods", "POST, GET, PUT, OPTIONS, DELETE");
            requestHeaders.SetHeader("Access-Control-Allow-Headers", "x-requested-with, content-type");
        }
        private void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
            webView21.Source = new System.Uri(e.Uri);
        }
        private void CoreWebView2_ContextMenuRequested(object sender, CoreWebView2ContextMenuRequestedEventArgs e)
        {
            IList<CoreWebView2ContextMenuItem> menuList = e.MenuItems;
            CoreWebView2ContextMenuItem newItem;
            newItem = webView21.CoreWebView2.Environment.CreateContextMenuItem("Copy page Uri", null, CoreWebView2ContextMenuItemKind.Command);
            newItem.CustomItemSelected += delegate (object send, Object ex)
            {
                string pageUri = e.ContextMenuTarget.PageUri;
                System.Threading.SynchronizationContext.Current.Post((_) =>
                {
                    Clipboard.SetText(pageUri);
                    System.Diagnostics.Process.Start(pageUri);
                }, null);
            };
            menuList.Insert(menuList.Count, newItem);
            newItem = webView21.CoreWebView2.Environment.CreateContextMenuItem("Go to menu", null, CoreWebView2ContextMenuItemKind.Command);
            newItem.CustomItemSelected += delegate (object send, Object ex)
            {
                string filepath = @"file:///" + System.Reflection.Assembly.GetEntryAssembly().Location.Replace("\\", "/").Replace("Playcipe.exe", "") + "assets/index.html";
                System.Threading.SynchronizationContext.Current.Post((_) =>
                {
                    webView21.Source = new System.Uri(filepath);
                }, null);
            };
            menuList.Insert(menuList.Count, newItem);
            newItem = webView21.CoreWebView2.Environment.CreateContextMenuItem("Go to Uri", null, CoreWebView2ContextMenuItemKind.Command);
            newItem.CustomItemSelected += delegate (object send, Object ex)
            {
                string newpageUri = Microsoft.VisualBasic.Interaction.InputBox("Prompt", "Enter a new page Uri", "https://google.com", 0, 0);
                System.Threading.SynchronizationContext.Current.Post((_) =>
                {
                    if (newpageUri != "")
                        Navigate(newpageUri);
                }, null);
            };
            menuList.Insert(menuList.Count, newItem);
            newItem = webView21.CoreWebView2.Environment.CreateContextMenuItem("Back", null, CoreWebView2ContextMenuItemKind.Command);
            newItem.CustomItemSelected += delegate (object send, Object ex)
            {
                System.Threading.SynchronizationContext.Current.Post((_) =>
                {
                    string stringinject = @"
                        history.back();
                    ".Replace("\r\n", " ");
                    execScriptHelper(stringinject);
                }, null);
            };
            menuList.Insert(menuList.Count, newItem);
            newItem = webView21.CoreWebView2.Environment.CreateContextMenuItem("Forward", null, CoreWebView2ContextMenuItemKind.Command);
            newItem.CustomItemSelected += delegate (object send, Object ex)
            {
                System.Threading.SynchronizationContext.Current.Post((_) =>
                {
                    string stringinject = @"
                        history.forward();
                    ".Replace("\r\n", " ");
                    execScriptHelper(stringinject);
                }, null);
            };
            menuList.Insert(menuList.Count, newItem);
            newItem = webView21.CoreWebView2.Environment.CreateContextMenuItem("Reload", null, CoreWebView2ContextMenuItemKind.Command);
            newItem.CustomItemSelected += delegate (object send, Object ex)
            {
                System.Threading.SynchronizationContext.Current.Post((_) =>
                {
                    string stringinject = @"
                        window.location.reload(false);
                    ".Replace("\r\n", " ");
                    execScriptHelper(stringinject);
                }, null);
            };
            menuList.Insert(menuList.Count, newItem);
        }
        private void Navigate(string address)
        {
            if (String.IsNullOrEmpty(address))
                return;
            if (address.Equals("about:blank"))
                return;
            if (!address.StartsWith("http://") & !address.StartsWith("https://"))
                address = "https://" + address;
            try
            {
                webView21.Source = new System.Uri(address);
            }
            catch (System.UriFormatException)
            {
                return;
            }
        }
        public void GetAudioByteArray()
        {
            capture = new CSCore.SoundIn.WasapiLoopbackCapture();
            capture.Initialize();
            CSCore.IWaveSource source = new CSCore.Streams.SoundInSource(capture);
            fftBuffer = new float[(int)CSCore.DSP.FftSize.Fft4096];
            spectrumProvider = new BasicSpectrumProvider(capture.WaveFormat.Channels, capture.WaveFormat.SampleRate, CSCore.DSP.FftSize.Fft4096);
            lineSpectrum = new LineSpectrum(CSCore.DSP.FftSize.Fft4096)
            {
                SpectrumProvider = spectrumProvider,
                UseAverage = true,
                BarCount = numBars,
                BarSpacing = 2,
                IsXLogScale = false,
                ScalingStrategy = ScalingStrategy.Sqrt
            };
            var notificationSource = new CSCore.Streams.SingleBlockNotificationStream(source.ToSampleSource());
            notificationSource.SingleBlockRead += NotificationSource_SingleBlockRead;
            finalSource = notificationSource.ToWaveSource();
            capture.DataAvailable += Capture_DataAvailable;
            capture.Start();
        }
        public void Capture_DataAvailable(object sender, CSCore.SoundIn.DataAvailableEventArgs e)
        {
            finalSource.Read(e.Data, e.Offset, e.ByteCount);
        }
        public void NotificationSource_SingleBlockRead(object sender, CSCore.Streams.SingleBlockReadEventArgs e)
        {
            spectrumProvider.Add(e.Left, e.Right);
        }
        public float[] GetFFtData()
        {
            lock (barData)
            {
                lineSpectrum.BarCount = numBars;
                if (numBars != barData.Length)
                {
                    barData = new float[numBars];
                }
            }
            if (spectrumProvider.IsNewDataAvailable)
            {
                lineSpectrum.MinimumFrequency = minFreq;
                lineSpectrum.MaximumFrequency = maxFreq;
                lineSpectrum.IsXLogScale = logScale;
                lineSpectrum.BarSpacing = barSpacing;
                lineSpectrum.SpectrumProvider.GetFftData(fftBuffer, this);
                return lineSpectrum.GetSpectrumPoints(100.0f, fftBuffer);
            }
            else
            {
                return null;
            }
        }
        public void ComputeData()
        {
            float[] resData = GetFFtData();
            int numBars = barData.Length;
            if (resData == null)
            {
                return;
            }
            lock (barData)
            {
                for (int i = 0; i < numBars && i < resData.Length; i++)
                {
                    barData[i] = resData[i] / 100.0f;
                    if (lineSpectrum.UseAverage)
                    {
                        barData[i] = barData[i] + highScaleAverage * (float)Math.Sqrt(i / (numBars + 0.0f)) * barData[i];
                    }
                    else
                    {
                        barData[i] = barData[i] + highScaleNotAverage * (float)Math.Sqrt(i / (numBars + 0.0f)) * barData[i];
                    }
                }
            }
        }
        private async void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                ComputeData();
                string stringinject = @"
                        try {
                            var parentcanvas = document.getElementById('parentcanvas');
                            if (parentcanvas == null) {
                                parentcanvas = document.createElement('div');
                                document.body.insertBefore(parentcanvas, document.body.firstChild);
                                parentcanvas.id = 'parentcanvas';
                            }
                            parentcanvas.style.position = 'relative';
                            parentcanvas.style.display = 'inline-block';
                            parentcanvas.style.width = '100%';
                            parentcanvas.style.height = '200px';
                            parentcanvas.style.left = '0px';
                            parentcanvas.style.top = '0px';
                            parentcanvas.style.backgroundColor = 'backgroundcolor';
                            var canvas = document.getElementsByTagName('canvas');
                            if (canvas.length == 0) {
                                canvas = document.createElement('canvas');
                                parentcanvas.append(canvas);
                                canvas.id = 'canvas';
                            }
                            canvas = document.getElementById('canvas');
                            canvas.width = parentcanvas.clientWidth;
                            canvas.height = parentcanvas.clientHeight;
                            var WIDTH = canvas.width;
                            var HEIGHT = canvas.height;
                            var ctx = canvas.getContext('2d');
                            ctx.fillStyle = 'backgroundcolor';
                            ctx.fillRect(0, 0, WIDTH, HEIGHT);
                            var audiorawdata = [rawdata100];
                            var barWidth = WIDTH / 99;
                            var barHeight = HEIGHT;
                            var x = 0;
                            for (var i = 1; i < 100; i += 1) {
                                barHeight = audiorawdata[i] / 2;
                                ctx.fillStyle = 'frequencystickscolor';
                                ctx.strokeStyle = 'frequencystickscolor';
                                ctx.fillRect(x, HEIGHT - barHeight, barWidth, barHeight);
                                x += barWidth;
                            }
                            ctx.stroke();
                        }
                        catch {}
                        var elements = document.getElementsByClassName('ads');
                        for (var i = 0; i < elements.length; i++) {
                            elements[i].style.display = 'none';
                        }
                        var els = document.getElementsByClassName('conversion-banner');
                        for (var i = 0; i < els.length; i++) {
                            els[i].style.display = 'none';
                        }
                        var elts = document.querySelectorAll('.ads-top');
                        for (var i = 0; i < elts.length; i++) {
                            elts[i].style.display = 'none';
                        }
                        var ets = document.querySelectorAll('.ads-bottom-alone');
                        for (var i = 0; i < ets.length; i++) {
                            ets[i].style.display = 'none';
                        }
                        document.getElementById('modal-close').click();
                    ";
                await execScriptHelper(stringinject.Replace("backgroundcolor", backgroundcolor).Replace("frequencystickscolor", frequencystickscolor).Replace("rawdata100", (int)barData[0] + ", " + (int)barData[1] + ", " + (int)barData[2] + ", " + (int)barData[3] + ", " + (int)barData[4] + ", " + (int)barData[5] + ", " + (int)barData[6] + ", " + (int)barData[7] + ", " + (int)barData[8] + ", " + (int)barData[9] + ", " + (int)barData[10] + ", " + (int)barData[11] + ", " + (int)barData[12] + ", " + (int)barData[13] + ", " + (int)barData[14] + ", " + (int)barData[15] + ", " + (int)barData[16] + ", " + (int)barData[17] + ", " + (int)barData[18] + ", " + (int)barData[19] + ", " + (int)barData[20] + ", " + (int)barData[21] + ", " + (int)barData[22] + ", " + (int)barData[23] + ", " + (int)barData[24] + ", " + (int)barData[25] + ", " + (int)barData[26] + ", " + (int)barData[27] + ", " + (int)barData[28] + ", " + (int)barData[29] + ", " + (int)barData[30] + ", " + (int)barData[31] + ", " + (int)barData[32] + ", " + (int)barData[33] + ", " + (int)barData[34] + ", " + (int)barData[35] + ", " + (int)barData[36] + ", " + (int)barData[37] + ", " + (int)barData[38] + ", " + (int)barData[39] + ", " + (int)barData[40] + ", " + (int)barData[41] + ", " + (int)barData[42] + ", " + (int)barData[43] + ", " + (int)barData[44] + ", " + (int)barData[45] + ", " + (int)barData[46] + ", " + (int)barData[47] + ", " + (int)barData[48] + ", " + (int)barData[49] + ", " + (int)barData[50] + ", " + (int)barData[51] + ", " + (int)barData[52] + ", " + (int)barData[53] + ", " + (int)barData[54] + ", " + (int)barData[55] + ", " + (int)barData[56] + ", " + (int)barData[57] + ", " + (int)barData[58] + ", " + (int)barData[59] + ", " + (int)barData[60] + ", " + (int)barData[61] + ", " + (int)barData[62] + ", " + (int)barData[63] + ", " + (int)barData[64] + ", " + (int)barData[65] + ", " + (int)barData[66] + ", " + (int)barData[67] + ", " + (int)barData[68] + ", " + (int)barData[69] + ", " + (int)barData[70] + ", " + (int)barData[71] + ", " + (int)barData[72] + ", " + (int)barData[73] + ", " + (int)barData[74] + ", " + (int)barData[75] + ", " + (int)barData[76] + ", " + (int)barData[77] + ", " + (int)barData[78] + ", " + (int)barData[79] + ", " + (int)barData[80] + ", " + (int)barData[81] + ", " + (int)barData[82] + ", " + (int)barData[83] + ", " + (int)barData[84] + ", " + (int)barData[85] + ", " + (int)barData[86] + ", " + (int)barData[87] + ", " + (int)barData[88] + ", " + (int)barData[89] + ", " + (int)barData[90] + ", " + (int)barData[91] + ", " + (int)barData[92] + ", " + (int)barData[93] + ", " + (int)barData[94] + ", " + (int)barData[95] + ", " + (int)barData[96] + ", " + (int)barData[97] + ", " + (int)barData[98] + ", " + (int)barData[99]));
            }
            catch { }
            KeyboardHookProcessButtons();
            if (handletopreviousnexttrack)
            {
                valchanged(0, Key_MEDIA_PLAY_PAUSE);
                if (wu[0] == 1)
                {
                    string stringinject = @"
                    var elements = document.getElementsByTagName('button');
                    for (i = 0; i < elements.length; i++) {
                        if (elements[i].ariaLabel == 'Pause' & elements[i].className == 'svg-icon-group-btn is-highlight') {
                            elements[i].click();
                        }
                        else if ((elements[i].ariaLabel == 'Écouter' | elements[i].ariaLabel == 'Listen') & elements[i].className == 'svg-icon-group-btn is-highlight') {
                            elements[i].click();
                        }
                    }
                ".Replace("\r\n", " ");
                    await execScriptHelper(stringinject);
                }
                valchanged(1, Key_MEDIA_NEXT_TRACK);
                if (wu[1] == 1)
                {
                    string stringinject = @"
                    var elements = document.getElementsByTagName('button');
                    for (i = 0; i < elements.length; i++) {
                        if ((elements[i].ariaLabel == 'Suivant' | elements[i].ariaLabel == 'Next') & elements[i].className == 'svg-icon-group-btn') {
                            elements[i].click();
                        }
                    }
                ".Replace("\r\n", " ");
                    await execScriptHelper(stringinject);
                }
                valchanged(2, Key_MEDIA_PREV_TRACK);
                if (wu[2] == 1)
                {
                    string stringinject = @"
                    var elements = document.getElementsByTagName('button');
                    for (i = 0; i < elements.length; i++) {
                        if ((elements[i].ariaLabel == 'Précédent' | elements[i].ariaLabel == 'Previous') & elements[i].className == 'svg-icon-group-btn') {
                            elements[i].click();
                        }
                    }
                ".Replace("\r\n", " ");
                    await execScriptHelper(stringinject);
                }
            }
            valchanged(3, Key_F11);
            if (wu[3] == 1)
            {
                if (!f11switch)
                {
                    width = Screen.PrimaryScreen.Bounds.Width;
                    height = Screen.PrimaryScreen.Bounds.Height;
                    WINDOW_NAME = GetActiveWindowTitle();
                    if (WINDOW_NAME == "Playzer")
                    {
                        IntPtr window = FindWindowByCaption(IntPtr.Zero, WINDOW_NAME);
                        SetWindowLong(window, GWL_STYLE, WS_SYSMENU);
                        SetWindowPos(window, -2, 0, 0, width, height, 0x0040);
                        DrawMenuBar(window);
                        f11switch = true;
                    }
                }
                else
                {
                    WINDOW_NAME = GetActiveWindowTitle();
                    if (WINDOW_NAME == "Playzer")
                    {
                        IntPtr window = FindWindowByCaption(IntPtr.Zero, WINDOW_NAME);
                        SetWindowLong(window, GWL_STYLE, WS_CAPTION | WS_POPUP | WS_BORDER | WS_SYSMENU | WS_TABSTOP | WS_VISIBLE | WS_OVERLAPPED | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
                        SetWindowPos(window, -2, x, y, cx, cy, 0x0040);
                        DrawMenuBar(window);
                        f11switch = false;
                    }
                }
            }
        }
        private async Task<String> execScriptHelper(String script)
        {
            var x = await webView21.ExecuteScriptAsync(script).ConfigureAwait(false);
            return x;
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            capture.Stop();
            webView21.Dispose();
            keyboardHook.Hook -= new KeyboardHook.KeyboardHookCallback(KeyboardHook_Hook);
            keyboardHook.Uninstall();
            if (echoboostenable)
            {
                var proc = Process.GetProcessesByName("EchoBoost");
                if (proc.Length > 0 & Process.GetProcessesByName("Playtov").Length == 0 & Process.GetProcessesByName("Playcipe").Length == 0 & Process.GetProcessesByName("Playtube").Length == 0)
                    proc[0].Kill();
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!closeonicon)
            {
                e.Cancel = true;
                MinimzedTray();
                return;
            }
        }
        private void TrayMenuContext()
        {
            this.notifyIcon1.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            this.notifyIcon1.ContextMenuStrip.Items.Add("Quit", null, this.MenuTest1_Click);
        }
        void MenuTest1_Click(object sender, EventArgs e)
        {
            closeonicon = true;
            this.Close();
        }
        private void MinimzedTray()
        {
            ShowWindow(Process.GetCurrentProcess().MainWindowHandle, 0);
        }
        private void MaxmizedFromTray()
        {
            if (File.Exists(Application.StartupPath + @"\temphandle"))
                using (System.IO.StreamReader file = new System.IO.StreamReader(Application.StartupPath + @"\temphandle"))
                {
                    IntPtr handle = new IntPtr(int.Parse(file.ReadLine()));
                    ShowWindow(handle, 9);
                    SetForegroundWindow(handle);
                    Microsoft.VisualBasic.Interaction.AppActivate("Playzer");
                }
        }
        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            Task.Run(() => MaxmizedFromTray());
        }
        private static string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();
            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }
        private async void KeyboardHook_Hook(KeyboardHook.KBDLLHOOKSTRUCT keyboardStruct) { }
        public const int VK_LBUTTON = (int)0x01;
        public const int VK_RBUTTON = (int)0x02;
        public const int VK_CANCEL = (int)0x03;
        public const int VK_MBUTTON = (int)0x04;
        public const int VK_XBUTTON1 = (int)0x05;
        public const int VK_XBUTTON2 = (int)0x06;
        public const int VK_BACK = (int)0x08;
        public const int VK_Tab = (int)0x09;
        public const int VK_CLEAR = (int)0x0C;
        public const int VK_Return = (int)0x0D;
        public const int VK_SHIFT = (int)0x10;
        public const int VK_CONTROL = (int)0x11;
        public const int VK_MENU = (int)0x12;
        public const int VK_PAUSE = (int)0x13;
        public const int VK_CAPITAL = (int)0x14;
        public const int VK_KANA = (int)0x15;
        public const int VK_HANGEUL = (int)0x15;
        public const int VK_HANGUL = (int)0x15;
        public const int VK_JUNJA = (int)0x17;
        public const int VK_FINAL = (int)0x18;
        public const int VK_HANJA = (int)0x19;
        public const int VK_KANJI = (int)0x19;
        public const int VK_Escape = (int)0x1B;
        public const int VK_CONVERT = (int)0x1C;
        public const int VK_NONCONVERT = (int)0x1D;
        public const int VK_ACCEPT = (int)0x1E;
        public const int VK_MODECHANGE = (int)0x1F;
        public const int VK_Space = (int)0x20;
        public const int VK_PRIOR = (int)0x21;
        public const int VK_NEXT = (int)0x22;
        public const int VK_END = (int)0x23;
        public const int VK_HOME = (int)0x24;
        public const int VK_LEFT = (int)0x25;
        public const int VK_UP = (int)0x26;
        public const int VK_RIGHT = (int)0x27;
        public const int VK_DOWN = (int)0x28;
        public const int VK_SELECT = (int)0x29;
        public const int VK_PRINT = (int)0x2A;
        public const int VK_EXECUTE = (int)0x2B;
        public const int VK_SNAPSHOT = (int)0x2C;
        public const int VK_INSERT = (int)0x2D;
        public const int VK_DELETE = (int)0x2E;
        public const int VK_HELP = (int)0x2F;
        public const int VK_APOSTROPHE = (int)0xDE;
        public const int VK_0 = (int)0x30;
        public const int VK_1 = (int)0x31;
        public const int VK_2 = (int)0x32;
        public const int VK_3 = (int)0x33;
        public const int VK_4 = (int)0x34;
        public const int VK_5 = (int)0x35;
        public const int VK_6 = (int)0x36;
        public const int VK_7 = (int)0x37;
        public const int VK_8 = (int)0x38;
        public const int VK_9 = (int)0x39;
        public const int VK_A = (int)0x41;
        public const int VK_B = (int)0x42;
        public const int VK_C = (int)0x43;
        public const int VK_D = (int)0x44;
        public const int VK_E = (int)0x45;
        public const int VK_F = (int)0x46;
        public const int VK_G = (int)0x47;
        public const int VK_H = (int)0x48;
        public const int VK_I = (int)0x49;
        public const int VK_J = (int)0x4A;
        public const int VK_K = (int)0x4B;
        public const int VK_L = (int)0x4C;
        public const int VK_M = (int)0x4D;
        public const int VK_N = (int)0x4E;
        public const int VK_O = (int)0x4F;
        public const int VK_P = (int)0x50;
        public const int VK_Q = (int)0x51;
        public const int VK_R = (int)0x52;
        public const int VK_S = (int)0x53;
        public const int VK_T = (int)0x54;
        public const int VK_U = (int)0x55;
        public const int VK_V = (int)0x56;
        public const int VK_W = (int)0x57;
        public const int VK_X = (int)0x58;
        public const int VK_Y = (int)0x59;
        public const int VK_Z = (int)0x5A;
        public const int VK_PAGE_UP = (int)0x21;
        public const int VK_PAGE_DOWN = (int)0x22;
        public const int VK_LWIN = (int)0x5B;
        public const int VK_RWIN = (int)0x5C;
        public const int VK_APPS = (int)0x5D;
        public const int VK_SLEEP = (int)0x5F;
        public const int VK_NUMPAD0 = (int)0x60;
        public const int VK_NUMPAD1 = (int)0x61;
        public const int VK_NUMPAD2 = (int)0x62;
        public const int VK_NUMPAD3 = (int)0x63;
        public const int VK_NUMPAD4 = (int)0x64;
        public const int VK_NUMPAD5 = (int)0x65;
        public const int VK_NUMPAD6 = (int)0x66;
        public const int VK_NUMPAD7 = (int)0x67;
        public const int VK_NUMPAD8 = (int)0x68;
        public const int VK_NUMPAD9 = (int)0x69;
        public const int VK_MULTIPLY = (int)0x6A;
        public const int VK_ADD = (int)0x6B;
        public const int VK_SEPARATOR = (int)0x6C;
        public const int VK_SUBTRACT = (int)0x6D;
        public const int VK_DECIMAL = (int)0x6E;
        public const int VK_DIVIDE = (int)0x6F;
        public const int VK_F1 = (int)0x70;
        public const int VK_F2 = (int)0x71;
        public const int VK_F3 = (int)0x72;
        public const int VK_F4 = (int)0x73;
        public const int VK_F5 = (int)0x74;
        public const int VK_F6 = (int)0x75;
        public const int VK_F7 = (int)0x76;
        public const int VK_F8 = (int)0x77;
        public const int VK_F9 = (int)0x78;
        public const int VK_F10 = (int)0x79;
        public const int VK_F11 = (int)0x7A;
        public const int VK_F12 = (int)0x7B;
        public const int VK_F13 = (int)0x7C;
        public const int VK_F14 = (int)0x7D;
        public const int VK_F15 = (int)0x7E;
        public const int VK_F16 = (int)0x7F;
        public const int VK_F17 = (int)0x80;
        public const int VK_F18 = (int)0x81;
        public const int VK_F19 = (int)0x82;
        public const int VK_F20 = (int)0x83;
        public const int VK_F21 = (int)0x84;
        public const int VK_F22 = (int)0x85;
        public const int VK_F23 = (int)0x86;
        public const int VK_F24 = (int)0x87;
        public const int VK_NUMLOCK = (int)0x90;
        public const int VK_SCROLL = (int)0x91;
        public const int VK_LeftShift = (int)0xA0;
        public const int VK_RightShift = (int)0xA1;
        public const int VK_LeftControl = (int)0xA2;
        public const int VK_RightControl = (int)0xA3;
        public const int VK_LMENU = (int)0xA4;
        public const int VK_RMENU = (int)0xA5;
        public const int VK_BROWSER_BACK = (int)0xA6;
        public const int VK_BROWSER_FORWARD = (int)0xA7;
        public const int VK_BROWSER_REFRESH = (int)0xA8;
        public const int VK_BROWSER_STOP = (int)0xA9;
        public const int VK_BROWSER_SEARCH = (int)0xAA;
        public const int VK_BROWSER_FAVORITES = (int)0xAB;
        public const int VK_BROWSER_HOME = (int)0xAC;
        public const int VK_VOLUME_MUTE = (int)0xAD;
        public const int VK_VOLUME_DOWN = (int)0xAE;
        public const int VK_VOLUME_UP = (int)0xAF;
        public const int VK_MEDIA_NEXT_TRACK = (int)0xB0;
        public const int VK_MEDIA_PREV_TRACK = (int)0xB1;
        public const int VK_MEDIA_STOP = (int)0xB2;
        public const int VK_MEDIA_PLAY_PAUSE = (int)0xB3;
        public const int VK_LAUNCH_MAIL = (int)0xB4;
        public const int VK_LAUNCH_MEDIA_SELECT = (int)0xB5;
        public const int VK_LAUNCH_APP1 = (int)0xB6;
        public const int VK_LAUNCH_APP2 = (int)0xB7;
        public const int VK_OEM_1 = (int)0xBA;
        public const int VK_OEM_PLUS = (int)0xBB;
        public const int VK_OEM_COMMA = (int)0xBC;
        public const int VK_OEM_MINUS = (int)0xBD;
        public const int VK_OEM_PERIOD = (int)0xBE;
        public const int VK_OEM_2 = (int)0xBF;
        public const int VK_OEM_3 = (int)0xC0;
        public const int VK_OEM_4 = (int)0xDB;
        public const int VK_OEM_5 = (int)0xDC;
        public const int VK_OEM_6 = (int)0xDD;
        public const int VK_OEM_7 = (int)0xDE;
        public const int VK_OEM_8 = (int)0xDF;
        public const int VK_OEM_102 = (int)0xE2;
        public const int VK_PROCESSKEY = (int)0xE5;
        public const int VK_PACKET = (int)0xE7;
        public const int VK_ATTN = (int)0xF6;
        public const int VK_CRSEL = (int)0xF7;
        public const int VK_EXSEL = (int)0xF8;
        public const int VK_EREOF = (int)0xF9;
        public const int VK_PLAY = (int)0xFA;
        public const int VK_ZOOM = (int)0xFB;
        public const int VK_NONAME = (int)0xFC;
        public const int VK_PA1 = (int)0xFD;
        public const int VK_OEM_CLEAR = (int)0xFE;
        public const int S_LBUTTON = (int)0x0;
        public const int S_RBUTTON = 0;
        public const int S_CANCEL = 70;
        public const int S_MBUTTON = 0;
        public const int S_XBUTTON1 = 0;
        public const int S_XBUTTON2 = 0;
        public const int S_BACK = 14;
        public const int S_Tab = 15;
        public const int S_CLEAR = 76;
        public const int S_Return = 28;
        public const int S_SHIFT = 42;
        public const int S_CONTROL = 29;
        public const int S_MENU = 56;
        public const int S_PAUSE = 0;
        public const int S_CAPITAL = 58;
        public const int S_KANA = 0;
        public const int S_HANGEUL = 0;
        public const int S_HANGUL = 0;
        public const int S_JUNJA = 0;
        public const int S_FINAL = 0;
        public const int S_HANJA = 0;
        public const int S_KANJI = 0;
        public const int S_Escape = 1;
        public const int S_CONVERT = 0;
        public const int S_NONCONVERT = 0;
        public const int S_ACCEPT = 0;
        public const int S_MODECHANGE = 0;
        public const int S_Space = 57;
        public const int S_PRIOR = 73;
        public const int S_NEXT = 81;
        public const int S_END = 79;
        public const int S_HOME = 71;
        public const int S_LEFT = 75;
        public const int S_UP = 72;
        public const int S_RIGHT = 77;
        public const int S_DOWN = 80;
        public const int S_SELECT = 0;
        public const int S_PRINT = 0;
        public const int S_EXECUTE = 0;
        public const int S_SNAPSHOT = 84;
        public const int S_INSERT = 82;
        public const int S_DELETE = 83;
        public const int S_HELP = 99;
        public const int S_APOSTROPHE = 41;
        public const int S_0 = 11;
        public const int S_1 = 2;
        public const int S_2 = 3;
        public const int S_3 = 4;
        public const int S_4 = 5;
        public const int S_5 = 6;
        public const int S_6 = 7;
        public const int S_7 = 8;
        public const int S_8 = 9;
        public const int S_9 = 10;
        public const int S_A = 16;
        public const int S_B = 48;
        public const int S_C = 46;
        public const int S_D = 32;
        public const int S_E = 18;
        public const int S_F = 33;
        public const int S_G = 34;
        public const int S_H = 35;
        public const int S_I = 23;
        public const int S_J = 36;
        public const int S_K = 37;
        public const int S_L = 38;
        public const int S_M = 39;
        public const int S_N = 49;
        public const int S_O = 24;
        public const int S_P = 25;
        public const int S_Q = 30;
        public const int S_R = 19;
        public const int S_S = 31;
        public const int S_T = 20;
        public const int S_U = 22;
        public const int S_V = 47;
        public const int S_W = 44;
        public const int S_X = 45;
        public const int S_Y = 21;
        public const int S_Z = 17;
        public const int S_PAGE_UP = 73;
        public const int S_PAGE_DOWN = 81;
        public const int S_LWIN = 91;
        public const int S_RWIN = 92;
        public const int S_APPS = 93;
        public const int S_SLEEP = 95;
        public const int S_NUMPAD0 = 82;
        public const int S_NUMPAD1 = 79;
        public const int S_NUMPAD2 = 80;
        public const int S_NUMPAD3 = 81;
        public const int S_NUMPAD4 = 75;
        public const int S_NUMPAD5 = 76;
        public const int S_NUMPAD6 = 77;
        public const int S_NUMPAD7 = 71;
        public const int S_NUMPAD8 = 72;
        public const int S_NUMPAD9 = 73;
        public const int S_MULTIPLY = 55;
        public const int S_ADD = 78;
        public const int S_SEPARATOR = 0;
        public const int S_SUBTRACT = 74;
        public const int S_DECIMAL = 83;
        public const int S_DIVIDE = 53;
        public const int S_F1 = 59;
        public const int S_F2 = 60;
        public const int S_F3 = 61;
        public const int S_F4 = 62;
        public const int S_F5 = 63;
        public const int S_F6 = 64;
        public const int S_F7 = 65;
        public const int S_F8 = 66;
        public const int S_F9 = 67;
        public const int S_F10 = 68;
        public const int S_F11 = 87;
        public const int S_F12 = 88;
        public const int S_F13 = 100;
        public const int S_F14 = 101;
        public const int S_F15 = 102;
        public const int S_F16 = 103;
        public const int S_F17 = 104;
        public const int S_F18 = 105;
        public const int S_F19 = 106;
        public const int S_F20 = 107;
        public const int S_F21 = 108;
        public const int S_F22 = 109;
        public const int S_F23 = 110;
        public const int S_F24 = 118;
        public const int S_NUMLOCK = 69;
        public const int S_SCROLL = 70;
        public const int S_LeftShift = 42;
        public const int S_RightShift = 54;
        public const int S_LeftControl = 29;
        public const int S_RightControl = 29;
        public const int S_LMENU = 56;
        public const int S_RMENU = 56;
        public const int S_BROWSER_BACK = 106;
        public const int S_BROWSER_FORWARD = 105;
        public const int S_BROWSER_REFRESH = 103;
        public const int S_BROWSER_STOP = 104;
        public const int S_BROWSER_SEARCH = 101;
        public const int S_BROWSER_FAVORITES = 102;
        public const int S_BROWSER_HOME = 50;
        public const int S_VOLUME_MUTE = 32;
        public const int S_VOLUME_DOWN = 46;
        public const int S_VOLUME_UP = 48;
        public const int S_MEDIA_NEXT_TRACK = 25;
        public const int S_MEDIA_PREV_TRACK = 16;
        public const int S_MEDIA_STOP = 36;
        public const int S_MEDIA_PLAY_PAUSE = 34;
        public const int S_LAUNCH_MAIL = 108;
        public const int S_LAUNCH_MEDIA_SELECT = 109;
        public const int S_LAUNCH_APP1 = 107;
        public const int S_LAUNCH_APP2 = 33;
        public const int S_OEM_1 = 27;
        public const int S_OEM_PLUS = 13;
        public const int S_OEM_COMMA = 50;
        public const int S_OEM_MINUS = 0;
        public const int S_OEM_PERIOD = 51;
        public const int S_OEM_2 = 52;
        public const int S_OEM_3 = 40;
        public const int S_OEM_4 = 12;
        public const int S_OEM_5 = 43;
        public const int S_OEM_6 = 26;
        public const int S_OEM_7 = 41;
        public const int S_OEM_8 = 53;
        public const int S_OEM_102 = 86;
        public const int S_PROCESSKEY = 0;
        public const int S_PACKET = 0;
        public const int S_ATTN = 0;
        public const int S_CRSEL = 0;
        public const int S_EXSEL = 0;
        public const int S_EREOF = 93;
        public const int S_PLAY = 0;
        public const int S_ZOOM = 98;
        public const int S_NONAME = 0;
        public const int S_PA1 = 0;
        public const int S_OEM_CLEAR = 0;
        public static bool Key_LBUTTON;
        public static bool Key_RBUTTON;
        public static bool Key_CANCEL;
        public static bool Key_MBUTTON;
        public static bool Key_XBUTTON1;
        public static bool Key_XBUTTON2;
        public static bool Key_BACK;
        public static bool Key_Tab;
        public static bool Key_CLEAR;
        public static bool Key_Return;
        public static bool Key_SHIFT;
        public static bool Key_CONTROL;
        public static bool Key_MENU;
        public static bool Key_PAUSE;
        public static bool Key_CAPITAL;
        public static bool Key_KANA;
        public static bool Key_HANGEUL;
        public static bool Key_HANGUL;
        public static bool Key_JUNJA;
        public static bool Key_FINAL;
        public static bool Key_HANJA;
        public static bool Key_KANJI;
        public static bool Key_Escape;
        public static bool Key_CONVERT;
        public static bool Key_NONCONVERT;
        public static bool Key_ACCEPT;
        public static bool Key_MODECHANGE;
        public static bool Key_Space;
        public static bool Key_PRIOR;
        public static bool Key_NEXT;
        public static bool Key_END;
        public static bool Key_HOME;
        public static bool Key_LEFT;
        public static bool Key_UP;
        public static bool Key_RIGHT;
        public static bool Key_DOWN;
        public static bool Key_SELECT;
        public static bool Key_PRINT;
        public static bool Key_EXECUTE;
        public static bool Key_SNAPSHOT;
        public static bool Key_INSERT;
        public static bool Key_DELETE;
        public static bool Key_HELP;
        public static bool Key_APOSTROPHE;
        public static bool Key_0;
        public static bool Key_1;
        public static bool Key_2;
        public static bool Key_3;
        public static bool Key_4;
        public static bool Key_5;
        public static bool Key_6;
        public static bool Key_7;
        public static bool Key_8;
        public static bool Key_9;
        public static bool Key_A;
        public static bool Key_B;
        public static bool Key_C;
        public static bool Key_D;
        public static bool Key_E;
        public static bool Key_F;
        public static bool Key_G;
        public static bool Key_H;
        public static bool Key_I;
        public static bool Key_J;
        public static bool Key_K;
        public static bool Key_L;
        public static bool Key_M;
        public static bool Key_N;
        public static bool Key_O;
        public static bool Key_P;
        public static bool Key_Q;
        public static bool Key_R;
        public static bool Key_S;
        public static bool Key_T;
        public static bool Key_U;
        public static bool Key_V;
        public static bool Key_W;
        public static bool Key_X;
        public static bool Key_Y;
        public static bool Key_Z;
        public static bool Key_PAGE_UP;
        public static bool Key_PAGE_DOWN;
        public static bool Key_LWIN;
        public static bool Key_RWIN;
        public static bool Key_APPS;
        public static bool Key_SLEEP;
        public static bool Key_NUMPAD0;
        public static bool Key_NUMPAD1;
        public static bool Key_NUMPAD2;
        public static bool Key_NUMPAD3;
        public static bool Key_NUMPAD4;
        public static bool Key_NUMPAD5;
        public static bool Key_NUMPAD6;
        public static bool Key_NUMPAD7;
        public static bool Key_NUMPAD8;
        public static bool Key_NUMPAD9;
        public static bool Key_MULTIPLY;
        public static bool Key_ADD;
        public static bool Key_SEPARATOR;
        public static bool Key_SUBTRACT;
        public static bool Key_DECIMAL;
        public static bool Key_DIVIDE;
        public static bool Key_F1;
        public static bool Key_F2;
        public static bool Key_F3;
        public static bool Key_F4;
        public static bool Key_F5;
        public static bool Key_F6;
        public static bool Key_F7;
        public static bool Key_F8;
        public static bool Key_F9;
        public static bool Key_F10;
        public static bool Key_F11;
        public static bool Key_F12;
        public static bool Key_F13;
        public static bool Key_F14;
        public static bool Key_F15;
        public static bool Key_F16;
        public static bool Key_F17;
        public static bool Key_F18;
        public static bool Key_F19;
        public static bool Key_F20;
        public static bool Key_F21;
        public static bool Key_F22;
        public static bool Key_F23;
        public static bool Key_F24;
        public static bool Key_NUMLOCK;
        public static bool Key_SCROLL;
        public static bool Key_LeftShift;
        public static bool Key_RightShift;
        public static bool Key_LeftControl;
        public static bool Key_RightControl;
        public static bool Key_LMENU;
        public static bool Key_RMENU;
        public static bool Key_BROWSER_BACK;
        public static bool Key_BROWSER_FORWARD;
        public static bool Key_BROWSER_REFRESH;
        public static bool Key_BROWSER_STOP;
        public static bool Key_BROWSER_SEARCH;
        public static bool Key_BROWSER_FAVORITES;
        public static bool Key_BROWSER_HOME;
        public static bool Key_VOLUME_MUTE;
        public static bool Key_VOLUME_DOWN;
        public static bool Key_VOLUME_UP;
        public static bool Key_MEDIA_NEXT_TRACK;
        public static bool Key_MEDIA_PREV_TRACK;
        public static bool Key_MEDIA_STOP;
        public static bool Key_MEDIA_PLAY_PAUSE;
        public static bool Key_LAUNCH_MAIL;
        public static bool Key_LAUNCH_MEDIA_SELECT;
        public static bool Key_LAUNCH_APP1;
        public static bool Key_LAUNCH_APP2;
        public static bool Key_OEM_1;
        public static bool Key_OEM_PLUS;
        public static bool Key_OEM_COMMA;
        public static bool Key_OEM_MINUS;
        public static bool Key_OEM_PERIOD;
        public static bool Key_OEM_2;
        public static bool Key_OEM_3;
        public static bool Key_OEM_4;
        public static bool Key_OEM_5;
        public static bool Key_OEM_6;
        public static bool Key_OEM_7;
        public static bool Key_OEM_8;
        public static bool Key_OEM_102;
        public static bool Key_PROCESSKEY;
        public static bool Key_PACKET;
        public static bool Key_ATTN;
        public static bool Key_CRSEL;
        public static bool Key_EXSEL;
        public static bool Key_EREOF;
        public static bool Key_PLAY;
        public static bool Key_ZOOM;
        public static bool Key_NONAME;
        public static bool Key_PA1;
        public static bool Key_OEM_CLEAR;
        public static void KeyboardHookProcessButtons()
        {
            if (KeyboardHookButtonDown)
            {
                if (scanCode == S_LBUTTON & vkCode == VK_LBUTTON)
                    Key_LBUTTON = true;
                if (scanCode == S_RBUTTON & vkCode == VK_RBUTTON)
                    Key_RBUTTON = true;
                if (scanCode == S_CANCEL & vkCode == VK_CANCEL)
                    Key_CANCEL = true;
                if (scanCode == S_MBUTTON & vkCode == VK_MBUTTON)
                    Key_MBUTTON = true;
                if (scanCode == S_XBUTTON1 & vkCode == VK_XBUTTON1)
                    Key_XBUTTON1 = true;
                if (scanCode == S_XBUTTON2 & vkCode == VK_XBUTTON2)
                    Key_XBUTTON2 = true;
                if (scanCode == S_BACK & vkCode == VK_BACK)
                    Key_BACK = true;
                if (scanCode == S_Tab & vkCode == VK_Tab)
                    Key_Tab = true;
                if (scanCode == S_CLEAR & vkCode == VK_CLEAR)
                    Key_CLEAR = true;
                if (scanCode == S_Return & vkCode == VK_Return)
                    Key_Return = true;
                if (scanCode == S_SHIFT & vkCode == VK_SHIFT)
                    Key_SHIFT = true;
                if (scanCode == S_CONTROL & vkCode == VK_CONTROL)
                    Key_CONTROL = true;
                if (scanCode == S_MENU & vkCode == VK_MENU)
                    Key_MENU = true;
                if (scanCode == S_PAUSE & vkCode == VK_PAUSE)
                    Key_PAUSE = true;
                if (scanCode == S_CAPITAL & vkCode == VK_CAPITAL)
                    Key_CAPITAL = true;
                if (scanCode == S_KANA & vkCode == VK_KANA)
                    Key_KANA = true;
                if (scanCode == S_HANGEUL & vkCode == VK_HANGEUL)
                    Key_HANGEUL = true;
                if (scanCode == S_HANGUL & vkCode == VK_HANGUL)
                    Key_HANGUL = true;
                if (scanCode == S_JUNJA & vkCode == VK_JUNJA)
                    Key_JUNJA = true;
                if (scanCode == S_FINAL & vkCode == VK_FINAL)
                    Key_FINAL = true;
                if (scanCode == S_HANJA & vkCode == VK_HANJA)
                    Key_HANJA = true;
                if (scanCode == S_KANJI & vkCode == VK_KANJI)
                    Key_KANJI = true;
                if (scanCode == S_Escape & vkCode == VK_Escape)
                    Key_Escape = true;
                if (scanCode == S_CONVERT & vkCode == VK_CONVERT)
                    Key_CONVERT = true;
                if (scanCode == S_NONCONVERT & vkCode == VK_NONCONVERT)
                    Key_NONCONVERT = true;
                if (scanCode == S_ACCEPT & vkCode == VK_ACCEPT)
                    Key_ACCEPT = true;
                if (scanCode == S_MODECHANGE & vkCode == VK_MODECHANGE)
                    Key_MODECHANGE = true;
                if (scanCode == S_Space & vkCode == VK_Space)
                    Key_Space = true;
                if (scanCode == S_PRIOR & vkCode == VK_PRIOR)
                    Key_PRIOR = true;
                if (scanCode == S_NEXT & vkCode == VK_NEXT)
                    Key_NEXT = true;
                if (scanCode == S_END & vkCode == VK_END)
                    Key_END = true;
                if (scanCode == S_HOME & vkCode == VK_HOME)
                    Key_HOME = true;
                if (scanCode == S_LEFT & vkCode == VK_LEFT)
                    Key_LEFT = true;
                if (scanCode == S_UP & vkCode == VK_UP)
                    Key_UP = true;
                if (scanCode == S_RIGHT & vkCode == VK_RIGHT)
                    Key_RIGHT = true;
                if (scanCode == S_DOWN & vkCode == VK_DOWN)
                    Key_DOWN = true;
                if (scanCode == S_SELECT & vkCode == VK_SELECT)
                    Key_SELECT = true;
                if (scanCode == S_PRINT & vkCode == VK_PRINT)
                    Key_PRINT = true;
                if (scanCode == S_EXECUTE & vkCode == VK_EXECUTE)
                    Key_EXECUTE = true;
                if (scanCode == S_SNAPSHOT & vkCode == VK_SNAPSHOT)
                    Key_SNAPSHOT = true;
                if (scanCode == S_INSERT & vkCode == VK_INSERT)
                    Key_INSERT = true;
                if (scanCode == S_DELETE & vkCode == VK_DELETE)
                    Key_DELETE = true;
                if (scanCode == S_HELP & vkCode == VK_HELP)
                    Key_HELP = true;
                if (scanCode == S_APOSTROPHE & vkCode == VK_APOSTROPHE)
                    Key_APOSTROPHE = true;
                if (scanCode == S_0 & vkCode == VK_0)
                    Key_0 = true;
                if (scanCode == S_1 & vkCode == VK_1)
                    Key_1 = true;
                if (scanCode == S_2 & vkCode == VK_2)
                    Key_2 = true;
                if (scanCode == S_3 & vkCode == VK_3)
                    Key_3 = true;
                if (scanCode == S_4 & vkCode == VK_4)
                    Key_4 = true;
                if (scanCode == S_5 & vkCode == VK_5)
                    Key_5 = true;
                if (scanCode == S_6 & vkCode == VK_6)
                    Key_6 = true;
                if (scanCode == S_7 & vkCode == VK_7)
                    Key_7 = true;
                if (scanCode == S_8 & vkCode == VK_8)
                    Key_8 = true;
                if (scanCode == S_9 & vkCode == VK_9)
                    Key_9 = true;
                if (scanCode == S_A & vkCode == VK_A)
                    Key_A = true;
                if (scanCode == S_B & vkCode == VK_B)
                    Key_B = true;
                if (scanCode == S_C & vkCode == VK_C)
                    Key_C = true;
                if (scanCode == S_D & vkCode == VK_D)
                    Key_D = true;
                if (scanCode == S_E & vkCode == VK_E)
                    Key_E = true;
                if (scanCode == S_F & vkCode == VK_F)
                    Key_F = true;
                if (scanCode == S_G & vkCode == VK_G)
                    Key_G = true;
                if (scanCode == S_H & vkCode == VK_H)
                    Key_H = true;
                if (scanCode == S_I & vkCode == VK_I)
                    Key_I = true;
                if (scanCode == S_J & vkCode == VK_J)
                    Key_J = true;
                if (scanCode == S_K & vkCode == VK_K)
                    Key_K = true;
                if (scanCode == S_L & vkCode == VK_L)
                    Key_L = true;
                if (scanCode == S_M & vkCode == VK_M)
                    Key_M = true;
                if (scanCode == S_N & vkCode == VK_N)
                    Key_N = true;
                if (scanCode == S_O & vkCode == VK_O)
                    Key_O = true;
                if (scanCode == S_P & vkCode == VK_P)
                    Key_P = true;
                if (scanCode == S_Q & vkCode == VK_Q)
                    Key_Q = true;
                if (scanCode == S_R & vkCode == VK_R)
                    Key_R = true;
                if (scanCode == S_S & vkCode == VK_S)
                    Key_S = true;
                if (scanCode == S_T & vkCode == VK_T)
                    Key_T = true;
                if (scanCode == S_U & vkCode == VK_U)
                    Key_U = true;
                if (scanCode == S_V & vkCode == VK_V)
                    Key_V = true;
                if (scanCode == S_W & vkCode == VK_W)
                    Key_W = true;
                if (scanCode == S_X & vkCode == VK_X)
                    Key_X = true;
                if (scanCode == S_Y & vkCode == VK_Y)
                    Key_Y = true;
                if (scanCode == S_Z & vkCode == VK_Z)
                    Key_Z = true;
                if (scanCode == S_PAGE_UP & vkCode == VK_PAGE_UP)
                    Key_PAGE_UP = true;
                if (scanCode == S_PAGE_DOWN & vkCode == VK_PAGE_DOWN)
                    Key_PAGE_DOWN = true;
                if (scanCode == S_LWIN & vkCode == VK_LWIN)
                    Key_LWIN = true;
                if (scanCode == S_RWIN & vkCode == VK_RWIN)
                    Key_RWIN = true;
                if (scanCode == S_APPS & vkCode == VK_APPS)
                    Key_APPS = true;
                if (scanCode == S_SLEEP & vkCode == VK_SLEEP)
                    Key_SLEEP = true;
                if (scanCode == S_NUMPAD0 & vkCode == VK_NUMPAD0)
                    Key_NUMPAD0 = true;
                if (scanCode == S_NUMPAD1 & vkCode == VK_NUMPAD1)
                    Key_NUMPAD1 = true;
                if (scanCode == S_NUMPAD2 & vkCode == VK_NUMPAD2)
                    Key_NUMPAD2 = true;
                if (scanCode == S_NUMPAD3 & vkCode == VK_NUMPAD3)
                    Key_NUMPAD3 = true;
                if (scanCode == S_NUMPAD4 & vkCode == VK_NUMPAD4)
                    Key_NUMPAD4 = true;
                if (scanCode == S_NUMPAD5 & vkCode == VK_NUMPAD5)
                    Key_NUMPAD5 = true;
                if (scanCode == S_NUMPAD6 & vkCode == VK_NUMPAD6)
                    Key_NUMPAD6 = true;
                if (scanCode == S_NUMPAD7 & vkCode == VK_NUMPAD7)
                    Key_NUMPAD7 = true;
                if (scanCode == S_NUMPAD8 & vkCode == VK_NUMPAD8)
                    Key_NUMPAD8 = true;
                if (scanCode == S_NUMPAD9 & vkCode == VK_NUMPAD9)
                    Key_NUMPAD9 = true;
                if (scanCode == S_MULTIPLY & vkCode == VK_MULTIPLY)
                    Key_MULTIPLY = true;
                if (scanCode == S_ADD & vkCode == VK_ADD)
                    Key_ADD = true;
                if (scanCode == S_SEPARATOR & vkCode == VK_SEPARATOR)
                    Key_SEPARATOR = true;
                if (scanCode == S_SUBTRACT & vkCode == VK_SUBTRACT)
                    Key_SUBTRACT = true;
                if (scanCode == S_DECIMAL & vkCode == VK_DECIMAL)
                    Key_DECIMAL = true;
                if (scanCode == S_DIVIDE & vkCode == VK_DIVIDE)
                    Key_DIVIDE = true;
                if (scanCode == S_F1 & vkCode == VK_F1)
                    Key_F1 = true;
                if (scanCode == S_F2 & vkCode == VK_F2)
                    Key_F2 = true;
                if (scanCode == S_F3 & vkCode == VK_F3)
                    Key_F3 = true;
                if (scanCode == S_F4 & vkCode == VK_F4)
                    Key_F4 = true;
                if (scanCode == S_F5 & vkCode == VK_F5)
                    Key_F5 = true;
                if (scanCode == S_F6 & vkCode == VK_F6)
                    Key_F6 = true;
                if (scanCode == S_F7 & vkCode == VK_F7)
                    Key_F7 = true;
                if (scanCode == S_F8 & vkCode == VK_F8)
                    Key_F8 = true;
                if (scanCode == S_F9 & vkCode == VK_F9)
                    Key_F9 = true;
                if (scanCode == S_F10 & vkCode == VK_F10)
                    Key_F10 = true;
                if (scanCode == S_F11 & vkCode == VK_F11)
                    Key_F11 = true;
                if (scanCode == S_F12 & vkCode == VK_F12)
                    Key_F12 = true;
                if (scanCode == S_F13 & vkCode == VK_F13)
                    Key_F13 = true;
                if (scanCode == S_F14 & vkCode == VK_F14)
                    Key_F14 = true;
                if (scanCode == S_F15 & vkCode == VK_F15)
                    Key_F15 = true;
                if (scanCode == S_F16 & vkCode == VK_F16)
                    Key_F16 = true;
                if (scanCode == S_F17 & vkCode == VK_F17)
                    Key_F17 = true;
                if (scanCode == S_F18 & vkCode == VK_F18)
                    Key_F18 = true;
                if (scanCode == S_F19 & vkCode == VK_F19)
                    Key_F19 = true;
                if (scanCode == S_F20 & vkCode == VK_F20)
                    Key_F20 = true;
                if (scanCode == S_F21 & vkCode == VK_F21)
                    Key_F21 = true;
                if (scanCode == S_F22 & vkCode == VK_F22)
                    Key_F22 = true;
                if (scanCode == S_F23 & vkCode == VK_F23)
                    Key_F23 = true;
                if (scanCode == S_F24 & vkCode == VK_F24)
                    Key_F24 = true;
                if (scanCode == S_NUMLOCK & vkCode == VK_NUMLOCK)
                    Key_NUMLOCK = true;
                if (scanCode == S_SCROLL & vkCode == VK_SCROLL)
                    Key_SCROLL = true;
                if (scanCode == S_LeftShift & vkCode == VK_LeftShift)
                    Key_LeftShift = true;
                if (scanCode == S_RightShift & vkCode == VK_RightShift)
                    Key_RightShift = true;
                if (scanCode == S_LeftControl & vkCode == VK_LeftControl)
                    Key_LeftControl = true;
                if (scanCode == S_RightControl & vkCode == VK_RightControl)
                    Key_RightControl = true;
                if (scanCode == S_LMENU & vkCode == VK_LMENU)
                    Key_LMENU = true;
                if (scanCode == S_RMENU & vkCode == VK_RMENU)
                    Key_RMENU = true;
                if (scanCode == S_BROWSER_BACK & vkCode == VK_BROWSER_BACK)
                    Key_BROWSER_BACK = true;
                if (scanCode == S_BROWSER_FORWARD & vkCode == VK_BROWSER_FORWARD)
                    Key_BROWSER_FORWARD = true;
                if (scanCode == S_BROWSER_REFRESH & vkCode == VK_BROWSER_REFRESH)
                    Key_BROWSER_REFRESH = true;
                if (scanCode == S_BROWSER_STOP & vkCode == VK_BROWSER_STOP)
                    Key_BROWSER_STOP = true;
                if (scanCode == S_BROWSER_SEARCH & vkCode == VK_BROWSER_SEARCH)
                    Key_BROWSER_SEARCH = true;
                if (scanCode == S_BROWSER_FAVORITES & vkCode == VK_BROWSER_FAVORITES)
                    Key_BROWSER_FAVORITES = true;
                if (scanCode == S_BROWSER_HOME & vkCode == VK_BROWSER_HOME)
                    Key_BROWSER_HOME = true;
                if (scanCode == S_VOLUME_MUTE & vkCode == VK_VOLUME_MUTE)
                    Key_VOLUME_MUTE = true;
                if (scanCode == S_VOLUME_DOWN & vkCode == VK_VOLUME_DOWN)
                    Key_VOLUME_DOWN = true;
                if (scanCode == S_VOLUME_UP & vkCode == VK_VOLUME_UP)
                    Key_VOLUME_UP = true;
                if (scanCode == S_MEDIA_NEXT_TRACK & vkCode == VK_MEDIA_NEXT_TRACK)
                    Key_MEDIA_NEXT_TRACK = true;
                if (scanCode == S_MEDIA_PREV_TRACK & vkCode == VK_MEDIA_PREV_TRACK)
                    Key_MEDIA_PREV_TRACK = true;
                if (scanCode == S_MEDIA_STOP & vkCode == VK_MEDIA_STOP)
                    Key_MEDIA_STOP = true;
                if (scanCode == S_MEDIA_PLAY_PAUSE & vkCode == VK_MEDIA_PLAY_PAUSE)
                    Key_MEDIA_PLAY_PAUSE = true;
                if (scanCode == S_LAUNCH_MAIL & vkCode == VK_LAUNCH_MAIL)
                    Key_LAUNCH_MAIL = true;
                if (scanCode == S_LAUNCH_MEDIA_SELECT & vkCode == VK_LAUNCH_MEDIA_SELECT)
                    Key_LAUNCH_MEDIA_SELECT = true;
                if (scanCode == S_LAUNCH_APP1 & vkCode == VK_LAUNCH_APP1)
                    Key_LAUNCH_APP1 = true;
                if (scanCode == S_LAUNCH_APP2 & vkCode == VK_LAUNCH_APP2)
                    Key_LAUNCH_APP2 = true;
                if (scanCode == S_OEM_1 & vkCode == VK_OEM_1)
                    Key_OEM_1 = true;
                if (scanCode == S_OEM_PLUS & vkCode == VK_OEM_PLUS)
                    Key_OEM_PLUS = true;
                if (scanCode == S_OEM_COMMA & vkCode == VK_OEM_COMMA)
                    Key_OEM_COMMA = true;
                if (scanCode == S_OEM_MINUS & vkCode == VK_OEM_MINUS)
                    Key_OEM_MINUS = true;
                if (scanCode == S_OEM_PERIOD & vkCode == VK_OEM_PERIOD)
                    Key_OEM_PERIOD = true;
                if (scanCode == S_OEM_2 & vkCode == VK_OEM_2)
                    Key_OEM_2 = true;
                if (scanCode == S_OEM_3 & vkCode == VK_OEM_3)
                    Key_OEM_3 = true;
                if (scanCode == S_OEM_4 & vkCode == VK_OEM_4)
                    Key_OEM_4 = true;
                if (scanCode == S_OEM_5 & vkCode == VK_OEM_5)
                    Key_OEM_5 = true;
                if (scanCode == S_OEM_6 & vkCode == VK_OEM_6)
                    Key_OEM_6 = true;
                if (scanCode == S_OEM_7 & vkCode == VK_OEM_7)
                    Key_OEM_7 = true;
                if (scanCode == S_OEM_8 & vkCode == VK_OEM_8)
                    Key_OEM_8 = true;
                if (scanCode == S_OEM_102 & vkCode == VK_OEM_102)
                    Key_OEM_102 = true;
                if (scanCode == S_PROCESSKEY & vkCode == VK_PROCESSKEY)
                    Key_PROCESSKEY = true;
                if (scanCode == S_PACKET & vkCode == VK_PACKET)
                    Key_PACKET = true;
                if (scanCode == S_ATTN & vkCode == VK_ATTN)
                    Key_ATTN = true;
                if (scanCode == S_CRSEL & vkCode == VK_CRSEL)
                    Key_CRSEL = true;
                if (scanCode == S_EXSEL & vkCode == VK_EXSEL)
                    Key_EXSEL = true;
                if (scanCode == S_EREOF & vkCode == VK_EREOF)
                    Key_EREOF = true;
                if (scanCode == S_PLAY & vkCode == VK_PLAY)
                    Key_PLAY = true;
                if (scanCode == S_ZOOM & vkCode == VK_ZOOM)
                    Key_ZOOM = true;
                if (scanCode == S_NONAME & vkCode == VK_NONAME)
                    Key_NONAME = true;
                if (scanCode == S_PA1 & vkCode == VK_PA1)
                    Key_PA1 = true;
                if (scanCode == S_OEM_CLEAR & vkCode == VK_OEM_CLEAR)
                    Key_OEM_CLEAR = true;
            }
            if (KeyboardHookButtonUp)
            {
                if (scanCode == S_LBUTTON & vkCode == VK_LBUTTON)
                    Key_LBUTTON = false;
                if (scanCode == S_RBUTTON & vkCode == VK_RBUTTON)
                    Key_RBUTTON = false;
                if (scanCode == S_CANCEL & vkCode == VK_CANCEL)
                    Key_CANCEL = false;
                if (scanCode == S_MBUTTON & vkCode == VK_MBUTTON)
                    Key_MBUTTON = false;
                if (scanCode == S_XBUTTON1 & vkCode == VK_XBUTTON1)
                    Key_XBUTTON1 = false;
                if (scanCode == S_XBUTTON2 & vkCode == VK_XBUTTON2)
                    Key_XBUTTON2 = false;
                if (scanCode == S_BACK & vkCode == VK_BACK)
                    Key_BACK = false;
                if (scanCode == S_Tab & vkCode == VK_Tab)
                    Key_Tab = false;
                if (scanCode == S_CLEAR & vkCode == VK_CLEAR)
                    Key_CLEAR = false;
                if (scanCode == S_Return & vkCode == VK_Return)
                    Key_Return = false;
                if (scanCode == S_SHIFT & vkCode == VK_SHIFT)
                    Key_SHIFT = false;
                if (scanCode == S_CONTROL & vkCode == VK_CONTROL)
                    Key_CONTROL = false;
                if (scanCode == S_MENU & vkCode == VK_MENU)
                    Key_MENU = false;
                if (scanCode == S_PAUSE & vkCode == VK_PAUSE)
                    Key_PAUSE = false;
                if (scanCode == S_CAPITAL & vkCode == VK_CAPITAL)
                    Key_CAPITAL = false;
                if (scanCode == S_KANA & vkCode == VK_KANA)
                    Key_KANA = false;
                if (scanCode == S_HANGEUL & vkCode == VK_HANGEUL)
                    Key_HANGEUL = false;
                if (scanCode == S_HANGUL & vkCode == VK_HANGUL)
                    Key_HANGUL = false;
                if (scanCode == S_JUNJA & vkCode == VK_JUNJA)
                    Key_JUNJA = false;
                if (scanCode == S_FINAL & vkCode == VK_FINAL)
                    Key_FINAL = false;
                if (scanCode == S_HANJA & vkCode == VK_HANJA)
                    Key_HANJA = false;
                if (scanCode == S_KANJI & vkCode == VK_KANJI)
                    Key_KANJI = false;
                if (scanCode == S_Escape & vkCode == VK_Escape)
                    Key_Escape = false;
                if (scanCode == S_CONVERT & vkCode == VK_CONVERT)
                    Key_CONVERT = false;
                if (scanCode == S_NONCONVERT & vkCode == VK_NONCONVERT)
                    Key_NONCONVERT = false;
                if (scanCode == S_ACCEPT & vkCode == VK_ACCEPT)
                    Key_ACCEPT = false;
                if (scanCode == S_MODECHANGE & vkCode == VK_MODECHANGE)
                    Key_MODECHANGE = false;
                if (scanCode == S_Space & vkCode == VK_Space)
                    Key_Space = false;
                if (scanCode == S_PRIOR & vkCode == VK_PRIOR)
                    Key_PRIOR = false;
                if (scanCode == S_NEXT & vkCode == VK_NEXT)
                    Key_NEXT = false;
                if (scanCode == S_END & vkCode == VK_END)
                    Key_END = false;
                if (scanCode == S_HOME & vkCode == VK_HOME)
                    Key_HOME = false;
                if (scanCode == S_LEFT & vkCode == VK_LEFT)
                    Key_LEFT = false;
                if (scanCode == S_UP & vkCode == VK_UP)
                    Key_UP = false;
                if (scanCode == S_RIGHT & vkCode == VK_RIGHT)
                    Key_RIGHT = false;
                if (scanCode == S_DOWN & vkCode == VK_DOWN)
                    Key_DOWN = false;
                if (scanCode == S_SELECT & vkCode == VK_SELECT)
                    Key_SELECT = false;
                if (scanCode == S_PRINT & vkCode == VK_PRINT)
                    Key_PRINT = false;
                if (scanCode == S_EXECUTE & vkCode == VK_EXECUTE)
                    Key_EXECUTE = false;
                if (scanCode == S_SNAPSHOT & vkCode == VK_SNAPSHOT)
                    Key_SNAPSHOT = false;
                if (scanCode == S_INSERT & vkCode == VK_INSERT)
                    Key_INSERT = false;
                if (scanCode == S_DELETE & vkCode == VK_DELETE)
                    Key_DELETE = false;
                if (scanCode == S_HELP & vkCode == VK_HELP)
                    Key_HELP = false;
                if (scanCode == S_APOSTROPHE & vkCode == VK_APOSTROPHE)
                    Key_APOSTROPHE = false;
                if (scanCode == S_0 & vkCode == VK_0)
                    Key_0 = false;
                if (scanCode == S_1 & vkCode == VK_1)
                    Key_1 = false;
                if (scanCode == S_2 & vkCode == VK_2)
                    Key_2 = false;
                if (scanCode == S_3 & vkCode == VK_3)
                    Key_3 = false;
                if (scanCode == S_4 & vkCode == VK_4)
                    Key_4 = false;
                if (scanCode == S_5 & vkCode == VK_5)
                    Key_5 = false;
                if (scanCode == S_6 & vkCode == VK_6)
                    Key_6 = false;
                if (scanCode == S_7 & vkCode == VK_7)
                    Key_7 = false;
                if (scanCode == S_8 & vkCode == VK_8)
                    Key_8 = false;
                if (scanCode == S_9 & vkCode == VK_9)
                    Key_9 = false;
                if (scanCode == S_A & vkCode == VK_A)
                    Key_A = false;
                if (scanCode == S_B & vkCode == VK_B)
                    Key_B = false;
                if (scanCode == S_C & vkCode == VK_C)
                    Key_C = false;
                if (scanCode == S_D & vkCode == VK_D)
                    Key_D = false;
                if (scanCode == S_E & vkCode == VK_E)
                    Key_E = false;
                if (scanCode == S_F & vkCode == VK_F)
                    Key_F = false;
                if (scanCode == S_G & vkCode == VK_G)
                    Key_G = false;
                if (scanCode == S_H & vkCode == VK_H)
                    Key_H = false;
                if (scanCode == S_I & vkCode == VK_I)
                    Key_I = false;
                if (scanCode == S_J & vkCode == VK_J)
                    Key_J = false;
                if (scanCode == S_K & vkCode == VK_K)
                    Key_K = false;
                if (scanCode == S_L & vkCode == VK_L)
                    Key_L = false;
                if (scanCode == S_M & vkCode == VK_M)
                    Key_M = false;
                if (scanCode == S_N & vkCode == VK_N)
                    Key_N = false;
                if (scanCode == S_O & vkCode == VK_O)
                    Key_O = false;
                if (scanCode == S_P & vkCode == VK_P)
                    Key_P = false;
                if (scanCode == S_Q & vkCode == VK_Q)
                    Key_Q = false;
                if (scanCode == S_R & vkCode == VK_R)
                    Key_R = false;
                if (scanCode == S_S & vkCode == VK_S)
                    Key_S = false;
                if (scanCode == S_T & vkCode == VK_T)
                    Key_T = false;
                if (scanCode == S_U & vkCode == VK_U)
                    Key_U = false;
                if (scanCode == S_V & vkCode == VK_V)
                    Key_V = false;
                if (scanCode == S_W & vkCode == VK_W)
                    Key_W = false;
                if (scanCode == S_X & vkCode == VK_X)
                    Key_X = false;
                if (scanCode == S_Y & vkCode == VK_Y)
                    Key_Y = false;
                if (scanCode == S_Z & vkCode == VK_Z)
                    Key_Z = false;
                if (scanCode == S_PAGE_UP & vkCode == VK_PAGE_UP)
                    Key_PAGE_UP = false;
                if (scanCode == S_PAGE_DOWN & vkCode == VK_PAGE_DOWN)
                    Key_PAGE_DOWN = false;
                if (scanCode == S_LWIN & vkCode == VK_LWIN)
                    Key_LWIN = false;
                if (scanCode == S_RWIN & vkCode == VK_RWIN)
                    Key_RWIN = false;
                if (scanCode == S_APPS & vkCode == VK_APPS)
                    Key_APPS = false;
                if (scanCode == S_SLEEP & vkCode == VK_SLEEP)
                    Key_SLEEP = false;
                if (scanCode == S_NUMPAD0 & vkCode == VK_NUMPAD0)
                    Key_NUMPAD0 = false;
                if (scanCode == S_NUMPAD1 & vkCode == VK_NUMPAD1)
                    Key_NUMPAD1 = false;
                if (scanCode == S_NUMPAD2 & vkCode == VK_NUMPAD2)
                    Key_NUMPAD2 = false;
                if (scanCode == S_NUMPAD3 & vkCode == VK_NUMPAD3)
                    Key_NUMPAD3 = false;
                if (scanCode == S_NUMPAD4 & vkCode == VK_NUMPAD4)
                    Key_NUMPAD4 = false;
                if (scanCode == S_NUMPAD5 & vkCode == VK_NUMPAD5)
                    Key_NUMPAD5 = false;
                if (scanCode == S_NUMPAD6 & vkCode == VK_NUMPAD6)
                    Key_NUMPAD6 = false;
                if (scanCode == S_NUMPAD7 & vkCode == VK_NUMPAD7)
                    Key_NUMPAD7 = false;
                if (scanCode == S_NUMPAD8 & vkCode == VK_NUMPAD8)
                    Key_NUMPAD8 = false;
                if (scanCode == S_NUMPAD9 & vkCode == VK_NUMPAD9)
                    Key_NUMPAD9 = false;
                if (scanCode == S_MULTIPLY & vkCode == VK_MULTIPLY)
                    Key_MULTIPLY = false;
                if (scanCode == S_ADD & vkCode == VK_ADD)
                    Key_ADD = false;
                if (scanCode == S_SEPARATOR & vkCode == VK_SEPARATOR)
                    Key_SEPARATOR = false;
                if (scanCode == S_SUBTRACT & vkCode == VK_SUBTRACT)
                    Key_SUBTRACT = false;
                if (scanCode == S_DECIMAL & vkCode == VK_DECIMAL)
                    Key_DECIMAL = false;
                if (scanCode == S_DIVIDE & vkCode == VK_DIVIDE)
                    Key_DIVIDE = false;
                if (scanCode == S_F1 & vkCode == VK_F1)
                    Key_F1 = false;
                if (scanCode == S_F2 & vkCode == VK_F2)
                    Key_F2 = false;
                if (scanCode == S_F3 & vkCode == VK_F3)
                    Key_F3 = false;
                if (scanCode == S_F4 & vkCode == VK_F4)
                    Key_F4 = false;
                if (scanCode == S_F5 & vkCode == VK_F5)
                    Key_F5 = false;
                if (scanCode == S_F6 & vkCode == VK_F6)
                    Key_F6 = false;
                if (scanCode == S_F7 & vkCode == VK_F7)
                    Key_F7 = false;
                if (scanCode == S_F8 & vkCode == VK_F8)
                    Key_F8 = false;
                if (scanCode == S_F9 & vkCode == VK_F9)
                    Key_F9 = false;
                if (scanCode == S_F10 & vkCode == VK_F10)
                    Key_F10 = false;
                if (scanCode == S_F11 & vkCode == VK_F11)
                    Key_F11 = false;
                if (scanCode == S_F12 & vkCode == VK_F12)
                    Key_F12 = false;
                if (scanCode == S_F13 & vkCode == VK_F13)
                    Key_F13 = false;
                if (scanCode == S_F14 & vkCode == VK_F14)
                    Key_F14 = false;
                if (scanCode == S_F15 & vkCode == VK_F15)
                    Key_F15 = false;
                if (scanCode == S_F16 & vkCode == VK_F16)
                    Key_F16 = false;
                if (scanCode == S_F17 & vkCode == VK_F17)
                    Key_F17 = false;
                if (scanCode == S_F18 & vkCode == VK_F18)
                    Key_F18 = false;
                if (scanCode == S_F19 & vkCode == VK_F19)
                    Key_F19 = false;
                if (scanCode == S_F20 & vkCode == VK_F20)
                    Key_F20 = false;
                if (scanCode == S_F21 & vkCode == VK_F21)
                    Key_F21 = false;
                if (scanCode == S_F22 & vkCode == VK_F22)
                    Key_F22 = false;
                if (scanCode == S_F23 & vkCode == VK_F23)
                    Key_F23 = false;
                if (scanCode == S_F24 & vkCode == VK_F24)
                    Key_F24 = false;
                if (scanCode == S_NUMLOCK & vkCode == VK_NUMLOCK)
                    Key_NUMLOCK = false;
                if (scanCode == S_SCROLL & vkCode == VK_SCROLL)
                    Key_SCROLL = false;
                if (scanCode == S_LeftShift & vkCode == VK_LeftShift)
                    Key_LeftShift = false;
                if (scanCode == S_RightShift & vkCode == VK_RightShift)
                    Key_RightShift = false;
                if (scanCode == S_LeftControl & vkCode == VK_LeftControl)
                    Key_LeftControl = false;
                if (scanCode == S_RightControl & vkCode == VK_RightControl)
                    Key_RightControl = false;
                if (scanCode == S_LMENU & vkCode == VK_LMENU)
                    Key_LMENU = false;
                if (scanCode == S_RMENU & vkCode == VK_RMENU)
                    Key_RMENU = false;
                if (scanCode == S_BROWSER_BACK & vkCode == VK_BROWSER_BACK)
                    Key_BROWSER_BACK = false;
                if (scanCode == S_BROWSER_FORWARD & vkCode == VK_BROWSER_FORWARD)
                    Key_BROWSER_FORWARD = false;
                if (scanCode == S_BROWSER_REFRESH & vkCode == VK_BROWSER_REFRESH)
                    Key_BROWSER_REFRESH = false;
                if (scanCode == S_BROWSER_STOP & vkCode == VK_BROWSER_STOP)
                    Key_BROWSER_STOP = false;
                if (scanCode == S_BROWSER_SEARCH & vkCode == VK_BROWSER_SEARCH)
                    Key_BROWSER_SEARCH = false;
                if (scanCode == S_BROWSER_FAVORITES & vkCode == VK_BROWSER_FAVORITES)
                    Key_BROWSER_FAVORITES = false;
                if (scanCode == S_BROWSER_HOME & vkCode == VK_BROWSER_HOME)
                    Key_BROWSER_HOME = false;
                if (scanCode == S_VOLUME_MUTE & vkCode == VK_VOLUME_MUTE)
                    Key_VOLUME_MUTE = false;
                if (scanCode == S_VOLUME_DOWN & vkCode == VK_VOLUME_DOWN)
                    Key_VOLUME_DOWN = false;
                if (scanCode == S_VOLUME_UP & vkCode == VK_VOLUME_UP)
                    Key_VOLUME_UP = false;
                if (scanCode == S_MEDIA_NEXT_TRACK & vkCode == VK_MEDIA_NEXT_TRACK)
                    Key_MEDIA_NEXT_TRACK = false;
                if (scanCode == S_MEDIA_PREV_TRACK & vkCode == VK_MEDIA_PREV_TRACK)
                    Key_MEDIA_PREV_TRACK = false;
                if (scanCode == S_MEDIA_STOP & vkCode == VK_MEDIA_STOP)
                    Key_MEDIA_STOP = false;
                if (scanCode == S_MEDIA_PLAY_PAUSE & vkCode == VK_MEDIA_PLAY_PAUSE)
                    Key_MEDIA_PLAY_PAUSE = false;
                if (scanCode == S_LAUNCH_MAIL & vkCode == VK_LAUNCH_MAIL)
                    Key_LAUNCH_MAIL = false;
                if (scanCode == S_LAUNCH_MEDIA_SELECT & vkCode == VK_LAUNCH_MEDIA_SELECT)
                    Key_LAUNCH_MEDIA_SELECT = false;
                if (scanCode == S_LAUNCH_APP1 & vkCode == VK_LAUNCH_APP1)
                    Key_LAUNCH_APP1 = false;
                if (scanCode == S_LAUNCH_APP2 & vkCode == VK_LAUNCH_APP2)
                    Key_LAUNCH_APP2 = false;
                if (scanCode == S_OEM_1 & vkCode == VK_OEM_1)
                    Key_OEM_1 = false;
                if (scanCode == S_OEM_PLUS & vkCode == VK_OEM_PLUS)
                    Key_OEM_PLUS = false;
                if (scanCode == S_OEM_COMMA & vkCode == VK_OEM_COMMA)
                    Key_OEM_COMMA = false;
                if (scanCode == S_OEM_MINUS & vkCode == VK_OEM_MINUS)
                    Key_OEM_MINUS = false;
                if (scanCode == S_OEM_PERIOD & vkCode == VK_OEM_PERIOD)
                    Key_OEM_PERIOD = false;
                if (scanCode == S_OEM_2 & vkCode == VK_OEM_2)
                    Key_OEM_2 = false;
                if (scanCode == S_OEM_3 & vkCode == VK_OEM_3)
                    Key_OEM_3 = false;
                if (scanCode == S_OEM_4 & vkCode == VK_OEM_4)
                    Key_OEM_4 = false;
                if (scanCode == S_OEM_5 & vkCode == VK_OEM_5)
                    Key_OEM_5 = false;
                if (scanCode == S_OEM_6 & vkCode == VK_OEM_6)
                    Key_OEM_6 = false;
                if (scanCode == S_OEM_7 & vkCode == VK_OEM_7)
                    Key_OEM_7 = false;
                if (scanCode == S_OEM_8 & vkCode == VK_OEM_8)
                    Key_OEM_8 = false;
                if (scanCode == S_OEM_102 & vkCode == VK_OEM_102)
                    Key_OEM_102 = false;
                if (scanCode == S_PROCESSKEY & vkCode == VK_PROCESSKEY)
                    Key_PROCESSKEY = false;
                if (scanCode == S_PACKET & vkCode == VK_PACKET)
                    Key_PACKET = false;
                if (scanCode == S_ATTN & vkCode == VK_ATTN)
                    Key_ATTN = false;
                if (scanCode == S_CRSEL & vkCode == VK_CRSEL)
                    Key_CRSEL = false;
                if (scanCode == S_EXSEL & vkCode == VK_EXSEL)
                    Key_EXSEL = false;
                if (scanCode == S_EREOF & vkCode == VK_EREOF)
                    Key_EREOF = false;
                if (scanCode == S_PLAY & vkCode == VK_PLAY)
                    Key_PLAY = false;
                if (scanCode == S_ZOOM & vkCode == VK_ZOOM)
                    Key_ZOOM = false;
                if (scanCode == S_NONAME & vkCode == VK_NONAME)
                    Key_NONAME = false;
                if (scanCode == S_PA1 & vkCode == VK_PA1)
                    Key_PA1 = false;
                if (scanCode == S_OEM_CLEAR & vkCode == VK_OEM_CLEAR)
                    Key_OEM_CLEAR = false;
            }
        }
    }
    public class KeyboardHook
    {
        public static bool KeyboardHookButtonDown, KeyboardHookButtonUp;
        public delegate IntPtr KeyboardHookHandler(int nCode, IntPtr wParam, IntPtr lParam);
        public KeyboardHookHandler hookHandler;
        public KBDLLHOOKSTRUCT keyboardStruct;
        public delegate void KeyboardHookCallback(KBDLLHOOKSTRUCT keyboardStruct);
        public event KeyboardHookCallback Hook;
        public IntPtr hookID = IntPtr.Zero;
        public void Install()
        {
            hookHandler = HookFunc;
            hookID = SetHook(hookHandler);
        }
        public void Uninstall()
        {
            if (hookID == IntPtr.Zero)
                return;
            UnhookWindowsHookEx(hookID);
            hookID = IntPtr.Zero;
        }
        ~KeyboardHook()
        {
            Uninstall();
        }
        public IntPtr SetHook(KeyboardHookHandler proc)
        {
            using (ProcessModule module = Process.GetCurrentProcess().MainModule)
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(module.ModuleName), 0);
        }
        public IntPtr HookFunc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            keyboardStruct = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
            if (KeyboardHook.KeyboardMessages.WM_KEYDOWN == (KeyboardHook.KeyboardMessages)wParam)
                KeyboardHookButtonDown = true;
            else
                KeyboardHookButtonDown = false;
            if (KeyboardHook.KeyboardMessages.WM_KEYUP == (KeyboardHook.KeyboardMessages)wParam)
                KeyboardHookButtonUp = true;
            else
                KeyboardHookButtonUp = false;
            Form1.KeyboardHookButtonDown = KeyboardHookButtonDown;
            Form1.KeyboardHookButtonUp = KeyboardHookButtonUp;
            Form1.vkCode = (int)keyboardStruct.vkCode;
            Form1.scanCode = (int)keyboardStruct.scanCode;
            Hook((KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT)));
            return CallNextHookEx(hookID, nCode, wParam, lParam);
        }

        public const int WH_KEYBOARD_LL = 13;
        public enum KeyboardMessages
        {
            WM_ACTIVATE = 0x0006,
            WM_APPCOMMAND = 0x0319,
            WM_CHAR = 0x0102,
            WM_DEADCHAR = 0x010,
            WM_HOTKEY = 0x0312,
            WM_KEYDOWN = 0x0100,
            WM_KEYUP = 0x0101,
            WM_KILLFOCUS = 0x0008,
            WM_SETFOCUS = 0x0007,
            WM_SYSDEADCHAR = 0x0107,
            WM_SYSKEYDOWN = 0x0104,
            WM_SYSKEYUP = 0x0105,
            WM_UNICHAR = 0x0109
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, KeyboardHookHandler lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
namespace WinformsVisualization.Visualization
{
    /// <summary>
    ///     BasicSpectrumProvider
    /// </summary>
    public class BasicSpectrumProvider : CSCore.DSP.FftProvider, ISpectrumProvider
    {
        public readonly int _sampleRate;
        public readonly List<object> _contexts = new List<object>();

        public BasicSpectrumProvider(int channels, int sampleRate, CSCore.DSP.FftSize fftSize)
            : base(channels, fftSize)
        {
            if (sampleRate <= 0)
                throw new ArgumentOutOfRangeException("sampleRate");
            _sampleRate = sampleRate;
        }

        public int GetFftBandIndex(float frequency)
        {
            int fftSize = (int)FftSize;
            double f = _sampleRate / 2.0;
            // ReSharper disable once PossibleLossOfFraction
            return (int)((frequency / f) * (fftSize / 2));
        }

        public bool GetFftData(float[] fftResultBuffer, object context)
        {
            if (_contexts.Contains(context))
                return false;

            _contexts.Add(context);
            GetFftData(fftResultBuffer);
            return true;
        }

        public override void Add(float[] samples, int count)
        {
            base.Add(samples, count);
            if (count > 0)
                _contexts.Clear();
        }

        public override void Add(float left, float right)
        {
            base.Add(left, right);
            _contexts.Clear();
        }
    }
}
namespace WinformsVisualization.Visualization
{
    public interface ISpectrumProvider
    {
        bool GetFftData(float[] fftBuffer, object context);
        int GetFftBandIndex(float frequency);
    }
}
namespace WinformsVisualization.Visualization
{
    internal class GradientCalculator
    {
        public Color[] _colors;

        public GradientCalculator()
        {
        }

        public GradientCalculator(params Color[] colors)
        {
            _colors = colors;
        }

        public Color[] Colors
        {
            get { return _colors ?? (_colors = new Color[] { }); }
            set { _colors = value; }
        }

        public Color GetColor(float perc)
        {
            if (_colors.Length > 1)
            {
                int index = Convert.ToInt32((_colors.Length - 1) * perc - 0.5f);
                float upperIntensity = (perc % (1f / (_colors.Length - 1))) * (_colors.Length - 1);
                if (index + 1 >= Colors.Length)
                    index = Colors.Length - 2;

                return Color.FromArgb(
                    255,
                    (byte)(_colors[index + 1].R * upperIntensity + _colors[index].R * (1f - upperIntensity)),
                    (byte)(_colors[index + 1].G * upperIntensity + _colors[index].G * (1f - upperIntensity)),
                    (byte)(_colors[index + 1].B * upperIntensity + _colors[index].B * (1f - upperIntensity)));
            }
            return _colors.FirstOrDefault();
        }
    }
}
namespace WinformsVisualization.Visualization
{
    public class LineSpectrum : SpectrumBase
    {
        public int _barCount;
        public double _barSpacing;
        public double _barWidth;
        public Size _currentSize;

        public LineSpectrum(CSCore.DSP.FftSize fftSize)
        {
            FftSize = fftSize;
        }

        [Browsable(false)]
        public double BarWidth
        {
            get { return _barWidth; }
        }

        public double BarSpacing
        {
            get { return _barSpacing; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value");
                _barSpacing = value;
                UpdateFrequencyMapping();

                RaisePropertyChanged("BarSpacing");
                RaisePropertyChanged("BarWidth");
            }
        }

        public int BarCount
        {
            get { return _barCount; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value");
                _barCount = value;
                SpectrumResolution = value;
                UpdateFrequencyMapping();

                RaisePropertyChanged("BarCount");
                RaisePropertyChanged("BarWidth");
            }
        }

        [BrowsableAttribute(false)]
        public Size CurrentSize
        {
            get { return _currentSize; }
            set
            {
                _currentSize = value;
                RaisePropertyChanged("CurrentSize");
            }
        }

        public Bitmap CreateSpectrumLine(Size size, Brush brush, Color background, bool highQuality)
        {
            if (!UpdateFrequencyMappingIfNessesary(size))
                return null;

            var fftBuffer = new float[(int)FftSize];

            //get the fft result from the spectrum provider
            if (SpectrumProvider.GetFftData(fftBuffer, this))
            {
                using (var pen = new Pen(brush, (float)_barWidth))
                {
                    var bitmap = new Bitmap(size.Width, size.Height);

                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        PrepareGraphics(graphics, highQuality);
                        graphics.Clear(background);

                        CreateSpectrumLineInternal(graphics, pen, fftBuffer, size);
                    }

                    return bitmap;
                }
            }
            return null;
        }

        public Bitmap CreateSpectrumLine(Size size, Color color1, Color color2, Color background, bool highQuality)
        {
            if (!UpdateFrequencyMappingIfNessesary(size))
                return null;

            using (
                Brush brush = new LinearGradientBrush(new RectangleF(0, 0, (float)_barWidth, size.Height), color2,
                    color1, LinearGradientMode.Vertical))
            {
                return CreateSpectrumLine(size, brush, background, highQuality);
            }
        }

        public void CreateSpectrumLineInternal(Graphics graphics, Pen pen, float[] fftBuffer, Size size)
        {
            int height = size.Height;
            //prepare the fft result for rendering 
            SpectrumPointData[] spectrumPoints = CalculateSpectrumPoints(height, fftBuffer);

            //connect the calculated points with lines
            for (int i = 0; i < spectrumPoints.Length; i++)
            {
                SpectrumPointData p = spectrumPoints[i];
                int barIndex = p.SpectrumPointIndex;
                double xCoord = BarSpacing * (barIndex + 1) + (_barWidth * barIndex) + _barWidth / 2;

                var p1 = new PointF((float)xCoord, height);
                var p2 = new PointF((float)xCoord, height - (float)p.Value - 1);

                graphics.DrawLine(pen, p1, p2);
            }
        }

        public override void UpdateFrequencyMapping()
        {
            _barWidth = Math.Max(((_currentSize.Width - (BarSpacing * (BarCount + 1))) / BarCount), 0.00001);
            base.UpdateFrequencyMapping();
        }

        public bool UpdateFrequencyMappingIfNessesary(Size newSize)
        {
            if (newSize != CurrentSize)
            {
                CurrentSize = newSize;
                UpdateFrequencyMapping();
            }

            return newSize.Width > 0 && newSize.Height > 0;
        }

        public void PrepareGraphics(Graphics graphics, bool highQuality)
        {
            if (highQuality)
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.CompositingQuality = CompositingQuality.AssumeLinear;
                graphics.PixelOffsetMode = PixelOffsetMode.Default;
                graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            }
            else
            {
                graphics.SmoothingMode = SmoothingMode.HighSpeed;
                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.PixelOffsetMode = PixelOffsetMode.None;
                graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
            }
        }
        public float[] GetSpectrumPoints(float height, float[] fftBuffer)
        {
            SpectrumPointData[] dats = CalculateSpectrumPoints(height, fftBuffer);
            float[] res = new float[dats.Length];
            for (int i = 0; i < dats.Length; i++)
            {
                res[i] = (float)dats[i].Value;
            }

            return res;
        }
    }
}
namespace WinformsVisualization.Visualization
{
    public class SpectrumBase : INotifyPropertyChanged
    {
        public const int ScaleFactorLinear = 9;
        public const int ScaleFactorSqr = 2;
        public const double MinDbValue = -90;
        public const double MaxDbValue = 0;
        public const double DbScale = (MaxDbValue - MinDbValue);

        public int _fftSize;
        public bool _isXLogScale;
        public int _maxFftIndex;
        public int _maximumFrequency = 20000;
        public int _maximumFrequencyIndex;
        public int _minimumFrequency = 20; //Default spectrum from 20Hz to 20kHz
        public int _minimumFrequencyIndex;
        public ScalingStrategy _scalingStrategy;
        public int[] _spectrumIndexMax;
        public int[] _spectrumLogScaleIndexMax;
        public ISpectrumProvider _spectrumProvider;

        public int SpectrumResolution;
        public bool _useAverage;

        public int MaximumFrequency
        {
            get { return _maximumFrequency; }
            set
            {
                if (value <= MinimumFrequency)
                {
                    throw new ArgumentOutOfRangeException("value",
                        "Value must not be less or equal the MinimumFrequency.");
                }
                _maximumFrequency = value;
                UpdateFrequencyMapping();

                RaisePropertyChanged("MaximumFrequency");
            }
        }

        public int MinimumFrequency
        {
            get { return _minimumFrequency; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value");
                _minimumFrequency = value;
                UpdateFrequencyMapping();

                RaisePropertyChanged("MinimumFrequency");
            }
        }

        [BrowsableAttribute(false)]
        public ISpectrumProvider SpectrumProvider
        {
            get { return _spectrumProvider; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                _spectrumProvider = value;

                RaisePropertyChanged("SpectrumProvider");
            }
        }

        public bool IsXLogScale
        {
            get { return _isXLogScale; }
            set
            {
                _isXLogScale = value;
                UpdateFrequencyMapping();
                RaisePropertyChanged("IsXLogScale");
            }
        }

        public ScalingStrategy ScalingStrategy
        {
            get { return _scalingStrategy; }
            set
            {
                _scalingStrategy = value;
                RaisePropertyChanged("ScalingStrategy");
            }
        }

        public bool UseAverage
        {
            get { return _useAverage; }
            set
            {
                _useAverage = value;
                RaisePropertyChanged("UseAverage");
            }
        }

        [BrowsableAttribute(false)]
        public CSCore.DSP.FftSize FftSize
        {
            get { return (CSCore.DSP.FftSize)_fftSize; }
            set
            {
                if ((int)Math.Log((int)value, 2) % 1 != 0)
                    throw new ArgumentOutOfRangeException("value");

                _fftSize = (int)value;
                _maxFftIndex = _fftSize / 2 - 1;

                RaisePropertyChanged("FFTSize");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void UpdateFrequencyMapping()
        {
            _maximumFrequencyIndex = Math.Min(_spectrumProvider.GetFftBandIndex(MaximumFrequency) + 1, _maxFftIndex);
            _minimumFrequencyIndex = Math.Min(_spectrumProvider.GetFftBandIndex(MinimumFrequency), _maxFftIndex);

            int actualResolution = SpectrumResolution;

            int indexCount = _maximumFrequencyIndex - _minimumFrequencyIndex;
            double linearIndexBucketSize = Math.Round(indexCount / (double)actualResolution, 3);

            _spectrumIndexMax = _spectrumIndexMax.CheckBuffer(actualResolution, true);
            _spectrumLogScaleIndexMax = _spectrumLogScaleIndexMax.CheckBuffer(actualResolution, true);

            double maxLog = Math.Log(actualResolution, actualResolution);
            for (int i = 1; i < actualResolution; i++)
            {
                int logIndex =
                    (int)((maxLog - Math.Log((actualResolution + 1) - i, (actualResolution + 1))) * indexCount) +
                    _minimumFrequencyIndex;

                _spectrumIndexMax[i - 1] = _minimumFrequencyIndex + (int)(i * linearIndexBucketSize);
                _spectrumLogScaleIndexMax[i - 1] = logIndex;
            }

            if (actualResolution > 0)
            {
                _spectrumIndexMax[_spectrumIndexMax.Length - 1] =
                    _spectrumLogScaleIndexMax[_spectrumLogScaleIndexMax.Length - 1] = _maximumFrequencyIndex;
            }
        }

        public virtual SpectrumPointData[] CalculateSpectrumPoints(double maxValue, float[] fftBuffer)
        {
            var dataPoints = new List<SpectrumPointData>();

            double value0 = 0, value = 0;
            double lastValue = 0;
            double actualMaxValue = maxValue;
            int spectrumPointIndex = 0;

            for (int i = _minimumFrequencyIndex; i <= _maximumFrequencyIndex; i++)
            {
                switch (ScalingStrategy)
                {
                    case ScalingStrategy.Decibel:
                        value0 = (((20 * Math.Log10(fftBuffer[i])) - MinDbValue) / DbScale) * actualMaxValue;
                        break;
                    case ScalingStrategy.Linear:
                        value0 = (fftBuffer[i] * ScaleFactorLinear) * actualMaxValue;
                        break;
                    case ScalingStrategy.Sqrt:
                        value0 = ((Math.Sqrt(fftBuffer[i])) * ScaleFactorSqr) * actualMaxValue;
                        break;
                }

                bool recalc = true;

                value = Math.Max(0, Math.Max(value0, value));

                while (spectrumPointIndex <= _spectrumIndexMax.Length - 1 &&
                       i ==
                       (IsXLogScale
                           ? _spectrumLogScaleIndexMax[spectrumPointIndex]
                           : _spectrumIndexMax[spectrumPointIndex]))
                {
                    if (!recalc)
                        value = lastValue;

                    if (value > maxValue)
                        value = maxValue;

                    if (_useAverage && spectrumPointIndex > 0)
                        value = (lastValue + value) / 2.0;

                    dataPoints.Add(new SpectrumPointData { SpectrumPointIndex = spectrumPointIndex, Value = value });

                    lastValue = value;
                    value = 0.0;
                    spectrumPointIndex++;
                    recalc = false;
                }

                //value = 0;
            }

            return dataPoints.ToArray();
        }

        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null && !String.IsNullOrEmpty(propertyName))
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        [DebuggerDisplay("{Value}")]
        public struct SpectrumPointData
        {
            public int SpectrumPointIndex;
            public double Value;
        }
    }
}
namespace WinformsVisualization.Visualization
{
    public enum ScalingStrategy
    {
        Decibel,
        Linear,
        Sqrt
    }
}