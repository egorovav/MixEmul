using MixGui.Events;
using MixGui.Utils;
using MixLib.Type;
using System.Drawing;
using System.Windows.Forms;

namespace MixGui.Components
{
	public class WordValueEditor : UserControl, IWordEditor, INavigableControl, IEscapeConsumer
	{
		readonly Label mEqualsLabel;
		bool mIncludeSign;
		readonly LongValueTextBox mLongValueTextBox;
		bool mReadOnly;
		int mTextBoxWidth;
		readonly WordEditor mWordEditor;

		public event KeyEventHandler NavigationKeyDown;

		public event WordEditorValueChangedEventHandler ValueChanged;

		public WordValueEditor()
			: this(FullWord.ByteCount, true)
		{
		}

		public WordValueEditor(IWord word)
			: this(word, true)
		{
		}

		public WordValueEditor(int byteCount)
			: this(byteCount, true)
		{
		}

		public WordValueEditor(IWord word, bool includeSign)
			: this(word.ByteCount, includeSign)
		{
			WordValue = word;
		}

		public WordValueEditor(int byteCount, bool includeSign)
		{
			mIncludeSign = includeSign;
			mReadOnly = false;
			mTextBoxWidth = 80;

			mWordEditor = new WordEditor(byteCount, includeSign);
			mEqualsLabel = new Label();
			mLongValueTextBox = new LongValueTextBox();

			mWordEditor.Name = "WordEditor";
			mWordEditor.Location = new Point(0, 0);
			mWordEditor.ReadOnly = mReadOnly;
			mWordEditor.TabIndex = 0;
			mWordEditor.TabStop = true;
			mWordEditor.ValueChanged += WordEditor_ValueChanged;
			mWordEditor.NavigationKeyDown += This_KeyDown;

			mEqualsLabel.Name = "EqualsLabel";
			mEqualsLabel.TabIndex = 1;
			mEqualsLabel.TabStop = false;
			mEqualsLabel.Text = "=";
			mEqualsLabel.Size = new Size(10, 19);

			mLongValueTextBox.Name = "LongValueTextBox";
			mLongValueTextBox.ReadOnly = mReadOnly;
			mLongValueTextBox.SupportNegativeZero = true;
			mLongValueTextBox.TabIndex = 2;
			mLongValueTextBox.TabStop = true;
			mLongValueTextBox.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top;
			mLongValueTextBox.ValueChanged += TextBox_ValueChanged;
			mLongValueTextBox.NavigationKeyDown += This_KeyDown;
			mLongValueTextBox.Height = mWordEditor.Height;

			SuspendLayout();
			Controls.Add(mWordEditor);
			Controls.Add(mEqualsLabel);
			Controls.Add(mLongValueTextBox);
			Name = "WordValueEditor";
			KeyDown += This_KeyDown;

			SizeComponent();
		}

		public Control EditorControl => this;

		public FieldTypes? FocusedField => mLongValueTextBox.Focused ? FieldTypes.Value : mWordEditor.FocusedField;

		public int? CaretIndex =>
				FocusedField == FieldTypes.Value ? mLongValueTextBox.SelectionStart + mLongValueTextBox.SelectionLength : mWordEditor.CaretIndex;

		public bool Focus(FieldTypes? field, int? index)
		{
			return field == FieldTypes.Value ? mLongValueTextBox.FocusWithIndex(index) : mWordEditor.Focus(field, index);
		}

		protected virtual void OnValueChanged(WordEditorValueChangedEventArgs args)
		{
			ValueChanged?.Invoke(this, args);
		}

		protected override void Dispose(bool disposing)
		{
		}

		void This_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Modifiers != Keys.None)
			{
				return;
			}

			FieldTypes editorField = FieldTypes.Word;
			int? index = null;

			if (sender == mLongValueTextBox)
			{
				editorField = FieldTypes.Value;
				index = mLongValueTextBox.SelectionLength + mLongValueTextBox.SelectionStart;
			}

