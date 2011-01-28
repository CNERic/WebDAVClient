using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using net.kvdb.webdav;

namespace TestWebDAVClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Debug.Listeners.Add(new ConsoleTraceListener());
            Boolean result;
            WebDAVClient c;

            // A private mod_dav WebDAV server running on Apache (All tests pass)
            // Basic authentication required
            c = new WebDAVClient();
            c.User = "iamedu";
            c.Pass = "iamedu";
            c.Server = "http://192.168.1.55:8080";
            c.BasePath = "/jnstorage/webdav/repo1/javanes/iamedu/";
            result = RunWebDAVTests(c);

            if (!result)
            {
                Debug.WriteLine("Tests didn't pass");
            }
            else
            {
                Debug.WriteLine("All passed");
            }
            Console.ReadLine();
        }

        static AutoResetEvent autoResetEvent;

        static Boolean RunWebDAVTests(WebDAVClient c)
        {
            autoResetEvent = new AutoResetEvent(false);

            // Generate unique string to test with.
            string basepath = Path.GetRandomFileName() + '/';
            string copypath = basepath + "copy/";
            string tempFilePath = Path.GetTempFileName();
            string uploadTestFilePath = @"c:\windows\notepad.exe";
            //string uploadTestFilePath = @"c:\windows\explorer.exe";
            // string uploadTestFilePath = @"c:\windows\setuplog.txt";
            string uploadTestFileName = Path.GetFileName(uploadTestFilePath);

            c.CreateDirComplete += new CreateDirCompleteDel(c_CreateDirComplete);
            c.CreateDir(basepath);
            autoResetEvent.WaitOne();
            c.CreateDir(copypath);
            autoResetEvent.WaitOne();
            Debug.WriteLine("CreateDir passed");

            c.ListComplete += new ListCompleteDel(c_ListComplete);
            c.List(basepath);
            autoResetEvent.WaitOne();
            if (_files.Count != 1) { return false; }
            Debug.WriteLine("List passed");

            c.UploadComplete += new UploadCompleteDel(c_UploadComplete);
            c.Upload(uploadTestFilePath, basepath + uploadTestFileName);
            autoResetEvent.WaitOne();
            Debug.WriteLine("Upload 1/2 passed");
            c.List(basepath);
            autoResetEvent.WaitOne();
            if (_files.Count != 2) { return false; }
            Debug.WriteLine("Upload 2/2 passed");

            autoResetEvent = new AutoResetEvent(false);
            c.DownloadComplete += new DownloadCompleteDel(c_DownloadComplete);
            c.Download(basepath + uploadTestFileName, tempFilePath);
            autoResetEvent.WaitOne();
            c.Download(basepath, @"c:\Users\iamedu\test");
            autoResetEvent.WaitOne();
            Debug.WriteLine("Download 1/2 passed");
            HashAlgorithm h = HashAlgorithm.Create("SHA1");
            byte[] localhash;
            byte[] remotehash;
            using (FileStream fs = new FileStream(uploadTestFilePath, FileMode.Open, FileAccess.Read))
            {
                localhash = h.ComputeHash(fs);
            }
            using (FileStream fs = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
            {
                remotehash = h.ComputeHash(fs);
            }
            for (int i = 0; i < localhash.Length; i++)
            {
                if (localhash[i] != remotehash[i]) { return false; }
            }
            Debug.WriteLine("Download 2/2 passed");

            c.CopyComplete += new CopyCompleteDel(c_CopyComplete);
            c.Copy(basepath + uploadTestFileName, copypath + uploadTestFileName);
            autoResetEvent.WaitOne();
            c.MoveComplete += new MoveCompleteDel(c_MoveComplete);
            c.Move(basepath + uploadTestFileName, basepath + "moved" + uploadTestFileName);
            autoResetEvent.WaitOne();
            c.DeleteComplete += new DeleteCompleteDel(c_DeleteComplete);
            c.Delete(basepath + "moved" + uploadTestFileName);
            autoResetEvent.WaitOne();
            c.Delete(copypath + uploadTestFileName);
            autoResetEvent.WaitOne();
            c.Delete(copypath);
            autoResetEvent.WaitOne();
            Debug.WriteLine("Delete 1/2 passed");

            c.List(basepath);
            autoResetEvent.WaitOne();
            if (_files.Count != 0) { return false; }
            c.Delete(basepath);
            autoResetEvent.WaitOne();
            Debug.WriteLine("Delete 2/2 passed");

            return true;
        }

        static void c_MoveComplete(int statusCode)
        {
            Debug.Assert(statusCode == 201);
            autoResetEvent.Set();
        }

        static void c_DeleteComplete(int statusCode)
        {
            Debug.Assert(statusCode == 204);
            autoResetEvent.Set();
        }

        static void c_UploadComplete(int statusCode, object state)
        {
            Debug.Assert(statusCode == 201);
            autoResetEvent.Set();
        }

        static void c_CreateDirComplete(int statusCode)
        {
            Debug.Assert(statusCode == 200 || statusCode == 201);
            autoResetEvent.Set();
        }

        static List<string> _files;
        static void c_ListComplete(List<string> files, int statusCode)
        {
            Debug.Assert(statusCode == 207);
            _files = files;
            autoResetEvent.Set();
        }

        static void c_DownloadComplete(int code)
        {
            Debug.Assert(code == 200);
            autoResetEvent.Set();
        }

        static void c_CopyComplete(int code)
        {
            Debug.Assert(code == 201);
            autoResetEvent.Set();
        }
    }
}
