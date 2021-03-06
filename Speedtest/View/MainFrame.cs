﻿using DevExpress.XtraBars.Ribbon;
using LiveCharts.Geared;
using Speedtest.Controller;
using Speedtest.Controller.TabControllers;
using Speedtest.View.MeasureWindow;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Speedtest
{
    public partial class MainFrame : RibbonForm
    {
        #region fields
        public DefaultChartUserControl defaultChart;
        public GearedValues<DefaultChartUserControl> defaultCharts;
        public PortController portController;
        public MainMeasureWindow mmw;
        public bool connectedState { get; set; }
        public bool isRunning { get; set; }
        public double deltaTime;
        public double[] printingData;
        DataCollector dc;
        public static int numberOfPanels = 1;
        private List<UserControl> activePanels;
        private List<string> mmwFocusedPages;
        CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
        public static int intervals = 10;
        #endregion

        public MainFrame()
        {
            InitializeComponent();
            HomeTabController.FillEditors(this);
            PortOptionsTabController.FillEditors(this);
            RecordingTabController.FillEditors(this);
            ImportTabController.FillEditors(this);
            HomeTabController.SetInitialState(this);
            MeasureTabController.FillEditors(this);
            portController = new PortController(this);
            defaultCharts = new GearedValues<DefaultChartUserControl>();
            activePanels = new List<UserControl>();
            availableFileFilters = getFileFilters();
            mmwFocusedPages = getMMWPages();
            timer = new Stopwatch();


            savingFileDestinationPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            changeFileDestinationCaption(savingFileDestinationPath);



        }



        public void testConnect()
        {
            if (!String.IsNullOrWhiteSpace(selectedPortElementValue))
            {
                try
                {
                    HomeTabController.SetGroupsAndIconsToCurrentState(this);

                    if (connectedState)
                    {
                        homePortBasicGroup.Enabled = false;
                        keepRecordsElement.Enabled = false;

                        mmw = new MainMeasureWindow(this);
                        bringContentToFront(mmw);
                    }
                    else
                    {
                        contentPanel.Controls.Clear();
                        activePanels.Remove(mmw);
                        mmw.deleteControls();

                    }
                }
                catch (Exception e)
                {
                    //MessageBox.Show(e.Message);
                }
            }

        }


        private void printTo(string currentlyArrived)
        {
            try
            {
                if (isRunning)
                {
                    Debug.WriteLine(currentlyArrived);

                    try
                    {
                        printingData = Array.ConvertAll(currentlyArrived.Split(' '), Double.Parse);
                    }
                    catch (FormatException)
                    {
                        startStopButton.PerformClick();
                        MessageBox.Show(Strings.Global_Error_WrongFormat);
                    }

                    if (useLinearity)
                    {
                        printingData = calculateLinearValue(printingData, sensitivity, zeroValue);
                    }
                    if (Recording)
                    {
                        csvBuffer.AppendLine(String.Join(",", printingData.Select(p => p.ToString("F", culture))));
                    }

                    if (DisplayModeElementValue == Strings.MeasureTab_DisplayMode_MultiPanel)
                    {
                        ChartController.printGearedChart(this, numberOfPanels, printingData);
                    }
                    else if (DisplayModeElementValue == Strings.MeasureTab_DisplayMode_Monitor)
                    {
                        ChartController.printChartMonitor(mmw.chartMonitor, printingData);
                    }
                    else if (DisplayModeElementValue == Strings.MeasureTab_DisplayMode_SinglePanel)
                    {
                        ChartController.printDefaultChart(this, mmw.xyChartUserControl, printingData);
                    }
                }


            }
            catch (Exception e)
            {

                //MessageBox.Show("printto" + e.Message);
            }


        }

        private void bringContentToFront(UserControl currentControl, bool alreadyExisting = false)
        {
            if (!alreadyExisting)
            {
                activePanels.Add(currentControl);
            }

            contentPanel.Controls.Clear();

            foreach (var panel in activePanels)
            {
                panel.Dock = DockStyle.None;
                panel.Visible = false;
            }
            currentControl.Dock = DockStyle.Fill;
            currentControl.Visible = true;
            contentPanel.Controls.Add(currentControl);
        }

        private void ribbonControl_SelectedPageChanged(object sender, EventArgs e)
        {
            var selectedPage = ribbonControl.SelectedPage.ToString();

            if (selectedPage == Strings.Global_Measure)
            {
                if (mmw != null && activePanels.Contains(mmw))
                {
                    bringContentToFront(mmw, alreadyExisting);
                    BringSingleChartToFrontInsideOfMMW();
                    DisplayModeElementValue = Strings.MeasureTab_DisplayMode_SinglePanel;
                    return;
                }
            }
            if (mmwFocusedPages.Contains(selectedPage))
            {
                if (mmw != null && activePanels.Contains(mmw))
                {
                    bringContentToFront(mmw, alreadyExisting);
                }
            }
            else
            {
                var lastUsedNOTMMWPage = activePanels.Where(p => p != mmw).LastOrDefault();
                if (lastUsedNOTMMWPage != null)
                {
                    bringContentToFront(lastUsedNOTMMWPage, alreadyExisting);

                }
            }

        }
        private List<string> getMMWPages()
        {
            mmwFocusedPages = new List<string>();
            mmwFocusedPages.Add(Strings.Global_Display);
            mmwFocusedPages.Add(Strings.Global_Export);
            mmwFocusedPages.Add(Strings.Global_Home);
            //mmwFocusedPages.Add(Strings.Global_Import);
            //mmwFocusedPages.Add(Strings.Global_Measure);
            mmwFocusedPages.Add(Strings.Global_Port);
            mmwFocusedPages.Add(Strings.Global_Sensor);

            return mmwFocusedPages;
        }

        private void edgeTypeElement_EditValueChanged(object sender, EventArgs e)
        {
            edgeType = edgeTypeElementValue;
        }

        private void selectIncomingLiveChannelsElement_EditValueChanged(object sender, EventArgs e)
        {
            //selectin
        }
    }

}
