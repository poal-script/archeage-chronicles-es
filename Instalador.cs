// Instalador y actualizador de las traducciones no oficiales de ArcheAge Chronicles (es, fr, de, it, pl).
// Compilar con publicar.ps1 (csc de .NET Framework 4, C# 5). Lleva incrustado languages.json y los String.<idioma>.locres
// que haya, para funcionar sin internet, y comprueba en CATALOG_URL si hay versiones mas nuevas.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
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

[assembly: AssemblyTitle("ArcheAge Chronicles translations")]
[assembly: AssemblyProduct("ArcheAge Chronicles Translations")]
[assembly: AssemblyVersion("2.0.0.0")]

namespace AacTr
{
    static class Config
    {
        // Cada release de GitHub publica languages.json (+ un String.<idioma>.locres por idioma disponible).
        public const string REPO_URL = "https://github.com/poal-script/archeage-chronicles-es";
        public const string CATALOG_URL = REPO_URL + "/releases/latest/download/languages.json";
        public const string RELEASES_URL = REPO_URL + "/releases/latest";
        public const string ISSUES_URL = REPO_URL + "/issues";
        public const string KOFI_URL = "https://ko-fi.com/poalscript";
        public const string APP_ID = "3218230";
        public static string LocresRel(string culture) { return @"X5\Content\Localization\String\" + culture + @"\String.locres"; }
        public static string VersionRel(string culture) { return @"X5\Content\Localization\String\" + culture + @"\traduccion-version.json"; }
    }

    // Una entrada de languages.json. Los nombres de campo coinciden con el antiguo manifest.json (compatible).
    class LangInfo
    {
        public string code;      // es, fr, de, it, pl, en
        public string culture;   // es-ES, fr-FR, de-DE, it-IT, pl-PL, en
        public string status;    // complete | partial | coming | original
        public int coverage;     // % traducido (0-100)
        public string version;   // version de la traduccion, p. ej. "1.2.0"
        public string build;     // build de Steam para el que se hizo
        public string locres;    // URL (absoluta o relativa al catalogo) del String.locres
        public string sha256;    // hash del String.locres
        public string notes;
    }

    class Catalog
    {
        public string installerVersion;
        public string installer;
        public List<LangInfo> languages;
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
                wc.Headers.Add("User-Agent", "AacTranslationsInstaller/2.0");
                return wc.DownloadData(url);
            }
        }

        public static byte[] ReadResource(string name)
        {
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (s == null) return null;
                var ms = new MemoryStream(); s.CopyTo(ms); return ms.ToArray();
            }
        }

        public static bool HasResource(string name)
        {
            foreach (var n in Assembly.GetExecutingAssembly().GetManifestResourceNames()) if (n == name) return true;
            return false;
        }

        public static string ReadResourceText(string name)
        {
            var b = ReadResource(name);
            return b == null ? null : Encoding.UTF8.GetString(b);
        }

        public static Catalog EmbeddedCatalog()
        {
            var json = ReadResourceText("languages.json");
            Catalog c = null;
            if (json != null) { try { c = new JavaScriptSerializer().Deserialize<Catalog>(json); } catch { } }
            if (c == null) c = new Catalog();
            if (c.languages == null) c.languages = new List<LangInfo>();
            if (c.languages.Count == 0)
            {
                foreach (var p in new[] { "es:es-ES", "fr:fr-FR", "de:de-DE", "it:it-IT", "pl:pl-PL", "en:en" })
                    c.languages.Add(new LangInfo { code = p.Split(':')[0], culture = p.Split(':')[1], status = p.StartsWith("en:") ? "original" : "coming" });
            }
            return c;
        }

