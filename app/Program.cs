// Terrain Inspector for Windows: shows the tool in its own window, using Microsoft Edge WebView2 (part of Windows 10 and 11).
//
// Built to be plain and easy to check:
//  - nothing is packed inside this .exe: the tool is the file page\terrain-inspector.html next to it, readable by anyone;
//  - no installer, no extraction, no admin rights, no registry, no internet connection;
//  - the only folder it writes to is its own settings folder, %LOCALAPPDATA%\TerrainInspector.
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
        const string Host = "terrain-inspector.example";      // a made-up name, only ever answered inside this window
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
            string pageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "page");
            if (!File.Exists(Path.Combine(pageDir, Page)))
            {
                MessageBox.Show("The file page\\" + Page + " is missing next to Terrain Inspector.exe.\n\nUnzip the whole Terrain Inspector folder and run the program from it.",
                    "Terrain Inspector", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close(); return;
            }
            try
            {
                // settings and remembered folders go to the user's AppData, so the program also runs from a read-only folder
                string data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TerrainInspector", "WebView2");
                var env = await CoreWebView2Environment.CreateAsync(null, data);
                await web.EnsureCoreWebView2Async(env);
            }
            catch (WebView2RuntimeNotFoundException)
            {
                MessageBox.Show("Terrain Inspector needs the Microsoft Edge WebView2 Runtime. It is part of Windows 10 and 11, but it is missing or turned off on this PC.\n\n" +
                    "You can get it from Microsoft (search for \"WebView2 Runtime\"), or open page\\" + Page + " in your browser instead: it is the same tool.",
                    "Terrain Inspector", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Close(); return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Terrain Inspector couldn't start its window:\n\n" + ex.Message + "\n\nYou can open page\\" + Page + " in your browser instead: it is the same tool.",
                    "Terrain Inspector", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close(); return;
            }

            var cw = web.CoreWebView2;
            // the page folder is shown under the made-up address; nothing else on the PC is reachable that way
            cw.SetVirtualHostNameToFolderMapping(Host, pageDir, CoreWebView2HostResourceAccessKind.DenyCors);
            cw.Settings.IsStatusBarEnabled = false;
            cw.Settings.AreDefaultContextMenusEnabled = true;
            cw.Settings.IsPasswordAutosaveEnabled = false;
            cw.Settings.IsGeneralAutofillEnabled = false;
            // links (Nexus, GitHub…) open in the normal browser; the window itself only ever shows the tool
            cw.NewWindowRequested += (s, e) => { e.Handled = true; OpenOutside(e.Uri); };
            cw.NavigationStarting += (s, e) =>
            {
                if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var u) && (u.Scheme == "http" || u.Scheme == "https") && u.Host != Host)
                { e.Cancel = true; OpenOutside(e.Uri); }
            };
            cw.DocumentTitleChanged += (s, e) => { var t = cw.DocumentTitle; Text = string.IsNullOrWhiteSpace(t) ? "Terrain Inspector" : t; };
            cw.Navigate("https://" + Host + "/" + Page);
        }

        static void OpenOutside(string url)
        {
            // only web links, handed to the user's default browser
            if (!Uri.TryCreate(url, UriKind.Absolute, out var u) || (u.Scheme != "http" && u.Scheme != "https")) return;
            try { Process.Start(new ProcessStartInfo(u.AbsoluteUri) { UseShellExecute = true }); } catch { }
        }
    }
}
