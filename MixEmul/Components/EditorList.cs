using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MixGui.Utils;
using MixLib.Type;

namespace MixGui.Components
{
	public class EditorList<T> : UserControl, IEnumerable<T> where T : IEditor
	{
		private object mEditorsSyncRoot;
		private VScrollBar mIndexScrollBar;
		private int mFirstVisibleIndex;
		private bool mReadOnly;
		private bool mReloading;
		private List<T> mEditors;
		private bool mResizeInProgress;
		private bool mSizeAdaptationPending;
		private int mMinIndex;
		private int mMaxIndex;
		private CreateEditorCallback mCreateEditor;
		private LoadEditorCallback mLoadEditor;

		public delegate T CreateEditorCallback(int index);
		public delegate void LoadEditorCallback(T editor, int index);
		public delegate void FirstVisibleIndexChangedHandler(EditorList<T> sender, FirstVisibleIndexChangedEventArgs args);

		public class FirstVisibleIndexChangedEventArgs : EventArgs
		{
			private int mFirstVisibleIndex;

			public FirstVisibleIndexChangedEventArgs(int firstVisibleIndex)
			{
				mFirstVisibleIndex = firstVisibleIndex;
			}

			public int FirstVisibleIndex
			{
				get
				{
					return mFirstVisibleIndex;
				}
			}
		}

		public event FirstVisibleIndexChangedHandler FirstVisibleIndexChanged;

		public EditorList()
			: this(0, -1, null, null)
		{
		}

		public EditorList(int minIndex, int maxIndex, CreateEditorCallback createEditor, LoadEditorCallback loadEditor)
		{
			mMinIndex = minIndex;
			mMaxIndex = maxIndex;
			mCreateEditor = createEditor;
			mLoadEditor = loadEditor;
			mFirstVisibleIndex = 0;

			mReloading = false;
			mReadOnly = false;
			mResizeInProgress = false;
			mSizeAdaptationPending = false;

			mEditorsSyncRoot = new object();
			mEditors = new List<T>();

			initializeComponent();
		}

		public int ActiveEditorIndex
		{
			get
			{
				ContainerControl container = (ContainerControl)this;
				Control control = null;

				List<Control> editorControls = new List<Control>(mEditors.Count);
				foreach (T editor in mEditors)
				{
					editorControls.Add(editor.EditorControl);
				}

				int index = -1;

				while (container != null)
				{
					control = container.ActiveControl;

					index = editorControls.IndexOf(control);
					if (index >= 0)
					{
						break;
					}

					container = control as ContainerControl;
				}

				return index;
			}
		}

		private void adaptToSize()
		{
			if (MaxEditorCount <= 0 || mEditors.Count == 0)
			{
				return;
			}

			int visibleEditorCount = VisibleEditorCount;
			int editorsToAddCount = (visibleEditorCount - mEditors.Count) + 1;
			FirstVisibleIndex = mFirstVisibleIndex;

			lock (mEditorsSyncRoot)
			{
				mReloading = true;

				for (int i = 0; i < mEditors.Count; i++)
				{
					mEditors[i].EditorControl.Visible = FirstVisibleIndex + i <= MaxIndex;
				}

				if (editorsToAddCount > 0)
				{
					for (int i = 0; i < editorsToAddCount; i++)
					{
						addEditor(FirstVisibleIndex + mEditors.Count);
					}
				}

				mReloading = false;
			}


			if (visibleEditorCount == MaxEditorCount)
			{
				mIndexScrollBar.Enabled = false;
			}
			else
			{
				mIndexScrollBar.Enabled = true;
				mIndexScrollBar.Value = mFirstVisibleIndex;
				mIndexScrollBar.LargeChange = visibleEditorCount;
				mIndexScrollBar.Refresh();
			}

			mSizeAdaptationPending = false;
		}

		private void addEditor(int index)
		{
			Control lastEditorControl = mEditors[mEditors.Count - 1].EditorControl;
			bool indexInItemRange = index <= mMaxIndex;

			base.SuspendLayout();

			T newEditor = mCreateEditor(indexInItemRange ? index : int.MinValue);
			Control newEditorControl = newEditor.EditorControl;
			newEditorControl.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top;
			newEditorControl.Location = new Point(0, lastEditorControl.Bottom);
			newEditorControl.Size = lastEditorControl.Size;
			newEditorControl.TabIndex = mEditors.Count + 1;
			newEditorControl.Visible = indexInItemRange;
			newEditorControl.MouseWheel += mouseWheel;
			newEditor.ReadOnly = mReadOnly;
			if (newEditor is INavigableControl)
			{
				((INavigableControl)newEditor).NavigationKeyDown += new KeyEventHandler(keyDown);
			}

			mEditors.Add(newEditor);

			base.Controls.Add(newEditor.EditorControl);

			base.ResumeLayout(false);
		}

