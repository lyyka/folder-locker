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
        
        static string DriveLetterInput()
        {
            Console.Write("Please, enter the drive letter you wish to lock: ");
            ConsoleKeyInfo partition_letter = Console.ReadKey();

            string keychar = partition_letter.KeyChar.ToString().Trim();

            if(keychar == "")
            {
                ProgramStart();
                return keychar;
            }
            else
            {
                switch (partition_letter.Key)
                {
                    case ConsoleKey.Backspace:
                        ProgramStart();
                        return "";
                    case ConsoleKey.Escape:
                        ProgramStart();
                        return "";
                    default:
                        return partition_letter.KeyChar.ToString();
                }
            }
        }

        static void Locking()
        {
            try
            {
                // clear start menu
                Console.Clear();
                DriveHolder d_holder = new DriveHolder();
                d_holder.ShowAvailableDrives(true);

                // enter the partition letter
                string partition_letter = DriveLetterInput();

                // get all files from partition
                Drive current_drive = new Drive(partition_letter);

                if (current_drive.Folders.Length == 0 && current_drive.Files.Length == 0)
                {
                    Console.WriteLine("Drive " + current_drive.DriveObj.Name + " is empty");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    Locking();
                }
                else
                {
                    Console.WriteLine();

                    if (current_drive.Lock())
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Drive successfully locked!");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error locking the drive!");
                        Console.ResetColor();
                    }
                    
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
                DriveHolder d_holder = new DriveHolder();
                d_holder.ShowAvailableDrives(false);

                // enter the partition letter
                string partition_letter = DriveLetterInput();

                Drive current_drive = new Drive(partition_letter);

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
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Password validated...");
                // unlock all
                if (drive.Unlock())
                {
                    Console.WriteLine("Drive successfully unlocked!");
                    Process.Start("explorer.exe", drive.DriveObj.Name);
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error unlocking the drive.");
                    Console.ResetColor();
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
    }
}
