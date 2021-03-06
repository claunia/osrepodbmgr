
// This file has been generated by the GUI designer. Do not modify.
namespace osrepodbmgr
{
	public partial class frmMain
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.Notebook notebook1;

		private global::Gtk.VBox vbox3;

		private global::Gtk.ScrolledWindow GtkScrolledWindow;

		private global::Gtk.TreeView treeOSes;

		private global::Gtk.Label lblOSStatus;

		private global::Gtk.HBox hbox2;

		private global::Gtk.Label lblProgress;

		private global::Gtk.ProgressBar prgProgress;

		private global::Gtk.HBox hbox3;

		private global::Gtk.Label lblProgress2;

		private global::Gtk.ProgressBar prgProgress2;

		private global::Gtk.HBox hbox1;

		private global::Gtk.Button btnAdd;

		private global::Gtk.Button btnRemove;

		private global::Gtk.Button btnQuit;

		private global::Gtk.Button btnSettings;

		private global::Gtk.Button btnHelp;

		private global::Gtk.Button btnSave;

		private global::Gtk.Button btnCompress;

		private global::Gtk.Button btnStop;

		private global::Gtk.Label label1;

		private global::Gtk.VBox vbox4;

		private global::Gtk.ScrolledWindow GtkScrolledWindow1;

		private global::Gtk.TreeView treeFiles;

		private global::Gtk.Label lblFileStatus;

		private global::Gtk.HBox hbox4;

		private global::Gtk.Label lblProgressFiles1;

		private global::Gtk.ProgressBar prgProgressFiles1;

		private global::Gtk.HBox hbox5;

		private global::Gtk.Label lblProgressFiles2;

		private global::Gtk.ProgressBar prgProgressFiles2;

		private global::Gtk.HBox hbox6;

		private global::Gtk.Button btnPopulateFiles;

		private global::Gtk.Button btnCheckInVirusTotal;

		private global::Gtk.Button btnScanWithClamd;

		private global::Gtk.Button btnScanAllPending;

		private global::Gtk.Button btnToggleCrack;

		private global::Gtk.Button btnCleanFiles;

		private global::Gtk.Button btnStopFiles;

