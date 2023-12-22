using System;
using System.Windows.Forms;
using EO.WebBrowser;
namespace Playzer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        public void InitializeChromium()
        {
            EO.WebEngine.BrowserOptions options = new EO.WebEngine.BrowserOptions();
            options.EnableWebSecurity = false;
            EO.WebBrowser.Runtime.DefaultEngineOptions.SetDefaultBrowserOptions(options);
            EO.WebEngine.Engine.Default.Options.AllowProprietaryMediaFormats();
            EO.WebEngine.Engine.Default.Options.SetDefaultBrowserOptions(new EO.WebEngine.BrowserOptions
            {
                EnableWebSecurity = false
            });
            this.webView1.Create(pictureBox1.Handle);
            this.webView1.Engine.Options.AllowProprietaryMediaFormats();
            this.webView1.SetOptions(new EO.WebEngine.BrowserOptions
            {
                EnableWebSecurity = false
            });
            this.webView1.Engine.Options.DisableGPU = false;
            this.webView1.Engine.Options.DisableSpellChecker = true;
            this.webView1.Engine.Options.CustomUserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";
            Navigate("https://www.deezer.com/fr/profile/4455794242/artists");
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
                webView1.Url = address;
            }
            catch (System.UriFormatException)
            {
                return;
            }
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
            this.webView1.QueueScriptCall(stringinject);
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.webView1.Dispose();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            InitializeChromium();
        }
    }
}