using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MergeSplit {
  /// <summary>
  /// Interaction logic for MergerSplitter.xaml
  /// </summary>
  public partial class MergerSplitter : Window {
    public MergerSplitter() {
      InitializeComponent();
      fic = new FileToIconConverter();
      fic.DefaultSize = 200;
      mlstMergeItems.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(mlstMergeItems_CollectionChanged);
      mlstSplitItems.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(mlstSplitItems_CollectionChanged);
      nfIcon = new System.Windows.Forms.NotifyIcon();
      //nfIcon.Icon = (System.Drawing.Icon)this.Icon.T;
      //nfIcon.ContextMenu = this.nMenu;
      nfIcon.Text = this.Title;
      mtimer.Tick += new EventHandler(mtimer_Tick);
      stimer.Tick += new EventHandler(stimer_Tick);
      mtimer.Interval = 1000;
      stimer.Interval = 1000;
      //nfIcon.DoubleClick += new System.EventHandler(this.nfIcon_DoubleClick);
    }

    /// <summary>
    /// Form Loaded event handler. The code has been added to give OS look to application. 
    /// By default my WPF application doesn't show OS look :-(, this may be happening due
    /// to SP3 installed in my system.
    /// </summary>
    private void OnLoad(object sender, RoutedEventArgs e) {
      pbMerger.Value = 0;
      //Uri uri = new Uri("PresentationFramework.Aero;V3.0.0.0;31bf3856ad364e35;component\\themes/aero.normalcolor.xaml", UriKind.Relative);
      //Resources.MergedDictionaries.Add(Application.LoadComponent(uri) as ResourceDictionary);
      lbmTime.Content = "";
      lbsTime.Content = "";
      pbSplitter.Value = 0;
      headerblock.Text = "File Merger/Splitter";
    }

    private void OnTabChange(object sender, SelectionChangedEventArgs e) {
      if (mstab.SelectedIndex == 0) {
        LinearGradientBrush brm = new LinearGradientBrush(Colors.Blue, Colors.Green, 0.5);
        headerborder.Background = brm;
        appborder.BorderBrush = brm;
        //headerblock.Text = "File Merger";
      } else if (mstab.SelectedIndex == 1) {
        LinearGradientBrush brs = new LinearGradientBrush(Colors.Green, Colors.Blue, 0.5);
        headerborder.Background = brs;
        appborder.BorderBrush = brs;
        //headerblock.Text = "File Splitter";
      }
    }

    /// <summary> Checks numeric part extension and return true if found else false. </summary>
    /// <param name="strExt"> Extenstion to be compared. </param>
    /// <returns> true if found else false </returns>
    private bool InOneOfNumeric(string strExt) {
      string[] strExts = { ".000", ".001", ".002", ".003", ".004", ".005", ".006", ".007", ".008", ".009", ".010" };
      foreach (string str in strExts)
        if (strExt == str) return true;
      return false;
    }

    /// <summary> Shows message at status label for merge and split.  </summary>
    /// <param name="msg"> Message to be displayed. </param>
    /// <param name="iError"> if true shown in red foreground else black. </param>
    private void ShowMsg(string msg, bool iError) {
      if (iError) headerblock.Foreground = Brushes.Red;
      else headerblock.Foreground = Brushes.White;
      headerblock.Text = msg;
    }

    private void ChangeImage(Button ctl, Image img, string ttip) {
      StackPanel objSpBtn = null;
      TextBlock objtbBtn = null;
      objSpBtn = new StackPanel();
      objSpBtn.Background = Brushes.Transparent;
      objSpBtn.Orientation = Orientation.Horizontal;
      objSpBtn.Children.Add(img);
      objtbBtn = new TextBlock();
      objSpBtn.Children.Add(objtbBtn);
      ctl.Content = objSpBtn;
      ctl.ToolTip = ttip;
    }

    private void mSelf_Closed(object sender, EventArgs e) {
      CloseMergeStreams();
      miAppStop = true;
    }

    /// <summary> Updates split/merge time to label. </summary>
    /// <param name="nTick">Total tick</param>
    /// <param name="tsl">label to update</param>
    private void SetTime(long nTick, Label tsl) {
      if (nTick < 60) tsl.Content = "00:00:" + ((nTick < 10) ? "0" + nTick.ToString() : nTick.ToString());
      else if (nTick >= 60 && nTick < 3600) {
        int nMinutes = (int)(nTick / 60);
        int nSeconds = (int)(nTick - 60 * nMinutes);
        string strMin = (nMinutes < 10) ? "0" + nMinutes.ToString() : nMinutes.ToString();
        string strSec = (nSeconds < 10) ? "0" + nSeconds.ToString() : nSeconds.ToString();
        tsl.Content = "00:" + strMin + ":" + strSec;
      } else {
        int nHours = (int)(nTick / 3600);
        int nMinutes = (int)((nTick - 60 * nHours) / 60);
        int nSeconds = (int)(nTick - 3600 * nHours - 60 * nMinutes);
        string strHr = (nHours < 10) ? "0" + nHours.ToString() : nHours.ToString();
        string strMin = (nMinutes < 10) ? "0" + nMinutes.ToString() : nMinutes.ToString();
        string strSec = (nSeconds < 10) ? "0" + nSeconds.ToString() : nSeconds.ToString();
        tsl.Content = strHr + ":" + strMin + ":" + strSec;
      }
    }

    public delegate void WriteResultantFile();
    private string mstrArg = "";
    private bool miStopm = false, miAppStop = false, miStops = false;
    static FileToIconConverter fic;
    System.Windows.Forms.Timer mtimer = new System.Windows.Forms.Timer();
    System.Windows.Forms.Timer stimer = new System.Windows.Forms.Timer();
    System.Windows.Forms.OpenFileDialog mofdOpenSplitSrc = new System.Windows.Forms.OpenFileDialog();
    System.Windows.Forms.SaveFileDialog msfdSaveMergeRes = new System.Windows.Forms.SaveFileDialog();

    //public static class Command {
    //   public static readonly RoutedUICommand DoSometing = new RoutedUICommand("Do something", "DoSomething", typeof(MergerSplitter));
    //   public static readonly RoutedUICommand SomeOtherAction = new RoutedUICommand("Some other action", "SomeOtherAction", typeof(MergerSplitter));
    //   public static readonly RoutedUICommand MoreDeeds = new RoutedUICommand("More deeds", "MoreDeeeds", typeof(MergerSplitter));
    //}
  }
}