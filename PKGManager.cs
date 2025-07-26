using System.Text;
using System.IO.Compression;

namespace WallpaperEnginePKGManager
{
    public class PKGManager
    {
        private PKGInfo _pkgInfo;
        private FileStream _pkgFileStream;
        private FileStream _zipFileStream;
        private ZipArchive _zipArchive;
        
        private bool _PKGtoZip;

        public PKGManager(string pkgFilePath, string zipFilePath, bool PKGtoZip)
        {
            this._PKGtoZip = PKGtoZip;
            if (PKGtoZip)
            {
                if (!File.Exists(pkgFilePath)) //Check exists pkg file?
                    throw new PKGManagerException(new FileNotFoundException(pkgFilePath), Error.PKG_FILE_NOT_FOUND);

                _pkgInfo = new PKGInfo(pkgFilePath);

                //Creating file streams
                try
                {
                    _pkgFileStream = new FileStream(pkgFilePath, FileMode.Open, FileAccess.Read);
                    _zipFileStream = new FileStream(zipFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                }
                catch (Exception ex)
                {
                    throw new PKGManagerException(ex, Error.FAILED_TO_CREATE_FILE_STREAM);
                }

                //Create zip archive
                try
                {
                    _zipArchive = new ZipArchive(_zipFileStream, ZipArchiveMode.Create);
                }
                catch (Exception ex)
                {
                    throw new PKGManagerException(ex, Error.FAILED_TO_CREATE_ZIP_ARCHIVE);
                }
            }
            else
            {
                if (!File.Exists(zipFilePath)) //Check exists pkg file?
                    throw new PKGManagerException(new FileNotFoundException(zipFilePath), Error.ZIP_FILE_NOT_FOUND);

                _pkgInfo = new PKGInfo(pkgFilePath);

                //Creating file streams
                try
                {
                    _zipFileStream = new FileStream(zipFilePath, FileMode.Open, FileAccess.ReadWrite);
                    _pkgFileStream = new FileStream(pkgFilePath, FileMode.OpenOrCreate, FileAccess.Write);
                }
                catch (Exception ex)
                {
                    throw new PKGManagerException(ex, Error.FAILED_TO_CREATE_FILE_STREAM);
                }

                //Create zip archive
                try
                {
                    _zipArchive = new ZipArchive(_zipFileStream, ZipArchiveMode.Read);
                }
                catch (Exception ex)
                {
                    throw new PKGManagerException(ex, Error.FAILED_TO_OPEN_ZIP_ARCHIVE);
                }
            }
        }

        private void CreatePKGInfoFromZip()
        {
            //Detecting original version of file
            _pkgInfo.Signature = DetectSignatureFromZip(); 
            if (string.IsNullOrEmpty(_pkgInfo.Signature))
            {
                _pkgInfo.Signature = "PKGV0001";
                Console.WriteLine($"PKGVersion: not detected, will be used \"PKGV0001\"");
            }
            else
                Console.WriteLine($"PKGVersion: \"{_pkgInfo.Signature}\"");
                
               
            _pkgInfo.FilePath = Path.GetFileName(_pkgFileStream.Name);
            _pkgInfo.FilesCount = _zipArchive.Entries.Count;

            //Precompute offset of start file in pkg
            _pkgInfo.Offset += 4 + Encoding.UTF8.GetByteCount(_pkgInfo.Signature)/*8*/ + 4; //signatureStringLenght + "signatureString" + filesCountInt
            foreach (var entry in _zipArchive.Entries)
                _pkgInfo.Offset += (4 + Encoding.UTF8.GetByteCount(entry.FullName) + 4 + 4); //pathStringLenght + "pathString" + offsetInt + lenghtInt

            //Generate tree of files
            int filesOffset = 0;
            foreach (var entry in _zipArchive.Entries)
            {
                _pkgInfo.Files.Add(new PKGInfo.FileInfo() { Path = entry.FullName, Lenght = (int)(entry.Length), Offset = filesOffset, });
                filesOffset += (int)(entry.Length);
            }
        }

        private void ZiptoPKG()
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            using (var bw = new BinaryWriter(_pkgFileStream, Encoding.UTF8, true))
            {
                Console.WriteLine($"Writing main signature and files count...");

                //Write signature
                byte[] signatureStringBytes = Encoding.UTF8.GetBytes(_pkgInfo.Signature);
                bw.Write(signatureStringBytes.Length); //Length of signature string in bytes (always is 8)
                bw.Write(signatureStringBytes); //Pkg signature

                //Write file count in
                bw.Write(_pkgInfo.FilesCount);

                Console.WriteLine($"Writing files tree...");

                //Create tree of files
                foreach (var file in _pkgInfo.Files)
                {
                    //Convert string to bytes
                    byte[] filePathBytes = Encoding.UTF8.GetBytes(file.Path);

                    //Write string length and th string itself
                    bw.Write(filePathBytes.Length);
                    bw.Write(filePathBytes);

                    //Write offset of this file in the package
                    bw.Write(file.Offset);

                    //Write length of file
                    bw.Write(file.Lenght);
                }

                Console.WriteLine($"Starting writing files data to pkg...\n");
                Console.ForegroundColor = ConsoleColor.DarkGreen;

                //Finally, write file data
                int filesPacked = 0;
                foreach (var entry in _zipArchive.Entries)
                {
                    //Open file entry in zip archive
                    using (var stream = Stream.Synchronized(entry.Open()))
                    {
                        //Unpack file from zip archive
                        byte[] readedBytes = new byte[entry.Length];
                        int readedCount = stream.Read(readedBytes, 0, readedBytes.Length);

                        if (readedCount != readedBytes.Length) //Throw a Exception if reading went wrong
                            throw new PKGManagerException(new ArgumentOutOfRangeException($"File lenght: {readedBytes.Length}, but readed: {readedCount}"), Error.READED_LENGHT_NOT_EQUALS_NEED_LENGHT);

                        //Write file data into pkg
                        bw.Write(readedBytes, 0, readedCount);
                    }

                    //Log in console
                    filesPacked++;
                    Console.WriteLine($"{filesPacked}:> {entry.FullName}");
                }
            }
        }

