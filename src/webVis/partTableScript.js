;



//
//    Pretty Important: Keep this as true unless/until you've incorperated some other
//                      method of getting file input
//
var manualFileInput=true;



// Put recieved data about the parts in here. 
// Fills out the table with the information in the given xml document text (as a string, mind you)
/**
*
* Given the contents of a part table XML file (as a string), fills out the table in the web page.
*
* @method recieveData
* @for partTableGlobal
* @param {String} theXMLText The contents of a part table
* @return {Void}
* 
*/
function recieveData(theXMLText){

	var theXML=$.parseXML(theXMLText);
	console.log(theXML);
	var theEntries=grab(theXML,"parts_properties");
	var theEntries=$(theEntries).children("part_properties");
	var pos=0;
	var lim=theEntries.length;
	var name;
	var classif;
	while(pos<lim){
		addEntry(theEntries[pos]);
		pos++;
	}
	console.log(theTable);

}


// Gets called when the user submits the table and everything is properly filled out
/**
*
* Is called whenever the user submits the part table and every entry has been
* properly filled out.
*
* @method sendData
* @for partTableGlobal
* @param {String} theXMLText The contents of the part table in the webpage, as a string
* in XML formatting
* @return {Void}
* 
*/
function sendData(theXMLText){

	// Do whatever you want with the resulting data to send it off, if you want
	

}









var fileReaders=[];
var inputXML=null;
var textFile=null;


// Some HTML bits to insert into the table as needed

// Starting Input for mass cells
var massElem="<div class='masselem'>"+
				"<button onclick='insertMassInput(this)'>Input By Mass</button>"+
				"<button onclick='insertDensityInput(this)'>Input By Volume+Density</button>"+
			 "</div>";


// Starting Input for Volume cells
var volElem="<button onclick='insertHollowInput(this)'>Is Hollow</button>";


// The button for showing the sample density dropdown menu
var dropDensityButton="<button class='dropbtn' onclick='doDensityDrop(this)'>Sample Densities</button>";
	
// The button for removing the sample density dropdown menu
var undropDensityButton="<button class='dropbtn' onclick='undoDensityDrop(this)'>Sample Densities</button>";
	
// The sample density dropdown menu
var densityMenu="<div class='dropdown-content' style='border-color: #666666; background-color: #DDDDDD; border-style: solid; padding: 10px 10px 10px 10px;'>"+
					"<button onclick='changeDensity(this)'>Aluminum</button>"+
					"<button onclick='changeDensity(this)'>Glass</button>"+
					"<button onclick='changeDensity(this)'>Plastic (Hi-Density)</button>"+
					"<button onclick='changeDensity(this)'>Plastic (Med-Density)</button>"+
					"<button onclick='changeDensity(this)'>Plastic (Low-Density)</button>"+
					"<button onclick='changeDensity(this)'>Rubber</button>"+
					"<button onclick='changeDensity(this)'>Steel</button>"+
					"<button onclick='changeDensity(this)'>Titanium</button>"+
					"<button onclick='changeDensity(this)'>Wood</button>"+
				"</div>";


// Starting input for density cells
var densityDiv= "\n<div class='dropdown'>"+dropDensityButton+"</div>";


var manualIO="<input type='file' id='fileinput' multiple ></input>"+
"<button style='display: inline;' onclick='renderXML()'>Render XML</button>"+
"<a href='' id='downloadLink' download='parts_properties2.xml' ></a>";


if(manualFileInput==true){

	document.getElementById("theBody").innerHTML=manualIO+document.getElementById("theBody").innerHTML;

}


// Setting up the table
var theTable= $('#table_id').DataTable({
    "paging": false
});

//document.getElementById("downloadLink").setAttribute("style","display: none");



// A simple function for getting the extension from a file name (sans period)
/**
*
* Accepts a string and outputs the string of all characters following the final '.' symbol
* in the string. This is used internally to extract file extensions from file names.
*
* @method grabExtension
* @for partTableGlobal
* @param {String} theName The file name to be processed
* @return {String} the extension in the given file name. If no extension is found, the 
* 'undefined' value is returned.
* 
*/
function grabExtension(theName){
	return (/[.]/.exec(theName)) ? /[^.]+$/.exec(theName) : undefined;
}


