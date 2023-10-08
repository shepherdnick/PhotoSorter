using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using MetadataExtractor;

namespace PhotoSeparator
{
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
            catch
            {
                // If the read meta data failed, we might be inspecting a .mov file
                if (file.ToLower().EndsWith(".mov"))
                {
                    FileInfo fileInfo = new FileInfo(file);
                    var lastWriteDateTime = ParseDateTime(fileInfo.LastWriteTimeUtc.ToString());
                    MoveFile(file, lastWriteDateTime, destinationPath);
                }

                return;
            }

            foreach (var directory in directories)
            {
                // Find the EXIF data for the image
                foreach (var tag in directory.Tags)
                {
                    Console.WriteLine($"[{directory.Name}] {tag.Name} = {tag.Description}");

                    if (tag.Name.Equals("Date/Time Original"))
                    {
                        try {
                            var parsedDateTime = ParseDateTime(tag.Description);
                            MoveFile(file, parsedDateTime, destinationPath);
                            return;
                        } 
                        catch
                        {

                        }
                    }
                    else if (tag.Name.Equals("Created"))
                    {
                        try
                        {
                            var parsedDateTime = ParseDateTime(tag.Description);
                            MoveFile(file, parsedDateTime, destinationPath);
                            return;
                        }
                        catch
                        {

                        }
                    }
                    else if (tag.Name.Equals("File Name"))
                    {
                        try
                        {
                            Regex regex = new Regex(@"(\d\d)*-(\d\d)*-(\d\d)*");
                            Match match = regex.Match(tag.Description);

                            if (match.Success)
                            {
                                var parsedDateTime = ParseDateTime(match.Value);
                                MoveFile(file, parsedDateTime, destinationPath);
                                return;
                            }

                            regex = new Regex(@"(\d\d\d\d20\d\d)*");
                            match = regex.Match(tag.Description);

                            if (match.Success)
                            {
                                var parsedDateTime = ParseDateTime(match.Value);
                                MoveFile(file, parsedDateTime, destinationPath);
                                return;
                            }
                        }
                        catch
                        {

                        }
                    }
                    else if (tag.Name.Equals("File Modified Date"))
                    {
                        try
                        {
                            var parsedDateTime = ParseDateTime(tag.Description);
                            MoveFile(file, parsedDateTime, destinationPath);
                            return;
                        }
                        catch
                        {

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

        private DateTime ParseDateTime(string dateTimeToParse)
        {
            // Parse the EXIF data for the created date into a date we can use
            DateTime fileDateTime;

            var dateTimeFormatList = new List<string>();
            dateTimeFormatList.Add("yyyy:MM:dd HH:mm:ss");
            dateTimeFormatList.Add("ddd MMM dd HH:mm:ss %K yyyy");// Sat Feb 02 13:22:34 + 00:00 2019
            dateTimeFormatList.Add("ddd MMM dd HH:mm:ss yyyy");
            dateTimeFormatList.Add("ddd MMM dd HH:mm:ss zzz yyyy");
            dateTimeFormatList.Add("dd-MM-yy");
            dateTimeFormatList.Add("ddMMyyyy");
            dateTimeFormatList.Add("dd/MM/yyyy HH:mm:ss");// 24/02/2005 20:24:24

            if (DateTime.TryParseExact(dateTimeToParse, dateTimeFormatList.ToArray(), new CultureInfo("en-GB"), DateTimeStyles.None, out fileDateTime))
                return fileDateTime;

            throw new FormatException("DateTime provided was not in a format that was recognized");
        }

        private void MoveFile(string file, DateTime dateTimeOnFile, string destinationPath)
        {   
            // Create the destination folder
            var destinationDirectory = @"" + destinationPath + dateTimeOnFile.Year + "-" + dateTimeOnFile.Month + "-" + dateTimeOnFile.Day;
            Console.WriteLine($"Destination directory: {destinationDirectory}");

            // Check the destination directory exists
            if (!System.IO.Directory.Exists(destinationDirectory))
            {
                System.IO.Directory.CreateDirectory(destinationDirectory);
            }

            // Knowing the directory exists, we can now move the file
            if (File.Exists(file))
            {
                var separator = Path.DirectorySeparatorChar;
                var destinationFile = $"{destinationDirectory}{separator}{Path.GetFileName(file)}";

                if (File.Exists(destinationFile))
                {
                    Random random = new Random();
                    var randomNumber = random.Next(100);
                    destinationFile = "" + destinationDirectory + separator + Path.GetFileNameWithoutExtension(file) + randomNumber + Path.GetExtension(file);
                }

                File.Move(file, destinationFile);
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

        /// <summary>
        /// Process all files in the directory passed in, recurse on any directories 
        /// that are found, and process the files they contain.
        /// </summary>
        /// <param name="targetDirectory">Target directory.</param>
        public void ProcessDirectory(string targetDirectory)
        {
            // Ignore already sorted folders
            if (targetDirectory.ToLower().Equals("sorted")) {
                return;
            }

            // Process the list of files found in the directory.
            string[] fileEntries = System.IO.Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
                ProcessFile(fileName);

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = System.IO.Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
            {
                ProcessDirectory(subdirectory);

                // Now check that this subdirectory is empty of files, then delete it 
                // (makes it easier to keep track of files that haven't been copied)
                string[] fileList = System.IO.Directory.GetFiles(subdirectory);
                if (fileList.Length == 0)
                {
                    try
                    {
                        System.IO.Directory.Delete(subdirectory);
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Processes the file. Deletes parent folder if empty.
        /// </summary>
        /// <param name="path">Path.</param>
        public void ProcessFile(string path)
        {
            Console.WriteLine("Processed file '{0}'.", path);

            if (path.ToLower().Contains("picasa") || path.ToLower().Contains("thumb"))
            {
                File.Delete(path);
                return;
            }

            var separator = Path.DirectorySeparatorChar;
            var destinationFolder = $"{System.IO.Directory.GetCurrentDirectory()}{separator}Sorted{separator}";
            SortFileIntoFolder(path, destinationFolder);
        }
    }
}