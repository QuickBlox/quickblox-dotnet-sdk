using System;

using Xamarin.Forms;

namespace Xamarin.Forms.Conference.WebRTC.Controls
{
	public class CustomSelectableCell : ViewCell
	{
		private bool checkStateChangedInternal;

		private Grid _gridLayout;
		private Label _text;
		private Image _image;
		private CheckBoxExtended _checkBox;

		/// <summary>
		/// The BindableProperty
		/// </summary>
		public static readonly BindableProperty CommandProperty = BindableProperty.Create<CustomSelectableCell, Command>(i => i.Command, default(Command));
		/// <summary>
		/// Gets/Sets the command which gets executed on tapped
		/// </summary>
		public Command Command
		{
			get
			{
				return (Command)this.GetValue(CommandProperty);
			}
			set
			{
				this.SetValue(CommandProperty, value);
			}
		}

		/// <summary>
		/// The BindableProperty
		/// </summary>
		public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create<CustomSelectableCell, bool>(i => i.IsSelected, default(bool), BindingMode.TwoWay, null, IsSelectedChanged);
		/// <summary>
		/// Gets/Sets if the cell is in selected state or not
		/// </summary>
		public bool IsSelected
		{
			get
			{
				return (bool)this.GetValue(IsSelectedProperty);
			}
			set
			{
				this.SetValue(IsSelectedProperty, value);
			}
		}
		/// <summary>
		/// The BindableProperty
		/// </summary>
		public static readonly BindableProperty TextProperty = BindableProperty.Create<CustomSelectableCell, string>(i => i.Text, default(string), BindingMode.TwoWay, null, TextChanged);
		/// <summary>
		///  Gets/Sets the text of the cell
		/// </summary>
		public string Text
		{
			get
			{
				return (string)this.GetValue(TextProperty);
			}
			set
			{
				this.SetValue(TextProperty, value);
			}
		}
		/// <summary>
		/// Creates a new instance of <c>CustomSelectableCell</c>
		/// </summary>
		public CustomSelectableCell()
		{
			this._gridLayout = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition
					{
						Height = new GridLength(1, GridUnitType.Star)
					}
				},
				ColumnDefinitions =
				{
					new ColumnDefinition
					{
						Width = new GridLength(1, GridUnitType.Auto)
					},
					new ColumnDefinition
					{
						Width = new GridLength(1, GridUnitType.Star)
					}
				}
			};

			this._checkBox = new CheckBoxExtended
			{
				WidthRequest = 32,
				HeightRequest = 32,
				VerticalOptions = LayoutOptions.Center,
				HorizontalOptions = LayoutOptions.Center
			};

			this._checkBox.CheckedChanged += (object sender, bool e) =>
			{
				if (!checkStateChangedInternal && e != this._checkBox.Checked)
					ChangeChekedState();
			};

			this._text = new Label
			{
				Style = Device.Styles.ListItemDetailTextStyle,
				VerticalOptions = LayoutOptions.Center
			};

			this._gridLayout.Children.Add(this._checkBox, 0, 0);
			this._gridLayout.Children.Add(this._text, 1, 0);

			this.View = this._gridLayout;
		}

		/// <summary>
		/// Raised when <c>IsSelected</c> changed
		/// </summary>
		/// <param name="obj">The cell</param>
		/// <param name="oldValue">Old value</param>
		/// <param name="newValue">New value</param>
		private static void IsSelectedChanged(BindableObject obj, bool oldValue, bool newValue)
		{
			var cell = obj as CustomSelectableCell;

			if (cell != null)
			{
				cell._checkBox.Checked = newValue;
			}
		}
		/// <summary>
		/// Raised when the <c>Text</c> changed
		/// </summary>
		/// <param name="obj">The cell</param>
		/// <param name="oldValue">Old value</param>
		/// <param name="newValue">New value</param>
		private static void TextChanged(BindableObject obj, string oldValue, string newValue)
		{
			var cell = obj as CustomSelectableCell;

			if (cell != null)
			{
				cell._text.Text = newValue;
			}
		}
		/// <summary>
		/// Auto Select on tapped
		/// </summary>
		protected override void OnTapped()
		{
			base.OnTapped();

			checkStateChangedInternal = true;
			ChangeChekedState();
			checkStateChangedInternal = false;
		}

		void ChangeChekedState()
		{
			this.IsSelected = !this.IsSelected;
			if (this.Command != null)
			{
				if (this.Command.CanExecute(this))
				{
					this.Command.Execute(this);
				}
			}
		}
	}
}


