using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Text;

namespace PassProtect
{
    class Program
    {
        static void Main(string[] args)
        {
            ProgramStart();
        }

        public static string GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssffff");
        }

        static string HashPassword(string password)
        {
            byte[] salt;
            new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);

            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
            byte[] hash = pbkdf2.GetBytes(20);

            byte[] hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);

            return Convert.ToBase64String(hashBytes);
        }

        static bool ComparePassword(string hashed_pass, string password)
        {
            bool valid = true;
            /* Extract the bytes */
            byte[] hashBytes = Convert.FromBase64String(hashed_pass);
            /* Get the salt */
            byte[] salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);
            /* Compute the hash on the password the user entered */
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
            byte[] hash = pbkdf2.GetBytes(20);
            /* Compare the results */
            for (int i = 0; i < 20 && valid; i++)
                if (hashBytes[i + 16] != hash[i])
                    valid = false;
                        
            return valid;
        }

        static string InputPassword()
        {
            string password = "";
            do
            {
                ConsoleKeyInfo cons_key = Console.ReadKey(true);
                // Backspace Should Not Work
                if (cons_key.Key != ConsoleKey.Backspace && cons_key.Key != ConsoleKey.Enter)
                {
                    password += cons_key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (cons_key.Key == ConsoleKey.Backspace && password.Length > 0)
                    {
                        password = password.Substring(0, (password.Length - 1));
                        Console.Write("\b \b");
                    }
                    else if (cons_key.Key == ConsoleKey.Enter)
                    {
                        break;
                    }
                }
            } while (true);

            return password;
        }

        static void ShowPasswordCreation(RegistryKey key)
        {
            Console.WriteLine("Please create a unique password for use to lock folders: ");

            string password = InputPassword();

            // hash the password
            string hashed = HashPassword(password);

            // save password hash
            key.SetValue("password", hashed);

            // set current date and time
            key.SetValue("last_updated", DateTime.Now);

            Console.Clear();
        }

        static void ProgramStart()
        {
            string[] exceptions = { "RECYCLER" , "System Volume Information" , "$RECYCLE.BIN" , "$AVG" , "Config.msi" , "Windows" , "OneDriveTemp", "ProgramData" , "$WINRE_BACKUP_PARTITION.MARKER" };
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\FolderLocker", true);

            Console.Clear();
            Console.WriteLine("Welcome to the folder locker tool!");
            // key.DeleteValue("password"); // used for testing purposes only

            if (key == null)
            {
                key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\FolderLocker");
                ShowPasswordCreation(key);
            }
            else
            {
                if (key.GetValue("password") == null)
                {
                    ShowPasswordCreation(key);
                }
                else
                {
                    Console.WriteLine("Password last updated: {0}", key.GetValue("last_updated"));
                }
            }

            Console.WriteLine("--Enter the number to start--");
            Console.WriteLine("1 -> Lock folders");
            Console.WriteLine("2 -> Unlock folders");
            Console.WriteLine("3 -> Close this");
            Console.WriteLine();

            try
            {
                int command = Convert.ToInt32(Console.ReadLine());
                if (command == 1)
                {
                    key.Close();
                    Locking(exceptions);
                }
                if (command == 2)
                {
                    Unlocking(exceptions, key);
                }
                if (command == 3)
                {
                    key.Close();
                    Environment.Exit(0);
                }
                if (command < 1 || command > 3)
                {
                    ProgramStart();
                }
            }
            catch
            {
                ProgramStart();
            }
        }
        static void Locking(string[] exceptions)
        {
            try
            {
                // clear start menu
                Console.Clear();
                ShowAvailableDrives(exceptions, true);

                // enter the partition letter
                Console.Write("Please, enter the drive letter you wish to lock: ");
                string partition_letter = Console.ReadLine();

                // get all files from partition
                DirectoryInfo d = new DirectoryInfo(partition_letter + ":\\");
                DirectoryInfo[] folders_on_drive = d.GetDirectories();
                FileInfo[] files_on_drive = d.GetFiles();
                folders_on_drive = FilterFolders(folders_on_drive, exceptions);
                files_on_drive = FilterFiles(files_on_drive, exceptions);

                if (folders_on_drive.Length == 0 && files_on_drive.Length == 0)
                {
                    Console.WriteLine("Drive " + partition_letter + ":\\ is empty");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    Locking(exceptions);
                }
                else
                {
                    // hide all folders
                    for(int i = 0;i < folders_on_drive.Length; i++)
                    {
                        folders_on_drive[i].Attributes = FileAttributes.Directory | FileAttributes.Hidden;
                    }
                    
                    // hide all files
                    for(int i = 0;i < files_on_drive.Length; i++)
                    {
                        files_on_drive[i].Attributes = FileAttributes.Hidden;
                    }

                    // put special file on drive and hide it
                    // this will tell us if the particular drive is locked
                    // remembering just the drive letter can cause other drives with that letter appear to be locked
                    string secret_path = partition_letter + ":\\.locked";
                    FileStream secret = File.Create(secret_path);
                    byte[] info = new UTF8Encoding(true).GetBytes("LOCKED=true");
                    secret.Write(info, 0, info.Length);
                    secret.Close();
                    File.SetAttributes(secret_path, File.GetAttributes(secret_path) | FileAttributes.Hidden);

                    // msg
                    Console.WriteLine();
                    Console.WriteLine("All folders are successfully locked!");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    ProgramStart();
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                Locking(exceptions);
            }
        }

        static void Unlocking(string[] exceptions, RegistryKey key)
        {
            try
            {
                // clear start menu
                Console.Clear();
                ShowAvailableDrives(exceptions, false);

                // enter the partition letter
                Console.Write("Please, enter the drive letter you wish to unlock: ");
                string partition_letter = Console.ReadLine();

                // folder and files getters
                DirectoryInfo d = new DirectoryInfo(partition_letter + ":\\");
                DirectoryInfo[] folders = d.GetDirectories();
                FileInfo[] files = d.GetFiles();

                if (folders.Length == 0 && files.Length == 0)
                {
                    Console.WriteLine("Drive " + partition_letter + ":\\ is empty");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    Unlocking(exceptions, key);
                }
                else
                {
                    folders = FilterFolders(folders, exceptions);
                    files = FilterFiles(files, exceptions);
                    PasswordVerifyAndUnlock(exceptions, partition_letter, folders, files, key);
                }
            }
            catch
            {
                Console.WriteLine("Something went wrong");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                Unlocking(exceptions, key);
            }
        }

        static void PasswordVerifyAndUnlock(string[] exceptions, string partition_letter, DirectoryInfo[] folders, FileInfo[] files, RegistryKey key)
        {
            Console.WriteLine("");
            Console.Write("Password: ");
            string input_password = InputPassword(); // password input
            if (ComparePassword(key.GetValue("password").ToString(), input_password)) // if password is correct
            {
                Console.WriteLine();
                // unlock all
                if (Unlock_All(folders, files, exceptions))
                {
                    // delete .locked file
                    File.Delete(partition_letter + ":\\.locked");

                    Console.WriteLine("Folders and files successfully unlocked!");
                    Process.Start("explorer.exe", partition_letter + ":\\");
                }
                else
                {
                    Console.WriteLine("There was a problem unlocking all folders on a selected drive.");
                }
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                ProgramStart();
            }
            else // if password is incorrect
            {
                Console.WriteLine("Wrong password");
                PasswordVerifyAndUnlock(exceptions, partition_letter, folders, files, key);
            }
        }

        static bool Unlock_All(DirectoryInfo[] folders, FileInfo[] files, string[] exceptions)
        {
            try
            {
                // show all folders
                for (int i = 0; i < folders.Length; i++)
                {
                    folders[i].Attributes = FileAttributes.Directory | FileAttributes.Normal;
                }

                // show all files
                for (int i = 0; i < files.Length; i++)
                {
                    files[i].Attributes = FileAttributes.Normal;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        static void ShowAvailableDrives(string[] exceptions, bool locking)
        {
            Console.Clear();

            DriveInfo[] allDrives = DriveInfo.GetDrives();

            if(allDrives.Length > 0)
            {
                Console.WriteLine("Here is the list of available drives:");

                foreach (DriveInfo d in allDrives)
                {
                    if (d.IsReady == true)
                    {
                        DirectoryInfo drive_root = new DirectoryInfo(d.Name);
                        DirectoryInfo[] directories = drive_root.GetDirectories();
                        FileInfo[] files = drive_root.GetFiles();

                        // filter out system files and exceptions
                        directories = FilterFolders(directories, exceptions);
                        files = FilterFiles(files, exceptions);

                        // find .locked
                        bool lock_found = false;
                        for (int i = 0; i < files.Length && !lock_found; i++)
                        {
                            if(files[i].Name == ".locked")
                            {
                                lock_found = true;
                            }
                        }

                        if (locking && !lock_found)
                        {
                            // show drives that are NOT locked
                            Console.WriteLine("Drive {0}", d.Name);
                            Console.WriteLine("     Drive Type: {0}", d.DriveType);
                            Console.WriteLine("     Drive Size: {0} GB ({1} GB free)", d.TotalSize / (1024 * 1024 * 1024), d.AvailableFreeSpace / (1024 * 1024 * 1024));
                            Console.WriteLine("     Folders: {0}", directories.Length);
                            Console.WriteLine("     Files: {0}", files.Length);
                            Console.WriteLine("---------------------");
                        }
                        else if(!locking && lock_found)
                        {
                            // show drives that ARE locked
                            Console.WriteLine("Drive {0}", d.Name);
                            Console.WriteLine("     This drive is locked");
                            Console.WriteLine("---------------------");
                        }
                    }
                }
            }
        }
        
        static DirectoryInfo[] FilterFolders(DirectoryInfo[] folders, string[] exceptions)
        {
            List<DirectoryInfo> result = new List<DirectoryInfo>();

            for(int i = 0; i < folders.Length; i++)
            {
                DirectoryInfo folder = folders[i];
                bool valid = true;
                // check if folder name is not empty
                if (folder.Name.Trim() == "")
                {
                    valid = false;
                }

                // check if it is not part of the exceptions
                for(int y = 0; y < exceptions.Length && valid; y++)
                {
                    string exception = exceptions[y];
                    if (folder.Name.ToLower() == exception.ToLower())
                    {
                        valid = false;
                    }
                }

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

            return result.ToArray();
        }

        static FileInfo[] FilterFiles(FileInfo[] files, string[] exceptions)
        {
            List<FileInfo> result = new List<FileInfo>();

            for (int i = 0; i < files.Length; i++)
            {
                FileInfo file = files[i];
                bool valid = true;
                // check if folder name is not empty
                if (file.Name.Trim() == "")
                {
                    valid = false;
                }

                // check if it is not part of the exceptions
                for (int y = 0; y < exceptions.Length && valid; y++)
                {
                    string exception = exceptions[y];
                    if (file.Name.ToLower() == exception.ToLower())
                    {
                        valid = false;
                    }
                }

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

            return result.ToArray();
        }
    }
}
