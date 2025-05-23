
using System.Collections.Generic;
using Emgu.CV;
using System.Drawing;

namespace SpotTheDifference.Logic
{
    public class GameManager
    {
        public Mat Image1 { get; private set; }
        public Mat Image2 { get; private set; }

        private int maxAttempts;
        private int remainingAttempts;
        private string level;

        public GameManager(string level)
        {
            this.level = level;
            LoadLevelImages(level);
            SetDifficultySettings(level);
        }

        private void LoadLevelImages(string level)
        {
            string path1 = $"Assets/Levels/{level}/img1.jpg";
            string path2 = $"Assets/Levels/{level}/img2.jpg";
            Image1 = CvInvoke.Imread(path1, Emgu.CV.CvEnum.ImreadModes.Color);
            Image2 = CvInvoke.Imread(path2, Emgu.CV.CvEnum.ImreadModes.Color);
        }

        private void SetDifficultySettings(string level)
        {
            if (level == "Easy") maxAttempts = 10;
            else if (level == "Medium") maxAttempts = 7;
            else maxAttempts = 5;

            remainingAttempts = maxAttempts;
        }

        public List<Rectangle> GetDifferences()
        {
            return ImageComparer.DetectDifferenceRegions(Image1, Image2);
        }

        public void RegisterMiss()
        {
            remainingAttempts--;
        }

        public bool IsGameOver(int foundCount)
        {
            return remainingAttempts <= 0 || foundCount >= GetDifferences().Count;
        }
    }
}
