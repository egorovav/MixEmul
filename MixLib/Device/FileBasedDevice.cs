using System.IO;
using MixLib.Device.Settings;
using MixLib.Device.Step;
using MixLib.Events;
using MixLib.Interrupts;
using MixLib.Type;

namespace MixLib.Device
{
	public abstract class FileBasedDevice : MixDevice
	{
		public const string FileNameExtension = "mixdev";

		private string mFileNamePrefix;
		private string mFilePath;

		protected StreamStatus StreamStatus { get; set; }

		protected FileBasedDevice(int id, string fileNamePrefix)
			: base(id)
		{
			mFileNamePrefix = fileNamePrefix;
			mFilePath = null;
			StreamStatus = new StreamStatus();
			StreamStatus.ReportingEvent += new ReportingEventHandler(streamStatus_Reporting);
		}

        public string DefaultFileName => mFileNamePrefix + base.Id + "." + FileNameExtension;

        public static FileStream OpenStream(string fileName, FileMode fileMode, FileAccess fileAccess, FileShare fileShare) =>
            new FileStream(fileName, fileMode, fileAccess, fileShare);

        public void CloseStream() => StreamStatus.CloseStream();

        private void streamStatus_Reporting(object sender, ReportingEventArgs args) => OnReportingEvent(args);

        protected override DeviceStep.Instance GetCurrentStepInstance()
		{
			if (base.CurrentStep is StreamStep)
			{
				return ((StreamStep)base.CurrentStep).CreateStreamInstance(StreamStatus);
			}
			return base.GetCurrentStepInstance();
		}

		public override void Reset()
		{
			StreamStatus.Reset();
			base.Reset();
		}

		public override void StartInput(IMemory memory, int mValue, int sector, InterruptQueueCallback callback)
		{
			StreamStatus.FileName = FilePath;
			base.StartInput(memory, mValue, sector, callback);
		}

		public override void StartIoc(int mValue, int sector, InterruptQueueCallback callback)
		{
			StreamStatus.FileName = FilePath;
			base.StartIoc(mValue, sector, callback);
		}

		public override void StartOutput(IMemory memory, int mValue, int sector, InterruptQueueCallback callback)
		{
			StreamStatus.FileName = FilePath;
			base.StartOutput(memory, mValue, sector, callback);
		}

		public string FilePath
		{
			get
			{
				string str = (mFilePath == null) ? DefaultFileName : mFilePath;
				return Path.Combine(DeviceSettings.DeviceFilesDirectory, str);
			}
			set
			{
				mFilePath = value;
			}
		}
	}
}
