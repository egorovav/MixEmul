using System.Collections.Generic;
using MixLib.Instruction;

namespace MixLib
{
	public class LoaderInstructions
	{
		private SortedDictionary<string, LoaderInstruction> mInstructions = new SortedDictionary<string, LoaderInstruction>();

		public LoaderInstructions()
		{
			addInstruction("ORIG", LoaderInstruction.Operations.SetLocationCounter, false);
			addInstruction("CON", LoaderInstruction.Operations.SetMemoryWord, false);
			addInstruction("ALF", LoaderInstruction.Operations.SetMemoryWord, true);
			addInstruction("END", LoaderInstruction.Operations.SetProgramCounter, false);
		}

		private void addInstruction(string mnemonic, LoaderInstruction.Operations operation, bool alphanumeric)
		{
			mInstructions.Add(mnemonic, new LoaderInstruction(mnemonic, operation, alphanumeric));
		}

		public LoaderInstruction this[string mnemonic]
		{
			get
			{
				return mInstructions.ContainsKey(mnemonic) ? mInstructions[mnemonic] : null;
			}
		}
	}
}
