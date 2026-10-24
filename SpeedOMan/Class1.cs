using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpeedDisplayPlugin
{
    public enum SpeedUnit
    {
        MetersPerSecond,
        KilometersPerHour
    }

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.giacomo.valheim.SpeedOMan";
        public const string PluginName = "Speed Display";
        public const string PluginVersion = "0.0.3";

        internal static ManualLogSource Log;

        // ---- Parametri della gestione errori ----
        // Errori consecutivi tollerati in una fase prima di sospendere il plugin
        private const int MaxConsecutiveFailures = 5;
        // Secondi minimi tra due log identici (anti-spam: Update/OnGUI girano ogni frame)
        private const float LogCooldownSeconds = 10f;
        // Intervallo di rigenerazione del testo mostrato
        private const float TextRefreshSeconds = 0.1f;

        // Generale
        private ConfigEntry<bool> modEnabled;
        private ConfigEntry<KeyboardShortcut> toggleKey;
        private ConfigEntry<SpeedUnit> unit;
        private ConfigEntry<bool> horizontalOnly;
        private ConfigEntry<float> smoothingTime;
        private ConfigEntry<bool> showOnlyWhenMoving;
        private ConfigEntry<bool> showMovementLabel;

        // Aspetto
        private ConfigEntry<int> fontSize;
        private ConfigEntry<Color> textColor;
        private ConfigEntry<float> posX;
        private ConfigEntry<float> posY;

        // Stato
        private float smoothedSpeed;
        private GUIStyle style;
        private GUIStyle shadowStyle;
        private string cachedText = string.Empty;
        private float lastTextRefresh = float.NegativeInfinity;

        // Stato della gestione errori
        private bool initialized;   // true solo se Awake è terminato senza eccezioni
        private bool faulted;       // true se il circuit breaker ha sospeso il plugin
        private int updateFailures;
        private int guiFailures;
        private readonly Dictionary<string, float> lastLogTimes = new Dictionary<string, float>();

        // ------------------------------------------------------------------
        // Inizializzazione
        // ------------------------------------------------------------------
        private void Awake()
        {
            Log = Logger;

            try
            {
                modEnabled = Config.Bind("Generale", "ModEnabled", true,
                    "Mostra o nasconde l'indicatore di velocità");
                toggleKey = Config.Bind("Generale", "ToggleKey", new KeyboardShortcut(KeyCode.F7),
                    "Tasto per attivare/disattivare l'indicatore (se il plugin è stato sospeso per errori, lo riattiva)");
                unit = Config.Bind("Generale", "Unit", SpeedUnit.MetersPerSecond,
                    "Unità di misura (metri al secondo o chilometri orari)");
                horizontalOnly = Config.Bind("Generale", "HorizontalOnly", true,
                    "Se attivo ignora la componente verticale (salti, cadute, salite ripide)");
                smoothingTime = Config.Bind("Generale", "SmoothingTime", 0.2f,
                    new ConfigDescription("Secondi di media mobile (0 = valore istantaneo)",
                        new AcceptableValueRange<float>(0f, 2f)));
                showOnlyWhenMoving = Config.Bind("Generale", "ShowOnlyWhenMoving", false,
                    "Nasconde l'indicatore quando il personaggio è fermo");
                showMovementLabel = Config.Bind("Generale", "ShowMovementLabel", true,
                    "Aggiunge la modalità di movimento (Corsa, Passo lento, Furtivo, Nuoto, Normale)");

                fontSize = Config.Bind("Aspetto", "FontSize", 22,
                    new ConfigDescription("Dimensione del testo",
                        new AcceptableValueRange<int>(8, 72)));
                textColor = Config.Bind("Aspetto", "TextColor", Color.white,
                    "Colore del testo (il canale alfa è rispettato)");
                posX = Config.Bind("Aspetto", "PositionX", 0.5f,
                    new ConfigDescription("Posizione orizzontale (0 = sinistra, 1 = destra)",
                        new AcceptableValueRange<float>(0f, 1f)));
                posY = Config.Bind("Aspetto", "PositionY", 0.92f,
                    new ConfigDescription("Posizione verticale (0 = alto, 1 = basso)",
                        new AcceptableValueRange<float>(0f, 1f)));

                initialized = true;
                Log.LogInfo($"{PluginName} {PluginVersion} caricato con successo!");
            }
            catch (Exception ex)
            {
                // Con initialized = false, Update e OnGUI non toccano mai le ConfigEntry null.
                Log.LogError($"[Awake] Inizializzazione fallita, il plugin resta inattivo: {ex}");
            }
        }

        // ------------------------------------------------------------------
        // Update: tasto di toggle + calcolo della velocità
        // ------------------------------------------------------------------
        private void Update()
        {
            if (!initialized) return;

            HandleToggle();
            if (faulted) return;

            try
            {
                UpdateSpeed();
                updateFailures = 0;
            }
            catch (Exception ex)
            {
                smoothedSpeed = 0f;
                updateFailures++;
                ReportError("Update", ex);
                CheckCircuitBreaker("Update", updateFailures);
            }
        }

        private void HandleToggle()
        {
            try
            {
                if (!toggleKey.Value.IsDown()) return;

                if (faulted)
                {
                    // Il tasto funge da "reset" dopo una sospensione per errori
                    faulted = false;
                    updateFailures = 0;
                    guiFailures = 0;
                    style = null;
                    shadowStyle = null;
                    Log.LogInfo("[Toggle] Plugin riattivato manualmente dopo la sospensione per errori.");
                    return;
                }

                modEnabled.Value = !modEnabled.Value;
            }
            catch (Exception ex)
            {
                ReportError("Toggle", ex);
            }
        }

        private void UpdateSpeed()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                smoothedSpeed = 0f;
                return;
            }

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // Character.GetVelocity(): per il proprietario restituisce la velocità del Rigidbody
            Vector3 velocity = player.GetVelocity();
            if (horizontalOnly.Value) velocity.y = 0f;
            float raw = velocity.magnitude;

            // Un NaN/Infinity si propagherebbe nella media mobile in modo permanente
            if (float.IsNaN(raw) || float.IsInfinity(raw))
            {
                smoothedSpeed = 0f;
                LogThrottled(LogLevel.Warning, "InvalidVelocity",
                    $"[Update] Velocità non valida ({raw}): campione scartato, media azzerata.");
                return;
            }

            float tau = smoothingTime.Value;
            if (tau <= 0.001f)
            {
                smoothedSpeed = raw;
            }
            else
            {
                float alpha = 1f - Mathf.Exp(-dt / tau);
                smoothedSpeed = Mathf.Lerp(smoothedSpeed, raw, alpha);
            }
        }

        // ------------------------------------------------------------------
        // OnGUI: disegno
        // ------------------------------------------------------------------
        private void OnGUI()
        {
            // OnGUI viene chiamato più volte per frame: si disegna solo nel Repaint
            Event e = Event.current;
            if (e == null || e.type != EventType.Repaint) return;
            if (!initialized || faulted || !modEnabled.Value) return;

            try
            {
                DrawSpeed();
                guiFailures = 0;
            }
            catch (Exception ex)
            {
                // Gli stili vengono ricreati al prossimo tentativo (possono essere la causa)
                style = null;
                shadowStyle = null;
                guiFailures++;
                ReportError("OnGUI", ex);
                CheckCircuitBreaker("OnGUI", guiFailures);
            }
        }

        private void DrawSpeed()
        {
            Player player = Player.m_localPlayer;
            if (player == null) return;
            if (IsUiBlocking()) return;

            bool moving = smoothedSpeed >= 0.1f;
            if (showOnlyWhenMoving.Value && !moving) return;

            float now = Time.unscaledTime;
            if (now - lastTextRefresh >= TextRefreshSeconds)
            {
                lastTextRefresh = now;
                cachedText = BuildText(player, moving);
            }

            EnsureStyles();
            style.fontSize = fontSize.Value;
            shadowStyle.fontSize = fontSize.Value;

            Color c = textColor.Value;
            style.normal.textColor = c;
            shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.85f * c.a);

            float w = 500f, h = fontSize.Value * 2f;
            float x = Screen.width * posX.Value - w / 2f;
            float y = Screen.height * posY.Value - h / 2f;

            // Ombra per la leggibilità su qualunque sfondo
            GUI.Label(new Rect(x + 1.5f, y + 1.5f, w, h), cachedText, shadowStyle);
            GUI.Label(new Rect(x, y, w, h), cachedText, style);
        }

        private string BuildText(Player player, bool moving)
        {
            float value = unit.Value == SpeedUnit.KilometersPerHour
                ? smoothedSpeed * 3.6f
                : smoothedSpeed;
            string suffix = unit.Value == SpeedUnit.KilometersPerHour ? "km/h" : "m/s";
            string text = $"{value:0.0} {suffix}";

            if (showMovementLabel.Value && moving)
                text += "  (" + SafeMovementLabel(player) + ")";

            return text;
        }

        // ------------------------------------------------------------------
        // Helper isolati: un loro fallimento non deve oscurare l'intero indicatore
        // ------------------------------------------------------------------
        private bool IsUiBlocking()
        {
            try
            {
                // "global::" evita l'ambiguità con System.Console
                return Hud.IsUserHidden()
                    || InventoryGui.IsVisible()
                    || Menu.IsVisible()
                    || global::Console.IsVisible();
            }
            catch (Exception ex)
            {
                // In caso di dubbio l'indicatore resta visibile
                ReportError("UiVisibility", ex);
                return false;
            }
        }

        // Nel gioco esistono tre andature a terra: corsa, andatura normale (trotto)
        // e "passo lento" (tasto walk). IsWalking() è vero solo per il passo lento.
        private string SafeMovementLabel(Player player)
        {
            try
            {
                return GetMovementLabel(player);
            }
            catch (Exception ex)
            {
                ReportError("MovementLabel", ex);
                return "?";
            }
        }

        private static string GetMovementLabel(Player player)
        {
            if (player.IsSwimming()) return "Nuoto";
            if (player.IsRunning()) return "Corsa";
            if (player.IsSneaking()) return "Furtivo";
            if (player.IsWalking()) return "Passo lento";
            return "Normale";
        }

        private void EnsureStyles()
        {
            if (style != null && shadowStyle != null) return;

            style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                wordWrap = false
            };
            shadowStyle = new GUIStyle(style);
            shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
        }

        // ------------------------------------------------------------------
        // Infrastruttura di logging e protezione
        // ------------------------------------------------------------------

        /// <summary>
        /// Registra un'eccezione sulla console BepInEx. Il messaggio sintetico va a livello
        /// Error; lo stack trace completo a livello Debug (visibile se in BepInEx.cfg,
        /// [Logging.Console], LogLevels include Debug).
        /// </summary>
        private void ReportError(string context, Exception ex)
        {
            string key = context + "|" + ex.GetType().Name + "|" + ex.Message;
            if (!ShouldLog(key)) return;

            Log.LogError($"[{context}] {ex.GetType().Name}: {ex.Message}");
            Log.LogDebug($"[{context}] Stack trace completo:\n{ex}");
        }

        private void LogThrottled(LogLevel level, string key, string message)
        {
            if (!ShouldLog(key)) return;
            Log.Log(level, message);
        }

        private bool ShouldLog(string key)
        {
            float now = Time.realtimeSinceStartup;

            if (lastLogTimes.TryGetValue(key, out float last) && now - last < LogCooldownSeconds)
                return false;

            // Limite alla crescita del dizionario in caso di messaggi sempre diversi
            if (lastLogTimes.Count > 64) lastLogTimes.Clear();

            lastLogTimes[key] = now;
            return true;
        }

        /// <summary>
        /// Dopo N fallimenti consecutivi sospende il plugin, per non produrre eccezioni
        /// a ogni frame. Il tasto di toggle lo riattiva.
        /// </summary>
        private void CheckCircuitBreaker(string phase, int failures)
        {
            if (failures < MaxConsecutiveFailures) return;

            faulted = true;
            Log.LogError($"[{phase}] {failures} errori consecutivi: {PluginName} sospeso. " +
                         $"Premi {toggleKey.Value} per riattivarlo (verifica il log per la causa).");
        }
    }
}