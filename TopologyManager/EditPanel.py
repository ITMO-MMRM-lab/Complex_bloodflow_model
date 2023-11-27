from PyQt6.QtWidgets import *
from PyQt6 import QtGui
from PyQt6.QtWidgets import QScrollArea
from PyQt6.uic import *
from Plot import *

import IOModule as IO
import TopologyPainter as TP

import numpy as np

class EditPanel(QObject):
    def __init__(self, editTab):
        self.selection = []
        self.frozen_selzection = []
        self.vasculat_net = None

        self.pointSelectionRButton = editTab.findChild(QRadioButton, str('pointSelectRButton'))
        self.threadSelectionRButton = editTab.findChild(QRadioButton, str('threadSelectRButton'))
        self.partSelectionRButton = editTab.findChild(QRadioButton, str('partSelectRButton'))

        self.pointSelectionRButton.setChecked(True)

        self.saveTopyButton = editTab.findChild(QPushButton, str('SaveTopologyButton'))
        self.invButton = editTab.findChild(QPushButton, str('InvertButton'))
        self.connectButton = editTab.findChild(QPushButton, str('ConnectButton'))
        self.delButton = editTab.findChild(QPushButton, str('DeleteButton'))
        self.cleanButton = editTab.findChild(QPushButton, str('CleanSelectionButton'))
        self.remConnectionButton = editTab.findChild(QPushButton, str('RemConnectionButton'))

        self.pointSelectionRButton.toggled.connect(lambda: self.buttonManager())
        self.threadSelectionRButton.toggled.connect(lambda: self.buttonManager())
        self.partSelectionRButton.toggled.connect(lambda: self.buttonManager())

        self.delButton.clicked.connect(lambda: self.removeNodes())
        self.invButton.clicked.connect(lambda: self.invSelection())
        self.connectButton.clicked.connect(lambda: self.conncetNodes())
        self.remConnectionButton.clicked.connect(lambda: self.disconncetNodes())
        self.cleanButton.clicked.connect(lambda: self.cleanSelection())

        if self.pointSelectionRButton.isChecked():
            self.selectHandler = [self.pointSelectManager]

        if self.threadSelectionRButton.isChecked():
           self.selectHandler = [self.threadSelectManager]

        if self.partSelectionRButton.isChecked():
            self.selectHandler = [self.partSelectManager]

        self.buttonManager()

    def buttonManager(self):
        self.saveTopyButton.setEnabled(False)
        self.invButton.setEnabled(False)
        self.connectButton.setEnabled(False)
        self.delButton.setEnabled(False)
        self.cleanButton.setEnabled(False)
        self.remConnectionButton.setEnabled(False)

        if self.pointSelectionRButton.isChecked():
            self.selectHandler[0] = self.pointSelectManager

        if self.threadSelectionRButton.isChecked():
            self.selectHandler[0] = self.threadSelectManager

        if self.partSelectionRButton.isChecked():
            self.selectHandler[0] = self.partSelectManager

        if len(self.selection)>0:
            self.invButton.setEnabled(True)
            self.delButton.setEnabled(True)
            self.cleanButton.setEnabled(True)

        if len(self.selection)==2:
            self.connectButton.setEnabled(True)
            if self.selection[1] in self.selection[0].bonds :
                self.remConnectionButton.setEnabled(True)

    def selectSlot(self, id):
        add = QGuiApplication.queryKeyboardModifiers()==Qt.ControlModifier
        if not(add):
            self.selection.clear()

        self.selectHandler[0](id)
        self.buttonManager()

    def pointSelectManager(self, id):
        n = self.vasculat_net[id]
        if n in self.selection:
            self.selection.remove(n)
        else:
            self.selection.append(n)

    def threadSelectManager(self, id):
        node_trash = []
        start_node = self.vasculat_net[id]
        select_list = self.wideSearch(start_node, lambda x: x.bonds.size>2)
        for n in select_list:
            if n in self.selection:
                self.selection.remove(n)
                node_trash.append(n)
            else:
                self.selection.append(n)

        for n in node_trash:
            if n.bonds.size>2 and not(n in self.selection):
                sel_nght = 0
                for nn_id in n.bonds:
                    nn = self.vasculat_net[nn_id]
                    sel_nght+=int(nn in self.selection)
                if sel_nght>=2:
                    self.selection.append(n)


    def partSelectManager(self, id):
        start_node = self.vasculat_net[id]
        select_list = self.wideSearch(start_node)
        for n in select_list:
            if n in self.selection:
                self.selection.remove(n)
            else:
                self.selection.append(n)

    def wideSearch(self, start, rule = lambda x: False):
        node_set = set()
        past_front = set()
        past_front.add(start)
        curr_front = set()

        if rule(start):
            return []

        while True:
            for n in past_front:
                node_set.add(n)
                if rule(n):
                    continue
                for nn_id in n.bonds:
                    nn = self.vasculat_net[nn_id]
                    if not(nn in node_set):
                        curr_front.add(nn)
            if len(curr_front)==0:
                break
            past_front = set(curr_front)
            curr_front.clear()
        return list(node_set)

    def invSelection(self):
        inv_selection_list = self.vasculat_net.copy()

        for n in self.selection:
            del inv_selection_list[n.id]

        self.selection.clear()
        for n in inv_selection_list.values():
            self.selection.append(n)
        pass

    def hideSelection(self):
        self.frozen_selzection = self.selection.copy()
        self.selection.clear()

    def showSelection(self):
        for n in self.frozen_selzection:
            self.selzection.append(n)

    def removeNodes(self):
        for n in self.selection:
            n_bonds = self.vasculat_net[n.id].bonds
            for nn_id in n_bonds:
                nn = self.vasculat_net[nn_id]
                rem_i = np.where(nn.bonds==n.id)
                nn.bonds = np.delete(nn.bonds, rem_i)

            del self.vasculat_net[n.id]

        self.selection.clear()
        pass

    def conncetNodes(self):
        if len(self.selection)!=2:
            return None

        self.selection[0].bonds = np.append(self.selection[0].bonds, self.selection[1].id)
        self.selection[1].bonds = np.append(self.selection[1].bonds, self.selection[0].id)
        pass

    def cleanSelection(self):
        self.selection.clear()

    def clearAll(self):
        self.selection.clear()
        self.frozen_selzection = []
        self.vasculat_net = None
        self.buttonManager()

    def disconncetNodes(self):
        if len(self.selection) != 2:
            return None

        rem_i1 = np.where(self.selection[0].bonds==self.selection[1].id)
        rem_i2 = np.where(self.selection[1].bonds == self.selection[0].id)

        np.delete(self.selection[0].bonds, rem_i1)
        np.delete(self.selection[1].bonds, rem_i2)
        pass