from PyQt5.QtWidgets import *
from PyQt5 import QtGui, QtCore
from PyQt5.QtWidgets import QScrollArea
from PyQt5.uic import *
from Plot import *
from EditPanel import *

import matplotlib.pyplot as plt

import IOModule as IO
import TopologyPainter as TP
import TopologyGraph as Graph

import numpy as np

class MainWindow(QMainWindow):
    def __init__(self, *args):
        super(MainWindow, self).__init__(*args)
        loadUi('minimal_1.ui', self)

        scroll_area   = self.findChild(QScrollArea, str('PlotsArea'))
        self.scroll_layout = QVBoxLayout()
        self.scroll_layout.setContentsMargins(1,1,1,1,)
        plot_widget = QWidget()
        plot_widget.setLayout(self.scroll_layout)
        scroll_area.setWidget(plot_widget)
        self.charts_dict = {}
        self.points_list = []
        self.frozen_points_list = []
        self.curr_chart = None

        self.topology_painter = TP.Topology3DPainter()

        self.top_info_string = ""
        self.dyn_info_string = ""
        self.node_info_string = ""
        self.error_msg = ""

        self.dyn_data_manager = DataManager()
        self.vascular_net = None
        self.sec_vascular_net = None

        self.top_path = ""
        self.par_path = ""

    def setupUI(self):
        self.topology_painter.initializeGL()
        self.topology_painter.resizeGL(700, 800)

        self.MainLayout.addWidget(self.topology_painter)
        self.topology_painter.setFixedHeight(800)
        self.topology_painter.setFixedWidth(700)

        self.topology_painter.show()
        self.setFocusPolicy(Qt.StrongFocus)
        self.topology_painter.resizeEvent = self.topology_painter.resizeEvent
        self.topology_painter.dclk_signal.connect(self.addPointHandler)


        self.edit_panel = EditPanel(self.EditTab)
        self.ControlPanel.setCurrentIndex(0)
        self.ControlPanel.currentChanged.connect(self.changeTabHandler)

        self.actionLoadTop.triggered.connect(self.openTopHandler)
        self.actionClearTop.triggered.connect(self.unloadAll)
        self.actionSaveTop.triggered.connect(lambda : self.saveTopology())
        self.actionSaveSelTop.triggered.connect(lambda: self.saveTopology(self.edit_panel.selection))
        self.actionLoadPar.triggered.connect(self.openParHandler)
        self.actionClearPar.triggered.connect(self.unloadParameters)
        self.actionScreenshot.triggered.connect(self.takeScreenshot)

        self.CurrentNode.returnPressed.connect(lambda: self.addPointHandler(int(self.CurrentNode.text())))
        self.showAgentValue(0.5, self.MaxAgent)
        self.horizontalSlider.setMinimum(0)
        self.horizontalSlider.setMaximum(100)
        self.horizontalSlider.setValue(50)
        self.horizontalSlider.setTickPosition(QSlider.TicksBelow)
        self.horizontalSlider.valueChanged.connect(lambda: self.showAgentValue(self.horizontalSlider.value(), self.MaxAgent))
        self.horizontalSlider.setTickInterval(0.05)

        self.periodSlider.hide()
        self.DecPeriod.hide()
        self.IncPeriod.hide()
        self.label.hide()

        self.actionClearAddTop.triggered.connect(self.clearSecTopology)
        self.actionAddTopology_2.triggered.connect(self.openSecTopHandler)

        self.DynButton.clicked.connect(self.openDynHandler)
        self.CleanDynButton.clicked.connect(self.unloadDynamics)

        self.menuBC_Parameters.setEnabled(False)

        self.DecPeriod.clicked.connect(self.nextPeriodHandler)
        self.IncPeriod.clicked.connect(self.prevPeriodHandler)

        self.SaveDButton.clicked.connect(self.saveDynamicsHandler)

        self.timer = QTimer(self)
        self.menu_timer = QTimer(self)
        self.timer.timeout.connect(self.topology_painter.update)
        self.menu_timer.timeout.connect(self.mainMenuManager)
        self.timer.start(40)
        self.menu_timer.start(1000)

        self.scroll_layout.setAlignment(Qt.AlignTop)
        self.updateInfo()

    def changeTabHandler(self):
        curr_tab_name = self.ControlPanel.currentWidget().objectName()

        if curr_tab_name=="ViewTab":
            for n in self.frozen_points_list:
                self.points_list.append(n)
                if not(n.id in self.vascular_net):
                    self.remPointHandler(n)
                    self.frozen_points_list.remove(n)
            self.edit_panel.hideSelection()
            self.topology_painter.dclk_signal.disconnect()
            self.topology_painter.dclk_signal.connect(self.addPointHandler)

        if curr_tab_name == "EditTab":
            self.frozen_points_list = self.points_list.copy()
            self.points_list.clear()
            self.edit_panel.showSelection()
            self.edit_panel.buttonManager()

            self.topology_painter.dclk_signal.disconnect()
            self.topology_painter.dclk_signal.connect(lambda id: self.edit_panel.selectSlot(id))

    def addPointHandler(self, id):
        self.node_info_string = "Id="+str(id)

        if self.vascular_net is None or not (id in self.vascular_net.keys()):
            return None

        self.points_list.append(self.vascular_net[id])
        self.current_node = self.vascular_net[id]
        self.topology_painter.selectNode(self.current_node.id)

        if self.dyn_data_manager.isEmpty():
            self.updateInfo()
            return None

        t, d1, d2, d3 = self.dyn_data_manager.getData(id)

        if self.curr_chart is not None:
            self.curr_chart.deselect()

        self.curr_chart = self.addChartWidget(id, t, d1, d2, d3)
        self.charts_dict[self.current_node] = self.curr_chart
        self.curr_chart.select()

        self.SaveDButton.setEnabled(True)

        self.updateInfo()

    def mainMenuManager(self):
        self.menuBC_Parameters.setEnabled(False)
        self.menuTopology.setEnabled(True)
        self.actionLoadTop.setEnabled(True)

        self.actionClearTop.setEnabled(False)
        self.actionSaveTop.setEnabled(False)
        self.actionSaveSelTop.setEnabled(False)
        self.actionLoadPar.setEnabled(True)
        self.actionClearPar.setEnabled(False)
        self.actionScreenshot.setEnabled(True)

        if self.vascular_net!=None:
            self.actionClearTop.setEnabled(True)
            self.actionSaveTop.setEnabled(True)
            self.menuBC_Parameters.setEnabled(True)
            self.actionAddTopology.setEnabled(True)

        if len(self.edit_panel.selection)>0:
            self.actionSaveSelTop.setEnabled(True)

        if self.par_path!="":
            self.actionClearPar.setEnabled(True)

    def unloadDynamics(self):
        self.dyn_data_manager.unloadDynamics()

        for node in self.points_list:
            chart = self.charts_dict[node]
            self.scroll_layout.removeWidget(chart)
            chart.deleteLater()

        self.curr_chart = None
        self.charts_dict.clear()

        self.CleanDynButton.setEnabled(False)
        self.DecPeriod.setEnabled(False)
        self.IncPeriod.setEnabled(False)

        self.SaveDButton.setEnabled(False)
        self.updateInfo()

    def unloadAll(self):
        self.vascular_net.clear()
        self.vascular_net = None

        charts_list = list(self.charts_dict.values())
        for chart in charts_list:
            self.scroll_layout.removeWidget(chart)
            chart.deleteLater()

        self.points_list.clear()
        self.charts_dict.clear()

        self.dyn_data_manager.unloadDynamics()

        self.DynButton.setEnabled(False)
        self.CleanDynButton.setEnabled(False)
        self.DecPeriod.setEnabled(False)
        self.IncPeriod.setEnabled(False)

        self.unloadParameters()

        self.edit_panel.clearAll()

        self.updateInfo()

    def takeScreenshot(self):
        self.topology_painter.takeScreenshot()

    def unloadParameters(self):
        if self.vascular_net!=None:
            for node in self.vascular_net.values():
                node.bc_par = None

        self.par_path = ""

        self.updateInfo()

    def openParHandler(self):
        fname = QFileDialog.getOpenFileName(self, 'Open *.par file')[0]
        if fname == "":
            return None

        self.error_msg = ""
        self.par_path = fname
        try:
            pars = IO.read_par_file(fname)
            for id in pars:
                self.vascular_net[id].bc_par = pars[id]

        except:
            self.error_msg += "Error loading parameters file: " + self.par_path + "\n"
            self.updateInfo()
            return None

        self.updateInfo()

    def clearParHandler(self):
        self.par_path = ""
        for p in self.vascular_net:
            p.bc_par = None

    def pullBCHandler(self):#Not used
        boundary_dict = {}

        for end_node in self.sec_vascular_net.values():
            if len(end_node.bonds)>1:
                continue
            for ref_node in self.vascular_net.values():
                if ref_node.id == 13257 and end_node.id == 8739:
                    print("20847!")
                if np.linalg.norm(ref_node.pos - end_node.pos) < 1e-4 and np.abs(ref_node.radius - end_node.radius)<2e-3:
                    if ref_node.bonds.size == 1 and not(ref_node.bc_par is None):
                        print(end_node.id)
                        end_node.bc_par = ref_node.bc_par
                        break

                    boundary_dict[ref_node.id] = end_node.id
                    ref_node.attribute = 0
                    if np.linalg.norm(self.vascular_net[ref_node.bonds[0]].pos - self.sec_vascular_net[end_node.bonds[0]].pos)<1e-4:
                        self.vascular_net[ref_node.bonds[0]].attribute = 0
                    else:
                        self.vascular_net[ref_node.bonds[1]].attribute = 0
                    break

        origins = set(boundary_dict.keys())
        a_parts = []
        while (len(origins)>0):
            protopart = set()
            front = set()
            o_id = origins.pop()
            if (o_id == 13257):
                o_id = 13257
            front.add(o_id)
            inlets = set()
            inlets.add(self.vascular_net[o_id])
            outlets = set()
            new_front = set()
            curr_front_value = 0
            while len(front)>0:
                for n_id in front:
                    n = self.vascular_net[n_id]
                    protopart.add(n)
                    if n.bonds.size == 1:
                        outlets.add(n)

                    for nn_id in n.bonds:
                        nn = self.vascular_net[nn_id]
                        if nn.attribute is None or nn.attribute > curr_front_value:
                            new_front.add(nn_id)
                            nn.attribute = curr_front_value + 1
                            continue

                        if nn_id in origins:
                            new_front.add(nn_id)
                            origins.remove(nn_id)
                            inlets.add(nn)
                            protopart.add(nn)
                            nn.attribute = curr_front_value + 1

                curr_front_value += 1
                front = new_front.copy()
                new_front.clear()

            a_parts.append(Graph.ArterialPart(self.vascular_net ,protopart, inlets, outlets))
            if list(inlets)[0].id == 13257:
                print("13257")


        for p in a_parts:
            p.parseNet()
            if list(p.inlets)[0].id == 13257:
                print("13257")
            p.calculateResistance()
            p.calcSurrogateBC(self.dyn_data_manager)
            #print ("R = " + str(p.R*1e3) + ", C = " + str(p.C*1e-3))

        for ref_bc in boundary_dict.keys():
            if ref_bc == 20847:
                print("20847")
            if self.vascular_net[ref_bc].bc_par:
                self.sec_vascular_net[boundary_dict[ref_bc]].bc_par = self.vascular_net[ref_bc].bc_par

        id_dict = IO.write_top_file("secondary.top",self.sec_vascular_net)
        IO.write_par_file("secondary.par", self.sec_vascular_net, id_dict)

        print ("Finished!")


    def clearSecTopology(self):
        self.sec_vascular_net.clear()

    def openSecTopHandler(self):
        fname = QFileDialog.getOpenFileName(self, 'Open *.top file')[0]
        if fname == "":
            return None

        self.error_msg = ""
        self.top_path = fname
        try:
            self.sec_vascular_net = IO.read_top_file(fname)
            if len(self.sec_vascular_net) == 0:
                raise Exception("Wrong *.top file format or empty file")
        except:
            self.error_msg += "Error loading topology file: " + self.top_path + "\n"
            self.sec_vascular_net = None
            self.updateInfo()
            return None

        self.topology_painter.loadSecObject(self.sec_vascular_net)
        self.updateInfo()

        self.pullBCHandler()

    def openTopHandler(self):
        fname = QFileDialog.getOpenFileName(self, 'Open *.top file')[0]
        if fname=="":
            return None

        self.error_msg = ""
        self.top_path = fname
        try:
            self.vascular_net = IO.read_top_file(fname)
            if len(self.vascular_net) == 0:
                raise Exception("Wrong *.top file format or empty file")

        except:
            self.error_msg += "Error loading topology file: " + self.top_path + "\n"
            self.vascular_net = None
            self.updateInfo()
            return None

        self.topology_painter.loadObject(self.vascular_net, self.points_list, self.edit_panel.selection)
        self.edit_panel.vasculat_net = self.vascular_net
        self.DynButton.setEnabled(True)
        self.menuBC_Parameters.setEnabled(True)
        self.actionClearTop.setEnabled(True)
        self.updateInfo()

    def showMessageBox(self, errorMessage):
        msg = QMessageBox()
        msg.setIcon(QMessageBox.Information)
        msg.setText(errorMessage)
        # msg.setInformativeText("Additional information")
        msg.setWindowTitle("Topology Manager")
        x = msg.exec_()

    def openDynHandler(self):
        fname = QFileDialog.getOpenFileName(self, 'Open *.dyn file')[0]
        if fname=="":
            return None

        self.dyn_data_manager.unloadDynamics()
        self.dyn_data_manager.loadData(fname)

        total_period_ids = len(self.dyn_data_manager.period_ids) - 2

        self.periodSlider.show()
        self.periodSlider.setPageStep(1)
        self.periodSlider.setMinimum(0)
        self.periodSlider.setMaximum(total_period_ids)
        self.periodSlider.setValue(0)
        self.periodSlider.setTickPosition(QSlider.TicksBelow)
        self.periodSlider.valueChanged.connect(
            lambda: self.updatePeriod())

        if not self.dyn_data_manager.isDynamicsFile():
            self.showMessageBox("Wrong file type")
            return None

        if self.dyn_data_manager.isEmpty():
            self.showMessageBox("Wrong dynamics file")
            return None

        if len(self.dyn_data_manager.data_dict) != len(self.vascular_net):
            self.showMessageBox(".top and .dyn files have different amount of nodes")
            return None

        self.topology_painter.dyn_data_manager = self.dyn_data_manager

        self.DecPeriod.setEnabled(True)
        self.IncPeriod.setEnabled(True)
        self.CleanDynButton.setEnabled(True)

        for node in self.points_list:
            t, d1, d2, d3 = self.dyn_data_manager.getData(node.id)
            self.charts_dict[node] = self.addChartWidget(node.id, t, d1, d2, d3)

        if len(self.points_list)>0:
            self.curr_chart = self.charts_dict[self.current_node]
            self.curr_chart.select

        self.updateInfo()

    def saveDynamicsHandler(self):
        f_prefix = QFileDialog.getSaveFileName(self, 'Choose path and file name prefix...')[0]
        if f_prefix=="":
            return None

        for node in self.points_list:
            fname = f_prefix + "_" + str(node.id) + ".txt"
            out_file = open(fname, "w")
            out_file.write(self.top_path + "\n")
            out_file.write(self.dyn_data_manager.name + "\n")
            out_file.write("Id: " + str(node.id) + "\n")
            scale_str = "s ml/s kPa\n"
            out_file.write(scale_str)
            t, d1, d2, d3 = self.dyn_data_manager.getData(node.id)
            for i in range(t.size):
                out_file.write(str(t[i]) + "\t" + str(d1[i]) + "\t" + str(d2[i]) + "\n")
            out_file.close()

    def addChartWidget(self, id, xdata, ydata_f, ydata_p=None, ydata_agent_c=None):
        chart = PlotWidget("Id = "+str(id))
        chart.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Fixed)
        chart.setMinimumHeight(200)
        chart.setObjectName(str(id))
        chart.add_major_data(xdata, ydata_f, "Flux, ml/s", color=Qt.blue)
        if not (ydata_p is None):
            chart.add_minor_data(xdata, ydata_p, "Pressure, kPa", color=Qt.red)
        if ydata_agent_c is not None:
            chart.add_minor_data(xdata, ydata_agent_c, "Agent C", color=Qt.green)

        self.scroll_layout.addWidget(chart)
        return chart

    def changePointHandler(self, inc):
        if len(self.points_list) > 0:
            index_pos = self.points_list.index(self.current_node)
            index_pos += inc
            index_pos = np.abs(index_pos % len(self.points_list))
            self.current_node = self.points_list[index_pos]
            self.topology_painter.selectNode(self.current_node.id)

        if self.curr_chart != None:
            self.curr_chart.deselect()

        if self.current_node in self.charts_dict:
            self.node_info_string = "Id=" + str(self.current_node.id)
            self.curr_chart = self.charts_dict[self.current_node]
            self.curr_chart.select()

        self.updateInfo()

    def keyPressEvent(self, event):
        if event.key() == Qt.Key_Right:
            self.changePointHandler(1)
        if event.key() == Qt.Key_Left:
            self.changePointHandler(-1)

        if event.key() == Qt.Key_Escape :
            self.remPointHandler()

        self.topology_painter.event(event)

    def remPointHandler(self, node = None):
        if node!=None:
            if node in self.points_list:
                self.current_node = node
            else:
                return None

        if len(self.points_list) == 0:
            return None
        index_pos = self.points_list.index(self.current_node)
        self.points_list.remove(self.current_node)

        if self.current_node in self.charts_dict:
            chart = self.charts_dict[self.current_node]
            self.scroll_layout.removeWidget(chart)
            chart.deleteLater()
            self.charts_dict[self.current_node] = None

        if len(self.points_list) > 0:
            index_pos = np.abs(index_pos % len(self.points_list))
            self.current_node = self.points_list[index_pos]
            self.topology_painter.selectNode(self.current_node.id)

            if self.current_node in self.charts_dict:
                self.curr_chart = self.charts_dict[self.current_node]
                self.curr_chart.select()
        else:
            self.SaveDButton.setEnabled(False)
            self.current_node = None

        self.updateInfo()

    def nextPeriodHandler(self):
        charts_list = list(self.charts_dict.values())
        self.dyn_data_manager.nextPeriod(charts_list)
        self.updateInfo()

    def prevPeriodHandler(self):
        charts_list = list(self.charts_dict.values())
        self.dyn_data_manager.prevPeriod(charts_list)
        self.updateInfo()

    def keyReleaseEvent(self, event):
        self.topology_painter.event(event)

    def updateInfo(self):
        final_msg = ""

        if self.error_msg!="":
            final_msg += self.error_msg

        if self.vascular_net is None or len(self.vascular_net)==0:
            final_msg += "Load topology data."
            self.Info.setText(final_msg)
            return 0

        name = self.top_path.split('/')[-1]
        self.top_info_string = name + "\nTotal nodes: " + str(len(self.vascular_net)) + "; "
        kn = sum(len(node.bonds) > 2 for node in self.vascular_net.values())
        term = sum(len(node.bonds) == 1 for node in self.vascular_net.values())
        self.top_info_string += "Knots: " + str(kn) + "; Ends: " + str(term) + "\n"

        bc_num = 0
        for node in self.vascular_net.values():
            if node.bc_par!=None:
                bc_num+=1

        final_msg += self.top_info_string

        final_msg += "\n"
        final_msg += "Parameters: "
        if self.par_path=="":
            final_msg+="No parameters loaded.\n"
        else:
            final_msg += self.par_path + "\n"
            final_msg += str(bc_num) + " outlet BC params loaded."
        final_msg += "\n"
        self.node_info_string=""
        if len(self.points_list)>0:
           # self.node_info_string += "Current node: "
           # self.node_info_string += str(self.current_node.id) + "\n"
            self.CurrentNode.setText(str(self.current_node.id))
            self.node_info_string += "Selected nodes: "

        for node in self.points_list:
            self.node_info_string+=str(node.id) + "; "
        self.node_info_string+="\n"
        final_msg+=self.node_info_string

        final_msg += "\n"
        final_msg += self.dyn_data_manager.getInfo()
        self.Info.setText(final_msg)

    def saveTopology(self, sel=None):
        fname = QFileDialog.getSaveFileName(self, 'Save topology and parameters files')[0]
        if fname == "":
            return None

        try :
            name = fname.split('.')[-2]
        except :
            name = fname

        fname_top = name +".top"
        fname_par = name + ".par"

        id_dict = IO.write_top_file(fname_top, self.vascular_net, sel)
        if self.par_path=="":
            return None
        if sel == None:
            IO.write_par_file(fname_par, self.vascular_net, id_dict)
        else:
            IO.write_par_file(fname_par, sel, id_dict)

    def showAgentValue(self, user_agent, max_agent):
        max_agent.setText(str(user_agent / 100))
        self.dyn_data_manager.max_agent_c = user_agent / 100
        self.dyn_data_manager.min_agent_c = 0.0

    def updatePeriod(self):
        charts_list = list(self.charts_dict.values())
        self.dyn_data_manager.period = self.periodSlider.value()
        self.dyn_data_manager.updateChartData(charts_list)
        self.updateInfo()

