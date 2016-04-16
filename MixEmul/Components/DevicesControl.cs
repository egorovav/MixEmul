using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MixGui.Events;
using MixLib.Device;

namespace MixGui.Components
{
	public class DevicesControl : UserControl
	{
		private DeviceStatusControl[] mDeviceControls;
		private MixLib.Devices mDevices;
		private int mLastSectionSize;
		private int mMaxSequenceSize;
		private Label mNoDevicesLabel;
		private int mSectionSize;
		private int mSequenceSize;
		private LayoutStructure mStructure;
		private ToolTip mToolTip;

		public event EventHandler<DeviceEventArgs> DeviceDoubleClick;

		public DevicesControl()
			: this(null, null)
		{
		}

		public DevicesControl(MixLib.Devices devices, LayoutStructure layout)
		{
			mStructure = layout;

			mNoDevicesLabel = new Label();
			mNoDevicesLabel.Location = new Point(0, 0);
			mNoDevicesLabel.Name = "mNoDevicesLabel";
			mNoDevicesLabel.Size = new Size(120, 16);
			mNoDevicesLabel.TabIndex = 0;
			mNoDevicesLabel.Text = "No devices connected";
			mNoDevicesLabel.TextAlign = ContentAlignment.MiddleCenter;

			base.Name = "DevicesControl";

			Devices = devices;
		}

		private void addDeviceControl(int index)
		{
			Orientations orientation = getOrientation();
			Breaks breaks = mStructure != null && index > 0 ? mStructure[index] : Breaks.None;

			int horizontalSpacing = getHorizontalSpacing();
			int verticalSpacing = getVerticalSpacing();

			if (index == 0)
			{
				mSequenceSize = 0;
				mSectionSize = 0;
				mLastSectionSize = 0;
				mMaxSequenceSize = 0;
			}

			Point controlLocation = new Point();
			DeviceStatusControl control = mDeviceControls[index];

			if (index == 0 || breaks == Breaks.Sequence)
			{
				if (orientation == Orientations.Horizontal)
				{
					controlLocation.X = mLastSectionSize;
					controlLocation.Y = mSequenceSize;
					mSequenceSize += control.Height + verticalSpacing;
					mSectionSize = Math.Max(mSectionSize, (mLastSectionSize + control.Width) + horizontalSpacing);
				}
				else
				{
					controlLocation.X = mSequenceSize;
					controlLocation.Y = mLastSectionSize;
					mSequenceSize += control.Width + horizontalSpacing;
					mSectionSize = Math.Max(mSectionSize, (mLastSectionSize + control.Height) + verticalSpacing);
				}
			}
			else if (breaks == Breaks.Section)
			{
				if (orientation == Orientations.Horizontal)
				{
					mLastSectionSize = mSectionSize;
					controlLocation.X = mLastSectionSize;
					controlLocation.Y = 0;
					mSequenceSize = control.Height + verticalSpacing;
					mSectionSize = (mLastSectionSize + control.Width) + horizontalSpacing;
				}
				else
				{
					mLastSectionSize = mSectionSize;
					controlLocation.X = 0;
					controlLocation.Y = mLastSectionSize;
					mSequenceSize = control.Width + horizontalSpacing;
					mSectionSize = (mLastSectionSize + control.Height) + verticalSpacing;
				}
			}
			else if (orientation == Orientations.Horizontal)
			{
				DeviceStatusControl previousControl = mDeviceControls[index - 1];
				controlLocation.X = (previousControl.Left + previousControl.Width) + horizontalSpacing;
				controlLocation.Y = previousControl.Top;
				mSequenceSize = Math.Max(mSequenceSize, (controlLocation.Y + control.Height) + verticalSpacing);
				mSectionSize = Math.Max(mSectionSize, (controlLocation.X + control.Width) + horizontalSpacing);
			}
			else
			{
				DeviceStatusControl previousControl = mDeviceControls[index - 1];
				controlLocation.X = previousControl.Left;
				controlLocation.Y = (previousControl.Top + previousControl.Height) + horizontalSpacing;
				mSequenceSize = Math.Max(mSequenceSize, (controlLocation.X + control.Width) + horizontalSpacing);
				mSectionSize = Math.Max(mSectionSize, (controlLocation.Y + control.Height) + verticalSpacing);
			}

			mMaxSequenceSize = Math.Max(mMaxSequenceSize, mSequenceSize);

			control.Location = controlLocation;
			base.Controls.Add(control);
		}

