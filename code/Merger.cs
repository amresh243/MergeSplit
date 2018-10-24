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
  /// <summary> Interaction logic for MergerSplitter.xaml </summary>
  public partial class MergerSplitter : Window {
    private void OnPlayPauseM(object sender, RoutedEventArgs e) {
      Image myImage = new Image();
      if (miPPM == false) {
        myImage.Source = new BitmapImage(new Uri("..\\..\\res/play.png", UriKind.Relative));
        ChangeImage(btPPM, myImage, "Resume");
        miPPM = true;
        mtimer.Stop();
      } else {
        myImage.Source = new BitmapImage(new Uri("..\\..\\res/pause.png", UriKind.Relative));
        ChangeImage(btPPM, myImage, "Pause");
        miPPM = false;
        mtimer.Start();
      }
    }

    /// <summary> Event handler for stop merge button. Stops the merge operation. </summary>
    private void OnClickMStop(object sender, RoutedEventArgs e) {
      try {
        miStopm = true;
        btStopM.Visibility = Visibility.Hidden;
        btPPM.Visibility = Visibility.Hidden;
        Image myImage = new Image();
        myImage.Source = new BitmapImage(new Uri("..\\..\\res/pause.png", UriKind.Relative));
        ChangeImage(btPPM, myImage, "Pause");
        if (miPPM == true) miPPM = false;
        ShowMsg("Aborted by user.", true);
        EnableMerge(true);
      } catch (Exception) {
        ShowMsg("Aborted by user.", true);
        CloseMergeStreams();
      }
    }

    /// <summary> Closes all opened file during merge. </summary>
    private void CloseMergeStreams() {
      if (mfsInput != null) mfsInput.Close();
      if (mfsMerge != null) mfsMerge.Close();
    }

    private void OnAdd(object sender, RoutedEventArgs e) {
      mofdOpenSplitSrc.Multiselect = true;
      mofdOpenSplitSrc.ReadOnlyChecked = true;
      mofdOpenSplitSrc.Filter = "";
      mofdOpenSplitSrc.Title = "Choose file to be merged...";
      if (mofdOpenSplitSrc.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
        for (int i = 0; i < mofdOpenSplitSrc.FileNames.Length; i++) {
          try {
            FileInfo file = new FileInfo(mofdOpenSplitSrc.FileNames[i]);
            if (mofdOpenSplitSrc.FileNames.Length == 1 &&
                (file.Extension.ToLower() == ".part" || InOneOfNumeric(file.Extension)) && mcbScanPart.IsChecked == true) {
              mstrArg = file.FullName;
              LoadPartFiles();
              return;
            }
            if (file.Length == 0) {
              ShowMsg("File size can't be zero.", true);
              continue;
            }
            if (file.Extension.ToLower() == ".lnk") {
              LoadLinkFileM(file.FullName);
              continue;
            }
            MergeItem mItem = new MergeItem();
            mItem.SN = mlstMergeItems.Count + 1;
            mItem.Name = file.Name;
            mItem.File = file.FullName;
            mItem.Size = file.Length;
            bool iFound = FindMergeItem(mItem);
            if (iFound) {
              if (MessageBox.Show(this, "File " + mItem.Name + " already exists in the list.\nDo you want to continue(Y/N)?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                continue;
            }
            mlstMergeItems.Add(mItem);
          } catch (Exception ex) { ShowMsg(ex.Message, true); }
        }
        UpdateMsg(0);
      }
      mofdOpenSplitSrc.Multiselect = false;
    }

    /// <summary> Event handler for remove item button. Calls RemoveMergeItem for actual work. </summary>
    private void OnRemove(object sender, RoutedEventArgs e) {
      if (mergelist.Items.Count == 0) {
        ShowMsg("Nothing to remove.", true);
        return;
      }
      bool iRemoved = false;
      if (mergelist.SelectedItems.Count == 0) {
        mlstMergeItems.Remove(mlstMergeItems[mlstMergeItems.Count - 1]);
        iRemoved = true;
      } else for (int i = mergelist.SelectedItems.Count - 1; i >= 0; i--) {
          MergeItem mItem = (MergeItem)mergelist.SelectedItems[i];
          if (mItem != null) mlstMergeItems.Remove(mItem);
          iRemoved = true;
        }
      if (iRemoved) UpdateMsg(0);
    }

    private void OnUp(object sender, RoutedEventArgs e) {
      if (mlstMergeItems.Count == 0) {
        ShowMsg("Nothing to move.", true);
        return;
      }
      if (mergelist.SelectedItems.Count == 0) return;
      bool iMoved = false;
      List<int> lstMoved = new List<int>();
      for (int i = 0; i < mergelist.SelectedItems.Count; i++) {
        MergeItem mItem = (MergeItem)mergelist.SelectedItems[i];
        if (mItem != null) lstMoved.Add(mItem.SN - 1);
      }
      for (int i = 0; i < lstMoved.Count; i++) {
        if (lstMoved[i] == 0) continue;
        MergeItem mItem = mlstMergeItems[lstMoved[i]];
        mlstMergeItems.Remove(mItem);
        mlstMergeItems.Insert(lstMoved[i] - 1, mItem);
        mergelist.SelectedItems.Add(mlstMergeItems[lstMoved[i] - 1]);
        iMoved = true;
      }
      if (iMoved) UpdateMsg(0);
    }

    private void OnDown(object sender, RoutedEventArgs e) {
      if (mlstMergeItems.Count == 0) {
        ShowMsg("Nothing to move.", true);
        return;
      }
      if (mergelist.SelectedItems.Count == 0) return;
      bool iMoved = false;
      List<int> lstMoved = new List<int>();
      for (int i = 0; i < mergelist.SelectedItems.Count; i++) {
        MergeItem mItem = (MergeItem)mergelist.SelectedItems[i];
        if (mItem != null) lstMoved.Add(mItem.SN - 1);
      }
      for (int i = lstMoved.Count - 1; i >= 0; i--) {
        if (lstMoved[i] == mlstMergeItems.Count - 1) continue;
        MergeItem mItem = mlstMergeItems[lstMoved[i]];
        mlstMergeItems.Remove(mItem);
        mlstMergeItems.Insert(lstMoved[i] + 1, mItem);
        mergelist.SelectedItems.Add(mlstMergeItems[lstMoved[i] + 1]);
        iMoved = true;
      }
      if (iMoved) UpdateMsg(0);
    }

    /// <summary> Loads link target into merge list (if possible, else adds link file itself to merge list. </summary>
    /// <param name="strLinkFile"> link file name with full path. </param>
    private void LoadLinkFileM(string strLinkFile) {
      mstrArg = Utils<string>.GetSourceOfLink(strLinkFile);
      if (File.Exists(mstrArg)) {
        FileInfo fileSrc = new FileInfo(mstrArg);
        MergeItem mItem = new MergeItem();
        mItem.File = fileSrc.FullName;
        mItem.Name = fileSrc.Name;
        mItem.Size = fileSrc.Length;
        mItem.SN = mlstMergeItems.Count + 1;
        mlstMergeItems.Add(mItem);
      } else if (Directory.Exists(mstrArg)) {
        DirectoryInfo dir = new DirectoryInfo(mstrArg);
        LoadDirectory(dir);
      } else {
        mstrArg = strLinkFile;
        FileInfo file = new FileInfo(strLinkFile);
        MergeItem mItem = new MergeItem();
        mItem.Name = file.Name;
        mItem.File = file.FullName;
        mItem.Size = file.Length;
        mItem.SN = mlstMergeItems.Count + 1;
        mlstMergeItems.Add(mItem);
      }
    }

    /// <summary> Loads the directory into merge tab </summary>
    /// <param name="dir"> directory to be loaded </param>
    private void LoadDirectory(DirectoryInfo dir) {
      FileInfo[] files = dir.GetFiles();
      for (int i = 0; i < files.Length; i++) {
        FileInfo file = files[i];
        if (file.Length == 0) continue;
        if (file.Extension.ToLower() == ".lnk") {
          LoadLinkFileM(file.FullName);
          continue;
        }
        MergeItem mItem = new MergeItem();
        mItem.Name = file.Name;
        mItem.File = file.FullName;
        mItem.SN = mlstMergeItems.Count + 1;
        mItem.Size = file.Length;
        mlstMergeItems.Add(mItem);
      }
    }

    /// <summary> Sorts given file array on split number pattern. </summary>
    /// <param name="files"> File array to be sorted </param>
    private void SortFilesByNumber(ref FileInfo[] files) {
      for (int i = 0; i < files.Length; i++) {
        for (int j = 0; j < i; j++) {
          string str1 = files[i].Name.Replace(files[i].Extension, "");
          string str2 = files[j].Name.Replace(files[j].Extension, "");
          int n1 = str1.LastIndexOf('_');
          int n2 = str2.LastIndexOf('_');
          if (n1 == -1 || n2 == -1) continue;
          str1 = str1.Remove(0, n1 + 1);
          str2 = str2.Remove(0, n2 + 1);
          n1 = Int32.Parse(str1);
          n2 = Int32.Parse(str2);
          if (n1 < n2) {
            FileInfo tmpfile = files[i];
            files[i] = files[j];
            files[j] = tmpfile;
          }
        }
      }
    }

    /// <summary> Loads all part files in existing part file directory. </summary>
    void LoadPartFiles() {
      FileInfo file = new FileInfo(mstrArg);
      if (file.Length == 0) {
        ShowMsg("File size can't be zero.", true);
        return;
      }
      string strToQualify = file.Name.Replace(file.Extension, "");
      if (file.Extension.ToLower() == ".part") {
        int nLastDot = strToQualify.LastIndexOf(".");
        if (nLastDot != -1) strToQualify = strToQualify.Remove(nLastDot);
        DirectoryInfo dir = file.Directory;
        FileInfo[] files = dir.GetFiles("*.part");
        SortFilesByNumber(ref files);
        for (int i = 0; i < files.Length; i++) {
          FileInfo curFile = files[i];
          if (curFile.Length == 0) continue;
          string strInToFind = curFile.Name.Replace(curFile.Extension, "");
          if (!strInToFind.Contains(strToQualify)) continue;
          MergeItem mItem = new MergeItem();
          mItem.SN = mlstMergeItems.Count + 1;
          mItem.Name = curFile.Name;
          mItem.File = curFile.FullName;
          FileInfo fileP = new FileInfo(mItem.Name);
          mItem.Size = fileP.Length;
          bool iFound = FindMergeItem(mItem);
          if (iFound) {
            if (MessageBox.Show(this, "File " + mItem.Name + " already exists in the list.\nDo you want to continue(Y/N)?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
              continue;
          }
          mlstMergeItems.Add(mItem);
        }
      }
      if (InOneOfNumeric(file.Extension)) {
        bool iNoneAdded = true;
        DirectoryInfo dir = file.Directory;
        FileInfo[] files = dir.GetFiles("*.*");
        int nFileLen = file.FullName.Length;
        for (int i = 0; i <= files.Length; i++) {
          if (!file.FullName.Contains(strToQualify)) continue;
          string strGFileName = dir.FullName + "\\";
          if (i < 10) strGFileName = strGFileName + strToQualify + ".00" + i.ToString();
          else if (i >= 10 && i < 100) strGFileName = strGFileName + strToQualify + ".0" + i.ToString();
          else strGFileName = strGFileName + strToQualify + "." + i.ToString();
          if (!File.Exists(strGFileName) || strGFileName.Length != nFileLen) continue;
          iNoneAdded = false;
          FileInfo curFile = new FileInfo(strGFileName);
          MergeItem mItem = new MergeItem();
          mItem.SN = mlstMergeItems.Count + 1;
          mItem.Name = curFile.Name;
          mItem.File = curFile.FullName;
          mItem.Size = curFile.Length;
          bool iFound = FindMergeItem(mItem);
          if (iFound) {
            if (MessageBox.Show(this, "File " + mItem.Name + " already exists in the list.\nDo you want to continue(Y/N)?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
              continue;
          }
          mlstMergeItems.Add(mItem);
        }
        if (iNoneAdded) {
          MergeItem mainItem = new MergeItem();
          mainItem.SN = mlstMergeItems.Count + 1;
          mainItem.Name = file.Name;
          mainItem.File = file.FullName;
          mainItem.Size = file.Length;
          bool iFound = FindMergeItem(mainItem);
          if (iFound) {
            if (MessageBox.Show(this, "File " + mainItem.Name + " already exists in the list.\nDo you want to continue(Y/N)?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
              mlstMergeItems.Add(mainItem);
          } else mlstMergeItems.Add(mainItem);
        }
      }
      UpdateMsg(0);
    }

    /// <summary> Updates the message for size and files. </summary>
    private void UpdateMsg(int nMissed) {
      long nSize = 0;
      for (int i = 0; i < mlstMergeItems.Count; i++) {
        MergeItem mi = mlstMergeItems[i];
        nSize += mi.Size;
        mi.SN = i + 1;
        mi.Number = mi.SN.ToString() + ".";
      }
      if (nMissed == 0)
        ShowMsg("Total: " + mlstMergeItems.Count.ToString() + " file/s. Size: " +
           Utils<string>.GetFormattedSize(nSize), false);
      else
        ShowMsg("Total: " + mlstMergeItems.Count.ToString() + " file/s. Size: " +
           Utils<string>.GetFormattedSize(nSize) + ". Missed: " + nMissed.ToString(), false);
    }

    void mtimer_Tick(object sender, EventArgs e) {
      mnTickm++;
      SetTime(mnTickm, lbmTime);
    }

    /// <summary> Finds the given item into the list. </summary>
    /// <param name="mItem"> Item to be searched. </param>
    /// <returns> true if found else false. </returns>
    private bool FindMergeItem(MergeItem mItem) {
      string strFile = mItem.Name;
      for (int i = 0; i < mlstMergeItems.Count; i++) {
        string strCurFile = mlstMergeItems[i].File;
        if (strFile.Equals(strCurFile)) return true;
      }
      return false;
    }

    /// <summary> Enables/Disables merge operation. </summary>
    /// <param name="iEnable"> if true enables else disables. </param>
    private void EnableMerge(bool iEnable) {
      try {
        //for (int i = 0; i < mcmMerge.Items.Count; i++) {
        //   if (i == 9 || i == 10 || i == 14) mcmMerge.Items[i].Enabled = !iEnable;
        //   else if (i != 4) mcmMerge.Items[i].Enabled = iEnable;
        //}
        //mtsmPPS.Image = mbtMPP.Image;
        btSMR.IsEnabled = iEnable;
        //if (mcmMerge.Items.Count != 0) {
        //   mcmMerge.Items[2].Enabled = iEnable;
        //   mcmMerge.Items[3].Enabled = iEnable;
        //}
        miMerging = !iEnable;
        btLMF.IsEnabled = iEnable;
        tbMB.IsEnabled = iEnable;
        btUp.IsEnabled = iEnable;
        btDown.IsEnabled = iEnable;
        btAdd.IsEnabled = iEnable;
        btRem.IsEnabled = iEnable;
        mtimer.Start();
        //for (int i = 0; i < 3; i++) lmenu.Items[i].Enabled = iEnable;
        if (iEnable) {
          mtimer.Stop();
          mnTickm = 1;
          pbMerger.Value = 0;
          lbmTime.Content = "";
          tbmVal.Text = "";
          if (mfsMerge != null) mfsMerge.Close();
          if (mfsInput != null) mfsInput.Close();
        }
      } catch (Exception) { }
    }

    /// <summary> Sets merge title </summary>
    private void SetMergeTitle() {
      if (mstrMergeFile.Length != 0) {
        FileInfo file = new FileInfo(mstrMergeFile);
        this.Title = file.Name + " - Merge files";
      } else {
        if (File.Exists(mstrMergeList)) {
          FileInfo fileM = new FileInfo(mstrMergeList);
          if (fileM.Extension.ToLower() == ".mrg") this.Title = fileM.Name + " - Merge files";
        } else this.Title = "Merge files";
      }
      if (this.Title.Length > 60) nfIcon.Text = "Merge files";
      else nfIcon.Text = this.Title;
    }

    /// <summary> Loads merge list file. </summary>
    void LoadMergeList() {
      try {
        if (!File.Exists(mstrArg)) return;
        mstrMergeList = mstrArg;
        mlstMergeItems.Clear();
        StreamReader sr = File.OpenText(mstrArg);
        string strFile = sr.ReadLine();
        if (strFile.ToLower().CompareTo("@mergelist") != 0) {
          ShowMsg("Invalid merge list.", true);
          return;
        }
        strFile = sr.ReadLine();
        int nMissed = 0;
        while (strFile != null) {
          if (!File.Exists(strFile)) {
            strFile = sr.ReadLine();
            nMissed++;
            continue;
          }
          FileInfo file = new FileInfo(strFile);
          if (file.Length == 0) continue;
          MergeItem mItem = new MergeItem();
          mItem.SN = mlstMergeItems.Count + 1;
          mItem.Name = file.Name;
          mItem.File = file.FullName;
          mItem.Size = file.Length;
          bool iFound = FindMergeItem(mItem);
          if (iFound) {
            if (MessageBox.Show(this, "File " + mItem.Name + " already exists in the list.\nDo you want to continue(Y/N)?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
              continue;
          }
          mlstMergeItems.Add(mItem);
          strFile = sr.ReadLine();
        }
        sr.Close();
        UpdateMsg(nMissed);
        SetMergeTitle();
      } catch (Exception ex) {
        ShowMsg(ex.Message, true);
      }
    }

    /// <summary> Load's selected merge file. </summary>
    private void OnLoadList(object sender, RoutedEventArgs e) {
      mofdOpenSplitSrc.Title = "Load merge list file...";
      mofdOpenSplitSrc.Filter = "MergeList files (*.mrg)|*.mrg";
      if (mofdOpenSplitSrc.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
        try {
          mstrArg = mofdOpenSplitSrc.FileName;
          LoadMergeList();
        } catch (Exception) { }
      }
    }

    /// <summary> Save list to a file. </summary>
    private void OnSaveList(object sender, RoutedEventArgs e) {
      if (mergelist.Items.Count == 0) {
        ShowMsg("Nothing to save.", true);
        return;
      }
      msfdSaveMergeRes.Title = "Save merge list to file...";
      msfdSaveMergeRes.Filter = "MergeList files (*.mrg)|*.mrg";
      FileInfo file = new FileInfo(mlstMergeItems[0].File);
      DirectoryInfo dir = file.Directory;
      msfdSaveMergeRes.FileName = dir.Name.ToUpper() + ".MRG";
      if (msfdSaveMergeRes.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
        StreamWriter sw = new StreamWriter(msfdSaveMergeRes.FileName);
        sw.WriteLine("@mergelist");
        for (int i = 0; i < mlstMergeItems.Count; i++) sw.WriteLine(mlstMergeItems[i].Name);
        sw.Close();
      }
    }

    /// <summary> Event handler for save button. Merges the list into single file and saves to disk. </summary>
    private void OnSaveM(object sender, RoutedEventArgs e) {
      try {
        if (mergelist.Items.Count == 0) {
          ShowMsg("Nothing to merge.", true);
          return;
        }
        FileInfo file = new FileInfo(mlstMergeItems[0].File);
        DirectoryInfo dir = file.Directory;
        if (file.Extension.ToLower() == ".part") {
          msfdSaveMergeRes.FileName = file.Name.Replace(file.Extension, "");
          msfdSaveMergeRes.Filter = "";
          int nUnderScore = msfdSaveMergeRes.FileName.LastIndexOf("_");
          if (nUnderScore != -1)
            msfdSaveMergeRes.FileName = msfdSaveMergeRes.FileName.Remove(nUnderScore);
          else msfdSaveMergeRes.FileName = file.Name.Replace(file.Extension, "");
        } else
          msfdSaveMergeRes.FileName = dir.Name.ToUpper() + file.Extension.ToUpper();
        if (mlstMergeItems.Count < 2) {
          ShowMsg("Merge items can't be less than two.", true);
          return;
        }
        if (msfdSaveMergeRes.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
          if (File.Exists(mstrSplitSrc) &&
              miSplitting &&
              mstrSplitSrc.CompareTo(msfdSaveMergeRes.FileName) == 0) {
            ShowMsg("Wait, current file is under split process.", true);
            return;
          }
          mstrMergeFile = msfdSaveMergeRes.FileName;
          FileInfo fileM = new FileInfo(mstrMergeFile);
          if (!fileM.Extension.Contains(".")) {
            FileInfo filePart1 = new FileInfo(mlstMergeItems[0].File);
            mstrMergeFile += filePart1.Extension;
            this.Title = fileM.Name + filePart1.Extension + " - Merge files";
          }
          this.Title = fileM.Name + " - Merge files";
          if (this.Title.Length > 60) nfIcon.Text = "Merge files";
          else nfIcon.Text = this.Title;
          btStopM.Visibility = Visibility.Visible;
          btPPM.Visibility = Visibility.Visible;
          try {
            long nBuf = long.Parse(tbMB.Text);
            if (nBuf <= 0 || nBuf >= 512 * 1024 * 1024) {
              ShowMsg("Invalid buffer size.", true);
              tbMB.Text = "8";
            } else mnBufM = nBuf * 1024 * 1024;
          } catch (Exception) {
            ShowMsg("Invalid buffer size.", true);
            tbMB.Text = "8";
          }
          this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new WriteResultantFile(WriteMergedFile));
        }
      } catch (Exception ex) { ShowMsg(ex.Message, true); }
    }

    /// <summary> Worker thread function to merge the files. </summary>
    private void WriteMergedFile() {
      try {
        ShowMsg("Merging...", false);
        pbMerger.Value = 0;
        tbmVal.Text = "";
        lbmTime.Content = "";
        EnableMerge(false);
        miStopm = false;
        long nTotalSize = 0;
        for (int i = 0; i < mlstMergeItems.Count; i++) nTotalSize += mlstMergeItems[i].Size;
        double nFactor = ((double)nTotalSize) / 100;
        if (nFactor == 0) {
          nFactor = 1;
          pbMerger.Maximum = (double)nTotalSize;
        } else pbMerger.Maximum = 100;
        if (File.Exists(mstrMergeFile)) File.Delete(mstrMergeFile);
        mfsMerge = File.OpenWrite(mstrMergeFile);
        long nBytesWritten = 0;
        mergelist.SelectedItems.Clear();
        for (int i = 0; i < mlstMergeItems.Count; i++) {
          string strInputFile = mlstMergeItems[i].File;
          mfsInput = File.OpenRead(strInputFile);
          if (!mfsInput.CanRead) {
            FileInfo fileNR = new FileInfo(strInputFile);
            ShowMsg("Can't read file " + fileNR.Name, true);
            continue;
          }
          mergelist.SelectedItems.Add(mlstMergeItems[i]);
          mergelist.Items.Refresh();
          byte[] bytes = new byte[mnBufM];
          int nBytes = mfsInput.Read(bytes, 0, (int)mnBufM);
          nBytesWritten += nBytes;
          while (nBytes != 0) {
            if (miAppStop == true || miStopm == true) {
              OnClickMStop(null, null);
              return;
            }
            if (miPPM == true) {
              Thread.Sleep(100);
              System.Windows.Forms.Application.DoEvents();
              continue;
            }
            mfsMerge.Write(bytes, 0, nBytes);
            nBytes = mfsInput.Read(bytes, 0, (int)mnBufM);
            nBytesWritten += nBytes;
            int nPos = (int)(nBytesWritten / nFactor);
            bool iShow = (nPos > pbMerger.Value) ? true : false;
            pbMerger.Value = nPos;
            System.Windows.Forms.Application.DoEvents();
            if (iShow) {
              ShowMsg("Merging: " + mlstMergeItems[i].Name + "...", false);
              tbmVal.Text = nPos.ToString() + "%";
              nfIcon.Text = tbmVal.Text + " Completed.";
            }
          }
          nBytes = 0;
          mfsInput.Flush();
          mfsMerge.Flush();
          mfsInput.Close();
        }
        pbMerger.Value = pbMerger.Maximum;
        tbmVal.Text = "100%";
        nfIcon.Text = this.Title;
        mfsMerge.Close();
        EnableMerge(true);
        btPPM.Visibility = Visibility.Hidden;
        btStopM.Visibility = Visibility.Hidden;
        if (cbDelPart.IsChecked == true) {
          for (int i = 0; i < mlstMergeItems.Count; i++) {
            MergeItem mi = mlstMergeItems[i];
            File.Delete(mi.File);
          }
          mlstMergeItems.Clear();
          UpdateMsg(0);
        }
        if (cbDelPart.IsChecked == true) GenerateMergeReport();
        ShowMsg("Merging Completed.", false);
      } catch (Exception ex) {
        if (miMerging) ShowMsg(ex.Message, true);
        EnableMerge(true);
        btPPM.Visibility = Visibility.Hidden;
        btStopM.Visibility = Visibility.Hidden;
      }
    }

    /// <summary> Generates merge report </summary>
    private void GenerateMergeReport() {
      ShowMsg("Writing Merge Report...", false);
      FileInfo fileMrg = new FileInfo(mstrMergeFile);
      string strReportFile = fileMrg.DirectoryName + "\\MergeReport.htm";
      if (File.Exists(strReportFile)) File.Delete(strReportFile);
      string strImageFile = fileMrg.DirectoryName + "\\Image.bmp";
      StreamWriter srRep = new StreamWriter(strReportFile);
      WriteReportHeader("Merge Report of", mstrMergeFile, srRep, "bgcolor=\"#000099\"");
      string strBG = "";
      for (int i = 0; i < mlstMergeItems.Count; i++) {
        MergeItem mi = mlstMergeItems[i];
        if (i % 2 != 0) strBG = "bgcolor=\"#EAEAEA\"";
        else strBG = "bgcolor=\"#FFFFFF\"";
        srRep.WriteLine("<tr>");
        srRep.WriteLine("<td width=\"45\" height=\"11\" {0}><b>", strBG);
        srRep.WriteLine("<font color=\"#000099\" face=\"Verdana\" size=\"2\">{0}</font></b></td>", i + 1);
        srRep.WriteLine("<td width=\"865\" height=\"11\" {0}><b>", strBG);
        srRep.WriteLine("<font face=\"Verdana\" size=\"2\" color=\"#000099\">{0}</font></b></td>", mi.Name);
        srRep.WriteLine("<td width=\"150\" height=\"11\" {0}><b>", strBG);
        srRep.WriteLine("<font color=\"#000099\" face=\"Verdana\" size=\"2\">{0} Bytes</font></b></td>", mi.Size);
        srRep.WriteLine("</tr>");
      }
      WriteReportFooter(mlstMergeItems.Count, srRep);
      srRep.Close();
    }

    /// <summary> Writes report header. </summary>
    /// <param name="strTitle"> Title of the document </param>
    /// <param name="strFile"> Merge/Split file name </param>
    /// <param name="stRep"> Report stream </param>
    private void WriteReportHeader(string strTitle, string strFile, StreamWriter stRep, string bgColor) {
      FileInfo file = new FileInfo(strFile);
      string strSize = Utils<string>.GetFormattedSize(file.Length);
      stRep.WriteLine("<html>");
      stRep.WriteLine("<head>");
      stRep.WriteLine("<title>{0} - {1}</title>", strTitle, strFile);
      stRep.WriteLine("</head>");
      stRep.WriteLine("<body>");
      stRep.WriteLine("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" width=\"1056\" height=\"1\">");
      stRep.WriteLine("<tr>");
      stRep.WriteLine("<td width=\"1050\" height=\"19\" bgcolor=\"#FFFFFF\" align=\"center\" colspan=\"4\"><font face=\"Verdana\" size=\"2\"><b><font color=\"#FF0000\">");
      stRep.WriteLine("Contents Of:</font> {0}</b></font></td>", strFile);
      stRep.WriteLine("</tr>");
      stRep.WriteLine("<tr>");
      stRep.WriteLine("<td width=\"1050\" height=\"8\" bgcolor=\"#FFFFFF\" align=\"center\" colspan=\"4\">");
      stRep.WriteLine("<hr noshade color=\"#000000\" align=\"left\" size=\"1\"></td>");
      stRep.WriteLine("</tr>");
      stRep.WriteLine("<tr>");
      stRep.WriteLine("<td width=\"1050\" height=\"9\" bgcolor=\"#FFFFFF\" align=\"center\" colspan=\"4\">");
      stRep.WriteLine("<p align=\"left\"><b><font face=\"Verdana\" size=\"2\"><font color=\"#FF0000\">");
      stRep.WriteLine("Total Size :</font> {0} Bytes [{1}]</font></b></td>", file.Length, strSize);
      stRep.WriteLine("</tr>");
      stRep.WriteLine("<tr>");
      stRep.WriteLine("<td width=\"1050\" height=\"7\" bgcolor=\"#FFFFFF\" align=\"center\" colspan=\"4\">&nbsp;</td>");
      stRep.WriteLine("</tr>");
      stRep.WriteLine("<tr>");
      stRep.WriteLine("<td width=\"45\" height=\"10\" {0} align=\"left\"><b>", bgColor);
      stRep.WriteLine("<font color=\"#FFFFFF\" face=\"Verdana\" size=\"2\">#</font></b></td>");
      stRep.WriteLine("<td width=\"865\" height=\"10\" {0} align=\"left\"><b>", bgColor);
      stRep.WriteLine("<font color=\"#FFFFFF\" face=\"Verdana\" size=\"2\">Filename</font></b></td>");
      stRep.WriteLine("<td width=\"150\" height=\"10\" {0} align=\"left\"><b>", bgColor);
      stRep.WriteLine("<font color=\"#FFFFFF\" face=\"Verdana\" size=\"2\">Filesize</font></b></td>");
      stRep.WriteLine("</tr>");
    }

    /// <summary> Writes footer to the report file. </summary>
    /// <param name="nItems"> Number of items </param>
    /// <param name="strAppImage"> Image file </param>
    /// <param name="stRep"> Report Stream </param>
    private void WriteReportFooter(int nItems, StreamWriter stRep) {
      stRep.WriteLine("<tr>");
      stRep.WriteLine("<td width=\"1050\" height=\"5\" colspan=\"4\">");
      stRep.WriteLine("<p align=\"right\"><b><font face=\"Verdana\" size=\"2\"><font color=\"#FF0000\">");
      stRep.WriteLine("<br>");
      stRep.WriteLine("Total Files:</font> {0}</font></b></td>", nItems);
      stRep.WriteLine("</tr>");
      stRep.WriteLine("</table>");
      stRep.WriteLine("<p align=\"center\"><font face=\"Verdana\" size=\"2\" color=\"#808080\">");
      stRep.WriteLine("Report Created By: <a href=\"mailto:kumar.anirudha@gmail.com\">File Merger/Splitter</a><br>");
      stRep.WriteLine("Copyright (C) Amraj 2005-2008</font></p>");
      stRep.WriteLine("</body>");
      stRep.WriteLine("</html>");
    }

    void mlstMergeItems_CollectionChanged(Object sender, NotifyCollectionChangedEventArgs e) {
      mergelist.Items.Refresh();
    }

    public ObservableCollection<MergeItem> MergeItems { get { return mlstMergeItems; } }

    /// <summary> Merge Item. Contains serial number, file name and file size. </summary>
    public class MergeItem {
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

      public string File {
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
        get {
          img = fic.GetImage(mstrFile, 16);
          return img;
        }
        set { img = value; }
      }

      public bool IsSelected {
        get { return Selected; }
        set { Selected = value; }
      }

      private int mnSn = 0;
      private string mstrName = "";
      private string mstrFile = "";
      private long mnSize = 0;
      private ImageSource img = null;
      private bool Selected = false;
      private string mstrNumber = "";
    }

    ObservableCollection<MergeItem> mlstMergeItems = new ObservableCollection<MergeItem>();
    private string mstrMergeFile = "", mstrMergeList = "";
    private bool miPPM = false, miMerging = false;
    FileStream mfsInput = null, mfsMerge = null;
    System.Windows.Forms.NotifyIcon nfIcon;
    long mnTickm = 1;
    long mnBufM = 8388608;
  }
}