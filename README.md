# Automated Assembly Planning for Digital Manufacturing Commons #
This repository represents a tool to automatically determine the best assembly plan for a given assembly. The basic method can be invoked:
* as a plugin to ESI's IC.IDO Virtual Reality software
* as a command line executable 
* as a web-based app.
However, the first one has significantly changed given the functionality of IC.IDO and is no longer included here. This work was funded under DARPA AVM iFAB from 
2012 to 2015 and then by DMDII from 2015 to 2017.
The work is being done primarily by the Design Engineering Lab at Oregon State University.

## What does the application do? ##
Given an CAD assembly representing a product to be assembled, the application searches for the best way to assemble it. Essentially, the application is a planning algorithm. It produces a plan along with estimates for timing for each step in the plan.


## What does the application require (inputs/outputs)? ##
###Inputs###
A group of shape files where each shape is a part positioned relative to one another in a global coordinate frame. The shapes can be in STL, PLY, 3MF or AMF file formats. 

###Outputs###
A single plan is created as an .xml file which describes a "treequence". A treequence (portmanteau of tree and sequence) is an assembly tree showing what parts come together in a particular order. 
Given the timing associated with the actions and the accommodation for parallel assembly steps the treequence provides details of how and when to build parts of the system. This treequence is not
particularly user-friendly. So, an external visualization has been created which shows the tree and an animation of the assembly process.

## How do I get set up? ##
Currently, the tool is not "live" on a website. However, you can run the software locally. Here are the steps to do so.
1. Clone the repository 
2. Open the file "Assembly Planner.sln" in visual studio. 
3. Build the solution (e.g. compile the code).
4. Download node.js (https://nodejs.org/) and install.
5. With a command-line tool, navigate to the main folder and type "node server.js"
6. In your favorite browser, navigate to http://localhost:3000
7. Upload your 3D models and follow along with the prompts.

...scratch...
6. In the resulting table, enter the density/mass information for each part and correct the fastener classification of each part, as needed, using the check boxes of each corresponding entry.
7. Once the entire table has been filled out, click the button labeled "Render XML". This should cause a "download" link to appear next to the button. Click on this and move the downloaded file (which should be called "parts_properties2.xml") into the "workspace" folder.
8. Navigate back to the console application and press enter.
9. Once the process finishes, producing a notification giving the total time of the process, the console application may be closed.
10. Open the file in the root folder named "render.html".
11. In the newly opened web page, click on the "choose files" button and select all models in the "workspace" folder as well as the file labeled "solution.xml". Do not select any files beyond this. Then press "Open".
12. The web page should be displaying a partially transparent panel overlay with a line of gibberish characters inside. This is a representation of the assembly. Click on the button to the left of this line of characters to show the sub-assemblies making up the final product. From there, two additional lines of symbols should appear, each with their own buttons. Once fully expanded, this interface should display the names of the parts as well as symbols that will be used for short-hand representation in parent assemblies. The show/hide button on the top-left of this may be used to toggle the visibility on this panel.
13. To explore the assembly process, hide the overlay and press on the "mouse lock" button on the top-right of the page. Your mouse will disapear. This means that your mouse is locked. One may exit mouse lock at any time by pressing the escape key. While in mouse lock, the virtual camera linked to the display may be angled through mouse movement. Furthermore, the camera may be moved using the W,S,A,D, and Space keys. For ease of viewing, time progression in the assembly animation may be sped up through scrolling up on the mouse wheel or slowed down by scrolling down.

## Contribution guidelines ##
Current efforts are being completed by the Design Engineering Lab at Oregon State University. If you are interested in contributing contact Prof. Matt Campbell at matt<dot>campbell<at>oregonstate<dot>edu.
