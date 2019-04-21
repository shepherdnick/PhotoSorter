using MetadataExtractor;
using System;
using System.Globalization;
using System.IO;

namespace PhotoSeparator
{
    class Program
    {
        static void Main(string[] args)
        {
            PhotoSorter photoSorter = new PhotoSorter();
        }
    }

    class PhotoSorter
    {
        public PhotoSorter()
        {
            //SortPhotosIntoFolders();
            SortExistingPhotosIntoCorrectFolder();
        }

        private void SortPhotosIntoFolders()
        {
            int folderNumber = 1;

            while (folderNumber < 20)
            {
                var path = @"D:\\Photos\\data-download-" + folderNumber + "\\";
                folderNumber++;

                string[] files = System.IO.Directory.GetFiles(path);

                foreach (var file in files)
                {
                    //string file = @"D:\\Photos\\data-download-1\\01022008_2265470602_o.jpg";
                    SortFileIntoFolder(file, "D:\\Photos\\Sorted\\");

                    Console.WriteLine("=======================================================");
                }
            }

            Console.ReadLine();
        }

        private void SortFileIntoFolder(string file, string destinationPath)
        {
            try
            {
                var tryToGetDirectories = ImageMetadataReader.ReadMetadata(file);
            }
            catch (Exception ex)
            {
                return;
            }

            var directories = ImageMetadataReader.ReadMetadata(file);

            foreach (var directory in directories)
            {
                foreach (var tag in directory.Tags)
                {
                    Console.WriteLine($"[{directory.Name}] {tag.Name} = {tag.Description}");

                    // Find the EXIF data for the image
                    if (tag.Name.Equals("Date/Time Original"))
                    {
                        Console.WriteLine($"[{directory.Name}] {tag.Name} = {tag.Description}");

                        // Parse the EXIF data for the created date into a date we can use
                        var fileDateTime = DateTime.ParseExact(tag.Description, "yyyy:MM:dd HH:mm:ss", new CultureInfo("en-GB"));
                        
                        // Create the destination folder
                        var destinationDirectory = @"" + destinationPath + fileDateTime.Year + "-" + fileDateTime.Month + "-" + fileDateTime.Day;
                        Console.WriteLine(destinationDirectory);

                        // Check the destination directory exists
                        if (!System.IO.Directory.Exists(destinationDirectory))
                        {
                            System.IO.Directory.CreateDirectory(destinationDirectory);
                        }

                        // Knowing the directory exists, we can now move the file
                        if (File.Exists(file))
                        {
                            var destinationFile = destinationDirectory + "\\" + Path.GetFileName(file);

                            if (File.Exists(destinationFile))
                            {
                                Random random = new Random();
                                var randomNumber = random.Next(100);
                                destinationFile = "" + Path.GetDirectoryName(file) + "\\" + Path.GetFileNameWithoutExtension(file) + randomNumber + Path.GetExtension(file);
                            }

                            File.Move(file, destinationFile);
                        }
                    }

                    // If the EXIF data tag wasn't found, find a new one (usually used for movie files)
                    if (tag.Name.Equals("Created"))
                    {
                        Console.WriteLine($"[{directory.Name}] {tag.Name} = {tag.Description}");
                        //Fri Apr 01 04:25:42 2011

                        // Parse the EXIF data for the created date into a date we can use
                        var fileDateTime = DateTime.ParseExact(tag.Description, "ddd MMM dd HH:mm:ss yyyy", new CultureInfo("en-GB"));

                        // Create the destination folder
                        var destinationDirectory = @"D:\\Photos\\Sorted\\" + fileDateTime.Year + "-" + fileDateTime.Month + "-" + fileDateTime.Day;
                        Console.WriteLine(destinationDirectory);

                        // Check the destination directory exists
                        if (!System.IO.Directory.Exists(destinationDirectory))
                        {
                            System.IO.Directory.CreateDirectory(destinationDirectory);
                        }

                        // Knowing the directory exists, we can now move the file
                        if (File.Exists(file))
                        {
                            File.Move(file, destinationDirectory + "\\" + Path.GetFileName(file));
                        }
                    }
                }

                if (directory.HasError)
                {
                    foreach (var error in directory.Errors)
                        Console.WriteLine($"ERROR: {error}");
                }
            }
        }

        private void SortExistingPhotosIntoCorrectFolder()
        {
            var path = System.IO.Directory.GetCurrentDirectory();

            if (File.Exists(path))
            {
                // This path is a file
                ProcessFile(path);
            }
            else if (System.IO.Directory.Exists(path))
            {
                // This path is a directory
                if (!path.ToLower().Contains("sorted"))
                {
                    ProcessDirectory(path);
                }
            }
            else
            {
                Console.WriteLine("{0} is not a valid file or directory.", path);
            }
        }

        // Process all files in the directory passed in, recurse on any directories 
        // that are found, and process the files they contain.
        public void ProcessDirectory(string targetDirectory)
        {
            // Process the list of files found in the directory.
            string[] fileEntries = System.IO.Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
                ProcessFile(fileName);

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = System.IO.Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                ProcessDirectory(subdirectory);
        }

        // Insert logic for processing found files here.
        public void ProcessFile(string path)
        {
            Console.WriteLine("Processed file '{0}'.", path);
            SortFileIntoFolder(path, System.IO.Directory.GetCurrentDirectory() + "\\Sorted\\");
        }
    }
}
