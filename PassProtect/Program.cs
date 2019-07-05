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

        static void ProgramStart()
        {
            // open the key
            Password pass = new Password();

            Console.Clear();
            // show main menu
            Console.WriteLine("Welcome to the folder locker tool!");
            Console.WriteLine("--Enter the number to start--");
            Console.WriteLine("1 -> Lock drive");
            Console.WriteLine("2 -> Unlock drive");
            Console.WriteLine("3 -> Exit");

            try
            {
                // insert command and act accordingly
                int command = Convert.ToInt32(Console.ReadLine());
                if (command == 1)
                {
                    Locking();
                }
                if (command == 2)
                {
                    Unlocking(pass);
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
        static void Locking()
        {
            try
            {
                // clear start menu
                Console.Clear();
                Drive[] drives = DriveObjs();
                ShowAvailableDrives(drives, true);

                // enter the partition letter
                Console.Write("Please, enter the drive letter you wish to lock: ");
                string partition_letter = Console.ReadLine();

                if (partition_letter.Trim() == "")
                {
                    ProgramStart();
                }

                // get all files from partition
                Drive current_drive = new Drive(new DriveInfo(partition_letter + ":\\"));

                if (current_drive.Folders.Length == 0 && current_drive.Files.Length == 0)
                {
                    Console.WriteLine("Drive " + current_drive.DriveObj.Name + " is empty");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    Locking();
                }
                else
                {
                    current_drive.Lock();

                    Console.WriteLine();
                    Console.WriteLine("Drive successfully locked!");
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
                Locking();
            }
        }

        static void Unlocking(Password pass)
        {
            try
            {
                // clear start menu
                Console.Clear();
                Drive[] drives = DriveObjs();
                ShowAvailableDrives(drives, false);

                // enter the partition letter
                Console.Write("Please, enter the drive letter you wish to unlock: ");
                string partition_letter = Console.ReadLine();

                if(partition_letter.Trim() == "")
                {
                    ProgramStart();
                }

                Drive current_drive = new Drive(new DriveInfo(partition_letter + ":\\"));

                if (current_drive.Folders.Length == 0 && current_drive.Files.Length == 0)
                {
                    Console.WriteLine("Drive " + current_drive.DriveObj.Name + " is empty");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    Unlocking(pass);
                }
                else
                {
                    PasswordVerifyAndUnlock(current_drive, pass);
                }
            }
            catch
            {
                Console.WriteLine("Something went wrong");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                Unlocking(pass);
            }
        }

        static void PasswordVerifyAndUnlock(Drive drive, Password pass)
        {
            Console.WriteLine();
            if (pass.Validate())
            {
                Console.WriteLine();
                // unlock all
                if (drive.Unlock())
                {
                    Console.WriteLine("Drive successfully unlocked!");
                    Process.Start("explorer.exe", drive.DriveObj.Name);
                }
                else
                {
                    Console.WriteLine("There was a problem unlocking selected drive.");
                }
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                ProgramStart();
            }
            else // if password is incorrect
            {
                Console.WriteLine("Wrong password");
                PasswordVerifyAndUnlock(drive, pass);
            }
        }

        static Drive[] DriveObjs()
        {
            List<Drive> result = new List<Drive>();
            DriveInfo[] allDrives = DriveInfo.GetDrives();

            foreach (DriveInfo d in allDrives)
            {
                if (d.IsReady == true)
                {
                    result.Add(new Drive(d));
                }
            }

            return result.ToArray();

        }

        static void ShowAvailableDrives(Drive[] drives, bool locking)
        {
            Console.Clear();

            if(drives.Length > 0)
            {
                Console.WriteLine("Here is the list of available drives:");
                bool at_least_one_drive = false;

                foreach (Drive d in drives)
                {
                    if (d.DriveObj.IsReady == true)
                    {
                        // find .locked
                        bool lock_found = false;
                        for (int i = 0; i < d.Files.Length && !lock_found; i++)
                        {
                            if(d.Files[i].Name == ".locked")
                            {
                                lock_found = true;
                            }
                        }

                        if (locking && !lock_found)
                        {
                            at_least_one_drive = true;
                            // show drives that are NOT locked
                            Console.WriteLine("Drive {0}", d.DriveObj.Name);
                            Console.WriteLine("     Drive Type: {0}", d.DriveObj.DriveType);
                            Console.WriteLine("     Drive Size: {0} GB ({1} GB free)", d.DriveObj.TotalSize / (1024 * 1024 * 1024), d.DriveObj.AvailableFreeSpace / (1024 * 1024 * 1024));
                            Console.WriteLine("     Folders: {0}", d.Folders.Length);
                            Console.WriteLine("     Files: {0}", d.Files.Length);
                            Console.WriteLine("---------------------");
                        }
                        else if(!locking && lock_found)
                        {
                            at_least_one_drive = true;
                            // show drives that ARE locked
                            Console.WriteLine("Drive {0}", d.DriveObj.Name);
                            Console.WriteLine("     This drive is locked");
                            Console.WriteLine("---------------------");
                        }
                    }
                }

                if (!at_least_one_drive)
                {
                    Console.WriteLine("No drives found");
                }
            }
        }
    }
}
