using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlanB
{
    /// <summary>Cached, soft-edged procedural audio with bounded voices and two ambience channels.</summary>
    public static class AudioFactory
    {
        private const int SampleRate = 22050;
        private const int PoolSize = 12;
        private const float MasterGain = .5f;
        private const string RootName = "PlanB Cached Audio (12 voices + ambience)";
        private static readonly Dictionary<string, AudioClip[]> Clips = new Dictionary<string, AudioClip[]>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, float> LastPlayed = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private static readonly string[] SoundNames =
        {
            "shoot", "reload", "dryfire", "bash", "hit", "hurt", "step_wood", "step_metal",
            "mutant_growl", "mutant_death", "spider_skitter", "spider_death", "laser",
            "boss_roar", "boss_burst", "boss_death", "pickup_ammo", "pickup_medkit",
            "pickup_fuse", "pickup_key", "pickup", "cabinet", "locked", "unlock", "door",
            "trap_arm", "trap_snap", "scientist", "generator", "elevator", "ui_hover",
            "ui_click", "alarm", "win", "loss"
        };
        private static readonly string[] AmbientNames =
        { "ambient_home", "ambient_home_power", "ambient_lab", "ambient_lab_power" };
        private static readonly Dictionary<string, string> Aliases = CreateAliases();
        private static readonly AudioSource[] Voices = new AudioSource[PoolSize];
        private static readonly float[] VoiceStarted = new float[PoolSize];
        private static readonly int[] VoicePriority = new int[PoolSize];
        private static readonly AudioSource[] Ambience = new AudioSource[2];
        private static readonly float[] AmbienceTarget = new float[2];
        // Sound variation must never advance UnityEngine.Random and change gameplay outcomes.
        private static System.Random variation = new System.Random(41897);
        private static AudioFactoryDriver driver;
        private static int activeAmbience = -1;
        private static string ambienceName;
        private static bool paused;
        private static bool warmed;

        public static int VoiceLimit { get { return PoolSize; } }
        public static int CachedClipCount { get; private set; }
        public static int PeakVoices { get; private set; }
        public static int PlayedSoundCount { get; private set; }
        public static int DroppedSoundCount { get; private set; }
        public static int ActiveVoices
        {
            get
            {
                int count = 0;
                for (int i = 0; i < PoolSize; i++) if (Voices[i] != null && Voices[i].isPlaying) count++;
                return count;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            // Explicit reset also supports Enter Play Mode with domain reload disabled.
            if (driver != null) driver.ClearSavedState();
            for (int i = 0; i < PoolSize; i++) if (Voices[i] != null) Voices[i].Stop();
            for (int i = 0; i < 2; i++) if (Ambience[i] != null) Ambience[i].Stop();
            foreach (AudioClip[] bank in Clips.Values)
                for (int i = 0; i < bank.Length; i++) if (bank[i] != null) UnityEngine.Object.Destroy(bank[i]);
            Clips.Clear();
            LastPlayed.Clear();
            Array.Clear(Voices, 0, Voices.Length);
            Array.Clear(Ambience, 0, Ambience.Length);
            Array.Clear(AmbienceTarget, 0, AmbienceTarget.Length);
            driver = null;
            activeAmbience = -1;
            ambienceName = null;
            paused = warmed = false;
            CachedClipCount = PeakVoices = PlayedSoundCount = DroppedSoundCount = 0;
            variation = new System.Random(41897);
        }

        /// <summary>Call on the title screen so the first fight never synthesizes clips.</summary>
        public static void Warmup()
        {
            EnsureSources();
            if (warmed) return;
            for (int i = 0; i < SoundNames.Length; i++) GetBank(SoundNames[i]);
            for (int i = 0; i < AmbientNames.Length; i++) GetBank(AmbientNames[i]);
            var snapshot = new AudioClip[CachedClipCount];
            int offset = 0;
            for (int i = 0; i < SoundNames.Length + AmbientNames.Length; i++)
            {
                string key = i < SoundNames.Length ? SoundNames[i] : AmbientNames[i - SoundNames.Length];
                AudioClip[] bank = Clips[key];
                Array.Copy(bank, 0, snapshot, offset, bank.Length);
                offset += bank.Length;
            }
            driver.SaveCachedClips(snapshot);
            warmed = true;
        }

        public static void Play(string name, float volume = 1f) { PlayInternal(name, volume, 0f); }

        /// <summary>Cheap 2D stereo and distance falloff; no moving audio objects or 3D spatializer.</summary>
        public static void PlayAt(string name, Vector2 position, float volume = 1f)
        {
            GameManager game = GameManager.Instance;
            if (game == null || game.Player == null) { Play(name, volume); return; }
            Vector2 offset = position - (Vector2)game.Player.transform.position;
            float distance = offset.magnitude;
            if (distance >= 20f) return;
            float attenuation = Mathf.Clamp01((20f - distance) / 7f) / (1f + .035f * offset.sqrMagnitude);
            PlayInternal(name, volume * attenuation, Mathf.Clamp(offset.x / 11f, -.75f, .75f));
        }

        public static void SetAmbience(int floor, bool powered)
        {
            EnsureSources();
            driver.SaveAmbience(floor, powered, true);
            string next = floor == 2 ? (powered ? "ambient_lab_power" : "ambient_lab")
                : (powered ? "ambient_home_power" : "ambient_home");
            if (next == ambienceName && activeAmbience >= 0) return;
            int target = activeAmbience == 0 ? 1 : 0;
            AmbienceTarget[1 - target] = 0f;
            AudioSource source = Ambience[target];
            source.Stop();
            source.clip = GetBank(next)[0];
            source.volume = 0f;
            source.Play();
            AmbienceTarget[target] = floor == 2 ? .16f : .14f;
            activeAmbience = target;
            ambienceName = next;
        }

        public static void StopAmbience()
        {
            if (driver != null) driver.SaveAmbience(1, false, false);
            AmbienceTarget[0] = AmbienceTarget[1] = 0f;
            activeAmbience = -1;
            ambienceName = null;
        }

        public static void SetPaused(bool value)
        {
            paused = value;
            if (driver != null) driver.SavePaused(value);
            if (!value) return;
            // Interrupt combat tails; menu sounds remain available while timeScale is zero.
            for (int i = 0; i < PoolSize; i++) if (Voices[i] != null) Voices[i].Stop();
        }

        internal static void Tick(float deltaTime)
        {
            for (int i = 0; i < 2; i++)
            {
                AudioSource source = Ambience[i];
                if (source == null) continue;
                float target = AmbienceTarget[i] * (paused ? .12f : 1f);
                source.volume = Mathf.MoveTowards(source.volume, target, deltaTime * .16f);
                if (target == 0f && source.volume <= .0001f && source.isPlaying) source.Stop();
            }
        }

        private static void EnsureSources()
        {
            if (driver != null) return;
            // Static references disappear on script reload, but DontDestroyOnLoad objects survive.
            // Rebind that root instead of adding another 14 sources and another update driver.
            GameObject root = GameObject.Find(RootName);
            if (root == null) root = new GameObject(RootName);
            AudioFactoryDriver existing = root.GetComponent<AudioFactoryDriver>();
            if (existing == null) existing = root.AddComponent<AudioFactoryDriver>();
            RestoreDriver(existing);
        }

        internal static void RestoreDriver(AudioFactoryDriver candidate)
        {
            if (driver == candidate && Voices[0] != null) return;
            if (driver != null && driver != candidate)
            {
                // Also retire a leftover duplicate from an older implementation.
                if (driver.gameObject != candidate.gameObject)
                {
                    AudioSource[] duplicateSources = candidate.GetComponents<AudioSource>();
                    for (int i = 0; i < duplicateSources.Length; i++) duplicateSources[i].Stop();
                    UnityEngine.Object.Destroy(candidate.gameObject);
                }
                else UnityEngine.Object.Destroy(candidate);
                return;
            }
            driver = candidate;
            GameObject root = candidate.gameObject;
            root.name = RootName;
            UnityEngine.Object.DontDestroyOnLoad(root);
            bool resumeAmbience = candidate.AmbienceEnabled;
            bool resumePaused = candidate.Paused;
            int resumeFloor = candidate.Floor;
            bool resumePower = candidate.Powered;
            AudioSource[] existingSources = root.GetComponents<AudioSource>();
            activeAmbience = -1;
            ambienceName = null;
            for (int i = 0; i < PoolSize + 2; i++)
            {
                AudioSource source = i < existingSources.Length ? existingSources[i] : root.AddComponent<AudioSource>();
                ConfigureSource(source, i >= PoolSize);
                if (i < PoolSize) Voices[i] = source;
                else
                {
                    Ambience[i - PoolSize] = source;
                    source.priority = 200;
                    AmbienceTarget[i - PoolSize] = 0f;
                }
            }
            for (int i = PoolSize + 2; i < existingSources.Length; i++)
            {
                existingSources[i].Stop();
                UnityEngine.Object.Destroy(existingSources[i]);
            }
            RestoreClipCache(candidate.CachedClips);
            // Driver and sources are already registered, so Warmup cannot recurse into creation.
            Warmup();
            SetPaused(resumePaused);
            if (resumeAmbience) SetAmbience(resumeFloor, resumePower);
        }

        private static void RestoreClipCache(AudioClip[] snapshot)
        {
            if (Clips.Count != 0 || snapshot == null) return;
            int expected = AmbientNames.Length;
            for (int i = 0; i < SoundNames.Length; i++) expected += VariantCount(SoundNames[i]);
            bool valid = snapshot.Length == expected;
            for (int i = 0; i < snapshot.Length; i++) if (snapshot[i] == null) valid = false;
            if (!valid)
            {
                for (int i = 0; i < snapshot.Length; i++)
                    if (snapshot[i] != null) UnityEngine.Object.Destroy(snapshot[i]);
                return;
            }
            int offset = 0;
            for (int i = 0; i < SoundNames.Length + AmbientNames.Length; i++)
            {
                string key = i < SoundNames.Length ? SoundNames[i] : AmbientNames[i - SoundNames.Length];
                int count = VariantCount(key);
                var bank = new AudioClip[count];
                Array.Copy(snapshot, offset, bank, 0, count);
                Clips[key] = bank;
                offset += count;
            }
            CachedClipCount = expected;
            warmed = true;
        }

        private static void ConfigureSource(AudioSource source, bool loop)
        {
            source.Stop();
            source.clip = null;
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.pitch = 1f;
            source.panStereo = 0f;
            source.volume = 0f;
            source.priority = 100;
        }

        private static void PlayInternal(string name, float volume, float pan)
        {
            if (string.IsNullOrEmpty(name) || volume <= .003f) return;
            string canonical;
            if (!Aliases.TryGetValue(name, out canonical)) canonical = "ui_click";
            bool menu = canonical == "ui_click" || canonical == "ui_hover" || canonical == "win" || canonical == "loss";
            if (paused && !menu) return;
            EnsureSources();
            float now = Time.unscaledTime;
            float interval = canonical == "step_wood" || canonical == "step_metal" ? .075f
                : canonical == "mutant_growl" ? .3f : canonical == "spider_skitter" ? .09f
                : canonical == "ui_hover" ? .06f : canonical == "alarm" ? .65f : 0f;
            float last;
            if (interval > 0f && LastPlayed.TryGetValue(canonical, out last) && now - last < interval) return;
            LastPlayed[canonical] = now;
            int priority = menu || canonical == "hurt" || canonical == "boss_death" || canonical == "generator" || canonical == "elevator" ? 3
                : canonical.StartsWith("step_", StringComparison.Ordinal) || canonical == "mutant_growl" || canonical == "spider_skitter" ? 0 : 1;
            int slot = -1;
            int weakestPriority = int.MaxValue;
            float oldest = float.MaxValue;
            for (int i = 0; i < PoolSize; i++)
            {
                if (!Voices[i].isPlaying) { slot = i; break; }
                if (VoicePriority[i] <= priority && (VoicePriority[i] < weakestPriority
                    || (VoicePriority[i] == weakestPriority && VoiceStarted[i] < oldest)))
                { weakestPriority = VoicePriority[i]; oldest = VoiceStarted[i]; slot = i; }
            }
            if (slot < 0) { DroppedSoundCount++; return; }
            AudioClip[] bank = GetBank(canonical);
            AudioSource source = Voices[slot];
            source.Stop();
            source.clip = bank[variation.Next(bank.Length)];
            float pitchRange = menu ? .015f : priority == 0 ? .1f : .055f;
            source.pitch = 1f + ((float)variation.NextDouble() * 2f - 1f) * pitchRange;
            source.panStereo = pan;
            source.volume = Mathf.Clamp01(volume) * MasterGain * (.93f + (float)variation.NextDouble() * .07f);
            source.priority = priority == 3 ? 40 : priority == 0 ? 160 : 100;
            VoicePriority[slot] = priority;
            VoiceStarted[slot] = now;
            source.Play();
            PlayedSoundCount++;
            PeakVoices = Mathf.Max(PeakVoices, ActiveVoices);
        }

        private static Dictionary<string, string> CreateAliases()
        {
            var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < SoundNames.Length; i++) aliases[SoundNames[i]] = SoundNames[i];
            aliases["shot"] = "shoot";
            aliases["melee"] = "bash";
            aliases["impact"] = "hit";
            aliases["damage"] = "hurt";
            aliases["death"] = "loss";
            aliases["select"] = "ui_click";
            aliases["confirm"] = "ui_click";
            aliases["footstep_wood"] = "step_wood";
            aliases["footstep_metal"] = "step_metal";
            return aliases;
        }

        private static AudioClip[] GetBank(string name)
        {
            AudioClip[] bank;
            if (Clips.TryGetValue(name, out bank) && bank[0] != null) return bank;
            bool ambient = name.StartsWith("ambient_", StringComparison.Ordinal);
            int variants = VariantCount(name);
            bank = new AudioClip[variants];
            for (int i = 0; i < variants; i++) bank[i] = ambient ? CreateAmbience(name) : CreateEffect(name, i);
            Clips[name] = bank;
            CachedClipCount += variants;
            return bank;
        }

        private static int VariantCount(string name)
        {
            return name == "shoot" || name == "step_wood" || name == "step_metal" || name == "hit" || name == "spider_skitter" ? 3
                : name == "mutant_growl" || name == "mutant_death" || name == "spider_death" ? 2 : 1;
        }

        private static AudioClip CreateEffect(string name, int variant)
        {
            float length = Duration(name);
            int count = Mathf.CeilToInt(length * SampleRate);
            var samples = new float[count];
            uint random = 0x4B2A61u + (uint)(variant * 173);
            float low = 0f, soft = 0f;
            float detune = 1f + variant * .045f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float p = t / length;
                float noise = Noise(ref random);
                low += (noise - low) * .06f;
                soft += (noise - soft) * .24f;
                float s;
                switch (name)
                {
                    case "shoot":
                        s = soft * .95f * Decay(t, 53f) + Chirp(t, 165f * detune, -720f) * .65f * Decay(t, 28f);
                        s += low * .8f * Decay(t, 14f);
                        break;
                    case "reload":
                        s = soft * (.5f * Pulse(t, .01f, .012f) + .65f * Pulse(t, .18f, .018f) + .5f * Pulse(t, .37f, .01f));
                        s += Tone(t, 670f) * .2f * Pulse(t, .38f, .018f);
                        break;
                    case "dryfire": case "ui_hover":
                        s = soft * .45f * Decay(t, 90f) + Tone(t, name == "dryfire" ? 440f : 980f) * .2f * Decay(t, 65f);
                        break;
                    case "bash":
                        s = low * Mathf.Sin(p * Mathf.PI) * .9f + Chirp(t, 170f, -300f) * .25f * Decay(t, 10f);
                        break;
                    case "hit": case "trap_snap":
                        s = Chirp(t, 180f * detune, -600f) * .55f * Decay(t, 22f) + soft * .7f * Decay(t, 43f);
                        if (name == "trap_snap") s += Tone(t, 940f) * .23f * Decay(t, 19f);
                        break;
                    case "hurt":
                        s = Chirp(t, 160f, -200f) * .55f * Decay(t, 9f) + low * .7f * Decay(t, 12f);
                        break;
                    case "step_wood": case "step_metal":
                        s = Chirp(t, 74f * detune, -210f) * .42f * Decay(t, 31f) + low * .55f * Decay(t, 36f);
                        if (name == "step_metal") s += (Tone(t, 390f * detune) + Tone(t, 740f)) * .09f * Decay(t, 29f);
                        else s += Chirp(t, 170f, -70f) * .13f * Decay(t, 20f);
                        break;
                    case "mutant_growl": case "mutant_death": case "boss_roar": case "boss_death":
                        bool boss = name == "boss_roar" || name == "boss_death";
                        bool death = name == "mutant_death" || name == "boss_death";
                        float baseHz = (boss ? 48f : 85f) * detune;
                        float growl = Chirp(t, baseHz, death ? -baseHz * .5f / length : -8f);
                        s = (growl * .46f + Tone(t, baseHz * 2f + 7f * Tone(t, 6f)) * .15f + low * 1.15f)
                            * Mathf.Sin(p * Mathf.PI) * (.7f + .3f * Tone(t, boss ? 11f : 17f));
                        break;
                    case "spider_skitter": case "spider_death":
                        float taps = Pulse(t, .01f, .015f) + Pulse(t, .075f, .013f) + Pulse(t, .145f, .018f);
                        s = (soft * .55f + Chirp(t, 620f * detune, -920f) * .25f) * taps;
                        if (name == "spider_death") s += Chirp(t, 950f, -1600f) * .3f * Mathf.Sin(p * Mathf.PI) + low * .6f * Decay(t, 7f);
                        break;
                    case "laser": case "boss_burst":
                        s = Chirp(t, name == "laser" ? 1150f : 550f, name == "laser" ? -2900f : -950f) * .38f * Decay(t, 13f);
                        s += low * (name == "laser" ? .35f : .9f) * Decay(t, 17f);
                        break;
                    case "pickup_ammo":
                        s = soft * .35f * (Pulse(t, .01f, .01f) + Pulse(t, .075f, .01f));
                        s += Bell(t - .11f, 784f) * .32f;
                        break;
                    case "pickup_medkit":
                        s = Bell(t, 523.25f) * .32f + Bell(t - .12f, 659.25f) * .36f;
                        break;
                    case "pickup_fuse":
                        s = Bell(t, 740f) * .27f + Bell(t - .085f, 988f) * .3f + Bell(t - .17f, 1480f) * .22f;
                        break;
                    case "pickup_key": case "unlock":
                        s = soft * .3f * Pulse(t, .01f, .012f) + Bell(t - .06f, 1046.5f) * .3f + Bell(t - .18f, 1318.5f) * .23f;
                        break;
                    case "pickup": case "ui_click":
                        s = Bell(t, name == "pickup" ? 659.25f : 690f) * .33f;
                        if (name == "pickup") s += Bell(t - .09f, 987.77f) * .25f;
                        break;
                    case "cabinet":
                        s = low * .8f * (Pulse(t, .01f, .045f) + Pulse(t, .18f, .025f));
                        s += Chirp(t, 230f, -250f) * .22f * Mathf.Sin(p * Mathf.PI);
                        break;
                    case "locked":
                        s = Bell(t, 245f) * .35f + Bell(t - .13f, 207.65f) * .3f + soft * .35f * Pulse(t, .01f, .01f);
                        break;
                    case "door":
                        s = Chirp(t, 120f, 110f) * .19f * Mathf.Sin(p * Mathf.PI) + low * .9f * Pulse(t, .015f, .07f);
                        s += Tone(t, 65f) * .4f * Pulse(t, .4f, .035f);
                        break;
                    case "trap_arm":
                        s = (soft * .5f + Tone(t, 430f) * .25f) * (Pulse(t, .015f, .017f) + Pulse(t, .11f, .013f));
                        break;
                    case "scientist":
                        s = Bell(t, 880f) * .2f + Bell(t - .1f, 1174.66f) * .24f + Bell(t - .22f, 987.77f) * .17f;
                        break;
                    case "generator":
                        s = (Chirp(t, 36f, 43f) * .28f + Tone(t, 83f) * .09f + low * .65f) * Mathf.Sin(p * Mathf.PI);
                        s += soft * .55f * Pulse(t, .035f, .025f);
                        break;
                    case "elevator":
                        s = (Tone(t, 66f) * .22f + Tone(t, 132f) * .06f + low * .32f) * Mathf.Sin(p * Mathf.PI);
                        s += Bell(t - .88f, 1046.5f) * .37f + Bell(t - 1.05f, 783.99f) * .23f;
                        break;
                    case "alarm":
                        s = Tone(t, 550f + 80f * Tone(t, 2.5f)) * .23f * Mathf.Sin(p * Mathf.PI);
                        break;
                    case "win":
                        s = Bell(t, 523.25f) * .3f + Bell(t - .22f, 659.25f) * .3f + Bell(t - .44f, 783.99f) * .3f + Bell(t - .7f, 1046.5f) * .36f;
                        break;
                    case "loss":
                        s = Bell(t, 392f) * .32f + Bell(t - .27f, 311.13f) * .3f + Bell(t - .54f, 196f) * .4f;
                        break;
                    default: s = 0f; break;
                }
                float fade = Mathf.Min(1f, t * 500f) * Mathf.Clamp01((length - t) * 70f);
                samples[i] = Mathf.Clamp(s * fade, -.85f, .85f);
            }
            AudioClip clip = AudioClip.Create("PlanB " + name + " " + (variant + 1), count, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateAmbience(string name)
        {
            const float length = 3f;
            int frames = (int)(length * SampleRate);
            var samples = new float[frames * 2];
            bool lab = name.StartsWith("ambient_lab", StringComparison.Ordinal);
            bool powered = name.EndsWith("power", StringComparison.Ordinal);
            uint random = lab ? 6357u : 9271u;
            float low = 0f;
            for (int i = 0; i < frames; i++)
            {
                float t = i / (float)SampleRate;
                float p = t / length;
                low += (Noise(ref random) - low) * .015f;
                // Integer cycle counts keep the hum seamless. Noise/creaks fade at the seam.
                float seam = Mathf.Sin(p * Mathf.PI);
                float hum = (Tone(t, lab ? 58f : 42f) * .25f + Tone(t, lab ? 116f : 84f) * .07f) * (powered ? 1f : .4f);
                float air = low * .9f * seam * seam;
                float detail = lab ? Tone(t, 310f + 3f * Tone(t, 1f)) * .016f * seam * seam
                    : Chirp(t - .55f, 145f, -18f) * .09f * Pulse(t, .55f, .15f);
                float pipe = Tone(t, lab ? 245f : 205f) * .055f * Pulse(t, 1.94f, .12f);
                samples[i * 2] = hum + air + detail + pipe;
                samples[i * 2 + 1] = hum + air * .85f - detail * .5f + pipe * .6f;
            }
            AudioClip clip = AudioClip.Create("PlanB " + name, frames, 2, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float Duration(string name)
        {
            switch (name)
            {
                case "ui_hover": case "dryfire": return .085f;
                case "ui_click": return .13f;
                case "shoot": case "hit": case "step_wood": case "step_metal": return .18f;
                case "laser": case "bash": case "trap_arm": return .25f;
                case "spider_skitter": return .24f;
                case "reload": case "scientist": case "pickup_key": case "unlock": return .48f;
                case "cabinet": case "door": case "spider_death": return .6f;
                case "mutant_growl": case "mutant_death": case "alarm": return .8f;
                case "generator": case "boss_roar": case "boss_death": return 1.25f;
                case "elevator": case "win": return 1.55f;
                case "loss": return 1.2f;
                default: return .34f;
            }
        }

        private static float Noise(ref uint state)
        {
            state ^= state << 13; state ^= state >> 17; state ^= state << 5;
            return (state & 65535u) / 32767.5f - 1f;
        }
        private static float Tone(float t, float hz) { return Mathf.Sin(t * hz * Mathf.PI * 2f); }
        private static float Chirp(float t, float hz, float slope) { return Mathf.Sin((hz * t + .5f * slope * t * t) * Mathf.PI * 2f); }
        private static float Decay(float t, float speed) { return Mathf.Exp(-t * speed); }
        private static float Bell(float t, float hz)
        {
            if (t < 0f) return 0f;
            return (Tone(t, hz) + Tone(t, hz * 2f) * .13f) * Decay(t, 10f) * Mathf.Min(1f, t * 500f);
        }
        private static float Pulse(float t, float start, float width)
        {
            if (t < start || t > start + width * 6f) return 0f;
            return Mathf.Exp(-(t - start) / width);
        }
    }

    // One tiny update drives all crossfades using real time, including the pause menu.
    internal sealed class AudioFactoryDriver : MonoBehaviour
    {
        [SerializeField] private int floor = 1;
        [SerializeField] private bool powered;
        [SerializeField] private bool ambienceEnabled;
        [SerializeField] private bool paused;
        [SerializeField] private AudioClip[] cachedClips;
        internal int Floor { get { return floor; } }
        internal bool Powered { get { return powered; } }
        internal bool AmbienceEnabled { get { return ambienceEnabled; } }
        internal bool Paused { get { return paused; } }
        internal AudioClip[] CachedClips { get { return cachedClips; } }
        internal void SaveAmbience(int level, bool power, bool enabled)
        { floor = level; powered = power; ambienceEnabled = enabled; }
        internal void SavePaused(bool value) { paused = value; }
        internal void SaveCachedClips(AudioClip[] value) { cachedClips = value; }
        internal void ClearSavedState()
        { floor = 1; powered = ambienceEnabled = paused = false; cachedClips = null; }
        private void OnEnable()
        {
            if (Application.isPlaying) AudioFactory.RestoreDriver(this);
        }
        private void Update() { AudioFactory.Tick(Time.unscaledDeltaTime); }
    }
}
