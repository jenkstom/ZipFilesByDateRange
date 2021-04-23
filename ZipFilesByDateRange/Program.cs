using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace ZipFilesByDateRange
{
    class Program
    {

        static bool DateRangeContainsEntries(Dictionary<String,DateTime> filelist, DateTime begin, DateTime end)
        {
            bool result = false;
            foreach (KeyValuePair<String, DateTime> entry in filelist)
            {
                if (entry.Value >= begin && entry.Value <= end) result = true;
            }
            return result;
        }

        static String FindNewFileName(String filename)
        {
            String result = filename;
            int index = 0;

            while (File.Exists(result))
            {
                index++;
                result = $"{Path.GetDirectoryName(filename)}{Path.GetFileNameWithoutExtension(filename)}_{index}{Path.GetExtension(filename)}";
            }

            return result;
        }

        static void Main(string[] args)
        {
            if (args.Length<2)
            {
                Console.WriteLine("ZipFilesByDateRange usage:\r\n\tZipFilesByDateRange <pathname> <pattern>");
                return;
            }
            String path = args[0];
            String pattern = args[1];

            List<String> filelist = new List<String>();
            Dictionary<String, DateTime> filedict = new Dictionary<String, DateTime>();
            DateTime firsttimestamp = DateTime.Now;
            DateTime thistimestamp;

            String zipfilename;

            if (pattern.IndexOf("*") > -1)
            {
                zipfilename = pattern.Split('*')[0];
            }
            else
            {
                zipfilename = pattern;
                pattern += "*";
            }

            foreach (String filename in Directory.EnumerateFiles(path, pattern))
            {
                thistimestamp = File.GetLastWriteTime(filename);
                filedict.Add(filename, thistimestamp);
                if (thistimestamp < firsttimestamp)
                {
                    firsttimestamp = thistimestamp;
                }
                //Console.WriteLine($"{filename} - {thistimestamp:yyyy-MM-dd HH:mm:ss}");
            }

            // Divide this into weeks
            double weeks = (DateTime.Now - firsttimestamp).TotalDays / 7;
            DateTime WeekDate = firsttimestamp.Date.AddDays(-(int)firsttimestamp.DayOfWeek);
            String entryname;
            String zipfile;
            String thisfile;
            ZipEntry entry;
            FileInfo fi;
            byte[] buffer = new byte[4096];
            int filecount;

            for (DateTime wd = WeekDate; wd < DateTime.Now; wd = wd.AddDays(7))
            {
                zipfile = $"{zipfilename}_{wd:yyyy-MM-dd}.zip";

                if (DateRangeContainsEntries(filedict, wd, wd.AddDays(7)))
                {
                    filelist.Clear();

                    if (File.Exists(Path.Combine(path,zipfile)))
                    {
                        File.Move(Path.Combine(path, zipfile), Path.Combine(path, FindNewFileName(zipfile)));
                    }

                    using (FileStream fsOut = File.Create(Path.Combine(path, zipfile)))
                    {
                        using (ZipOutputStream zipStream = new ZipOutputStream(fsOut))
                        {
                            Console.Write(zipfile);
                            filecount = 0;

                            foreach (KeyValuePair<String, DateTime> file in filedict)
                            {
                                if (file.Value >= wd && file.Value <= wd.AddDays(7))
                                {
                                    if (Path.GetExtension(file.Key).ToUpper().Trim('.') != "ZIP")
                                    {
                                        fi = new FileInfo(file.Key);
                                        //Console.WriteLine($"  {file.Key}");
                                        thisfile = ZipEntry.CleanName(Path.GetFileName(file.Key));
                                        entry = new ZipEntry(thisfile);
                                        entry.DateTime = file.Value;
                                        entry.Size = fi.Length;
                                        zipStream.PutNextEntry(entry);

                                        using (FileStream fsInput = File.OpenRead(file.Key))
                                        {
                                            StreamUtils.Copy(fsInput, zipStream, buffer);
                                        }

                                        zipStream.CloseEntry();
                                        filelist.Add(file.Key);
                                    }
                                }
                            }
                        }
                    }

                    foreach (String file in filelist)
                    {
                        File.Delete(file);
                        filecount++;
                    }

                    Console.WriteLine($" {filecount} files.");
                }
            }
        }
    }
}
