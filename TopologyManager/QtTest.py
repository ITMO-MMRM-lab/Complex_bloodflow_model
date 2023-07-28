import sys

from PyQt5.QtWidgets import *
from PyQt5.QtGui import *
from PyQt5.QtCore import *
from PyQt5.QtCore import QObject, pyqtSignal
from PyQt5.QtWidgets import QScrollArea
from PyQt5.uic import *

from PyQt5.QtChart import *
from PyQt5.QtGui import QPolygonF, QPainter

from OpenGL.GL import *
from OpenGL.GLUT import *
from OpenGL.GLU import *


import IOModule as IO
import EventProcessing

import numpy as np

class Topology3DPainter(QObject):
    add_point_signal = pyqtSignal(int, name='add_point')
    change_point_signal = pyqtSignal(int, name='change_point')
    rem_point_signal = pyqtSignal(int, int, name='rem_point')

    def __init__(self):
        QObject.__init__(self)
        self.center_pos = np.zeros(3)
        self.max_vector = np.zeros(3)
        self.min_vector = np.zeros(3)
        self.vascular_net = None

        self.angle_x = 0
        self.angle_y = 0

        self.trans_x = 0
        self.trans_y = 0
        self.trans_z = 0

        self.zoom = 1

        self.event_processor = EventProcessing.Mouse3DParser(1, 1, 1)
        self.selected_nodes = []
        self.current_node    = None
        self.current_node_id = -1


    def __draw_circle__(self, pos, size = 0.01, color = (1.0, 1.0, 0.0)):
        glColor3f(color[0], color[1], color[2])
        glBegin(GL_LINES)
        glVertex3f(pos[0]+size, pos[1], pos[2])
        glVertex3f(pos[0]- size, pos[1], pos[2])
        glVertex3f(pos[0], pos[1]+size, pos[2])
        glVertex3f(pos[0], pos[1]-size, pos[2])
        glVertex3f(pos[0], pos[1], pos[2] + size)
        glVertex3f(pos[0], pos[1], pos[2] - size)

        glEnd()
        glColor3f(1.0, 1.0, 1.0)

      #  glBegin(GL_POINTS)

    def paintGL(self):
        if self.vascular_net==None:
            return None

        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT)
        glBegin(GL_POINTS)
        for node in self.vascular_net:
            for ngb in node.bonds:
                nnode = self.vascular_net[ngb]
                glVertex3f(node.pos[0], node.pos[1], node.pos[2])
                glVertex3f(nnode.pos[0], nnode.pos[1], nnode.pos[2])
        glEnd()

        for node in self.selected_nodes:
            self.__draw_circle__(node.pos)
            self.__draw_circle__(self.current_node.pos, 0.02, (1.0, 0.0, 0.0))

        glLoadIdentity()
        gluPerspective(60*self.zoom, self.size.width()/self.size.height(), 0.1, 50.0)

        glTranslatef(-self.trans_x, self.trans_y, self.trans_z)
        gluLookAt(-2, 0, 0, 0, 0, 0, 0, 1, 0)
        glRotatef(self.angle_x, 0, -1, 0)

        glRotatef(self.angle_y, -1*np.sin(self.angle_x*np.pi/180.0), 0, -1*np.cos(self.angle_x*np.pi/180.0))
        glTranslatef(-self.center_pos[0], -self.center_pos[1], -self.center_pos[2])

        self.modelview  = glGetDoublev(GL_MODELVIEW_MATRIX)
        self.projection = glGetDoublev(GL_PROJECTION_MATRIX)
        self.viewport =   glGetIntegerv(GL_VIEWPORT)

    def loadObject(self, vasculat_net):
        self.vascular_net = vasculat_net
        self.__calcSceneSize()

    def unloadAll(self):
        self.vascular_net = None
        self.selected_nodes = []
        center_pos = np.zeros(3)
        max_vector = np.zeros(3)
        min_vector = np.zeros(3)

        self.current_node = None
        self.current_node_id = -1

        self.angle_x = 0
        self.angle_y = 0

        self.trans_x = 0
        self.trans_y = 0
        self.trans_z = 0

        self.zoom = 1

    def __calcSceneSize(self):
        center_pos = np.zeros(3)
        max_vector = np.zeros(3)
        min_vector = np.zeros(3)

        for node in self.vascular_net:
            center_pos[:] = center_pos[:] + node.pos[:]

        center_pos = center_pos / len(self.vascular_net)

        for node in self.vascular_net:
            delta = node.pos - center_pos
            for i in range(3):
                if max_vector[i] < delta[i]:
                    max_vector[i] = delta[i]
                if min_vector[i] > delta[i]:
                    min_vector[i] = delta[i]

        delta = max_vector - min_vector
        up = np.zeros(3)
        up[delta.argmax()] = 1.0

        self.center_pos = center_pos
        self.min_vector = min_vector
        self.max_vector = max_vector


    def mouseMoveEvent(self, event):
        if event.buttons() == Qt.LeftButton:
            self.angle_x, self.angle_y, self.trans_x, self.trans_y = self.event_processor.getGasture((event.x(), event.y()))
        pass

    def mouseClkEvent(self, event):
        self.event_processor.clk((event.x(), event.y()))
        pass

    def keyPressEvent(self, event):
        if event.key()==Qt.Key_Control:
            self.event_processor.cntrl_press()

        if event.key()==Qt.Key_Escape and len(self.selected_nodes)>0:
            self.selected_nodes.remove(self.current_node)
            rem_id = self.current_node_id
            if len(self.selected_nodes)==0:
                self.current_node = None
                self.current_node_id = -1
                self.rem_point_signal.emit(rem_id, -1)
                return None
            else:
                self.current_node_id=0
                self.current_node = self.selected_nodes[self.current_node_id]
                self.rem_point_signal.emit(rem_id, self.current_node.id)
                return None



        if event.key() == Qt.Key_Right or event.key() == Qt.Key_Left:
            if event.key() == Qt.Key_Right:
                self.current_node_id+=1
            if event.key() == Qt.Key_Left:
                self.current_node_id -= 1
            self.current_node_id = self.current_node_id % len(self.selected_nodes)
            self.current_node = self.selected_nodes[self.current_node_id]
            self.change_point_signal.emit(self.current_node.id)

    def keyReleaseEvent(self, event):
        if event.key()==Qt.Key_Control:
            self.event_processor.cntrl_rel()
        pass

    def wheelEvent(self, event):
        self.trans_z, self.zoom = self.event_processor.transz(np.sign(event.angleDelta().y()))

    def doubleClickEvent(self, event):
        print((event.x(), event.y()))

        clk_x = event.x()
        clk_y = (self.size.height() - event.y())

        min_i = 0

        def delta(x):
            screen_c = gluProject(x.pos[0], x.pos[1], x.pos[2], self.modelview, self.projection, self.viewport)
            return np.linalg.norm(np.asarray([clk_x - screen_c[0], clk_y - screen_c[1]]))

        min_i = np.asarray([delta(node) for node in self.vascular_net]).argmin()

        if delta(self.vascular_net[min_i])<50:
            print(min_i)
            self.selected_nodes.append(self.vascular_net[min_i])
            self.current_node = self.vascular_net[min_i]
            self.current_node_id = len(self.selected_nodes)-1

        self.add_point_signal.emit(min_i)

    def resizeEvent(self, event):
        self.size = event.size()
        pass

    def getCurrentNode(self):
        return self.current_node