        private void ReadPKGInfo()
        {
            using (var br = new BinaryReader(_pkgFileStream, Encoding.UTF8, true))
            {
                //Read pkg file signature
                int signatureLength = br.ReadInt32();
                _pkgInfo.Signature = Encoding.UTF8.GetString(br.ReadBytes(signatureLength));

                if (!_pkgInfo.Signature.StartsWith("PKGV")) //Check if it's a PKG file?
                    throw new PKGManagerException(new InvalidDataException(_pkgInfo.Signature), Error.INVALID_PKG_FILE_SIGNATURE);

                    //Log the PKG Version
                    Console.WriteLine($"PKGVersion: {_pkgInfo.Signature}");

                //Read a file count in package
                _pkgInfo.FilesCount = br.ReadInt32();

                //Through all the files in the package
                for (int i = 0; i < _pkgInfo.FilesCount; i++)
                {
                    int pathLength = br.ReadInt32();
                    string path = Encoding.UTF8.GetString(br.ReadBytes(pathLength));
                    int offset = br.ReadInt32();
                    int lenght = br.ReadInt32();

                    _pkgInfo.Files.Add(new PKGInfo.FileInfo() { Path = path, Offset = offset, Lenght = lenght });
                }

                //We get the beginning of the contents of the files
                _pkgInfo.Offset = (int)(br.BaseStream.Position);
            }
        }

        public void PKGtoZip()
        {
            //Set signature of PKG to Zip comment
            SetSignaturetoZip();

            int filesPacked = 0;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            foreach (var file in _pkgInfo.Files)
            {
                //Create a new entry in the archive with the desired name
                var fileEntry = _zipArchive.CreateEntry(file.Path, CompressionLevel.NoCompression);
                using (Stream writer = Stream.Synchronized(fileEntry.Open()))
                {
                    //Go to the desired position in the package
                    try
                    {
                        _pkgFileStream.Seek(_pkgInfo.Offset + file.Offset, SeekOrigin.Begin);
                    }
                    catch (Exception ex)
                    {
                        throw new PKGManagerException(ex, Error.FAILED_SEEKING_PKG_FILE);
                    }

                    //Read...
                    byte[] binBytes = new byte[file.Lenght];
                    int readedCount = 0;
                    try
                    {
                        readedCount = _pkgFileStream.Read(binBytes, 0, file.Lenght);
                    }
                    catch (Exception ex)
                    {
                        throw new PKGManagerException(ex, Error.FAILED_READING_PKG_FILE);
                    }


                    if (readedCount != file.Lenght) //Throw a Exception if reading went wrong
                        throw new PKGManagerException(new ArgumentOutOfRangeException($"File lenght: {file.Lenght}, but readed: {readedCount}"), Error.READED_LENGHT_NOT_EQUALS_NEED_LENGHT);


                    //Write into zip archive!
                    try
                    {
                        writer.Write(binBytes, 0, readedCount);
                        writer.Flush();
                    }
                    catch (Exception ex)  {
                        throw new PKGManagerException(ex, Error.FAILED_WRITING_INTO_ZIP_FILE);
                    }
                }

                //Log into console
                filesPacked++;
                Console.WriteLine($"{filesPacked}:> {file.Path}");
            }
        }