		private void initializeComponent()
		{
			base.SuspendLayout();
			base.Controls.Clear();

			mIndexScrollBar = new VScrollBar();

			mIndexScrollBar.Location = new Point(base.Width - 16, 0);
			mIndexScrollBar.Size = new Size(16, base.Height);
			mIndexScrollBar.Anchor = AnchorStyles.Right | AnchorStyles.Bottom | AnchorStyles.Top;
			mIndexScrollBar.Minimum = mMinIndex;
			mIndexScrollBar.Maximum = mMaxIndex;
			mIndexScrollBar.Name = "mAddressScrollBar";
			mIndexScrollBar.LargeChange = 1;
			mIndexScrollBar.TabIndex = 0;
			mIndexScrollBar.Enabled = !mReadOnly;
			mIndexScrollBar.Scroll += mIndexScrollBar_Scroll;

			Control editorControl = mCreateEditor != null ? createFirstEditor() : null;

			base.Controls.Add(mIndexScrollBar);

			if (editorControl != null)
			{
				base.Controls.Add(editorControl);
			}

			base.Name = "WordEditorList";

			base.BorderStyle = BorderStyle.Fixed3D;

			base.ResumeLayout(false);

			adaptToSize();

			base.SizeChanged += this_SizeChanged;
			base.KeyDown += keyDown;
			base.MouseWheel += mouseWheel;
		}

		void mouseWheel(object sender, MouseEventArgs e)
		{
			FirstVisibleIndex -= (int)((e.Delta / 120.0F) * SystemInformation.MouseWheelScrollLines);
		}

		private Control createFirstEditor()
		{
			T editor = mCreateEditor(0);
			Control editorControl = editor.EditorControl;
			editorControl.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top;
			editorControl.Location = new Point(0, 0);
			editorControl.Width = mIndexScrollBar.Left;
			editorControl.TabIndex = 1;
			editorControl.MouseWheel += mouseWheel;
			editor.ReadOnly = mReadOnly;
			if (editor is INavigableControl)
			{
				((INavigableControl)editor).NavigationKeyDown += new KeyEventHandler(keyDown);
			}

			mEditors.Add(editor);

			return editorControl;
		}

		public bool IsIndexVisible(int index)
		{
			return index >= FirstVisibleIndex && index < (FirstVisibleIndex + VisibleEditorCount);
		}

		private void keyDown(object sender, KeyEventArgs e)
		{
			if (e.Modifiers != Keys.None)
			{
				return;
			}

			FieldTypes? editorField = null;
			int? index = null;

			if (e is IndexKeyEventArgs)
			{
				index = ((IndexKeyEventArgs)e).Index;
			}

			if (e is FieldKeyEventArgs)
			{
				editorField = ((FieldKeyEventArgs)e).Field;
			}

			switch (e.KeyCode)
			{
				case Keys.Prior:
					FirstVisibleIndex -= VisibleEditorCount;
					break;

				case Keys.Next:
					FirstVisibleIndex += VisibleEditorCount;
					return;

				case Keys.Up:
					if (sender is INavigableControl)
					{
						T editor = (T)sender;

						if (editor.Equals(mEditors[0]))
						{
							mIndexScrollBar.Focus();
							FirstVisibleIndex--;
							editor.Focus(editorField, index);

							return;
						}

						mEditors[mEditors.IndexOf(editor) - 1].Focus(editorField, index);

						return;
					}

					FirstVisibleIndex--;

					return;

				case Keys.Right:
					break;

				case Keys.Down:
					if (sender is INavigableControl)
					{
						T editor = (T)sender;

						if (mEditors.IndexOf(editor) >= VisibleEditorCount - 1)
						{
							mIndexScrollBar.Focus();
							FirstVisibleIndex++;
							mEditors[VisibleEditorCount - 1].Focus(editorField, index);

							return;
						}

						mEditors[mEditors.IndexOf(editor) + 1].Focus(editorField, index);

						return;
					}

					FirstVisibleIndex++;

					return;
			}
		}

		private void mIndexScrollBar_Scroll(object sender, ScrollEventArgs e)
		{
			FirstVisibleIndex = mIndexScrollBar.Value;
		}

		public void MakeIndexVisible(int index)
		{
			if (!IsIndexVisible(index))
			{
				FirstVisibleIndex = index;
			}
		}

		public LoadEditorCallback LoadEditor
		{
			get
			{
				return mLoadEditor;
			}
			set
			{
				if (value == null)
				{
					return;
				}

				if (mLoadEditor != null)
				{
					throw new InvalidOperationException("value may only be set once");
				}

				mLoadEditor = value;
			}
		}

		public CreateEditorCallback CreateEditor
		{
			get
			{
				return mCreateEditor;
			}
			set
			{
				if (value == null)
				{
					return;
				}

				if (mCreateEditor != null)
				{
					throw new InvalidOperationException("value may only be set once");
				}

				mCreateEditor = value;

				base.Controls.Add(createFirstEditor());
				adaptToSize();
			}
		}

		public bool ResizeInProgress
		{
			get
			{
				return mResizeInProgress;
			}

			set
			{
				if (mResizeInProgress == value)
				{
					return;
				}

				mResizeInProgress = value;

				if (mResizeInProgress)
				{
					for (int i = 1; i < mEditors.Count; i++)
					{
						mEditors[i].EditorControl.SuspendDrawing();
					}
				}
				else
				{
					for (int i = 1; i < mEditors.Count; i++)
					{
						mEditors[i].EditorControl.ResumeDrawing();
					}

					if (mSizeAdaptationPending)
					{
						adaptToSize();
					}

					Invalidate(true);
				}
			}
		}

