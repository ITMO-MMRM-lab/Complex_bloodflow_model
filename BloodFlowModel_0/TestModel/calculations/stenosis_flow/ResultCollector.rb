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

def main(path)		

	lines = []
	nodes = []
	
	Dir.chdir(path)
		Dir["*.out"].each {|a|
			f = open(a)		
			puts "!!!"
			tmp_string = f.read
			lines+= tmp_string.split("\n")		
			}		
	lines.each{|line|					
		foundSamples = line.scan(/^(\d+)\s+(-*\d+.\d+)\s+(.+)$/)	
		puts foundSamples[0]		
		if(foundSamples[0] != nil)																			
			node = Node.new(foundSamples[0][0], foundSamples[0][1])
			cases = foundSamples[0][2].split("\s")			
			cases.each{|cs| 			
			cs.strip!
			foundCases = cs.scan(/^(\d.\d+):(-*\d+.\d+)$/)
				node.addCase(foundCases[0][0].to_f,foundCases[0][1].to_f)				
			}			
			nodes << node			
		end		
	}
	
	output_string = ""
	nodes.each{|node|		
		output_string+=node.node_id+"\s"
		output_string+=node.ref_flux+"\s"
		node.depressed_flux.each{|d_p|
			output_string+=d_p[0].to_s+":"
			output_string+=d_p[1].to_s+"\s"
		}		
		output_string+="\n"
	}
	Dir.chdir("..\\")
	File.open("output.txt", 'w'){ |f| f.write(output_string) }
	
	#puts output_string
end

main("0_6_cutoff")