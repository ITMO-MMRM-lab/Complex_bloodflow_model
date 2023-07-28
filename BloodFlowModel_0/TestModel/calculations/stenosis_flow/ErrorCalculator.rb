class Node
	attr_accessor :node_id, :ref_flux, :depressed_flux	
	
	def initialize(node_id, ref_flux) 
		@node_id  = node_id
		@ref_flux = ref_flux
		@depressed_flux = []
	end 
	
	def addCase(degree, flux)
		@depressed_flux << [degree, flux]
	end	
end

def main(path_f, path_c, path_map)		
	lines = []	
	nodes_fine 	 = []
	nodes_coarse = []

	f = open (path_c)
	lines = f.read
	
	lines = lines.split("\n")	
	lines.each{ |line|
		line.strip!		
		foundSamples = line.scan(/^(\d+)\s+(-*\d+.\d+)\s+(.+)$/)
		if(foundSamples[0] != nil)
			node = Node.new(foundSamples[0][0].to_i, foundSamples[0][1].to_f)
			cases = foundSamples[0][2].split("\s")						
			cases.each{|cs| 			
				cs.strip!
				foundCases = cs.scan(/^(\d.\d+):(-*\d+.\d+)$/)
				node.addCase(foundCases[0][0].to_f,foundCases[0][1].to_f)				
			}			
			nodes_coarse << node			
			end
			}
			
	nodes_coarse.sort!{|x,y| x.node_id<=>y.node_id}
			
	f = open (path_f)
	lines = f.read
	
	lines = lines.split("\n")	
	lines.each{ |line|
		line.strip!		
		foundSamples = line.scan(/^(\d+)\s+(-*\d+.\d+)\s+(.+)$/)
			if(foundSamples[0] != nil)
			node = Node.new(foundSamples[0][0].to_i, foundSamples[0][1].to_f)			
			cases = foundSamples[0][2].split("\s")			
			cases.each{|cs| 			
				cs.strip!
				foundCases = cs.scan(/^(\d.\d+):(-*\d+.\d+)$/)
				node.addCase(foundCases[0][0].to_f,foundCases[0][1].to_f)								
			}			
			nodes_fine << node			
			end
			}
	
			
	map = Hash.new	
	f = open (path_map)
	lines = f.read	
	lines = lines.split("\n")	
	lines.each{ |line|
		line.strip!		
		foundSamples = line.scan(/^(\d+)\t+(\d+)$/)		
		if(foundSamples[0] != nil)
			map[foundSamples[0][0].to_i]=foundSamples[0][1].to_i			
		end
		}
		
	output_string = ""
	nodes_coarse.each{ |node|
		fine_corr_id = map[node.node_id]				
		fine_corr_node = nodes_fine.detect{|n| n.node_id.to_i==fine_corr_id.to_i}		
		if(fine_corr_node!=nil)		
			output_string += "Coarse:\t" + node.node_id.to_s + "\t" + node.ref_flux.to_s + "\t"
				node.depressed_flux.each{|d_p|
				output_string+=d_p[0].to_s+":\t"
				output_string+=d_p[1].to_s+"\t"
			}		
			output_string += "Fine:\t" + fine_corr_node.node_id.to_s + "\t" + fine_corr_node.ref_flux.to_s + "\t"
				fine_corr_node.depressed_flux.each{|d_p|
				output_string+=d_p[0].to_s+":\t"
				output_string+=d_p[1].to_s+"\t"
			}		
			output_string+="\n"
		end
	}
	File.open("Medium_Hight_corr.txt", 'w'){ |f| f.write(output_string) }
end

main("0_2_cutoff\\Hight_all.out", "0_6_cutoff\\Medium_all.out", "0_6_cutoff\\0_6_to_0_2_map.map")