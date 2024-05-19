namespace WoTB_Mod_Creator2.Class
{
    public static partial class DownloadManager
    {
        static HttpClient Client = new();

        public static void UseCustomHttpClient(HttpClient client)
        {
            ArgumentNullException.ThrowIfNull(client);

            Client.Dispose();
            Client = client;
        }

        public static async Task DownloadAsync(string file, string url, IProgress<double>? progress = default, CancellationToken token = default)
        {
            using HttpResponseMessage response = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);

            long total = response.Content.Headers.ContentLength ?? -1L;

            using Stream streamToReadFrom = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
            long totalRead = 0L;
            byte[] buffer = new byte[2048];
            bool isMoreToRead = true;
            FileStream output = new(file, FileMode.Create);
            do
            {
                token.ThrowIfCancellationRequested();

                var read = await streamToReadFrom.ReadAsync(buffer, token);

                if (read == 0)
                    isMoreToRead = false;

                else
                {
                    await output.WriteAsync(buffer.AsMemory(0, read), token);

                    totalRead += read;

                    progress?.Report(totalRead * 1d / (total * 1d) * 100);
                }

            } while (isMoreToRead);

            output.Close();
        }
    }
}
