// Instalador y actualizador de la traduccion al espanol de ArcheAge Chronicles (no oficial).
// Compilar con csc de .NET Framework 4 (ver README). Lleva la traduccion incrustada para funcionar sin internet
// y comprueba en MANIFEST_URL si hay una version mas nueva publicada.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Traduccion al espanol de ArcheAge Chronicles")]
[assembly: AssemblyProduct("ArcheAge Chronicles ES")]
[assembly: AssemblyVersion("1.0.0.0")]

namespace AacEs
{
    static class Config
    {
        // Manifiesto de la ultima release de GitHub (cada release publica manifest.json y String.locres).
        public const string REPO_URL = "https://github.com/poal-script/archeage-chronicles-es";
        public const string MANIFEST_URL = REPO_URL + "/releases/latest/download/manifest.json";
        public const string RELEASES_URL = REPO_URL + "/releases/latest";
        public const string KOFI_URL = "https://ko-fi.com/poalscript";
        public const string APP_ID = "3218230";
        public const string CULTURE = "es-ES";
        public const string LOCRES_REL = @"X5\Content\Localization\String\es-ES\String.locres";
        public const string VERSION_FILE_REL = @"X5\Content\Localization\String\es-ES\traduccion-version.json";
    }

    class Manifest
    {
        public string version;   // version de la traduccion, p. ej. "1.0.0"
        public string build;     // build de Steam para el que se hizo
        public string locres;    // URL (absoluta o relativa al manifiesto) del String.locres
        public string sha256;    // hash del String.locres
        public string notes;     // novedades (opcional)
        public string installer; // URL del instalador nuevo (opcional)
        public string installerVersion; // version del instalador (opcional)
    }

    static class Util
    {
        public static string Sha256(byte[] data)
        {
            using (var h = SHA256.Create())
            {
                var sb = new StringBuilder();
                foreach (var b in h.ComputeHash(data)) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public static int CompareVersions(string a, string b)
        {
            var pa = (a ?? "0").Split('.'); var pb = (b ?? "0").Split('.');
            for (int i = 0; i < Math.Max(pa.Length, pb.Length); i++)
            {
                int x = 0, y = 0;
                if (i < pa.Length) int.TryParse(pa[i], out x);
                if (i < pb.Length) int.TryParse(pb[i], out y);
                if (x != y) return x.CompareTo(y);
            }
            return 0;
        }

        public static byte[] Download(string url)
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2
            using (var wc = new WebClient())
            {
                wc.Headers.Add("User-Agent", "AacEsInstaller/1.0");
                return wc.DownloadData(url);
            }
        }

        public static Manifest Embedded()
        {
            var json = ReadResourceText("manifest.json");
            return json == null ? null : new JavaScriptSerializer().Deserialize<Manifest>(json);
        }

        public static byte[] ReadResource(string name)
        {
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (s == null) return null;
                var ms = new MemoryStream(); s.CopyTo(ms); return ms.ToArray();
            }
        }

        public static string ReadResourceText(string name)
        {
            var b = ReadResource(name);
            return b == null ? null : Encoding.UTF8.GetString(b);
        }
    }

