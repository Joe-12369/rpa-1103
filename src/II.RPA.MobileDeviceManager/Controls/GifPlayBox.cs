using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Media.Imaging;
using Control = System.Windows.Controls.Control;

namespace II.RPA.MobileDeviceManager.Controls
{
    [TemplatePart(Name = "PART_Host", Type = typeof(WindowsFormsHost))]
    public class GifPlayBox : Control
    {
        private WindowsFormsHost PART_Host;
        private PictureBox PART_PictureBox;
        public static readonly DependencyProperty GifUrlProperty = DependencyProperty.Register("GifUrl", typeof(string), typeof(GifPlayBox), new PropertyMetadata(null, OnGifUrlChangedCallback));


        static GifPlayBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GifPlayBox), new FrameworkPropertyMetadata(typeof(GifPlayBox)));
        }

        public string GifUrl
        {
            get
            {
                return (string)GetValue(GifUrlProperty);
            }
            set
            {
                SetValue(GifUrlProperty, value);
            }
        }

        private static void OnGifUrlChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((GifPlayBox)d).OnGifUrlChanged();
        }

        private void OnGifUrlChanged()
        {
            TryLoadGifUrl();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (PART_Host != null)
            {
                PART_Host.Loaded -= PART_Host_Loaded;
            }
            PART_Host = GetTemplateChild("PART_Host") as WindowsFormsHost;
            if (PART_Host != null)
            {
                PART_Host.Loaded += PART_Host_Loaded;
            }
        }

        private void PART_Host_Loaded(object sender, RoutedEventArgs e)
        {
            PART_PictureBox = PART_Host.Child as PictureBox;
            PART_PictureBox.InitialImage = BitmapHelper.GetBitmap("Resources/Images/gifloading.png");
            PART_PictureBox.ErrorImage = BitmapHelper.GetBitmap("Resources/Images/gifloading.png");
            TryLoadGifUrl();
        }

        private void TryLoadGifUrl()
        {
            if (PART_PictureBox != null)
            {
                if (string.IsNullOrEmpty(GifUrl))
                {
                    PART_PictureBox.Tag = null;
                    PART_PictureBox.Image = null;
                }
                else if (!(PART_PictureBox.Tag is string url) || !(url == GifUrl))
                {
                    PART_PictureBox.Tag = GifUrl;
                    PART_PictureBox.WaitOnLoad = false;
                    PART_PictureBox.LoadAsync(GifUrl);
                }
            }
        }
    }

    public static class BitmapHelper
    {
        public static BitmapSource GetImageSource(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return null;
            }
            if (!Uri.TryCreate(new Uri("pack://application:,,,/II.RPA.MobileDeviceManager;component/", UriKind.Absolute), relativePath, out Uri uri))
            {
                return null;
            }
            try
            {
                if (uri.Scheme != "pack")
                {
                    BitmapImage bitmapImage = new BitmapImage(uri);
                    bitmapImage.Freeze();
                    return bitmapImage;
                }
                if (relativePath.EndsWith(".svg"))
                {
                    throw new NotSupportedException($"{relativePath}");
                }
                BitmapImage bitmapImage2 = new BitmapImage(uri);
                bitmapImage2.Freeze();
                return bitmapImage2;
            }
            catch
            {
                return null;
            }
        }

        public static Bitmap GetBitmap(string relativePath)
        {
            BitmapSource bitmapSource = GetImageSource(relativePath);
            Bitmap bitmap = new Bitmap(bitmapSource.PixelWidth, bitmapSource.PixelHeight, PixelFormat.Format32bppPArgb);
            BitmapData data = bitmap.LockBits(new Rectangle(System.Drawing.Point.Empty, bitmap.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
            bitmapSource.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
            bitmap.UnlockBits(data);
            return bitmap;
        }
    }
}