			switch (e.KeyCode)
			{
				case Keys.Prior:
				case Keys.Next:
				case Keys.Up:
				case Keys.Down:
					NavigationKeyDown?.Invoke(this, new FieldKeyEventArgs(e.KeyData, editorField, index));
					break;

				case Keys.Right:
					if (sender == mWordEditor)
					{
						mLongValueTextBox.Focus();
					}
					else if (sender == mLongValueTextBox && NavigationKeyDown != null)
					{
						NavigationKeyDown(this, e);
					}

					break;

				case Keys.Left:
					if (sender == mLongValueTextBox)
					{
						mWordEditor.Focus(FieldTypes.LastByte, null);
					}
					else if (sender == mWordEditor && NavigationKeyDown != null)
					{
						NavigationKeyDown(this, e);
					}

					break;
			}
		}

		void SizeComponent()
		{
			SuspendLayout();

			mEqualsLabel.Location = new Point(mWordEditor.Width, 2);
			mLongValueTextBox.AutoSize = false;
			mLongValueTextBox.MinValue = IncludeSign ? -mWordEditor.WordValue.MaxMagnitude : 0L;
			mLongValueTextBox.MaxValue = mWordEditor.WordValue.MaxMagnitude;
			mLongValueTextBox.Location = new Point(mEqualsLabel.Left + mEqualsLabel.Width, 0);
			mLongValueTextBox.Size = new Size(TextBoxWidth, mWordEditor.Height);
			mLongValueTextBox.Magnitude = mWordEditor.WordValue.MagnitudeLongValue;
			mLongValueTextBox.Sign = mWordEditor.WordValue.Sign;
			Size = new Size(mLongValueTextBox.Left + TextBoxWidth, mWordEditor.Height);

			ResumeLayout(false);
		}

		void TextBox_ValueChanged(LongValueTextBox source, LongValueTextBox.ValueChangedEventArgs args)
		{
			IWord wordValue = mWordEditor.WordValue;
			IWord oldValue = new Word(wordValue.Magnitude, wordValue.Sign);
			wordValue.MagnitudeLongValue = mLongValueTextBox.Magnitude;
			wordValue.Sign = mLongValueTextBox.Sign;
			mWordEditor.Update();

			OnValueChanged(new WordEditorValueChangedEventArgs(oldValue, new Word(wordValue.Magnitude, wordValue.Sign)));
		}

		public new void Update()
		{
			mWordEditor.Update();
			mLongValueTextBox.Magnitude = mWordEditor.WordValue.MagnitudeLongValue;
			mLongValueTextBox.Sign = mWordEditor.WordValue.Sign;
			base.Update();
		}

		public void UpdateLayout()
		{
			mWordEditor.UpdateLayout();
			mLongValueTextBox.UpdateLayout();
		}

		void WordEditor_ValueChanged(IWordEditor sender, WordEditorValueChangedEventArgs args)
		{
			mLongValueTextBox.Magnitude = mWordEditor.WordValue.MagnitudeLongValue;
			mLongValueTextBox.Sign = mWordEditor.WordValue.Sign;
			OnValueChanged(args);
		}

		public int ByteCount
		{
			get => mWordEditor.ByteCount;
			set
			{
				if (ByteCount != value)
				{
					mWordEditor.ByteCount = value;
					SizeComponent();
				}
			}
		}

		public bool IncludeSign
		{
			get => mIncludeSign;
			set
			{
				if (mIncludeSign != value)
				{
					mIncludeSign = value;
					mWordEditor.IncludeSign = mIncludeSign;
					SizeComponent();
				}
			}
		}

		public bool ReadOnly
		{
			get => mReadOnly;
			set
			{
				if (ReadOnly != value)
				{
					mReadOnly = value;
					mWordEditor.ReadOnly = mReadOnly;
					mLongValueTextBox.ReadOnly = mReadOnly;
				}
			}
		}

		public int TextBoxWidth
		{
			get => mTextBoxWidth;
			set
			{
				if (mTextBoxWidth != value)
				{
					mTextBoxWidth = value;
					SizeComponent();
				}
			}
		}

		public IWord WordValue
		{
			get => mWordEditor.WordValue;
			set
			{
				int byteCount = ByteCount;
				mWordEditor.WordValue = value;

				if (byteCount == value.ByteCount)
				{
					Update();
				}
				else
				{
					SizeComponent();
				}
			}
		}

		public void Select(int start, int length)
		{
			if (FocusedField == FieldTypes.Value)
			{
				mLongValueTextBox.Select(start, length);
			}
			else
			{
				mWordEditor.Select(start, length);
			}
		}
	}
}
