import random

from PyQt6.QtCore import *
from PyQt6.QtCore import QObject, pyqtSignal
from PyQt6.QtOpenGLWidgets import QOpenGLWidget
from PyQt6.QtWidgets import *

from OpenGL.GL import *
from OpenGL.GLU import *

import EventProcessing
import numpy as np

from matplotlib import pyplot as plt
import matplotlib
import numpy as np

class Topology3DPainter(QOpenGLWidget):
    dclk_signal = pyqtSignal(int, name='dclk_signal')

    def __init__(self, parent = None):
        QOpenGLWidget.__init__(self, parent)
        self.center_pos = np.zeros(3)
        self.max_vector = np.zeros(3)
        self.min_vector = np.zeros(3)
        self.vascular_net = None
        self.sec_vascular_net = None
        self.dyn_data_manager = None

        self.angle_x = 0
        self.angle_y = 0

        self.trans_x = 0
        self.trans_y = 0
        self.trans_z = 0

        self.zoom = 1

        self.event_processor = EventProcessing.Mouse3DParser(1, 1, 1)
        self.selected_nodes = []
        self.editable_nodes = []
        self.current_node    = None
        self.current_node_id = -1


    def __draw_cross__(self, pos, size = 0.01, color = (1.0, 1.0, 0.0)):
        glColor3f(color[0], color[1], color[2])

        glVertex3f(pos[0]+size, pos[1], pos[2])
        glVertex3f(pos[0]- size, pos[1], pos[2])
        glVertex3f(pos[0], pos[1]+size, pos[2])
        glVertex3f(pos[0], pos[1]-size, pos[2])
        glVertex3f(pos[0], pos[1], pos[2] + size)
        glVertex3f(pos[0], pos[1], pos[2] - size)

       # glEnd()
       # glColor3f(1.0, 1.0, 1.0)

    def takeScreenshot(self):
        qimage = self.grabFramebuffer()
        qimage.save("fileName.png", "PNG")

    def paintGL(self):
        if self.vascular_net==None:
            return None
        # glClearColor(1.0, 1.0, 1.0, 1.0)  # White background for screenshots
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT)
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA)
        glEnable(GL_BLEND)

        glColor4f(1, 1, 1, 1)  # glColor4f(0, 0, 0, 0.4)
        glBegin(GL_LINES)
        for node in self.vascular_net.values():
            if self.dyn_data_manager is not None:
                density = self.dyn_data_manager.density_for_item_at_index(node.id)
                density = max(0.1, min(1, density))
                # glColor4f(0 + density, 0, 0, 0.3 + density) # White background for screenshots
                glColor4f(1, 1 - density, 1 - density, 1)

            for ngb in node.bonds:
                nnode = self.vascular_net[ngb]
                glVertex3f(node.pos[0], node.pos[1], node.pos[2])
                glVertex3f(nnode.pos[0], nnode.pos[1], nnode.pos[2])

        if self.sec_vascular_net!=None:
            glColor4f(1.0, 0.0, 0.5, 0.25)
            for node in self.sec_vascular_net.values():
                for ngb in node.bonds:
                    nnode = self.sec_vascular_net[ngb]
                    glVertex3f(node.pos[0]+1e-3, node.pos[1], node.pos[2])
                    glVertex3f(nnode.pos[0]+1e-3, nnode.pos[1], nnode.pos[2])
        glEnd()

        glColor3f(1.0, 1.0, 0.0)
        glBegin(GL_LINES)
        for node in self.editable_nodes:
            self.__draw_cross__(node.pos, 0.005, (1.0, 1.0, 0.0))
        glEnd()

        glBegin(GL_LINES)
        for node in self.selected_nodes:
            if node.id == self.current_node_id:
                self.__draw_cross__(node.pos, 0.02, (1.0, 0.0, 0.0))
            else:
                self.__draw_cross__(node.pos)
        glEnd()

        #if self.current_node!=None:
         #   self.__draw_cross__(self.current_node.pos, 0.02, (1.0, 0.0, 0.0))

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

    def loadObject(self, vasculat_net, selected_nodes_arr, editbale_nodes_arr):
        self.vascular_net = vasculat_net
        self.selected_nodes = selected_nodes_arr
        self.editable_nodes = editbale_nodes_arr
        self.__calcSceneSize()

    def loadSecObject(self, vasculat_net):
        self.sec_vascular_net = vasculat_net

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

        for node in self.vascular_net.values():
            center_pos[:] = center_pos[:] + node.pos[:]

        center_pos = center_pos / len(self.vascular_net)

        for node in self.vascular_net.values():
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

    def mousePressEvent(self, event):
        self.event_processor.clk((event.position().x(), event.position().y()))
        pass

    def mouseMoveEvent(self, event):
        if event.buttons() == Qt.MouseButton.LeftButton:
            self.angle_x, self.angle_y, self.trans_x, self.trans_y = self.event_processor.getGasture((event.position().x(), event.position().y()))
        pass

    def wheelEvent(self, event):
        self.trans_z, self.zoom = self.event_processor.transz(np.sign(event.angleDelta().y()))

    def keyPressEvent(self, event):
        if event.key()==Qt.Key.Key_Control.value:
            self.event_processor.cntrl_press()

    def keyReleaseEvent(self, event):
        if event.key()==Qt.Key.Key_Control.value:
            self.event_processor.cntrl_rel()
        pass

    def mouseDoubleClickEvent(self, event):
        print((event.position().x(), event.position().y()))

        clk_x = event.position().x()
        clk_y = (self.size.height() - event.position().y())

        min_i = 0

        def delta(x):
            screen_c = gluProject(x.pos[0], x.pos[1], x.pos[2], self.modelview, self.projection, self.viewport)
            return np.linalg.norm(np.asarray([clk_x - screen_c[0], clk_y - screen_c[1]]))

        min_i = np.asarray([delta(node) for node in self.vascular_net.values()]).argmin()

        if delta(self.vascular_net[min_i])<50:
            print(min_i)
            self.current_node = self.vascular_net[min_i]
            self.current_node_id = len(self.selected_nodes)-1

        self.dclk_signal.emit(min_i)

    def selectNode(self, id):
        self.current_node_id = id

    def deselectNode(self):
        if self.current_node==None:
            return None

        sel_id = self.selected_nodes.index(self.current_node)
        self.selected_nodes.remove(self.current_node)
        if len(self.selected_nodes)==0:
            self.current_node = None
        else:
            sel_id = sel_id % len(self.selected_nodes)
            self.current_node = self.selected_nodes[sel_id]

    def resizeEvent(self, event):
        self.size = event.size()
        pass

    def getCurrentNode(self):
        return self.current_node



    '''
            if event.key() == Qt.Key_Right or event.key() == Qt.Key_Left:
            if event.key() == Qt.Key_Right:
                self.current_node_id+=1
            if event.key() == Qt.Key_Left:
                self.current_node_id-=1
            self.current_node_id = self.current_node_id % len(self.selected_nodes)
            self.current_node = self.selected_nodes[self.current_node_id]
            self.dclk_signal.emit(self.current_node.id)
    '''