﻿using MixLib.Modules;
using MixLib.Modules.Settings;
using MixLib.Type;
using System;

namespace MixLib.Instruction
{
	public static class FloatingPointInstructions
	{
		const int faddOpcode = 1;
		const int fsubOpcode = 2;
		const int fmulOpcode = 3;
		const int fdivOpcode = 4;
		const int fcmpOpcode = 56;
		public const string FcmpMnemonic = "FCMP";

		static ExecutionStatus mExecutionStatus;
		static int[] mPrenormOpcodes = { faddOpcode, fsubOpcode, fmulOpcode, fdivOpcode };

		public static bool DoFloatingPoint(ModuleBase module, MixInstruction.Instance instance)
		{
			if (!(module is Mix))
			{
				module.ReportRuntimeError(string.Format("Floating point instruction {0} is only available within Mix", instance.Instruction.Mnemonic));
				return false;
			}

			var mix = (Mix)module;

			FloatingPointModule floatingPointModule = mix.FloatingPointModule;
			if (floatingPointModule == null)
			{
				module.ReportRuntimeError(string.Format("Instruction {0} requires floating point module to be enabled", instance.Instruction.Mnemonic));
				return false;
			}

			if (mExecutionStatus != null && mExecutionStatus.ProgramCounter != module.ProgramCounter)
			{
				mExecutionStatus = null;
			}

			if (mExecutionStatus == null)
			{
				mExecutionStatus = new ExecutionStatus(module.Mode, module.ProgramCounter, instance.Instruction.Mnemonic);
			}

			if (mExecutionStatus.CurrentStep == ExecutionStatus.Step.Initialize)
			{
				mExecutionStatus.RAValue = module.Registers.RA.FullWordValue;

				bool prenormInstruction = Array.IndexOf(mPrenormOpcodes, instance.MixInstruction.Opcode) != -1;
				bool fcmpInstruction = !prenormInstruction && instance.MixInstruction.Opcode == fcmpOpcode;

				if (prenormInstruction || fcmpInstruction)
				{
					var indexedAddress = InstructionHelpers.GetValidIndexedAddress(module, instance.AddressValue, instance.Index);
					if (indexedAddress == int.MinValue)
					{
						mExecutionStatus = null;
						return false;
					}

					mExecutionStatus.ParameterValue = module.Memory[indexedAddress];
				}

				module.Registers.SaveToMemory(floatingPointModule.FullMemory, ModuleSettings.FloatingPointMemoryWordCount, 0);

				if (!floatingPointModule.ValidateCall(mExecutionStatus.Mnemonic))
				{
					module.ReportRuntimeError("Unable to initialize floating point module");
					mExecutionStatus = null;

					return false;
				}

				module.Mode = ModuleBase.RunMode.Module;

				if (prenormInstruction)
				{
					mExecutionStatus.CurrentStep = ExecutionStatus.Step.PrenormRA;

					if (floatingPointModule.PreparePrenorm(mExecutionStatus.RAValue))
					{
						module.ReportBreakpointReached();
						return false;
					}
				}
				else
				{
					mExecutionStatus.CurrentStep = ExecutionStatus.Step.ExecuteInstruction;

					if (fcmpInstruction
						? floatingPointModule.PrepareCall(mExecutionStatus.Mnemonic, mExecutionStatus.RAValue, mExecutionStatus.ParameterValue)
						: floatingPointModule.PrepareCall(mExecutionStatus.Mnemonic, mExecutionStatus.RAValue))
					{
						module.ReportBreakpointReached();
						return false;
					}
				}
			}

			mExecutionStatus.OverflowDetected |= floatingPointModule.Tick();

			switch (floatingPointModule.Status)
			{
				case ModuleBase.RunStatus.Idle:

					switch (mExecutionStatus.CurrentStep)
					{
						case ExecutionStatus.Step.PrenormRA:
							mExecutionStatus.RAValue = floatingPointModule.Registers.RA.FullWordValue;
							mExecutionStatus.CurrentStep = ExecutionStatus.Step.PrenormParameter;

							if (floatingPointModule.PreparePrenorm(mExecutionStatus.ParameterValue))
							{
								module.ReportBreakpointReached();
								return false;
							}

							break;

						case ExecutionStatus.Step.PrenormParameter:
							mExecutionStatus.ParameterValue = floatingPointModule.Registers.RA.FullWordValue;
							mExecutionStatus.CurrentStep = ExecutionStatus.Step.ExecuteInstruction;

							if (floatingPointModule.PrepareCall(mExecutionStatus.Mnemonic, mExecutionStatus.RAValue, mExecutionStatus.ParameterValue))
							{
								module.ReportBreakpointReached();
								return false;
							}

							break;

						case ExecutionStatus.Step.ExecuteInstruction:
							mExecutionStatus.RAValue = floatingPointModule.Registers.RA.FullWordValue;
							Registers.CompValues comparatorValue = floatingPointModule.Registers.CompareIndicator;
							module.Registers.LoadFromMemory(floatingPointModule.FullMemory, ModuleSettings.FloatingPointMemoryWordCount);
							module.Registers.OverflowIndicator = mExecutionStatus.OverflowDetected;

							if (mExecutionStatus.Mnemonic == FcmpMnemonic)
							{
								module.Registers.CompareIndicator = comparatorValue;
							}
							else
							{
								module.Registers.RA.Magnitude = mExecutionStatus.RAValue.Magnitude;
								module.Registers.RA.Sign = mExecutionStatus.RAValue.Sign;
							}

							module.Mode = ModuleBase.RunMode.Normal;

							mExecutionStatus = null;

							return true;
					}

					break;

				case ModuleBase.RunStatus.BreakpointReached:
					module.ReportBreakpointReached();

					break;

				case ModuleBase.RunStatus.InvalidInstruction:
				case ModuleBase.RunStatus.RuntimeError:
					module.ReportRuntimeError(string.Format("Floating point module failed to execute instruction {0}", mExecutionStatus.Mnemonic));
					module.Mode = mExecutionStatus.Mode;

					mExecutionStatus = null;

					break;
			}

			return false;
		}

		class ExecutionStatus
		{
			public ModuleBase.RunMode Mode { get; private set; }
			public string Mnemonic { get; private set; }
			public int ProgramCounter { get; private set; }
			public Step CurrentStep { get; set; }
			public IFullWord ParameterValue { get; set; }
			public IFullWord RAValue { get; set; }
			public bool OverflowDetected { get; set; }

			public ExecutionStatus(ModuleBase.RunMode mode, int programCounter, string mnemonic)
			{
				Mode = mode;
				ProgramCounter = programCounter;
				Mnemonic = mnemonic;
				CurrentStep = Step.Initialize;
				OverflowDetected = false;
			}

			public enum Step
			{
				Initialize,
				PrenormRA,
				PrenormParameter,
				ExecuteInstruction
			}
		}
	}
}
