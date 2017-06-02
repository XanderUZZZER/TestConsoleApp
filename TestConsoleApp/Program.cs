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
            Write("0");
        }

        private static readonly object lockObject = new object();
        public void Write(string s)
        {
            ThreadPool.QueueUserWorkItem(o =>
               {
                   lock (lockObject)
                   {
                       using (StreamWriter writer = new StreamWriter(TextFile.FullName))
                       {
                           writer.Write(s);
                       }
                   }
               });
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
        private static readonly object lockObject = new object();
        public void Start()
        {
            Thread t = new Thread(() =>
            {
                DateTime currentModTime = File.GetLastWriteTime(file.TextFile.FullName);
                while (true)
                {
                    lock (lockObject)
                    {
                        if (currentModTime != File.GetLastWriteTime(file.TextFile.FullName))
                        {
                            currentModTime = File.GetLastWriteTime(file.TextFile.FullName);
                            FileChanged?.Invoke(file);
                        }
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
        private static readonly object lockObject2 = new object();
        private static void OnFileChanged(FileObject file)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                lock (lockObject1)
                {
                    string content;
                    DateTime lastModTime = File.GetLastWriteTime(file.TextFile.FullName);
                    using (StreamReader reader = new StreamReader(file.TextFile.FullName))
                    {
                        content = reader.ReadLine();
                    }
                    Console.WriteLine($"File content: {content}\nFile changed: {lastModTime}");

                    if (content == "1")
                    {
                        file.Write("0");
                        ThreadPool.QueueUserWorkItem(s =>
                            {
                                lock (lockObject2)
                                {
                                    Thread.Sleep(10000);
                                    Console.WriteLine("File content changed to 0 ten seconds ago");
                                }
                            });
                    }
                }
            });
        }
    }
}
