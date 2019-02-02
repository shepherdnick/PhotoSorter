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
            int folderNumber = 1;

            while (folderNumber < 20)
            {
                var path = @"D:\\Photos\\data-download-" + folderNumber + "\\";
                folderNumber++;

                string[] files = System.IO.Directory.GetFiles(path);

                foreach (var file in files)
                {
                    //string file = @"D:\\Photos\\data-download-1\\01022008_2265470602_o.jpg";
                    try
                    {
                        var tryToGetDirectories = ImageMetadataReader.ReadMetadata(file);
                    }
                    catch(Exception ex)
                    {
                        continue;
                    }

                    var directories = ImageMetadataReader.ReadMetadata(file);

                    foreach (var directory in directories)
                    {
                        foreach (var tag in directory.Tags)
                        {
                            if (tag.Name.Equals("Date/Time Original"))
                            {
                                Console.WriteLine($"[{directory.Name}] {tag.Name} = {tag.Description}");

                                var fileDateTime = DateTime.ParseExact(tag.Description, "yyyy:MM:dd HH:mm:ss", new CultureInfo("en-GB"));

                                //var destinationDirectory = Path.GetDirectoryName(file) + "\\" + fileDateTime.Year + "-" + fileDateTime.Month + "-" + fileDateTime.Day;
                                var destinationDirectory = @"D:\\Photos\\Sorted\\" + fileDateTime.Year + "-" + fileDateTime.Month + "-" + fileDateTime.Day;
                                Console.WriteLine(destinationDirectory);

                                if (!System.IO.Directory.Exists(destinationDirectory))
                                {
                                    System.IO.Directory.CreateDirectory(destinationDirectory);
                                }

                                File.Move(file, destinationDirectory + "\\" + Path.GetFileName(file));
                            }

                            if (tag.Name.Equals("Created"))
                            {
                                Console.WriteLine($"[{directory.Name}] {tag.Name} = {tag.Description}");

                                //Fri Apr 01 04:25:42 2011

                                var fileDateTime = DateTime.ParseExact(tag.Description, "ddd MMM dd HH:mm:ss yyyy", new CultureInfo("en-GB"));

                                //var destinationDirectory = Path.GetDirectoryName(file) + "\\" + fileDateTime.Year + "-" + fileDateTime.Month + "-" + fileDateTime.Day;
                                var destinationDirectory = @"D:\\Photos\\Sorted\\" + fileDateTime.Year + "-" + fileDateTime.Month + "-" + fileDateTime.Day;
                                Console.WriteLine(destinationDirectory);

                                if (!System.IO.Directory.Exists(destinationDirectory))
                                {
                                    System.IO.Directory.CreateDirectory(destinationDirectory);
                                }

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

                    Console.WriteLine("=======================================================");
                }
            }            

            Console.ReadLine();
        }
    }
}
