using System;

namespace WallpaperEnginePKGtoZip
{
    class Program
    {
        public static string Version = "v1.0";
        public static readonly string ZipComment = $"┌────────────────────────────────────────────────────────────┐\n│              This zip was created in program:              │ \n├────────────────────────────────────────────────────────────┤\n│         Wallpaper Engine PKG to Zip and back  [{Version}]       │\n├────────────────────────────────────────────────────────────┤\n│ https://github.com/Parsa307/Wallpaper-Engine-PKG-to-Zip │\n╘════════════════════════════════════════════════════════════╛\n";

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

            string pkg = args[1];
            string zip = args[2];

            PkgConverter converter = null;

            bool convertToZip;

            if (args[0].ToLower() == "--pkgtozip")
            {
                convertToZip = true;
            }
            else if (args[0].ToLower() == "--ziptopkg")
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
                        converter = new PkgConverter(pkg, zip, convertToZip);
                    }
                    catch (PkgConverter.PkgConverterException ex)
                    {
                        //Error handling
                        Console.ForegroundColor = ConsoleColor.Red;
                        {
                            switch (ex.Error)
                            {
                                case PkgConverter.Error.PKG_FILE_NOT_FOUND:
                                    Console.WriteLine($"PKG file: '{pkg}' not found! You are sure about correctness of this path?");
                                    break;
                                case PkgConverter.Error.FAILED_TO_CREATE_FILE_STREAM:
                                    Console.WriteLine($"Failed to create file streams for pkg: '{pkg}' and zip: '{zip}' - Message:[{ex.SrcMsg}]");
                                    break;
                                case PkgConverter.Error.FAILED_TO_OPEN_ZIP_ARCHIVE:
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
                    catch (PkgConverter.PkgConverterException ex)
                    {
                        //Error handling
                        Console.ForegroundColor = ConsoleColor.Red;
                        switch (ex.Error)
                        {
                            case PkgConverter.Error.UNHANDLED_EXCEPTION:
                                Console.WriteLine($"Unhandled exception occured! - Message:[{ex.SrcMsg}]");
                                break;
                            case PkgConverter.Error.PKG_FILE_CORRUPTED:
                                Console.WriteLine($"Pkg file: '{pkg}' corrupted or unhandled error! - Message:[{ex.SrcMsg}]");
                                break;
                            case PkgConverter.Error.INVALID_PKG_FILE_SIGNATURE:
                                Console.WriteLine($"Unknown pkg signature - [{ex.SrcMsg}]");
                                break;
                            case PkgConverter.Error.FAILED_SEEKING_PKG_FILE:
                                Console.WriteLine($"Failed seeking in pkg file - [{ex.SrcMsg}]");
                                break;
                            case PkgConverter.Error.FAILED_READING_PKG_FILE:
                                Console.WriteLine($"Failed reading pkg file! - Message:[{ex.SrcMsg}]");
                                break;
                            case PkgConverter.Error.READED_LENGHT_NOT_EQUALS_NEED_LENGHT:
                                Console.WriteLine($"Readed length != Need length - Message:[{ex.SrcMsg}]");
                                break;
                            case PkgConverter.Error.FAILED_WRITING_INTO_ZIP_FILE:
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
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Usage: \"WallpaperEnginePKGtoZip.exe\" [mode] [pkgFile] [zipFile]");
            Console.WriteLine("pkgFile      Wallpaper Engine \".pkg\" file path");
            Console.WriteLine("zipFile      Archive \".zip\" file path");
            Console.WriteLine("Example: \"WallpaperEnginePKGtoZip.exe\" --pkgtozip scene.pkg result.zip");
            Environment.Exit(0);
        }
    }
}