class DataManager:
    def __init__(self):
        self.data_dict = {}
        self.period_ids = np.array([])
        self.time_data  = np.array([])
        self.name = ""
        self.period = 0

        self.min_agent_c = None
        self.max_agent_c = None
        pass

    def update_agent_c_range(self): # Checks the min and max agent_c value of all nodes on a specific period of time
        min_value = None
        max_value = None

        for index in self.data_dict:
            node_data = self.data_dict[index]
            lb = self.period_ids[self.period]
            agent_c = node_data[2][lb]
            min_value = min(min_value if min_value else 1e9, agent_c)
            max_value = max(max_value if max_value else 0, agent_c)
        self.min_agent_c = min_value
        self.max_agent_c = max_value

    def density_for_item_at_index(self, index):
        lb = self.period_ids[self.period]
        data3 = self.data_dict[index][2]
        agent_c_range = self.max_agent_c - self.min_agent_c
        if agent_c_range > 0:
            return min(1, max(0, (data3[lb] - self.min_agent_c) / agent_c_range))
        else:
            return 0.01

    def isEmpty(self):
        if self.period_ids.size==0:
            return True
        return False

    def isDynamicsFile(self):
        return self.name.endswith('.dyn')

    def loadData(self, fname):
        self.period = 0
        self.unloadDynamics()
        self.name = fname.split('/')[-1]
        self.period_ids, self.time_data, self.data_dict = IO.read_dyn_file(fname)
        # self.update_agent_c_range()

        # ## To define global min and max agent
        # min_value = None
        # max_value = None
        #
        # for index in self.data_dict:
        #     node_data = self.data_dict[index]
        #     for agent_c_value in node_data[2]:
        #         min_value = min(min_value if min_value else 1e9, agent_c_value)
        #         max_value = max(max_value if max_value else 0, agent_c_value)
        # print("max : ", max_value)
        # print("min : ", min_value)
        # self.min_agent_c = min_value
        # self.max_agent_c = max_value

    def unloadDynamics(self):
        self.period_ids = np.array([])
        self.data_dict = {}
        self.time_data = np.array([])
        self.name = ""
        self.period = 0

    def updateChartData(self, chart_list):
        if self.period_ids.size==0:
            return None

        lb = self.period_ids[self.period]
        rb = len(self.time_data)
        if self.period + 1 < len(self.period_ids):
            rb = self.period_ids[self.period + 1]

        for c_wgt in chart_list:
            data1 = self.data_dict[int(c_wgt.objectName())][0]
            data2 = self.data_dict[int(c_wgt.objectName())][1]
            data3 = self.data_dict[int(c_wgt.objectName())][2]
            c_wgt.replaceData(self.time_data[lb:rb], data1[lb:rb], data2[lb:rb], data3[lb:rb])
        pass

    def getData(self, id):
        lb = self.period_ids[self.period]
        rb = len(self.time_data)
        if self.period + 1 < len(self.period_ids):
            rb = self.period_ids[self.period + 1]

        data1 = self.data_dict[id][0]
        data2 = self.data_dict[id][1]
        data3 = self.data_dict[id][2]

        return self.time_data[lb:rb], data1[lb:rb], data2[lb:rb], data3[lb:rb]

    def nextPeriod(self, chart_list):
        if self.isEmpty():
            return None
        self.period += 1
        if self.period >= len(self.period_ids) - 1:
            self.period -= 1
        self.updateChartData(chart_list)
        # self.update_agent_c_range()

    def prevPeriod(self, chart_list):
        if self.isEmpty():
            return None
        self.period -= 1
        if self.period < 0:
            self.period = 0
        self.updateChartData(chart_list)
        # self.update_agent_c_range()

    def getInfo(self):
        dyn_info_string = ""
        if self.isEmpty():
            dyn_info_string="No dynamic data.\n"
            return dyn_info_string

        lb = self.period_ids[self.period]
        rb = len(self.time_data) - 1
        if self.period + 1 < len(self.period_ids):
            rb = self.period_ids[self.period + 1]

        dyn_info_string += "Dynamics: " + self.name + ";\nTime: " + str(
            self.time_data[lb]) + " - " + str(self.time_data[rb]) + "; "
        dyn_info_string += "Periods: " + str(self.period) + "/" + str(len(self.period_ids)-2) + "\n"
        return dyn_info_string


