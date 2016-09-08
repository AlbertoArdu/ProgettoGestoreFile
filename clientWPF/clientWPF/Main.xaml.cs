using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;


namespace clientWPF
{
	/// <summary>
	/// Interaction logic for Main.xaml
	/// </summary>
	public partial class Main : Window
	{
		List<Version> versions = null;
		private bool loggedin = false;
		private NotifyIcon notifyIcon;
		private System.Windows.Forms.ContextMenu notifyIconMenu;
        private ConnectionSettings connectionSettings;

		public Main()
		{
			InitializeComponent();

            //initialize tray icon
            notifyIcon = new NotifyIcon();
			Stream iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/clientWPF;component/sync.png")).Stream;
			notifyIcon.Icon = new System.Drawing.Icon(iconStream);
			notifyIconMenu = new System.Windows.Forms.ContextMenu();
			System.Windows.Forms.MenuItem mnuItemSyncNow = new System.Windows.Forms.MenuItem();
			System.Windows.Forms.MenuItem mnuItemShow = new System.Windows.Forms.MenuItem();
			mnuItemShow.Text = "Show";
			mnuItemShow.Click += new System.EventHandler(notifyIcon_Click);
			notifyIconMenu.MenuItems.Add(mnuItemShow);
			mnuItemSyncNow.Text = "SyncNow";
			mnuItemSyncNow.Click += new System.EventHandler(syncMenuItem);
			notifyIconMenu.MenuItems.Add(mnuItemSyncNow);
			notifyIcon.Text = "SyncClient";
			notifyIcon.ContextMenu = notifyIconMenu;
			notifyIcon.BalloonTipTitle = "App minimized to tray";
			notifyIcon.BalloonTipText = "Sync sill running.";
            //notifyIcon.Visible = true;

            // Settings manager
            connectionSettings = new ConnectionSettings();
			tAddress.Text = connectionSettings.readSetting("connection", "address");
			tPort.Text = connectionSettings.readSetting("connection", "port");
			tDirectory.Text = connectionSettings.readSetting("account", "directory");
			tTimeout.Text = connectionSettings.readSetting("connection", "syncTime");
		}

		private void syncMenuItem(object sender, System.EventArgs e)
		{
			ControlloModifiche.Check();
			//this.Hide();
		}

		private void StartSync_Click(object sender, EventArgs e)
		{
			// start the sync manager
			try
			{
				bStart.IsEnabled = false;
				lVersions.Items.Clear();
				ControlloModifiche.Inizializza(tAddress.Text, Int32.Parse(tPort.Text), tDirectory.Text, Int32.Parse(tTimeout.Text) * 1000);
				bStop.IsEnabled = true;
				bSyncNow.IsEnabled = true;
				bGetVersions.IsEnabled = true;
				tDirectory.IsEnabled = false;
				tTimeout.IsEnabled = false;
				bBrowse.IsEnabled = false;
				tAddress.IsEnabled = false;
				tPort.IsEnabled = false;
				updateStatus("Started");
                // Save settings
                connectionSettings.writeSetting("connection", "address", tAddress.Text);
                connectionSettings.writeSetting("connection", "port", tPort.Text);
                connectionSettings.writeSetting("account", "directory", tDirectory.Text);
                connectionSettings.writeSetting("connection", "syncTime", tTimeout.Text);
			}
			catch (Exception ex)
			{
				bStart.IsEnabled = true;
				updateStatus(ex.Message);
			}
		}

		private void StopSync_Click(object sender, EventArgs e)
		{
			// stop the sync manager
			try
			{
				lVersions.Items.Clear();
				updateStatus("Stop");
				forceStop();
			}
			catch (Exception ex)
			{
				bStop.IsEnabled = true;
				bSyncNow.IsEnabled = true;
				updateStatus(ex.Message);
			}
		}

