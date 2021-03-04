using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace WpfApp1
{
	public partial class MainWindow : Window
	{
		private double _angle = 0;
		private double angle
		{
			get { return _angle; }
			set
			{
				if (value >= 0.0 && value < 360.0) _angle = value;
				else if (value < 0.0 && value > -360.0) _angle = 360.0 + value;
				else if (value >= 360.0) _angle = value % 360.0;
				else if (value <= -360.0) _angle = value % 360.0 + 360.0;
				Dispatcher.Invoke(new Action(delegate { Output.Content = _angle.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture); }));
			}
		}
		private int stepsperrevolution = 400;
		private int shaftteeth = 15;
		private int tableteeth = 177;

		private long steps = 0;
		private long _totalsteps = 0;
		private long totalsteps
		{
			get { return _totalsteps; }
			set
			{
				_totalsteps = value;
				angle = _totalsteps / (stepsperrevolution * ((double)tableteeth / shaftteeth)) * 360;
			}
		}
		private long prevtotalsteps = 0;

		private string data = String.Empty;
		private bool paused = false;
		private bool _rotating = false;
		private bool rotating
		{
			get { return _rotating; }
			set
			{
				_rotating = value;
				Dispatcher.Invoke(new Action(delegate
				{
					if (value)
					{
						rad1.IsEnabled = rad2.IsEnabled = rad3.IsEnabled = false;
						statusLabel.Content = "Вращение";
					}
					else
					{
						rad1.IsEnabled = rad2.IsEnabled = rad3.IsEnabled = true;
						statusLabel.Content = "Ожидание";
					}
				}));
			}
		}
		private UDPSocket server = new UDPSocket();
		private UDPSocket client = new UDPSocket();
		List<Grid> grids = new List<Grid>();
		public MainWindow()
		{
			server.Server(8888);
			client.Client("192.168.1.1", 8888);
			InitializeComponent();
			KeyDown += new KeyEventHandler(KeyboardControlPressed);
			KeyUp += new KeyEventHandler(KeyboardControlReleased);
			testbtnAdd_Click(new object(), new RoutedEventArgs());
			tab1.IsSelected = true;
			rad2.IsChecked = true;
			client.ReceivedData += HandleRecievedData;
			angle = 0;
		}

		private void KeyboardControlReleased(object sender, KeyEventArgs e)
		{
			if (rad2.IsChecked == true && !rotating)
				switch (e.Key)
				{
					case Key.Left:
						{ RotateCW_PreviewMouseLeftButtonUp(new object(), null); }
						break;
					case Key.Right:
						{ RotateCCW_PreviewMouseLeftButtonUp(new object(), null); }
						break;
				}
		}

		private void KeyboardControlPressed(object sender, KeyEventArgs e)
		{
			if (rad2.IsChecked == true)
				switch (e.Key)
				{
					case Key.Left:
						{ if (!rotating) RotateCW_PreviewMouseLeftButtonDown(new object(), null); }
						break;
					case Key.Right:
						{ if (!rotating) RotateCCW_PreviewMouseLeftButtonDown(new object(), null); }
						break;
					case Key.Down:
						{ PlayPause_Click(new object(), new RoutedEventArgs()); }
						break;
				}
		}

		private void HandleRecievedData(object sender, string recieveddata)
		{
			data = recieveddata;
			switch (recieveddata[0])
			{
				case 'M':
					{
						recieveddata = recieveddata.Trim('M');
						totalsteps = prevtotalsteps + steps - long.Parse(recieveddata);
						rotating = true;
					}
					break;
				case 'P':
					{
						recieveddata = recieveddata.Trim('P');
						totalsteps = prevtotalsteps + steps - long.Parse(recieveddata);
						rotating = false;
					}
					break;
				case 'R':
					{
						recieveddata = recieveddata.Trim('R');
						totalsteps = prevtotalsteps + steps - long.Parse(recieveddata);
						rotating = true;
					}
					break;
				case 'S':
					{
						recieveddata = recieveddata.Trim('S');
						totalsteps = prevtotalsteps + steps - long.Parse(recieveddata);
						rotating = false;
					}
					break;
				case 'F':
					{
						totalsteps = prevtotalsteps + steps;
						rotating = false;
					}
					break;
			}
		}

		private void Rotation(long _steps)
		{
			prevtotalsteps = totalsteps;
			steps = _steps;
			rotating = true;
			client.Send(_steps.ToString());
		}

		private void ForceStop_Click(object sender, RoutedEventArgs e)
		{
			client.Send("S");
			if (data != String.Empty) while (data[0] != 'S') ;
		}

		private void Reset_Click(object sender, RoutedEventArgs e)
		{

		}

		private void btn1_Click(object sender, RoutedEventArgs e)
		{
			tab1.IsSelected = true;
			Style PressedStyle = FindResource("PressedButton") as Style;
			btn1.Style = PressedStyle;
			Style ReleasedStyle = FindResource("SimpleButton") as Style;
			btn2.Style = ReleasedStyle;
		}

		private void btn2_Click(object sender, RoutedEventArgs e)
		{
			tab2.IsSelected = true;
			Style PressedStyle = FindResource("PressedButton") as Style;
			btn2.Style = PressedStyle;
			Style ReleasedStyle = FindResource("SimpleButton") as Style;
			btn1.Style = ReleasedStyle;
		}

		private void rad1_Checked(object sender, RoutedEventArgs e)
		{
			angleBox.IsEnabled = true;
			minusAngle.IsEnabled = true;
			plusAngle.IsEnabled = true;
			PlayPause.IsEnabled = false;
		}

		private void rad2_Checked(object sender, RoutedEventArgs e)
		{
			angleBox.IsEnabled = false;
			minusAngle.IsEnabled = false;
			plusAngle.IsEnabled = false;
			PlayPause.IsEnabled = false;
		}

		private void rad3_Checked(object sender, RoutedEventArgs e)
		{
			angleBox.IsEnabled = false;
			minusAngle.IsEnabled = false;
			plusAngle.IsEnabled = false;
			PlayPause.IsEnabled = false;
		}

		private void btnHome_Click(object sender, RoutedEventArgs e)
		{
			Rotation(0 - totalsteps);
		}

		private void btnSetHome_Click(object sender, RoutedEventArgs e)
		{
			totalsteps = 0;
			prevtotalsteps = 0;
			steps = 0;
			angle = 0;
		}

		private void minusAngle_Click(object sender, RoutedEventArgs e)
		{
			double OutVal = double.Parse(angleBox.Text, System.Globalization.CultureInfo.InvariantCulture);
			OutVal -= 1;
			angleBox.Text = OutVal.ToString("00.0", System.Globalization.CultureInfo.InvariantCulture);
		}

		private void plusAngle_Click(object sender, RoutedEventArgs e)
		{
			double OutVal = double.Parse(angleBox.Text, System.Globalization.CultureInfo.InvariantCulture);
			OutVal += 1;
			angleBox.Text = OutVal.ToString("00.0", System.Globalization.CultureInfo.InvariantCulture);
		}

		private void angleBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			Regex regex = new Regex("[^0-9.]+");
			e.Handled = regex.IsMatch(e.Text);
		}

		private void minusSpeed_Click(object sender, RoutedEventArgs e)
		{
			double OutVal = double.Parse(speedBox.Text, System.Globalization.CultureInfo.InvariantCulture);
			OutVal -= 0.1;
			speedBox.Text = OutVal.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
		}

		private void plusSpeed_Click(object sender, RoutedEventArgs e)
		{
			double OutVal = double.Parse(speedBox.Text, System.Globalization.CultureInfo.InvariantCulture);
			OutVal += 0.1;
			speedBox.Text = OutVal.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
		}

		private void speedBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			Regex regex = new Regex("[^0-9.]+");
			e.Handled = regex.IsMatch(e.Text);
		}

		private void speedBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (speedBox.Text != String.Empty)
				client.Send($"V{((int)(stepsperrevolution * (tableteeth / shaftteeth) * double.Parse(speedBox.Text, System.Globalization.CultureInfo.InvariantCulture) / 60)).ToString()}");
		}

		private void btn_Click(object sender, RoutedEventArgs e)
		{
			Label label = new Label();
			label.Content = "asd";
			Scroll.Children.Add(label);
		}

		private void PlayPause_Click(object sender, RoutedEventArgs e)
		{
			if (!paused)
			{
				client.Send("P");
				while (data[0] != 'P') ;
				paused = true;
			}
			else
			{
				client.Send("R");
				while (data[0] != 'R') ;
				paused = false;
			}
		}

		private void RotateCW_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (rad2.IsChecked == false) PlayPause.IsEnabled = true;
			else PlayPause.IsEnabled = true;
			if (rad1.IsChecked == false) Rotation(32000);
			else if (!rotating) Rotation((long)(stepsperrevolution * ((double)tableteeth / shaftteeth) * double.Parse(angleBox.Text, System.Globalization.CultureInfo.InvariantCulture) / 360));
		}

		private void RotateCW_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (rad2.IsChecked == true) ForceStop_Click(new object(), new RoutedEventArgs());
		}

		private void RotateCCW_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (rad2.IsChecked == false) PlayPause.IsEnabled = true;
			else PlayPause.IsEnabled = true;
			if (rad1.IsChecked == false) Rotation(-32000);
			else if (!rotating) Rotation(0 - (long)(stepsperrevolution * ((double)tableteeth / shaftteeth) * double.Parse(angleBox.Text, System.Globalization.CultureInfo.InvariantCulture) / 360));
		}

		private void RotateCCW_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (rad2.IsChecked == true) ForceStop_Click(new object(), new RoutedEventArgs());
		}

		private void RefreshGrids()
		{
			Scroll.Children.Clear();
			int i = 0;
			foreach (Grid gr in grids)
			{
				UIElement toremove = null;
				foreach (var child in gr.Children) if ((string)child.GetType().GetProperty("Name").GetValue(child, null) == $"dynLabel{i}") toremove = (UIElement)child;
				gr.Children.Remove(toremove);
				i++;
				Scroll.Children.Add(gr);

			}
			foreach (Grid gr in grids)
			{
				//Label dynLabel = new Label() { Name = $"dynLabel{grids.Count}", Content = i };
				//Grid.SetColumn(dynLabel, 0);
				//gr.Children.Add(dynLabel);
				//Scroll.Children.Add(gr);
			}
		}

		private void RemoveGrid(object sender, RoutedEventArgs e)
		{
			foreach (Grid gr in grids.ToList()) if (gr.Children.Contains((UIElement)sender)) grids.RemoveAt(grids.FindIndex(x => x == gr));
			RefreshGrids();
		}

		private void testbtnAdd_Click(object sender, RoutedEventArgs e)
		{
			int[] widths = new int[] { 30, 70, 40, 70, 40 };
			Grid grid = new Grid() { Name = $"dynGrid{grids.Count}", Width = 250, Height = 50, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
			for (int i = 0; i < 5; i++) grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(widths[i]) });

			Label dynLabel = new Label() { Name = $"dynLabel{grids.Count}", Content = grids.Count + 1 };//, Style = FindResource("dynamicLabel") as Style };
			Grid.SetColumn(dynLabel, 0);
			grid.Children.Add(dynLabel);

			TextBox dynTxtAngle = new TextBox() { Name = $"dynTxtAngle{grids.Count}", Height = 40, Width = 50 };
			Grid.SetColumn(dynTxtAngle, 1);
			grid.Children.Add(dynTxtAngle);

			TextBox dynTxtSpeed = new TextBox() { Name = $"dynTxtSpeed{grids.Count}", Height = 40, Width = 50 };
			Grid.SetColumn(dynTxtSpeed, 3);
			grid.Children.Add(dynTxtSpeed);

			Button dynBtn = new Button() { Name = $"dynBtn{grids.Count}", ClickMode = ClickMode.Press, Style = FindResource("dynamicRemoveBtn") as Style };
			dynBtn.Click += RemoveGrid;
			Grid.SetColumn(dynBtn, 4);
			grid.Children.Add(dynBtn);

			grids.Add(grid);
			RefreshGrids();
		}

		private void FromZero_Checked(object sender, RoutedEventArgs e)
		{

		}

		private void FromLast_Checked(object sender, RoutedEventArgs e)
		{

		}

		private void Seconds_Checked(object sender, RoutedEventArgs e)
		{

		}

		private void Minutes_Checked(object sender, RoutedEventArgs e)
		{

		}
	}
}