    static class Game
    {
        public static List<string> SteamLibraries()
        {
            var libs = new List<string>();
            string steam = null;
            foreach (var key in new[] { @"HKEY_CURRENT_USER\Software\Valve\Steam", @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam" })
            {
                var v = Registry.GetValue(key, key.StartsWith("HKEY_CURRENT") ? "SteamPath" : "InstallPath", null) as string;
                if (!string.IsNullOrEmpty(v)) { steam = v.Replace('/', '\\'); break; }
            }
            if (steam == null) steam = @"C:\Program Files (x86)\Steam";
            libs.Add(steam);
            var vdf = Path.Combine(steam, @"steamapps\libraryfolders.vdf");
            if (File.Exists(vdf))
                foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
                {
                    var p = m.Groups[1].Value.Replace(@"\\", @"\");
                    if (!libs.Exists(x => string.Equals(x.TrimEnd('\\'), p.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))) libs.Add(p);
                }
            return libs;
        }

        // Devuelve (carpeta del juego, build) o null.
        public static string[] Find()
        {
            foreach (var lib in SteamLibraries())
            {
                var acf = Path.Combine(lib, @"steamapps\appmanifest_" + Config.APP_ID + ".acf");
                if (!File.Exists(acf)) continue;
                var t = File.ReadAllText(acf);
                var dir = Regex.Match(t, "\"installdir\"\\s+\"([^\"]+)\"").Groups[1].Value;
                var build = Regex.Match(t, "\"buildid\"\\s+\"(\\d+)\"").Groups[1].Value;
                var root = Path.Combine(lib, @"steamapps\common", dir);
                if (File.Exists(Path.Combine(root, "ArcheAgeChronicles.exe"))) return new[] { root, build };
            }
            return null;
        }

        public static bool Running()
        {
            foreach (var n in new[] { "ArcheAgeChronicles", "xldr_ArcheageChronicles_GL_loader_x64" })
                if (Process.GetProcessesByName(n).Length > 0) return true;
            return false;
        }

        public static string IniPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"X5\Saved\Config\Windows\GameUserSettings.ini");
        }

        public static string CurrentCulture()
        {
            var ini = IniPath();
            if (!File.Exists(ini)) return null;
            var m = Regex.Match(File.ReadAllText(ini), @"(?m)^Culture=(.*)$");
            return m.Success ? m.Groups[1].Value.Trim() : null;
        }

        public static void SetCulture(string culture)
        {
            var ini = IniPath();
            if (!File.Exists(ini)) throw new Exception("No se encuentra la configuracion del juego:\n" + ini + "\n\nAbre el juego una vez, cierralo y vuelve a intentarlo.");
            var backup = ini + ".antes-de-traduccion";
            if (!File.Exists(backup)) File.Copy(ini, backup);
            var text = File.ReadAllText(ini);
            var nl = text.Contains("\r\n") ? "\r\n" : "\n";
            if (Regex.IsMatch(text, @"(?ms)^\[Internationalization\][^\[]*?^Culture="))
                text = new Regex(@"(?m)^(Culture=).*$").Replace(text, "${1}" + culture, 1);
            else if (Regex.IsMatch(text, @"(?m)^\[Internationalization\]\s*$"))
                text = new Regex(@"(?m)^\[Internationalization\]\s*$").Replace(text, "[Internationalization]" + nl + "Culture=" + culture, 1);
            else
                text = text.TrimEnd() + nl + nl + "[Internationalization]" + nl + "Culture=" + culture + nl;
            File.WriteAllText(ini, text, new UTF8Encoding(false));
        }

        public static Manifest InstalledVersion(string root)
        {
            var f = Path.Combine(root, Config.VERSION_FILE_REL);
            if (!File.Exists(f)) return null;
            try { return new JavaScriptSerializer().Deserialize<Manifest>(File.ReadAllText(f)); } catch { return null; }
        }

        public static void Install(string root, byte[] locres, Manifest m)
        {
            if (Util.Sha256(locres) != (m.sha256 ?? "").ToLowerInvariant())
                throw new Exception("El archivo de traduccion no supera la comprobacion de integridad (SHA-256). No se ha instalado nada.");
            var dest = Path.Combine(root, Config.LOCRES_REL);
            Directory.CreateDirectory(Path.GetDirectoryName(dest));
            var tmp = dest + ".nuevo";
            File.WriteAllBytes(tmp, locres);
            if (File.Exists(dest)) File.Delete(dest);
            File.Move(tmp, dest);
            File.WriteAllText(Path.Combine(root, Config.VERSION_FILE_REL), new JavaScriptSerializer().Serialize(m), new UTF8Encoding(false));
        }
    }

    class MainForm : Form
    {
        Label lblGame, lblInstalled, lblAvailable, lblStatus;
        Button btnInstall, btnDisable, btnCheck;
        TextBox txtNotes;
        string gameRoot, gameBuild;
        Manifest embedded, remote;
        byte[] remoteLocres;

