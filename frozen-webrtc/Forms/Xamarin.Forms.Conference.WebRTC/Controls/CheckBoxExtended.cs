namespace Xamarin.Forms.Conference.WebRTC.Controls
{
	using System;
	using Xamarin.Forms;

	public class CheckBoxExtended : View
	{
		/// <summary>
		/// The checked state property.
		/// </summary>
		public static readonly BindableProperty CheckedProperty =
			BindableProperty.Create<CheckBoxExtended, bool>(
				p => p.Checked, false, BindingMode.TwoWay, propertyChanged: OnCheckedPropertyChanged);

		/// <summary>
		/// The checked text property.
		/// </summary>
		public static readonly BindableProperty CheckedTextProperty =
			BindableProperty.Create<CheckBoxExtended, string>(
				p => p.CheckedText, string.Empty, BindingMode.TwoWay);

		/// <summary>
		/// The unchecked text property.
		/// </summary>
		public static readonly BindableProperty UncheckedTextProperty =
			BindableProperty.Create<CheckBoxExtended, string>(
				p => p.UncheckedText, string.Empty);

		/// <summary>
		/// The default text property.
		/// </summary>
		public static readonly BindableProperty DefaultTextProperty =
			BindableProperty.Create<CheckBoxExtended, string>(
				p => p.Text, string.Empty);

		/// <summary>
		/// Identifies the TextColor bindable property.
		/// </summary>
		/// 
		/// <remarks/>
		public static readonly BindableProperty TextColorProperty =
			BindableProperty.Create<CheckBoxExtended, Color>(
				p => p.TextColor, Color.Default);

		/// <summary>
		/// The font size property
		/// </summary>
		public static readonly BindableProperty FontSizeProperty =
			BindableProperty.Create<CheckBoxExtended, double>(
				p => p.FontSize, -1);

		/// <summary>
		/// The font name property.
		/// </summary>
		public static readonly BindableProperty FontNameProperty =
			BindableProperty.Create<CheckBoxExtended, string>(
				p => p.FontName, string.Empty);


		/// <summary>
		/// The checked changed event.
		/// </summary>
		public event EventHandler<bool> CheckedChanged;

		/// <summary>
		/// Gets or sets a value indicating whether the control is checked.
		/// </summary>
		/// <value>The checked state.</value>
		public bool Checked
		{
			get
			{
				return (bool)this.GetValue(CheckedProperty);
			}

			set
			{
				if (this.Checked != value)
				{
					this.SetValue(CheckedProperty, value);
					var handler = this.CheckedChanged;
					if (handler != null)
						this.CheckedChanged.Invoke(this, value);
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating the checked text.
		/// </summary>
		/// <value>The checked state.</value>
		/// <remarks>
		/// Overwrites the default text property if set when checkbox is checked.
		/// </remarks>
		public string CheckedText
		{
			get
			{
				return (string)this.GetValue(CheckedTextProperty);
			}

			set
			{
				this.SetValue(CheckedTextProperty, value);
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the control is checked.
		/// </summary>
		/// <value>The checked state.</value>
		/// <remarks>
		/// Overwrites the default text property if set when checkbox is checked.
		/// </remarks>
		public string UncheckedText
		{
			get
			{
				return (string)this.GetValue(UncheckedTextProperty);
			}

			set
			{
				this.SetValue(UncheckedTextProperty, value);
			}
		}

		/// <summary>
		/// Gets or sets the text.
		/// </summary>
		public string DefaultText
		{
			get
			{
				return (string)this.GetValue(DefaultTextProperty);
			}

			set
			{
				this.SetValue(DefaultTextProperty, value);
			}
		}

		/// <summary>
		/// Gets or sets the color of the text.
		/// </summary>
		/// <value>The color of the text.</value>
		public Color TextColor
		{
			get
			{
				return (Color)this.GetValue(TextColorProperty);
			}

			set
			{
				this.SetValue(TextColorProperty, value);
			}
		}

		/// <summary>
		/// Gets or sets the size of the font.
		/// </summary>
		/// <value>The size of the font.</value>
		public double FontSize
		{
			get
			{
				return (double)GetValue(FontSizeProperty);
			}
			set
			{
				SetValue(FontSizeProperty, value);
			}
		}

		/// <summary>
		/// Gets or sets the name of the font.
		/// </summary>
		/// <value>The name of the font.</value>
		public string FontName
		{
			get
			{
				return (string)GetValue(FontNameProperty);
			}
			set
			{
				SetValue(FontNameProperty, value);
			}
		}
		/// <summary>
		/// Gets the text.
		/// </summary>
		/// <value>The text.</value>
		public string Text
		{
			get
			{
				return this.Checked
					? (string.IsNullOrEmpty(this.CheckedText) ? this.DefaultText : this.CheckedText)
						: (string.IsNullOrEmpty(this.UncheckedText) ? this.DefaultText : this.UncheckedText);
			}
		}

		/// <summary>
		/// Called when [checked property changed].
		/// </summary>
		/// <param name="bindable">The bindable.</param>
		/// <param name="oldvalue">if set to <c>true</c> [oldvalue].</param>
		/// <param name="newvalue">if set to <c>true</c> [newvalue].</param>
		private static void OnCheckedPropertyChanged(BindableObject bindable, bool oldvalue, bool newvalue)
		{
			var checkBox = (CheckBoxExtended)bindable;
			//checkBox.Checked = newvalue;
		}
	}
}