        // Carpeta de ajustes propios del instalador (idioma de la interfaz elegido la ultima vez).
        public static string PrefFile()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"AAC-Traducciones\idioma.txt");
        }
        public static string LoadPref() { try { var f = PrefFile(); return File.Exists(f) ? File.ReadAllText(f).Trim() : null; } catch { return null; } }
        public static void SavePref(string code) { try { var f = PrefFile(); Directory.CreateDirectory(Path.GetDirectoryName(f)); File.WriteAllText(f, code); } catch { } }
    }

    // Textos de la interfaz del instalador. Orden de columnas: es, en, fr, de, it, pl.
    static class T
    {
        public static string Lang = "en";
        public static readonly string[] Order = { "es", "en", "fr", "de", "it", "pl" };
        public static readonly string[] Native = { "Espa\u00f1ol", "English", "Fran\u00e7ais", "Deutsch", "Italiano", "Polski" };
        static Dictionary<string, string[]> S = new Dictionary<string, string[]>();

        public static string Name(string code) { int i = Array.IndexOf(Order, code); return i < 0 ? code : Native[i]; }
        public static string G(string key)
        {
            string[] v; if (!S.TryGetValue(key, out v)) return key;
            int i = Array.IndexOf(Order, Lang); if (i < 0) i = 1;
            return string.IsNullOrEmpty(v[i]) ? v[1] : v[i];
        }
        public static string F(string key, params object[] a) { return string.Format(G(key), a); }
        static void A(string key, string es, string en, string fr, string de, string it, string pl) { S[key] = new[] { es, en, fr, de, it, pl }; }

        static T()
        {
            A("wtitle", "ArcheAge Chronicles \u2013 Traducciones", "ArcheAge Chronicles \u2013 Translations", "ArcheAge Chronicles \u2013 Traductions", "ArcheAge Chronicles \u2013 \u00dcbersetzungen", "ArcheAge Chronicles \u2013 Traduzioni", "ArcheAge Chronicles \u2013 T\u0142umaczenia");
            A("title", "Traducciones de ArcheAge Chronicles (no oficiales)", "ArcheAge Chronicles translations (unofficial)", "Traductions d'ArcheAge Chronicles (non officielles)", "\u00dcbersetzungen f\u00fcr ArcheAge Chronicles (inoffiziell)", "Traduzioni di ArcheAge Chronicles (non ufficiali)", "T\u0142umaczenia ArcheAge Chronicles (nieoficjalne)");
            A("choose", "Elige el idioma del juego", "Choose the game language", "Choisissez la langue du jeu", "W\u00e4hle die Sprache des Spiels", "Scegli la lingua del gioco", "Wybierz j\u0119zyk gry");
            A("game_found", "Juego: {0}\nBuild de Steam: {1}    Idioma actual del juego: {2}", "Game: {0}\nSteam build: {1}    Current game language: {2}", "Jeu : {0}\nBuild Steam : {1}    Langue actuelle du jeu : {2}", "Spiel: {0}\nSteam-Build: {1}    Aktuelle Spielsprache: {2}", "Gioco: {0}\nBuild Steam: {1}    Lingua attuale del gioco: {2}", "Gra: {0}\nBuild Steam: {1}    Aktualny j\u0119zyk gry: {2}");
            A("game_missing", "No se ha encontrado ArcheAge Chronicles instalado con Steam.", "ArcheAge Chronicles was not found installed through Steam.", "ArcheAge Chronicles n'a pas \u00e9t\u00e9 trouv\u00e9 parmi les jeux install\u00e9s via Steam.", "ArcheAge Chronicles wurde in der Steam-Installation nicht gefunden.", "ArcheAge Chronicles non \u00e8 stato trovato tra i giochi installati con Steam.", "Nie znaleziono gry ArcheAge Chronicles zainstalowanej przez Steam.");
            A("installed", "Traducci\u00f3n instalada: {0}", "Installed translation: {0}", "Traduction install\u00e9e : {0}", "Installierte \u00dcbersetzung: {0}", "Traduzione installata: {0}", "Zainstalowane t\u0142umaczenie: {0}");
            A("none", "ninguna", "none", "aucune", "keine", "nessuna", "brak");
            A("available", "Traducci\u00f3n disponible: {0}", "Available translation: {0}", "Traduction disponible : {0}", "Verf\u00fcgbare \u00dcbersetzung: {0}", "Traduzione disponibile: {0}", "Dost\u0119pne t\u0142umaczenie: {0}");
            A("vb", "{0} (build {1})", "{0} (build {1})", "{0} (build {1})", "{0} (Build {1})", "{0} (build {1})", "{0} (build {1})");
            A("included", " \u2013 incluida en este instalador", " \u2013 included in this installer", " \u2013 incluse dans cet installateur", " \u2013 in diesem Installationsprogramm enthalten", " \u2013 inclusa in questo programma di installazione", " \u2013 do\u0142\u0105czone do tego instalatora");
            A("downloadable", " \u2013 descargable", " \u2013 downloadable", " \u2013 t\u00e9l\u00e9chargeable", " \u2013 zum Download", " \u2013 scaricabile", " \u2013 do pobrania");
            A("coverage", "{0} % traducido", "{0}% translated", "{0} % traduit", "{0} % \u00fcbersetzt", "{0}% tradotto", "{0}% przet\u0142umaczono");
            A("coming_title", "Esta traducci\u00f3n est\u00e1 en preparaci\u00f3n", "This translation is in preparation", "Cette traduction est en cours de pr\u00e9paration", "Diese \u00dcbersetzung ist in Vorbereitung", "Questa traduzione \u00e8 in preparazione", "To t\u0142umaczenie jest w przygotowaniu");
            A("coming_help", "Todav\u00eda no est\u00e1 disponible. Sigue el progreso o ofrece tu ayuda en GitHub.", "It is not available yet. Follow the progress or offer your help on GitHub.", "Elle n'est pas encore disponible. Suivez l'avancement ou proposez votre aide sur GitHub.", "Sie ist noch nicht verf\u00fcgbar. Verfolge den Fortschritt oder biete deine Hilfe auf GitHub an.", "Non \u00e8 ancora disponibile. Segui i progressi o offri il tuo aiuto su GitHub.", "Nie jest jeszcze dost\u0119pne. \u015aled\u017a post\u0119py lub zaoferuj pomoc na GitHubie.");
            A("link_help", "Seguir el progreso en GitHub", "Follow progress on GitHub", "Suivre l'avancement sur GitHub", "Fortschritt auf GitHub verfolgen", "Segui i progressi su GitHub", "\u015aled\u017a post\u0119py na GitHubie");
            A("original_title", "Ingl\u00e9s: idioma original del juego", "English: the game's original language", "Anglais : langue d'origine du jeu", "Englisch: Originalsprache des Spiels", "Inglese: lingua originale del gioco", "Angielski: oryginalny j\u0119zyk gry");
            A("original_help", "No necesita traducci\u00f3n. Pulsa el bot\u00f3n para volver al ingl\u00e9s.", "It needs no translation. Press the button to switch back to English.", "Aucune traduction n\u00e9cessaire. Cliquez sur le bouton pour revenir \u00e0 l'anglais.", "Daf\u00fcr ist keine \u00dcbersetzung n\u00f6tig. Klicke auf die Schaltfl\u00e4che, um zu Englisch zur\u00fcckzukehren.", "Non serve alcuna traduzione. Premi il pulsante per tornare all'inglese.", "Nie wymaga t\u0142umaczenia. Naci\u015bnij przycisk, aby wr\u00f3ci\u0107 do angielskiego.");
            A("btn_install", "Instalar / Actualizar", "Install / Update", "Installer / Mettre \u00e0 jour", "Installieren / Aktualisieren", "Installa / Aggiorna", "Zainstaluj / Zaktualizuj");
            A("btn_english", "Volver al ingl\u00e9s", "Back to English", "Revenir \u00e0 l'anglais", "Zur\u00fcck zu Englisch", "Torna all'inglese", "Wr\u00f3\u0107 do angielskiego");
            A("btn_check", "Buscar actualizaci\u00f3n", "Check for updates", "Rechercher une mise \u00e0 jour", "Nach Updates suchen", "Cerca aggiornamenti", "Sprawd\u017a aktualizacje");
            A("btn_donate", "Donar", "Donate", "Faire un don", "Spenden", "Dona", "Wesprzyj");
            A("btn_go", "\u00a1Juego cerrado! Instalar", "Game closed! Install", "Jeu ferm\u00e9 ! Installer", "Spiel geschlossen! Installieren", "Gioco chiuso! Installa", "Gra zamkni\u0119ta! Zainstaluj");
            A("btn_cancel", "Cancelar", "Cancel", "Annuler", "Abbrechen", "Annulla", "Anuluj");
            A("confirm", "Antes de instalar, cierra el juego y su lanzador.\n\nDona para que el proyecto se actualice: cada parche del juego necesita traducir y publicar los textos nuevos.",
                "Before installing, close the game and its launcher.\n\nDonate to keep the project up to date: every game patch needs the new texts translated and published.",
                "Avant d'installer, fermez le jeu et son lanceur.\n\nFaites un don pour que le projet reste \u00e0 jour : chaque patch du jeu demande de traduire et de publier les nouveaux textes.",
                "Schlie\u00dfe vor der Installation das Spiel und seinen Launcher.\n\nSpende, damit das Projekt aktuell bleibt: Jeder Spiel-Patch erfordert, dass die neuen Texte \u00fcbersetzt und ver\u00f6ffentlicht werden.",
                "Prima di installare, chiudi il gioco e il suo launcher.\n\nDona per mantenere il progetto aggiornato: ogni patch del gioco richiede di tradurre e pubblicare i nuovi testi.",
                "Przed instalacj\u0105 zamknij gr\u0119 i jej launcher.\n\nWesprzyj projekt, aby by\u0142 aktualizowany: ka\u017cdy patch gry wymaga przet\u0142umaczenia i opublikowania nowych tekst\u00f3w.");
            A("running", "El juego sigue abierto. Cierra el juego y su lanzador e int\u00e9ntalo de nuevo.", "The game is still open. Close the game and its launcher and try again.", "Le jeu est encore ouvert. Fermez le jeu et son lanceur, puis r\u00e9essayez.", "Das Spiel l\u00e4uft noch. Schlie\u00dfe das Spiel und seinen Launcher und versuche es erneut.", "Il gioco \u00e8 ancora aperto. Chiudi il gioco e il suo launcher e riprova.", "Gra jest nadal uruchomiona. Zamknij gr\u0119 i launcher, a potem spr\u00f3buj ponownie.");
            A("running_switch", "Cierra el juego antes de cambiar el idioma.", "Close the game before changing the language.", "Fermez le jeu avant de changer de langue.", "Schlie\u00dfe das Spiel, bevor du die Sprache wechselst.", "Chiudi il gioco prima di cambiare lingua.", "Zamknij gr\u0119 przed zmian\u0105 j\u0119zyka.");
            A("no_translation", "Todav\u00eda no hay ninguna traducci\u00f3n disponible para este idioma.", "There is no translation available for this language yet.", "Aucune traduction n'est encore disponible pour cette langue.", "F\u00fcr diese Sprache ist noch keine \u00dcbersetzung verf\u00fcgbar.", "Non \u00e8 ancora disponibile alcuna traduzione per questa lingua.", "Dla tego j\u0119zyka nie ma jeszcze dost\u0119pnego t\u0142umaczenia.");
            A("build_mismatch", "La traducci\u00f3n {0} se hizo para el build {1}, pero tu juego est\u00e1 en el build {2}.\n\nPuede que haya textos sin traducir o descolocados. Si algo falla, usa \u00abVolver al ingl\u00e9s\u00bb.\n\n\u00bfInstalar de todos modos?",
                "Translation {0} was made for build {1}, but your game is on build {2}.\n\nSome texts may be untranslated or misplaced. If something goes wrong, use \"Back to English\".\n\nInstall anyway?",
                "La traduction {0} a \u00e9t\u00e9 r\u00e9alis\u00e9e pour le build {1}, mais votre jeu est au build {2}.\n\nCertains textes peuvent \u00eatre non traduits ou mal plac\u00e9s. En cas de probl\u00e8me, utilisez \u00ab Revenir \u00e0 l'anglais \u00bb.\n\nInstaller quand m\u00eame ?",
                "Die \u00dcbersetzung {0} wurde f\u00fcr Build {1} erstellt, dein Spiel hat aber Build {2}.\n\nEinige Texte k\u00f6nnten un\u00fcbersetzt oder falsch zugeordnet sein. Falls etwas schiefgeht, nutze \u201eZur\u00fcck zu Englisch\u201c.\n\nTrotzdem installieren?",
                "La traduzione {0} \u00e8 stata realizzata per il build {1}, ma il tuo gioco \u00e8 al build {2}.\n\nAlcuni testi potrebbero non essere tradotti o risultare fuori posto. Se qualcosa va storto, usa \u00abTorna all'inglese\u00bb.\n\nInstallare comunque?",
                "T\u0142umaczenie {0} powsta\u0142o dla buildu {1}, a Twoja gra ma build {2}.\n\nNiekt\u00f3re teksty mog\u0105 by\u0107 nieprzet\u0142umaczone lub \u017ale przypisane. Je\u015bli co\u015b p\u00f3jdzie nie tak, u\u017cyj \u201eWr\u00f3\u0107 do angielskiego\u201d.\n\nZainstalowa\u0107 mimo to?");
            A("downloading", "Descargando la traducci\u00f3n {0}...", "Downloading translation {0}...", "T\u00e9l\u00e9chargement de la traduction {0}...", "\u00dcbersetzung {0} wird heruntergeladen ...", "Download della traduzione {0}...", "Pobieranie t\u0142umaczenia {0}...");
            A("installed_ok", "Traducci\u00f3n {0} instalada y {1} activado. Abre el juego desde Steam.", "Translation {0} installed and {1} enabled. Open the game from Steam.", "Traduction {0} install\u00e9e et {1} activ\u00e9. Lancez le jeu depuis Steam.", "\u00dcbersetzung {0} installiert und {1} aktiviert. Starte das Spiel \u00fcber Steam.", "Traduzione {0} installata e {1} attivato. Avvia il gioco da Steam.", "T\u0142umaczenie {0} zainstalowane, {1} w\u0142\u0105czony. Uruchom gr\u0119 ze Steam.");
            A("done_msg", "Listo. Abre el juego desde Steam.\n\nSi sigue en ingl\u00e9s, a\u00f1ade -culture={0} en Steam > Propiedades > Opciones de lanzamiento.",
                "Done. Open the game from Steam.\n\nIf it is still in English, add -culture={0} in Steam > Properties > Launch options.",
                "Termin\u00e9. Lancez le jeu depuis Steam.\n\nS'il reste en anglais, ajoutez -culture={0} dans Steam > Propri\u00e9t\u00e9s > Options de lancement.",
                "Fertig. Starte das Spiel \u00fcber Steam.\n\nFalls es auf Englisch bleibt, f\u00fcge -culture={0} unter Steam > Eigenschaften > Startoptionen hinzu.",
                "Fatto. Avvia il gioco da Steam.\n\nSe resta in inglese, aggiungi -culture={0} in Steam > Propriet\u00e0 > Opzioni di avvio.",
                "Gotowe. Uruchom gr\u0119 ze Steam.\n\nJe\u015bli nadal jest po angielsku, dodaj -culture={0} w Steam > W\u0142a\u015bciwo\u015bci > Opcje uruchamiania.");
            A("no_perms", "Sin permisos para escribir en la carpeta del juego. Ejecuta el instalador como administrador.", "No permission to write to the game folder. Run the installer as administrator.", "Pas d'autorisation d'\u00e9criture dans le dossier du jeu. Ex\u00e9cutez l'installateur en tant qu'administrateur.", "Keine Schreibrechte f\u00fcr den Spielordner. F\u00fchre das Installationsprogramm als Administrator aus.", "Nessun permesso di scrittura nella cartella del gioco. Esegui il programma come amministratore.", "Brak uprawnie\u0144 do zapisu w folderze gry. Uruchom instalator jako administrator.");
            A("english_on", "Ingl\u00e9s activado. Los archivos de las traducciones se quedan instalados.", "English enabled. The translation files stay installed.", "Anglais activ\u00e9. Les fichiers des traductions restent install\u00e9s.", "Englisch aktiviert. Die \u00dcbersetzungsdateien bleiben installiert.", "Inglese attivato. I file delle traduzioni restano installati.", "W\u0142\u0105czono angielski. Pliki t\u0142umacze\u0144 pozostaj\u0105 zainstalowane.");
            A("checking", "Buscando actualizaciones...", "Checking for updates...", "Recherche de mises \u00e0 jour...", "Suche nach Updates ...", "Ricerca di aggiornamenti...", "Szukanie aktualizacji...");
            A("latest", "\u00daltima versi\u00f3n publicada: {0}.", "Latest published version: {0}.", "Derni\u00e8re version publi\u00e9e : {0}.", "Zuletzt ver\u00f6ffentlichte Version: {0}.", "Ultima versione pubblicata: {0}.", "Ostatnia opublikowana wersja: {0}.");
            A("new_installer", "Hay un instalador nuevo ({0}). Desc\u00e1rgalo en: {1}", "A new installer is available ({0}). Download it from: {1}", "Un nouvel installateur est disponible ({0}). T\u00e9l\u00e9chargez-le ici : {1}", "Ein neues Installationsprogramm ist verf\u00fcgbar ({0}). Download: {1}", "\u00c8 disponibile un nuovo programma di installazione ({0}). Scaricalo qui: {1}", "Dost\u0119pny jest nowy instalator ({0}). Pobierz go tutaj: {1}");
            A("no_conn", "Sin conexi\u00f3n con el servidor de actualizaciones: se usar\u00e1 la versi\u00f3n incluida.", "No connection to the update server: the included version will be used.", "Pas de connexion au serveur de mises \u00e0 jour : la version incluse sera utilis\u00e9e.", "Keine Verbindung zum Update-Server: Es wird die enthaltene Version verwendet.", "Nessuna connessione al server degli aggiornamenti: verr\u00e0 usata la versione inclusa.", "Brak po\u0142\u0105czenia z serwerem aktualizacji: zostanie u\u017cyta do\u0142\u0105czona wersja.");
            A("no_conn_v", "No se ha podido comprobar si hay actualizaciones ({0}). Se usar\u00e1 la versi\u00f3n incluida.", "Could not check for updates ({0}). The included version will be used.", "Impossible de rechercher des mises \u00e0 jour ({0}). La version incluse sera utilis\u00e9e.", "Die Suche nach Updates ist fehlgeschlagen ({0}). Es wird die enthaltene Version verwendet.", "Impossibile cercare aggiornamenti ({0}). Verr\u00e0 usata la versione inclusa.", "Nie uda\u0142o si\u0119 sprawdzi\u0107 aktualizacji ({0}). Zostanie u\u017cyta do\u0142\u0105czona wersja.");
            A("integrity", "El archivo de traducci\u00f3n no supera la comprobaci\u00f3n de integridad (SHA-256). No se ha instalado nada.", "The translation file failed the integrity check (SHA-256). Nothing was installed.", "Le fichier de traduction ne passe pas le contr\u00f4le d'int\u00e9grit\u00e9 (SHA-256). Rien n'a \u00e9t\u00e9 install\u00e9.", "Die \u00dcbersetzungsdatei besteht die Integrit\u00e4tspr\u00fcfung (SHA-256) nicht. Es wurde nichts installiert.", "Il file di traduzione non supera il controllo di integrit\u00e0 (SHA-256). Non \u00e8 stato installato nulla.", "Plik t\u0142umaczenia nie przechodzi kontroli integralno\u015bci (SHA-256). Nic nie zainstalowano.");
            A("no_ini", "No se encuentra la configuraci\u00f3n del juego:\n{0}\n\nAbre el juego una vez, ci\u00e9rralo y vuelve a intentarlo.", "The game settings were not found:\n{0}\n\nOpen the game once, close it and try again.", "Configuration du jeu introuvable :\n{0}\n\nLancez le jeu une fois, fermez-le puis r\u00e9essayez.", "Die Spieleinstellungen wurden nicht gefunden:\n{0}\n\nStarte das Spiel einmal, beende es und versuche es erneut.", "Impostazioni del gioco non trovate:\n{0}\n\nAvvia il gioco una volta, chiudilo e riprova.", "Nie znaleziono ustawie\u0144 gry:\n{0}\n\nUruchom gr\u0119 raz, zamknij j\u0105 i spr\u00f3buj ponownie.");
            A("tile_active", "En uso", "In use", "Actif", "Aktiv", "In uso", "Aktywny");
            A("tile_installed", "Instalado", "Installed", "Install\u00e9", "Installiert", "Installato", "Zainstalowano");
            A("tile_available", "Disponible", "Available", "Disponible", "Verf\u00fcgbar", "Disponibile", "Dost\u0119pne");
            A("tile_soon", "Pr\u00f3ximamente", "Coming soon", "Bient\u00f4t", "Demn\u00e4chst", "In arrivo", "Wkr\u00f3tce");
            A("tile_original", "Original", "Original", "Original", "Original", "Originale", "Orygina\u0142");
            A("link_by", "por poal-script", "by poal-script", "par poal-script", "von poal-script", "di poal-script", "autor: poal-script");
            A("link_support", "Apoya el proyecto en Ko-fi", "Support the project on Ko-fi", "Soutenez le projet sur Ko-fi", "Unterst\u00fctze das Projekt auf Ko-fi", "Sostieni il progetto su Ko-fi", "Wesprzyj projekt na Ko-fi");
            A("fan", "Proyecto de fans, sin relaci\u00f3n con XLGAMES ni Kakao Games.", "Fan project, not affiliated with XLGAMES or Kakao Games.", "Projet de fans, sans lien avec XLGAMES ni Kakao Games.", "Fan-Projekt, nicht mit XLGAMES oder Kakao Games verbunden.", "Progetto di fan, non affiliato a XLGAMES n\u00e9 a Kakao Games.", "Projekt fan\u00f3w, niepowi\u0105zany z XLGAMES ani Kakao Games.");
        }
    }

    // Banderas dibujadas con GDI+ (Windows no pinta las banderas emoji).
    static class Flags
    {
        static GraphicsPath Round(Rectangle r, int rad)
        {
            var p = new GraphicsPath(); int d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure(); return p;
        }

        static void Bands(Graphics g, Rectangle r, bool vertical, float[] weights, Color[] cols)
        {
            float total = 0; foreach (var w in weights) total += w;
            float pos = vertical ? r.Left : r.Top;
            float len = vertical ? r.Width : r.Height;
            for (int i = 0; i < cols.Length; i++)
            {
                float l = len * weights[i] / total;
                using (var b = new SolidBrush(cols[i]))
                {
                    if (vertical) g.FillRectangle(b, pos, r.Top - 1, l + 1.5f, r.Height + 2);
                    else g.FillRectangle(b, r.Left - 1, pos, r.Width + 2, l + 1.5f);
                }
                pos += l;
            }
        }

        public static void Draw(Graphics g, string code, Rectangle r)
        {
            var old = g.SmoothingMode; g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = Round(r, 5))
            {
                var st = g.Save(); g.SetClip(path, CombineMode.Intersect);
                switch (code)
                {
                    case "es": Bands(g, r, false, new float[] { 1, 2, 1 }, new[] { Color.FromArgb(170, 21, 27), Color.FromArgb(241, 191, 0), Color.FromArgb(170, 21, 27) }); break;
                    case "fr": Bands(g, r, true, new float[] { 1, 1, 1 }, new[] { Color.FromArgb(0, 85, 164), Color.White, Color.FromArgb(239, 65, 53) }); break;
                    case "de": Bands(g, r, false, new float[] { 1, 1, 1 }, new[] { Color.Black, Color.FromArgb(221, 0, 0), Color.FromArgb(255, 206, 0) }); break;
                    case "it": Bands(g, r, true, new float[] { 1, 1, 1 }, new[] { Color.FromArgb(0, 146, 70), Color.White, Color.FromArgb(206, 43, 55) }); break;
                    case "pl": Bands(g, r, false, new float[] { 1, 1 }, new[] { Color.White, Color.FromArgb(220, 20, 60) }); break;
                    default: // en: bandera del Reino Unido simplificada
                        using (var b = new SolidBrush(Color.FromArgb(1, 33, 105))) g.FillRectangle(b, r);
                        float h = r.Height; float cx = r.Left + r.Width / 2f, cy = r.Top + r.Height / 2f;
                        using (var p = new Pen(Color.White, h * 0.22f)) { g.DrawLine(p, r.Left, r.Top, r.Right, r.Bottom); g.DrawLine(p, r.Left, r.Bottom, r.Right, r.Top); }
                        using (var p = new Pen(Color.FromArgb(200, 16, 46), h * 0.08f)) { g.DrawLine(p, r.Left, r.Top, r.Right, r.Bottom); g.DrawLine(p, r.Left, r.Bottom, r.Right, r.Top); }
                        using (var p = new Pen(Color.White, h * 0.36f)) { g.DrawLine(p, r.Left, cy, r.Right, cy); g.DrawLine(p, cx, r.Top, cx, r.Bottom); }
                        using (var p = new Pen(Color.FromArgb(200, 16, 46), h * 0.22f)) { g.DrawLine(p, r.Left, cy, r.Right, cy); g.DrawLine(p, cx, r.Top, cx, r.Bottom); }
                        break;
                }
                g.Restore(st);
                using (var pen = new Pen(Color.FromArgb(110, 0, 0, 0), 1f)) g.DrawPath(pen, path);
            }
            g.SmoothingMode = old;
        }
    }

    // Tarjeta clicable con la bandera, el nombre del idioma y su estado.
    class LangTile : Control
    {
        public string Code, NameText = "", Badge = "";
        public Color BadgeColor = Color.Gray;
        public bool Selected;
        bool hover;

        public LangTile()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            Cursor = Cursors.Hand; TabStop = true; Size = new Size(130, 128);
        }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) { OnClick(EventArgs.Empty); e.Handled = true; }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : BackColor);
            var rect = new Rectangle(2, 2, Width - 5, Height - 5);
            using (var path = RoundPath(rect, 10))
            {
                using (var b = new SolidBrush(Selected ? Color.FromArgb(52, 47, 36) : (hover ? Color.FromArgb(46, 50, 60) : Color.FromArgb(37, 40, 49)))) g.FillPath(b, path);
                var border = Selected ? Color.FromArgb(226, 184, 92) : (Focused ? Color.FromArgb(150, 160, 190) : (hover ? Color.FromArgb(110, 118, 140) : Color.FromArgb(62, 67, 80)));
                using (var pen = new Pen(border, Selected ? 3f : 1.5f)) g.DrawPath(pen, path);
            }
            Flags.Draw(g, Code, new Rectangle((Width - 78) / 2, 16, 78, 52));
            var fn = new Font("Segoe UI", 11f, FontStyle.Bold);
            var fb = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            TextRenderer.DrawText(g, NameText, fn, new Rectangle(0, 76, Width, 24), Selected ? Color.FromArgb(240, 208, 130) : Color.FromArgb(232, 234, 240), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, Badge, fb, new Rectangle(0, 100, Width, 20), BadgeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            fn.Dispose(); fb.Dispose();
        }

        static GraphicsPath RoundPath(Rectangle r, int rad)
        {
            var p = new GraphicsPath(); int d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure(); return p;
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
            if (!File.Exists(ini)) throw new Exception(T.F("no_ini", ini));
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

        public static LangInfo InstalledVersion(string root, string culture)
        {
            var f = Path.Combine(root, Config.VersionRel(culture));
            if (!File.Exists(f) || !File.Exists(Path.Combine(root, Config.LocresRel(culture)))) return null;
            try { return new JavaScriptSerializer().Deserialize<LangInfo>(File.ReadAllText(f)); } catch { return null; }
        }

        public static void Install(string root, byte[] locres, LangInfo m)
        {
            if (Util.Sha256(locres) != (m.sha256 ?? "").ToLowerInvariant()) throw new Exception(T.G("integrity"));
            var dest = Path.Combine(root, Config.LocresRel(m.culture));
            Directory.CreateDirectory(Path.GetDirectoryName(dest));
            var tmp = dest + ".nuevo";
            File.WriteAllBytes(tmp, locres);
            if (File.Exists(dest)) File.Delete(dest);
            File.Move(tmp, dest);
            File.WriteAllText(Path.Combine(root, Config.VersionRel(m.culture)), new JavaScriptSerializer().Serialize(m), new UTF8Encoding(false));
        }
    }

    class MainForm : Form
    {
        static readonly Color Bg = Color.FromArgb(26, 28, 34), Fg = Color.FromArgb(230, 232, 238), Dim = Color.FromArgb(150, 156, 170), Gold = Color.FromArgb(226, 184, 92);
        LangTile[] tiles;
        Label lblTitle, lblChoose, lblGame, lblInstalled, lblAvailable, lblStatus, lblFan;
        Button btnInstall, btnDisable, btnCheck;
        LinkLabel links, linkHelp;
        string gameRoot, gameBuild, sel = "en";
        Catalog embedded, remote;
        Dictionary<string, byte[]> remoteLocres = new Dictionary<string, byte[]>();
        readonly bool offline;

        public MainForm(string forcedLang, bool offlineMode)
        {
            offline = offlineMode;
            embedded = Util.EmbeddedCatalog();
            BackColor = Bg; ForeColor = Fg;
            ClientSize = new Size(860, 590); FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen; Font = new Font("Segoe UI", 9.5f);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            lblTitle = new Label { Left = 24, Top = 16, Width = 800, Height = 32, Font = new Font("Segoe UI", 16f, FontStyle.Bold), ForeColor = Gold };
            lblChoose = new Label { Left = 24, Top = 54, Width = 800, Height = 24, Font = new Font("Segoe UI", 11f), ForeColor = Fg };
            Controls.Add(lblTitle); Controls.Add(lblChoose);

            // Orden de las tarjetas: espanol, frances, aleman, italiano, polaco y, al final, ingles (original).
            var order = new[] { "es", "fr", "de", "it", "pl", "en" };
            tiles = new LangTile[order.Length];
            for (int i = 0; i < order.Length; i++)
            {
                var t = new LangTile { Code = order[i], NameText = T.Name(order[i]), Left = 24 + i * 138, Top = 88 };
                var code = order[i];
                t.Click += delegate { Select(code, true); };
                tiles[i] = t; Controls.Add(t);
            }

            lblGame = new Label { Left = 24, Top = 236, Width = 810, Height = 40, ForeColor = Dim };
            lblInstalled = new Label { Left = 24, Top = 282, Width = 810, Height = 22 };
            lblAvailable = new Label { Left = 24, Top = 306, Width = 810, Height = 22 };
            lblStatus = new Label { Left = 24, Top = 336, Width = 810, Height = 64, ForeColor = Dim };
            linkHelp = new LinkLabel { Left = 24, Top = 400, Width = 600, Height = 22, LinkColor = Gold, ActiveLinkColor = Color.White, VisitedLinkColor = Gold, Visible = false };
            linkHelp.LinkClicked += delegate { try { Process.Start(Config.ISSUES_URL); } catch { } };
            btnInstall = MakeButton(24, 432, 240, true); btnDisable = MakeButton(276, 432, 200, false); btnCheck = MakeButton(488, 432, 210, false);
            links = new LinkLabel { Left = 24, Top = 500, Width = 640, Height = 22, LinkColor = Gold, ActiveLinkColor = Color.White, VisitedLinkColor = Gold, ForeColor = Dim };
            lblFan = new Label { Left = 24, Top = 526, Width = 640, Height = 22, ForeColor = Dim, Font = new Font("Segoe UI", 8.5f) };
            Controls.AddRange(new Control[] { lblGame, lblInstalled, lblAvailable, lblStatus, linkHelp, btnInstall, btnDisable, btnCheck, links, lblFan });
            var img = Util.ReadResource("icono.png");
            if (img != null)
            {
                var pic = new PictureBox { Left = 722, Top = 408, Width = 120, Height = 150, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Bg };
                pic.Image = Image.FromStream(new MemoryStream(img)); Controls.Add(pic);
            }
            btnInstall.Click += delegate { DoInstall(); };
            btnDisable.Click += delegate { DoEnglish(); };
            btnCheck.Click += delegate { CheckRemote(true); };

            // Idioma inicial: el que se forzo, el que se uso la ultima vez o el del Windows si lo tenemos; si no, ingles.
            string first = forcedLang ?? Util.LoadPref();
            if (first == null || Array.IndexOf(T.Order, first) < 0)
            {
                var two = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                first = Array.IndexOf(T.Order, two) >= 0 ? two : "en";
            }
            Select(first, false);
            Shown += delegate { if (!offline) CheckRemote(false); };
            tiles[Array.IndexOf(order, first)].Focus();
        }

        Button MakeButton(int x, int y, int w, bool primary)
        {
            var b = new Button { Left = x, Top = y, Width = w, Height = 42, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 10f, primary ? FontStyle.Bold : FontStyle.Regular) };
            b.FlatAppearance.BorderSize = 1;
            if (primary) { b.BackColor = Color.FromArgb(214, 170, 80); b.ForeColor = Color.FromArgb(30, 26, 14); b.FlatAppearance.BorderColor = Color.FromArgb(240, 208, 130); }
            else { b.BackColor = Color.FromArgb(44, 48, 58); b.ForeColor = Fg; b.FlatAppearance.BorderColor = Color.FromArgb(80, 86, 102); }
            // Deshabilitado: apagado y sin dorado, para que se vea que no se puede pulsar.
            b.EnabledChanged += delegate
            {
                if (b.Enabled && primary) { b.BackColor = Color.FromArgb(214, 170, 80); b.ForeColor = Color.FromArgb(30, 26, 14); }
                else if (b.Enabled) { b.BackColor = Color.FromArgb(44, 48, 58); b.ForeColor = Fg; }
                else { b.BackColor = Color.FromArgb(40, 42, 50); b.ForeColor = Color.FromArgb(105, 110, 124); }
            };
            return b;
        }

        // Elegir idioma: cambia toda la interfaz a ese idioma.
        void Select(string code, bool save)
        {
            sel = code; T.Lang = code;
            if (save) Util.SavePref(code);
            Apply();
        }

        LangInfo Entry(string code) { return embedded.languages.Find(x => x.code == code); }

        // Mejor traduccion disponible del idioma: la publicada (si es mas nueva) o la incluida. null = no hay.
        LangInfo Best(string code, out bool isRemote)
        {
            isRemote = false;
            var emb = Entry(code);
            bool embHas = emb != null && !string.IsNullOrEmpty(emb.sha256) && Util.HasResource("String." + code + ".locres");
            LangInfo rem = null;
            if (remote != null && remote.languages != null) rem = remote.languages.Find(x => x.code == code);
            bool remHas = rem != null && !string.IsNullOrEmpty(rem.locres) && !string.IsNullOrEmpty(rem.sha256);
            if (remHas && (!embHas || Util.CompareVersions(rem.version, emb.version) > 0)) { isRemote = true; return rem; }
            return embHas ? emb : null;
        }

        string ActiveCode()
        {
            var cur = Game.CurrentCulture();
            if (string.IsNullOrEmpty(cur) || cur.StartsWith("en", StringComparison.OrdinalIgnoreCase)) return "en";
            foreach (var l in embedded.languages) if (string.Equals(l.culture, cur, StringComparison.OrdinalIgnoreCase)) return l.code;
            return null;
        }

        string Ver(LangInfo i) { return T.F("vb", i.version, i.build); }

        void Apply()
        {
            Text = T.G("wtitle");
            lblTitle.Text = T.G("title");
            lblChoose.Text = T.G("choose");
            lblFan.Text = T.G("fan");
            btnCheck.Text = T.G("btn_check");
            links.Text = T.G("link_by") + "   \u00b7   GitHub   \u00b7   " + T.G("link_support");
            links.Links.Clear();
            links.Links.Add(links.Text.IndexOf("GitHub"), 6, Config.REPO_URL);
            links.Links.Add(links.Text.IndexOf(T.G("link_support")), T.G("link_support").Length, Config.KOFI_URL);
            links.LinkClicked -= LinkClicked; links.LinkClicked += LinkClicked;
            linkHelp.Text = T.G("link_help"); linkHelp.Links.Clear(); linkHelp.Links.Add(0, linkHelp.Text.Length);

            var g = Game.Find();
            if (g == null) { gameRoot = null; gameBuild = null; lblGame.Text = T.G("game_missing"); }
            else { gameRoot = g[0]; gameBuild = g[1]; lblGame.Text = T.F("game_found", gameRoot, gameBuild, Game.CurrentCulture() ?? "?"); }

            string active = gameRoot == null ? null : ActiveCode();
            foreach (var t in tiles)
            {
                t.Selected = t.Code == sel; t.NameText = T.Name(t.Code);
                var e = Entry(t.Code); bool dummy;
                var best = Best(t.Code, out dummy);
                if (t.Code == "en") { t.Badge = active == "en" ? "\u25cf " + T.G("tile_active") : T.G("tile_original"); t.BadgeColor = active == "en" ? Color.FromArgb(120, 210, 130) : Dim; }
                else if (active == t.Code) { t.Badge = "\u25cf " + T.G("tile_active"); t.BadgeColor = Color.FromArgb(120, 210, 130); }
                else if (gameRoot != null && e != null && Game.InstalledVersion(gameRoot, e.culture) != null) { t.Badge = "\u2713 " + T.G("tile_installed"); t.BadgeColor = Color.FromArgb(150, 200, 160); }
                else if (best != null) { t.Badge = T.G("tile_available"); t.BadgeColor = Gold; }
                else { t.Badge = T.G("tile_soon"); t.BadgeColor = Dim; }
                t.Invalidate();
            }

            // Detalle del idioma elegido
            var cur = Entry(sel);
            bool isRem; var b = Best(sel, out isRem);
            linkHelp.Visible = false; btnDisable.Visible = true; lblAvailable.ForeColor = Fg; lblInstalled.Visible = true;
            btnDisable.Text = T.G("btn_english");
            btnInstall.BackColor = Color.FromArgb(214, 170, 80); btnInstall.ForeColor = Color.FromArgb(30, 26, 14); btnInstall.FlatAppearance.BorderColor = Color.FromArgb(240, 208, 130);
            if (sel == "en")
            {
                lblInstalled.Visible = false; lblAvailable.Text = T.G("original_title"); lblStatus.Text = T.G("original_help");
                btnInstall.Text = T.G("btn_english"); btnInstall.Enabled = gameRoot != null; btnDisable.Visible = false;
            }
            else if (b == null)
            {
                lblInstalled.Visible = false; lblAvailable.Text = T.G("coming_title"); lblAvailable.ForeColor = Gold;
                lblStatus.Text = T.G("coming_help"); linkHelp.Visible = true;
                btnInstall.Text = T.G("btn_install"); btnInstall.Enabled = true; btnDisable.Enabled = gameRoot != null;
                btnInstall.BackColor = Color.FromArgb(58, 56, 50); btnInstall.ForeColor = Color.FromArgb(150, 146, 130); btnInstall.FlatAppearance.BorderColor = Color.FromArgb(90, 86, 72);   // apagado: al pulsarlo avisa de que aun no hay traduccion
            }
            else
            {
                var inst = gameRoot == null || cur == null ? null : Game.InstalledVersion(gameRoot, cur.culture);
                lblInstalled.Text = T.F("installed", inst == null ? T.G("none") : Ver(inst));
                var cov = b.status == "partial" && b.coverage > 0 ? "  \u00b7  " + T.F("coverage", b.coverage) : "";
                lblAvailable.Text = T.F("available", Ver(b)) + (isRem ? T.G("downloadable") : T.G("included")) + cov;
                if (statusMsg == null) lblStatus.Text = "";
                btnInstall.Text = T.G("btn_install"); btnInstall.Enabled = gameRoot != null; btnDisable.Enabled = gameRoot != null;
            }
            if (statusMsg != null) lblStatus.Text = statusMsg;
        }

        string statusMsg;
        void LinkClicked(object s, LinkLabelLinkClickedEventArgs e) { try { Process.Start((string)e.Link.LinkData); } catch { } }
        void SetStatus(string s) { statusMsg = s; lblStatus.Text = s ?? ""; }

        void CheckRemote(bool verbose)
        {
            SetStatus(T.G("checking"));
            var t = new Thread(delegate ()
            {
                string msgKey = null; string msg = null;
                try
                {
                    var data = Util.Download(Config.CATALOG_URL);
                    var c = new JavaScriptSerializer().Deserialize<Catalog>(Encoding.UTF8.GetString(data));
                    remote = c;
                    var myVer = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
                    if (!string.IsNullOrEmpty(c.installerVersion) && Util.CompareVersions(c.installerVersion, myVer) > 0) msgKey = "new_installer";
                    BeginInvoke((MethodInvoker)delegate
                    {
                        bool dummy; var b = Best(sel, out dummy);
                        var parts = new List<string>();
                        if (b != null && sel != "en") parts.Add(T.F("latest", Ver(b)));
                        if (msgKey != null) parts.Add(T.F("new_installer", c.installerVersion, Config.RELEASES_URL));
                        SetStatus(parts.Count == 0 ? null : string.Join("\n", parts.ToArray()));
                        Apply();
                    });
                }
                catch (Exception ex)
                {
                    msg = verbose ? T.F("no_conn_v", ex.Message) : T.G("no_conn");
                    BeginInvoke((MethodInvoker)delegate { SetStatus(msg); Apply(); });
                }
            });
            t.IsBackground = true; t.Start();
        }

        // Aviso previo: cerrar el juego + donativo. Botones: Donar, "Juego cerrado! Instalar", Cancelar.
        bool ConfirmarInstalacion()
        {
            using (var f = new Form())
            {
                f.Text = Text; f.FormBorderStyle = FormBorderStyle.FixedDialog; f.MaximizeBox = false; f.MinimizeBox = false;
                f.StartPosition = FormStartPosition.CenterParent; f.ClientSize = new Size(520, 230); f.Font = Font; f.ShowInTaskbar = false;
                f.BackColor = Bg; f.ForeColor = Fg;
                var msg = new Label { Left = 20, Top = 18, Width = 480, Height = 110, Text = T.G("confirm") };
                var bDonar = MakeButton(20, 160, 130, false); bDonar.Text = T.G("btn_donate");
                var bOk = MakeButton(160, 160, 220, true); bOk.Text = T.G("btn_go"); bOk.DialogResult = DialogResult.OK;
                var bCancel = MakeButton(390, 160, 110, false); bCancel.Text = T.G("btn_cancel"); bCancel.DialogResult = DialogResult.Cancel;
                bDonar.Click += delegate { try { Process.Start(Config.KOFI_URL); } catch { } };
                f.Controls.AddRange(new Control[] { msg, bDonar, bOk, bCancel });
                f.AcceptButton = bOk; f.CancelButton = bCancel;
                return f.ShowDialog(this) == DialogResult.OK;
            }
        }

        void DoInstall()
        {
            if (sel == "en") { DoEnglish(); return; }
            try
            {
                bool isRem; var m = Best(sel, out isRem);
                if (m == null) { MessageBox.Show(T.G("no_translation"), Text); return; }
                if (!ConfirmarInstalacion()) return;
                if (Game.Running()) { MessageBox.Show(T.G("running"), Text); return; }
                if (!string.IsNullOrEmpty(gameBuild) && !string.IsNullOrEmpty(m.build) && m.build != gameBuild)
                {
                    var r = MessageBox.Show(T.F("build_mismatch", m.version, m.build, gameBuild), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (r != DialogResult.Yes) return;
                }
                byte[] locres;
                if (isRem)
                {
                    SetStatus(T.F("downloading", m.version)); Application.DoEvents();
                    var url = m.locres;
                    if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) url = new Uri(new Uri(Config.CATALOG_URL), url).ToString();
                    byte[] have; if (!remoteLocres.TryGetValue(sel + m.version, out have)) { have = Util.Download(url); remoteLocres[sel + m.version] = have; }
                    locres = have;
                }
                else locres = Util.ReadResource("String." + sel + ".locres");
                Game.Install(gameRoot, locres, m);
                Game.SetCulture(m.culture);
                SetStatus(T.F("installed_ok", m.version, T.Name(sel)));
                Apply();
                MessageBox.Show(T.F("done_msg", m.culture), Text);
            }
            catch (UnauthorizedAccessException) { MessageBox.Show(T.G("no_perms"), Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
            catch (Exception ex) { SetStatus(ex.Message); MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        void DoEnglish()
        {
            try
            {
                if (Game.Running()) { MessageBox.Show(T.G("running_switch"), Text); return; }
                Game.SetCulture("en");
                SetStatus(T.G("english_on"));
                Apply();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // Para pruebas: guarda una captura de la ventana en un PNG.
        public void Snapshot(string path)
        {
            Show(); Application.DoEvents(); Thread.Sleep(150); Application.DoEvents();
            using (var bmp = new Bitmap(Width, Height))
            {
                DrawToBitmap(bmp, new Rectangle(0, 0, Width, Height));
                bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
            Close();
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
                var g = Game.Find(); var cat = Util.EmbeddedCatalog();
                Console.WriteLine("juego=" + (g == null ? "-" : g[0]) + " build=" + (g == null ? "-" : g[1]));
                Console.WriteLine("cultura=" + Game.CurrentCulture());
                foreach (var l in cat.languages)
                {
                    var loc = Util.ReadResource("String." + l.code + ".locres");
                    var ok = loc != null && Util.Sha256(loc) == (l.sha256 ?? "");
                    Console.WriteLine(l.code + " " + l.culture + " " + l.status + " v=" + l.version + " build=" + l.build + " incluido=" + (loc != null) + " sha_ok=" + ok +
                        " instalada=" + (g == null ? "-" : (Game.InstalledVersion(g[0], l.culture) == null ? "ninguna" : Game.InstalledVersion(g[0], l.culture).version)));
                }
                return 0;
            }
            // Autocomprobacion sin tocar el juego: instala cada traduccion incluida en una carpeta temporal y comprueba hash y archivos.
            if (args.Length > 1 && args[0] == "/selftest")
            {
                var cat = Util.EmbeddedCatalog(); int fails = 0; var log = new StringBuilder();
                foreach (var l in cat.languages)
                {
                    var loc = Util.ReadResource("String." + l.code + ".locres");
                    if (loc == null) { log.AppendLine(l.code + ": sin traduccion incluida (" + l.status + ")"); continue; }
                    try
                    {
                        Game.Install(args[1], loc, l);
                        var back = Game.InstalledVersion(args[1], l.culture);
                        var dest = Path.Combine(args[1], Config.LocresRel(l.culture));
                        bool ok = back != null && back.version == l.version && File.Exists(dest) && Util.Sha256(File.ReadAllBytes(dest)) == l.sha256;
                        log.AppendLine(l.code + " " + l.culture + ": " + (ok ? "OK" : "FALLO") + " v" + l.version + " " + dest); if (!ok) fails++;
                    }
                    catch (Exception ex) { log.AppendLine(l.code + ": EXCEPCION " + ex.Message); fails++; }
                }
                log.AppendLine(fails == 0 ? "RESULTADO: OK" : "RESULTADO: FALLOS " + fails);
                File.WriteAllText(Path.Combine(args[1], "selftest.txt"), log.ToString());
                return fails;
            }
            // Capturas de la ventana en cada idioma (pruebas): /shots <carpeta>
            if (args.Length > 1 && args[0] == "/shots")
            {
                Directory.CreateDirectory(args[1]);
                Application.EnableVisualStyles();
                foreach (var code in T.Order)
                {
                    var f = new MainForm(code, true); f.StartPosition = FormStartPosition.Manual; f.Location = new Point(40, 40);
                    f.Snapshot(Path.Combine(args[1], "ventana_" + code + ".png"));
                }
                return 0;
            }
            string lang = null;
            foreach (var a in args) if (a.StartsWith("/lang=")) lang = a.Substring(6);
            Application.EnableVisualStyles();
            Application.Run(new MainForm(lang, false));
            return 0;
        }
    }
}
