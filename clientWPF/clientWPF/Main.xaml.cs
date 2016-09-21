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
        
        private bool loggedin = false;
        private NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenu notifyIconMenu;
        private ConnectionSettings connectionSettings;
        private FileUtente selectedFileUtente;
        private DateTime selectedFileVersion;
        private DateTime deletedFileVersion;
        private FileUtente deletedFileUtente;

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
            GetFiles();
            GetDelFiles();
        }

        private void GetFiles()
        {
            try
            {
                lDetails.Items.Clear();
                lFileVersions.Items.Clear();


                FileUtenteList list = new FileUtenteList();
                for (int i=0; i<list.Length; i++)
                {
                    lDetails.Items.Add(new VersionListViewItem(list[i].Nome, list[i].Path, list[i].TempoModifica));
                }

                lDetails.SelectedIndex = 0;

            }
            catch (Exception ex)
            {
                updateStatus(ex.Message);
            }
        }

        private void GetDelFiles()
        {
            try
            {
                lDeletedFiles.Items.Clear();

                FileUtenteList fileUtenteList = new FileUtenteList();

                foreach (FileUtente fu in fileUtenteList.Deleted)
                {
                    lDeletedFiles.Items.Add(new VersionListViewItem(fu.Nome, fu.Path, fu.TempoModifica));
                }
                lDeletedFiles.SelectedIndex = 0;

            }
            catch (Exception ex)
            {
                updateStatus(ex.Message);
            }
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
                this.GetFiles();
                this.GetDelFiles();
                bStop.IsEnabled = true;
                bSyncNow.IsEnabled = true;
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
            if (!loggedin)
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
                                
                                lw.Close();
                                bLogInOut.IsEnabled = true;
                                return;
                            case Login.LoginResponse.LOGIN:
                                Command loginComm = new ComandoLogin(lw.Username, lw.Password);
                                loginAuthorized = loginComm.esegui();
                                
                                if (!loginAuthorized)
                                {
                                    lw.ErrorMessage = "Login failed";
                                }
                                break;
                            case Login.LoginResponse.REGISTER:
                                Command regComm = new ComandoRegistra(lw.Username, lw.Password);
                                loginAuthorized = regComm.esegui();
                               
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
                Command logoutComm = new ComandoEsci();
                logoutComm.esegui();
            }
            lDetails.Items.Clear();
            lFileVersions.Items.Clear();
            this.openLogin();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
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
            this.GetFiles();
            this.GetDelFiles();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            switch (this.WindowState)
            {
                case WindowState.Maximized:
                    break;
                case WindowState.Minimized:
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
            this.WindowState = WindowState.Normal;
            this.Activate();
            notifyIcon.Visible = false;
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
                        lFileVersions.Items.Add(new FileVersionListViewItem(fv));
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

                    MessageBoxResult res = System.Windows.MessageBox.Show("Do you want to restore file \"" + selectedFileUtente.Nome + "\" with version of " + selectedVersion + " ?", "Restore system", System.Windows.MessageBoxButton.YesNo);

                    if (res == MessageBoxResult.Yes)
                    {
                        try
                        {
                            if (Command.Logged == true)
                            {
                                ControlloModifiche.StopTimer();
                                Command getVersComm = new ComandoScaricaFile(selectedFileUtente.Nome, selectedFileUtente.Path, selectedFileVersion);
                                getVersComm.esegui();
                                ControlloModifiche.Inizializza();
                            }
                            else
                            {
                                Command loginComm = new ComandoLogin(connectionSettings.readSetting("account", "username"), connectionSettings.readSetting("account", "password"));
                                loginComm.esegui();
                            }
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

        private void lDeletedFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject obj = (DependencyObject)e.OriginalSource;
            while (obj != null && obj != lDeletedFiles)
            {
                if (obj.GetType() == typeof(System.Windows.Controls.ListViewItem))
                {
                    string deletedFileName = ((VersionListViewItem)lDeletedFiles.SelectedItem).sFilename;
                    string deletedFilePath = ((VersionListViewItem)lDeletedFiles.SelectedItem).sPath;

                    FileUtenteList fileUtenteList = new FileUtenteList();
                    FileUtente[] deletedFileList = fileUtenteList.Deleted;

                    foreach (FileUtente fu in deletedFileList)
                    {
                        if (fu.Nome == deletedFileName && fu.Path == deletedFilePath)
                        {
                            deletedFileUtente = fu;
                            break;
                        }
                    }

                    lDeletedFileVersions.Items.Clear();
                    foreach (DateTime fv in deletedFileUtente.Items)
                    {
                        lDeletedFileVersions.Items.Add(new FileVersionListViewItem(fv));
                    }
                    break;
                }
                obj = VisualTreeHelper.GetParent(obj);
            }
        }

        private void lDeletedFileVersions_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject obj = (DependencyObject)e.OriginalSource;
            while (obj != null && obj != lDeletedFiles)
            {
                if (obj.GetType() == typeof(System.Windows.Controls.ListViewItem))
                {
                    DateTime deletedVersion = ((FileVersionListViewItem)lDeletedFileVersions.SelectedItem).sTimestamp;

                    foreach (DateTime fv in deletedFileUtente.Items)
                    {
                        if (deletedVersion == fv)
                            deletedFileVersion = fv;
                    }

                    MessageBoxResult res = System.Windows.MessageBox.Show("Do you want to restore file \"" + deletedFileUtente.Nome + "\" with version " + deletedVersion + " ?", "Restore system", System.Windows.MessageBoxButton.YesNo);

                    if (res == MessageBoxResult.Yes)
                    {
                        try
                        {
                            if (Command.Logged == true)
                            {

                                Command getVersComm = new ComandoScaricaFile(deletedFileUtente.Nome, deletedFileUtente.Path, deletedFileVersion);
                                getVersComm.esegui();
                            }
                            else
                            {
                                Command loginComm = new ComandoLogin(connectionSettings.readSetting("account", "username"), connectionSettings.readSetting("account", "password"));
                                loginComm.esegui();
                            }
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
        public DateTime sTime { get; set; }
        public VersionListViewItem(string filename, string path, DateTime time)
        {
            sFilename = filename;
            sPath = path;
            sTime = time;
        }
    }

    class FileVersionListViewItem
    {
        public DateTime sTimestamp { get; set; }
        public FileVersionListViewItem(DateTime timestamp)
        {
            sTimestamp = timestamp;
        }
    }
}