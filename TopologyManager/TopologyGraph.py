import numpy as np
import networkx as nx

class Thread:
    def __init__(self, protothread):
        self.nodes = np.array(protothread)
        RT = 0
        CT = 0
        pass

def BoileauBeta(R):
    R = R*1e-3
    h_a = 0.2802
    h_b = -505.3 #m^-1
    h_c = 0.1324
    h_d = -11.14 #m^-1
    y_m = 225e3; #YOUNG_MODULUS
    w_t = R * (h_a * np.exp(h_b * R) + h_c * np.exp(h_d * R))
    return 4.0 / 3.0 * np.sqrt(np.pi) * y_m * w_t

class ArterialPart:
    def __init__(self, p_net, nodes, inlets, outlets):
        self.inlets  = inlets.copy()
        self.fluxes  = {}
        self.outlets = outlets.copy()
        self.nodes = nodes.copy()
        self.main_graph = nx.Graph()
        self.parent_net = p_net
        self.R = None
        self.C = None

    def calcSurrogateBC(self, data_manager, beta_func=BoileauBeta, density=1050):
        if data_manager.isEmpty():
            for inlt in self.inlets:
                inlt.bc_par = (1.0, 0.0, 0.0)
            return None
        all_tot_flux = 0

        if self.C==0 and self.R==0:
            for inlt in self.inlets:
                inlt.bc_par = None
            return None

        for inlt in self.inlets:
            t, f, p, agent_c = data_manager.getData(inlt.id) #s ml/s kPa
            n = t.size
            tot_flux = abs(np.sum((t[1:n] - t[0:n-1])*(f[0:n-1]+f[1:n])/2.0))/np.sum((t[1:n] - t[0:n-1])) #ml/s
            av_pressure = np.sum((t[1:n] - t[0:n-1])*(p[0:n-1]+p[1:n])/2.0) / np.sum(t[1:n] - t[0:n-1]) #kPa
            A = inlt.radius**2 * np.pi * 1e-6 #SI
            R1 = np.sqrt(0.5*beta_func(inlt.radius)*density/A/A/np.sqrt(A)) #SI
            inlt.bc_par = (R1, (av_pressure*1e3)/(tot_flux*1e-6)  - R1, tot_flux) #SI
            all_tot_flux+=tot_flux

        for inlt in self.inlets:
            if self.C:
                inlt.bc_par = (inlt.bc_par[0]*1e-9, inlt.bc_par[1]*1e-9, inlt.bc_par[2]/all_tot_flux*self.C*1e3)
            else:
                inlt.bc_par = (inlt.bc_par[0] * 1e-9, inlt.bc_par[1] * 1e-9, 0)

       #     print ("R1: " + str(inlt.bc_par[0]*1e-9) + " R2: " + str(inlt.bc_par[1]*1e-9) + " C: " + str(inlt.bc_par[2]*1e3))



    def parseNet(self, beta_func=BoileauBeta, vel_coeff=9, visc = 3.5*1e-3):
        for n in self.nodes:
            n.attribute = None

        edge_c = {}
        edge_r = {}

        curr_edge2knt = {}
        new_edge2knt = {}
        for inlt in self.inlets:
            inlt.attribute = 0
            for nn_id in inlt.bonds:
                nn = self.parent_net[nn_id]
                if not(nn in self.nodes):
                    nn.attribute = -1
            curr_edge2knt[inlt] = inlt
            edge_c[inlt] = 0
            edge_r[inlt] = 0
            self.main_graph.add_node(inlt)


        search_count = 0
        while (len(curr_edge2knt)!=0):
            for n in curr_edge2knt.keys():
                for nn_id in n.bonds:
                    nn = self.parent_net[nn_id]
                    if nn == curr_edge2knt[n]:
                        continue

                    if ((nn.attribute is None) or (nn.attribute>n.attribute+1) or ((nn.bonds.size > 2) and nn.attribute>0)):

                        edge_c[nn] = edge_c[n] + (0.25/beta_func(nn.radius) + 0.25/beta_func(n.radius))*0.5*(np.sqrt(np.pi**3)*(nn.radius**3)+np.sqrt(np.pi**3)*(n.radius**3))*np.linalg.norm(nn.pos - n.pos) # 1e-9 - SI scale coeff
                        edge_r[nn] = edge_r[n] + (vel_coeff+2)*visc*(1.0/np.pi/nn.radius**4 + 1.0/np.pi/n.radius**4)*np.linalg.norm(nn.pos - n.pos) # 1e12 - SI scale coeff.

                    if ((nn.attribute is None) or (nn.attribute>n.attribute+1)):
                        nn.attribute = search_count+1
                        new_edge2knt[nn] = curr_edge2knt[n]
                        #edge_c[nn] = edge_c[n]+1.0
                        #edge_r[nn] = edge_r[n]+1.0

                    if (nn.bonds.size > 2) and nn.attribute>0:
                        #edge_c[nn] = edge_c[n]+1.0
                        #edge_r[nn] = edge_r[n]+1.0

                        if not (self.main_graph.has_node(nn)):
                            self.main_graph.add_node(nn)

                        if not (self.main_graph.has_edge(curr_edge2knt[n], nn)):
                            self.main_graph.add_edge(curr_edge2knt[n], nn, R=edge_r[nn], C = edge_c[nn])

                        edge_c[nn] = 0
                        edge_r[nn] = 0
                        new_edge2knt[nn] = nn

                if n.bonds.size==1:
                    if not (self.main_graph.has_node(n)):
                        self.main_graph.add_node(n)
                    if not (self.main_graph.has_edge(curr_edge2knt[n], n)) and not (curr_edge2knt[n] == n):
                        if n.bc_par:
                            edge_c[n] += n.bc_par[2]*1e-3
                            edge_r[n] += (n.bc_par[0] + n.bc_par[1])*1e-3
                        self.main_graph.add_edge(curr_edge2knt[n], n, R=edge_r[n], C = edge_c[n])

            search_count = search_count+1
            curr_edge2knt = new_edge2knt.copy()
            new_edge2knt.clear()

        pass

    def calculateResistance(self):
        for n in self.nodes:
            n.attribute = 0

        for n in self.inlets:
            n.attribute = 1.0

        for n in self.outlets:
            n.attribute = 0.0

        max_diff = 1.0
        while(max_diff>1e-5):
            max_diff = 0.0
            for gn in self.main_graph.nodes:
                if gn in self.inlets:
                    continue
                if gn in self.outlets:
                    continue
                numerator = 0
                denumerator = 0
                for gnn in self.main_graph.neighbors(gn):
                    numerator+=1.0/self.main_graph.get_edge_data(gn, gnn)['R']*gnn.attribute
                    denumerator+=1.0/self.main_graph.get_edge_data(gn, gnn)['R']
                diff = abs(gn.attribute - numerator/denumerator)
                gn.attribute = numerator / denumerator
                if diff>max_diff:
                    max_diff = diff

        I_tot = 0
        for gn in self.outlets:
            for gnn in self.main_graph.neighbors(gn):
                I_tot+=abs((gn.attribute - gnn.attribute)/self.main_graph.get_edge_data(gn, gnn)['R'])
        if I_tot == 0:
            R_tot = 0
        else:
            R_tot = 1/I_tot

        self.R = R_tot
        self.C = np.sum(list(nx.get_edge_attributes(self.main_graph, 'C').values()))








