using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSystemLog
{
    class Program
    {
        public static string       IgnoredFiles          = "ignoredFiles";
        public static List<string> IgnoredFileNames      = new List<string>();      // Синхронизировать доступ к обоим с помощью lock (watchers)
        public static List<string> IgnoredFileStartNames = new List<string>();
        static void Main(string[] args)
        {
            if (!File.Exists(IgnoredFiles))
                File.WriteAllText(IgnoredFiles, "");
            UpdateIgnoredFileNames();

            CreateFileSystemWatchers();

            Console.WriteLine("Logging started to file FileSystem*.log . Press any key to exit");

            Console.ReadKey();
        }

        public static SortedList<string, FileSystemWatcher> watchers = new SortedList<string, FileSystemWatcher>(16);
        public static FileSystemWatcher IgnoredFilesWatcher = null;
        public static long CreateFileSystemWatchers()
        {
            IgnoredFilesWatcher = new FileSystemWatcher(new FileInfo(IgnoredFiles).DirectoryName);
            IgnoredFilesWatcher.Changed += IgnoredFilesWatcher_Changed;
            IgnoredFilesWatcher.EnableRaisingEvents = true;

            var now = DateTime.Now;
            LogFileName = new FileInfo("FileSystem" + now.Year + "-" + now.DayOfYear + "-" + now.Hour + ".log").FullName;

            foreach (var w in watchers)
            {
                w.Value.EnableRaisingEvents = false;
                w.Value.Dispose();
            }

            watchers.Clear();

            long lastDrivesCount = 0;

            var disks = DriveInfo.GetDrives();
            for (int i = 0; i < disks.Length; i++)
            {
                if (!disks[i].IsReady)
                    continue;

                // Не создаём Watcher для съёмных устройств, т.к. он мешает их извлекать
                // if (disks[i].DriveType != DriveType.Removable)
                CreateSystemWatcher(disks[i].RootDirectory.FullName);

                lastDrivesCount++;
            }

            return lastDrivesCount;
        }

        public static void IgnoredFilesWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            lock (watchers)
                UpdateIgnoredFileNames();
        }

        public static void UpdateIgnoredFileNames()
        {
            bool flag = false;
            do
            {
                try
                {
                    IgnoredFileNames     .Clear();
                    IgnoredFileStartNames.Clear();

                    var names = File.ReadAllLines(IgnoredFiles);

                    foreach (var name in names)
                    {
                        if (name.Trim().Length <= 0)
                            continue;
                        if (name.Trim().StartsWith("#"))
                            continue;

                        if (name.StartsWith("::"))
                            IgnoredFileStartNames.Add(name.Substring(2));
                        else
                            IgnoredFileNames.Add(name);
                    }

                    flag = true;
                }
                catch (IOException)
                {
                }
            }
            while (!flag);
        }

        public static void CreateSystemWatcher(string path)
        {
            try
            {
                var watcher = new FileSystemWatcher(path);
                watcher.Changed += A_Changed;
                watcher.Created += A_Changed;
                watcher.Deleted += A_Changed;
                watcher.Renamed += A_Changed;
                watcher.Error   += A_Error;

                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents   = true;

                if (watchers.ContainsKey(path))
                {
                    watchers[path].EnableRaisingEvents = false;
                    watchers[path].Dispose();
                    watchers.Remove(path);
                }

                watchers.Add(path, watcher);
            }
            catch
            {
            }
        }

        public static void A_Error(object sender, ErrorEventArgs e)
        {
        }

        public static string lastChangedFileName = "";
        public static long   lastChangedDate = 0;
        public static void A_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath == LogFileName)
                return;
                
            var fileName = e.FullPath;

            lock (watchers)
            foreach (var fn in IgnoredFileNames)
            {
                if (fn == fileName)
                    return;
            }

            lock (watchers)
            foreach (var fn in IgnoredFileStartNames)
            {
                if (fileName.StartsWith(fn))
                    return;
            }

            var sb = new StringBuilder();
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    
                    sb.Append("created");
                    break;

                case WatcherChangeTypes.Changed:
                    
                    // Защита от слишком большого количества логирования одного и того же файла (если логи подряд идут)
                    if (e.FullPath == lastChangedFileName)
                    if ((DateTime.Now.Ticks - lastChangedDate) / 10000 / 1000 < 15)     // 15 секунд
                        return;

                    sb.Append("Changed");

                    lastChangedFileName = e.FullPath;
                    lastChangedDate     = DateTime.Now.Ticks;
                    break;


                case WatcherChangeTypes.Deleted:
                    
                    sb.Append("Deleted");
                    break;


                case WatcherChangeTypes.Renamed:
                    
                    sb.Append("Renamed");
                    break;


                default:
                    
                    sb.Append("unknown operation");
                    break;

            }

            sb.AppendLine(" " + fileName);

            Log(sb.ToString());
        }

        public static string LogFileName = null;
        public static void Log(string toLog)
        {
            lock (watchers)
                File.AppendAllText(LogFileName, DateTime.Now.ToString("r") + ":\r\n" + toLog + "\r\n\r\n");
        }
    }
}
