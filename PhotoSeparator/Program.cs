using MetadataExtractor;
using System;
using System.Collections.Generic;
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
            SortExistingPhotosIntoCorrectFolder();
        }

        private void SortFileIntoFolder(string file, string destinationPath)
        {
            // Try to safely read the image file
            IReadOnlyList<MetadataExtractor.Directory> directories = new List<MetadataExtractor.Directory>();

            try
            {
                var tryToGetDirectories = ImageMetadataReader.ReadMetadata(file);
                directories = tryToGetDirectories;
            }
            catch (Exception ex)
            {
                // If the read meta data failed, we might be inspecting a .mov file
                if (file.ToLower().EndsWith(".mov"))
                {
                    FileInfo fileInfo = new FileInfo(file);
                    MoveFile(file, fileInfo.LastWriteTimeUtc.ToString(), destinationPath);
                }

                return;
            }

            foreach (var directory in directories)
            {
                foreach (var tag in directory.Tags)
                {
                    Console.WriteLine($"[{directory.Name}] {tag.Name} = {tag.Description}");

                    // Find the EXIF data for the image
                    if (tag.Name.Equals("Date/Time Original") || tag.Name.Equals("Created"))
                    {
                        Console.WriteLine($"[{directory.Name}] {tag.Name} = {tag.Description}");
                        MoveFile(file, tag.Description, destinationPath);
                        return;
                    }
                }

                if (directory.HasError)
                {
                    foreach (var error in directory.Errors)
                        Console.WriteLine($"ERROR: {error}");
                }
            }
        }

        private void MoveFile(string file, string dateTimeOnFile, string destinationPath)
        {
            // Parse the EXIF data for the created date into a date we can use
            DateTime fileDateTime = DateTime.Now;
            try
            {
                // Get the date time format for the jpg
                fileDateTime = DateTime.ParseExact(dateTimeOnFile, "yyyy:MM:dd HH:mm:ss", new CultureInfo("en-GB"));
            }
            catch (FormatException fmex)
            {
                try
                {
                    // Newer .mov files have a date time format like this
                    fileDateTime = DateTime.ParseExact(dateTimeOnFile, "ddd MMM dd HH:mm:ss yyyy", new CultureInfo("en-GB"));
                }
                catch(FormatException fmexcep)
                {
                    // Just try an parse older .mov date time formats
                    fileDateTime = DateTime.Parse(dateTimeOnFile);
                }
            }            

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
                    File.Move(file, destinationFile);
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
