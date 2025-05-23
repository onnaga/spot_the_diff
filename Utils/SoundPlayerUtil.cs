using System;
using System.IO;
using System.Media;

namespace WinFormsApp_deff_Game.Utils
{
    public static class SoundPlayerUtil
    {
        private static readonly string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sounds");
        private static readonly string correctSoundPath = Path.Combine(basePath, "c.wav");
        private static readonly string wrongSoundPath = Path.Combine(basePath, "w.wav");

        public static void PlayCorrect() => PlaySound(correctSoundPath);

        public static void PlayWrong() => PlaySound(wrongSoundPath);

        private static void PlaySound(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"Sound file not found: {path}");
                return;
            }

            try
            {
                using SoundPlayer player = new(path);
                player.Play();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sound playback error: {ex.Message}");
            }
        }
    }
}
