using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using System.Security.Cryptography;

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
            bool password_set = false;
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
                password_set = key.GetValue("password") != null;
                if (!password_set)
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
                    Locking(exceptions, key);
                }
                if (command == 2)
                {
                    Unlocking(exceptions, key);
                }
                if (command == 3)
                {
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
        static void Locking(string[] exceptions, RegistryKey key)
        {
            try
            {
                // clear start menu
                Console.Clear();
                ShowAvailableDrives();

                // enter the partition letter
                Console.Write("Please, enter the drive letter you wish to lock: ");
                string partition_letter = Console.ReadLine();

                // get all files from partition
                DirectoryInfo d = new DirectoryInfo(partition_letter + ":\\");
                DirectoryInfo[] folders_on_drive = d.GetDirectories();
                FileInfo[] files_on_drive = d.GetFiles();

                if (folders_on_drive.Length == 0 && files_on_drive.Length == 0)
                {
                    Console.WriteLine("Drive " + partition_letter + ":\\ is empty");
                    Console.WriteLine("Press any key to continue . . .");
                    Console.ReadKey();
                    Locking(exceptions, key);
                }
                else
                {
                    // hide all folders
                    foreach (DirectoryInfo folder in folders_on_drive)
                    {
                        // before, it was adding files to the list here, and locking them in next commented foreach
                        if (CheckName(folder.Name, exceptions))
                        {
                            DirectoryInfo d1 = new DirectoryInfo(partition_letter + ":\\" + folder.Name);
                            d1.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
                        }
                    }

                    // hide all files
                    foreach (FileInfo file in files_on_drive)
                    {
                        file.Attributes = FileAttributes.Hidden;
                    }

                    Console.WriteLine();
                    Console.WriteLine("All folders are successfully locked!");
                    Console.WriteLine("Press any key to continue . . .");
                    Console.ReadKey();
                    ProgramStart();
                }
            }
            catch
            {
                Console.WriteLine("Something went wrong (maybe the drive you entered does not exist)!");
                Console.WriteLine("Press any key to continue . . .");
                Console.ReadKey();
                Locking(exceptions, key);
            }
        }

        static void Unlocking(string[] exceptions, RegistryKey key)
        {
            try
            {
                // clear start menu
                Console.Clear();
                ShowAvailableDrives();

                // enter the partition letter
                Console.Write("Please, enter the drive letter you wish to unlock: ");
                string partition_letter = Console.ReadLine();

                // folder getters
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
                    PasswordVerifyAndUnlock(exceptions, partition_letter, folders, files, key);
                }
            }
            catch
            {
                Console.WriteLine("Something went wrong (maybe the drive you entered does not exist)! :(");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                Unlocking(exceptions, key);
            }
        }

        static void PasswordVerifyAndUnlock(string[] exceptions, string partition_letter, DirectoryInfo[] folders, FileInfo[] files, RegistryKey key)
        {
            // the list that holds folder names
            List<string> folder_names = new List<string>();
            // get folder names
            foreach (DirectoryInfo folder in folders)
            {
                if (CheckName(folder.Name, exceptions))
                {
                    folder_names.Add(folder.Name);
                }
            }

            Console.WriteLine("");
            Console.Write("Enter the password: ");
            string input_password = InputPassword(); // password input
            if (ComparePassword(key.GetValue("password").ToString(), input_password)) // if password is correct
            {
                Console.WriteLine();
                // unlock all
                if (Unlock_All(folder_names, files, partition_letter, exceptions))
                {
                    Console.WriteLine("Folders and files successfully unlocked!");
                    Process.Start("explorer.exe", partition_letter + ":\\");
                }
                else
                {
                    Console.WriteLine("There was a problem unlocking all folders on a selected drive!");
                }
                Console.ReadKey();
                ProgramStart();
            }
            else // if password is incorrect
            {
                Console.WriteLine("Wrong password");
                PasswordVerifyAndUnlock(exceptions, partition_letter, folders, files, key);
            }
        }

        static void ShowAvailableDrives()
        {
            Console.Clear();

            DriveInfo[] allDrives = DriveInfo.GetDrives();

            if(allDrives.Length > 0)
            {
                Console.WriteLine("Here is the list of available drives:");
            }

            foreach (DriveInfo d in allDrives)
            {
                if (d.IsReady == true)
                {
                    DirectoryInfo drive_root = new DirectoryInfo(d.Name);
                    DirectoryInfo[] directories = drive_root.GetDirectories();
                    FileInfo[] files = drive_root.GetFiles();

                    Console.WriteLine("Drive {0}", d.Name);
                    Console.WriteLine("  Drive Type: {0}", d.DriveType);
                    Console.WriteLine("  Drive Size: {0} GB ({1} GB free)", d.TotalSize / (1024 * 1024 * 1024), d.AvailableFreeSpace / (1024 * 1024 * 1024));
                    Console.WriteLine("  Folders: {0}", directories.Length);
                    Console.WriteLine("  Files: {0}", files.Length);

                    Console.WriteLine("---------------------");
                }
            }
        }
        static bool Unlock_All(List<string> folder_names, FileInfo[] files, string drive_letter, string[] exceptions)
        {
            try
            {
                foreach (string folder_name in folder_names)
                {
                    if (CheckName(folder_name,exceptions))
                    {
                        DirectoryInfo d1 = new DirectoryInfo(drive_letter + ":\\" + folder_name);
                        d1.Attributes = FileAttributes.Directory | FileAttributes.Normal;
                    }
                }

                foreach(FileInfo file in files)
                {
                    file.Attributes = FileAttributes.Normal;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
        static bool CheckName(string folder_name,string[] exceptions)
        {
            // check if folder name is not empty
            if(folder_name.Trim() == "")
            {
                return false;
            }
            // check if it is not part of the exceptions
            foreach(string exception in exceptions)
            {
                if(folder_name.ToLower() == exception.ToLower())
                {
                    return false;
                }
            }
            return true;
        }
    }
}
