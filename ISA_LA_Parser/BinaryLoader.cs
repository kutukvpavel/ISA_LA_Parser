using System;
using System.IO;

namespace ISA_LA_Parser
{
    public class BinaryLoader
    {
        public BinaryLoader(string filePath, int chunkSize = (int)100E6)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException();
            FilePath = filePath;
            _dataLeft = (new FileInfo(filePath)).Length;
            ChunkSize = chunkSize;
            Data = new byte[chunkSize];
            _stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        #region Private

        private long _dataLeft = 0;

        private FileStream _stream;

        #endregion

        #region Properties

        public int ChunkSize { get; set; }

        public byte[] Data { get; private set; }

        public string FilePath { get; }

        public bool DataAvailable { get { return _dataLeft > 0; } }

        #endregion

        #region Public Methods

        public void LoadNextChunk()
        {
            if (_dataLeft < ChunkSize)
            {
                Data = new byte[_dataLeft];
            }
            _dataLeft -= _stream.Read(Data, 0, Data.Length);
        }

        #endregion
    }
}
