namespace WallpaperEnginePKGtoZip
{
    public class PKGInfo
    {
        public string Signature;
        public string FilePath;     
        public int FilesCount;
        public int Offset;
        public List<FileInfo> Files = new List<FileInfo>();

        public PKGInfo() { }

        public PKGInfo(string pkgFilePath)
        {
            this.FilePath = pkgFilePath;
        }

        public struct FileInfo
        {
            public string Path;
            public int Offset;
            public int Lenght;
        }
    }
}
