using Newtonsoft.Json.Linq;
using Octgn.Core.DataExtensionMethods;
using Octgn.DataNew.Entities;
using Octgn.Library;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ThronesImageFetcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BackgroundWorker backgroundWorker = new BackgroundWorker();
        public IEnumerable<Card> cards;
        public JArray cardsjson;

        public bool OverwriteBool = false;

        public MainWindow()
        {
            this.InitializeComponent();
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.DoWork += DoWork;
            backgroundWorker.ProgressChanged += ProgressChanged;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;

        }

        private void Generate(object sender, RoutedEventArgs e)
        {
            if (backgroundWorker.IsBusy)
            {
                CurrentCard.Text = "Busy";
                return;
            }
            ProgressBar.Maximum = cards.Count();
            backgroundWorker.RunWorkerAsync();
        }


        void DoWork(object sender, DoWorkEventArgs e)
        {
            var i = 0;

            foreach (var card in cards)
            {
                if (backgroundWorker.CancellationPending) break;
                i++;
                var jcard = cardsjson.FirstOrDefault(x => x.Value<string>("octgn_id") == card.Id.ToString());
                if (jcard == null) continue;

                var cardset = card.GetSet();
                var garbage = Config.Instance.Paths.GraveyardPath;
                if (!Directory.Exists(garbage)) Directory.CreateDirectory(garbage);

                var imageUri = card.GetImageUri();

                var files =
                    Directory.GetFiles(cardset.ImagePackUri, imageUri + ".*")
                        .Where(x => System.IO.Path.GetFileNameWithoutExtension(x).Equals(imageUri, StringComparison.InvariantCultureIgnoreCase))
                        .OrderBy(x => x.Length)
                        .ToArray();

                if (files.Length > 0 && OverwriteBool == false)
                {
                    backgroundWorker.ReportProgress(i, card);
                    continue;
                }

                foreach (var f in files.Select(x => new FileInfo(x)))
                {
                    f.MoveTo(System.IO.Path.Combine(garbage, f.Name));
                }

                var newPath = System.IO.Path.Combine(cardset.ImagePackUri, imageUri + ".png");

                var url = "http://www.thronesdb.com" + jcard.Value<string>("imagesrc");

                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadFile(new Uri(url), newPath);
                }
                backgroundWorker.ReportProgress(i, card);
            }
        }

        private void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBar.Value = e.ProgressPercentage;
            CurrentCard.Text = (e.UserState as Card).Name;
            Stream imageStream = File.OpenRead((e.UserState as Card).GetPicture());

            var ret = new BitmapImage();
            ret.BeginInit();
            ret.CacheOption = BitmapCacheOption.OnLoad;
            ret.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            ret.StreamSource = imageStream;
            ret.EndInit();
            imageStream.Close();

            dbImage.Source = ret;

        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            CurrentCard.Text = "DONE";
        }

        private void CancelWorker(object sender, EventArgs e)
        {
            if (backgroundWorker.IsBusy)
            {
                CurrentCard.Text = "Cancel";
                backgroundWorker.CancelAsync();
            }
        }

        private void Overwrite(object sender, RoutedEventArgs e)
        {
            OverwriteBool = (sender as CheckBox).IsChecked ?? false;
        }
    }
}