class mainWindow(QMainWindow):
    def __init__(self, *args):
        super(mainWindow, self).__init__(*args)
        loadUi('minimal_1.ui', self)

        scroll_area   = self.findChild(QScrollArea, str('PlotsArea'))
        self.scroll_layout = QVBoxLayout()
        self.scroll_layout.setContentsMargins(1,1,1,1,)
        plot_widget = QWidget()
        plot_widget.setLayout(self.scroll_layout)
        scroll_area.setWidget(plot_widget)
        self.charts_list = []
        self.curr_chart = None
        self.data_dictionary = {}

        #self.openGLWidget = self.findChild(QOpenGLWidget, str('Topology3D'))
        self.topology_painter = Topology3DPainter()

        self.period_ids = np.array([])
        self.data_dict = {}
        self.time_data = np.array([])

        self.top_info_string = ""
        self.dyn_info_string = ""
        self.node_info_string = ""

        self.time = 0.0
        self.period = 0
        self.time_info_string = "Period: "+ str(self.period)+ "; Time: " + str(self.time)

    def setVascularNet(self, net, name=""):
        self.top_info_string = self.top_info_string + name + "\nTotal nodes: " + str(len(net)) + "; "
        kn = sum(len(node.bonds) > 2 for node in net)
        term = sum(len(node.bonds) == 1 for node in net)
        self.top_info_string = self.top_info_string + "Knots: " + str(kn) + "; Ends: " + str(term)
        self.topology_painter.loadObject(net)
        self.vascular_net = net

    def setupUI(self):
        self.Topology3D.initializeGL()
        self.Topology3D.resizeGL(1000,600)
        self.Topology3D.paintGL = self.topology_painter.paintGL
        self.Topology3D.mouseMoveEvent = self.topology_painter.mouseMoveEvent
        self.Topology3D.mousePressEvent = self.topology_painter.mouseClkEvent
        self.Topology3D.keyPressEvent = self.topology_painter.keyPressEvent
        self.Topology3D.keyReleaseEvent = self.topology_painter.keyReleaseEvent
        self.Topology3D.wheelEvent = self.topology_painter.wheelEvent
        self.Topology3D.mouseDoubleClickEvent = self.topology_painter.doubleClickEvent

        self.Topology3D.setFocusPolicy(Qt.StrongFocus)
        self.Topology3D.resizeEvent = self.topology_painter.resizeEvent

        self.topology_painter.add_point_signal.connect(self.addPointHandler)
        self.topology_painter.change_point_signal.connect(self.changePointHandler)
        self.topology_painter.rem_point_signal.connect(self.remPointHandler)
        self.TopButton.clicked.connect(self.openTopHandler)
        self.DynButton.clicked.connect(self.openDynHandler)
        self.CleanButton.clicked.connect(self.unloadAll)

        self.DecPeriod.clicked.connect(self.incPeriodHandler)
        self.IncPeriod.clicked.connect(self.decPeriodHandler)

        self.timer = QTimer(self)
        self.timer.timeout.connect(self.Topology3D.update)
        self.timer.start(40)

        self.setFocusPolicy(Qt.StrongFocus)

        self.scroll_layout.setAlignment(Qt.AlignTop)

        self.updateInfo()

    def changePointHandler(self, id):
        if self.curr_chart != None:
            self.curr_chart.deselect()

        self.node_info_string = "Id="+str(id)

        self.curr_chart = None

        if len(self.charts_list)!=0:
            chart = list(filter(lambda el: int(el.objectName())==id, self.charts_list))[0]
            chart.select()
            self.curr_chart = chart

        self.updateInfo()
        pass

    def addPointHandler(self, id):
        self.node_info_string = "Id="+str(id)

        if self.period_ids.size==0:
            self.node_info_string += "; No data!"
            self.updateInfo()
            return None

        lb = self.period_ids[self.period]
        rb = len(self.time_data)
        if self.period + 1 < len(self.period_ids):
            rb = self.period_ids[self.period + 1]

        data1 = self.data_dict[id][0]
        data2 = self.data_dict[id][1]
        if self.curr_chart!=None:
            self.curr_chart.deselect()

        self.curr_chart = self.addChartWidget(id, self.time_data[lb:rb], data1[lb:rb], data2[lb:rb])
        self.changePointHandler(id)

        self.updateInfo()
        pass

    def incPeriodHandler(self):
        self.period+=1
        if self.period>=len(self.period_ids):
            self.period -=1
        self.__updateChartData__()

    def decPeriodHandler(self):
        self.period -= 1
        if self.period <0:
            self.period = 0
        self.__updateChartData__()

    def __updateChartData__(self):
        if self.period_ids.size()==0:
            return

        lb = self.period_ids[self.period]
        rb = len(self.time_data)
        if self.period + 1 < len(self.period_ids):
            rb = self.period_ids[self.period + 1]

        for c_wgt in self.charts_list:
            data1 = self.data_dict[int(c_wgt.objectName())][0]
            data2 = self.data_dict[int(c_wgt.objectName())][1]
            c_wgt.replaceData(self.time_data[lb:rb], data1[lb:rb], data2[lb:rb])

        self.time_info_string = "Period: " + str(self.period) + "; Time: " + str(self.time)
        self.updateInfo()
        pass

    def remPointHandler(self, rem_id, id):
        if id>-1:
            self.changePointHandler(id)
        else:
            self.node_info_string = ""
            self.curr_chart = None

        chart = self.charts_list[rem_id]
        self.charts_list.remove(chart)
        self.scroll_layout.removeWidget(chart)
        chart.deleteLater()

        self.updateInfo()
        pass

    def unloadDynamics(self):
        self.dyn_info_string = ""
        self.period_ids = np.array([])
        self.data_dict = {}
        self.time_data = np.array([])
        self.time = 0.0
        self.period = 0
        self.DecPeriod.setEnabled(False)
        self.IncPeriod.setEnabled(False)

    def unloadAll(self):
        self.topology_painter.unloadAll()
        self.charts_list = []
        self.data_dictionary = {}
        self.node_info_string = ""
        self.top_info_string = ""
        self.dyn_info_string = ""
        self.time = 0.0
        self.period = 0
        self.DynButton.setEnabled(False)
        self.CleanButton.setEnabled(False)
        self.updateInfo()
        self.DecPeriod.setEnabled(False)
        self.IncPeriod.setEnabled(False)

    def openTopHandler(self):
        fname = QFileDialog.getOpenFileName(self, 'Open *.top file')[0]
        if fname=="":
            return None
        self.unloadAll()
        name = fname.split('/')[-1]
        self.setVascularNet(IO.read_top_file(fname),name)
        self.DynButton.setEnabled(True)
        self.CleanButton.setEnabled(True)
        self.updateInfo()

    def openDynHandler(self):
        fname = QFileDialog.getOpenFileName(self, 'Open *.dyn file')[0]
        if fname=="":
            return None
        self.unloadDynamics()
        name = fname.split('/')[-1]
        self.period_ids, self.time_data, self.data_dict = IO.read_dyn_file(fname)
        self.dyn_info_string = self.dyn_info_string + "Dynamics: " + name + "; Time:" + str(self.time_data[0]) + " - " + str(self.time_data[-1])

        self.DecPeriod.setEnabled(True)
        self.IncPeriod.setEnabled(True)

        self.updateInfo()

    def addChartWidget(self, id, xdata, ydataF, ydataP=None):
        chart = PlotWidget("Id = "+str(id))
        chart.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Fixed)
        chart.setMinimumHeight(200)
        chart.setObjectName(str(id))
        chart.add_major_data(xdata, ydataF, "Flux, ml/s")
        if not (ydataP is None):
            chart.add_minor_data(xdata, ydataP, "Pressure, kPa")
        self.charts_list.append(chart)
        self.data_dictionary[id] = (xdata, ydataF, ydataP)
        self.scroll_layout.addWidget(chart)
        return chart

    def keyPressEvent(self, event):
        print(event.key())

    def updateInfo(self):
        self.Info.setText(self.top_info_string + "\n" +self.node_info_string+ "\n" + self.dyn_info_string
                          + "\n" + self.time_info_string)