        public MainForm()
        {
            Text = "ArcheAge Chronicles - Traduccion al espanol";
            Width = 560; Height = 470; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen; Font = new Font("Segoe UI", 9.5f);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            var title = new Label { Text = "Traduccion al espanol (no oficial)", Font = new Font("Segoe UI", 13f, FontStyle.Bold), Left = 16, Top = 12, Width = 520, Height = 28 };
            var links = new LinkLabel { Text = "por poal-script  ·  GitHub  ·  Apoya el proyecto en Ko-fi", Left = 16, Top = 380, Width = 520, Height = 22 };
            links.Links.Add(links.Text.IndexOf("GitHub"), 6, Config.REPO_URL);
            links.Links.Add(links.Text.IndexOf("Apoya"), "Apoya el proyecto en Ko-fi".Length, Config.KOFI_URL);
            links.LinkClicked += (s, e) => { try { Process.Start((string)e.Link.LinkData); } catch { } };
            Controls.Add(links);
            lblGame = new Label { Left = 16, Top = 50, Width = 520, Height = 36 };
            lblInstalled = new Label { Left = 16, Top = 90, Width = 520, Height = 20 };
            lblAvailable = new Label { Left = 16, Top = 112, Width = 520, Height = 20 };
            txtNotes = new TextBox { Left = 16, Top = 138, Width = 512, Height = 120, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
            btnInstall = new Button { Text = "Instalar / Actualizar", Left = 16, Top = 270, Width = 170, Height = 34 };
            btnDisable = new Button { Text = "Volver al ingles", Left = 196, Top = 270, Width = 150, Height = 34 };
            btnCheck = new Button { Text = "Buscar actualizacion", Left = 356, Top = 270, Width = 172, Height = 34 };
            lblStatus = new Label { Left = 16, Top = 314, Width = 520, Height = 60, ForeColor = Color.DimGray };
            Controls.AddRange(new Control[] { title, lblGame, lblInstalled, lblAvailable, txtNotes, btnInstall, btnDisable, btnCheck, lblStatus });
            btnInstall.Click += delegate { DoInstall(); };
            btnDisable.Click += delegate { DoDisable(); };
            btnCheck.Click += delegate { CheckRemote(true); };
            Shown += delegate { Refresh2(); CheckRemote(false); };
        }

        void Refresh2()
        {
            embedded = Util.Embedded();
            var g = Game.Find();
            if (g == null)
            {
                gameRoot = null;
                lblGame.Text = "No se ha encontrado ArcheAge Chronicles instalado con Steam.";
                btnInstall.Enabled = btnDisable.Enabled = false;
            }
            else
            {
                gameRoot = g[0]; gameBuild = g[1];
                lblGame.Text = "Juego: " + gameRoot + "\nBuild de Steam: " + gameBuild + "    Idioma actual: " + (Game.CurrentCulture() ?? "?");
                btnInstall.Enabled = btnDisable.Enabled = true;
            }
            var inst = gameRoot == null ? null : Game.InstalledVersion(gameRoot);
            lblInstalled.Text = "Traduccion instalada: " + (inst == null ? "ninguna" : inst.version + " (build " + inst.build + ")");
            var best = Best();
            lblAvailable.Text = "Traduccion disponible: " + (best == null ? "?" : best.version + " (build " + best.build + ")" + (best == remote ? " - descargable" : " - incluida en este instalador"));
            if (best != null && !string.IsNullOrEmpty(best.notes)) txtNotes.Text = best.notes.Replace("\n", "\r\n");
        }

        Manifest Best()
        {
            if (remote != null && (embedded == null || Util.CompareVersions(remote.version, embedded.version) > 0)) return remote;
            return embedded;
        }

        void CheckRemote(bool verbose)
        {
            lblStatus.Text = "Buscando actualizaciones...";
            var t = new Thread(delegate ()
            {
                string msg;
                try
                {
                    var data = Util.Download(Config.MANIFEST_URL);
                    var m = new JavaScriptSerializer().Deserialize<Manifest>(Encoding.UTF8.GetString(data));
                    remote = m;
                    msg = "Ultima version publicada: " + m.version + " (build " + m.build + ").";
                    var myVer = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
                    if (!string.IsNullOrEmpty(m.installerVersion) && Util.CompareVersions(m.installerVersion, myVer) > 0)
                        msg += "\nHay un instalador nuevo (" + m.installerVersion + "). Descargalo en: " + Config.RELEASES_URL;
                }
                catch (Exception ex)
                {
                    msg = verbose ? "No se ha podido comprobar si hay actualizaciones (" + ex.Message + "). Se usara la version incluida." : "Sin conexion con el servidor de actualizaciones: se usara la version incluida.";
                }
                BeginInvoke((MethodInvoker)delegate { lblStatus.Text = msg; Refresh2(); });
            });
            t.IsBackground = true; t.Start();
        }

        void DoInstall()
        {
            try
            {
                if (Game.Running()) { MessageBox.Show("Cierra el juego y su lanzador antes de instalar.", Text); return; }
                var m = Best();
                if (m == null) throw new Exception("No hay ninguna traduccion disponible.");
                if (!string.IsNullOrEmpty(gameBuild) && m.build != gameBuild)
                {
                    var r = MessageBox.Show("La traduccion " + m.version + " se hizo para el build " + m.build + ", pero tu juego esta en el build " + gameBuild + ".\n\nPuede que haya textos sin traducir o descolocados. Si algo falla, usa \"Volver al ingles\".\n\nInstalar de todos modos?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (r != DialogResult.Yes) return;
                }
                byte[] locres;
                if (m == remote)
                {
                    lblStatus.Text = "Descargando la traduccion " + m.version + "..."; Application.DoEvents();
                    var url = m.locres;
                    if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) url = new Uri(new Uri(Config.MANIFEST_URL), url).ToString();
                    locres = remoteLocres ?? Util.Download(url);
                    remoteLocres = locres;
                }
                else locres = Util.ReadResource("String.locres");
                Game.Install(gameRoot, locres, m);
                Game.SetCulture(Config.CULTURE);
                lblStatus.Text = "Traduccion " + m.version + " instalada y espanol activado. Abre el juego desde Steam.";
                Refresh2();
                MessageBox.Show("Listo. Abre el juego desde Steam.\n\nSi sigue en ingles, anade -culture=es-ES en Steam > Propiedades > Opciones de lanzamiento.", Text);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Sin permisos para escribir en la carpeta del juego. Ejecuta el instalador como administrador.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex) { lblStatus.Text = "Error: " + ex.Message; MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        void DoDisable()
        {
            try
            {
                if (Game.Running()) { MessageBox.Show("Cierra el juego antes de cambiar el idioma.", Text); return; }
                Game.SetCulture("en");
                lblStatus.Text = "Ingles activado. Los archivos de la traduccion se quedan instalados.";
                Refresh2();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }

    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            // Modo sin ventana para pruebas: /status
            if (args.Length > 0 && args[0] == "/status")
            {
                var g = Game.Find(); var e = Util.Embedded();
                Console.WriteLine("juego=" + (g == null ? "-" : g[0]) + " build=" + (g == null ? "-" : g[1]));
                Console.WriteLine("incluida=" + (e == null ? "-" : e.version + "/" + e.build + "/" + e.sha256));
                var loc = Util.ReadResource("String.locres");
                Console.WriteLine("locres_incluido_ok=" + (loc != null && e != null && Util.Sha256(loc) == e.sha256));
                Console.WriteLine("instalada=" + (g == null ? "-" : (Game.InstalledVersion(g[0]) == null ? "ninguna" : Game.InstalledVersion(g[0]).version)));
                Console.WriteLine("cultura=" + Game.CurrentCulture());
                return 0;
            }
            Application.EnableVisualStyles();
            Application.Run(new MainForm());
            return 0;
        }
    }
}