// Returns a list of all immediate children of the input html element which have the given tag name
/**
*
* Given an HTML element and a string, returns a list containing all child elements
* of the given element with a tag equivalent to the given string
*
* @method getChildrenByTag
* @for partTableGlobal
* @param {HTML Element} theNode The HTML element whose children are to be searched
* @param {String} tag The string to be used when searching for element children
* @return {Void}
* 
*/
function getChildrenByTag(theNode,tag){
	var childs=theNode.children;
	var pos=0;
	var lim=childs.length;
	var result=[];
	while(pos<lim){
		if(childs[pos].tagName===tag){
			result.push(childs[pos]);
		}
		pos++;
	}
	return result;
}




// Upon a file upload event triggering, defines and attaches each file's onload function 
/**
*
* Accepts a fileinput event, presumably from a file upload event listener, and assigns
* functions to each file reader listed in the event to be called upon the full loading
* of that given reader's files 
*
* @method readMultipleFiles
* @for partTableGlobal
* @param {Event} evt A fileinput event, to be given by a fileinput event listener
* @return {Void}
* 
*/
function readMultipleFiles(evt) {
	if(inputXML===null){
		//Retrieve all the files from the FileList object
		var files = evt.target.files; 
				
		if (files) {
			for (var i=0, f; f=files[i]; i++) {
				
				var r = new FileReader();
				var extension=grabExtension(f.name)[0];
				
				if(extension===undefined){
					continue;
				}
				if(extension.toLowerCase()==="xml"){
					if(!(inputXML===null)){
						console.log("Warning: More than one XML file provided");
					}
					r.onload = (function(f) {
						return function(e) {
							var contents = e.target.result;
							inputXML=r.result;
							loadParts();
						};
					})(f);
					r.readAsText(f,"US-ASCII");
					fileReaders.push({Reader: r, Name: f.name});
				}
							
			}
		} 
		else {
			  alert("Failed to load files"); 
		}
	}
	else {
		  alert("Refresh page to reattempt upload of files"); 
	}
}


// Sets up the file loading function
document.getElementById('fileinput').addEventListener('change', readMultipleFiles, false);



// Checks that all files are loaded. If so, fills out the table with the given information
/**
*
* Called internally upon every recieved fileload event. Checks if every file reader in the 
* array "fileReaders" has fully read each of their files. If so, then the function calls
* "recieveData".
*
* @method loadParts
* @for partTableGlobal
* @return {Void}
* 
*/
function loadParts (){
	var pos=0;
	var lim=fileReaders.length;
	while(pos<lim){
		if(!(fileReaders[pos].Reader.readyState===2)){
			break;
		}
		pos++;
	}
	if(pos===lim){
		console.log("Done loading parts");
		recieveData(inputXML);				
	}
}




// Returns the first child node in the given document element with the given class
/**
*
* Given a jQuery object and a string, returns the first child of the given element with
* a tag equivalent to the given string.
*
* @method grab
* @for partTableGlobal
* @param {jQuery Object} theTree The jQuery object whose child is to be returned
* @param {String} theMember The name of the tag being searched
* @return {jQuery Object} The first child with the given tag. If such a child does not 
* exist, null is returned.
* 
*/
function grab(theTree,theMember){
	if($(theTree).children(theMember).length!=0){
		return $(theTree).children(theMember)[0];
	}
	else{
		return null;
	}
}


// Returns the nth child from the list of children nodes in the given document element with the given class
/**
*
* Given a jQuery object and an integer "N", returns the Nth child of the given element with
* the given tag. 
*
* @method grabInd
* @for partTableGlobal
* @param {jQuery Object} theTree The jQuery object whose child is to be returned
* @param {String} theMember The name of the tag being searched
* @param {String} theIndex The ordinal of the matching child to be returned
* @return {jQuery Object} The child meeting the tag and ordinal requirement. 
* If such a child does not exist, null is returned.
* 
*/
function grabInd(theTree,theMember, theIndex){
	if($(theTree).children(theMember).length>theIndex){
		return $(theTree).children(theMember)[theIndex];
	}
	else{
		return null;
	}
}