def series_to_polyline(xdata, ydata):
    """Convert series data to QPolygon(F) polyline

    This code is derived from PythonQwt's function named
    `qwt.plot_curve.series_to_polyline`"""
    size = len(xdata)
    polyline = QPolygonF(size)
    pointer = polyline.data()
    dtype, tinfo = np.float, np.finfo  # integers: = np.int, np.iinfo
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

        max = np.ceil(ydata.max()* 1.2 * 2)/2.0
        min = np.floor(2*ydata.min())/2.0#np.ceil(np.abs(ydata.min())* 1.2 * 2) * np.sign(ydata.min())/2.0

        add_axisY.setMax(max)
        add_axisY.setMin(min)

        add_axisY.setLabelsFont(font)
        add_axisY.setLabelFormat("%2.1f")
        add_axisY.setLabelsAngle(-90)

        self.chart.addAxis(add_axisY, Qt.AlignRight)
        curve.attachAxis(add_axisY)
        str = self.chart.title()
        if titel!="":
            self.chart.setTitle(str + "; " + titel)

        self.chart.setTitle(str + "; " +titel)

        self.ncurves += 1

    def replaceData(self, x_data, y_data, y_data1=None):
        self.chart.removeAllSeries()
        self.chart.setTitle("")

        self.chart.removeAxis(self.chart.axisY())
        if not (self.chart.axisY() is None):
            self.chart.removeAxis(self.chart.axisY())

        self.chart.removeAxis(self.chart.axisY())
        self.add_major_data(x_data, y_data, "Flux, ml/s")
        if not (y_data1 is None):
            self.add_minor_data(x_data, y_data1, "Pressure, kPa")

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

        max = np.ceil(ydata.max()* 1.2 * 2)/2.0
        min = np.floor(2*ydata.min())/2.0#np.ceil(np.floor(ydata.min())* 1.2 * 2)*np.sign(ydata.min())/2.0

        axisY.setMax(max)
        axisY.setMin(min)

        axisY.setLabelsFont(font)
        axisY.setLabelFormat("%2.1f")
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


def main():
    app = QApplication(sys.argv)
    window = mainWindow()
    window.setupUI()

   # vascular_net = IO.read_top_file("full_body_Boileau_LR_2.5mm.top")

 #   window.setVascularNet(vascular_net, "full_body_Boileau_LR_2.5mm.top")

    #IO.read_dyn_file("full_body_Boileau_LR_2.5mm.dyn")

    npoints = 1000
    xdata = np.linspace(0., 10., npoints)
    ydata1 = np.sin(xdata)
    ydata2 = np.cos(xdata)

    window.show()
    sys.exit(app.exec_())

main()

