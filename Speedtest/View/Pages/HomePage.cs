﻿using DevExpress.XtraBars;
using DevExpress.XtraEditors.Repository;
using Speedtest.Controller;
using Speedtest.Properties;
using System;
using System.Windows.Forms;

namespace Speedtest
{
    public partial class MainFrame
    {
        #region ElementValues

        public int numberOfChannelsElementValue
        {
            get { return Int32.Parse(channelsElement.EditValue.ToString()); }
            set { channelsElement.EditValue = value; }
        }

        public int samplingRateElementValue
        {
            get { return Int32.Parse(samplingRateElement.EditValue.ToString()); }
            set
            {
                samplingRateElement.EditValue = value;
                deltaTime = 1 / (double)value;
            }
        }
        public int keepRecordsElementValue
        {
            get { return Int32.Parse(keepRecordsElement.EditValue.ToString()); }
            set { keepRecordsElement.EditValue = value; }
        }
        public string DisplayModeElementValue
        {
            get { return (string)displayModeElement.EditValue; }
            set { displayModeElement.EditValue = value; }
        }

        public int selectIncomingLiveChannelsElementValue
        {
            get { return Int32.Parse(selectIncomingLiveChannelsElement.EditValue.ToString()) - 1; }
            set { selectIncomingLiveChannelsElement.EditValue = value; }
        }
        #endregion
        #region EditValueChanged
        private void channelsElement_EditValueChanged(object sender, EventArgs e)
        {
            numberOfPanels = Int32.Parse(channelsElement.EditValue.ToString());
        }
        private void samplingRateElement_EditValueChanged(object sender, EventArgs e)
        {
            //Hz given, and we need millisec
            //so 1/f*1000 -> 1000/f
            if (portController != null)
            {
                portController.deltaTime = 1 / samplingRateElementValue;
                deltaTime = 1 / (double)samplingRateElementValue;
            }
        }

        private void keepRecordsElement_EditValueChanged(object sender, EventArgs e)
        {
            if (keepRecordsElement.EditValue != null)
            {
                if (keepRecordsElementValue < 1)
                {
                    keepRecordsElementValue = 1;
                }
                else if (keepRecordsElementValue > 1000)
                {
                    keepRecordsElementValue = 1000;
                }

                if (mmw != null)
                {
                    foreach (var i in mmw.gearedCharts)
                    {
                        i.viewModel.keepRecords = keepRecordsElementValue;
                    }
                }
            }
        }

        #endregion
        #region Buttons
        public BarButtonItem ConnectButton { get { return connectButton; } }
        public BarButtonItem StartStopButton { get { return startStopButton; } }
        #endregion
        #region ComboBoxes
        public RepositoryItemComboBox NumberOfChannelsRepositoryItemComboBox { get { return numberOfChannelsRepositoryItemComboBox; } }
        public RepositoryItemComboBox DisplayModeRepositoryItemComboBox { get { return displayModeRepositoryItemComboBox; } }
        public RepositoryItemComboBox SelectIncomingLiveChannelsRepositoryItemComboBox { get { return selectIncomingLiveChannelsRepositoryItemComboBox; } }
        #endregion
        #region Elements
        public BarEditItem DisplayModeElement { get { return displayModeElement; } }
        public BarEditItem ChannelsElement { get { return channelsElement; } }
        public BarEditItem SelectIncomingLiveChannelsElement { get { return selectIncomingLiveChannelsElement; } }
        #endregion

