using Emgu.CV;
using Emgu.CV.CvEnum;
using SpotTheDifference.Logic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WinFormsApp_deff_Game.Utils;

namespace WinFormsApp_deff_Game
{
    public partial class Form1 : Form
    {
        private Mat img1, img2;
        private List<Rectangle> differences = new();
        private List<Rectangle> foundDifferences = new();
        private int attemptsLeft;

        private PictureBox pictureBox;
        private Label lblStatus;
        private ComboBox modeBox;
        private ComboBox playModeBox;
        private Button loadBtn1, loadBtn2, startBtn;
        private Bitmap combinedImage;
        private const int imageWidth = 600, imageHeight = 400;

        private System.Windows.Forms.Timer gameTimer;
        private int timeLeft; // in seconds

        public Form1()
        {
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "Spot the Difference - Game Modes";
            this.Width = 1300;
            this.Height = 700;

            loadBtn1 = new Button() { Text = "Load Image 1", Left = 20, Top = 20, Width = 120 };
            loadBtn2 = new Button() { Text = "Load Image 2", Left = 160, Top = 20, Width = 120 };
            startBtn = new Button() { Text = "Start Game", Left = 300, Top = 20, Width = 120 };

            modeBox = new ComboBox() { Left = 440, Top = 20, Width = 120 };
            modeBox.Items.AddRange(new string[] { "Easy", "Medium", "Hard" });
            modeBox.SelectedIndex = 0;

            playModeBox = new ComboBox() { Left = 580, Top = 20, Width = 150 };
            playModeBox.Items.AddRange(new string[] { "Attempts Mode", "Timer Mode" });
            playModeBox.SelectedIndex = 0;

            lblStatus = new Label() { Left = 750, Top = 25, Width = 500, Height = 25, Text = "Status: Ready" };

            pictureBox = new PictureBox()
            {
                Left = 20,
                Top = 60,
                Width = 1220,
                Height = 500,
                SizeMode = PictureBoxSizeMode.Normal,
                BorderStyle = BorderStyle.Fixed3D
            };

            this.Controls.AddRange(new Control[] { loadBtn1, loadBtn2, startBtn, modeBox, playModeBox, lblStatus, pictureBox });

            loadBtn1.Click += LoadBtn1_Click;
            loadBtn2.Click += LoadBtn2_Click;
            startBtn.Click += StartBtn_Click;
            pictureBox.MouseClick += PictureBox_MouseClick;

            gameTimer = new System.Windows.Forms.Timer();
            gameTimer.Interval = 1000; // 1 second
            gameTimer.Tick += GameTimer_Tick;
        }

        private void LoadBtn1_Click(object sender, EventArgs e)
        {
            using OpenFileDialog ofd = new();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                img1 = CvInvoke.Imread(ofd.FileName, ImreadModes.Color);
                img1 = ImageComparer.ResizeAndPad(img1, imageHeight, imageWidth);
                MessageBox.Show("Image 1 loaded.");
            }
        }

