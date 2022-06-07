using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics; //デバック用

namespace osuMusicPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string value;
        private string audioFilename;
        private string artistName;
        private string songTitle;
        private string bgFileName;
        private string directoryPath;

        public class MediaFile
        {
            public String FilePath { get; set; }
            public String ArtistName { get; set; }
            public String SongTitle { get; set; }
            public String JoinArtistTitle { get; set; }
            public String AudioFilename { get; set; }
            public String BgFileName { get; set; }

            public MediaFile(String filePath, String artistName, String songTitle, String audioFilename, String bgFileName)
            {
                var join = artistName + " - " + songTitle;
                this.FilePath = filePath;
                this.ArtistName = artistName;
                this.SongTitle = songTitle;
                this.JoinArtistTitle = join;
                this.AudioFilename = audioFilename;
                this.BgFileName = bgFileName;
            }
        }

        private List<MediaFile> FileList = new List<MediaFile>();

        public MainWindow()
        {
            InitializeComponent();

            // クリックイベント
            this.PlayButton.Click += PlayButtonClick;
            this.PauseButton.Click += PauseButton_Click;
            this.LoadButton.Click += LoadButton_Click;
            this.OpenButton.Click += OpenButton_Click;

            this.MediaElement.MediaEnded += MediaElement_MediaEnded;
            this.MediaElement.LoadedBehavior = MediaState.Manual;

        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            using (var cofd = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog()
            {
                Title = "Songsフォルダを選択してください",
                InitialDirectory = @"D:",
                IsFolderPicker = true, // フォルダ選択モードにする
            })
            {
                if (cofd.ShowDialog() != Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
                {
                    return;
                }

                this.FilePathText.Text = cofd.FileName;
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            //Songsにあるディレクトリを全て取得
            directoryPath = this.FilePathText.Text;
            DirectoryInfo di = new DirectoryInfo(directoryPath);

            DirectoryInfo[] diAlls = di.GetDirectories();

            if (diAlls.Any())
            {
                foreach (DirectoryInfo d in diAlls)
                {
                    bool getBG = false;
                    string selectedSongMetaPath = "";
                    //osuファイルの検索
                    IEnumerable<string> files =
                        System.IO.Directory.EnumerateFiles(
                            d.FullName, "*.osu", System.IO.SearchOption.TopDirectoryOnly);

                    if (files.Any())
                    {
                        foreach (string f in files)
                        {
                            selectedSongMetaPath = f;
                            break;
                        }

                        // osuファイルの読み取り
                        using (StreamReader reader = new StreamReader(selectedSongMetaPath))
                        {
                            while (!reader.EndOfStream)
                            {
                                string line = reader.ReadLine();
                                string[] values = line.Split(',');
                                

                                foreach (string value in values)
                                {
                                    // Artistの取得
                                    if (value.Contains("ArtistUnicode:"))
                                    {
                                        string[] artistNameLines = value.Split(':');
                                        foreach (string a in artistNameLines)
                                        {
                                            if (!a.Contains("ArtistUnicode"))
                                            {
                                                artistName = a.Trim();
                                            }
                                        }
                                        //SongTitleの取得
                                    }
                                    else if (value.Contains("TitleUnicode:"))
                                    {
                                        string[] titleLines = value.Split(':');
                                        foreach (string t in titleLines)
                                        {
                                            if (!t.Contains("TitleUnicode"))
                                            {
                                                songTitle = t.Trim();
                                            }
                                        }
                                        //AudioFilenameの取得
                                    }
                                    else if (value.Contains("AudioFilename:"))
                                    {
                                        string[] audioFileLines = value.Split(':');
                                        foreach (string a in audioFileLines)
                                        {
                                            if (!a.Contains("AudioFilename"))
                                            {
                                                audioFilename = a.Trim();
                                            }
                                        }
                                    }
                                    else if (value.Contains(".png") || value.Contains(".jpg") || value.Contains(".jpeg"))
                                    {
                                        if (!getBG) // SBの画像が引っかかる可能性があるので最初の1枚だけ
                                        {
                                            bgFileName = value.Trim('"');
                                            getBG = true;
                                        }
                                    }
                                }

                                // Unicode空の時用
                                if (!values.Contains("ArtistUnicode:"))
                                {
                                    foreach (string value in values)
                                    {
                                        if (value.Contains("Artist:"))
                                        {
                                            string[] artistNameLines = value.Split(':');
                                            foreach (string a in artistNameLines)
                                            {
                                                if (!a.Contains("Artist"))
                                                {
                                                    artistName = a.Trim();
                                                }
                                            }
                                        }
                                    }
                                }

                                if (!values.Contains("TitleUnicode"))
                                {
                                    foreach (string value in values)
                                    {
                                        if (value.Contains("Title:"))
                                        {
                                            string[] titleLines = value.Split(':');
                                            foreach (string t in titleLines)
                                            {
                                                if (!t.Contains("Title"))
                                                {
                                                    songTitle = t.Trim();
                                                }
                                            }

                                        }
                                    }
                                }
                            }

                        }

                        FileList.Add(new MediaFile(d.FullName, artistName, songTitle, audioFilename, bgFileName));
                    }
                }
                this.SongListBox.ItemsSource = FileList;

                this.PlayButton.IsEnabled = true;
                this.PauseButton.IsEnabled = true;
            }
            else
            {
                MessageBox.Show("ファイルが見つかりませんでした");
            }
        }

        private void PlayButtonClick(object sender, RoutedEventArgs e)
        {
            var mediaFile = this.SongListBox.SelectedItem as MediaFile;

            string selectedSongAudioPath = mediaFile.FilePath + @"\" + mediaFile.AudioFilename;

            if (this.MediaElement.Source == null || 
                this.MediaElement.Source.LocalPath != selectedSongAudioPath)
            {
                this.MediaElement.Source = new Uri(selectedSongAudioPath);
            }
            this.MediaElement.Play();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            this.MediaElement.Pause();
        }

        private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            this.MediaElement.Stop();
        }

        private void ChangeMediaVolume(object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            this.MediaElement.Volume = (double)this.volumeSlider.Value;
        }
    }
}
