
from PyQt5.QtGui import *
from PyQt5.QtGui import QPainter
from PyQt5.QtCore import *
from PyQt5.QtChart import *

import numpy as np

def series_to_polyline(xdata, ydata):
    """Convert series data to QPolygon(F) polyline

    This code is derived from PythonQwt's function named
    `qwt.plot_curve.series_to_polyline`"""
    size = len(xdata)
    polyline = QPolygonF(size)
    pointer = polyline.data()
    dtype, tinfo = float, np.finfo  # integers: = np.int, np.iinfo
    pointer.setsize(2 * polyline.size() * tinfo(dtype).dtype.itemsize)
    memory = np.frombuffer(pointer, dtype)
    memory[:(size - 1) * 2 + 1:2] = xdata
    memory[1:(size - 1) * 2 + 2:2] = ydata
    return polyline

class PlotWidget(QChartView):
    def __init__(self, main_titel, parent = None):
        QChartView.__init__(self, parent)
        self.ncurves = 0
        self.chart = QChart()
        self.chart.setContentsMargins(-30, -20, -25, -20)
        self.setFrameStyle(0)
        font = QFont()
        font.setPixelSize(10)
        self.chart.setTitleFont(font)

        self.chart.legend().hide()
        self.setRenderHint(QPainter.Antialiasing)
        self.chart.setTitle(main_titel)
        self.setChart(self.chart)

        self.setMinimumHeight(100)
        self.setMaximumHeight(200)

    def add_minor_data(self, xdata, ydata, titel="", color=Qt.blue):
        curve = QLineSeries()
        pen = curve.pen()
        if color is not None:
            pen.setColor(color)
        pen.setWidthF(.1)
        curve.setPen(pen)
        curve.setUseOpenGL(True)
        curve.append(series_to_polyline(xdata, ydata))
        font = QFont()
        font.setPixelSize(10)

        add_axisY = QValueAxis()
        self.chart.addSeries(curve)

        if (ydata.max() !=0):
            mult_max = 10 ** (np.ceil(np.log10(abs(1 / ydata.max())))+1)
        else:
            mult_max = 1.0

        if (ydata.min() !=0):
            mult_min = 10 ** (np.ceil(np.log10(abs(1 / ydata.min())))+1)
        else:
            mult_min = 1.0

        max = np.ceil(ydata.max() * mult_max) / mult_max
        min = np.floor(mult_min * ydata.min()) / mult_min


        if(max==min):
            min = min-0.1
            max = max+0.1

        add_axisY.setMax(max)
        add_axisY.setMin(min)

        add_axisY.setLabelsFont(font)
        add_axisY.setLabelFormat("%2.2f")
        add_axisY.setLabelsAngle(-90)

        self.chart.addAxis(add_axisY, Qt.AlignRight)
        curve.attachAxis(add_axisY)
        str = self.chart.title()
        if titel!="":
            self.chart.setTitle(str + "; " + titel)

        self.chart.setTitle(str + "; " +titel)

        self.ncurves += 1

    def replaceData(self, x_data, y_data, y_data1=None, y_data2=None):
        self.chart.removeAllSeries()
        self.chart.setTitle("Id = " + self.objectName())

        self.chart.removeAxis(self.chart.axisY())
        if not (self.chart.axisY() is None):
            self.chart.removeAxis(self.chart.axisY())

        self.chart.removeAxis(self.chart.axisY())
        self.add_major_data(x_data, y_data, "Flux, ml/s")
        if not (y_data1 is None):
            self.add_minor_data(x_data, y_data1, "Pressure, kPa")
        if y_data is not None:
            self.add_minor_data(x_data, y_data2, "Agent C", color=Qt.green)

    def add_major_data(self, xdata, ydata, titel="", color=Qt.red):
        curve = QLineSeries()
        pen = curve.pen()
        if color is not None:
            pen.setColor(color)
        pen.setWidthF(.1)
        curve.setPen(pen)
        curve.setUseOpenGL(True)
        curve.append(series_to_polyline(xdata, ydata))
        font = QFont()
        font.setPixelSize(10)

        axisX = QValueAxis()
        axisY = QValueAxis()
        self.chart.addSeries(curve)
        axisX.setMax(1)
        axisX.setLabelsFont(font)
        axisX.setLabelFormat("%2.1f")

        if (ydata.max() !=0):
            mult_max = 10 ** (np.ceil(np.log10(abs(1 / ydata.max())))+1)
        else:
            mult_max = 1.0

        if (ydata.min() !=0):
            mult_min = 10 ** (np.ceil(np.log10(abs(1 / ydata.min())))+1)
        else:
            mult_min = 1.0

        max = np.ceil(ydata.max() * mult_max) / mult_max
        min = np.floor(mult_min * ydata.min()) / mult_min

        if(max==min):
            min = min-0.1
            max = max+0.1

        axisY.setMax(max)
        axisY.setMin(min)

        axisY.setLabelsFont(font)
        axisY.setLabelFormat("%2.2f")
        axisY.setLabelsAngle(-90)

        self.chart.setAxisX(axisX)
        self.chart.setAxisY(axisY, curve)

        str = self.chart.title()
        if titel!="":
            self.chart.setTitle(str + "; " + titel)

        self.ncurves += 1

    def select(self):
        self.setFrameStyle(2)

    def deselect(self):
        self.setFrameStyle(0)

    def set_title(self, title):
        self.chart.setTitle(title)