        private void LoadBtn2_Click(object sender, EventArgs e)
        {
            using OpenFileDialog ofd = new();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                img2 = CvInvoke.Imread(ofd.FileName, ImreadModes.Color);
                img2 = ImageComparer.ResizeAndPad(img2, imageHeight, imageWidth);
                MessageBox.Show("Image 2 loaded.");
            }
        }

        private void StartBtn_Click(object sender, EventArgs e)
        {
            if (img1 == null || img2 == null)
            {
                MessageBox.Show("Please load both images first.");
                return;
            }

            differences = ImageComparer.DetectDifferenceRegions(img1, img2);
            foundDifferences.Clear();

            string difficulty = modeBox.SelectedItem.ToString();
            string playMode = playModeBox.SelectedItem.ToString();

            if (playMode == "Attempts Mode")
            {
                attemptsLeft = difficulty switch
                {
                    "Easy" => differences.Count * 3,
                    "Medium" => differences.Count * 2,
                    _ => differences.Count
                };
                gameTimer.Stop();
                lblStatus.Text = $"Found: 0 / {differences.Count} | Attempts: {attemptsLeft}";
            }
            else if (playMode == "Timer Mode")
            {
                timeLeft = difficulty switch
                {
                    "Easy" => 180,
                    "Medium" => 120,
                    _ => 60
                };
                gameTimer.Start();
                lblStatus.Text = $"Found: 0 / {differences.Count} | Time: {FormatTime(timeLeft)}";
            }

            combinedImage = CombineImages(img1.ToBitmap(), img2.ToBitmap());
            pictureBox.Image = combinedImage;
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            timeLeft--;
            lblStatus.Text = $"Found: {foundDifferences.Count} / {differences.Count} | Time: {FormatTime(timeLeft)}";

            if (timeLeft <= 0)
            {
                gameTimer.Stop();
                MessageBox.Show("⏰ Time's up!");
                ShowAllDifferences();
            }
        }

        private void PictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (differences == null) return;

            bool isTimerMode = playModeBox.SelectedItem.ToString() == "Timer Mode";
            if (!isTimerMode && attemptsLeft <= 0) return;

            Point clickPoint = e.Location;
            bool hit = false;

            foreach (Rectangle rect in differences)
            {
                Rectangle rightSide = new(rect.X + imageWidth, rect.Y, rect.Width, rect.Height);
                Rectangle clickCircle = new(clickPoint.X - 20, clickPoint.Y - 20, 40, 40);

                if ((rightSide.IntersectsWith(clickCircle) || rect.IntersectsWith(clickCircle)) && !foundDifferences.Contains(rect))
                {
                    foundDifferences.Add(rect);
                    SoundPlayerUtil.PlayCorrect();
                    DrawAutoSizedCircle(rect, clickPoint, true);
                    hit = true;
                    break;
                }
            }

            if (!hit)
            {
                SoundPlayerUtil.PlayWrong();
                DrawAutoSizedCircle(new Rectangle(clickPoint, new Size(1, 1)), clickPoint, false);
            }

            if (!isTimerMode)
                attemptsLeft--;

            if (foundDifferences.Count == differences.Count)
            {
                gameTimer.Stop();
                MessageBox.Show("🎉 You found all differences!");
                ShowAllDifferences();
            }
            else if (!isTimerMode && attemptsLeft <= 0)
            {
                MessageBox.Show("💥 Game Over!");
                ShowAllDifferences();
            }

            if (isTimerMode)
            {
                lblStatus.Text = $"Found: {foundDifferences.Count} / {differences.Count} | Time: {FormatTime(timeLeft)}";
            }
            else
            {
                lblStatus.Text = $"Found: {foundDifferences.Count} / {differences.Count} | Attempts: {attemptsLeft}";
            }
        }

        private void DrawAutoSizedCircle(Rectangle targetRect, Point center, bool success)
        {
            using Graphics g = Graphics.FromImage(combinedImage);
            Pen pen = new(success ? Color.Green : Color.Red, 3);

            int radius = Math.Max(targetRect.Width, targetRect.Height) / 2 + 10;
            g.DrawEllipse(pen, center.X - radius, center.Y - radius, radius * 2, radius * 2);

            pictureBox.Image = (Bitmap)combinedImage.Clone();
        }

        private void ShowAllDifferences()
        {
            using Graphics g = Graphics.FromImage(combinedImage);
            Pen pen = new(Color.Red, 2);

            foreach (var rect in differences)
            {
                Rectangle rightRect = new(rect.X + imageWidth, rect.Y, rect.Width, rect.Height);
                g.DrawRectangle(pen, rightRect);
            }

            pictureBox.Image = (Bitmap)combinedImage.Clone();
        }

        private Bitmap CombineImages(Bitmap bmp1, Bitmap bmp2)
        {
            Bitmap combined = new(bmp1.Width + bmp2.Width, bmp1.Height);
            using Graphics g = Graphics.FromImage(combined);
            g.DrawImage(bmp1, 0, 0);
            g.DrawImage(bmp2, bmp1.Width, 0);
            return combined;
        }

        private string FormatTime(int totalSeconds)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:D2}:{seconds:D2}";
        }
    }
}