		private global::Gtk.Label label3;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget osrepodbmgr.frmMain
			this.Name = "osrepodbmgr.frmMain";
			this.Title = global::Mono.Unix.Catalog.GetString("OS Repository DB Manager");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			// Container child osrepodbmgr.frmMain.Gtk.Container+ContainerChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			// Container child vbox2.Gtk.Box+BoxChild
			this.notebook1 = new global::Gtk.Notebook();
			this.notebook1.CanFocus = true;
			this.notebook1.Name = "notebook1";
			this.notebook1.CurrentPage = 1;
			// Container child notebook1.Gtk.Notebook+NotebookChild
			this.vbox3 = new global::Gtk.VBox();
			this.vbox3.Name = "vbox3";
			this.vbox3.Spacing = 6;
			// Container child vbox3.Gtk.Box+BoxChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			this.GtkScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			this.treeOSes = new global::Gtk.TreeView();
			this.treeOSes.Sensitive = false;
			this.treeOSes.CanFocus = true;
			this.treeOSes.Name = "treeOSes";
			this.GtkScrolledWindow.Add(this.treeOSes);
			this.vbox3.Add(this.GtkScrolledWindow);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.GtkScrolledWindow]));
			w2.Position = 0;
			// Container child vbox3.Gtk.Box+BoxChild
			this.lblOSStatus = new global::Gtk.Label();
			this.lblOSStatus.Name = "lblOSStatus";
			this.lblOSStatus.LabelProp = global::Mono.Unix.Catalog.GetString("label1");
			this.vbox3.Add(this.lblOSStatus);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.lblOSStatus]));
			w3.Position = 1;
			w3.Expand = false;
			w3.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.hbox2 = new global::Gtk.HBox();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 6;
			// Container child hbox2.Gtk.Box+BoxChild
			this.lblProgress = new global::Gtk.Label();
			this.lblProgress.Name = "lblProgress";
			this.lblProgress.LabelProp = global::Mono.Unix.Catalog.GetString("label1");
			this.hbox2.Add(this.lblProgress);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.lblProgress]));
			w4.Position = 0;
			w4.Expand = false;
			w4.Fill = false;
			// Container child hbox2.Gtk.Box+BoxChild
			this.prgProgress = new global::Gtk.ProgressBar();
			this.prgProgress.Name = "prgProgress";
			this.hbox2.Add(this.prgProgress);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.prgProgress]));
			w5.Position = 1;
			this.vbox3.Add(this.hbox2);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.hbox2]));
			w6.Position = 2;
			w6.Expand = false;
			w6.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.hbox3 = new global::Gtk.HBox();
			this.hbox3.Name = "hbox3";
			this.hbox3.Spacing = 6;
			// Container child hbox3.Gtk.Box+BoxChild
			this.lblProgress2 = new global::Gtk.Label();
			this.lblProgress2.Name = "lblProgress2";
			this.lblProgress2.LabelProp = global::Mono.Unix.Catalog.GetString("label2");
			this.hbox3.Add(this.lblProgress2);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.hbox3[this.lblProgress2]));
			w7.Position = 0;
			w7.Expand = false;
			w7.Fill = false;
			// Container child hbox3.Gtk.Box+BoxChild
			this.prgProgress2 = new global::Gtk.ProgressBar();
			this.prgProgress2.Name = "prgProgress2";
			this.hbox3.Add(this.prgProgress2);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.hbox3[this.prgProgress2]));
			w8.Position = 1;
			this.vbox3.Add(this.hbox3);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.hbox3]));
			w9.Position = 3;
			w9.Expand = false;
			w9.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.btnAdd = new global::Gtk.Button();
			this.btnAdd.CanFocus = true;
			this.btnAdd.Name = "btnAdd";
			this.btnAdd.UseStock = true;
			this.btnAdd.UseUnderline = true;
			this.btnAdd.Label = "gtk-add";
			this.hbox1.Add(this.btnAdd);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.btnAdd]));
			w10.Position = 0;
			w10.Expand = false;
			w10.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.btnRemove = new global::Gtk.Button();
			this.btnRemove.CanFocus = true;
			this.btnRemove.Name = "btnRemove";
			this.btnRemove.UseStock = true;
			this.btnRemove.UseUnderline = true;
			this.btnRemove.Label = "gtk-remove";
			this.hbox1.Add(this.btnRemove);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.btnRemove]));
			w11.Position = 1;
			w11.Expand = false;
			w11.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.btnQuit = new global::Gtk.Button();
			this.btnQuit.CanFocus = true;
			this.btnQuit.Name = "btnQuit";
			this.btnQuit.UseStock = true;
			this.btnQuit.UseUnderline = true;
			this.btnQuit.Label = "gtk-quit";
			this.hbox1.Add(this.btnQuit);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.btnQuit]));
			w12.PackType = ((global::Gtk.PackType)(1));
			w12.Position = 2;
			w12.Expand = false;
			w12.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.btnSettings = new global::Gtk.Button();
			this.btnSettings.CanFocus = true;
			this.btnSettings.Name = "btnSettings";
			this.btnSettings.UseUnderline = true;
			this.btnSettings.Label = global::Mono.Unix.Catalog.GetString("_Settings");
			global::Gtk.Image w13 = new global::Gtk.Image();
			w13.Pixbuf = global::Stetic.IconLoader.LoadIcon(this, "gtk-preferences", global::Gtk.IconSize.Menu);
			this.btnSettings.Image = w13;
			this.hbox1.Add(this.btnSettings);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.btnSettings]));
			w14.PackType = ((global::Gtk.PackType)(1));
			w14.Position = 3;
			w14.Expand = false;
			w14.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.btnHelp = new global::Gtk.Button();
			this.btnHelp.CanFocus = true;
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.UseStock = true;
			this.btnHelp.UseUnderline = true;
			this.btnHelp.Label = "gtk-help";
			this.hbox1.Add(this.btnHelp);
			global::Gtk.Box.BoxChild w15 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.btnHelp]));
			w15.PackType = ((global::Gtk.PackType)(1));
			w15.Position = 4;
			w15.Expand = false;
			w15.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.btnSave = new global::Gtk.Button();
			this.btnSave.CanFocus = true;
			this.btnSave.Name = "btnSave";
			this.btnSave.UseUnderline = true;
			this.btnSave.Label = global::Mono.Unix.Catalog.GetString("Save _As");
			global::Gtk.Image w16 = new global::Gtk.Image();
			w16.Pixbuf = global::Stetic.IconLoader.LoadIcon(this, "gtk-directory", global::Gtk.IconSize.Menu);
			this.btnSave.Image = w16;
			this.hbox1.Add(this.btnSave);
			global::Gtk.Box.BoxChild w17 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.btnSave]));
			w17.PackType = ((global::Gtk.PackType)(1));
			w17.Position = 5;
			w17.Expand = false;
			w17.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.btnCompress = new global::Gtk.Button();
			this.btnCompress.CanFocus = true;
			this.btnCompress.Name = "btnCompress";
			this.btnCompress.UseUnderline = true;
			this.btnCompress.Label = global::Mono.Unix.Catalog.GetString("Compress to");
			global::Gtk.Image w18 = new global::Gtk.Image();
			w18.Pixbuf = global::Stetic.IconLoader.LoadIcon(this, "gtk-save", global::Gtk.IconSize.Menu);
			this.btnCompress.Image = w18;
			this.hbox1.Add(this.btnCompress);
			global::Gtk.Box.BoxChild w19 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.btnCompress]));
			w19.PackType = ((global::Gtk.PackType)(1));
			w19.Position = 6;
			w19.Expand = false;
			w19.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.btnStop = new global::Gtk.Button();
			this.btnStop.CanFocus = true;
			this.btnStop.Name = "btnStop";
			this.btnStop.UseStock = true;
			this.btnStop.UseUnderline = true;
			this.btnStop.Label = "gtk-stop";
			this.hbox1.Add(this.btnStop);
			global::Gtk.Box.BoxChild w20 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.btnStop]));
			w20.PackType = ((global::Gtk.PackType)(1));
			w20.Position = 7;
			w20.Expand = false;
			w20.Fill = false;
			this.vbox3.Add(this.hbox1);
			global::Gtk.Box.BoxChild w21 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.hbox1]));
			w21.Position = 4;
			w21.Expand = false;
			w21.Fill = false;
			this.notebook1.Add(this.vbox3);
			// Notebook tab
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("Operating systems");
			this.notebook1.SetTabLabel(this.vbox3, this.label1);
			this.label1.ShowAll();
			// Container child notebook1.Gtk.Notebook+NotebookChild
			this.vbox4 = new global::Gtk.VBox();
			this.vbox4.Name = "vbox4";
			this.vbox4.Spacing = 6;
			// Container child vbox4.Gtk.Box+BoxChild
			this.GtkScrolledWindow1 = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow1.Name = "GtkScrolledWindow1";
			this.GtkScrolledWindow1.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow1.Gtk.Container+ContainerChild
			this.treeFiles = new global::Gtk.TreeView();
			this.treeFiles.Sensitive = false;
			this.treeFiles.CanFocus = true;
			this.treeFiles.Name = "treeFiles";
			this.GtkScrolledWindow1.Add(this.treeFiles);
			this.vbox4.Add(this.GtkScrolledWindow1);
			global::Gtk.Box.BoxChild w24 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.GtkScrolledWindow1]));
			w24.Position = 0;
			// Container child vbox4.Gtk.Box+BoxChild
			this.lblFileStatus = new global::Gtk.Label();
			this.lblFileStatus.Name = "lblFileStatus";
			this.lblFileStatus.LabelProp = global::Mono.Unix.Catalog.GetString("label2");
			this.vbox4.Add(this.lblFileStatus);
			global::Gtk.Box.BoxChild w25 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.lblFileStatus]));
			w25.Position = 1;
			w25.Expand = false;
			w25.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.hbox4 = new global::Gtk.HBox();
			this.hbox4.Name = "hbox4";
			this.hbox4.Spacing = 6;
			// Container child hbox4.Gtk.Box+BoxChild
			this.lblProgressFiles1 = new global::Gtk.Label();
			this.lblProgressFiles1.Name = "lblProgressFiles1";
			this.lblProgressFiles1.LabelProp = global::Mono.Unix.Catalog.GetString("label4");
			this.hbox4.Add(this.lblProgressFiles1);
			global::Gtk.Box.BoxChild w26 = ((global::Gtk.Box.BoxChild)(this.hbox4[this.lblProgressFiles1]));
			w26.Position = 0;
			w26.Expand = false;
			w26.Fill = false;
			// Container child hbox4.Gtk.Box+BoxChild
			this.prgProgressFiles1 = new global::Gtk.ProgressBar();
			this.prgProgressFiles1.Name = "prgProgressFiles1";
			this.hbox4.Add(this.prgProgressFiles1);
			global::Gtk.Box.BoxChild w27 = ((global::Gtk.Box.BoxChild)(this.hbox4[this.prgProgressFiles1]));
			w27.Position = 1;
			this.vbox4.Add(this.hbox4);
			global::Gtk.Box.BoxChild w28 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.hbox4]));
			w28.Position = 2;
			w28.Expand = false;
			w28.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.hbox5 = new global::Gtk.HBox();
			this.hbox5.Name = "hbox5";
			this.hbox5.Spacing = 6;
			// Container child hbox5.Gtk.Box+BoxChild
			this.lblProgressFiles2 = new global::Gtk.Label();
			this.lblProgressFiles2.Name = "lblProgressFiles2";
			this.lblProgressFiles2.LabelProp = global::Mono.Unix.Catalog.GetString("label6");
			this.hbox5.Add(this.lblProgressFiles2);
			global::Gtk.Box.BoxChild w29 = ((global::Gtk.Box.BoxChild)(this.hbox5[this.lblProgressFiles2]));
			w29.Position = 0;
			w29.Expand = false;
			w29.Fill = false;
			// Container child hbox5.Gtk.Box+BoxChild
			this.prgProgressFiles2 = new global::Gtk.ProgressBar();
			this.prgProgressFiles2.Name = "prgProgressFiles2";
			this.hbox5.Add(this.prgProgressFiles2);
			global::Gtk.Box.BoxChild w30 = ((global::Gtk.Box.BoxChild)(this.hbox5[this.prgProgressFiles2]));
			w30.Position = 1;
			this.vbox4.Add(this.hbox5);
			global::Gtk.Box.BoxChild w31 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.hbox5]));
			w31.Position = 3;
			w31.Expand = false;
			w31.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.hbox6 = new global::Gtk.HBox();
			this.hbox6.Name = "hbox6";
			this.hbox6.Spacing = 6;
			// Container child hbox6.Gtk.Box+BoxChild
			this.btnPopulateFiles = new global::Gtk.Button();
			this.btnPopulateFiles.CanFocus = true;
			this.btnPopulateFiles.Name = "btnPopulateFiles";
			this.btnPopulateFiles.UseUnderline = true;
			this.btnPopulateFiles.Label = global::Mono.Unix.Catalog.GetString("Populate");
			this.hbox6.Add(this.btnPopulateFiles);
			global::Gtk.Box.BoxChild w32 = ((global::Gtk.Box.BoxChild)(this.hbox6[this.btnPopulateFiles]));
			w32.PackType = ((global::Gtk.PackType)(1));
			w32.Position = 0;
			w32.Expand = false;
			w32.Fill = false;
			// Container child hbox6.Gtk.Box+BoxChild
			this.btnCheckInVirusTotal = new global::Gtk.Button();
			this.btnCheckInVirusTotal.CanFocus = true;
			this.btnCheckInVirusTotal.Name = "btnCheckInVirusTotal";
			this.btnCheckInVirusTotal.UseUnderline = true;
			this.btnCheckInVirusTotal.Label = global::Mono.Unix.Catalog.GetString("Check with VirusTotal");
			this.hbox6.Add(this.btnCheckInVirusTotal);
			global::Gtk.Box.BoxChild w33 = ((global::Gtk.Box.BoxChild)(this.hbox6[this.btnCheckInVirusTotal]));
			w33.PackType = ((global::Gtk.PackType)(1));
			w33.Position = 1;
			w33.Expand = false;
			w33.Fill = false;
			// Container child hbox6.Gtk.Box+BoxChild
			this.btnScanWithClamd = new global::Gtk.Button();
			this.btnScanWithClamd.CanFocus = true;
			this.btnScanWithClamd.Name = "btnScanWithClamd";
			this.btnScanWithClamd.UseUnderline = true;
			this.btnScanWithClamd.Label = global::Mono.Unix.Catalog.GetString("Scan with clamd");
			this.hbox6.Add(this.btnScanWithClamd);
			global::Gtk.Box.BoxChild w34 = ((global::Gtk.Box.BoxChild)(this.hbox6[this.btnScanWithClamd]));
			w34.PackType = ((global::Gtk.PackType)(1));
			w34.Position = 2;
			w34.Expand = false;
			w34.Fill = false;
			// Container child hbox6.Gtk.Box+BoxChild
			this.btnScanAllPending = new global::Gtk.Button();
			this.btnScanAllPending.CanFocus = true;
			this.btnScanAllPending.Name = "btnScanAllPending";
			this.btnScanAllPending.UseUnderline = true;
			this.btnScanAllPending.Label = global::Mono.Unix.Catalog.GetString("Scan all with clamd");
			this.hbox6.Add(this.btnScanAllPending);
			global::Gtk.Box.BoxChild w35 = ((global::Gtk.Box.BoxChild)(this.hbox6[this.btnScanAllPending]));
			w35.PackType = ((global::Gtk.PackType)(1));
			w35.Position = 3;
			w35.Expand = false;
			w35.Fill = false;
			// Container child hbox6.Gtk.Box+BoxChild
			this.btnToggleCrack = new global::Gtk.Button();
			this.btnToggleCrack.CanFocus = true;
			this.btnToggleCrack.Name = "btnToggleCrack";
			this.btnToggleCrack.UseUnderline = true;
			this.btnToggleCrack.Label = global::Mono.Unix.Catalog.GetString("Mark as crack");
			this.hbox6.Add(this.btnToggleCrack);
			global::Gtk.Box.BoxChild w36 = ((global::Gtk.Box.BoxChild)(this.hbox6[this.btnToggleCrack]));
			w36.PackType = ((global::Gtk.PackType)(1));
			w36.Position = 4;
			w36.Expand = false;
			w36.Fill = false;
			// Container child hbox6.Gtk.Box+BoxChild
			this.btnCleanFiles = new global::Gtk.Button();
			this.btnCleanFiles.CanFocus = true;
			this.btnCleanFiles.Name = "btnCleanFiles";
			this.btnCleanFiles.UseUnderline = true;
			this.btnCleanFiles.Label = global::Mono.Unix.Catalog.GetString("Clean files");
			this.hbox6.Add(this.btnCleanFiles);
			global::Gtk.Box.BoxChild w37 = ((global::Gtk.Box.BoxChild)(this.hbox6[this.btnCleanFiles]));
			w37.PackType = ((global::Gtk.PackType)(1));
			w37.Position = 5;
			w37.Expand = false;
			w37.Fill = false;
			// Container child hbox6.Gtk.Box+BoxChild
			this.btnStopFiles = new global::Gtk.Button();
			this.btnStopFiles.CanFocus = true;
			this.btnStopFiles.Name = "btnStopFiles";
			this.btnStopFiles.UseStock = true;
			this.btnStopFiles.UseUnderline = true;
			this.btnStopFiles.Label = "gtk-stop";
			this.hbox6.Add(this.btnStopFiles);
			global::Gtk.Box.BoxChild w38 = ((global::Gtk.Box.BoxChild)(this.hbox6[this.btnStopFiles]));
			w38.PackType = ((global::Gtk.PackType)(1));
			w38.Position = 6;
			w38.Expand = false;
			w38.Fill = false;
			this.vbox4.Add(this.hbox6);
			global::Gtk.Box.BoxChild w39 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.hbox6]));
			w39.Position = 4;
			w39.Expand = false;
			w39.Fill = false;
			this.notebook1.Add(this.vbox4);
			global::Gtk.Notebook.NotebookChild w40 = ((global::Gtk.Notebook.NotebookChild)(this.notebook1[this.vbox4]));
			w40.Position = 1;
			// Notebook tab
			this.label3 = new global::Gtk.Label();
			this.label3.Name = "label3";
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString("Files");
			this.notebook1.SetTabLabel(this.vbox4, this.label3);
			this.label3.ShowAll();
			this.vbox2.Add(this.notebook1);
			global::Gtk.Box.BoxChild w41 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.notebook1]));
			w41.Position = 0;
			this.Add(this.vbox2);
			if((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 686;
			this.DefaultHeight = 365;
			this.lblOSStatus.Hide();
			this.lblProgress2.Hide();
			this.prgProgress2.Hide();
			this.btnAdd.Hide();
			this.btnRemove.Hide();
			this.btnSettings.Hide();
			this.btnHelp.Hide();
			this.btnSave.Hide();
			this.btnStop.Hide();
			this.lblFileStatus.Hide();
			this.lblProgressFiles1.Hide();
			this.prgProgressFiles1.Hide();
			this.lblProgressFiles2.Hide();
			this.prgProgressFiles2.Hide();
			this.btnCheckInVirusTotal.Hide();
			this.btnScanWithClamd.Hide();
			this.btnScanAllPending.Hide();
			this.btnToggleCrack.Hide();
			this.btnCleanFiles.Hide();
			this.btnStopFiles.Hide();
			this.Show();
			this.DeleteEvent += new global::Gtk.DeleteEventHandler(this.OnDeleteEvent);
			this.btnAdd.Clicked += new global::System.EventHandler(this.OnBtnAddClicked);
			this.btnRemove.Clicked += new global::System.EventHandler(this.OnBtnRemoveClicked);
			this.btnStop.Clicked += new global::System.EventHandler(this.OnBtnStopClicked);
			this.btnCompress.Clicked += new global::System.EventHandler(this.OnBtnCompressClicked);
			this.btnSave.Clicked += new global::System.EventHandler(this.OnBtnSaveClicked);
			this.btnHelp.Clicked += new global::System.EventHandler(this.OnBtnHelpClicked);
			this.btnSettings.Clicked += new global::System.EventHandler(this.OnBtnSettingsClicked);
			this.btnQuit.Clicked += new global::System.EventHandler(this.OnBtnQuitClicked);
			this.btnStopFiles.Clicked += new global::System.EventHandler(this.OnBtnStopFilesClicked);
			this.btnCleanFiles.Clicked += new global::System.EventHandler(this.OnBtnCleanFilesClicked);
			this.btnToggleCrack.Clicked += new global::System.EventHandler(this.OnBtnToggleCrackClicked);
			this.btnScanAllPending.Clicked += new global::System.EventHandler(this.OnBtnScanAllPendingClicked);
			this.btnScanWithClamd.Clicked += new global::System.EventHandler(this.OnBtnScanWithClamdClicked);
			this.btnCheckInVirusTotal.Clicked += new global::System.EventHandler(this.OnBtnCheckInVirusTotalClicked);
			this.btnPopulateFiles.Clicked += new global::System.EventHandler(this.OnBtnPopulateFilesClicked);
		}
	}
}
