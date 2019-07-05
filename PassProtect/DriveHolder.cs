using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PassProtect
{
    class DriveHolder
    {
        public Drive[] Drives { get; private set; }

        public DriveHolder()
        {
            Drives = GetAllDrives();
        }

        private Drive[] GetAllDrives()
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

        public void ShowAvailableDrives(bool locking)
        {
            Console.Clear();

            if (Drives.Length > 0)
            {
                Console.WriteLine("Here is the list of available drives:");
                bool at_least_one_drive = false;

                foreach (Drive drive in Drives)
                {
                    if (drive.DriveObj.IsReady == true)
                    {
                        // find .locked
                        bool lock_found = false;
                        for (int i = 0; i < drive.Files.Length && !lock_found; i++)
                        {
                            if (drive.Files[i].Name == ".locked")
                            {
                                lock_found = true;
                            }
                        }

                        if (locking && !lock_found)
                        {
                            at_least_one_drive = true;
                            // show drives that are NOT locked
                            Console.WriteLine("Drive {0}", drive.DriveObj.Name);
                            Console.WriteLine("     Drive Type: {0}", drive.DriveObj.DriveType);
                            Console.WriteLine("     Drive Size: {0} GB ({1} GB free)", drive.DriveObj.TotalSize / (1024 * 1024 * 1024), drive.DriveObj.AvailableFreeSpace / (1024 * 1024 * 1024));
                            Console.WriteLine("     Folders: {0}", drive.Folders.Length);
                            Console.WriteLine("     Files: {0}", drive.Files.Length);
                            Console.WriteLine("---------------------");
                        }
                        else if (!locking && lock_found)
                        {
                            at_least_one_drive = true;
                            // show drives that ARE locked
                            Console.WriteLine("Drive {0}", drive.DriveObj.Name);
                            Console.WriteLine("     This drive is locked");
                            Console.WriteLine("---------------------");
                        }
                    }
                }

                if (!at_least_one_drive)
                {
                    Console.WriteLine("     No drives found");
                    Console.WriteLine("---------------------");
                }
            }
        }

    }
}
