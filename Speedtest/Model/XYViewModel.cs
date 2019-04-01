﻿using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Geared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Speedtest.Model
{
    public class XYViewModel
    {
        public GearedValues<ObservablePoint> xyChartList { get; set; }


        public XYViewModel()
        {
            xyChartList = new GearedValues<ObservablePoint>();
        }
    }
    public class XYPoint{
        public double x;
        public double y;

        public XYPoint(double x, double y)
        {
            this.x = x;
            this.y = y;
        }
    }
}
