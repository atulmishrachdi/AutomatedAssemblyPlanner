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
3. Build and run

## How to run the application ##
1. The root directory
2. Open the file "Assembly Planner.sln" in visual studio. 
3. Build and run

## Contribution guidelines ##
Current efforts are being completed by the Design Engineering Lab at Oregon State University. If you are interested in contributing contact Prof. Matt Campbell at matt<dot>campbell<at>oregonstate<dot>edu.