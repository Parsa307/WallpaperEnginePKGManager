﻿using System.Collections.Generic;

namespace WallpaperEnginePKGtoZip
{
    public class PkgInfo
    {
        public string Signature;
        public string FilePath;     
        public int FilesCount;
        public int Offset;
        public List<FileInfo> Files = new List<FileInfo>();

        public PkgInfo() { }

        public PkgInfo(string pkgFilePath)
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