// Adds an entry to the table with the information from the given document element
/**
*
* Given a jQuery object representation of a part entry, inserts an html representation
* of that entry in the table 
*
* @method addEntry
* @for partTableGlobal
* @param {jQuery Object} theEntry The jQuery object containing the representation of a table
* entry, as extracted from an XML document
* @return {Void}
* 
*/
function addEntry(theEntry){

	var theName=grab(theEntry,"name").innerHTML;
	var theVolume="<text>"+Number.parseFloat(grab(theEntry,"volume").innerHTML)+"</text>\n"+volElem;
	
	var theMass=massElem;
	var theCertainty=Number.parseFloat(grab(theEntry,"fastener_certainty").innerHTML);
	var theSurfaceArea=Number.parseFloat(grab(theEntry,"surface_area").innerHTML);
	
	console.log(theCertainty);
	
	var fstChecked="<input type='checkbox' onchange='flipCheck(this)' value='off'></input>";
	console.log(theCertainty);
	if(theCertainty>0.5){
		console.log("Box is checked");
		fstChecked="<input type='checkbox' onchange='flipCheck(this)' value='on' checked></input>";
	}
	
	var theAmbiguity=1-2*Math.abs(theCertainty-0.5);
	theAmbiguity=theAmbiguity.toFixed(2);
	
	
	theTable.row.add( [
		theName,
		theVolume,
		theSurfaceArea,
		theMass,
		fstChecked,
		theAmbiguity
	] ).draw();

	
}





// Creates an XML file out of the information present in the table and then adds a download link to the page
/**
*
* Parses through each entry in the table and, if all entries are fully filled out, converts the table into an
* XML file and adds a download link for that file to the webpage
*
* @method renderXML
* @for partTableGlobal
* @return {Void}
* 
*/
function renderXML(){

	theTable.search("").draw();
	
	console.log(theTable.rows().data());
	
	var result="<?xml version='1.0' encoding='utf-8'?>\n<parts_properties xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>\n";
	
	theTable=document.getElementById("body_id");

	
	var theEntries=getChildrenByTag(theTable,"TR");

	var entryPos=0;
	var entryLim=theEntries.length;
	
	var theCells;
	var thisResult;
	
	
	while(entryPos<entryLim){
		
		thisResult=renderEntry(getChildrenByTag(theEntries[entryPos],"TD"));
		if(thisResult!=null){
			result=result+thisResult;
		}
		else{
			return null;
		}
		
		entryPos++;
	}
	
	result+="</parts_properties>";
	

	
	sendData(result);
	
	
	if(manualFileInput){
	
		var data = new Blob([result], {type: 'text/plain'});

		if (textFile !== null) {
		  window.URL.revokeObjectURL(textFile);
		}

		textFile = window.URL.createObjectURL(data);

		document.getElementById("downloadLink").setAttribute("style","color: white; display: inline;");
		document.getElementById("downloadLink").innerHTML="Download";
		document.getElementById("downloadLink").href=textFile;
	
	}


}




// Converts the information in the given table entry to an XML element (as a plaintext string)
/**
*
* Given an html row element containing data regarding a part, converts the information into an
* xml formatted string and returns it
*
* @method renderEntry
* @for partTableGlobal
* @param {HTML Element} theCells An html row element containing information about a part
* @return {String} XML representation of the table entry
* 
*/
function renderEntry(theCells){

	var massCell=theCells[3];
	massCell=getChildrenByTag(massCell,"DIV")[0];

	var texts = getChildrenByTag(massCell,"TEXT");
	var inputs = getChildrenByTag(massCell,"INPUT");
	var conv = conversion(document.getElementById("massUnits"));
	
	var massText="";
	if(inputs.length==0 || isNaN(Number.parseFloat(inputs[0].value))){
		console.log(inputs);
		alert("Mass data not provided for one or more cells");
		return null;
	}
	else if(texts.length==1){
		if(inputs.length==1){
			if(inputs[0].value.toString()===""){
				return null;
			}
			massText="  <mass>"+(parseFloat(inputs[0].value)*conv)+"</mass>\n";
		}
		else{
			alert("HTML Corrupted");
		}
	}
	else if(texts.length==2){

		if(texts[0].innerHTML.toString()==="" || isNaN(Number.parseFloat(texts[0].innerHTML))){
			return null;
		}
		massText="  <mass>"+(parseFloat(texts[0].innerHTML)*conv)+"</mass>\n";
	}
	else{
		alert("HTML Corrupted");
	}
	
	
	var checkText="  <fastener_certainty>0</fastener_certainty>\n";

	if(getChildrenByTag(theCells[4],"INPUT")[0].value=="on"){
		checkText="  <fastener_certainty>1</fastener_certainty>\n";
	}
	

	var result="";
	result=result+"<part_properties>\n";
	result=result+"  <name>"+theCells[0].innerHTML+"</name>\n";
	result=result+massText;
	if(getChildrenByTag(theCells[1],"TEXT")[0].innerHTML=="" || isNaN(Number.parseFloat(getChildrenByTag(theCells[1],"TEXT")[0].innerHTML))){
		alert("Volume data not provided for one or more cells");
		return null;
	}
	result=result+"  <volume>"+getChildrenByTag(theCells[1],"TEXT")[0].innerHTML+"</volume>\n";
	result=result+"  <surface_area>"+theCells[2].innerHTML+"</surface_area>\n";
	result=result+checkText;
	result=result+"</part_properties>\n";
	return result;

}




