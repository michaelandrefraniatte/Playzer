using System;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
namespace Playzer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        public ChromiumWebBrowser chromeBrowser;
        public void InitializeChromium()
        {
            CefSettings settings = new CefSettings();
            settings.CachePath = Environment.CurrentDirectory + @"\CEF";
            settings.CefCommandLineArgs.Add("disable-web-security");
            settings.CefCommandLineArgs.Add("allow-running-insecure-content");
            settings.CefCommandLineArgs.Add("enable-media-stream"); 
            settings.CefCommandLineArgs.Add("no-proxy-server");
            settings.CefCommandLineArgs.Add("disable-site-isolation-trials");
            settings.CefCommandLineArgs.Add("enable-features", "CastMediaRouteProvider,NetworkServiceInProcess");
            Cef.Initialize(settings);
            chromeBrowser = new ChromiumWebBrowser("https://www.deezer.com/fr/profile/4455794242/artists");
            BrowserSettings browserSettings = new BrowserSettings();
            browserSettings.WindowlessFrameRate = 15;
            chromeBrowser.BrowserSettings = browserSettings;
            this.pictureBox1.Controls.Add(chromeBrowser);
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.pictureBox1.Visible = false;
                }
                else
                {
                    this.pictureBox1.Visible = true;
                }
            }
            catch { }
            string stringinject = @"
                    var scripts = document.getElementsByTagName('script');
                    for (let i = 0; i < scripts.length; i++)
                    {
                        var content = scripts[i].innerHTML;
                        if (content.indexOf('ads') > -1) {
                            scripts[i].innerHTML = '';
                        }
                        var src = scripts[i].getAttribute('src');
                        if (src.indexOf('ads') > -1) {
                            scripts[i].setAttribute('src', '');
                        }
                    }
                    var iframes = document.getElementsByTagName('iframe');
                    for (let i = 0; i < iframes.length; i++)
                    {
                        var content = iframes[i].innerHTML;
                        if (content.indexOf('ads') > -1) {
                            iframes[i].innerHTML = '';
                        }
                        var src = iframes[i].getAttribute('src');
                        if (src.indexOf('ads') > -1) {
                            iframes[i].setAttribute('src', '');
                        }
                    }
                    var allelements = document.querySelectorAll('*');
                    for (var i = 0; i < allelements.length; i++) {
	                    var classname = allelements[i].className;
                        if (classname.indexOf('ads') > -1)  {
                                allelements[i].innerHTML = '';
			            }
                    }
                ".Replace("\r\n", " ");
            chromeBrowser.ExecuteScriptAsyncWhenPageLoaded(stringinject);
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Cef.Shutdown();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            InitializeChromium();
        }
    }
}