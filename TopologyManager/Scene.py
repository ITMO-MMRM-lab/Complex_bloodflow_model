from OpenGL.GL import *
from OpenGL.GLU import *
import numpy as np
import EventProcessing
import pygame as pg

class Scene:
    def __init__(self, x_root, y_root, x_size, y_size):
        self.aspectRatio = x_size / y_size;
        pass

    def on_update(self):
        raise NotImplementedError("on_update abstract method must be defined in subclass.")

    def on_event(self, event, keys):
        raise NotImplementedError("on_event abstract method must be defined in subclass.")

    def on_draw(self, screen):
        raise NotImplementedError("on_draw abstract method must be defined in subclass.")

class TopologyScene(Scene):
    def __init__(self, x_root, y_root, x_size, y_size):
        Scene.__init__(self, x_root, y_root, x_size, y_size)
        glViewport(x_root, y_root, x_size, y_size)

        glMatrixMode(GL_PROJECTION)
        glLoadIdentity()

        glEnable(GL_DEPTH_TEST)
        glDepthFunc(GL_LESS)

        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT)

        self.center_pos = np.zeros(3)
        self.max_vector = np.zeros(3)
        self.min_vector = np.zeros(3)

        self.mouse_parser = EventProcessing.Mouse3DParser(1,1,1)

        self.event_processor = EventProcessing.Mouse3DParser(1, 1, 1)

    def loadObject(self, vasculat_net):
        self.vascular_net = vasculat_net
        self.__calcSceneSize()

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
        self.diagonal_size = delta
        self.up = up

    def on_update(self):
        pass

    def on_event(self, event, keys):
        if event.type == pg.MOUSEBUTTONDOWN:
            if event.button == 4:
                self.mouse_parser.transz_in(keys[pg.K_LCTRL])
            if event.button == 5:
                self.mouse_parser.transz_out(keys[pg.K_LCTRL])


            if pg.key.get_pressed()[pg.K_LCTRL]:
                self.mouse_parser.trans_clk(pg.mouse.get_pos())

            else:
                self.mouse_parser.rot_clk(pg.mouse.get_pos())

        if event.type == pg.MOUSEBUTTONUP:
            self.mouse_parser.rot_rel()
            self.mouse_parser.trans_rel()

        if event.type == pg.KEYDOWN and event.key == pg.K_LCTRL:
            self.mouse_parser.trans_clk(pg.mouse.get_pos())

        if event.type == pg.KEYUP and event.key == pg.K_LCTRL:
            self.mouse_parser.trans_rel()
            self.mouse_parser.rot_clk(pg.mouse.get_pos())
        pass

    def on_draw(self):
        glBegin(GL_LINES)

        for node in self.vascular_net:
            for ngb in node.bonds:
                nnode = self.vascular_net[ngb]
                glVertex3f(node.pos[0], node.pos[1], node.pos[2])
                glVertex3f(nnode.pos[0], nnode.pos[1], nnode.pos[2])
        glEnd()

    def on_update(self):
        angle_x, angle_y, pos_x, pos_y, pos_z, zoom = self.mouse_parser.getGasture(pg.mouse.get_pos())

       # print(pos_x)

        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT)
        glLoadIdentity();
        gluPerspective(60 * zoom, self.aspectRatio, 0.1, 100.0)

        glTranslatef(-pos_x, pos_y, pos_z)
        gluLookAt(-2, 0, 0, 0, 0, 0, 0, 1, 0)
        glRotatef(angle_x, 0, -1, 0)
        glRotatef(angle_y, -1, 0, 0)
        glTranslatef(-self.center_pos[0], -self.center_pos[1], -self.center_pos[2])
        pass

class PlotScene(Scene):
    def __init__(self, x_root, y_root, x_size, y_size):
        Scene.__init__(self, x_root, y_root, x_size, y_size)
        glViewport(x_root, y_root, x_size, y_size)
        #gluOrtho2D(x_root, y_root, x_size, y_size)
        #glLoadIdentity()
        #glClearColor(0., 0., 0., 1.)
    def on_update(self):
        pass

    def on_event(self, event, keys):
        pass

    def on_draw(self):
        #glClearColor(1.0, 0.0, 0.0)
        #glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT)
        pass