/*
function hollowOpt(theCheckBox){
	theCheckBox.parentElement.innerHTML="<input type='text' onchange='revertHollowOpt(this)'> </input>";
}

function revertHollowOpt(theTextBox){
	if(isNaN(Number.parseFloat(theTextBox.value))){
		theTextBox.parentElement.innerHTML="<text>hollow </text><input type='checkbox' onchange='hollowOpt(this)'> </input>";
	}
}
*/

// Adds in plain mass input
/**
*
* A function automatically called by button elements associated with the mass option
* in an entry's mass section when pressed. Changes the parent mass section to contain a
* text input element and a button allowing the user to switch over to density input.
*
* @method insertMassInput
* @for partTableGlobal
* @param {HTML Element} theButton The button that calls this function
* @return {Void}
* 
*/
function insertMassInput(theButton){
	theButton.parentElement.innerHTML=	"<div class='masselem'>"+
										"<text>Mass:</text> "+
										"<input type='text'></input>"+
										"<button onclick='insertDensityInput(this)'>Input By Volume+Density</button>"+
										"</div>";
}

// Adds in input by part density
/**
*
* A function automatically called by button elements associated with the density option
* in an entry's mass section when pressed. Changes the parent mass section to contain a
* text input element and a button allowing the user to switch over to mass input.
*
* @method insertDensityInput
* @for partTableGlobal
* @param {HTML Element} theButton The button that calls this function
* @return {Void}
* 
*/
function insertDensityInput(theButton){
	theButton.parentElement.innerHTML=	"<div class='masselem'>"+
										"<text></text><text>Density:</text> "+
										"<input type='text' onchange='updateMassDisplay(this)'></input>"+
										densityDiv+
										"<button onclick='insertMassInput(this)' >Input By Mass</button>"+
										"</div>";
}


// Adds in input for part surface thickness, hiding the origional volume
/**
*
* A function automatically called by button elements associated with the hollow option
* in an entry's volume section when pressed. Changes the parent volume section to contain a
* text input element and a button allowing the user to indicate the part is not hollow.
*
* @method insertHollowInput
* @for partTableGlobal
* @param {HTML Element} theButton The button that calls this function
* @return {Void}
* 
*/
function insertHollowInput(theButton){
	var storage="<p style='display: none;'>"+Number.parseFloat(getChildrenByTag(theButton.parentElement,"TEXT")[0].innerHTML)+"</p>";
	var display="<text></text>";
	var backButton="<button onclick='removeHollowInput(this)'>Is Not Hollow</button>";
	var thicknessBox="Thickness: <input type='text' onchange='updateVolumeDisplay(this)'></input>";
	theButton.parentElement.innerHTML=storage+display+thicknessBox+backButton;
}


// Removes thickness input and displays original volume
/**
*
* A function automatically called by button elements associated with the hollow option
* in an entry's volume section when pressed. Changes the parent volume section to contain
* only a mass value and a button allowing the user to indicate the part is hollow.
*
* @method removeHollowInput
* @for partTableGlobal
* @param {HTML Element} theButton The button that calls this function
* @return {Void}
* 
*/
function removeHollowInput(theButton){ 
	var storage="<text style='display: block;'>"+Number.parseFloat(getChildrenByTag(theButton.parentElement,"P")[0].innerHTML)+"</text>";
	var backButton="<button onclick='insertHollowInput(this)'>Is Hollow</button>";
	theButton.parentElement.innerHTML=storage+backButton;
}




