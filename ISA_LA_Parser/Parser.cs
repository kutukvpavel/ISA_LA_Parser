using System;
using System.Collections.Generic;
using System.Text;

namespace ISA_LA_Parser
{
    public class Parser
    {
        private const byte _targetBit = 0;
        private const byte _AENBit = 2;
        private const byte _memBit = 1;
        private const byte _SBHEBit = 3;
        private const byte _BALEBit = 4;

        public Parser(byte[] data)
        {
            Data = data;
        }

        #region Private        

        private bool SkipUntilTargetAddressRangeDetected()
        {
            for ( ; CurrentIndex < Data.Length; CurrentIndex += 2) //16 channels
            {
                if (((Data[CurrentIndex] & (1 << _targetBit)) != 0) &&
                    ((Data[CurrentIndex] & (1 << _AENBit)) == 0)) break; //Target & !AEN
            }
            return CurrentIndex != Data.Length;
        }

        private bool GetCurrentBALE()
        {
            return (Data[CurrentIndex] & (1 << _BALEBit)) != 0;
        }

        private bool SkipUntilFallingEdgeOfBALE()
        {
            bool lastBALE = GetCurrentBALE();
            CurrentIndex += 2;
            for ( ; CurrentIndex < Data.Length; CurrentIndex += 2) //16 channels
            {
                bool currentBALE = GetCurrentBALE();
                if (!currentBALE && (lastBALE ^ currentBALE)) //Negative edge detector 
                {
                    break;
                }
                else
                {
                    lastBALE = currentBALE;
                }
            }
            return CurrentIndex != Data.Length;
        }

        private Transaction CreateTransactionAtCurrentIndex()
        {
            return new Transaction(
                (Data[CurrentIndex + 1] << 8) | (Data[CurrentIndex] >> 5),
                (Data[CurrentIndex] & (1 << _memBit)) != 0,
                (Data[CurrentIndex] & (1 << _AENBit)) != 0,
                (Data[CurrentIndex] & (1 << _SBHEBit)) != 0 //TODO: SBHE polarity?
                );
        }

        #endregion

        #region Properties

        public int CurrentIndex { get; private set; } = 0;
        public bool Finished { get; private set; } = false;
        public byte[] Data { get; }

        public List<Transaction> ParsedData { get; } = new List<Transaction>(1000);

        #endregion

        #region Public Methods

        public void Parse()
        {
            Finished = false;
            while (SkipUntilTargetAddressRangeDetected())
            {
                if (!SkipUntilFallingEdgeOfBALE()) break;
                CurrentIndex += 4; //Skip 125nS = 1 ISA 8MHz clock cycle, usually enough to get nMEM(W/R) / nIO(W/R) signals
                ParsedData.Add(CreateTransactionAtCurrentIndex());
            }
            Finished = true;
        }

        #endregion
    }

    public struct Transaction
    {
        public Transaction(int addr, bool mem, bool dma, bool sbhe)
        {
            Address = addr;
            Properties = TransactionProperties.None;
            if (mem) Properties |= TransactionProperties.Memory;
            if (dma) Properties |= TransactionProperties.DMA;
            if (sbhe) Properties |= TransactionProperties.SBHE;
        }

        int Address;
        TransactionProperties Properties;
    }

    [Flags]
    public enum TransactionProperties : byte
    {
        None = 0,
        Memory,
        DMA,
        SBHE
    }
}
