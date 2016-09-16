# Automated Assembly Planning for Digital Manufacturing Commons #
This repository represents a tool to automatically determine the best assembly plan for a given assembly. The basic method can be invoked:
* as a plugin to ESI's IC.IDO Virtual Reality software
* as a command line executable 
* as a app for the Digital Manufacturing Commons.
However, the first one has significantly changed given the functionality of IC.IDO and is no longer included here. This work was funded under DARPA AVM iFAB from 
2012 to 2015 and then by DMDII from 2015 to present (set to expire on 3/31/2017).
The work is being done primarily by the Design Engineering Lab at Oregon State University.

## What does the application do? ##
Given an CAD assembly representing a product to be assembled, the application searches for the best way to assemble it. Essentially, the application is a planning algorithm. It produces a plan along with estimates for timing for each step in the plan.


## What does the application require (inputs/outputs)? ##
###Inputs###
A folder of shapes where each shape is positioned relative to one another in a global coordinate frame. The shapes can be in STL, PLY, 3MF or AMF file formats. 

###Outputs###
A single plan is created as an .xml file which describes a "treequence". A treequence (portmanteau of tree and sequence) is an assembly tree showing what parts come together in a particular order. 
Given the timing associated with the actions and the accommodation for parallel assembly steps the treequence provides details of how and when to build parts of the system. This treequence is not
particularly user-friendly. So, an external visualization has been created which shows the tree and an animation of the assembly process.

## How do I get set up? ##
1. Clone the repository 
2. Open the file "Assembly Planner.sln" in visual studio. 
3. Build. This should create a file in the root folder called "AssemblyPlanner.exe".

## How to run the application ##
1. The root directory is populated with a number of folders and binaries. All the dll's are there to support the main exe, AssemblyPlanner.exe. 
2. In the folder, "workspace", place your shape files for your assembly (stls, plys, AMFs or 3MFs). Remove all other files.
3. Now, run AssemblyPlanner.exe. A console window will open and begin sending status updates about the process. Do not close this window.
4. This process will pause with the following statement, "Press enter once input parts table generated". At this time, go back to the root folder and open the file "partTable.html"
5. In the newly opened web page, click on the "choose files" button in the upper-left corner of the screen and select the file named "parts_properties.xml" in the folder "workspace" (which should have just been generate by the program).
6. In the resulting table, enter the density/mass information for each part and correct the fastener classification of each part, as needed, using the check boxes of each corresponding entry.
7. Once the entire table has been filled out, click the button labeled "Render XML". This should cause a "download" link to appear next to the button. Click on this and move the downloaded file (which should be called "parts_properties2.xml") into the "workspace" folder.
8. Navigate back to the console application and press enter.
9. Once the process finishes, producing a notification giving the total time of the process, the console application may be closed.
10. Open the file in the root folder named "render.html".
11. In the newly opened web page, click on the "choose files" button and select all models in the "workspace" folder as well as the file labeled "solution.xml". Do not select any files beyond this. Then press "Open".
12. 

## Contribution guidelines ##
Current efforts are being completed by the Design Engineering Lab at Oregon State University. If you are interested in contributing contact Prof. Matt Campbell at matt<dot>campbell<at>oregonstate<dot>edu.