using System;
using System.IO;
using System.Threading;

namespace TestConsoleApp
{
    class FileObject
    {
        public FileInfo TextFile { get; set; }

        public FileObject()
        {
            TextFile = new FileInfo("test.txt");
            //FileStream fs = TextFile.Create();
            //fs.Close();            
            Write("0");
        }

        public void Write(string s)
        {
            File.WriteAllText(TextFile.FullName, s);
        }
    }
    class FileWatcher
    {
        FileObject file;
        public event Action<FileObject> FileChanged;

        public FileWatcher(FileObject file)
        {
            this.file = file;
        }
        private readonly object lockObject = new object();
        public void Start()
        {
            Thread t = new Thread(() =>
            {
                DateTime currentModTime = File.GetLastWriteTime(file.TextFile.FullName);
                while (true)
                {
                    DateTime latestModTime = File.GetLastWriteTime(file.TextFile.FullName);
                    if (currentModTime != latestModTime)
                    {
                        currentModTime = latestModTime;
                        ThreadPool.QueueUserWorkItem(o =>
                           {
                               lock (lockObject)
                               {
                                   FileChanged?.Invoke(file);
                               }
                           });
                    }

                }
            });
            t.Start();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            FileObject fileObject = new FileObject();
            FileWatcher watcher = new FileWatcher(fileObject);
            watcher.FileChanged += OnFileChanged;
            watcher.Start();

            while (true)
            {
                Console.WriteLine("Do you want to change file content to 1? y/n");
                if (Console.ReadLine() == "y")
                {
                    fileObject.Write("1");
                }
            }
            Console.ReadLine();
        }
        private static readonly object lockObject1 = new object();
        private static void OnFileChanged(FileObject file)
        {
            DateTime lastModTime = File.GetLastWriteTime(file.TextFile.FullName);
            string content = File.ReadAllText(file.TextFile.FullName);
            Console.WriteLine($"\tFile content: {content}\n\tFile changed: {lastModTime}");

            if (content == "1")
            {
                file.Write("0");
                Thread.Sleep(4000);
                Console.WriteLine("\t\tFile content changed to 0 ten seconds ago");
            }
        }
    }
}
