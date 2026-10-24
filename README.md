# SpeedOMan

Plugin [BepInEx](https://github.com/BepInEx/BepInEx) per **Valheim** che mostra a schermo la velocità del tuo personaggio, in tempo reale, in metri al secondo o in chilometri orari.

> Il plugin è solo lato client: non applica patch Harmony e non modifica il comportamento del gioco, quindi non dovrebbe richiedere l'installazione sul server.

## Funzionalità

- Velocità visualizzata in **m/s** o **km/h**
- Media mobile esponenziale indipendente dal framerate, con tempo di smoothing configurabile
- Opzione per ignorare la componente verticale (salti, cadute, salite ripide)
- Etichetta della modalità di movimento: *Corsa*, *Passo lento*, *Furtivo*, *Nuoto*, *Normale*
- Tasto rapido per attivare/disattivare l'indicatore (predefinito: `F7`)
- Testo con ombra, per restare leggibile su qualsiasi sfondo
- Indicatore nascosto automaticamente quando l'HUD è nascosto o sono aperti inventario, menu o console
- Dimensione, colore (con trasparenza) e posizione configurabili
- Gestione degli errori con log sulla console BepInEx (vedi [Diagnostica](#diagnostica))

## Requisiti

- Valheim
- [BepInEx per Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) (BepInExPack_Valheim)

## Installazione

### Manuale

1. Installa BepInEx per Valheim.
2. Scarica l'ultima versione dalla pagina [Releases](../../releases).
3. Copia `SpeedDisplayPlugin.dll` nella cartella `BepInEx/plugins/` di Valheim.
4. Avvia il gioco.

### Gestore di mod

Non ancora disponibile su Thunderstore.

## Utilizzo

Una volta in gioco, la velocità compare in basso al centro dello schermo. Premi `F7` (configurabile) per nasconderla o mostrarla.

Al primo avvio viene generato il file di configurazione:

```
BepInEx/config/com.giacomo.valheim.SpeedOMan.cfg
```

Modificalo con un editor di testo a gioco chiuso, oppure usa [BepInEx Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager) se lo hai installato.

## Configurazione

### Sezione `Generale`

| Voce | Predefinito | Descrizione |
|---|---|---|
| `ModEnabled` | `true` | Mostra o nasconde l'indicatore |
| `ToggleKey` | `F7` | Tasto per attivare/disattivare l'indicatore; dopo una sospensione per errori lo riattiva |
| `Unit` | `MetersPerSecond` | Unità di misura: `MetersPerSecond` o `KilometersPerHour` |
| `HorizontalOnly` | `true` | Ignora la componente verticale della velocità |
| `SmoothingTime` | `0.2` | Secondi di media mobile, da 0 a 2 (0 = valore istantaneo) |
| `ShowOnlyWhenMoving` | `false` | Nasconde l'indicatore quando il personaggio è fermo |
| `ShowMovementLabel` | `true` | Aggiunge la modalità di movimento accanto al valore |

### Sezione `Aspetto`

| Voce | Predefinito | Descrizione |
|---|---|---|
| `FontSize` | `22` | Dimensione del testo, da 8 a 72 |
| `TextColor` | bianco | Colore del testo; il canale alfa è rispettato |
| `PositionX` | `0.5` | Posizione orizzontale (0 = sinistra, 1 = destra) |
| `PositionY` | `0.92` | Posizione verticale (0 = alto, 1 = basso) |

## Diagnostica

Il plugin intercetta le proprie eccezioni e le scrive sulla console BepInEx, senza interrompere il gioco.

- **Errori** (`LogError`): messaggio sintetico con contesto, ad esempio `[Update] NullReferenceException: ...`. Messaggi identici vengono limitati a uno ogni 10 secondi.
- **Stack trace completo** (`LogDebug`): visibile solo se in `BepInEx/config/BepInEx.cfg`, sezione `[Logging.Console]`, il parametro `LogLevels` include `Debug` (ad esempio `All`).
- **Sospensione automatica**: dopo 5 errori consecutivi nella stessa fase (`Update` o `OnGUI`) il plugin si sospende e lo segnala nel log. Premi il tasto di toggle per riattivarlo.
- **Avvio fallito**: se la lettura della configurazione fallisce in `Awake`, il plugin resta inattivo e registra l'errore.

Se segnali un problema, allega le righe del log relative a `Speed Display` (`BepInEx/LogOutput.log`).

## Limiti noti

- Il comportamento a bordo di **navi** e **cavalcature** non è stato verificato. È possibile che venga mostrata la velocità del mezzo e che l'etichetta indichi "Normale"; una modalità "Navigazione" non è ancora implementata.
- La velocità è letta in `Update` e non in `FixedUpdate`, quindi con framerate alto lo stesso valore fisico può essere campionato più volte. Lo smoothing riduce l'effetto, ma non lo elimina del tutto.
- Le etichette di movimento sono scritte in italiano e non sono localizzabili.
- Le condizioni di visibilità (inventario, menu, console) dipendono da API interne di Valheim e potrebbero cambiare dopo un aggiornamento del gioco.

## Compilazione dal sorgente

1. Clona il repository.
2. Crea un progetto di libreria di classi e aggiungi `Plugin.cs`.
3. Aggiungi i riferimenti a queste librerie, prese dall'installazione di Valheim e da BepInEx:
   - `BepInEx/core/BepInEx.dll`
   - `valheim_Data/Managed/assembly_valheim.dll`
   - `valheim_Data/Managed/UnityEngine.dll` e `UnityEngine.CoreModule.dll`
   - `valheim_Data/Managed/UnityEngine.IMGUIModule.dll`
4. Imposta come framework di destinazione quello richiesto dalla tua versione di BepInEx (tipicamente .NET Framework 4.x; verifica nella documentazione di BepInEx).
5. Compila e copia la DLL in `BepInEx/plugins/`.

Se Valheim viene aggiornato e la compilazione fallisce su un metodo specifico, controlla i nomi nell'`assembly_valheim.dll` con un decompilatore come dnSpy o ILSpy.

## Cronologia delle versioni

### 0.0.3

- Gestione degli errori con log sulla console BepInEx, anti-spam e sospensione automatica
- Disegno solo durante l'evento `Repaint`, con testo memorizzato e aggiornato ogni 0,1 s
- Indicatore nascosto con inventario, menu o console aperti
- Il canale alfa di `TextColor` viene ora rispettato
- Rimosso un `using` inutilizzato

# Speed Display

🇮🇹 [Leggi in italiano](README.md)

A [BepInEx](https://github.com/BepInEx/BepInEx) plugin for **Valheim** that shows your character's speed on screen in real time, in meters per second or kilometers per hour.

> The plugin is client-side only: it applies no Harmony patches and does not change gameplay, so it should not need to be installed on the server.

## Features

- Speed displayed in **m/s** or **km/h**
- Framerate-independent exponential moving average with configurable smoothing time
- Option to ignore the vertical component (jumps, falls, steep slopes)
- Movement mode label: *Run*, *Slow walk*, *Sneak*, *Swim*, *Normal* (currently displayed in Italian, see [Known limitations](#known-limitations))
- Hotkey to toggle the display (default: `F7`)
- Text drop shadow for readability on any background
- Display hidden automatically when the HUD is hidden or when the inventory, menu or console is open
- Configurable size, color (with transparency) and position
- Error handling with output to the BepInEx console (see [Diagnostics](#diagnostics))

## Requirements

- Valheim
- [BepInEx for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) (BepInExPack_Valheim)

## Installation

### Manual

1. Install BepInEx for Valheim.
2. Download the latest version from the [Releases](../../releases) page.
3. Copy `SpeedDisplayPlugin.dll` into Valheim's `BepInEx/plugins/` folder.
4. Launch the game.

### Mod manager

Not yet available on Thunderstore.

## Usage

Once in game, the speed appears at the bottom center of the screen. Press `F7` (configurable) to hide or show it.

The configuration file is generated on first launch:

```
BepInEx/config/com.giacomo.valheim.SpeedOMan.cfg
```

Edit it with a text editor while the game is closed, or use [BepInEx Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager) if you have it installed.

## Configuration

### `Generale` section

The section names and descriptions in the config file are currently in Italian.

| Entry | Default | Description |
|---|---|---|
| `ModEnabled` | `true` | Shows or hides the display |
| `ToggleKey` | `F7` | Key to toggle the display; after a suspension caused by errors, it re-enables the plugin |
| `Unit` | `MetersPerSecond` | Unit of measurement: `MetersPerSecond` or `KilometersPerHour` |
| `HorizontalOnly` | `true` | Ignores the vertical component of the velocity |
| `SmoothingTime` | `0.2` | Seconds of moving average, from 0 to 2 (0 = instantaneous value) |
| `ShowOnlyWhenMoving` | `false` | Hides the display while the character is standing still |
| `ShowMovementLabel` | `true` | Adds the movement mode next to the value |

### `Aspetto` section

| Entry | Default | Description |
|---|---|---|
| `FontSize` | `22` | Text size, from 8 to 72 |
| `TextColor` | white | Text color; the alpha channel is respected |
| `PositionX` | `0.5` | Horizontal position (0 = left, 1 = right) |
| `PositionY` | `0.92` | Vertical position (0 = top, 1 = bottom) |

## Diagnostics

The plugin catches its own exceptions and writes them to the BepInEx console without interrupting the game.

- **Errors** (`LogError`): concise message with context, for example `[Update] NullReferenceException: ...`. Identical messages are limited to one every 10 seconds.
- **Full stack trace** (`LogDebug`): visible only if, in `BepInEx/config/BepInEx.cfg`, section `[Logging.Console]`, the `LogLevels` setting includes `Debug` (for example `All`).
- **Automatic suspension**: after 5 consecutive errors in the same phase (`Update` or `OnGUI`) the plugin suspends itself and reports it in the log. Press the toggle key to re-enable it.
- **Failed startup**: if reading the configuration fails in `Awake`, the plugin stays inactive and logs the error.

When reporting a problem, please attach the log lines related to `Speed Display` (`BepInEx/LogOutput.log`).

## Known limitations

- Behavior on **ships** and **mounts** has not been verified. The display may show the vehicle's speed and the label may read "Normal"; a dedicated "Sailing" mode is not implemented yet.
- Speed is read in `Update` rather than `FixedUpdate`, so at high framerates the same physics value may be sampled several times. Smoothing reduces the effect but does not remove it entirely.
- Movement labels are hardcoded in Italian and cannot be localized yet.
- The visibility conditions (inventory, menu, console) rely on Valheim's internal APIs and may break after a game update.

## Building from source

1. Clone the repository.
2. Create a class library project and add `Plugin.cs`.
3. Add references to the following libraries, taken from your Valheim installation and from BepInEx:
   - `BepInEx/core/BepInEx.dll`
   - `valheim_Data/Managed/assembly_valheim.dll`
   - `valheim_Data/Managed/UnityEngine.dll` and `UnityEngine.CoreModule.dll`
   - `valheim_Data/Managed/UnityEngine.IMGUIModule.dll`
4. Set the target framework required by your BepInEx version (typically .NET Framework 4.x; check the BepInEx documentation).
5. Build and copy the DLL into `BepInEx/plugins/`.

If Valheim is updated and the build fails on a specific method, check the names in `assembly_valheim.dll` with a decompiler such as dnSpy or ILSpy.

## Changelog

### 0.0.3

- Error handling with BepInEx console logging, log throttling and automatic suspension
- Drawing only during the `Repaint` event, with cached text refreshed every 0.1 s
- Display hidden while the inventory, menu or console is open
- The alpha channel of `TextColor` is now respected
- Removed an unused `using`



## Author

Giacomo
## Autore

Giacomo
