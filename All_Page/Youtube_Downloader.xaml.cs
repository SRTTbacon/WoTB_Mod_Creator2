using System.Net;
using Un4seen.Bass;
using WoTB_Mod_Creator2.Class;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace WoTB_Mod_Creator2.All_Page;

public partial class Youtube_Downloader : ContentPage
{
    Music_Player? musicPlayer = null;

    Music_Type_List? downloadedMusic = null;
    readonly List<string> addedMusic = [];
    bool bDownloading = false;
    readonly YoutubeClient youtube = new();
    MemoryStream ms = new();
    string downloadingMessage = "";
    int streamHandle = 0;
    bool bMessageShowing = false;
    bool bShowing = false;
    bool bThumGetting = false;

    public Youtube_Downloader()
	{
		InitializeComponent();
        Download_B.Clicked += Download_B_Clicked;
        URL_T.TextChanged += URL_T_TextChanged;
        Back_B.Clicked += Back_B_Clicked;
    }

    public void Init(Music_Player player)
    {
        musicPlayer = player;
    }

    private async void URL_T_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (bThumGetting)
            return;
        bThumGetting = true;
        try
        {
            string id = "";
            if (URL_T.Text.Contains("youtu.be/"))
                id = URL_T.Text.Substring(URL_T.Text.LastIndexOf('/') + 1, 11);
            else if (URL_T.Text.Contains("youtube.com/watch"))
                id = URL_T.Text.Substring(URL_T.Text.IndexOf('=') + 1, 11);
            //サムネイル画像を取得
            if (id.Length == 11)
            {
                var video = await youtube.Videos.GetAsync(id);
                string thumLink = "https://img.youtube.com/vi/" + id + "/hqdefault.jpg";
                if (video.Thumbnails.Count > 0)
                    thumLink = video.Thumbnails[video.Thumbnails.Count - 1].Url;
                Get_Thumbnail(thumLink);
                Thumbnail_Image.Source = ImageSource.FromStream(() => ms);
                Thumbnail_Image.IsVisible = true;
                bThumGetting = false;
            }
        }
        catch
        {
            if (Thumbnail_Image != null)
            {
                Thumbnail_Image.Source = null;
                Thumbnail_Image.IsVisible = false;
            }
        }
    }

    private void Get_Thumbnail(string link)
    {
        ms.Close();
        ms = new MemoryStream();
#pragma warning disable SYSLIB0014 // 型またはメンバーが旧型式です
        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(link);
#pragma warning restore SYSLIB0014 // 型またはメンバーが旧型式です
        req.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows 10)";
        req.Referer = "http://www.ipentec.com/index.html";
        WebResponse res = req.GetResponse();
        Stream st = res.GetResponseStream();
        byte[] buffer = new byte[65535];
        while (true)
        {
            int rb = st.Read(buffer, 0, buffer.Length);
            if (rb > 0)
                ms.Write(buffer, 0, rb);
            else
                break;
        }
        ms.Seek(0, SeekOrigin.Begin);
        st.Close();
    }

    async void Message_Feed_Out(string Message)
    {
        //テキストが一定期間経ったらフェードアウト
        if (bMessageShowing)
        {
            bMessageShowing = false;
            await Task.Delay(1000 / 59);
        }
        Message_T.Text = Message;
        bMessageShowing = true;
        Message_T.Opacity = 1;
        int Number = 0;
        while (Message_T.Opacity > 0 && bMessageShowing)
        {
            Number++;
            if (Number >= 120)
                Message_T.Opacity -= 0.025;
            await Task.Delay(1000 / 60);
        }
        bMessageShowing = false;
        Message_T.Text = "";
        Message_T.Opacity = 1.0;
    }

    async void Downloading()
    {
        while (bDownloading)
        {
            if (downloadingMessage != "")
            {
                Message_T.Text = downloadingMessage;
                if (streamHandle != 0)
                {
                    double percent = (double)Bass.BASS_ChannelGetPosition(streamHandle, BASSMode.BASS_POS_BYTES) / Bass.BASS_ChannelGetLength(streamHandle, BASSMode.BASS_POS_BYTES);
                    Message_T.Text += "\n" + (int)(percent * 100.0) + "%";
                }
            }
            await Task.Delay(50);
        }
        string addedMusicName = "";
        if (downloadedMusic == null)
        {
            Message_Feed_Out("エラーが発生しました。");
            return;
        }
        else if (musicPlayer != null)
        {
            addedMusicName = downloadedMusic.Name_Text;
            musicPlayer.Add_Youtube_Music(downloadedMusic);
            downloadedMusic = null;
        }
        Message_Feed_Out(addedMusicName + "を保存しました。");
    }

    private async void Download_B_Clicked(object? sender, EventArgs e)
    {
        if (bDownloading)
            return;
        bDownloading = true;
        string id = "";
        if (!string.IsNullOrWhiteSpace(URL_T.Text))
        {
            if (URL_T.Text.Contains("youtu.be/"))
                id = URL_T.Text.Substring(URL_T.Text.LastIndexOf('/') + 1, 11);
            else if (URL_T.Text.Contains("youtube.com/watch"))
                id = URL_T.Text.Substring(URL_T.Text.IndexOf('=') + 1, 11);
        }
        if (string.IsNullOrWhiteSpace(id))
        {
            Message_Feed_Out("動画を取得できませんでした。");
            bDownloading = false;
            return;
        }
        if (addedMusic.Contains(id))
        {
            Message_Feed_Out("指定した動画は既に追加されています。");
            bDownloading = false;
            return;
        }
        if (!Directory.Exists(Sub_Code.ExDir + "/Music"))
            Directory.CreateDirectory(Sub_Code.ExDir + "/Music");
        Message_T.Text = "動画を取得しています...";
        await Task.Delay(50);
        Downloading();
        Video_Download(URL_T.Text, id, Sub_Code.ExDir + "/Music/");
    }

    private async void Video_Download(string Link, string linkIDOnly, string outDir)
    {
        await Task.Run(async () =>
        {
            downloadingMessage = "";
            //動画と音声を別々にダウンロード
            //動画の場合はダウンロード後ffmpegで合わせる
            YoutubeExplode.Videos.Video video = await youtube.Videos.GetAsync(Link);
            if (video == null)
            {
                bDownloading = false;
                return;
            }
            string title = Sub_Code.File_Replace_Name(video.Title);
            StreamManifest streamManifest = await youtube.Videos.Streams.GetManifestAsync(linkIDOnly);
            IStreamInfo streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            if (streamInfo != null)
            {
                downloadingMessage = "音声を取得しています...";
                await Task.Delay(50);
                string outFile = outDir + Sub_Code.RandomValue.Next(1000000, 9999999);
                await youtube.Videos.Streams.DownloadAsync(streamInfo, outFile + ".webm");
                downloadingMessage = ".ogg形式へ変換しています...";
                await Task.Delay(50);
                streamHandle = Bass.BASS_StreamCreateFile(outFile + ".webm", 0, 0, BASSFlag.BASS_STREAM_DECODE);
                Un4seen.Bass.AddOn.EncOgg.BassEnc_Ogg.BASS_Encode_OGG_StartFile(streamHandle, null, Un4seen.Bass.AddOn.Enc.BASSEncode.BASS_ENCODE_DEFAULT, outFile + ".ogg");
                await Task.Run(() =>
                {
                    Bass.BASS_ChannelPlay(streamHandle, true);
                    Utils.DecodeAllData(streamHandle, true);
                    streamHandle = 0;
                    bDownloading = false;
                    downloadedMusic = new(outFile + ".ogg", title + ".ogg");
                    downloadingMessage = "";
                    addedMusic.Add(linkIDOnly);
                    File.Delete(outFile + ".webm");
                });
                return;
            }
            bDownloading = false;
        });
    }

    private void Back_B_Clicked(object? sender, EventArgs e)
    {
        if (!bShowing)
            return;

        bShowing = false;
        _ = Navigation.PopAsync();
    }

    private void ContentPage_Disappearing(object sender, EventArgs e)
    {
        Thumbnail_Image.Source = null;
        Thumbnail_Image.IsVisible = false;
        bShowing = false;
    }

    private void ContentPage_Appearing(object sender, EventArgs e)
    {
        bShowing = true;
    }
}
