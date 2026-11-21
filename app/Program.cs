// Terrain Inspector for Windows: opens terrain-inspector.html (next to this .exe) in its own window,
// using Microsoft Edge WebView2, which is part of Windows 10 and 11. Nothing goes to the internet:
// the page is served from the program's own folder under a made-up local address.
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace TerrainInspector
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    sealed class MainForm : Form
    {
        const string Host = "terrain-inspector.example";      // never leaves this PC
        const string Page = "terrain-inspector.html";
        readonly WebView2 web = new WebView2 { Dock = DockStyle.Fill };

        public MainForm()
        {
            Text = "Terrain Inspector";
            Width = 1500; Height = 950;
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            try { Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            Controls.Add(web);
            Load += async (s, e) => await Start();
        }

        async Task Start()
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            if (!File.Exists(Path.Combine(dir, Page)))
            {
                MessageBox.Show($"{Page} is missing.\n\nKeep it in the same folder as Terrain Inspector.exe (unzip the whole download, not only the .exe).",
                    "Terrain Inspector", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close(); return;
            }
            try
            {
                // settings and remembered folders are kept in the user's AppData, so the program also works from a read-only folder
                string data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TerrainInspector", "WebView2");
                var env = await CoreWebView2Environment.CreateAsync(null, data);
                await web.EnsureCoreWebView2Async(env);
            }
            catch (WebView2RuntimeNotFoundException)
            {
                var r = MessageBox.Show("Terrain Inspector needs the Microsoft Edge WebView2 Runtime, which is part of Windows 10 and 11 but missing on this PC.\n\nOpen Microsoft's download page now?",
                    "Terrain Inspector", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r == DialogResult.Yes) OpenOutside("https://developer.microsoft.com/microsoft-edge/webview2/");
                Close(); return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Terrain Inspector could not start its window:\n\n" + ex.Message, "Terrain Inspector", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close(); return;
            }

            var cw = web.CoreWebView2;
            cw.SetVirtualHostNameToFolderMapping(Host, dir, CoreWebView2HostResourceAccessKind.Deny);
            cw.Settings.IsStatusBarEnabled = false;
            cw.Settings.AreDefaultContextMenusEnabled = true;
            // links to Nexus, GitHub, etc. open in the normal browser, not inside the tool
            cw.NewWindowRequested += (s, e) => { e.Handled = true; OpenOutside(e.Uri); };
            cw.NavigationStarting += (s, e) =>
            {
                if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var u) && (u.Scheme == "http" || u.Scheme == "https") && u.Host != Host)
                { e.Cancel = true; OpenOutside(e.Uri); }
            };
            cw.DocumentTitleChanged += (s, e) => { var t = cw.DocumentTitle; Text = string.IsNullOrWhiteSpace(t) ? "Terrain Inspector" : t; };
            cw.Navigate($"https://{Host}/{Page}");
        }

        static void OpenOutside(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
        }
    }
}
