def main(target_num, dynamics_med, error_list_med, map_coarse, map_med)		
	lines = []		
	fine_index_coarse = Hash.new
	fine_index_coarse_inv = Hash.new
	fine_index_med    = Hash.new 		
	
	med_to_coarse    = Hash.new 	
	rms_dynamics = Hash.new

	f = open (map_coarse)
	lines = f.read
	
	lines = lines.split("\n")	
	lines.each{ |line|
		line.strip!		
		foundSamples = line.scan(/^(\d+)\t+(\d+)$/)
			if(foundSamples[0] != nil)
			fine_index_coarse_inv[foundSamples[0][0].to_i] = foundSamples[0][1].to_i
			end
		}			
		
	
	f = open (target_num)	
	lines = f.read
	
	lines = lines.split("\n")	
	lines.each{ |line|
		line.strip!		
		foundSamples = line.scan(/^(\d+)$/)			
			if(foundSamples[0] != nil)
				fine_index_coarse[fine_index_coarse_inv[foundSamples[0][0].to_i]] = foundSamples[0][0].to_i			
				puts fine_index_coarse_inv[foundSamples[0][0].to_i].to_s + "=" + foundSamples[0][0].to_s	
			end
		}
	
	f = open (map_med)
	lines = f.read
	
	lines = lines.split("\n")	
	lines.each{ |line|
		line.strip!		
		foundSamples = line.scan(/^(\d+)\t+(\d+)$/)
			if(foundSamples[0] != nil)
			fine_index_med[foundSamples[0][1].to_i] = foundSamples[0][0].to_i			
			end
		}		
		
	fine_index_coarse.each_key{
		|fine_index|
		med_to_coarse[fine_index_med[fine_index]] = fine_index_coarse[fine_index]		
	}
	
	
	
	
	output_string = ""
	f = open (dynamics_med)
	lines = f.read
	lines = lines.split("\n")	
	output_string = ""
	lines.each{ |line|
		line.strip!		
		foundSamples = line.scan(/^(\d+)\t(-*\d+.\d+)$/)		
			if(foundSamples[0] != nil)							
				rms_dynamics[foundSamples[0][0].to_i] = foundSamples[0][1]				
			end
		}
	
	
	f = open (error_list_med)
	lines = f.read
	lines = lines.split("\n")	
	output_string = ""
	lines.each{ |line|		
		line.strip!		
		foundSamples = line.scan(/^([A-Za-z]+:)\t(\d+)\t(.+)$/)		
			if(foundSamples[0] != nil)				
				if(med_to_coarse.has_key?(foundSamples[0][1].to_i))					
					output_string+=foundSamples[0][1].to_s + "\t" +rms_dynamics[foundSamples[0][1].to_i] + "\t" + line+"\n"
				end				
			end
		}		
	File.open("1_0_to_0_2_avg.txt", 'w'){ |f| f.write(output_string) }
	
	
end

main("0_6_cutoff\\centers_0_6.txt","0_6_cutoff\\0_6_avg_error_list.txt","0_6_cutoff\\Medium_Hight_corr.txt", "0_6_cutoff\\0_6_to_0_2_map.map", "0_6_cutoff\\0_6_to_0_2_map.map")