		private void addDeviceControls()
		{
			base.SuspendLayout();
			base.Controls.Clear();

			if (Devices == null)
			{
				base.Controls.Add(mNoDevicesLabel);
			}
			else
			{
				for (int i = 0; i < mDeviceControls.Length; i++)
				{
					addDeviceControl(i);
				}
			}

			base.ResumeLayout(false);
		}

		private int getHorizontalSpacing()
		{
			return mStructure == null ? 0 : mStructure.HorizontalSpacing;
		}

		private Orientations getOrientation()
		{
			return mStructure == null ? Orientations.Horizontal : mStructure.Orientation;
		}

		private int getVerticalSpacing()
		{
			return mStructure == null ? 0 : mStructure.VerticalSpacing;
		}

		public new void Update()
		{
			foreach (DeviceStatusControl control in mDeviceControls)
			{
				control.Update();
			}

			base.Update();
		}

		public void UpdateLayout()
		{
			foreach (DeviceStatusControl control in mDeviceControls)
			{
				control.UpdateLayout();
			}
		}

		public MixLib.Devices Devices
		{
			get
			{
				return mDevices;
			}
			set
			{
				if (mDevices == null)
				{
					mDevices = value;
					if (mDevices != null)
					{
						mDeviceControls = new DeviceStatusControl[mDevices.Count];
						DeviceStatusControl control;
						foreach (MixDevice device in mDevices)
						{
							control = new DeviceStatusControl(device);
							control.ToolTip = mToolTip;
							control.DoubleClick += DevicesControl_DoubleClick;

							mDeviceControls[device.Id] = control;
						}
					}

					addDeviceControls();
				}
			}
		}

		private void DevicesControl_DoubleClick(object sender, EventArgs e)
		{
			if (DeviceDoubleClick != null)
			{
				DeviceDoubleClick(this, new DeviceEventArgs(((DeviceStatusControl)sender).Device));
			}
		}

		public LayoutStructure Structure
		{
			get
			{
				return mStructure;
			}
			set
			{
				if (value != null)
				{
					mStructure = value;
					addDeviceControls();
				}
			}
		}

		public enum Breaks
		{
			None = 0,
			Sequence,
			Section
		}

		public ToolTip ToolTip
		{
			get
			{
				return mToolTip;
			}
			set
			{
				mToolTip = value;

				if (mDeviceControls != null)
				{
					foreach (DeviceStatusControl control in mDeviceControls)
					{
						control.ToolTip = value;
					}
				}
			}
		}

		public class LayoutStructure
		{
			private int mHorizontalSpacing;
			private SortedList<int, DevicesControl.Breaks> mLayoutList;
			private DevicesControl.Orientations mOrientation;
			private int mVerticalSpacing;

			public LayoutStructure(DevicesControl.Orientations orientation)
				: this(orientation, 0, 0)
			{
			}

			public LayoutStructure(DevicesControl.Orientations orientation, int horizontalSpacing, int verticalSpacing)
			{
				mLayoutList = new SortedList<int, DevicesControl.Breaks>();
				mOrientation = orientation;
				mHorizontalSpacing = horizontalSpacing;
				mVerticalSpacing = verticalSpacing;
			}

			public int HorizontalSpacing
			{
				get
				{
					return mHorizontalSpacing;
				}
				set
				{
					mHorizontalSpacing = value;
				}
			}

			public DevicesControl.Breaks this[int location]
			{
				get
				{
					return !mLayoutList.ContainsKey(location) ? DevicesControl.Breaks.None : mLayoutList[location];
				}
				set
				{
					if (mLayoutList.ContainsKey(location))
					{
						mLayoutList.Remove(location);
					}
					if (value != DevicesControl.Breaks.None)
					{
						mLayoutList.Add(location, value);
					}
				}
			}

			public DevicesControl.Orientations Orientation
			{
				get
				{
					return mOrientation;
				}
				set
				{
					mOrientation = value;
				}
			}

			public int VerticalSpacing
			{
				get
				{
					return mVerticalSpacing;
				}
				set
				{
					mVerticalSpacing = value;
				}
			}
		}

		public enum Orientations
		{
			Horizontal,
			Vertical
		}
	}
}
