using MixLib.Modules;
using MixLib.Type;

namespace MixLib.Instruction
{
	/// <summary>
	/// Methods for performing MIX address transfer instructions
	/// </summary>
	public static class AddressTransferInstructions
	{
		const byte opcodeBase = 48;

		/// <summary>
		/// Method for performing DECx instructions
		/// </summary>
		public static bool Decrease(ModuleBase module, MixInstruction.Instance instance)
		{
			return DoIncrease(module, instance, instance.MixInstruction.Opcode - opcodeBase, true);
		}

		static bool DoEnter(ModuleBase module, MixInstruction.Instance instance, int registerIndex, bool negateSign)
		{
			var indexedAddress = module.Registers.GetIndexedAddress(instance.AddressValue, instance.Index);
			Register register = module.Registers[registerIndex];

			if (negateSign)
			{
				indexedAddress = -indexedAddress;
			}

			register.LongValue = indexedAddress;

			if (indexedAddress == 0)
			{
				register.Sign = instance.Sign;
			}

			return true;
		}

		static bool DoIncrease(ModuleBase module, MixInstruction.Instance instance, int registerIndex, bool negateSign)
		{
			var indexedAddress = module.Registers.GetIndexedAddress(instance.AddressValue, instance.Index);
			Register register = module.Registers[registerIndex];
			long longValue = register.LongValue;

			if (negateSign)
			{
				indexedAddress = -indexedAddress;
			}

			longValue += indexedAddress;

			if (longValue != 0L)
			{
				register.Sign = longValue.GetSign();
				longValue = longValue.GetMagnitude();
			}

			if (longValue > register.MaxMagnitude)
			{
				if (register is IndexRegister)
				{
					module.ReportRuntimeError("Index register overflow");
					return false;
				}

				module.ReportOverflow();
				longValue &= register.MaxMagnitude;
			}

			register.MagnitudeLongValue = longValue;

			return true;
		}

		/// <summary>
		/// Method for performing ENTx instructions
		/// </summary>
		public static bool Enter(ModuleBase module, MixInstruction.Instance instance)
		{
			return DoEnter(module, instance, instance.MixInstruction.Opcode - opcodeBase, false);
		}

		/// <summary>
		/// Method for performing ENNx instructions
		/// </summary>
		public static bool EnterNegative(ModuleBase module, MixInstruction.Instance instance)
		{
			return DoEnter(module, instance, instance.MixInstruction.Opcode - opcodeBase, true);
		}

		/// <summary>
		/// Method for performing INCx instructions
		/// </summary>
		public static bool Increase(ModuleBase module, MixInstruction.Instance instance)
		{
			return DoIncrease(module, instance, instance.MixInstruction.Opcode - opcodeBase, false);
		}
	}
}
