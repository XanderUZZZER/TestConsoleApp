using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
                DateTime currentModTime = file.TextFile.LastWriteTime;
                while (true)
                {
                    using (StreamReader reader = new StreamReader(file.TextFile.FullName))
                    {
                        if (currentModTime != file.TextFile.LastWriteTime)
                        {
                            currentModTime = file.TextFile.LastWriteTime;
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

        private static void OnFileChanged(FileObject file)
        {
            string content;
            DateTime lastModTime = file.TextFile.LastWriteTime;
            using (StreamReader reader = new StreamReader(file.TextFile.FullName))
            {
                content = reader.ReadLine();
            }
            Console.WriteLine($"File content: {content}\nFile changed: {lastModTime}/n");

            if (content == "1")
            {
                file.Write("0");
                Thread.Sleep(10000);
                Console.WriteLine("File content changed to 0 ten seconds ago");
            }
        }
    }

}
