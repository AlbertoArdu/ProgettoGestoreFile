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
        //private List<FileUtente> fileList = null;
        private bool loggedin = false;
        private NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenu notifyIconMenu;
        private ConnectionSettings connectionSettings;
        private FileUtente selectedFileUtente;
        private DateTime selectedFileVersion;

        public Main()
        {
            InitializeComponent();

            //initialize tray icon
            notifyIcon = new NotifyIcon();
            Stream iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/clientWPF;component/sync.ico")).Stream;
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

            // Settings
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
                // Save settings
                connectionSettings.writeSetting("connection", "address", tAddress.Text);
                connectionSettings.writeSetting("connection", "port", tPort.Text);
                connectionSettings.writeSetting("account", "directory", tDirectory.Text);
                connectionSettings.writeSetting("connection", "syncTime", tTimeout.Text);

                bStart.IsEnabled = false;
                lDetails.Items.Clear();
                lFileVersions.Items.Clear();
                ControlloModifiche.Inizializza();
                bStop.IsEnabled = true;
                bSyncNow.IsEnabled = true;
                bGetFiles.IsEnabled = true;
                tDirectory.IsEnabled = false;
                tTimeout.IsEnabled = false;
                bBrowse.IsEnabled = false;
                tAddress.IsEnabled = false;
                tPort.IsEnabled = false;
                updateStatus("Started");
            }
            catch (ClientException ex)
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
                lDetails.Items.Clear();
                lFileVersions.Items.Clear();
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
            bGetFiles.IsEnabled = false;
            ControlloModifiche.StopTimer();
            bStart.IsEnabled = true;
            tDirectory.IsEnabled = true;
            tTimeout.IsEnabled = true;
            bBrowse.IsEnabled = true;
            tAddress.IsEnabled = true;
            tPort.IsEnabled = true;
        }

        private void openLogin()
        {
            Login lw = new Login();
            bool loginAuthorized = false;
            bLogInOut.IsEnabled = false;
            lw.Username = connectionSettings.readSetting("account", "username");
            lw.Password = connectionSettings.readSetting("account", "password");
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
                            Command loginComm = new ComandoLogin(lw.Username, lw.Password);
                            loginAuthorized = loginComm.esegui();
                            //loginAuthorized = await syncManager.login(tAddress.Text, Convert.ToInt32(tPort.Text), lw.Username, lw.Password);
                            if (!loginAuthorized)
                            {
                                lw.ErrorMessage = "Login faild";
                            }
                            break;
                        case Login.LoginResponse.REGISTER:
                            Command regComm = new ComandoRegistra(lw.Username, lw.Password);
                            loginAuthorized = regComm.esegui();
                            //loginAuthorized = await syncManager.login(tAddress.Text, Convert.ToInt32(tPort.Text), lw.Username, lw.Password, tDirectory.Text, true);
                            if (!loginAuthorized)
                            {
                                lw.ErrorMessage = "Registration failed";
                            }
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
                        connectionSettings.writeSetting("account", "password", lw.Password);
                        bStart.IsEnabled = true;
                        loggedin = true;
                        updateStatus("Logged in");
                        //StartSync_Click(null, null); // start sync
                    }
                }
                catch (Exception ex) when (ex is ServerException || ex is ClientException)
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
            lDetails.Items.Clear();
            lFileVersions.Items.Clear();
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

        private void GetFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bGetFiles.IsEnabled = false;
                lDetails.Items.Clear();
                lFileVersions.Items.Clear();

                FileUtenteList fileUtenteList = new FileUtenteList();
                int i = 0;
                for (FileUtente fu = fileUtenteList[i]; i< fileUtenteList.Length; i++)
                { 
                    lDetails.Items.Add(new VersionListViewItem(fu.Nome, fu.Path));
                }
                lDetails.SelectedIndex = 0;

                bGetFiles.IsEnabled = true;
            }
            catch (Exception ex)
            {
                bGetFiles.IsEnabled = true;
                updateStatus(ex.Message);
            }
        }

        private void lDetails_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject obj = (DependencyObject)e.OriginalSource;
            while (obj != null && obj != lDetails)
            {
                if (obj.GetType() == typeof(System.Windows.Controls.ListViewItem))
                {
                    string selectedFileName = ((VersionListViewItem)lDetails.SelectedItem).sFilename;
                    string selectedFilePath = ((VersionListViewItem)lDetails.SelectedItem).sPath;

                    FileUtenteList fileUtenteList = new FileUtenteList();
                    selectedFileUtente = fileUtenteList[selectedFileName, selectedFilePath];

                    lFileVersions.Items.Clear();
                    foreach (DateTime fv in selectedFileUtente.Items)
                    {
                        lFileVersions.Items.Add(new FileVersionListViewItem(selectedFileUtente.Nome, fv));
                    }
                    break;
                }
                obj = VisualTreeHelper.GetParent(obj);
            }
        }

        private void lFileVersions_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject obj = (DependencyObject)e.OriginalSource;
            while (obj != null && obj != lDetails)
            {
                if (obj.GetType() == typeof(System.Windows.Controls.ListViewItem))
                {
                    DateTime selectedVersion = ((FileVersionListViewItem)lFileVersions.SelectedItem).sTimestamp;
                    
                    foreach (DateTime fv in selectedFileUtente.Items)
                    {
                        if (selectedVersion == fv)
                            selectedFileVersion = fv;
                    }

                    MessageBoxResult res = System.Windows.MessageBox.Show("Do you want to restore file \"" + selectedFileUtente.Nome + "\" with version number " + selectedVersion + " ?", "Restore system", System.Windows.MessageBoxButton.YesNo);

                    if (res == MessageBoxResult.Yes)
                    {
                        try
                        {
                            if (Command.Logged == true)
                            {
                                Command getVersComm = new ComandoScaricaFile(selectedFileUtente.Nome, selectedFileUtente.Path, selectedFileVersion);
                                getVersComm.esegui();
                                //versions = await syncManager.getVersions();
                            }
                            else
                            {
                                Command loginComm = new ComandoLogin(connectionSettings.readSetting("account", "username"), connectionSettings.readSetting("account", "password"));
                                loginComm.esegui();
                            }

                            //await syncManager.restoreFileVersion(selectedFileName, selectedVersion);
                            //System.Windows.MessageBox.Show("Restore Done!", "Restoring system");
                        }
                        catch (ServerException ex)
                        {
                            System.Windows.MessageBox.Show("Restore failed\n" + ex.Message, "Restoring system", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    break;
                }
                obj = VisualTreeHelper.GetParent(obj);
            }

        }
    }

    class VersionListViewItem
    {
        public string sFilename { get; set; }
        public string sPath { get; set; }
        public VersionListViewItem(string filename, string path)
        {
            sFilename = filename;
            sPath = path;
        }
    }

    class FileVersionListViewItem
    {
        public string sVersion { get; set; }
        public DateTime sTimestamp { get; set; }
        public FileVersionListViewItem(string version, DateTime timestamp)
        {
            sVersion = version;
            sTimestamp = timestamp;
        }
    }
}