import IOModule as IO
import re

def readMapFile(filename):
    file = open(filename, 'r')

    map_dict = {}
    for line in file:
        result = re.match(r'^(\d+)\t(\d+)$', line)
        if result != None:
            key_id = int(result.group(1))
            map_dict[key_id] = int(result.group(2))
            continue

    return map_dict


print ("Hello!")

net1 = IO.read_top_file("0_6_cutoff_CoW.top")
net2 = IO.read_top_file("0_2_cutoff.top")
map_dict  = readMapFile("0_6_to_0_2_map.map")

for n_id in net1:
    n = net1[n_id]
    nn = net2[map_dict[n_id]]
    if (n.radius - nn.radius)>1e-7:
        print("!!!\n")


