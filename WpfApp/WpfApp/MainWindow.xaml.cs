using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Collections.Generic;

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
				_angle = value;
				Dispatcher.Invoke(new Action(delegate { Output.Content = value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture); }));
			}
		}
		private int stepsperrevolution = 400;
		private int shaftteeth = 15;
		private int tableteeth = 177;
		private int _destination = 0;
		private int destination
		{
			get { return _destination; }
			set
			{
				_destination = value;
				angle = (totalsteps - (double)value) / (stepsperrevolution * ((double)tableteeth / shaftteeth)) * 360;
			}
		}
		private int currentaction = 0;
		private bool _rotating = false;
		private long totalsteps = 0;
		private string data = String.Empty;
		private bool rotating
		{
			get { return _rotating; }
			set
			{
				_rotating = value;
				Dispatcher.Invoke(new Action(delegate { if (value) statusLabel.Content = "Вращение"; else statusLabel.Content = "Ожидание"; }));
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
			testbtnAdd_Click(new object(), new RoutedEventArgs());
			tab1.IsSelected = true;
			rad2.IsChecked = true;
			client.ReceivedData += HandleRecievedData;
		}

		private void HandleRecievedData(object sender, string recieveddata)
		{
			data = recieveddata;
			if (recieveddata[0] == 'F')
			{
				recieveddata = recieveddata.Trim('F');
				rotating = false;
			}
			destination = int.Parse(recieveddata);
		}

		private void Rotation(long steps)
		{
			rotating = true;
			totalsteps += steps;
			client.Send(steps.ToString());
			testLabel.Content = totalsteps.ToString();
		}

		private void ForceStop_Click(object sender, RoutedEventArgs e)
		{
			client.Send("S");
			rotating = false;
			if (data != String.Empty) while (data[0] != 'F');
			totalsteps -= destination;
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

		private void angleBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (angleBox.Text != String.Empty) angle = double.Parse(angleBox.Text, System.Globalization.CultureInfo.InvariantCulture);
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

		private void ResumeRotation(bool resume)
		{
			if (!resume && rotating)
			{
				ForceStop_Click(new object(), new RoutedEventArgs());
				currentaction = destination;
			}
			else if (resume && !rotating) Rotation(currentaction);
		}

		private void PlayPause_Click(object sender, RoutedEventArgs e)
		{
			if (rad3.IsChecked == true) ForceStop_Click(new object(), new RoutedEventArgs());
			else
			{
				if (rotating) ResumeRotation(false);
				else ResumeRotation(true);
			}
		}

		private void RotateCW_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (rad2.IsChecked == false)
            {
				PlayPause.IsEnabled = true;
				if (rad1.IsChecked == false) Rotation(32000);
				else if (!rotating) Rotation((long)(stepsperrevolution * ((double)tableteeth / shaftteeth) * double.Parse(angleBox.Text, System.Globalization.CultureInfo.InvariantCulture) / 360));
			}
		}

		private void RotateCW_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (rad2.IsChecked == true) ForceStop_Click(new object(), new RoutedEventArgs());
		}

		private void RotateCCW_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (rad2.IsChecked == false)
			{
				PlayPause.IsEnabled = true;
				if (rad1.IsChecked == false) Rotation(-32000);
				else if (!rotating) Rotation(0 - (long)(stepsperrevolution * ((double)tableteeth / shaftteeth) * double.Parse(angleBox.Text, System.Globalization.CultureInfo.InvariantCulture) / 360));
			}
		}

		private void RotateCCW_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (rad2.IsChecked == true) ForceStop_Click(new object(), new RoutedEventArgs());
		}

		private void testbtnAdd_Click(object sender, RoutedEventArgs e)
		{
			int[] widths = new int[] { 30, 70, 40, 70, 40 };
			Grid grid = new Grid() { Name = $"dynGrid{grids.Count}", Width = 250, Height = 50, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top };
			for (int i = 0; i < 5; i++) grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(widths[i]) });

			Label dynLabel = new Label() { Name = $"dynLabel{grids.Count}", Content = "TestAsd", Style = FindResource("dynamicLabel") as Style };
			Grid.SetColumn(dynLabel, 0);
			grid.Children.Add(dynLabel);

			TextBox dynTxtAngle = new TextBox() { Name = $"dynTxtAngle{grids.Count}" };
			Grid.SetColumn(dynTxtAngle, 1);
			grid.Children.Add(dynTxtAngle);
			
			TextBox dynTxtSpeed = new TextBox() { Name = $"dynLabel{grids.Count}" };
			Grid.SetColumn(dynTxtSpeed, 3);
			grid.Children.Add(dynTxtSpeed);

			Button dynBtn = new Button() { Name = $"dynBtn{grids.Count}", Width = 20, Height = 20 };
			Grid.SetColumn(dynBtn, 4);
			grid.Children.Add(dynBtn);

			grids.Add(grid);
			Scroll.Children.Clear();
			foreach (Grid gr in grids) Scroll.Children.Add(gr);
		}
	}
}