        public string DetectSignatureFromZip()
        {
            try
            {
                string comment = _zipArchive.GetComment(Encoding.UTF8);
                if (comment != "")
                {
                    string findSignature = "PKGVersion: ";
                    int pkgVersionIndex = comment.IndexOf(findSignature) + findSignature.Length;
                    if (pkgVersionIndex > 0)
                        return comment.Substring(pkgVersionIndex, 8);
                }
            }
            catch (Exception ex)
            {
                var savedColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error reading zip archive comment! - Message:[{ex.Message}]");
                Console.ForegroundColor = savedColor;
            }

            return ""; //Not detected or exception
        }

        public void SetSignaturetoZip()
        {
            string pkgVersion = $"PKGVersion: {_pkgInfo.Signature}";
            _zipArchive.SetComment($"{pkgVersion}", Encoding.UTF8);
        }

        public void Convert()
        {
            if (disposed) //We cannot convert multiply times at one converter object
                throw new PKGManagerException(new ObjectDisposedException(GetType().Name), Error.ALREADY_CONVERTED);

            Console.ForegroundColor = ConsoleColor.Gray;
            if (_PKGtoZip)
            {
                Console.WriteLine($"Reading PKG: {_pkgInfo.FilePath}");

                try
                {
                    ReadPKGInfo(); //Read PKG
                }
                catch (PKGManagerException) //Rethrown converter exception
                {
                    throw;
                }
                catch (Exception ex) //Not converter exception
                {
                    throw new PKGManagerException(ex, Error.PKG_FILE_CORRUPTED);
                }

                //We write how many files are in the archive and begin packing in the zip archive (.zip)
                Console.WriteLine($"Files in PKG: {_pkgInfo.FilesCount}");
                Console.WriteLine($"Starting repacking to Zip: {Path.GetFileName(_zipFileStream.Name)}\n");

                try
                {
                    PKGtoZip();
                }
                catch (PKGManagerException) //Rethrown converter exception
                {
                    throw;
                }
                catch (Exception ex) //Not converter exception
                {
                    throw new PKGManagerException(ex, Error.UNHANDLED_EXCEPTION);
                }
                finally
                {
                    Dispose(); //Dispose all resourses
                }

                //Tell the user if the program succeeded
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine($"\nExtract & Repack succeeded");
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            //Zip to PKG
            else
            {
                Console.WriteLine($"Reading Zip: \"{Path.GetFileName(_zipFileStream.Name)}\"");

                try
                {
                    CreatePKGInfoFromZip(); //Create PkgInfo from zip
                }
                catch (PKGManagerException) //Rethrown converter exception
                {
                    throw;
                }
                catch (Exception ex) //Not converter exception
                {
                    throw new PKGManagerException(ex, Error.UNHANDLED_EXCEPTION);
                }

                //We write how many files are in the archive and begin packing into a .pkg
                Console.WriteLine($"Files in Zip: {_pkgInfo.FilesCount}");
                Console.WriteLine($"Starting repacking to PKG: \"{_pkgInfo.FilePath}\"\n");

                try
                {
                    ZiptoPKG();
                }
                catch (PKGManagerException) //Rethrown converter exception
                {
                    throw;
                }
                catch (Exception ex) //Not converter exception
                {
                    throw new PKGManagerException(ex, Error.UNHANDLED_EXCEPTION);
                }
                finally
                {
                    Dispose(); //Dispose all resourses
                }

                //Tell the user if the program succeeded
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine($"\nRepack succeeded!");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        #region DISPOSE
        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~PKGManager()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    //Release a resources
                    _zipArchive.Dispose();
                    _zipFileStream.Dispose();
                    _pkgFileStream.Dispose();
                }
                disposed = true;
            }
        }
        #endregion

        public class PKGManagerException : Exception
        {
            public Exception SourceException;
            public Error Error;
            public string SrcMsg => SourceException.Message;

            public PKGManagerException(Error Error)
            {
                this.Error = Error;
            }

            public PKGManagerException(Exception SourceException, Error Error)
            {
                this.SourceException = SourceException;
                this.Error = Error;
            }
        }

        public enum Error
        {
            NONE,
            UNHANDLED_EXCEPTION,
            INVALID_PKG_FILE_SIGNATURE,
            PKG_FILE_CORRUPTED,
            PKG_FILE_NOT_FOUND,
            ZIP_FILE_NOT_FOUND,
            FAILED_TO_CREATE_FILE_STREAM,
            FAILED_TO_CREATE_ZIP_ARCHIVE,
            FAILED_TO_OPEN_ZIP_ARCHIVE,
            FAILED_WRITING_INTO_ZIP_FILE,
            READED_LENGHT_NOT_EQUALS_NEED_LENGHT,
            FAILED_SEEKING_PKG_FILE,
            FAILED_READING_PKG_FILE,
            ALREADY_CONVERTED,
        }
    }
}
