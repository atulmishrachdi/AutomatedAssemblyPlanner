# Automated Assembly Planning for Digital Manufacturing Commons #
This repository represents efforts funded under DARPA AVM iFAB from 2012 to present. Following termination of funds in 2013, researchers at Oregon State University have continued to develop the assembly planning tool. Current efforts are developing the tool to run:
* as a command line executable 
* as a plugin to ESI's IC.IDO Virtual Reality software
* as a app for the Digital Manufacturing Commons.

## What does the application do? ##
Given an CAD assembly representing a product to be assembled, the application searches for the best way to assemble it. Essentially, the application is a planning algorithm. It produces a plan along with estimates for timing for each step in the plan.
## What does the application require (inputs/outputs)? ##
###Inputs###
A folder of shapes where each shape is positioned relative to one another in a global coordinate frame. The shapes can be in STL, PLY or AMF file formats. Additionally, a configuration files are used for reading in numerous constants used by the search (default values are provided).
###Outputs###
A folder of plans are created. Each plan is a xml file which describes a "treequence". A treequence (portmanteau of tree and sequence) is an assembly tree showing what parts come together in a particular order. Given the timing associated with the actions and the accommodation for parallel assembly steps the treequence provides details of how and when to build parts of the system. This treequence requires some external visualization and could also be used to create documentation, which would essentially be an instruction manual.

* Quick summary
* Version
* [Learn Markdown](https://bitbucket.org/tutorials/markdowndemo)

### How do I get set up? ###

* Summary of set up
* Configuration
* Dependencies
* Database configuration
* How to run tests
* Deployment instructions

### Contribution guidelines ###

* Writing tests
* Code review
* Other guidelines

### Who do I talk to? ###

* Repo owner or admin
* Other community or team contact