        private void connectButton_ItemClick(object sender, ItemClickEventArgs ea)
        {
            if (connectedState)
            {
                //Disconnecting
                if (isRunning)
                {
                    startStopButton.PerformClick();
                }

                homePortBasicGroup.Enabled = true;
                keepRecordsElement.Enabled = true;


                contentPanel.Controls.Clear();
                activePanels.Remove(mmw);
                mmw.deleteControls();
                mmw.Dispose();
                PortController.CloseSerialOnExit(serialPort);
                serialPort.Dispose();
                HomeTabController.SetGroupsAndIconsToCurrentState(this);
                connectedState = false;
                homeControlPanelGroup.Enabled = false;


            }
            else
            {
                //Connecting

                homePortBasicGroup.Enabled = false;
                keepRecordsElement.Enabled = false;

                if (String.IsNullOrWhiteSpace(selectedPortElementValue))
                {
                    MessageBox.Show(Strings.Global_Error_NoPortSelected);
                }
                else
                {
                    testConnect();
                }
                connectedState = true;
                isRunning = false;
                homeControlPanelGroup.Enabled = true;
            }

        }
        private void displayModeElement_EditValueChanged(object sender, EventArgs e)
        {

            try
            {
                if (mmw != null)
                {
                    if (DisplayModeElementValue == Strings.MeasureTab_DisplayMode_MultiPanel)
                    {
                        BringChartToFrontInsideOfMMW();
                        SelectIncomingLiveChannelsElement.Enabled = false;
                    }
                    else if (DisplayModeElementValue == Strings.MeasureTab_DisplayMode_Monitor)
                    {
                        BringMonitorToFrontInsideOfMMW();
                        SelectIncomingLiveChannelsElement.Enabled = false;
                    }
                    else if (DisplayModeElementValue == Strings.MeasureTab_DisplayMode_SinglePanel)
                    {
                        BringSingleChartToFrontInsideOfMMW();
                        SelectIncomingLiveChannelsElement.Enabled = true;

                    }
                }
            }
            catch (Exception)
            {

                //MessageBox.Show("displayModeElement_EditValueChanged");
            }


        }

        private void BringSingleChartToFrontInsideOfMMW()
        {
            mmw.xyChartUserControl.Dock = DockStyle.Fill;
            mmw.xyChartUserControl.BringToFront();

            mmw.chartMonitor.SendToBack();
            mmw.chartMonitor.Dock = DockStyle.None;

            ChartController.RemoveMonitorText(this);
            ChartController.RemoveAllPointsFromGeared(defaultCharts);
        }

        private void BringMonitorToFrontInsideOfMMW()
        {
            mmw.chartMonitor.Dock = DockStyle.Fill;
            mmw.chartMonitor.BringToFront();

            mmw.xyChartUserControl.Dock = DockStyle.None;
            mmw.xyChartUserControl.SendToBack();

            ChartController.RemoveAllPointsFromGeared(defaultCharts);
        }

        private void BringChartToFrontInsideOfMMW()
        {
            mmw.chartMonitor.Dock = DockStyle.None;
            mmw.chartMonitor.SendToBack();
            mmw.xyChartUserControl.Dock = DockStyle.None;
            mmw.xyChartUserControl.SendToBack();
            ChartController.RemoveMonitorText(this);
        }

        private void startStopButton_ItemClick(object sender, ItemClickEventArgs ea)
        {
            try
            {
                deltaTime = 1 / samplingRateElementValue;
                if (isRunning)
                {
                    isRunning = false;
                    dc.Dispose();
                    StartStopButton.Caption = Strings.Global_Start;
                    StartStopButton.ImageOptions.SvgImage = Resources.start;
                }
                else
                {
                    isRunning = true;
                    if (serialPort == null)
                    {
                        portController.CreatePort();

                    }
                    if (!serialPort.IsOpen)
                    {
                        portController.OpenThePort();
                    }
                    dc = new DataCollector(serialPort, printTo);
                    serialPort.DiscardInBuffer();
                    StartStopButton.Caption = Strings.Global_Stop;
                    StartStopButton.ImageOptions.SvgImage = Resources.stop;

                    if (DisplayModeElementValue == Strings.MeasureTab_DisplayMode_Monitor)
                    {
                        BringMonitorToFrontInsideOfMMW();
                    }
                    else if (DisplayModeElementValue == Strings.MeasureTab_DisplayMode_MultiPanel)
                    {
                        BringChartToFrontInsideOfMMW();
                    }
                    else if (DisplayModeElementValue == Strings.MeasureTab_DisplayMode_SinglePanel)
                    {
                        BringSingleChartToFrontInsideOfMMW();
                    }


                }
            }
            catch (Exception e)
            {

                //MessageBox.Show("startStopButton_ItemClick");
            }




        }
        private void MainFrame_Resize(object sender, EventArgs e)
        {
            if ((this.WindowState == FormWindowState.Maximized || this.WindowState == FormWindowState.Minimized) && mmw != null)
            {

                mmw.resizeControls(contentPanel.Height, contentPanel.Width);

            }
            else if (mmw != null)
            {
                mmw.resizeControls(contentPanel.Height, contentPanel.Width);
            }

        }
        #region Labels
        public BarStaticItem IsPortConnectedStatusBarLabel
        {
            get { return portStatusLabel; }
            set { portStatusLabel = value; }
        }
        #endregion
    }
}
