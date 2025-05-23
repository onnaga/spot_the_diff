using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Collections.Generic;
using System.Drawing;

namespace SpotTheDifference.Logic
{
    public static class ImageComparer
    {
        public static List<Rectangle> DetectDifferenceRegions(Mat img1, Mat img2)
        {
            // تحويل الصور إلى LAB (أكثر دقة لتمييز الفروقات في اللون)
            Mat img1Lab = new();
            Mat img2Lab = new();
            CvInvoke.CvtColor(img1, img1Lab, ColorConversion.Bgr2Lab);
            CvInvoke.CvtColor(img2, img2Lab, ColorConversion.Bgr2Lab);

            // حساب الفرق بين الصورتين في الفضاء اللوني
            Mat diff = new();
            CvInvoke.AbsDiff(img1Lab, img2Lab, diff);

            // تجاهل القناة L (الإضاءة) والتركيز على A و B فقط
            VectorOfMat channels = new();
            CvInvoke.Split(diff, channels);
            Mat abDiff = new();
            CvInvoke.AddWeighted(channels[1], 0.5, channels[2], 0.5, 0, abDiff);

            // تحويل للصورة الرمادية ثم Threshold
            CvInvoke.GaussianBlur(abDiff, abDiff, new Size(7, 7), 0);
            CvInvoke.Threshold(abDiff, abDiff, 20, 255, ThresholdType.Binary);

            // تنظيف الضوضاء Morphological operations
            Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(5, 5), new Point(-1, -1));
            CvInvoke.MorphologyEx(abDiff, abDiff, MorphOp.Close, kernel, new Point(-1, -1), 2, BorderType.Default, new MCvScalar());

            // استخراج المكونات المتصلة
            List<Rectangle> differences = new();
            Mat labels = new(), stats = new(), centroids = new();
            int nLabels = CvInvoke.ConnectedComponentsWithStats(abDiff, labels, stats, centroids);

            for (int i = 1; i < nLabels; i++)
            {
                int x = stats.GetData().GetValue(i, 0) as int? ?? 0;
                int y = stats.GetData().GetValue(i, 1) as int? ?? 0;
                int width = stats.GetData().GetValue(i, 2) as int? ?? 0;
                int height = stats.GetData().GetValue(i, 3) as int? ?? 0;

                // تجاهل الفروق الصغيرة جدًا
                if (width > 15 && height > 15)
                {
                    differences.Add(new Rectangle(x, y, width, height));
                }
            }

            return differences;
        }

        public static Mat ResizeAndPad(Mat image, int targetHeight, int targetWidth)
        {
            int originalWidth = image.Width;
            int originalHeight = image.Height;

            float ratio = Math.Min((float)targetWidth / originalWidth, (float)targetHeight / originalHeight);
            int newWidth = (int)(originalWidth * ratio);
            int newHeight = (int)(originalHeight * ratio);

            Mat resized = new();
            CvInvoke.Resize(image, resized, new Size(newWidth, newHeight));

            Mat padded = new(new Size(targetWidth, targetHeight), DepthType.Cv8U, 3);
            padded.SetTo(new MCvScalar(255, 255, 255)); // خلفية بيضاء

            int xOffset = (targetWidth - newWidth) / 2;
            int yOffset = (targetHeight - newHeight) / 2;
            Rectangle roi = new(xOffset, yOffset, newWidth, newHeight);

            Mat roiMat = new(padded, roi);
            resized.CopyTo(roiMat);

            return padded;
        }
    }
}
