using System;
using System.IO;
using System.Threading;

namespace TestConsoleApp
{
    class FileObject
    {
        public FileInfo TextFile { get; set; }
        public string Content { get; set; }
        private readonly object lockObject = new object();

        public FileObject()
        {
            TextFile = new FileInfo("test.txt");
            Write("0");
            Content = this.Read();
        }

        public void Write(string s)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                lock(lockObject)
                {
                    File.WriteAllText(TextFile.FullName, s);
                }                
            });            
        }
        public string Read()
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                lock (lockObject)
                {
                    Content = File.ReadAllText(TextFile.FullName);
                }
            });//, (object)Content);
            return Content;
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
        
        private static void OnFileChanged(FileObject file)
        {
            string content = file.Read();
            DateTime lastModTime = File.GetLastWriteTime(file.TextFile.FullName);            
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
