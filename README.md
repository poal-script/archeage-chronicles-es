<p align="center"><img src="icono.png" width="160" alt="poal-script"></p>

# ArcheAge Chronicles — Fan translations / Traducciones / Traductions

**🇪🇸 [Español](#español) · 🇫🇷 [Français](#français) · 🇩🇪 [Deutsch](#deutsch) · 🇮🇹 [Italiano](#italiano) · 🇵🇱 [Polski](#polski) · 🇬🇧 [English](#english)**

**[⬇ Download / Descargar / Télécharger](https://github.com/poal-script/archeage-chronicles-es/releases/latest)** → `AAC-Traducciones.exe`

One installer, one language selector with flags. Open it, pick your language and the whole installer switches to it.

| Language | Status |
|---|---|
| 🇪🇸 Español | ✅ Available (54,000+ texts) |
| 🇫🇷 Français | ✅ Available (first version, 80% of translatable texts) |
| 🇩🇪 Deutsch | 🛠 In preparation |
| 🇮🇹 Italiano | 🛠 In preparation |
| 🇵🇱 Polski | 🛠 In preparation |

Want to help with French, German, Italian or Polish? Open an [issue](https://github.com/poal-script/archeage-chronicles-es/issues) and tell us which language.

---

## English

Unofficial fan translations of **ArcheAge Chronicles**: interface, quests, dialogues, items, skills and descriptions. Voices are unchanged.

1. Download `AAC-Traducciones.exe` from the [latest release](https://github.com/poal-script/archeage-chronicles-es/releases/latest).
2. Close the game and its launcher.
3. Run the installer (it asks for administrator rights because it writes to the Steam game folder), **choose your language** and press **Install / Update**.
4. Start the game from Steam. If it is still in English: Steam → ArcheAge Chronicles → Properties → Launch options → `-culture=es-ES` (use `fr-FR`, `de-DE`, `it-IT` or `pl-PL` for the other languages).

To go back to English press **Back to English**. Translation files stay installed so you can switch again at any time.

## Español

Traducción de fans de **ArcheAge Chronicles**: interfaz, misiones, diálogos, objetos, habilidades y descripciones. Las voces no cambian.

1. Descarga `AAC-Traducciones.exe` desde la [última release](https://github.com/poal-script/archeage-chronicles-es/releases/latest).
2. Cierra el juego y su lanzador.
3. Ejecuta el instalador (pide permisos de administrador porque escribe en la carpeta del juego de Steam), **elige tu idioma** y pulsa **Instalar / Actualizar**.
4. Abre el juego desde Steam. Si siguiera en inglés: Steam → ArcheAge Chronicles → Propiedades → Opciones de lanzamiento → `-culture=es-ES`.

Para volver al inglés pulsa **Volver al inglés**. Cuando el juego reciba un parche, abre el instalador y pulsa **Buscar actualización**.

## Français

Traductions de fans d'**ArcheAge Chronicles** : interface, quêtes, dialogues, objets, compétences et descriptions. Les voix ne changent pas.

1. Téléchargez `AAC-Traducciones.exe` depuis la [dernière release](https://github.com/poal-script/archeage-chronicles-es/releases/latest).
2. Fermez le jeu et son lanceur.
3. Lancez l'installateur (il demande les droits d'administrateur car il écrit dans le dossier Steam du jeu), **choisissez votre langue** et cliquez sur **Installer / Mettre à jour**.
4. Lancez le jeu depuis Steam. S'il reste en anglais : Steam → ArcheAge Chronicles → Propriétés → Options de lancement → `-culture=fr-FR`.

La traduction française est **disponible** (première version : si tu vois un souci, ouvre un « issue » avec une capture). Pour revenir à l'anglais, cliquez sur **Revenir à l'anglais**.

## Deutsch

Inoffizielle Fan-Übersetzungen für **ArcheAge Chronicles**: Oberfläche, Quests, Dialoge, Gegenstände, Fähigkeiten und Beschreibungen. Die Sprachausgabe bleibt unverändert.

1. Lade `AAC-Traducciones.exe` aus dem [neuesten Release](https://github.com/poal-script/archeage-chronicles-es/releases/latest) herunter.
2. Schließe das Spiel und seinen Launcher.
3. Starte das Installationsprogramm (es braucht Administratorrechte, weil es in den Steam-Spielordner schreibt), **wähle deine Sprache** und klicke auf **Installieren / Aktualisieren**.
4. Starte das Spiel über Steam. Falls es auf Englisch bleibt: Steam → ArcheAge Chronicles → Eigenschaften → Startoptionen → `-culture=de-DE`.

Die deutsche Übersetzung ist **in Vorbereitung** („Demnächst“ im Installationsprogramm). Mit **Zurück zu Englisch** wechselst du zurück.

## Italiano

Traduzioni non ufficiali di **ArcheAge Chronicles** realizzate dai fan: interfaccia, missioni, dialoghi, oggetti, abilità e descrizioni. Le voci non cambiano.

1. Scarica `AAC-Traducciones.exe` dall'[ultima release](https://github.com/poal-script/archeage-chronicles-es/releases/latest).
2. Chiudi il gioco e il suo launcher.
3. Avvia il programma di installazione (richiede i permessi di amministratore perché scrive nella cartella Steam del gioco), **scegli la tua lingua** e premi **Installa / Aggiorna**.
4. Avvia il gioco da Steam. Se resta in inglese: Steam → ArcheAge Chronicles → Proprietà → Opzioni di avvio → `-culture=it-IT`.

La traduzione italiana è **in preparazione** (nel programma compare «In arrivo»). Per tornare all'inglese premi **Torna all'inglese**.

## Polski

Nieoficjalne tłumaczenia fanów **ArcheAge Chronicles**: interfejs, zadania, dialogi, przedmioty, umiejętności i opisy. Głosy się nie zmieniają.

1. Pobierz `AAC-Traducciones.exe` z [najnowszego wydania](https://github.com/poal-script/archeage-chronicles-es/releases/latest).
2. Zamknij grę i jej launcher.
3. Uruchom instalator (wymaga praw administratora, bo zapisuje w folderze gry Steam), **wybierz swój język** i naciśnij **Zainstaluj / Zaktualizuj**.
4. Uruchom grę ze Steam. Jeśli nadal jest po angielsku: Steam → ArcheAge Chronicles → Właściwości → Opcje uruchamiania → `-culture=pl-PL`.

Polskie tłumaczenie jest **w przygotowaniu** (w instalatorze widnieje „Wkrótce”). Aby wrócić do angielskiego, naciśnij **Wróć do angielskiego**.

---

## What the installer does / Qué hace / Ce que fait l'installateur

- Copies `String.locres` to `X5\Content\Localization\String\<culture>\` inside the game folder (`es-ES`, `fr-FR`, `de-DE`, `it-IT`, `pl-PL`).
- Sets `Culture=<culture>` in `%LOCALAPPDATA%\X5\Saved\Config\Windows\GameUserSettings.ini` (keeps a copy first: `GameUserSettings.ini.antes-de-traduccion`).
- **Does not modify** the `.pak` files or the game executable.
- Checks the integrity (SHA-256) of everything it installs or downloads.
- Checks this repository for newer translations (`languages.json` in the latest release).

Source: [`Instalador.cs`](Instalador.cs).

## Translation notes / Notas

- **Traits** and **Raid** stay in English on purpose (all languages).
- Found an error? Open an [issue](https://github.com/poal-script/archeage-chronicles-es/issues) with a screenshot and where it appears.

## Support / Apoya / Soutenez / Unterstütze / Sostieni / Wesprzyj

Free, and always will be. If it helps you and you want to keep it up to date with every patch:

**[☕ Ko-fi: poalscript](https://ko-fi.com/poalscript)**

## Disclaimer

Fan project, unofficial. Not affiliated with XLGAMES or Kakao Games. ArcheAge Chronicles and its contents belong to their respective owners. Use at your own risk.

Contact: poal.script@proton.me
