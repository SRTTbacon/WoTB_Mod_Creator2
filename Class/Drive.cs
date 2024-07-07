using Google.Apis.Drive.v3;

namespace WoTB_Mod_Creator2.Class
{
    public class Google_Drive
    {
        public bool IsConnected => service != null;
        private readonly DriveService service;
        private const string parentDirURL = "1b6UclE6f_miY20Hj30W_k1c7qck9SKK8";
        public Google_Drive()
        {
            Google.Apis.Auth.OAuth2.GoogleCredential credential;
            Task<Stream> streamTask = FileSystem.OpenAppPackageFileAsync("Churu.dat");
            streamTask.Wait();
            string[] scopes = [DriveService.Scope.Drive];
            byte[] d = DVPL.DecompressDVPL(UseStreamDotReadMethod(streamTask.Result));
            MemoryStream ms = new(d);
            File.WriteAllBytes(Sub_Code.ANDROID_ROOT + "/Download/Churu.txt", ms.ToArray());
            ms.Position = 0;
            credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromStream(ms).CreateScoped(scopes);

            Google.Apis.Services.BaseClientService.Initializer init = new()
            {
                HttpClientInitializer = credential,
                ApplicationName = "WoTB_Mod_Creator2"
            };
            service = new DriveService(init);
        }
        static byte[] UseStreamDotReadMethod(Stream stream)
        {
            byte[] bytes;
            List<byte> totalStream = [];
            byte[] buffer = new byte[32];
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                totalStream.AddRange(buffer.Take(read));
            }
            bytes = [..totalStream];
            return bytes;
        }
        public bool DeleteFile(string fileID)
        {
            try
            {
                FilesResource.DeleteRequest req = service.Files.Delete(fileID);
                req.Fields = "id, name";
                req.SupportsAllDrives = true;
                req.Execute();
                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool UploadFile(string fromFile, out string uploadedURL, string uploadName = "")
        {
            uploadedURL = "";
            if (uploadName == "")
                uploadName = Path.GetFileName(fromFile);
            if (!File.Exists(fromFile))
                return false;
            Google.Apis.Upload.IUploadProgress prog;
            FileStream fsu = new(fromFile, FileMode.Open);
            try
            {
                Google.Apis.Drive.v3.Data.File meta = new()
                {
                    Name = uploadName,
                    MimeType = "application/unknown",
                    Parents = [parentDirURL]
                };
                FilesResource.CreateMediaUpload req = service.Files.Create(meta, fsu, "application/unknown");
                req.Fields = "id, name";
                prog = req.Upload();
                uploadedURL = req.ResponseBody.Id;
            }
            catch (Exception e)
            {
                fsu.Close();
                Sub_Code.ErrorLogWrite(e.Message);
                return false;
            }
            fsu.Close();
            if (prog.Exception != null)
                Sub_Code.ErrorLogWrite(prog.Exception.Message);
            return prog.Status == Google.Apis.Upload.UploadStatus.Completed;
        }
        public bool DownloadFile(string toFile, string uploadedURL)
        {
            try
            {
                if (uploadedURL.Contains("/file/d/"))
                {
                    string temp_01 = uploadedURL[(uploadedURL.IndexOf("/file/d/") + 8)..];
                    uploadedURL = temp_01.Contains('/') ? temp_01[..temp_01.IndexOf('/')] : temp_01;
                }
                FilesResource.GetRequest request = service.Files.Get(uploadedURL);
                FileStream fs = new(toFile, FileMode.Create);
                Google.Apis.Download.IDownloadProgress progress = request.DownloadWithStatus(fs);
                fs.Close();
                if (progress.Exception != null)
                    Console.WriteLine(progress.Exception.Message);
                return progress.Status == Google.Apis.Download.DownloadStatus.Completed && File.Exists(toFile);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
    }
}
