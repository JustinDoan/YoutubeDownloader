using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;
using Syroot.Windows.IO;
using YoutubeExtractor;
using System.Threading.Tasks;
using System.IO;

namespace WpfApplication2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : MetroWindow
    {
        public static readonly Regex YoutubeVideoRegex = new Regex(@"(?:youtube.[a-z]+/[a-z\?\&]*v[/|=]|youtu.be/)([0-9a-zA-Z-_]+)", RegexOptions.IgnoreCase);

        public static string videoID;

        public MainWindow()
        {
            InitializeComponent();
        }


        public void getImage(string id)
        {

            
            Uri thumbnailUri = new Uri("https://i1.ytimg.com/vi/" + id + "/hqdefault.jpg");
            Console.WriteLine(thumbnailUri.ToString());
            using (WebClient client = new WebClient())
            {
                client.DownloadFileCompleted += (sender, e) => setThumbnail();
                client.DownloadFileAsync(thumbnailUri, AppDomain.CurrentDomain.BaseDirectory + "Thumbnail.jpg");
            }


        }
        public void getIdFromUrl(string url)
        {
            Match youtubeMatch = YoutubeVideoRegex.Match(url);
            string id = string.Empty;
            if (youtubeMatch.Success)
                id = youtubeMatch.Groups[1].Value;

            videoID = id;
        }

        private void setThumbnail()
        {

            thumbnailBox.Source = new BitmapImage(new Uri(Environment.CurrentDirectory + @"/Thumbnail.jpg"));

        }

        private void onSearch(object sender, RoutedEventArgs e)
        {
            if (linkBox.Text == "")
            {
                actionLabel.Content = "You need to enter in a url!";
            }
            else
            {
                actionLabel.Content = "Video Found!";
                getIdFromUrl(linkBox.Text);
                getVideoInformation(videoID);
                getImage(videoID);
                downloadButton.IsEnabled = true;
            }

        }


        public void getVideoInformation(string id)
        {
            string json = "";
            using (WebClient client = new WebClient())
            {
                string apiKey = "AIzaSyBu-SrLfJ8LuQ_2v5ElOsAoDeVXYcZZnG8";
                json = client.DownloadString(@"https://www.googleapis.com/youtube/v3/videos?part=snippet&id=" + id + "&key=" + apiKey);

            }

            var videoInformation = JObject.Parse(json);
            Console.WriteLine(videoInformation.ToString());
            videoTitle.Content = videoInformation["items"][0]["snippet"]["title"].ToString();



        }
        public Task downloadVideo(string url, string resolution)
        {
            
            return Task.Run(() =>
            {



                IEnumerable<VideoInfo> videoInfos = DownloadUrlResolver.GetDownloadUrls(url);
                VideoInfo video;
                try
                {
                    
                    int reso;
                    int.TryParse(resolution, out reso);
                    video = videoInfos
                   .First(info => info.VideoType == VideoType.Mp4 && info.Resolution == reso);
                } catch
                {
                    Console.WriteLine("Switch to fall back resolution");
                    video = videoInfos
                    .First(info => info.VideoType == VideoType.Mp4 &&
                    (info.Resolution == 1080 || info.Resolution == 720 || info.Resolution == 480 ||
                    info.Resolution == 360 || info.Resolution == 240 || info.Resolution == 144));
                }

                if (video.RequiresDecryption)
                {
                    DownloadUrlResolver.DecryptDownloadUrl(video);
                }

                string downloadsPath = new KnownFolder(KnownFolderType.Downloads).Path;
                string fullPath = Path.Combine(video.Title + video.VideoExtension);
                foreach (var c in Path.GetInvalidFileNameChars()) { fullPath = fullPath.Replace(c, '-'); }
                fullPath = downloadsPath + @"\" + fullPath;
                var videoDownloader = new VideoDownloader(video, fullPath);
                videoDownloader.DownloadProgressChanged += UpdateProgressBar;
                videoDownloader.Execute();







            });
                       


            
        }

        private void UpdateProgressBar(Object sender, ProgressEventArgs args) {
            Dispatcher.BeginInvoke((Action) (() =>
            {
                downloadProgressBar.Value = args.ProgressPercentage;
                progressLabel.Content = args.ProgressPercentage + "%";
            }));
        }

        private async void onClickDownload(object sender, RoutedEventArgs e)
        {
            //We also want to download at the resolution that the user selected.
            string resolution = resolutionBox.SelectedValue.ToString();


            await downloadVideo(linkBox.Text, resolution);
            progressLabel.Content = "Completed!";
            
        }
    }
}
