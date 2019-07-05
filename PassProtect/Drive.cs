﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PassProtect
{
    class Drive
    {
        string[] exceptions = { "RECYCLER", "System Volume Information", "$RECYCLE.BIN", "$AVG", "Config.msi", "Windows", "OneDriveTemp", "ProgramData", "$WINRE_BACKUP_PARTITION.MARKER" };

        public DriveInfo DriveObj { get; private set; }

        public DirectoryInfo[] Folders { get; private set; }

        public FileInfo[] Files { get; private set; }

        public Drive(DriveInfo drive)
        {
            DriveObj = drive;
            DirectoryInfo root = new DirectoryInfo(drive.Name);
            Folders = root.GetDirectories();
            Files = root.GetFiles();

            // filter
            FilterFolders();
            FilterFiles();
        }

        private void GenerateLock()
        {
            string secret_path = DriveObj.Name + ".locked";
            FileStream secret = File.Create(secret_path);
            byte[] info = new UTF8Encoding(true).GetBytes("LOCKED=true");
            secret.Write(info, 0, info.Length);
            secret.Close();
            File.SetAttributes(secret_path, File.GetAttributes(secret_path) | FileAttributes.Hidden);
        }

        private void RemoveLock()
        {
            File.Delete(DriveObj.Name + ".locked");
        }

        public void Lock()
        {
            // generate locked file
            GenerateLock();

            // lock

            // hide all folders
            for (int i = 0; i < Folders.Length; i++)
            {
                Folders[i].Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            }

            // hide all files
            for (int i = 0; i < Files.Length; i++)
            {
                Files[i].Attributes = FileAttributes.Hidden;
            }
        }

        public bool Unlock()
        {
            try
            {
                // unlock all folders
                for (int i = 0; i < Folders.Length; i++)
                {
                    Folders[i].Attributes = FileAttributes.Directory | FileAttributes.Normal;
                }

                // unlock all files
                for (int i = 0; i < Files.Length; i++)
                {
                    Files[i].Attributes = FileAttributes.Normal;
                }

                // remove lock at the end
                RemoveLock();

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void FilterFolders()
        {
            List<DirectoryInfo> result = new List<DirectoryInfo>();

            for (int i = 0; i < Folders.Length; i++)
            {
                DirectoryInfo folder = Folders[i];
                bool valid = true;
                // check if folder name is not empty
                if (folder.Name.Trim() == "")
                {
                    valid = false;
                }

                // check if it is not part of the exceptions
                valid = !IsException(folder.Name);

                // check if it is not a system folder
                FileAttributes atrs = folder.Attributes;
                if (folder.Attributes == FileAttributes.System)
                {
                    valid = false;
                }

                if (valid)
                {
                    result.Add(folder);
                }
            }

            Folders = result.ToArray();
        }

        private void FilterFiles()
        {
            List<FileInfo> result = new List<FileInfo>();

            for (int i = 0; i < Files.Length; i++)
            {
                FileInfo file = Files[i];
                bool valid = true;
                // check if folder name is not empty
                if (file.Name.Trim() == "")
                {
                    valid = false;
                }

                // check if it is not part of the exceptions
                valid = !IsException(file.Name);

                // check if it is not a system folder
                FileAttributes atrs = file.Attributes;
                if (file.Attributes == FileAttributes.System)
                {
                    valid = false;
                }

                if (valid)
                {
                    result.Add(file);
                }
            }

            Files = result.ToArray();
        }

        private bool IsException(string name)
        {
            bool exception = false;

            for (int y = 0; y < exceptions.Length && !exception; y++)
            {
                string except = exceptions[y];
                if (name.ToLower() == except.ToLower())
                {
                    exception = true;
                }
            }

            return exception;
        }
    }
}