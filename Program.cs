using System.Diagnostics;

namespace WallpaperEnginePKGManager
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 1 && (args[0].ToLower() == "-h" || args[0].ToLower() == "--help"))
            {
                ShowUsage();
                return;
            }

            // Check for valid argument count
            if (args.Length < 3)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Not a valid command. Run -h or --help for help.");
                Console.ForegroundColor = ConsoleColor.Gray;
                Environment.Exit(1);
            }

            PKGManager converter = null;

            bool convertToZip;
            string pkg = null;
            string zip = null;

            if (args[0].ToLower() == "--extract")
            {
                convertToZip = true;
                pkg = args[1];
                zip = args[2];
            }
            else if (args[0].ToLower() == "--repack")
            {
                convertToZip = false;
                zip = args[1];
                pkg = args[2];
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Invalid conversion direction. Use --pkgtozip or --ziptopkg.");
                Console.ForegroundColor = ConsoleColor.Gray;
                Environment.Exit(1);
                return;
            }

                    try
                    {
                        converter = new PKGManager(pkg, zip, convertToZip);
                    }
                    catch (PKGManager.PKGManagerException ex)
                    {
                        //Error handling
                        Console.ForegroundColor = ConsoleColor.Red;
                        {
                            switch (ex.Error)
                            {
                                case PKGManager.Error.PKG_FILE_NOT_FOUND:
                                    Console.WriteLine($"PKG file: '{pkg}' not found! You are sure about correctness of this path?");
                                    break;
                                case PKGManager.Error.FAILED_TO_CREATE_FILE_STREAM:
                                    Console.WriteLine($"Failed to create file streams for pkg: '{pkg}' and zip: '{zip}' - Message:[{ex.SrcMsg}]");
                                    break;
                                case PKGManager.Error.FAILED_TO_OPEN_ZIP_ARCHIVE:
                                    Console.WriteLine($"Failed to open zip archive: '{zip}' - Message:[{ex.SrcMsg}]");
                                    break;
                            }
                        }
                        Console.ForegroundColor = ConsoleColor.Gray;
                        return;
                    }

                    //Convert!
                    try
                    {
                        converter.Convert();
                    }
                    catch (PKGManager.PKGManagerException ex)
                    {
                        //Error handling
                        Console.ForegroundColor = ConsoleColor.Red;
                        switch (ex.Error)
                        {
                            case PKGManager.Error.UNHANDLED_EXCEPTION:
                                Console.WriteLine($"Unhandled exception occured! - Message:[{ex.SrcMsg}]");
                                break;
                            case PKGManager.Error.PKG_FILE_CORRUPTED:
                                Console.WriteLine($"PKG file: '{pkg}' corrupted or unhandled error! - Message:[{ex.SrcMsg}]");
                                break;
                            case PKGManager.Error.INVALID_PKG_FILE_SIGNATURE:
                                Console.WriteLine($"Unknown PKG signature - [{ex.SrcMsg}]");
                                break;
                            case PKGManager.Error.FAILED_SEEKING_PKG_FILE:
                                Console.WriteLine($"Failed seeking in PKG file - [{ex.SrcMsg}]");
                                break;
                            case PKGManager.Error.FAILED_READING_PKG_FILE:
                                Console.WriteLine($"Failed reading PKG file! - Message:[{ex.SrcMsg}]");
                                break;
                            case PKGManager.Error.READED_LENGHT_NOT_EQUALS_NEED_LENGHT:
                                Console.WriteLine($"Readed length != Need length - Message:[{ex.SrcMsg}]");
                                break;
                            case PKGManager.Error.FAILED_WRITING_INTO_ZIP_FILE:
                                Console.WriteLine($"Failed writing into zip file! - Message:[{ex.SrcMsg}]");
                                break;
                        }
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Environment.ExitCode = (int)ex.Error;
                        return;
                    }
                }

        private static void ShowUsage()
        {
            //Usage for the user!
            string exeName = Process.GetCurrentProcess().ProcessName;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"Convert PKG to Zip: {exeName} --extract [pkgFile] [zipFile]");
            Console.WriteLine($"Example: {exeName} --extract scene.pkg result.zip");
            Console.WriteLine($"Convert Zip to PKG: {exeName} --repack [zipFile] [pkgFile]");
            Console.WriteLine($"Example: {exeName} --repack result.zip scene.pkg");
            Environment.Exit(0);
        }
    }
}
