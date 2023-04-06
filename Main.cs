using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using SharpCompress.Common;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.Zip;
using SharpCompress.Archives.SevenZip;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.WindowsAPICodePack.Dialogs;
using CodeWalker.GameFiles;
using CodeWalker.Utils;
using Sentry;

namespace rpf2fivem
{

    public partial class Main : Form
    {

    // GLOBALS

    int currentQueue = 1;
        Random rnd = new Random();
        bool vmenuhelper = true;
        bool servercfghelper = true;
        bool combiner = false;
        int convertFromFolder_resname;
        static string latestModelName = "";

        static Dictionary<string, string[]> extensions = new Dictionary<string, string[]>()
        {
            { "meta",  new string[]{ ".meta", "clip_sets.xml" } },
            { "stream", new string[]{".ytd", ".yft", ".ydr" } }
        };

        static Dictionary<string, string> modelNames = new Dictionary<string, string>();

        struct StructureFolders
        {
            public string streamFolder;
            public string dataFolder;
        }

        public Main()
        {
            InitializeComponent();
            if (!Directory.Exists("./logs"))
            {
                Directory.CreateDirectory("logs");
            }
            if (!File.Exists(@"./logs/latest.log"))
            {
                FileStream fs = File.Create(@"./logs/latest.log");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.ActiveControl = label1; // prevent random textbox focus
            fivemresname_tb.Text = rnd.Next(2147483647).ToString();

            LogAppend("rpf2fivem");
            LogAppend("---------------");
            LogAppend("Developed by Avenzey#6184 (thanks to https://github.com/vscorpio for developing the original version!)");
            LogAppend("GitHub repository: https://github.com/Avenze/rpf2fivem-repository");
            LogAppend("Discord support: https://discord.gg/C4e4q6g");
            LogAppend("Patreon: https://patreon.com/avenzey");
            LogAppend("---------------");

            LogAppend("GTA5-Mods links must look like this: ");
            LogAppend("https://files.gta5-mods.com/uploads/XXXCARNAMEXXXX/XXXCARNAMEXXXX.zip");
            LogAppend("Links must be DIRECT link else they won't download!");

            if (!Directory.Exists("./NConvert"))
            {
                // add warning if the user hasn't installed NConvert properly
                WarningAppend("[NConvert] It seems like you haven't installed NConvert, please follow");
                WarningAppend("[NConvert] the installation instructions on the GitHub Repository or the forum thread.");

                CompressCheck.Checked = false;
                CompressCheck.Enabled = false;
            }

            SentrySdk.CaptureMessage("Hello Sentry");
        }

        // Helper Functions

        public void LogAppend(string text)
        {
            log.AppendText(text + Environment.NewLine);
            StatusHandler(text);
            LogFile("[INFO] " + text);
        }

        public void WarningAppend(string text)
        {
            log.AppendText(text + Environment.NewLine);
            StatusHandler(text);
            LogFile("[WARNING] " + text);
        }

        public void ErrorAppend(string text)
        {
            log.AppendText("[Error] An error occoured during execution, stacktrace has been logged to /logs/latest.log, please submit to GitHub Issues page.");
            LogFile("[ERROR] " + text);
        }

        public void LogFile(string text)
        {
            try
            {
                string currentDate = DateTime.Now.ToString(@"MM\/dd\/yyyy\ hh\:mm\:ss");
                using (TextWriter tw = new StreamWriter(@"./logs/latest.log", append: true))
                {
                    tw.WriteLine("[" + currentDate + "] " + text + Environment.NewLine);
                    tw.Close();
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                ErrorAppend("[Worker] Failed to write log to file. Stacktrace: " + ex);
            }
        }

        private void StatusHandler(string status)
        {
            tsStatus.Text = "Status: " + status;
        }

        private void QueueHandler(int current, int total)
        {
            tsQueue.Text = "Queue: " + current + "/" + total;
        }

        private void ShellCmd(string cmd)
        {
            string strCmdText = "/K " + cmd;
            System.Diagnostics.Process.Start("CMD.exe", strCmdText);
        }


        async Task AsyncFileDownload(string url)
        {
            string file = System.IO.Path.GetFileName(url);
            WebClient wb = new WebClient();
            await wb.DownloadFileTaskAsync(new Uri(url), file);
        }

        private void HideShellCmd(string cmd)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C " + cmd;
            process.StartInfo = startInfo;
            process.Start();
        }

        // Helper Functions
        private void VmenuHelper(string ytdcarname)
        {
            if (vmenuhelper && servercfghelper)
            {
                using (StreamWriter w = File.AppendText("vmenu.txt"))
                {
                    w.WriteLine("        " + '"' + ytdcarname + '"' + ",");
                }
            }

        }

        private void CfgHelper(string servercfg)
        {
            if (vmenuhelper && servercfghelper)
            {
                using (StreamWriter w = File.AppendText("servercfg.txt"))
                {
                    w.WriteLine("ensure " + servercfg);
                }
            }
        }

        // SharpCompress Functions
        private void unZip(string target)
        {
            using (var archive = ZipArchive.Open(target))
            {
                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                {
                    entry.WriteToDirectory("cache\\unpack", new ExtractionOptions()
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }
            }
        }

        private void unRar(string target)
        {
            using (var archive = RarArchive.Open(target))
            {
                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                {
                    entry.WriteToDirectory("cache\\unpack", new ExtractionOptions()
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }
            }
        }

        private void unSeven(string target)
        {

            using (var archive = SevenZipArchive.Open(target))
            {
                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                {
                    entry.WriteToDirectory("cache\\unpack", new ExtractionOptions()
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }
            }

        }

        // Decompression Functions
        private void universalCacheUnpack()
        {
            string rarfileExtension = "*.rar";
            string[] rarFiles = Directory.GetFiles("cache", rarfileExtension, SearchOption.AllDirectories);

            foreach (var item in rarFiles)
            {
                LogAppend("[SharpCompress] Found .RAR archive, decompressing...");
                unRar(Path.Combine("cache", Path.GetFileName(item)));
            }

            string zipfileExtension = "*.zip";
            string[] zipFiles = Directory.GetFiles("cache", zipfileExtension, SearchOption.AllDirectories);

            foreach (var item in zipFiles)
            {
                LogAppend("[SharpCompress] Found .ZIP archive, decompressing...");
                unZip(Path.Combine("cache", Path.GetFileName(item)));
            }

            string sevenfileExtension = "*.7z";
            string[] sevenFiles = Directory.GetFiles("cache", sevenfileExtension, SearchOption.AllDirectories);

            foreach (var item in sevenFiles)
            {
                LogAppend("[SharpCompress] Found .7Z archive, decompressing...");
                unSeven(Path.Combine("cache", Path.GetFileName(item)));
            }

            return;
        }

        // Unpacking Functions
        private void RpfUnpack(string resname, string rpfFile)
        {
            if (rpfFile == "")
            {
                string rpfExtension = "*.rpf";
                string[] rpfFiles = Directory.GetFiles("cache", rpfExtension, SearchOption.AllDirectories);
                foreach (var item in rpfFiles)
                {
                    RpfFile rpf = new RpfFile(item, item);
                    LogAppend("[CodeWalker] Unpacking " + item + "...");

                    if (rpf.ScanStructure(null, null))
                    {
                        ExtractFilesInRPF(rpf, @".\cache\rpfunpack\");
                    }
                }

                if (rpfFiles.Length == 0)
                {
                    WarningAppend("[CodeWalker] Vehicle (" + resname + ") is incompatible, no .rpf file was found.");
                }
            }
            else
            {
                RpfFile rpf = new RpfFile(rpfFile, rpfFile);
                LogAppend("[CodeWalker] Unpacking " + rpfFile + "...");

                if (rpf.ScanStructure(null, null))
                {
                    ExtractFilesInRPF(rpf, @".\cache\rpfunpack\");
                }
            }
        }

        private void ExtractFilesInRPF(RpfFile rpf, string directoryOffset)
        {
            using (BinaryReader br = new BinaryReader(File.OpenRead(rpf.GetPhysicalFilePath())))
            {
                foreach (RpfEntry entry in rpf.AllEntries)
                {
                    if (!entry.NameLower.EndsWith(".rpf")) //don't try to extract rpf's, they will be done separately..
                    {
                        if (entry is RpfBinaryFileEntry)
                        {
                            RpfBinaryFileEntry binentry = entry as RpfBinaryFileEntry;
                            byte[] data = rpf.ExtractFileBinary(binentry, br);
                            if (data == null)
                            {
                                if (binentry.FileSize == 0)
                                {
                                    LogAppend("[CodeWalker] Invalid binary filesize!");
                                }
                                else
                                {
                                    LogAppend("[CodeWalker] Binary data is null");
                                }
                            }
                            else if (data.Length == 0)
                            {
                                LogAppend("[CodeWalker] Decompressed output " + entry.Path + " was empty!");
                            }
                            else
                            {
                                File.WriteAllBytes(directoryOffset + entry.NameLower, data);
                            }
                        }
                        else if (entry is RpfResourceFileEntry)
                        {
                            RpfResourceFileEntry resentry = entry as RpfResourceFileEntry;
                            byte[] data = rpf.ExtractFileResource(resentry, br);
                            data = ResourceBuilder.Compress(data); //not completely ideal to recompress it...
                            data = ResourceBuilder.AddResourceHeader(resentry, data);
                            if (data == null)
                            {
                                if (resentry.FileSize == 0)
                                {
                                    LogAppend("[CodeWalker] Resource (" + entry.Path + ") filesize was empty!");
                                }
                            }
                            else if (data.Length == 0)
                            {
                                LogAppend("[CodeWalker] Decompressed output (" + entry.Path + ") was empty!");
                            }
                            else
                            {
                                foreach (KeyValuePair<string, string[]> extensionMap in extensions)
                                {
                                    foreach (string extension in extensionMap.Value)
                                    {
                                        if (entry.NameLower.EndsWith(extension))
                                        {

                                            if (extension.Equals(".ytd"))
                                            {
                                                if (CompressCheck.Checked == true)
                                                {
                                                    RpfFileEntry rpfentry = entry as RpfFileEntry;

                                                    byte[] ytddata = rpfentry.File.ExtractFile(rpfentry);

                                                    YtdFile ytd = new YtdFile();
                                                    ytd.Load(ytddata, rpfentry);

                                                    Dictionary<uint, Texture> Dicts = new Dictionary<uint, Texture>();

                                                    bool somethingResized = false;
                                                    foreach (KeyValuePair<uint, Texture> texture in ytd.TextureDict.Dict)
                                                    {
                                                        if (texture.Value.Width > 512) // Only resize if it is greater than 1440p
                                                        {
                                                            byte[] dds = DDSIO.GetDDSFile(texture.Value);
                                                            File.WriteAllBytes("./NConvert/" + texture.Value.Name + ".dds", dds);

                                                            //HideShellCmd(@"./library/nconvert/nconvert.exe -out dds -resize 50% 50% -overwrite ./cache/images/" + texture.Value.Name + ".dds");

                                                            Process p = new Process();
                                                            p.StartInfo.FileName = @"./NConvert/nconvert.exe";
                                                            p.StartInfo.Arguments = $"-out dds -resize 50% 50% -overwrite ./NConvert/{texture.Value.Name}.dds";
                                                            p.StartInfo.UseShellExecute = false;
                                                            p.StartInfo.CreateNoWindow = true;
                                                            p.StartInfo.RedirectStandardOutput = true;
                                                            p.Start();

                                                            //p.WaitForExit();

                                                            LogAppend("[NConvert] " + p.StandardOutput.ReadToEnd());

                                                            LogAppend("[NConvert] Sucessfully resized texture (" + texture.Value.Name + ") to 50%!");
                                                            File.Move("./NConvert/" + texture.Value.Name + ".dds", directoryOffset + texture.Value.Name + ".dds");

                                                            byte[] resizedData = File.ReadAllBytes(directoryOffset + texture.Value.Name + ".dds");
                                                            Texture resizedTex = DDSIO.GetTexture(resizedData);
                                                            resizedTex.Name = texture.Value.Name;
                                                            Dicts.Add(texture.Key, resizedTex);

                                                            File.Delete(directoryOffset + texture.Value.Name + ".dds");
                                                            somethingResized = true;
                                                        }
                                                        else
                                                        {
                                                            Dicts.Add(texture.Key, texture.Value);
                                                        }
                                                    }

                                                    if (!somethingResized)
                                                    {
                                                        LogAppend("[CodeWalker] No textures were resized, skipping .ytd recreation.");
                                                        break;
                                                    }

                                                    TextureDictionary dictionary = new TextureDictionary();
                                                    dictionary.Textures = new ResourcePointerList64<Texture>();
                                                    dictionary.TextureNameHashes = new ResourceSimpleList64_uint();
                                                    dictionary.Textures.data_items = Dicts.Values.ToArray();
                                                    dictionary.TextureNameHashes.data_items = Dicts.Keys.ToArray();

                                                    dictionary.BuildDict();
                                                    ytd.TextureDict = dictionary;

                                                    byte[] resizedYtdData = ytd.Save();
                                                    File.WriteAllBytes(directoryOffset + entry.NameLower, resizedYtdData);

                                                    LogAppend("[CodeWalker] Resized texture dictionary (ytd) " + entry.NameLower + ".");
                                                    break;
                                                }
                                            }

                                            File.WriteAllBytes(directoryOffset + entry.NameLower, data);
                                            break;
                                        }
                                    }
                                }

                                if (entry.NameLower.EndsWith(".ytd"))
                                {
                                    latestModelName = entry.NameLower.Remove(entry.NameLower.Length - 4);
                                }
                            }
                        }
                    }
                    else
                    {
                        RpfBinaryFileEntry binaryentry = entry as RpfBinaryFileEntry;
                        byte[] data = rpf.ExtractFileBinary(binaryentry, br);
                        File.WriteAllBytes(directoryOffset + entry.NameLower, data);

                        RpfFile subRPF = new RpfFile(directoryOffset + entry.NameLower, directoryOffset + entry.NameLower);

                        if (subRPF.ScanStructure(null, null))
                        {
                            ExtractFilesInRPF(subRPF, directoryOffset);
                        }
                        File.Delete(directoryOffset + entry.NameLower);
                    }
                }
            }

        }

        // Cleanup Functions
        private void fixTextureFile(string filePath)
        {
            LogAppend("[Worker] Fixing " + filePath + "...");
            string content = File.ReadAllText(filePath, Encoding.Default);
            char[] array = content.ToCharArray();
            array[3] = '7';
            content = new string(array);
            File.WriteAllText(filePath, content, Encoding.Default);
        }

        private void InflateResourceFolder(string streamFolder, string dataFolder, string type, bool isYtd, bool isYtf, bool combined)
        {
            //Assume user types .txt into textbox
            string fileExtension = "*." + type;
            string[] txtFiles = Directory.GetFiles("cache", fileExtension, SearchOption.AllDirectories);

            foreach (var item in txtFiles)
            {
                LogAppend("[Worker] Inflating " + item);
                if (isYtd)
                {
                    fixTextureFile(item);
                    File.Move(item, Path.Combine(streamFolder, Path.GetFileName(item))); // put into stream folder inside resource name
                    VmenuHelper(Path.GetFileName(item));

                }
                else if (isYtf)
                {
                    fixTextureFile(item);
                    File.Move(item, Path.Combine(streamFolder, Path.GetFileName(item))); // put into stream folder inside resource name
                }
                else
                {
                    File.Move(item, Path.Combine(dataFolder, Path.GetFileName(item)));
                }
            }
        }

        private void RemoveUnnessecary(string type)
        {
            string fileExtension = "*." + type;
            string[] txtFiles = Directory.GetFiles("cache", fileExtension, SearchOption.AllDirectories);
            foreach (var item in txtFiles)
            {
                LogAppend("[Worker] Deleting / " + item + " ...");
                File.Delete(item);
            }
        }

        private void cleanUp()
        {
            try
            {
                Directory.Delete("cache", true);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
            }
            fivemresname_tb.Text = rnd.Next(2147483647).ToString();
            StatusHandler("Idle");
        }

        // Setup functions
        string fxmanifest_single = Properties.Resources.fxmanifest_false;
        string fxmanifest_combined = Properties.Resources.fxmanifest_true;

        private void SetupBasicEnviroment()
        {
            LogAppend("[Worker] Setting up basic enviroment...");
            if (!Directory.Exists("./cache"))
            {
                Directory.CreateDirectory("cache");
                Directory.CreateDirectory(@"cache\unpack");
                Directory.CreateDirectory(@"cache\rpfunpack");
                Directory.CreateDirectory(@"cache\structure");
                Directory.CreateDirectory(@"cache\data");
            }
            LogAppend("[Worker] Created /cache directory.");

            if (!Directory.Exists("./resources"))
            {
                Directory.CreateDirectory("resources");
            }
            LogAppend("[Worker] Created /resources directory.");
        }

        private void SetupStructureFolders(string folderName, string combinedFolderName, bool combinedEnv)
        {

            Encoding utf8WithoutBom = new UTF8Encoding(false);
            string directory = "";
            string fxmanifest = "";

            if (combinedEnv == true)
            {
                directory = @"./cache/structure/" + combinedFolderName;
                fxmanifest = fxmanifest_combined;
            }
            else
            {
                directory = @"./cache/structure/" + folderName;
                fxmanifest = fxmanifest_single;
            }

            Directory.CreateDirectory(directory);
            Directory.CreateDirectory(directory + "/stream/");
            Directory.CreateDirectory(directory + "/data/");
            File.WriteAllText(directory + @"\fxmanifest.lua", fxmanifest, utf8WithoutBom);

            LogAppend("[Worker] Created resource folder structure.");
        }

        private StructureFolders CreateDataFolders(string folderName, string combinedFolderName, bool combinedEnv)
        {
            StructureFolders structure;

            if (combinedEnv == true)
            {
                string directory = @"./cache/structure/" + combinedFolderName;

                Directory.CreateDirectory(directory + "/stream/" + folderName); // refer to the folder structure :/
                Directory.CreateDirectory(directory + "/data/" + folderName);

                structure.streamFolder = directory + @"/stream/" + folderName;
                structure.dataFolder = directory + @"/data/" + folderName;
            }
            else
            {
                string directory = @"./cache/structure/" + folderName;

                structure.streamFolder = directory + "/stream/";
                structure.dataFolder = directory + "/data/";
            }

            return structure;
        }

        // Conversion Functions
        public async Task startConversion(bool folder, string resource, string saferesource)
        {
            Encoding utf8WithoutBom = new UTF8Encoding(false);
            Random random = new Random();
            Regex regex = new Regex(@"<(.*?)>");

            LogAppend("[Worker] Start conversion process...");

            if (folder == false)
            {

                tsBar.Maximum = queueList.Items.Count;
                tsBar.Value = 0;

                foreach (string CurrentItem in queueList.Items)
                {

                    LogAppend("[Worker] Setting up basic enviroment...");
                    SetupBasicEnviroment();

                    QueueHandler(currentQueue, queueList.Items.Count);
                    tsBar.Value++;

                    string filteredresname = "";
                    string SingleEnviromentFolder = regex.Match(CurrentItem).Groups[1].Value;
                    string CombinedEnviromentFolder = rnd.Next(2147483647).ToString();

                    string StreamFolder = "";
                    string DataFolder = "";
                    var StopwatchTimer = Stopwatch.StartNew();

                    LogAppend("[Worker] Setting up resource folder structure...");
                    SetupStructureFolders(SingleEnviromentFolder, CombinedEnviromentFolder, combiner);

                    LogAppend("[Worker] Fetching resource stream and data folders...");
                    var StructureFolders = CreateDataFolders(SingleEnviromentFolder, CombinedEnviromentFolder, combiner);
                    StreamFolder = StructureFolders.streamFolder;
                    DataFolder = StructureFolders.dataFolder;

                    LogAppend("[Worker] Downloading archive...");
                    if (CurrentItem != "")
                    {
                        await AsyncFileDownload(CurrentItem.Replace($"<{SingleEnviromentFolder}>", ""));
                    }

                    LogAppend("[Worker] Moving archives to cache...");
                    HideShellCmd(@"move *.rar cache");
                    HideShellCmd(@"move *.zip cache");
                    HideShellCmd(@"move *.7z cache");

                    LogAppend("[SharpCompress] Decompressing...");
                    await Task.Delay(500);
                    universalCacheUnpack();
                    await Task.Delay(2500);

                    LogAppend("[Worker] Removing leftover files from the archive...");
                    RemoveUnnessecary("yft");
                    RemoveUnnessecary("ytd");
                    RemoveUnnessecary("meta");

                    LogAppend("[CodeWalker] Searching for dlc.rpf...");
                    try
                    {
                        RpfUnpack(filteredresname, "");
                    }
                    catch (Exception ex)
                    {
                        SentrySdk.CaptureException(ex);
                        ErrorAppend("[CodeWalker] Failed to extract dlc.rpf, stack trace: " + ex);
                    }

                    LogAppend("[Worker] Moving items from cache to resource folder."); // Clean cache from unused files, such as fragments and texture dictionaries.
                    await Task.Delay(5000);
                    InflateResourceFolder(StreamFolder, DataFolder, "meta", false, false, false);
                    InflateResourceFolder(StreamFolder, DataFolder, "yft", false, true, false);
                    InflateResourceFolder(StreamFolder, DataFolder, "ytd", true, false, false);

                    LogAppend("[Worker] Moving resource folder to /resources...");
                    string VehicleResourceName = "";
                    if (combiner == true)
                    {
                        Directory.Move(@"./cache/structure/" + CombinedEnviromentFolder, @"./resources/" + CombinedEnviromentFolder);
                        VehicleResourceName = CombinedEnviromentFolder;
                    }
                    else
                    {
                        Directory.Move(@"./cache/structure/" + SingleEnviromentFolder, @"./resources/" + SingleEnviromentFolder);
                        VehicleResourceName = SingleEnviromentFolder;
                    }

                    CfgHelper(VehicleResourceName);

                    LogAppend("[Worker] Conversion of vehicle " + VehicleResourceName + " has finished, cleaning up...");
                    currentQueue += 1;
                    cleanUp();
                    StopwatchTimer.Stop();
                    jobTime.Text = "| Last job took: " + StopwatchTimer.ElapsedMilliseconds + " ms";
                }
            }
            else
            {
                QueueHandler(currentQueue, queueList.Items.Count);
                tsBar.Value++;

                string filteredresname = "";
                string SingleEnviromentFolder = rnd.Next(2147483647).ToString();

                string StreamFolder = "";
                string DataFolder = "";
                var StopwatchTimer = Stopwatch.StartNew();

                LogAppend("[Worker] Moving selected archive to cache...");
                File.Move(resource, @"./cache/" + saferesource);

                LogAppend("[Worker] Setting up resource folder structure...");
                SetupStructureFolders(SingleEnviromentFolder, "", false);

                LogAppend("[Worker] Fetching resource stream and data folders...");
                var StructureFolders = CreateDataFolders(SingleEnviromentFolder, "", false);
                StreamFolder = StructureFolders.streamFolder;
                DataFolder = StructureFolders.dataFolder;

                LogAppend("[SharpCompress] Decompressing...");
                await Task.Delay(500);
                universalCacheUnpack();
                await Task.Delay(2500);

                LogAppend("[Worker] Removing leftover files from the archive...");
                RemoveUnnessecary("yft");
                RemoveUnnessecary("ytd");
                RemoveUnnessecary("meta");

                LogAppend("[CodeWalker] Searching for dlc.rpf...");
                try
                {
                    RpfUnpack(filteredresname, "");
                }
                catch (Exception ex)
                {
                    SentrySdk.CaptureException(ex);
                    ErrorAppend("[CodeWalker] Failed to extract dlc.rpf, stack trace: " + ex);
                }

                LogAppend("[Worker] Moving items from cache to resource folder."); // Clean cache from unused files, such as fragments and texture dictionaries.
                await Task.Delay(5000);
                InflateResourceFolder(StreamFolder, DataFolder, "meta", false, false, false);
                InflateResourceFolder(StreamFolder, DataFolder, "yft", false, true, false);
                InflateResourceFolder(StreamFolder, DataFolder, "ytd", true, false, false);

                LogAppend("[Worker] Moving converted resource into /resources folder.");
                Directory.Move(@"./cache/structure/" + SingleEnviromentFolder, @"./resources/" + SingleEnviromentFolder);

                CfgHelper(SingleEnviromentFolder);

                LogAppend("[Worker] Conversion of vehicle " + SingleEnviromentFolder + " has finished, cleaning up...");
                currentQueue += 1;
                cleanUp();
                StopwatchTimer.Stop();
                jobTime.Text = "| Last job took: " + StopwatchTimer.ElapsedMilliseconds + " ms";
            }
        }

        // Events

        private void checkBox1_CheckedChanged_1(object sender, EventArgs e)
        {
            if (CompressCheck.Checked == true)
            {
                LogAppend("[InputHandler] Enabled texture compression/downsizing.");
            }
            else
            {
                LogAppend("[InputHandler] Disabled texture compression/downsizing.");
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (VmenuCheck.Checked == true)
            {
                servercfghelper = true;
                vmenuhelper = true;
                LogAppend("[InputHandler] Config Helpers switched on");
            }
            else
            {
                servercfghelper = false;
                vmenuhelper = false;
                LogAppend("[InputHandler] Config Helpers switched off");
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
        }

        private async void button2_Click_1(object sender, EventArgs e)
        {
            Random rnd = new Random();
            convertFromFolder_resname = rnd.Next(555555);

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "7z|*.7z|ZIP|*.zip|RAR|*.rar";
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var filePath = openFileDialog.FileName;
                    var safeFileName = openFileDialog.SafeFileName;
                    LogAppend("[Worker] Converting resource at " + filePath);
                    startConversion(true, filePath, safeFileName);

                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            queueList.Items.Clear();
            btnStart.Enabled = false;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            for (int i = 1; i <= 3; i++)
            {
                queueList.Items.Add("https://files.gta5-mods.com/uploads/1998-audi-s8-d2-us-6spd-add-on-replace-tuning-extras/15b8b3-1998%20Audi%20S8%20(D2)%20-%20v1.1.zip");
            }
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            startConversion(false, "", "");
        }

        private void btnAddQueue_Click(object sender, EventArgs e)
        {
            LogAppend("Job added!");
            queueList.Items.Add($"<{fivemresname_tb.Text}>" + textBox1.Text);
            btnStart.Enabled = true;
            QueueHandler(0, queueList.Items.Count);
            textBox1.Clear();
            this.ActiveControl = label1;
            fivemresname_tb.Text = rnd.Next(2147483647).ToString();
            textBox1.Text = "https://files.gta5-mods.com/uploads/XXXCARNAMEXXXX/XXXCARNAMEXXXX.zip";
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text.Contains("https://files.gta5-mods.com/") && !textBox1.Text.Contains("XXXCARNAMEXXXX"))
            {
                gta5mods_status.ForeColor = Color.Green;
                gta5mods_status.Text = "OK";
                btnAddQueue.Enabled = true;

            }
            else
            {
                gta5mods_status.ForeColor = Color.Red;
                gta5mods_status.Text = "ERROR";
                btnAddQueue.Enabled = false;

            }
        }

        private void reslua_TextChanged(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged_2(object sender, EventArgs e)
        {
            if (CombineCheck.Checked == true)
            {
                combiner = true;
                LogAppend("[InputHandler] Combine Helpers switched on");
            }
            else
            {
                combiner = false;
                LogAppend("[InputHandler] Combine Helpers switched off");
            }
        }
    }
}