		private void Browse_Click(object sender, EventArgs e)
		{
			FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
			folderBrowserDialog.Description = "Select the folder to sync";
			folderBrowserDialog.ShowNewFolderButton = true;
			//folderBrowserDialog.RootFolder = Environment.SpecialFolder.Personal;
			if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				tDirectory.Text = folderBrowserDialog.SelectedPath;
			}
		}

		private void updateStatus(String message, bool fatalError)
		{
			this.Dispatcher.BeginInvoke((Action)(() =>
			{
				updateStatus(message);
				if (fatalError)
				{
					forceStop();
				}
			}));
		}
		private void updateStatusBar(int percentage)
		{
			this.Dispatcher.BeginInvoke((Action)(() =>
			{
				lStatusBar.Value = percentage;
			}));
		}

		private void forceStop()
		{
			bStop.IsEnabled = false;
			bSyncNow.IsEnabled = false;
			bRestore.IsEnabled = false;
			bGetVersions.IsEnabled = false;
			ControlloModifiche.stopSync();
			bStart.IsEnabled = true;
			tDirectory.IsEnabled = true;
			tTimeout.IsEnabled = true;
			bBrowse.IsEnabled = true;
			tAddress.IsEnabled = true;
			tPort.IsEnabled = true;
		}

		private async void openLogin()
		{
			Login lw = new Login();
			bool loginAuthorized = false;
			bLogInOut.IsEnabled = false;
			lw.Username = connectionSettings.readSetting("account", "username");
			lw.Username = connectionSettings.readSetting("account", "password");
			while (!loginAuthorized)
			{
				lw.showLogin();
				try
				{
					switch (lw.waitResponse())
					{
						case Login.LoginResponse.CANCEL:
							//System.Windows.Application.Current.Shutdown();
							lw.Close();
							bLogInOut.IsEnabled = true;
							return;
						case Login.LoginResponse.LOGIN:
                            Command loginComm = new ComandoLogin(tAddress.Text, Convert.ToInt32(tPort.Text), lw.Username, lw.Password);
                            loginComm.esegui();
                            /*loginAuthorized = await syncManager.login(tAddress.Text, Convert.ToInt32(tPort.Text), lw.Username, lw.Password);
							if (!loginAuthorized)
							{
								lw.ErrorMessage = "Login faild";
							}*/
                            break;
						case Login.LoginResponse.REGISTER:
                            Command regComm = new ComandoRegistra(tAddress.Text, Convert.ToInt32(tPort.Text), lw.Username, lw.Password, tDirectory.Text);
                            regComm.esegui();
                            /*loginAuthorized = await syncManager.login(tAddress.Text, Convert.ToInt32(tPort.Text), lw.Username, lw.Password, tDirectory.Text, true);
							if (!loginAuthorized)
							{
								lw.ErrorMessage = "Registration faild";
							}*/
                            break;
						default:
							throw new Exception("Not implemented");
					}
					if (loginAuthorized)
					{
						lUsername.Content = lw.Username;
						bLogInOut.Content = "Logout";
						lw.Close();
						connectionSettings.writeSetting("account", "username", lw.Username);
						if(lw.KeepLoggedIn)
							connectionSettings.writeSetting("account", "password", lw.Username);
						else
							connectionSettings.writeSetting("account", "password", "");
						bStart.IsEnabled = true;
						loggedin = true;
						updateStatus("Logged in");
						//StartSync_Click(null, null); // start sync
					}
				}
				catch (Exception ex)
				{
					lw.ErrorMessage = ex.Message;
					loginAuthorized = false;
				}
			}
			bLogInOut.IsEnabled = true;
		}

		private void LogInOut_Click(object sender, RoutedEventArgs e)
		{
			if (loggedin)
			{
				forceStop();
				lUsername.Content = "Please login";
				bLogInOut.Content = "Login";
				bStart.IsEnabled = false;
				loggedin = false;
			}
			lVersions.Items.Clear();
			this.openLogin();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			// Start the login procedure
			this.Dispatcher.BeginInvoke((Action)(() =>
			{
				// TODO login at startup
				// i have to create the connection in order to perform the login
				openLogin();
			}));
		}

