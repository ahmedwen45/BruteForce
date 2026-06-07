using System;
using System.Collections.Generic;

namespace BruteForceApp
{
    /// <summary>
    /// Independently generates all possible character combinations
    /// from length 1 up to maxLength, starting at a given offset.
    /// Supports partitioned generation for multi-threaded use.
    /// </summary>
    public class CombinationGenerator
    {
        private readonly string _charset;
        private readonly int _maxLength;

        public CombinationGenerator(string charset, int maxLength = 6)
        {
            _charset = charset;
            _maxLength = maxLength;
        }

        /// <summary>
        /// Returns total number of combinations of exactly the given length.
        /// </summary>
        public long CountForLength(int length)
        {
            long count = 1;
            for (int i = 0; i < length; i++)
                count *= _charset.Length;
            return count;
        }

        /// <summary>
        /// Decodes a global index into a candidate string.
        /// Index 0 = first char of length 1, then wraps into length 2, etc.
        /// </summary>
        public string IndexToCandidate(long globalIndex)
        {
            // Find which length bucket this index falls into
            long offset = globalIndex;
            for (int len = 1; len <= _maxLength; len++)
            {
                long count = CountForLength(len);
                if (offset < count)
                    return DecodeIndex(offset, len);
                offset -= count;
            }
            return null; // exhausted
        }

        private string DecodeIndex(long index, int length)
        {
            char[] result = new char[length];
            int base_ = _charset.Length;
            for (int i = length - 1; i >= 0; i--)
            {
                result[i] = _charset[(int)(index % base_)];
                index /= base_;
            }
            return new string(result);
        }

        /// <summary>
        /// Total number of combinations across all lengths 1..maxLength.
        /// </summary>
        public long TotalCombinations()
        {
            long total = 0;
            for (int len = 1; len <= _maxLength; len++)
                total += CountForLength(len);
            return total;
        }

        /// <summary>
        /// Enumerates candidates assigned to a specific thread (partition).
        /// threadIndex: 0-based index of thread
        /// threadCount: total number of threads
        /// </summary>
        public IEnumerable<(long globalIndex, string candidate)> GetPartition(int threadIndex, int threadCount)
        {
            long total = TotalCombinations();
            for (long i = threadIndex; i < total; i += threadCount)
            {
                string candidate = IndexToCandidate(i);
                if (candidate == null) yield break;
                yield return (i, candidate);
            }
        }
    }
}
