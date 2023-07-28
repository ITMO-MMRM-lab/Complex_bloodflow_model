import re
import numpy as np
import os

class Node:
    def __init__(self, id, pos, radius, bonds = []):
        self.id = id
        self.pos = np.array(pos)
        self.radius = radius
        self.bonds = np.array(bonds)
        self.bc_par = None
        self.attribute = None

    def setData(self, y_data1, y_data2=None):
        self.y_data1 = y_data1
        if y_data2!=None:
            self.y_data2 = y_data2

    def getData1(self):
        if self.y_data1 == None:
            return None
        return np.array(self.y_data1)

    def getData2(self):
        if self.y_data2 == None:
            return None
        return np.array(self.y_data2)

    def setBonds(self, bonds):
        self.bonds = np.array(bonds)[1:]

states = ("RNAME", "RPOS", "RBONDS")

def set_top_template(state):
    if state == "RNAME":
        return (r'^Name: (\w+)$')
    if state == "RPOS":
        return (r'^\s*(\d+)\s+X:(-*\d+.\d+)\s+Y:(-*\d+.\d+)\s+Z:(-*\d+.\d+)\s+R:(-*\d+.\d+)\s+C:(\d+.\d+)$')
    if state == "RBONDS":
        return (r'(\d+)\s*')

def switch_state(line, state):
    result = re.match(r'Coordinates:\s*$', line)
    if result!=None:
        return  "RPOS"
    result = re.match(r'Bonds:\s*$', line)
    if result!=None:
        return  "RBONDS"
    return state

def read_top_file(filename):

    system_name = ""
    error = ""

    file = open(filename, 'r')

    c_state = states[0]

    positions = []
    ids = []
    radii = []
    bonds = []

    all_nodes = {}

    for line in file:

        c_state = switch_state(line, c_state)
        template = set_top_template(c_state)

        if c_state == "RNAME":
            result = re.match(template, line)
            if result==None:
                continue
            system_name = result.group(1)

        if c_state == "RPOS":
            result = re.match(template, line)
            if result==None:
                continue
            id = int(result.group(1))
            ids.append(id)
            position = [float(result.group(2)), float(result.group(3)), float(result.group(4))]
            radius = float(result.group(5))
            all_nodes[id] = Node(id, position, radius)

        if c_state == "RBONDS":
            result = re.findall(template, line)
            if len(result)==0:
                continue
            bonds.append(list(map(int,result)))

  #  all_nodes = sorted(all_nodes, key=lambda x: x.id)

    id_dict = dict(zip(ids, range(len(ids))))

    for b in bonds:
        id = id_dict[b[0]]
        array_bonds = list(map(lambda x: id_dict[x], b))
        all_nodes[id].setBonds(array_bonds)

    return all_nodes

def read_par_file(filename):
    file = open(filename, 'r')
    pars = {}
    for line in file:
        result = re.match(r'^(\d+)\s+R1:(\s*\d+.\d+)\s+R2:(\s*\d+.\d+)\s+C:(\s*\d+.\d+)$', line)
        if result != None:
            pars[int(result.group(1))] = (float(result.group(2)), float(result.group(3)), float(result.group(4)))

    return pars

def read_dyn_file(filename):
    file = None
    if os.stat(filename).st_size == 0:
        return (np.array([]), np.array([]), {})
    else:
        file = open(filename, 'r')
    data_dict = {}
    prtiods_ids = []
    time_data = []
    wall_time = 0
    for line in file:
        result = re.match(r'^(\d+)\s+(-*\d+.\d+)\s+(-*\d+.\d+)\s+(-*\d+.\d+)\s+(-*\d+.\d+)$', line)
        if result != None:
            pers_id = int(result.group(1))
            data_y1 = float(result.group(2))
            data_y2 = float(result.group(3))/1000.0
            data_y3 = float(result.group(5))    # Концентрация контрастного вещества

            if data_dict.get(pers_id) is not None:
                data_dict[pers_id][0].append(data_y1)
                data_dict[pers_id][1].append(data_y2)
                data_dict[pers_id][2].append(data_y3)
            else:
                data_dict[pers_id] = [[data_y1], [data_y2], [data_y3]]
            continue

        result = re.match(r'^WT:\s+(\d+.*\d*)', line)
        if result is None:
            continue
        p_wall_time = wall_time
        wall_time = float(result.group(1))
        # if np.trunc(p_wall_time * 10) != np.trunc(wall_time * 10): # CHECK HERE
        if np.trunc(p_wall_time) != np.trunc(wall_time):
            prtiods_ids.append(len(time_data))
        time_data.append(wall_time)

    time_data = np.array(time_data)
    array_data_dicr = {}
    for k in data_dict.keys():
        data_dict[k][0] = np.array(data_dict[k][0])
        data_dict[k][1] = np.array(data_dict[k][1])
        data_dict[k][2] = np.array(data_dict[k][2])

    prtiods_ids = np.array(prtiods_ids)

    return (prtiods_ids, time_data, data_dict)

def write_par_file(filename, v_net, id_dict):
    out_file = open(filename, 'w', newline="\n")
    arr = []
    if type(v_net) is dict:
        arr = list(v_net.values())
    else:
        arr = list(v_net)

    for p in arr:
        if p.bc_par==None:
            continue
        bc = p.bc_par
        out_str = str(id_dict[p.id]) + " R1:{0:.5f}".format(bc[0]) + " R2:{0:.5f} ".format(bc[1]) + " C:{0:.5f}".format(bc[2]) + "\n"
        out_file.write(out_str)
    out_file.close()

def write_top_file(filename, v_net, sel=None):
    out_file = open(filename, 'w', newline="\n")
    sel_set = None
    id_dict = {}

    if sel!=None:
        sel_set = set(sel)
        node_list = sorted(sel, key = lambda x: x.id)
    else:
        node_list = list(v_net.values())

    out_str = "Name: System_0\n"
    out_str += "Coordinates:\n"
    out_file.write(out_str)
    inc_id = 0
    for node in node_list:
        id_dict[node.id] = inc_id
        out_str=str(inc_id) + " X:{0:.5f}".format(node.pos[0]) + " Y:{0:.5f}".format(node.pos[1]) + " Z:{0:.5f}".format(node.pos[2])+ " R:{0:.3f}".format(node.radius) + " C:0.000000\n"
        out_file.write(out_str)
        inc_id +=1

    out_str = "\nBonds:\n"
    out_file.write(out_str)
    out_str = ""
    for node in node_list:
        out_str+=str(id_dict[node.id])
        for nn_id in node.bonds:
            nn = v_net[nn_id]
            if sel==None or nn in sel_set:
                out_str += " "+str(id_dict[nn_id])
        out_str += "\n"
        out_file.write(out_str)
        out_str = ""

    out_file.close()

    return id_dict