﻿using System;

namespace MixLib.Type
{
	public class MemoryView : IMemory
	{
		int mMinIndex;
		int mMaxIndex;
		int mIndexOffset;

		public IMemory SourceMemory { get; private set; }

		public MemoryView(IMemory sourceMemory)
	: this(sourceMemory, sourceMemory.MinWordIndex, sourceMemory.MaxWordIndex, 0) { }

		public MemoryView(IMemory sourceMemory, int indexOffset)
			: this(sourceMemory, sourceMemory.MinWordIndex, sourceMemory.MaxWordIndex, indexOffset) { }

		public MemoryView(IMemory sourceMemory, int minIndex, int maxIndex, int indexOffset)
		{
			SourceMemory = sourceMemory;

			ValidateParameters(minIndex, maxIndex, indexOffset);

			mMinIndex = minIndex;
			mMaxIndex = maxIndex;
			mIndexOffset = indexOffset;
		}

		public int WordCount => mMaxIndex - mMinIndex + 1;

		public long MaxProfilingTickCount => SourceMemory.MaxProfilingTickCount;

		public long MaxProfilingExecutionCount => SourceMemory.MaxProfilingExecutionCount;

		public MemoryFullWord GetRealWord(int index)
		{
			return SourceMemory.GetRealWord(index + mIndexOffset);
		}

		public void ResetRealWord(int index)
		{
			SourceMemory.ResetRealWord(index + mIndexOffset);
		}

		public void ClearRealWordSourceLine(int index)
		{
			SourceMemory.ClearRealWordSourceLine(index + mIndexOffset);
		}

		void ValidateParameters(int minIndex, int maxIndex, int offset)
		{
			if (minIndex + offset < SourceMemory.MinWordIndex)
			{
				throw new IndexOutOfRangeException("MinWordIndex + IndexOffset must be greater than or equal to the MinWordIndex of SourceMemory");
			}

			if (maxIndex + offset > SourceMemory.MaxWordIndex)
			{
				throw new IndexOutOfRangeException("MaxWordIndex + IndexOffset must be less than or equal to the MaxWordIndex of SourceMemory");
			}
		}

		public void ClearSourceLines()
		{
			for (int index = mMinIndex; index <= mMaxIndex; index++)
			{
				ClearRealWordSourceLine(index);
			}
		}

		public void Reset()
		{
			for (int index = mMinIndex; index <= mMaxIndex; index++)
			{
				ResetRealWord(index);
			}
		}

		public SearchResult FindMatch(SearchParameters options)
		{
			if (options.SearchFromWordIndex < mMinIndex || options.SearchFromWordIndex > mMaxIndex)
			{
				return null;
			}

			var filteredOptions = new SearchParameters
			{
				SearchText = options.SearchText,
				SearchFields = options.SearchFields,
				MatchWholeWord = options.MatchWholeWord,
				WrapSearch = options.WrapSearch,
				IncludeOriginalSource = options.IncludeOriginalSource,
				SearchFromWordIndex = options.SearchFromWordIndex + mIndexOffset,
				SearchFromField = options.SearchFromField,
				SearchFromFieldIndex = options.SearchFromFieldIndex
			};

			var result = SourceMemory.FindMatch(filteredOptions);

			if (result == null)
			{
				return null;
			}

			result.WordIndex -= mIndexOffset;

			if (result.WordIndex >= mMinIndex && result.WordIndex <= mMaxIndex)
			{
				return result;
			}

			if (!options.WrapSearch || options.SearchFromWordIndex == mMinIndex)
			{
				return null;
			}

			filteredOptions.SearchFromWordIndex = mMinIndex + mIndexOffset;
			filteredOptions.WrapSearch = false;

			result = SourceMemory.FindMatch(filteredOptions);

			return result != null && (result.WordIndex -= mIndexOffset) < options.SearchFromWordIndex ? result : null;
		}

		public IMemoryFullWord this[int index]
		{
			get => SourceMemory[index + mIndexOffset];
			set => SourceMemory[index + mIndexOffset] = value;
		}

		public int MinWordIndex
		{
			get => mMinIndex;
			set
			{
				ValidateParameters(value, mMaxIndex, mIndexOffset);

				mMinIndex = value;
			}
		}

		public int MaxWordIndex
		{
			get => mMaxIndex;
			set
			{
				ValidateParameters(mMinIndex, value, mIndexOffset);

				mMaxIndex = value;
			}
		}

		public int IndexOffset
		{
			get => mIndexOffset;
			set
			{
				ValidateParameters(mMinIndex, mMaxIndex, value);

				mIndexOffset = value;
			}
		}
	}
}