// Updates the displayed volume based off of the part's surface area and thickness
/**
*
* A function automatically called by text input elements associated with the hollow option
* in an entry's volume section when changed. Changes the currently displayed volume to 
* match the given thickness
*
* @method updateVolumeDisplay
* @for partTableGlobal
* @param {HTML Element} theBox The text input element that calls this function
* @return {Void}
* 
*/
function updateVolumeDisplay(theBox){
	var theThickness=Number.parseFloat(getChildrenByTag(theBox.parentElement,"INPUT")[0].value);
	var area=Number.parseFloat(getChildrenByTag(theBox.parentElement.parentElement.parentElement,"TD")[2].innerHTML);
	var vol=theThickness*area;
	getChildrenByTag(theBox.parentElement,"TEXT")[0].innerHTML=vol.toString()+"\n";
}


// Updates the displayed mass based off of volume and density
/**
*
* A function automatically called by text input elements associated with the density option
* in an entry's mass section when changed. Changes the currently displayed mass to 
* match the given density
*
* @method updateMassDisplay
* @for partTableGlobal
* @param {HTML Element} theBox The text input element that calls this function
* @return {Void}
* 
*/
function updateMassDisplay(theBox){
	var theDensity=Number.parseFloat(getChildrenByTag(theBox.parentElement,"INPUT")[0].value);
	var theVolume=Number.parseFloat(getChildrenByTag(getChildrenByTag(theBox.parentElement.parentElement.parentElement,"TD")[1],"TEXT")[0].innerHTML);
	var mass=theDensity*theVolume;
	getChildrenByTag(theBox.parentElement,"TEXT")[0].innerHTML=mass.toString()+"\n";
}




// Adds in density dropdown
/**
*
* A function automatically called by button elements associated with accessing sample densities.
* Adds a div element containing several sample density options.
*
* @method doDensityDrop
* @for partTableGlobal
* @param {HTML Element} theButton The button element that called this function
* @return {Void}
* 
*/
function doDensityDrop(theButton){
	theButton.parentElement.innerHTML=undropDensityButton+densityMenu;
}


// Removes density dropdown
/**
*
* A function automatically called by button elements associated with accessing sample densities.
* Removes the sample density option div element.
*
* @method undoDensityDrop
* @for partTableGlobal
* @param {HTML Element} theButton The button element that called this function
* @return {Void}
* 
*/
function undoDensityDrop(theButton){
	theButton.parentElement.innerHTML=dropDensityButton;
}




// Sets the value of the density of the part to the density associated with the calling button
/**
*
* A function automatically called by button elements associated with sample densities. Will fill
* the associated density input box with the value associated with the inner text of the button.
*
* @method changeDensity
* @for partTableGlobal
* @param {HTML Element} theButton The button element that called this function
* @return {Void}
* 
*/
function changeDensity(theButton){
	
	var mat=theButton.innerHTML;
	var val;
	var coef=1000*1000*1000;
	if(mat=="Aluminum"){
		val=2700/coef;
	}
	else if(mat=="Glass"){
		val=2520/coef;
	}
	else if(mat=="Plastic (Hi-Density)"){
		val=1950/coef;
	}
	else if(mat=="Plastic (Med-Density)"){
		val=1100/coef;
	}
	else if(mat=="Plastic (Low-Density)"){
		val=900/coef;
	}
	else if(mat=="Rubber"){
		val=1270/coef;
	}
	else if(mat=="Steel"){
		val=7859/coef;
	}
	else if(mat=="Titanium"){
		val=4507/coef;
	}
	else if(mat=="Wood"){
		val=6300/coef;
	}
	else{
		return;
	}
	
	getChildrenByTag(theButton.parentElement.parentElement.parentElement,"INPUT")[0].value=val;
	updateMassDisplay(getChildrenByTag(theButton.parentElement.parentElement.parentElement,"INPUT")[0]);

}


// Sets the value of the checkbox to the appropriate value
/**
*
* A function automatically called by text box elements upon becoming checked/unchecked.
* Sets an internal value to indicate the checked state of the element.
*
* @method flipCheck
* @for partTableGlobal
* @param {HTML Element} theBox The checkbox element calling this function
* @return {Void}
* 
*/
function flipCheck(theBox){

	if(theBox.value=='on'){
		theBox.value='off';
	}
	else{
		theBox.value='on';
	}

}



