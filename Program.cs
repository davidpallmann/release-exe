using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace release
{
    class Program
    {
        private static List<String> paths = new List<String>();
        private static List<String> files = new List<String>();
        private static SHA256 Sha256 = SHA256.Create();
        private static ConsoleColor consoleColor;
        private static String filename;
        private static String RootDir;
        private static String releaseName;

        #region Main

        static void Main(string[] args)
        {
            consoleColor = Console.ForegroundColor;

            try
            {
                if (args.Length == 2 && args[0].ToLower() == "hash")
                {
                    // release hash <file> : hash a file
                    filename = args[1];
                    Console.WriteLine(hash(filename) + " " + filename);
                }
                else if (args.Length >= 2 && args[0].ToLower() == "create")
                {
                    Create(args);
                } // end if create
                else if (args.Length >= 3 && args[0].ToLower() == "diff")
                {
                    Diff(args);
                } // end if diff
                else if (args.Length >= 2 && args[0].ToLower() == "verify")
                {
                    Verify(args);
                }
                else
                {
                    // Command help
                    Console.WriteLine("The release command creates a release manifest, or verifies a release manifest.");
                    Console.WriteLine();
                    Console.WriteLine("To create a manifest (all files): ......................... release create <release>.txt [ <release-path> ]");
                    Console.WriteLine("To create a differential manifest (changed files only): ... release diff <release>.txt <prior-release>.txt");
                    Console.WriteLine("To verify a manifest: ..................................... release verify <release>.txt [ <release-path> ]");
                    Console.WriteLine("To hash a file: ........................................... release hash <release>");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("EXCEPTION: " + ex.Message);
                Console.ForegroundColor = consoleColor;
            }
            Environment.Exit(0);
        }

        #endregion

        #region Create

        // Create a manifest
        // release create <release-file> [ <release-path> ]

        static void Create(String[] args)
        {
            // release create <release>.txt [ <release-folder> ] : create manifest file

            filename = args[1];

            if (File.Exists(filename))
            {
                File.Delete(filename);  // Ensure prior version of manfest file does not get included in new manifest.
            }

            // Get the release name from the manifest file name. 2421.txt => "2421".

            releaseName = filename;
            int pos = releaseName.LastIndexOf("\\");
            if (pos != -1) releaseName = releaseName.Substring(pos);
            pos = releaseName.LastIndexOf(".");
            if (pos != -1) releaseName = releaseName.Substring(0, pos) + "_release";

            // Get the release path. If not specified (optional parameter 3), default to working directory.

            String path = Environment.CurrentDirectory;
            if (args.Length > 2) path = args[2];
            DirectoryInfo di = new DirectoryInfo(path);
            path = di.FullName;
            RootDir = di.FullName;
            if (!RootDir.EndsWith("\\")) RootDir += "\\";

            Console.WriteLine("Creating manifest " + filename + " for " + path);
            ProcessPath(di);
            using (TextWriter tw = File.CreateText(filename))
            {
                foreach (String line in files)
                {
                    tw.WriteLine(line);
                }
            }
        }

        #endregion

        #region Diff

        // Create a differential manifest
        // release diff <release-file>.txt | <prior-release-file>.txt

        static void Diff(String[] args)
        {
            String line;
            String file;
            String path;
            String hash1, hash2;
            int errors = 0;
            int files = 0;

            path = Environment.CurrentDirectory;
            DirectoryInfo di = new DirectoryInfo(path);
            path = di.FullName;
            RootDir = di.FullName;
            if (!RootDir.EndsWith("\\")) RootDir += "\\";

            String[] createArgs = new String[]
            {
                args[0],
                args[1]
            };

            filename = args[1];
            String priorReleaseFilename = args[2];

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Differential release:");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("    New release manifest file ............. " + filename);
            Console.WriteLine("    Prior release manifest file ........... " + priorReleaseFilename);
            Console.WriteLine("    Files common to prior release and this release will be DELETED from this folder, leaving only new/changed files.");
            Console.WriteLine();

            Console.WriteLine("WARNING: This command will DELETE FILES from " + RootDir);
            Console.Write("Are you Sure? Type Y to proceed: ");
            if (Console.ReadLine().ToUpper() != "Y")
            {
                Console.ForegroundColor = consoleColor;
                Console.WriteLine("Cancelled");
                return;
            }

            Console.ForegroundColor = consoleColor;

            Create(createArgs);       // Start with a standard |release create <release-file>.txt|, to create full manifest

            // Now, iterate through each line of the prior release manifiest.
            // For each identical file (current file has same path/name/hash), delete the local  file.

            files = 0;
            int identicalFiles = 0;
            int deletedFiles = 0;
            int keepFiles = 0;

            using (TextReader tr = File.OpenText(priorReleaseFilename))
            {
                while ((line = tr.ReadLine()) != null)
                {
                    files++;
                    if (!line.EndsWith("\\release.exe"))   // don't delete release.exe
                    {
                        hash1 = line.Substring(0, 15);
                        file = line.Substring(16);
                        file = NewPath(file, path);
                        hash2 = hash(file);
                        if (hash1 == hash2)
                        {
                            identicalFiles++;
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Removing " + file);
                            //Console.WriteLine("Removing " + file + " (hash: " + hash2 + " - identical to " + line);
                            try
                            {
                                File.Delete(file);
                                deletedFiles++;
                            }
                            catch (Exception ex)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine(ex);
                                errors++;
                            }
                            Console.ForegroundColor = consoleColor;
                        }
                        else
                        {
                            keepFiles++;
                        }
                    } // end if !release.exe
                } // end while
            } // end TextReader

            DeleteEmptySubfolders(path);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Differential release created:");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("    Release manifest file ................. " + filename);
            Console.WriteLine("    Files in Full Release ................. " + files.ToString());
            Console.WriteLine("    Files in Differential release ......... " + keepFiles.ToString());
            Console.WriteLine("    Files removed from this directory ..... " + deletedFiles.ToString());

            if (errors != 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("WARNING: " + errors.ToString() + " error(s) occurred");
            }

            Console.ForegroundColor = consoleColor;
        }

        #endregion

        #region Verify

        // Verify a manifest.
        // manifest verify <release>.txt [ <release-path> ]

        static void Verify(String[] args)
        {
            // manifest verify <release>.txt [ <deploy-folder> ] : verify a release

            filename = args[1];
            String path = Environment.CurrentDirectory;
            if (args.Length > 2) path = args[2];
            DirectoryInfo di = new DirectoryInfo(path);
            String line;
            String file;
            String hash1, hash2;
            int errors = 0;
            int files = 0;

            using (TextReader tr = File.OpenText(filename))
            {
                while ((line = tr.ReadLine()) != null)
                {
                    files++;
                    hash1 = line.Substring(0, 15);
                    file = line.Substring(16);
                    file = NewPath(file, path);
                    hash2 = hash(file);
                    if (hash1 != hash2)
                    {
                        Console.WriteLine(line);
                        Console.ForegroundColor = ConsoleColor.Red;
                        if (hash2 == "FILE NOT FOUND ")
                        {
                            Console.WriteLine(hash2 + " " + file);
                        }
                        else
                        {
                            Console.WriteLine(line);
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine(hash2 + " " + file + "- ERROR: file is different");
                        }
                        Console.ForegroundColor = consoleColor;
                        errors++;
                    }
                }
            }
            Console.WriteLine(files.ToString() + " files checked");
            if (errors == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Release verified");
                Console.ForegroundColor = consoleColor;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(errors.ToString() + " error(s)");
                Console.ForegroundColor = consoleColor;
            }
        }

        #endregion

        #region Support Functions

        #region NewPath

        // Convert a file from old path to new path

        static String NewPath(String file, String path)
        {
            int pos = file.IndexOf("\\");
            if (pos != -1)
            {
                file = file.Substring(pos + 1);
                pos = file.IndexOf("\\");
                if (pos != -1)
                {
                    file = file.Substring(pos + 1);
                }
            }
            if (!path.EndsWith("\\")) path = path + "\\";
            return path + file;
        }

        #endregion

        #region ProcessPath

        // Add file hashes for a folder (and its subfolders)

        static void ProcessPath(DirectoryInfo di)
        {
            String name;
            String line;
            String path = di.FullName;
            paths.Add(path);
            foreach (FileInfo fi in di.GetFiles())
            {
                name = fi.FullName.Replace(RootDir, "C:\\" + releaseName + "\\");
                line = hash(fi.FullName) + " " + name;
                Console.WriteLine(line);
                files.Add(line);
            }
            foreach (DirectoryInfo sdi in di.GetDirectories())
            {
                ProcessPath(sdi);       // Recursively process subfolders
            }
        }

        #endregion

        #region DeleteEmptySubfolders

        private static void DeleteEmptySubfolders(string startLocation)
        {
            foreach (var directory in Directory.GetDirectories(startLocation))
            {
                DeleteEmptySubfolders(directory);
                if (Directory.GetFiles(directory).Length == 0 &&
                    Directory.GetDirectories(directory).Length == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Removing empty folder " + directory);
                    try
                    {
                        Directory.Delete(directory, false);
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(ex);
                    }
                    Console.ForegroundColor = consoleColor;
                }
            }
        }

        #endregion

        #region Hash

        // Create and return a quick hash on a file

        static String hash(String filename)
        {
            try
            {
                if (!File.Exists(filename))
                {
                    return "FILE NOT FOUND ";
                }
                byte[] bytes = null;
                using (FileStream stream = File.OpenRead(filename))
                {
                    bytes = Sha256.ComputeHash(stream);
                }
                String result = "";
                foreach (byte b in bytes)
                {
                    result += b.ToString("x2");
                }

                String formatted = "";
                for (int p = 0; p < result.Length - 3; p += 3)
                {
                    if (p != 0) formatted = formatted + "-";
                    formatted = formatted + result.Substring(p, 3);
                }
                // Hash is quite long. Shorten to the first two and last two segements so it is manageable.
                int len = formatted.Length;
                formatted = formatted.ToUpper();
                return formatted.Substring(0, 8) + formatted.Substring(len - 7);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ForegroundColor = consoleColor;
                return "ERROR          ";
            }
        }

        #endregion

        #endregion
    }
}