		private void updateStatus(String newStatus)
		{
			lStatus.Content = newStatus;
			ListBoxItem lbi = new ListBoxItem();
			lbi.Content = newStatus;
			lbStatus.Items.Add(lbi);
		}

		private async void GetVersions_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				bGetVersions.IsEnabled = false;
				lVersions.Items.Clear();

                if (Command.Logged == true) {
                    Command getVersComm = new ();
                    getVersComm.esegui();
                    //versions = await syncManager.getVersions();
                }
                else
                {
                    //LOGIN!!!!!!!!!!!!!!!
                }
                foreach (Version version in versions)
				{
                    lVersions.Items.Add(new VersionsListViewItem(version.VersionNum, version.NewFiles, version.EditFiles, version.DelFiles, version.Timestamp));
                }
				lVersions.SelectedIndex = 0;

				bGetVersions.IsEnabled = true;
				bRestore.IsEnabled = true;
			}
			catch (Exception ex)
			{
				bGetVersions.IsEnabled = true;
				updateStatus(ex.Message);
			}
		}
        /*
		private void lVersions_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			DependencyObject obj = (DependencyObject)e.OriginalSource;
			while (obj != null && obj != lVersions)
			{
				if (obj.GetType() == typeof(System.Windows.Controls.ListViewItem))
				{
					Details vdw = new Details(versions[lVersions.SelectedIndex], syncManager);
					vdw.Show();
					break;
				}
				obj = VisualTreeHelper.GetParent(obj);
			}
		}
        */
		private async void Restore_Click(object sender, EventArgs e)
		{
			bRestore.IsEnabled = false;
			Int64 selVersion = versions[lVersions.SelectedIndex].VersionNum;
			MessageBoxResult res = System.Windows.MessageBox.Show("Do you want to restore version number " + selVersion + " ?", "Restore system", System.Windows.MessageBoxButton.YesNo);
			if (res == MessageBoxResult.Yes)
			{
				try
				{
                    if (Command.Logged == true)
                    {
                        Command restoreComm = new ();
                        restoreComm.esegui();                       
                        //await syncManager.restoreVersion(selVersion);
                        System.Windows.MessageBox.Show("Restore Done!", "Restoring system");
                    }
                    else
                    {
                        //LOGIN!!!!!!!!!!!!!!!!!!!!!
                    }
				}
				catch (Exception ex)
				{
					System.Windows.MessageBox.Show("Restore failed:\n" + ex.Message, "Restoring system", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			bRestore.IsEnabled = true;
		}

		private void bSyncNow_Click(object sender, RoutedEventArgs e)
		{
            ControlloModifiche.Check();
		}

		private void Window_StateChanged(object sender, EventArgs e)
		{
			switch (this.WindowState)
			{
				case WindowState.Maximized:
					break;
				case WindowState.Minimized:
					// Do your stuff
					notifyIcon.Visible = true;
					notifyIcon.ShowBalloonTip(1000);
					this.Hide();
					break;
				case WindowState.Normal:

					break;
			}
		}

		private void notifyIcon_Click(object o, EventArgs ea)
		{
			this.Show();

			// this.Activate();
			//this.Focus(); // todo focus

			this.WindowState = WindowState.Normal;
			this.Activate();
			notifyIcon.Visible = false;
		}

	}

	class VersionsListViewItem
	{
		public String sVersion { get; set; }
		public String sNewFiles { get; set; }
		public String sEditFiles { get; set; }
		public String sDelFiles { get; set; }
		public String sDateTime { get; set; }
		public VersionsListViewItem(Int64 version, int newFiles, int editFiles, int delFiles, String dateTime)
		{
			sVersion = version.ToString();
			sNewFiles = newFiles.ToString();
			sEditFiles = editFiles.ToString();
			sDelFiles = delFiles.ToString();
			sDateTime = dateTime;
		}
	}

}