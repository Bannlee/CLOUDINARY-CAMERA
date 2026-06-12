using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace API_TRIAL
{
    public partial class MainWindow : System.Windows.Window
    {
        private Cloudinary cloudinary;
        private VideoCapture capture;
        private Mat latestFrame;
        private CancellationTokenSource cameraToken;

        public MainWindow()
        {
            InitializeComponent();

            //CONNECTS TO CLOUDINARY ON JERZEY'S ACCOUNT
            var account = new Account(
                "dlwwvseit",
                "688359832119721",
                "xEWSO3a2Lux_YDt3GD__VrbBwpE"
            );

            cloudinary = new Cloudinary(account);
        }

        //OPENS THE IMAGE SELECTOR
        private void SelectImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select an image",
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp"
            };

            if (dialog.ShowDialog() == true)
            {
                UploadImage(dialog.FileName);
            }
        }

        //SEARCHES FOR CAMERA || RECORDS FRAMES
        private void StartCamera_Click(object sender, RoutedEventArgs e)
        {
            for (int cameraIndex = 0; cameraIndex < 5; cameraIndex++)
            {
                using (var testCapture = new VideoCapture(cameraIndex))
                {
                    if (testCapture.IsOpened())
                    {
                        MessageBox.Show("Camera found at index: " + cameraIndex);

                        capture = new VideoCapture(cameraIndex);

                        cameraToken = new CancellationTokenSource();

                        Task.Run(() =>
                        {
                            while (!cameraToken.Token.IsCancellationRequested)
                            {
                                var frame = new Mat();
                                capture.Read(frame);

                                if (!frame.Empty())
                                {
                                    latestFrame?.Dispose();
                                    latestFrame = frame.Clone();

                                    Dispatcher.Invoke(() =>
                                    {
                                        CameraPreview.Source = frame.ToBitmapSource();
                                    });
                                }

                                frame.Dispose();
                            }
                        });

                        return;
                    }
                }
            }

            MessageBox.Show("No camera opened at indexes 0-4.");
        }

        // CAPTURES THE FRAME OF THE CAMERA WHEN ITS CLICKED, TURNS IT INTO A FILE
        private void TakePicture_Click(object sender, RoutedEventArgs e)
        {
            if (latestFrame == null || latestFrame.Empty())
            {
                MessageBox.Show("Start the camera first.");
                return;
            }

            string filePath = Path.Combine(Path.GetTempPath(), "captured-photo.jpg");

            Cv2.ImWrite(filePath, latestFrame);

            UploadImage(filePath);
        }

        //UPLOADS TO CLOUDINARY
        private void UploadImage(string filePath)
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(filePath)
            };

            var uploadResult = cloudinary.Upload(uploadParams);

            string imageUrl = uploadResult.SecureUrl.ToString(); //GENERATES THE LINK TO PUT INTO DATABASE

            MessageBox.Show("Upload worked!\n\nImage URL:\n" + imageUrl);
        }

        //BASTA CLOSED
        protected override void OnClosed(EventArgs e)
        {
            cameraToken?.Cancel();

            capture?.Release();
            capture?.Dispose();

            latestFrame?.Dispose();

            base.OnClosed(e);
        }
    }
}