function fillGlobalDensity(){
	var densInp= document.getElementById("GlobalDensityInput");
	var theDensity;
	if(densInp.value===""){
		console.log(densInp);
		return;
	}
	else{
		theDensity=parseFloat(densInp.value);
	}
	
	var massElems = document.getElementsByClassName('masselem');
	console.log(massElems);
	var pos=0;
	var lim=massElems.length;
	while(pos<lim){
		massElems[pos].innerHTML=		"<text></text><text>Density:</text> "+
										"<input type='text' onchange='updateMassDisplay(this)' value='"+theDensity.toString()+"'>"+
										"</input>"+
										densityDiv+
										"<button onclick='insertMassInput(this)' >Input By Mass</button>";
		updateMassDisplay(getChildrenByTag(massElems[pos],"BUTTON")[0]);
		pos++;
	}
	
}


function conversion (theString){
	
	switch(theString){
		case "millimeters" :
			return 1.0;
			break;
		case "centimeters" :
			return 10.0;
			break;
		case "meters":
			return 1000.0;
			break;
		case "inches":
			return 25.4;
			break;
		case "feet":
			return 304.8;
			break;
		case "kilograms":
			return 1.0;
			break;
		case "pounds":
			return 0.4536;
			break;
		default:
			return 1.0;
			break;
	}
	return 1.0;
	
}




var massElem="<div class='masselem'>"+
				"<button onclick='insertMassInput(this)'>Input By Mass</button>"+
				"<button onclick='insertDensityInput(this)'>Input By Volume+Density</button>"+
			 "</div>";
function makeMassElem(){
	
	var result=document.createElement("DIV");
	result.className="masselem";
	var massButton=document.createElement("BUTTON");
	massButton.onclick="insertMassInput(this)";
	massButton.innerHTML="Input By Mass";
	var densityButton=document.createElement("BUTTON");
	densityButton.onclick="insertDensityInput(this)";
	densityButton.innerHTML="Input By Volume+Density";
	result.appendChild(massButton);
	result.appendChild(densityButton);
	return result;
	
}


// Starting Input for Volume cells
var volElem="<button onclick='insertHollowInput(this)'>Is Hollow</button>";
function makeVolElem(){
	
	result=document.creatElement("BUTTON");
	result.onclick="insertHollowInput(this)";
	result.innerHTML="Is Hollow";
	return result;
	
}


// The button for showing the sample density dropdown menu
var dropDensityButton="<button class='dropbtn' onclick='doDensityDrop(this)'>Sample Densities</button>";
function makeDropButton(){
	
	result=document.creatElement("BUTTON");
	result.className="dropbtn";
	result.onclick="doDensityDrop(this)";
	result.innerHTML="Sample Densities";
	return result;
	
}
	
// The button for removing the sample density dropdown menu
var undropDensityButton="<button class='dropbtn' onclick='undoDensityDrop(this)'>Sample Densities</button>";
function makeUndropButton(){
	
	result=document.creatElement("BUTTON");
	result.className="dropbtn";
	result.onclick="undoDensityDrop(this)";
	result.innerHTML="Sample Densities";
	return result;
	
}
	
// The sample density dropdown menu
var densityMenu="<div class='dropdown-content' style='border-color: #666666; background-color: #DDDDDD; border-style: solid; padding: 10px 10px 10px 10px;'>"+
					"<button onclick='changeDensity(this)'>Aluminum</button>"+
					"<button onclick='changeDensity(this)'>Glass</button>"+
					"<button onclick='changeDensity(this)'>Plastic (Hi-Density)</button>"+
					"<button onclick='changeDensity(this)'>Plastic (Med-Density)</button>"+
					"<button onclick='changeDensity(this)'>Plastic (Low-Density)</button>"+
					"<button onclick='changeDensity(this)'>Rubber</button>"+
					"<button onclick='changeDensity(this)'>Steel</button>"+
					"<button onclick='changeDensity(this)'>Titanium</button>"+
					"<button onclick='changeDensity(this)'>Wood</button>"+
				"</div>";

// Starting input for density cells
var densityDiv= "\n<div class='dropdown'>"+dropDensityButton+"</div>";
function makeDensityDiv(){
	
	result=document.creatElement("DIV");
	result.className="dropdown";
	result.innerHTML=densityMenu;
	return result;
	
}



