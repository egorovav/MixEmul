using System;
using System.IO;
using MixLib.Device.Settings;
using MixLib.Device.Step;
using MixLib.Events;
using MixLib.Misc;
using MixLib.Type;

namespace MixLib.Device
{
	public class DiskDevice : FileBasedDevice
	{
		private const string shortName = "DSK";
		private const string fileNamePrefix = "disk";

		private const string initializationDescription = "Initializing disk";
		private const string openingDescription = "Starting data transfer with disk";
		private const string seekingDescription = "Seeking sector";

		public const long SectorCount = 4096;
		public const int WordsPerSector = 100;

		public DiskDevice(int id)
			: base(id, fileNamePrefix)
		{
			UpdateSettings();
		}

		public override void UpdateSettings()
		{
			int tickCount = DeviceSettings.GetTickCount(DeviceSettings.DiskInitialization);

			DeviceStep nextStep = new NoOpStep(tickCount, initializationDescription);
			base.FirstInputDeviceStep = nextStep;
			nextStep.NextStep = new openStreamStep();
			nextStep = nextStep.NextStep;
			nextStep.NextStep = new seekStep();
			nextStep = nextStep.NextStep;
			nextStep.NextStep = new BinaryReadStep(WordsPerSector);
			nextStep = nextStep.NextStep;
			nextStep.NextStep = new CloseStreamStep();
			nextStep = nextStep.NextStep;
			nextStep.NextStep = new MixDevice.WriteToMemoryStep(true, WordsPerSector);
			nextStep.NextStep.NextStep = null;

			nextStep = new NoOpStep(tickCount, initializationDescription);
			base.FirstOutputDeviceStep = nextStep;
			nextStep.NextStep = new MixDevice.ReadFromMemoryStep(true, WordsPerSector);
			nextStep = nextStep.NextStep;
			nextStep.NextStep = new openStreamStep();
			nextStep = nextStep.NextStep;
			nextStep.NextStep = new seekStep();
			nextStep = nextStep.NextStep;
			nextStep.NextStep = new BinaryWriteStep(WordsPerSector);
			nextStep = nextStep.NextStep;
			nextStep.NextStep = new CloseStreamStep();
			nextStep.NextStep.NextStep = null;

			nextStep = new NoOpStep(tickCount, initializationDescription);
			base.FirstIocDeviceStep = nextStep;
			nextStep.NextStep = new seekStep();
			nextStep.NextStep.NextStep = null;
		}

		public override int RecordWordCount
		{
			get
			{
				return WordsPerSector;
			}
		}

		public override string ShortName
		{
			get
			{
				return shortName;
			}
		}

		public override bool SupportsInput
		{
			get
			{
				return true;
			}
		}

		public override bool SupportsOutput
		{
			get
			{
				return true;
			}
		}

		public new static FileStream OpenStream(string fileName, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
		{
			FileStream stream = FileBasedDevice.OpenStream(fileName, fileMode, fileAccess, fileShare);
			long byteCount = SectorCount * WordsPerSector * (FullWord.ByteCount + 1);

			int bytesToWrite = (int)(byteCount - stream.Length);
			if (bytesToWrite > 0)
			{
				stream.Position = stream.Length;
				stream.Write(new byte[bytesToWrite], 0, bytesToWrite);
				stream.Flush();
			}

			return stream;
		}

		public static long CalculateBytePosition(long sector)
		{
			return sector * WordsPerSector * (FullWord.ByteCount + 1);
		}

		private class openStreamStep : StreamStep
		{
			public override StreamStep.Instance CreateStreamInstance(StreamStatus streamStatus)
			{
				return new Instance(streamStatus);
			}

			public override string StatusDescription
			{
				get
				{
					return openingDescription;
				}
			}

			private new class Instance : StreamStep.Instance
			{
				public Instance(StreamStatus streamStatus)
					: base(streamStatus)
				{
				}

				public override bool Tick()
				{
					try
					{
						FileStream stream = DiskDevice.OpenStream(base.StreamStatus.FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

						if (!base.StreamStatus.PositionSet)
						{
							stream.Position = 0L;
						}

						base.StreamStatus.Stream = stream;
					}
					catch (Exception exception)
					{
						OnReportingEvent(new ReportingEventArgs(Severity.Error, "exception while opening file " + base.StreamStatus.FileName + ": " + exception.Message));
					}

					return true;
				}
			}
		}

		private class seekStep : StreamStep
		{
			public override StreamStep.Instance CreateStreamInstance(StreamStatus streamStatus)
			{
				return new Instance(streamStatus);
			}

			public override string StatusDescription
			{
				get
				{
					return seekingDescription;
				}
			}

			private new class Instance : StreamStep.Instance
			{
				private long mTicksLeft;
				private const long unset = long.MinValue;

				public Instance(StreamStatus streamStatus)
					: base(streamStatus)
				{
					mTicksLeft = unset;
				}

				public override bool Tick()
				{
					long desiredPosition = CalculateBytePosition(base.Operands.Sector);
					if (desiredPosition != base.StreamStatus.Position)
					{
						if (mTicksLeft == unset)
						{
							mTicksLeft = DeviceSettings.GetTickCount(DeviceSettings.DiskSectorSeek);
						}

						mTicksLeft -= 1L;

						if (mTicksLeft > 0L)
						{
							return false;
						}

						if (desiredPosition < 0L)
						{
							desiredPosition = 0L;
						}
						else if (base.Operands.Sector >= SectorCount)
						{
							desiredPosition = (SectorCount - 1) * WordsPerSector * (FullWord.ByteCount + 1);
						}

						base.StreamStatus.Position = desiredPosition;
					}
					return true;
				}
			}
		}
	}
}
