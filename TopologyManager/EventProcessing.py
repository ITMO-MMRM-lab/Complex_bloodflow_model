import numpy  as np

class Mouse3DParser:
    def __init__(self, rot_speed, trns_speed, zoom_speed):
        self.cntrl = False
        self.trans_x = 0
        self.trans_y = 0
        self.trans_z = 0

        self.rot_x = 0
        self.rot_y = 0

        self.zoom = 1

        self.rot_speed  = rot_speed
        self.trns_speed = trns_speed
        self.zoom_speed = zoom_speed

        self.clk_x, self.clk_y = 0, 0

    def clk(self, pos):
        self.clk_x, self.clk_y = pos[0], pos[1]

    def cntrl_press(self):
        self.cntrl = True

    def cntrl_rel(self):
        self.cntrl = False

    def transz(self, wheel):
        if self.cntrl:
            self.trans_z += 0.05*self.trns_speed*wheel
        else:
            self.zoom -= 0.02*self.zoom_speed*wheel
            if self.zoom < 0.1:
                self.zoom = 0.1

        return self.trans_z, self.zoom

    def getGasture(self, pos):
        delta = np.array([self.clk_x - pos[0], self.clk_y - pos[1]])

        self.clk_x = pos[0]
        self.clk_y = pos[1]

        if self.cntrl:
            self.trans_x += delta[0] * 1e-3 * self.trns_speed
            self.trans_y += delta[1] * 1e-3 * self.trns_speed
        else:
            self.rot_x += delta[0] * self.rot_speed
            self.rot_y += delta[1] * self.rot_speed

        return self.rot_x, self.rot_y, self.trans_x, self.trans_y