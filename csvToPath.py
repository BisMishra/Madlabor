from robot_command.rpl import *
set_units("mm", "deg", "s")
'''
SVG Drawer
This example shows how to get the ZA6 robot arm to draw SVG paths.
To get this to work we will be using svgpathtools library. It's a great collection of tools that make it easy to play with SVG files.

NOTE: For this project Make sure you set up a user frame to make sure it supports the tool you choose to use. Here is a link on how to get this set up [https://www.youtube.com/watch?v=i_zQoZG7DYQ]

I have updated this program to read data from a premade csv file to deal with the library installation issues
'''
import csv
import math
import numpy as np
import geometry_msgs.msg
import moveit_commander
# c_paths=[]
# current_path_index=-1
# path_points=[]

# with open('realtimePath.csv','r') as file:
# 	csv_reader=csv.DictReader(file)
# 	for row in csv_reader:

# 		path_index=int(row["pathIndex"])
# 		x, y, z=float(row["xCoord"]),float(row["yCoord"]),float(row["zCoord"])
# 		if path_index != current_path_index:
# 			if path_points:
# 				c_paths.append(path_points)
# 			path_points=[]
# 			current_path_index=path_index
# 		path_points.append((x,y,z))
# 	if path_points:
# 		c_paths.append(path_points)

safeHeight = 450

def start():
    c_paths=[]
    current_path_index=-1
    path_points=[]

    with open('realtimePath.csv','r') as file:
        csv_reader=csv.DictReader(file)
        for row in csv_reader:
            path_index=int(row["pathIndex"])
            x, y, z=float(row["xCoord"]),float(row["yCoord"]),float(row["zCoord"])
            if path_index != current_path_index:
                if path_points:
                    c_paths.append(path_points)
                path_points=[]
                current_path_index=path_index
            path_points.append((x,y,z))
        if path_points:
            c_paths.append(path_points)

    #movej(j[0,55.506,-13.092,0,-42.414,180])

    #choose path blending radii
    # dist=[.0001]
    # blendrad=[.0001]

    # for c_path in c_paths:
    #     #create variables to hold x, y coodinate points
    #     lx = c_path[0][0]
    #     ly = c_path[0][1]
    #     lz = c_path[0][2]
    #     #lx0=c_path[0][0]
    #     #ly0=c_path[0][1]
    #     for pt in c_path: # for each point in c_path
    #         #Get points
    #         lx0=lx
    #         ly0=ly
    #         lz0 = lz
    #         lx = pt[0]
    #         ly = pt[1]
    #         lz = pt[2]
    #         #notify(str([lx,lx0,ly,ly0])) #for debugging
    #         #sleep(5)
    #         dist.append(math.sqrt(((lx-lx0)**2)+((ly-ly0)**2)+(lz-lz0)**2))
    #     notify(str(dist))
    #     sleep(5)
    #     dist.sort()
    #     blendrad.append((dist[1]*0.451000)-0.261389)
    #     dist=[]
    # sync()

    #set_path_blending(False)
        #Execute moves by path
    pathnum=0
    #for c_path in c_paths: # For each path in c_paths
    #set_path_blending(True, blendrad[pathnum])
    #notify(str(blendrad[pathnum])) #debugging
    #create variables to hold x, y coodinate points
    lx = 0
    ly = 0
    lz = 0
    pointnum=0
    print(c_paths)
    for point in c_paths:
        for coord in point: # for each point in c_path
            #Get points
            lx = coord[0]
            ly = coord[1]
            lz = coord[2]
            #notify(str([lx, ly, lz, 0, 90, 0]))
            #sleep(5)
            movej(p[lx, ly, lz, 0, 90, 0]) # Move pen to (lx, ly) point using the movej() function
            pointnum+=1
    sync()
    set_path_blending(False)
    pathnum+=1

def main():
    change_user_frame('vr_frame') # Just to make sure that the robot uses the right user frame (NOTE: Don't forget to set up your user frame, if you don't know how to here is a great video that will get you up to speed [https://www.youtube.com/watch?v=i_zQoZG7DYQ])
    start()
if __name__=='__main__':
    main()