		private void this_SizeChanged(object sender, EventArgs e)
		{
			if (!mResizeInProgress)
			{
				adaptToSize();
			}
			else
			{
				mSizeAdaptationPending = true;
			}
		}

		public new void Update()
		{
			lock (mEditorsSyncRoot)
			{
				for (int i = 0; i < mEditors.Count; i++)
				{
					int index = mFirstVisibleIndex + i;
					mLoadEditor(mEditors[i], index <= mMaxIndex ? index : int.MinValue);
				}
			}

			base.Update();
		}

		public void UpdateLayout()
		{
			base.SuspendLayout();

			lock (mEditorsSyncRoot)
			{
				foreach (T editor in mEditors)
				{
					editor.UpdateLayout();
				}
			}

			base.ResumeLayout();
		}

		public int FirstVisibleIndex
		{
			get
			{
				return mFirstVisibleIndex;
			}
			set
			{
				if (value < mMinIndex)
				{
					value = mMinIndex;
				}

				if (value + VisibleEditorCount > mMaxIndex + 1)
				{
					value = mMaxIndex - VisibleEditorCount + 1;
				}

				if (value != mFirstVisibleIndex)
				{
					int indexDelta = value - mFirstVisibleIndex;
					int selectedEditorIndex = ActiveEditorIndex;
					FieldTypes? field = selectedEditorIndex >= 0 ? mEditors[selectedEditorIndex].FocusedField : null;
					int? caretIndex = selectedEditorIndex >= 0 ? mEditors[selectedEditorIndex].CaretIndex : null;

					mFirstVisibleIndex = value;

					lock (mEditorsSyncRoot)
					{
						mReloading = true;

						for (int i = 0; i < mEditors.Count; i++)
						{
							int index = mFirstVisibleIndex + i;
							bool indexInItemRange = index <= mMaxIndex;

							T editor = mEditors[i];
							if (mLoadEditor != null)
							{
								mLoadEditor(editor, indexInItemRange ? index : int.MinValue);
							}

							editor.EditorControl.Visible = indexInItemRange;
						}

						if (selectedEditorIndex != -1)
						{
							selectedEditorIndex -= indexDelta;
							if (selectedEditorIndex < 0)
							{
								selectedEditorIndex = 0;
							}
							else if (selectedEditorIndex >= VisibleEditorCount)
							{
								selectedEditorIndex = VisibleEditorCount - 1;
							}

							mEditors[selectedEditorIndex].Focus(field, caretIndex);
						}

						mReloading = false;
					}

					mIndexScrollBar.Value = mFirstVisibleIndex;
					OnFirstVisibleIndexChanged(new FirstVisibleIndexChangedEventArgs(mFirstVisibleIndex));
				}
			}
		}

		protected void OnFirstVisibleIndexChanged(FirstVisibleIndexChangedEventArgs args)
		{
			if (FirstVisibleIndexChanged != null)
			{
				FirstVisibleIndexChanged(this, args);
			}
		}

		public bool ReadOnly
		{
			get
			{
				return mReadOnly;
			}
			set
			{
				if (mReadOnly != value)
				{
					mReadOnly = value;
					lock (mEditorsSyncRoot)
					{
						foreach (T editor in mEditors)
						{
							editor.ReadOnly = mReadOnly;
						}
					}
				}
			}
		}

		public int VisibleEditorCount
		{
			get
			{
				if (mEditors.Count == 0)
				{
					return 0;
				}

				int editorAreaHeight = base.Height;

				if (editorAreaHeight < 0)
				{
					editorAreaHeight = 0;
				}

				return Math.Min(editorAreaHeight / mEditors[0].EditorControl.Height, MaxEditorCount);
			}
		}

		public int EditorCount
		{
			get
			{
				return mEditors.Count;
			}
		}

		public int MaxIndex
		{
			get
			{
				return mMaxIndex;
			}
			set
			{
				mMaxIndex = value;

				mIndexScrollBar.Maximum = mMaxIndex;

				adaptToSize();
			}
		}

		public int MinIndex
		{
			get
			{
				return mMinIndex;
			}
			set
			{
				mMaxIndex = value;

				mIndexScrollBar.Minimum = mMinIndex;

				adaptToSize();
			}
		}


		public int MaxEditorCount
		{
			get
			{
				return mMaxIndex - mMinIndex + 1;
			}
		}

		public bool IsReloading
		{
			get
			{
				return mReloading;
			}
		}

		public T this[int index]
		{
			get
			{
				return mEditors[index];
			}
		}

		#region IEnumerable Members

		public IEnumerator GetEnumerator()
		{
			return mEditors.GetEnumerator();
		}

		#endregion

		#region IEnumerable<IWordEditor> Members

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return mEditors.GetEnumerator();
		}

		#endregion
	}
}