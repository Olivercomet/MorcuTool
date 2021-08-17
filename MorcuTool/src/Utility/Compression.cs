using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorcuTool
{
    public class Compression
    {

        public static byte[] Compress_QFS(byte[] filebytes)
        {
            byte[] res;
            bool smaller = Tiger.DBPFCompression.Compress(filebytes, out res);
            return smaller ? res : filebytes;
        }

        public static byte[] Decompress_QFS(byte[] filebytes)
        {
            int currentoffset = 0;

            currentoffset += 0x02; //skip 10FB header

            int uncompressedsize = (filebytes[currentoffset] * 0x10000) + (filebytes[currentoffset + 1] * 0x100) + filebytes[currentoffset + 2];

            byte[] output = new byte[uncompressedsize];

            currentoffset += 0x03;

            byte cc = 0; //control byte
            int len = filebytes.Length;
            int numplain = 0; ;
            int numcopy = 0;
            int offset = 0;
            byte byte1 = 0;
            byte byte2 = 0;
            byte byte3 = 0;

            int output_pos = 0;

            while (output_pos < uncompressedsize)
            {
                cc = filebytes[currentoffset];

                len--;

                if (cc >= 0xFC)
                {
                    numplain = cc & 0x03;
                    if (numplain > len)
                    { numplain = len; }
                    numcopy = 0;
                    offset = 0;
                    currentoffset++;
                }
                else if (cc >= 0xE0)
                {
                    numplain = (cc - 0xdf) << 2;
                    numcopy = 0;
                    offset = 0;
                    currentoffset++;
                }
                else if (cc >= 0xC0)
                {
                    len -= 3;
                    byte1 = filebytes[currentoffset + 1];
                    byte2 = filebytes[currentoffset + 2];
                    byte3 = filebytes[currentoffset + 3];
                    numplain = cc & 0x03;
                    numcopy = ((cc & 0x0c) << 6) + 5 + byte3;
                    offset = ((cc & 0x10) << 12) + (byte1 << 8) + byte2;
                    currentoffset += 4;
                }
                else if (cc >= 0x80)
                {
                    len -= 2;
                    byte1 = filebytes[currentoffset + 1];
                    byte2 = filebytes[currentoffset + 2];
                    numplain = (byte1 & 0xc0) >> 6;
                    numcopy = (cc & 0x3f) + 4;
                    offset = ((byte1 & 0x3f) << 8) + byte2;
                    currentoffset += 3;
                }
                else
                {
                    len -= 1;
                    byte1 = filebytes[currentoffset + 1];
                    numplain = (cc & 0x03);
                    numcopy = ((cc & 0x1c) >> 2) + 3;
                    offset = ((cc & 0x60) << 3) + byte1;
                    currentoffset += 2;
                }
                len -= numplain;

                // This section basically copies the parts of the string to the end of the buffer:
                if (numplain > 0)
                {
                    for (int i = 0; i < numplain; i++)
                    {
                        output[output_pos] = filebytes[currentoffset];
                        currentoffset++;
                        output_pos++;
                    }
                }

                int fromoffset = output_pos - (offset + 1); // 0 == last char
                for (int i = 0; i < numcopy; i++)     //copy bytes from earlier in the output
                {
                    output[output_pos] = output[fromoffset + i];
                    output_pos++;
                }
            }
            return output;
        }
    }

    /*
     * The following code was provided to S3PI by Tiger
    **/

    namespace Tiger
    {
        class DBPFCompression
        {
            public DBPFCompression(int level)
            {
                mTracker = CreateTracker(level, out mBruteForceLength);
            }

            public DBPFCompression(int blockinterval, int lookupstart, int windowlength, int bucketdepth, int bruteforcelength)
            {
                mTracker = CreateTracker(blockinterval, lookupstart, windowlength, bucketdepth);
                mBruteForceLength = bruteforcelength;
            }

            private int mBruteForceLength;
            private IMatchtracker mTracker;

            private byte[] mData;

            private int mSequenceSource;
            private int mSequenceLength;
            private int mSequenceDest;
            private bool mSequenceFound;

            public static bool Compress(byte[] data, out byte[] compressed)
            {
                DBPFCompression compressor = new DBPFCompression(5);
                compressed = compressor.Compress(data);
                return (compressed != null);
            }

            public static bool Compress(byte[] data, out byte[] compressed, int level)
            {
                DBPFCompression compressor = new DBPFCompression(level);
                compressed = compressor.Compress(data);
                return (compressed != null);
            }

            public byte[] Compress(byte[] data)
            {
                bool endisvalid = false;
                List<byte[]> compressedchunks = new List<byte[]>();
                int compressedidx = 0;
                int compressedlen = 0;

                if (data.Length < 16 || data.LongLength > UInt32.MaxValue)
                    return null;

                mData = data;

                try
                {
                    int lastbytestored = 0;

                    while (compressedidx < data.Length)
                    {
                        if (data.Length - compressedidx < 4)
                        {
                            // Just copy the rest
                            byte[] chunk = new byte[data.Length - compressedidx + 1];
                            chunk[0] = (byte)(0xFC | (data.Length - compressedidx));
                            Array.Copy(data, compressedidx, chunk, 1, data.Length - compressedidx);
                            compressedchunks.Add(chunk);
                            compressedidx += chunk.Length - 1;
                            compressedlen += chunk.Length;

                            endisvalid = true;
                            continue;
                        }

                        while (compressedidx > lastbytestored - 3)
                            mTracker.Addvalue(data[lastbytestored++]);

                        // Search ahead in blocks of 4 bytes for a match until one is found
                        // Record the best match if multiple are found
                        mSequenceSource = 0;
                        mSequenceLength = 0;
                        mSequenceDest = int.MaxValue;
                        mSequenceFound = false;
                        do
                        {
                            for (int loop = 0; loop < 4; loop++)
                            {
                                if (lastbytestored < data.Length)
                                    mTracker.Addvalue(data[lastbytestored++]);
                                FindSequence(lastbytestored - 4);
                            }
                        }
                        while (!mSequenceFound && lastbytestored + 4 <= data.Length);

                        if (!mSequenceFound)
                            mSequenceDest = mData.Length;

                        // If the next match is more than four bytes away, put in codes to read uncompressed data
                        while (mSequenceDest - compressedidx >= 4)
                        {
                            int tocopy = (mSequenceDest - compressedidx) & ~3;
                            if (tocopy > 112)
                                tocopy = 112;

                            byte[] chunk = new byte[tocopy + 1];
                            chunk[0] = (byte)(0xE0 | ((tocopy >> 2) - 1));
                            Array.Copy(data, compressedidx, chunk, 1, tocopy);
                            compressedchunks.Add(chunk);
                            compressedidx += tocopy;
                            compressedlen += chunk.Length;
                        }

                        if (mSequenceFound)
                        {
                            byte[] chunk = null;
                            /*
                             * 00-7F  0oocccpp oooooooo
                             *   Read 0-3
                             *   Copy 3-10
                             *   Offset 0-1023
                             *   
                             * 80-BF  10cccccc ppoooooo oooooooo
                             *   Read 0-3
                             *   Copy 4-67
                             *   Offset 0-16383
                             *   
                             * C0-DF  110cccpp oooooooo oooooooo cccccccc
                             *   Read 0-3
                             *   Copy 5-1028
                             *   Offset 0-131071
                             *   
                             * E0-FC  111ppppp
                             *   Read 4-128 (Multiples of 4)
                             *   
                             * FD-FF  111111pp
                             *   Read 0-3
                             */
                            //if (FindRunLength(data, seqstart, compressedidx + seqidx) < seqlength)
                            //{
                            //    break;
                            //}
                            while (mSequenceLength > 0)
                            {
                                int thislength = mSequenceLength;
                                if (thislength > 1028)
                                    thislength = 1028;
                                mSequenceLength -= thislength;

                                int offset = mSequenceDest - mSequenceSource - 1;
                                int readbytes = mSequenceDest - compressedidx;

                                mSequenceSource += thislength;
                                mSequenceDest += thislength;

                                if (thislength > 67 || offset > 16383)
                                {
                                    chunk = new byte[readbytes + 4];
                                    chunk[0] = (byte)(0xC0 | readbytes | (((thislength - 5) >> 6) & 0x0C) | ((offset >> 12) & 0x10));
                                    chunk[1] = (byte)((offset >> 8) & 0xFF);
                                    chunk[2] = (byte)(offset & 0xFF);
                                    chunk[3] = (byte)((thislength - 5) & 0xFF);
                                }
                                else if (thislength > 10 || offset > 1023)
                                {
                                    chunk = new byte[readbytes + 3];
                                    chunk[0] = (byte)(0x80 | ((thislength - 4) & 0x3F));
                                    chunk[1] = (byte)(((readbytes << 6) & 0xC0) | ((offset >> 8) & 0x3F));
                                    chunk[2] = (byte)(offset & 0xFF);
                                }
                                else
                                {
                                    chunk = new byte[readbytes + 2];
                                    chunk[0] = (byte)((readbytes & 0x3) | (((thislength - 3) << 2) & 0x1C) | ((offset >> 3) & 0x60));
                                    chunk[1] = (byte)(offset & 0xFF);
                                }

                                if (readbytes > 0)
                                    Array.Copy(data, compressedidx, chunk, chunk.Length - readbytes, readbytes);

                                compressedchunks.Add(chunk);
                                compressedidx += thislength + readbytes;
                                compressedlen += chunk.Length;
                            }
                        }
                    }

                    if (compressedlen + 6 < data.Length)
                    {
                        int chunkpos;
                        byte[] compressed;

                        if (data.Length > 0xFFFFFF)
                        {
                            // Activate the large data bit for > 16mb uncompressed data
                            compressed = new byte[compressedlen + 6 + (endisvalid ? 0 : 1)];
                            compressed[0] = 0x10 | 0x80; // 0x80 = length is 4 bytes
                            compressed[1] = 0xFB;
                            compressed[2] = (byte)(data.Length >> 24);
                            compressed[3] = (byte)(data.Length >> 16);
                            compressed[4] = (byte)(data.Length >> 8);
                            compressed[5] = (byte)(data.Length);
                            chunkpos = 6;
                        }
                        else
                        {
                            compressed = new byte[compressedlen + 5 + (endisvalid ? 0 : 1)];
                            compressed[0] = 0x10;
                            compressed[1] = 0xFB;
                            compressed[2] = (byte)(data.Length >> 16);
                            compressed[3] = (byte)(data.Length >> 8);
                            compressed[4] = (byte)(data.Length);
                            chunkpos = 5;
                        }

                        for (int loop = 0; loop < compressedchunks.Count; loop++)
                        {
                            Array.Copy(compressedchunks[loop], 0, compressed, chunkpos, compressedchunks[loop].Length);
                            chunkpos += compressedchunks[loop].Length;
                        }
                        if (!endisvalid)
                            compressed[compressed.Length - 1] = 0xfc;
                        return compressed;
                    }

                    return null;
                }
                finally
                {
                    mData = null;
                    mTracker.Reset();
                }
            }

            private void FindSequence(int startindex)
            {
                // Try a straight forward brute force first
                int end = -mBruteForceLength;
                if (startindex < mBruteForceLength)
                    end = -startindex;

                byte searchforbyte = mData[startindex];

                for (int loop = -1; loop >= end && mSequenceLength < 1028; loop--)
                {
                    byte curbyte = mData[loop + startindex];
                    if (curbyte != searchforbyte)
                        continue;

                    int len = FindRunLength(startindex + loop, startindex);

                    if (len <= mSequenceLength
                        || len < 3
                        || len < 4 && loop <= -1024
                        || len < 5 && loop <= -16384)
                        continue;

                    mSequenceFound = true;
                    mSequenceSource = startindex + loop;
                    mSequenceLength = len;
                    mSequenceDest = startindex;
                }

                // Use the look-up table next
                int matchloc;
                if (mSequenceLength < 1028 && mTracker.FindMatch(out matchloc))
                {
                    do
                    {
                        int len = FindRunLength(matchloc, startindex);
                        if (len < 5)
                            continue;

                        mSequenceFound = true;
                        mSequenceSource = matchloc;
                        mSequenceLength = len;
                        mSequenceDest = startindex;
                    }
                    while (mSequenceLength < 1028 && mTracker.Nextmatch(out matchloc));
                }
            }

            private int FindRunLength(int src, int dst)
            {
                int endsrc = src + 1;
                int enddst = dst + 1;
                while (enddst < mData.Length && mData[endsrc] == mData[enddst] && enddst - dst < 1028)
                {
                    endsrc++;
                    enddst++;
                }

                return enddst - dst;
            }

            private interface IMatchtracker
            {
                bool FindMatch(out int where);
                bool Nextmatch(out int where);
                void Addvalue(byte val);
                void Reset();
            }

            static IMatchtracker CreateTracker(int blockinterval, int lookupstart, int windowlength, int bucketdepth)
            {
                if (bucketdepth <= 1)
                    return new SingledepthMatchTracker(blockinterval, lookupstart, windowlength);
                else
                    return new DeepMatchTracker(blockinterval, lookupstart, windowlength, bucketdepth);
            }

            static IMatchtracker CreateTracker(int level, out int bruteforcelength)
            {
                switch (level)
                {
                    case 0:
                        bruteforcelength = 0;
                        return CreateTracker(4, 0, 16384, 1);
                    case 1:
                        bruteforcelength = 0;
                        return CreateTracker(2, 0, 32768, 1);
                    case 2:
                        bruteforcelength = 0;
                        return CreateTracker(1, 0, 65536, 1);
                    case 3:
                        bruteforcelength = 0;
                        return CreateTracker(1, 0, 131000, 2);
                    case 4:
                        bruteforcelength = 16;
                        return CreateTracker(1, 16, 131000, 2);
                    case 5:
                        bruteforcelength = 16;
                        return CreateTracker(1, 16, 131000, 5);
                    case 6:
                        bruteforcelength = 32;
                        return CreateTracker(1, 32, 131000, 5);
                    case 7:
                        bruteforcelength = 32;
                        return CreateTracker(1, 32, 131000, 10);
                    case 8:
                        bruteforcelength = 64;
                        return CreateTracker(1, 64, 131000, 10);
                    case 9:
                        bruteforcelength = 128;
                        return CreateTracker(1, 128, 131000, 20);
                    default:
                        return CreateTracker(5, out bruteforcelength);
                }
            }

            private class SingledepthMatchTracker : IMatchtracker
            {
                public SingledepthMatchTracker(int blockinterval, int lookupstart, int windowlength)
                {
                    mInterval = blockinterval;
                    if (lookupstart > 0)
                    {
                        mPendingValues = new UInt32[lookupstart / blockinterval];
                        mQueueLength = mPendingValues.Length * blockinterval;
                    }
                    else
                        mQueueLength = 0;
                    mInsertedValues = new UInt32[windowlength / blockinterval - lookupstart / blockinterval];
                    mWindowStart = -(mInsertedValues.Length + lookupstart / blockinterval) * blockinterval - 4;
                }

                public void Reset()
                {
                    mLookupTable.Clear();
                    mRunningValue = 0;

                    mRollingInterval = 0;
                    mWindowStart = -(mInsertedValues.Length + (mPendingValues != null ? mPendingValues.Length : 0)) * mInterval - 4;
                    mDataLength = 0;

                    mInitialized = false;
                    mInsertLocation = 0;
                    mPendingOffset = 0;

                    // No need to clear the arrays, the values will be ignored by the next time around
                }

                // Current 32 bit value of the last 4 bytes
                private UInt32 mRunningValue;

                // How often to insert into the table
                private int mInterval;
                // Avoid division by using a rolling count instead
                private int mRollingInterval;

                // How many bytes to queue up before adding it to the lookup table
                private int mQueueLength;

                // Queued up values for future matches
                private UInt32[] mPendingValues;
                private int mPendingOffset;

                // Bytes processed so far
                private int mDataLength;
                private int mWindowStart;

                // Four or more bytes read
                private bool mInitialized;

                // Values values pending removal
                private UInt32[] mInsertedValues;
                private int mInsertLocation;

                // Hash of seen values
                private Dictionary<UInt32, int> mLookupTable = new Dictionary<uint, int>();

                #region IMatchtracker Members

                // Never more than one match with a depth of 1
                public bool Nextmatch(out int where)
                {
                    where = 0;
                    return false;
                }

                public void Addvalue(byte val)
                {
                    if (mInitialized)
                    {
                        mRollingInterval++;
                        // Time to add a value to the table
                        if (mRollingInterval == mInterval)
                        {
                            mRollingInterval = 0;
                            // Remove a value from the table if the window just rolled past it
                            if (mWindowStart >= 0)
                            {
                                int idx;
                                if (mInsertLocation == mInsertedValues.Length)
                                    mInsertLocation = 0;
                                UInt32 oldval = mInsertedValues[mInsertLocation];
                                if (mLookupTable.TryGetValue(oldval, out idx) && idx == mWindowStart)
                                    mLookupTable.Remove(oldval);
                            }
                            if (mPendingValues != null)
                            {
                                // Pop the top of the queue and put it in the table
                                if (mDataLength > mQueueLength + 4)
                                {
                                    UInt32 poppedval = mPendingValues[mPendingOffset];
                                    mInsertedValues[mInsertLocation] = poppedval;
                                    mInsertLocation++;
                                    if (mInsertLocation > mInsertedValues.Length)
                                        mInsertLocation = 0;

                                    // Put it into the table
                                    mLookupTable[poppedval] = mDataLength - mQueueLength - 4;
                                }
                                // Push the next value onto the queue
                                mPendingValues[mPendingOffset] = mRunningValue;
                                mPendingOffset++;
                                if (mPendingOffset == mPendingValues.Length)
                                    mPendingOffset = 0;
                            }
                            else
                            {
                                // No queue, straight to the dictionary
                                mInsertedValues[mInsertLocation] = mRunningValue;
                                mInsertLocation++;
                                if (mInsertLocation > mInsertedValues.Length)
                                    mInsertLocation = 0;

                                mLookupTable[mRunningValue] = mDataLength - 4;
                            }
                        }
                    }
                    else
                    {
                        mRollingInterval++;
                        if (mRollingInterval == mInterval)
                            mRollingInterval = 0;
                        mInitialized = (mDataLength == 3);
                    }

                    mRunningValue = (mRunningValue << 8) | val;
                    mDataLength++;
                    mWindowStart++;
                }

                public bool FindMatch(out int where)
                {
                    return mLookupTable.TryGetValue(mRunningValue, out where);
                }

                #endregion
            }

            private class DeepMatchTracker : IMatchtracker
            {
                public DeepMatchTracker(int blockinterval, int lookupstart, int windowlength, int bucketdepth)
                {
                    mInterval = blockinterval;
                    if (lookupstart > 0)
                    {
                        mPendingValues = new UInt32[lookupstart / blockinterval];
                        mQueueLength = mPendingValues.Length * blockinterval;
                    }
                    else
                        mQueueLength = 0;
                    mInsertedValues = new UInt32[windowlength / blockinterval - lookupstart / blockinterval];
                    mWindowStart = -(mInsertedValues.Length + lookupstart / blockinterval) * blockinterval - 4;
                    mBucketDepth = bucketdepth;
                }

                public void Reset()
                {
                    mLookupTable.Clear();
                    mRunningValue = 0;

                    mRollingInterval = 0;
                    mWindowStart = -(mInsertedValues.Length + (mPendingValues != null ? mPendingValues.Length : 0)) * mInterval - 4;
                    mDataLength = 0;

                    mInitialized = false;
                    mInsertLocation = 0;
                    mPendingOffset = 0;

                    mCurrentMatch = null;

                    // No need to clear the arrays, the values will be ignored by the next time around
                }

                private int mBucketDepth;

                // Current 32 bit value of the last 4 bytes
                private UInt32 mRunningValue;

                // How often to insert into the table
                private int mInterval;
                // Avoid division by using a rolling count instead
                private int mRollingInterval;

                // How many bytes to queue up before adding it to the lookup table
                private int mQueueLength;

                // Queued up values for future matches
                private UInt32[] mPendingValues;
                private int mPendingOffset;

                // Bytes processed so far
                private int mDataLength;
                private int mWindowStart;

                // Four or more bytes read
                private bool mInitialized;

                // Values values pending removal
                private UInt32[] mInsertedValues;
                private int mInsertLocation;

                // Hash of seen values
                private Dictionary<UInt32, List<int>> mLookupTable = new Dictionary<uint, List<int>>();

                // Save allocating items unnecessarily
                private Stack<List<int>> mUnusedLists = new Stack<List<int>>();

                private List<int> mCurrentMatch;
                private int mCurrentMatchIndex;

                #region IMatchtracker Members

                public void Addvalue(byte val)
                {
                    if (mInitialized)
                    {
                        mRollingInterval++;
                        // Time to add a value to the table
                        if (mRollingInterval == mInterval)
                        {
                            mRollingInterval = 0;
                            // Remove a value from the table if the window just rolled past it
                            if (mWindowStart > 0)
                            {
                                List<int> locations;
                                if (mInsertLocation == mInsertedValues.Length)
                                    mInsertLocation = 0;
                                UInt32 oldval = mInsertedValues[mInsertLocation];
                                if (mLookupTable.TryGetValue(oldval, out locations) && locations[0] == mWindowStart)
                                {
                                    locations.RemoveAt(0);
                                    if (locations.Count == 0)
                                    {
                                        mLookupTable.Remove(oldval);
                                        mUnusedLists.Push(locations);
                                    }
                                }
                            }
                            if (mPendingValues != null)
                            {
                                // Pop the top of the queue and put it in the table
                                if (mDataLength > mQueueLength + 4)
                                {
                                    UInt32 poppedval = mPendingValues[mPendingOffset];
                                    mInsertedValues[mInsertLocation] = poppedval;
                                    mInsertLocation++;
                                    if (mInsertLocation > mInsertedValues.Length)
                                        mInsertLocation = 0;

                                    // Put it into the table
                                    List<int> locations;
                                    if (mLookupTable.TryGetValue(poppedval, out locations))
                                    {
                                        // Check if the bucket runneth over
                                        if (locations.Count == mBucketDepth)
                                            locations.RemoveAt(0);
                                    }
                                    else
                                    {
                                        // Allocate a new bucket
                                        if (mUnusedLists.Count > 0)
                                            locations = mUnusedLists.Pop();
                                        else
                                            locations = new List<int>();
                                        mLookupTable[poppedval] = locations;
                                    }
                                    locations.Add(mDataLength - mQueueLength - 4);
                                }
                                // Push the next value onto the queue
                                mPendingValues[mPendingOffset] = mRunningValue;
                                mPendingOffset++;
                                if (mPendingOffset == mPendingValues.Length)
                                    mPendingOffset = 0;
                            }
                            else
                            {
                                mInsertedValues[mInsertLocation] = mRunningValue;
                                mInsertLocation++;
                                if (mInsertLocation > mInsertedValues.Length)
                                    mInsertLocation = 0;

                                // Put it into the table
                                List<int> locations;
                                if (mLookupTable.TryGetValue(mRunningValue, out locations))
                                {
                                    // Check if the bucket runneth over
                                    if (locations.Count == mBucketDepth)
                                        locations.RemoveAt(0);
                                }
                                else
                                {
                                    // Allocate a new bucket
                                    if (mUnusedLists.Count > 0)
                                        locations = mUnusedLists.Pop();
                                    else
                                        locations = new List<int>();
                                    mLookupTable[mRunningValue] = locations;
                                }
                                locations.Add(mDataLength - 4);
                            }
                        }
                    }
                    else
                    {
                        mRollingInterval++;
                        if (mRollingInterval == mInterval)
                            mRollingInterval = 0;
                        mInitialized = (mDataLength == 3);
                    }
                    mRunningValue = (mRunningValue << 8) | val;
                    mDataLength++;
                    mWindowStart++;
                }

                public bool Nextmatch(out int where)
                {
                    if (mCurrentMatch != null && mCurrentMatchIndex < mCurrentMatch.Count)
                    {
                        where = mCurrentMatch[mCurrentMatchIndex];
                        mCurrentMatchIndex++;
                        return true;
                    }
                    where = -1;
                    return false;
                }

                public bool FindMatch(out int where)
                {
                    if (mLookupTable.TryGetValue(mRunningValue, out mCurrentMatch))
                    {
                        mCurrentMatchIndex = 1;
                        where = mCurrentMatch[0];
                        return true;
                    }
                    mCurrentMatch = null;
                    where = -1;
                    return false;
                }

                #endregion
            }
        }
    }
}

