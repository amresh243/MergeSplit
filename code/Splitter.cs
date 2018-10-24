using AmrajCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MergeSplit {
  /// <summary>
  /// Interaction logic for MergerSplitter.xaml
  /// </summary>
  public partial class MergerSplitter : Window {
    private void OnPlayPauseS(object sender, RoutedEventArgs e) {
      Image myImage = new Image();
      if (miPPS == false) {
        myImage.Source = new BitmapImage(new Uri("..\\..\\res/play.png", UriKind.Relative));
        ChangeImage(btPPS, myImage, "Resume");
        miPPS = true;
        stimer.Stop();
      } else {
        myImage.Source = new BitmapImage(new Uri("..\\..\\res/pause.png", UriKind.Relative));
        ChangeImage(btPPS, myImage, "Pause");
        miPPS = false;
        stimer.Start();
      }
    }

    /// <summary> Event handler for stop split button. Stops the split operation. </summary>
    private void OnClickSStop(object sender, RoutedEventArgs e) {
      try {
        miStops = true;
        btStopS.Visibility = Visibility.Hidden;
        btPPS.Visibility = Visibility.Hidden;
        Image myImage = new Image();
        myImage.Source = new BitmapImage(new Uri("..\\..\\res/pause.png", UriKind.Relative));
        ChangeImage(btPPS, myImage, "Pause");
        if (miPPS == true) miPPM = false;
        ShowMsg("Aborted by user.", true);
        EnableSplit(true);
      } catch (Exception) {
        ShowMsg("Aborted by user.", true);
      }
    }

    /// <summary> Enables/Disables split operation. </summary>
    /// <param name="iEnable"> if true enables else disables. </param>
    private void EnableSplit(bool iEnable) {
      try {
        /*for (int i = 0; i < mcmSplit.Items.Count; i++) {
           if (i == 5 || i == 6 || i == 10) mcmSplit.Items[i].Enabled = !iEnable;
           else if (i != 3) mcmSplit.Items[i].Enabled = iEnable;
        }*/
        //mtsmPPS.Image = mbtSPP.Image;
        rbSBS.IsEnabled = iEnable;
        rbSBN.IsEnabled = iEnable;
        tbSB.IsEnabled = iEnable;
        tbSBS.IsEnabled = iEnable;
        tbSBN.IsEnabled = iEnable;
        btCF.IsEnabled = iEnable;
        cbSST.IsEnabled = iEnable;
        chbDSAF.IsEnabled = iEnable;
        btSSR.IsEnabled = iEnable;
        miSplitting = !iEnable;
        btLSF.IsEnabled = iEnable;
        stimer.Start();
        if (iEnable) {
          for (int i = 0; i < mlstSplitItems.Count; i++) {
            SplitItem si = mlstSplitItems[i];
            if (si.Stream != null) si.Stream.Close();
          }
          if (sfsInput != null) sfsInput.Close();
          stimer.Stop();
          mnTicks = 1;
          pbSplitter.Value = 0;
          lbsTime.Content = "";
          tbsVal.Text = "";
        }
      } catch (Exception) { }
    }

    /// <summary> Calculates the number of splitted file. </summary>
    /// <returns> No. of splitted files. </returns>
    int GetSplittedFileNumber() {
      if (File.Exists(mstrSplitSrc)) {
        FileInfo fileSrc = new FileInfo(mstrSplitSrc);
        mnSrcSize = fileSrc.Length;
        if (mnSrcSize == 0) return 0;
      } else return 0;
      long nFileSize = 0;
      int nFiles = 0;
      if (rbSBS.IsChecked == true && mnSrcSize != 0) {
        if (cbSST.SelectedIndex >= 0 && cbSST.SelectedIndex <= 3) {
          nFileSize = long.Parse(tbSBS.Text);
          if (cbSST.SelectedIndex == 0) nFileSize *= 1;
          else if (cbSST.SelectedIndex == 1) nFileSize *= 1024;
          else if (cbSST.SelectedIndex == 2) nFileSize *= (1024 * 1024);
          else if (cbSST.SelectedIndex == 3) nFileSize *= (1024 * 1024 * 1024);
          else if (cbSST.SelectedIndex == 4) nFileSize *= 1509949;
          else if (cbSST.SelectedIndex == 5) nFileSize *= (10 * 1024 * 1024);
          else if (cbSST.SelectedIndex == 6) nFileSize *= (700 * 1024 * 1024);
          if (nFileSize >= mnSrcSize) return 0;
          nFiles = (int)(mnSrcSize / nFileSize);
          long nLeftSize = mnSrcSize - (nFiles * nFileSize);
          if (nLeftSize > 0) nFiles++;
        } else if (cbSST.SelectedIndex > 3 && mnSrcSize != 0) {
          long nMediaSize = 0;
          if (cbSST.SelectedIndex == 4) nMediaSize = 1509949;
          else if (cbSST.SelectedIndex == 5) nMediaSize = 10 * 1024 * 1024;
          else if (cbSST.SelectedIndex == 6) nMediaSize = 700 * 1024 * 1024;
          nFiles = (int)(mnSrcSize / nMediaSize);
          if (nFiles == 0) {
            tbSBS.Text = "";
            return 0;
          }
          long nLeftSize = mnSrcSize - (nFiles * nMediaSize);
          if (nLeftSize > 0) nFiles++;
          tbSBS.Text = "1";
        }
        return nFiles;
      } else {
        nFiles = Int32.Parse(tbSBN.Text);
        return nFiles;
      }
    }

    /// <summary> Sets split parameters. </summary>
    /// <returns> true if success else false. </returns>
    private bool SetSplitter() {
      try {
        FileInfo fileSrc = new FileInfo(mstrSplitSrc);
        mnSrcSize = fileSrc.Length;
        int nFiles = 0;
        long nLastFileSize = 0;
        long nSplitSize = 0;
        mlstSplitItems.Clear();
        if (rbSBS.IsChecked == true) {
          nSplitSize = long.Parse(tbSBS.Text);
          if (cbSST.SelectedIndex == 0) nSplitSize *= 1;
          else if (cbSST.SelectedIndex == 1) nSplitSize *= 1024;
          else if (cbSST.SelectedIndex == 2) nSplitSize *= (1024 * 1024);
          else if (cbSST.SelectedIndex == 3) nSplitSize *= (1024 * 1024 * 1024);
          else if (cbSST.SelectedIndex == 4) nSplitSize *= 1509949;
          else if (cbSST.SelectedIndex == 5) nSplitSize *= (10 * 1024 * 1024);
          else if (cbSST.SelectedIndex == 6) nSplitSize *= (700 * 1024 * 1024);
          if (nSplitSize >= mnSrcSize) {
            ShowMsg("Split size can't be greater than source size.", true);
            return false;
          } else nFiles = (int)(mnSrcSize / nSplitSize);
          nLastFileSize = mnSrcSize - (nFiles * nSplitSize);
        } else {
          nFiles = Int32.Parse(tbSBN.Text);
          if (nFiles == 1) {
            ShowMsg("Total splitted file can't be less than 2.", true);
            return false;
          }
          nSplitSize = mnSrcSize / nFiles;
          if (nSplitSize == 0) {
            ShowMsg("Total file size can't be less than a single byte.", true);
            return false;
          }
          nLastFileSize = mnSrcSize - ((nFiles - 1) * nSplitSize);
          nFiles--;
        }
        if (nFiles > 100) {
          if (MessageBox.Show(this, "There are two many split files. Want to continue?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            return false;
        }
        for (int i = 0; i < nFiles; i++) {
          SplitItem si = new SplitItem();
          si.SN = i + 1;
          si.Name = fileSrc.Name + "_" + (i + 1).ToString() + ".part";
          si.SFile = fileSrc.FullName + "_" + (i + 1).ToString() + ".part";
          si.Size = nSplitSize;
          mlstSplitItems.Add(si);
        }
        if (nLastFileSize != 0) {
          SplitItem si = new SplitItem();
          si.SN = mlstSplitItems.Count + 1;
          si.Name = fileSrc.Name + "_" + (mlstSplitItems.Count + 1).ToString() + ".part";
          si.SFile = fileSrc.FullName + "_" + (mlstSplitItems.Count + 1).ToString() + ".part";
          si.Size = nLastFileSize;
          mlstSplitItems.Add(si);
        }
        mlbFiles.Text = "Files: " + mlstSplitItems.Count.ToString();
        ShowMsg("Total: " + mlstSplitItems.Count.ToString() + " split files. Source: " + Utils<string>.GetFormattedSize(mnSrcSize), false);
        splitlist.Items.Refresh();
        //FillSplitListView();
        return true;
      } catch (Exception) {
        ShowMsg("Split parameters are not correct.", true);
        return false;
      }
    }

    /// <summary> Event handler for save split list button. saves a split file list. </summary>
    private void OnSaveSList(object sender, RoutedEventArgs e) {
      if (mlstSplitItems.Count == 0) {
        ShowMsg("Nothing to save.", true);
        return;
      }
      msfdSaveMergeRes.Title = "Save split list to file...";
      msfdSaveMergeRes.Filter = "SplitList files (*.slt)|*.slt";
      FileInfo file = new FileInfo(mstrSplitSrc);
      msfdSaveMergeRes.FileName = file.Name.Replace(file.Extension, "").ToUpper() + ".SLT";
      if (msfdSaveMergeRes.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
        StreamWriter sw = new StreamWriter(msfdSaveMergeRes.FileName);
        sw.WriteLine("@splitlist");
        sw.WriteLine("{0}", mstrSplitSrc);
        for (int i = 0; i < mlstSplitItems.Count; i++) {
          SplitItem si = mlstSplitItems[i];
          sw.WriteLine("{0},{1}", si.Name, si.Size);
        }
        sw.Close();
      }
    }

    /// <summary> Event handler for load split list button. Loads a split file list. </summary>
    private void OnLoadSList(object sender, RoutedEventArgs e) {
      mofdOpenSplitSrc.Title = "Load split list file...";
      mofdOpenSplitSrc.Filter = "SplitList files (*.slt)|*.slt";
      if (mofdOpenSplitSrc.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
        try {
          pbSplitter.Value = 0;
          tbsVal.Text = "";
          mstrArg = mofdOpenSplitSrc.FileName;
          LoadSplitList();
        } catch (Exception) { }
      }
    }

    /// <summary> Loads split list file. </summary>
    void LoadSplitList() {
      try {
        if (!File.Exists(mstrArg)) return;
        StreamReader sr = File.OpenText(mstrArg);
        string strFile = sr.ReadLine();
        if (strFile.ToLower().CompareTo("@splitlist") != 0) {
          ShowMsg("Invalid split list.", true);
          return;
        }
        strFile = sr.ReadLine();
        if (!File.Exists(strFile)) {
          ShowMsg("Split source is missing.", true);
          return;
        }
        string strSplitFile = strFile;
        strFile = sr.ReadLine();
        char[] chTok = { ',' };
        List<SplitItem> lstSplitItems = new List<SplitItem>();
        while (strFile != null) {
          string[] strSplitPart = strFile.Split(chTok);
          if (strSplitPart.Length < 2) {
            lstSplitItems.Clear();
            ShowMsg("Invalid part file.", true);
            sr.Close();
            return;
          }
          SplitItem sItem = new SplitItem();
          for (int i = 0; i < strSplitPart.Length - 1; i++) {
            FileInfo files = new FileInfo(strSplitPart[i]);
            sItem.Name += files.Name;
            sItem.SFile += files.FullName;
          }
          sItem.Size = long.Parse(strSplitPart[strSplitPart.Length - 1]);
          lstSplitItems.Add(sItem);
          strFile = sr.ReadLine();
        }
        sr.Close();
        FileInfo fileSplit = new FileInfo(strSplitFile);
        long nSrcSize = fileSplit.Length;
        long nSplitSize = 0;
        for (int i = 0; i < lstSplitItems.Count; i++) nSplitSize += lstSplitItems[i].Size;
        if ((nSrcSize != nSplitSize) || lstSplitItems.Count == 0) {
          ShowMsg("Source size mismatches with total split size.", true);
          return;
        }
        ResetSplitter();
        for (int i = 0; i < lstSplitItems.Count; i++) mlstSplitItems.Add(lstSplitItems[i]);
        mstrSplitSrc = strSplitFile;
        tbSS.Text = mstrSplitSrc;
        SetSrcImage(fileSplit.FullName);
        ShowMsg("Split Source Size: " + Utils<string>.GetFormattedSize(fileSplit.Length), false);
        SetSrcImage(strSplitFile);
        SetSplitSizeParam();
        mlbFiles.Text = "Files: " + mlstSplitItems.Count.ToString();
        if (!SetSplitter()) {
          ShowMsg("Couldn't set split parameters properly.", true);
          return;
        }
        SetSplitTitle();
      } catch (Exception ex) {
        ShowMsg(ex.Message, true);
      }
    }

    /// <summary> Sets split title </summary>
    private void SetSplitTitle() {
      if (mstrSplitSrc.Length != 0) {
        FileInfo file = new FileInfo(mstrSplitSrc);
        this.Title = file.Name + " - Split file";
      } else this.Title = "Split file";
      if (this.Title.Length > 60) nfIcon.Text = "Split file";
      else nfIcon.Text = this.Title;
    }

    /// <summary> Sets file size text and size type. </summary>
    private void SetSplitSizeParam() {
      long nSize = mlstSplitItems[0].Size;
      long nKB = 1024, nMB = 1024 * 1024, nGB = 1024 * 1024 * 1024;
      int nFraction = 0;
      long nLeft = 0;
      if (nSize >= nKB && nSize < nMB) {
        nFraction = (int)(nSize / nKB);
        nLeft = nSize - nKB * nFraction;
        if (nLeft != 0) tbSBS.Text = nSize.ToString();
        else {
          tbSBS.Text = nFraction.ToString();
          cbSST.SelectedIndex = 1;
        }
      } else if (nSize >= nMB && nSize < nGB) {
        nFraction = (int)(nSize / nMB);
        nLeft = nSize - nMB * nFraction;
        if (nLeft != 0) tbSBS.Text = nSize.ToString();
        else {
          tbSBS.Text = nFraction.ToString();
          cbSST.SelectedIndex = 2;
        }
      } else if (nSize >= nGB) {
        nFraction = (int)(nSize / nGB);
        nLeft = nSize - nGB * nFraction;
        if (nLeft != 0) tbSBS.Text = nSize.ToString();
        else {
          tbSBS.Text = nFraction.ToString();
          cbSST.SelectedIndex = 3;
        }
      } else tbSBS.Text = nSize.ToString();
    }

    /// <summary> Event handler for change index of combo. </summary>
    private void OnSTIndexChange(object sender, SelectionChangedEventArgs e) {
      if (mlbFiles == null) return;
      try {
        int nFiles = GetSplittedFileNumber();
        mlbFiles.Text = "Files: " + nFiles.ToString();
      } catch (Exception) {
        ShowMsg("Invalid size.", true);
      }
    }

    private void OnBySize(object sender, RoutedEventArgs e) {
      if (tbSBS == null) return;
      tbSBS.IsEnabled = rbSBS.IsChecked.Value;
      cbSST.IsEnabled = rbSBS.IsChecked.Value;
      tbSBN.IsEnabled = !rbSBS.IsChecked.Value;
    }

    private void OnByNumber(object sender, RoutedEventArgs e) {
      if (tbSBN == null) return;
      tbSBS.IsEnabled = !rbSBN.IsChecked.Value;
      cbSST.IsEnabled = !rbSBN.IsChecked.Value;
      tbSBN.IsEnabled = !rbSBS.IsChecked.Value;
    }

    /// <summary> Event handler for browse split source button. </summary>
    private void OnSplitSrc(object sender, RoutedEventArgs e) {
      try {
        mofdOpenSplitSrc.Title = "Select file to be splitted...";
        mofdOpenSplitSrc.Filter = "";
        if (mofdOpenSplitSrc.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
          if (File.Exists(mstrMergeFile) && miMerging && mstrMergeFile.CompareTo(mofdOpenSplitSrc.FileName) == 0) {
            ShowMsg("Wait, current file is under merge process.", true);
            return;
          }
          ResetSplitter();
          mstrSplitSrc = mofdOpenSplitSrc.FileName;
          tbSS.Text = mstrSplitSrc;
          FileInfo fileSrc = new FileInfo(mstrSplitSrc);
          if (fileSrc.Extension.ToLower() == ".lnk")
            LoadLinkFileS(fileSrc.FullName);
          else {
            btSS.Content = Utils<string>.GetFormattedSize(fileSrc.Length);
            mnSrcSize = fileSrc.Length;
            this.Title = fileSrc.Name + " - Split file";
          }
          if (this.Title.Length > 60) nfIcon.Text = "Split file";
          else nfIcon.Text = this.Title;
          ShowMsg("Split Source Size: " + Utils<string>.GetFormattedSize(mnSrcSize), false);
          SetSrcImage(mstrSplitSrc);
        }
      } catch (Exception ex) { ShowMsg(ex.Message, true); }
    }

    /// <summary> Event handler for calculate button. Caculates split file. </summary>
    private void OnCalculate(object sender, RoutedEventArgs e) {
      SetSplitter();
    }

    /// <summary> Loads given link file target into split (if possible, else loads link file itself). </summary>
    /// <param name="strLinkFile"> link file name with full path. </param>
    private void LoadLinkFileS(string strLinkFile) {
      mstrArg = Utils<string>.GetSourceOfLink(strLinkFile);
      FileInfo file = null;
      if (File.Exists(mstrArg)) file = new FileInfo(mstrArg);
      else file = new FileInfo(strLinkFile);
      this.Title = file.Name + " - Split file";
      mstrSplitSrc = file.FullName;
      tbSS.Text = mstrSplitSrc;
      mnSrcSize = file.Length;
    }

    /// <summary> Set browse button image with the source image. </summary>
    /// <param name="strFile"> Source file whose icon has to be set. </param>
    private void SetSrcImage(string strFile) {
      try {
        Image img = new Image();
        FileInfo file = new FileInfo(strFile);
        img.Source = fic.GetImage(strFile, 16);
        StackPanel objSpBtn = null;
        TextBlock objtbBtn = null;
        objSpBtn = new StackPanel();
        objSpBtn.Background = Brushes.Transparent;
        objSpBtn.Orientation = Orientation.Horizontal;
        objSpBtn.Children.Add(img);
        objtbBtn = new TextBlock();
        objtbBtn.TextAlignment = TextAlignment.Right;
        double bw = sgrid.ActualHeight / 24;
        objtbBtn.Margin = new Thickness(0, bw - 10, 0, 0);
        objtbBtn.Text = Utils<string>.GetFormattedSize(file.Length);
        objSpBtn.Children.Add(objtbBtn);
        btSS.Content = objSpBtn;
      } catch (Exception) { }
    }

    /// <summary> Event handler for save split button. </summary>
    private void OnSaveS(object sender, RoutedEventArgs e) {
      try {
        if (!File.Exists(mstrSplitSrc)) {
          ShowMsg("First set the source file before split.", true);
          return;
        }
        if (mlstSplitItems.Count == 0) if (!SetSplitter()) return;
        if (mlstSplitItems.Count == 0) {
          ShowMsg("First set the parameters before split.", true);
          return;
        }
        if (mfbdSaveSplitLoc.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
          mstrSplitLocation = mfbdSaveSplitLoc.SelectedPath;
          SetSplitFilePath();
          btStopS.Visibility = Visibility.Visible;
          btPPS.Visibility = Visibility.Visible;
          try {
            long nBuf = long.Parse(tbMB.Text);
            if (nBuf <= 0 || nBuf >= 512 * 1024 * 1024) {
              ShowMsg("Invalid buffer size.", true);
              tbSB.Text = "8";
            } else mnBufS = nBuf * 1024 * 1024;
          } catch (Exception) {
            ShowMsg("Invalid buffer size.", true);
            tbSB.Text = "8";
          }
          this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new WriteResultantFile(WriteSplittedFiles));
          //WriteSplittedFiles();
        }
      } catch (Exception ex) { ShowMsg(ex.Message, true); }
    }

    /// <summary> Worker thread function to split the files. </summary>
    private void WriteSplittedFiles() {
      try {
        ShowMsg("Splitting...", false);
        pbSplitter.Value = 0;
        tbsVal.Text = "";
        lbsTime.Content = "";
        EnableSplit(false);
        miStops = false;
        double nFactor = ((double)mnSrcSize) / 100;
        if (nFactor == 0) {
          nFactor = 1;
          pbSplitter.Maximum = (int)mnSrcSize;
        } else pbSplitter.Maximum = 100;
        sfsInput = File.OpenRead(mstrSplitSrc);
        int nCurrentIndex = 0;
        long nBytesWritten = 0, nTotalBytesWritten = 0;
        long nFirstItemSize = mlstSplitItems[0].Size;
        int nStoreBytes = (nFirstItemSize <= mnBufS) ? (int)nFirstItemSize : (int)mnBufS;
        byte[] bytes = new byte[nStoreBytes];
        int nBytes = sfsInput.Read(bytes, 0, nStoreBytes);
        SplitItem si = mlstSplitItems[nCurrentIndex];
        if (File.Exists(si.SFile)) File.Delete(si.SFile);
        si.Stream = File.OpenWrite(si.SFile);
        FileInfo fileSplit = new FileInfo(si.SFile);
        splitlist.SelectedItems.Clear();
        string msg = "Splitting: " + fileSplit.Name + "...";
        ShowMsg(msg, false);
        splitlist.SelectedItems.Add(mlstSplitItems[nCurrentIndex]);
        while (nBytes != 0) {
          if (miAppStop == true || miStops == true) {
            OnClickSStop(null, null);
            return;
          }
          if (miPPS == true) {
            Thread.Sleep(100);
            System.Windows.Forms.Application.DoEvents();
            continue;
          }
          if (nBytesWritten < si.Size) {
            si.Stream.Write(bytes, 0, nBytes);
            nBytesWritten = si.Stream.Length;
            nTotalBytesWritten += nBytes;
            int nPos = (int)(nTotalBytesWritten / nFactor);
            bool iShow = (nPos > pbSplitter.Value) ? true : false;
            pbSplitter.Value = nPos;
            tbsVal.Text = nPos.ToString() + "%";
            System.Windows.Forms.Application.DoEvents();
            nfIcon.Text = tbsVal.Text + " Completed.";
            long nLeftBytes = si.Size - nBytesWritten;
            if ((nBytesWritten + nStoreBytes) < si.Size)
              nBytes = sfsInput.Read(bytes, 0, nStoreBytes);
            else if (nLeftBytes > 0)
              nBytes = sfsInput.Read(bytes, 0, (int)(nLeftBytes));
            if (nBytesWritten < nStoreBytes) {
              nBytesWritten = (int)si.Size;
              continue;
            }
          } else {
            si.Stream.Close();
            si.IsCompleted = true;
            nCurrentIndex++;
            nBytesWritten = 0;
            if (nCurrentIndex == mlstSplitItems.Count) {
              nBytes = 0;
              continue;
            }
            si = mlstSplitItems[nCurrentIndex];
            if (File.Exists(si.SFile)) File.Delete(si.SFile);
            si.Stream = File.OpenWrite(si.SFile);
            msg = "Splitting: " + fileSplit.Name + "...";
            ShowMsg(msg, false);
            splitlist.SelectedItems.Add(mlstSplitItems[nCurrentIndex]);
            fileSplit = new FileInfo(si.SFile);
            nBytes = sfsInput.Read(bytes, 0, nStoreBytes);
          }
        }
        si.Stream.Close();
        si.IsCompleted = true;
        EnableSplit(true);
        pbSplitter.Value = pbSplitter.Maximum;
        tbsVal.Text = "100%";
        nfIcon.Text = this.Title;
        sfsInput.Close();
        btStopS.Visibility = Visibility.Hidden;
        btPPS.Visibility = Visibility.Hidden;
        if (chbDSAF.IsChecked == true) {
          tbSS.Text = "";
          File.Delete(mstrSplitSrc);
          mstrSplitSrc = "";
        }
        if (chbGSR.IsChecked == true) GenerateSplitReport();
        ShowMsg("Splitting Completed.", false);
      } catch (Exception ex) {
        if (miSplitting) ShowMsg(ex.Message, true);
        EnableSplit(true);
        btStopS.Visibility = Visibility.Hidden;
        btPPS.Visibility = Visibility.Hidden;
      }
    }

    /// <summary> Generates split report </summary>
    private void GenerateSplitReport() {
      ShowMsg("Writing Split Report...", false);
      string strReportFile = mstrSplitLocation + "\\SplitReport.htm";
      if (File.Exists(strReportFile)) File.Delete(strReportFile);
      StreamWriter srRep = new StreamWriter(strReportFile);
      WriteReportHeader("Split Report of", mstrSplitSrc, srRep, "bgcolor=\"#009900\"");
      string strBG = "";
      for (int i = 0; i < mlstSplitItems.Count; i++) {
        SplitItem si = mlstSplitItems[i];
        if (i % 2 != 0) strBG = "bgcolor=\"#EAEAEA\"";
        else strBG = "bgcolor=\"#FFFFFF\"";
        srRep.WriteLine("<tr>");
        srRep.WriteLine("<td width=\"45\" height=\"11\" {0}><b>", strBG);
        srRep.WriteLine("<font color=\"#009900\" face=\"Verdana\" size=\"2\">{0}</font></b></td>", i + 1);
        srRep.WriteLine("<td width=\"865\" height=\"11\" {0}><b>", strBG);
        srRep.WriteLine("<font face=\"Verdana\" size=\"2\" color=\"#009900\">{0}</font></b></td>", si.SFile);
        srRep.WriteLine("<td width=\"150\" height=\"11\" {0}><b>", strBG);
        srRep.WriteLine("<font color=\"#009900\" face=\"Verdana\" size=\"2\">{0} Bytes</font></b></td>", si.Size);
        srRep.WriteLine("</tr>");
      }
      WriteReportFooter(mlstSplitItems.Count, srRep);
      srRep.Close();
    }

    /// <summary> Sets the split files. </summary>
    private void SetSplitFilePath() {
      for (int i = 0; i < mlstSplitItems.Count; i++) {
        SplitItem si = mlstSplitItems[i];
        si.SFile = mstrSplitLocation + "\\" + si.Name;
      }
    }

    ///<summary> Resets split </summary>
    void ResetSplitter() {
      pbSplitter.Value = 0;
      tbsVal.Text = "";
      btSS.Content = "";
      tbSS.Text = "";
      mlstSplitItems.Clear();
      mstrSplitSrc = "";
      mnSrcSize = 0;
      cbSST.SelectedIndex = 0;
      mlbFiles.Text = "Files: 0";
      tbSBS.Text = "";
      ShowMsg("", false);
    }

    void stimer_Tick(object sender, EventArgs e) {
      mnTicks++;
      SetTime(mnTicks, lbsTime);
    }

    void mlstSplitItems_CollectionChanged(Object sender, NotifyCollectionChangedEventArgs e) {
      splitlist.Items.Refresh();
    }

    public ObservableCollection<SplitItem> SplitItems { get { return mlstSplitItems; } }

    /// <summary> Split Item. Contains file name and file size. </summary>
    public class SplitItem {
      public SplitItem() {
        Image myImage = new Image();
        myImage.Width = 16;
        myImage.Height = 16;
        myImage.Source = new BitmapImage(new Uri("..\\..\\res/split.png", UriKind.Relative));
        img = myImage.Source;
      }

      /// <summary> Default distructor. Deletes uncompleted file. </summary>
      ~SplitItem() { if (!miCompleted && File.Exists(mstrFile)) File.Delete(mstrFile); }

      /// <summary> Get/set file name. </summary>
      /// <summary> Get/set serial number. </summary>
      public int SN {
        get { return mnSn; }
        set { mnSn = value; }
      }

      public string Number {
        get {
          mstrNumber = mnSn.ToString() + ".";
          return mstrNumber;
        }
        set { mstrNumber = value; }
      }

      /// <summary> Get/set file name. </summary>
      public string Name {
        get { return mstrName; }
        set { mstrName = value; }
      }

      public string SFile {
        get { return mstrFile; }
        set { mstrFile = value; }
      }

      /// <summary> Get/set file size. </summary>
      public long Size {
        get { return mnSize; }
        set { mnSize = value; }
      }

      public string FSize {
        get { return Utils<string>.GetFormattedSize(mnSize); }
      }

      public ImageSource Icon {
        get { return img; }
        set { img = value; }
      }

      /// <summary> Get/set file stream. </summary>
      public FileStream Stream {
        get { return mfs; }
        set { mfs = value; }
      }

      /// <summary> Get/set complete status. </summary>
      public bool IsCompleted {
        get { return miCompleted; }
        set { miCompleted = value; }
      }

      private ImageSource img = null;
      private string mstrNumber = "";
      private int mnSn = 0;
      private string mstrName = "";
      private string mstrFile = "";
      private long mnSize;
      private FileStream mfs = null;
      bool miCompleted = false;
    }
    ObservableCollection<SplitItem> mlstSplitItems = new ObservableCollection<SplitItem>();
    private bool miPPS = false;
    string mstrSplitLocation = "";
    System.Windows.Forms.FolderBrowserDialog mfbdSaveSplitLoc = new System.Windows.Forms.FolderBrowserDialog();
    FileStream sfsInput = null;
    private string mstrSplitSrc = "";
    private bool miSplitting = false;
    long mnTicks = 1;
    long mnSrcSize = 0;
    long mnBufS = 8388608;
  }
}