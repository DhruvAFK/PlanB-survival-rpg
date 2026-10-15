using System;
using System.IO;
using UnityEngine;

namespace PlanB.Editor
{
    public static class PlanBPolishChecks
    {
        static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Polish regression: " + message);
        }

        public static void Verify(GameManager game)
        {
            AudioFactory.Warmup(); Bullet.Warmup(); Effects.Warmup();
            Bullet.ClearActive(); Effects.ClearActive();
            int initialBullets = Bullet.CreatedCount;
            int initialEffects = Effects.CreatedCount;
            Vector2 position = game.Player.transform.position;
            for (int i = 0; i < 512; i++)
            {
                Bullet.Spawn(position, Vector2.right, (i & 1) == 0, 1, 6);
                Bullet.ClearActive();
                Effects.Burst(position, Color.yellow, 14);
                Effects.ClearActive();
            }
            Require(Bullet.CreatedCount == initialBullets && initialBullets == 64 && Bullet.ActiveCount == 0,
                "Repeated firing created extra projectiles or retained active rounds.");
            Require(Effects.CreatedCount == initialEffects && initialEffects == 1 && Effects.ActiveCount == 0,
                "Repeated impacts created extra debris objects or retained fragments.");

            // Verify cache hits rather than counting only the managed cache entries.
            Sprite sprite = SpriteLibrary.Get("fighter", 1, 0);
            long beforeSprites = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) SpriteLibrary.Get("fighter", 1, 0);
            long spriteBytes = GC.GetAllocatedBytesForCurrentThread() - beforeSprites;
            Require(sprite != null && spriteBytes == 0, "Warmed sprite lookups allocated " + spriteBytes + " managed bytes.");
            game.Hud.Refresh();
            long beforeHud = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) game.Hud.Refresh();
            long hudBytes = GC.GetAllocatedBytesForCurrentThread() - beforeHud;
            Require(hudBytes == 0, "Unchanged HUD refreshes allocated " + hudBytes + " managed bytes.");
            int mapTextures = 0;
            foreach (var texture in Resources.FindObjectsOfTypeAll<Texture2D>())
                if (texture.name.StartsWith("PlanB floor ", StringComparison.Ordinal) ||
                    texture.name.StartsWith("PlanB wall atlas ", StringComparison.Ordinal) ||
                    texture.name.StartsWith("PlanB map preview ", StringComparison.Ordinal)) mapTextures++;
            Require(mapTextures == 6, "Map textures grew across repeated restarts: " + mapTextures);

            int clipsBefore = AudioFactory.CachedClipCount;
            Require(clipsBefore >= 52, "The complete sound bank was not warmed.");
            var audioRoot = GameObject.Find("PlanB Cached Audio (12 voices + ambience)");
            Require(audioRoot != null && audioRoot.GetComponents<AudioSource>().Length == 14,
                "Audio source count grew beyond 12 effect voices and two ambience channels.");
            var savedRandom = UnityEngine.Random.state;
            UnityEngine.Random.InitState(2718);
            float expected = UnityEngine.Random.value;
            UnityEngine.Random.InitState(2718);
            AudioFactory.Play("shoot");
            float actual = UnityEngine.Random.value;
            UnityEngine.Random.state = savedRandom;
            Require(expected == actual, "Cosmetic sound variation changed the gameplay random sequence.");
            for (int i = 0; i < 40; i++) AudioFactory.Play("shoot", .2f);
            Require(AudioFactory.ActiveVoices <= AudioFactory.VoiceLimit && AudioFactory.PeakVoices <= AudioFactory.VoiceLimit,
                "Sound concurrency exceeded its fixed voice budget.");
            Require(AudioFactory.CachedClipCount == clipsBefore, "Combat synthesized extra clips after warmup.");
            game.SetPaused(true);
            int played = AudioFactory.PlayedSoundCount;
            AudioFactory.Play("shoot");
            Require(AudioFactory.PlayedSoundCount == played, "Combat sound continued while paused.");
            AudioFactory.Play("ui_click");
            Require(AudioFactory.PlayedSoundCount == played + 1, "Pause menu sounds were silenced.");
            game.SetPaused(false);

            int checkedClips = 0;
            foreach (var clip in Resources.FindObjectsOfTypeAll<AudioClip>())
            {
                if (!clip.name.StartsWith("PlanB ", StringComparison.Ordinal)) continue;
                float[] samples = new float[clip.samples * clip.channels];
                Require(clip.GetData(samples, 0), "Sound data is unavailable: " + clip.name);
                float peak = 0;
                for (int i = 0; i < samples.Length; i++)
                {
                    if (float.IsNaN(samples[i]) || float.IsInfinity(samples[i]))
                        throw new Exception("Invalid audio sample in " + clip.name);
                    peak = Mathf.Max(peak, Mathf.Abs(samples[i]));
                }
                Require(peak > .001f && peak <= 1, "Silent or clipped sound: " + clip.name);
                checkedClips++;
            }
            Require(checkedClips >= 52, "Not all sound clips could be inspected.");
            string report = "PASS\n512 fire/impact reuse cycles: 64 projectile objects, one debris system, no retained rounds/fragments.\n" +
                "10000 warmed sprite lookups: " + spriteBytes + " managed bytes.\n1000 unchanged HUD refreshes: " + hudBytes + " managed bytes.\n" +
                checkedClips + " non-silent finite audio clips checked. Twelve effect voices and two ambience sources; pause/menu and gameplay RNG checks passed.\n" +
                "Six baked map textures after repeated restarts; no duplicate floor atlases retained.\n" +
                "These are allocation/lifecycle checks, not an FPS benchmark on the user's machine.\n";
            Directory.CreateDirectory("Assets/PlanB/Verification");
            File.WriteAllText("Assets/PlanB/Verification/PolishResult.txt", report);
            Debug.Log("PLANB_POLISH_PASS: " + report);
        }
    }
}
