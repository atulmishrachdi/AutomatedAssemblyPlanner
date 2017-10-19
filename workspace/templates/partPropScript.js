;


if( typeof(startupScripts) == 'undefined'){

	var startupScripts = {
		"0":function(){},
		"1":function(){},
		"2":function(){},
		"3":function(){},
		"4":function(){},
		"5":function(){},
		"6":function(){},
		"7":function(){}
	};

}





// Put recieved data about the parts in here.
// Fills out the table with the information in the given xml document text (as a string, mind you)
/**
*
* Given the contents of a part table XML file (as a string), fills out the table in the web page.
*
* @method part_recieveData
* @for partTableGlobal
* @param {String} theXMLText The contents of a part table
* @return {Void}
*
*/
function part_recieveData(theXMLText){

	var theXML=$.parseXML(theXMLText);
	console.log(theXML);
	var theEntries=part_grab(theXML,"parts_properties");
	var theEntries=$(theEntries).children("properties");
	var pos=0;
	var lim=theEntries.length;
	var name;
	var classif;
	while(pos<lim){
		part_addEntry(theEntries[pos]);
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
* @method part_sendData
* @for partTableGlobal
* @param {String} theXMLText The contents of the part table in the webpage, as a string
* in XML formatting
* @return {Void}
*
*/
function part_sendData(theXMLText){

	// Do whatever you want with the resulting data to send it off, if you want


}













// Changes key press states based off of key presses
/**
*
* Accepts a key press event and, if the key press corresponds to one
* of the keys used for manipulating the view, sets the proper components
* of "inputState" to true.
*
* @method part_registerDown
* @for partTableGlobal
*
*
* @param {Event} e The key down event to be supplied to the function by a key down event
* listener on the web page
* @return {Void}
*
*/
function part_registerDown(e){



	var theKey;
	if (e.which == null) {
		theKey= String.fromCharCode(e.keyCode) // IE
	} else if (e.which!=0 /*&& e.charCode!=0*/) {
		theKey= String.fromCharCode(e.which)   // the rest
	} else {
		return;// special key
	}
	theKey=theKey.toUpperCase();



	if(theKey=='A'){
		if(inputState.A === false){
			var box = part_getChildrenByTag(focusRow,"TD")[4].childNodes[0];
			if(box.value === "on"){
				box.click();
			}
		}
		inputState.A=true;
	}
	if(theKey=='S'){
		if(inputState.S === false){
			var rowList = part_getChildrenByTag(document.getElementById("body_id"),"TR");
			var tabLen = rowList.length;
			if(focusIdx < tabLen-1){
				rowList[focusIdx+1].onclick();
			}
		}
		inputState.S=true;
	}
	if(theKey=='D'){
		if(inputState.D === false){
			var box = part_getChildrenByTag(focusRow,"TD")[4].childNodes[0];
			if(box.value === "off"){
				box.click();
			}
		}
		inputState.D=true;
	}
	if(theKey=='W'){
		if(inputState.W === false){
			var rowList = part_getChildrenByTag(document.getElementById("body_id"),"TR");
			var tabLen = rowList.length;
			if(focusIdx > 0){
				rowList[focusIdx-1].onclick();
			}
		}
		inputState.W=true;
	}
	return;

}




/**
*
* Accepts a key press release and, if the key release corresponds to one
* of the keys used for manipulating the view, sets the proper components
* of "inputState" to false
*
* @method part_registerUp
* @for partTableGlobal
*
*
* @param {Event} e The key up event to be supplied to the function by a key up event
* listener on the web page
* @return {Void}
*
*/
function part_registerUp(e){


	var theKey;
	if (e.which == null) {
		theKey= String.fromCharCode(e.keyCode) // IE
	} else if (e.which!=0 /*&& e.charCode!=0*/) {
		theKey= String.fromCharCode(e.which)   // the rest
	} else {
		return;// special key
	}
	theKey=theKey.toUpperCase();



	if(theKey=='A'){
		inputState.A=false;
	}
	if(theKey=='S'){
		inputState.S=false;
	}
	if(theKey=='D'){
		inputState.D=false;
	}
	if(theKey=='W'){
		inputState.W=false;
	}
	return;

}







//document.getElementById("downloadLink").setAttribute("style","display: none");





// Returns a list of all immediate children of the input html element which have the given tag name
/**
*
* Given an HTML element and a string, returns a list containing all child elements
* of the given element with a tag equivalent to the given string
*
* @method part_getChildrenByTag
* @for partTableGlobal
* @param {HTML Element} theNode The HTML element whose children are to be searched
* @param {String} tag The string to be used when searching for element children
* @return {Void}
*
*/
function part_getChildrenByTag(theNode,tag){
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


// Returns the first child node in the given document element with the given class
/**
*
* Given a jQuery object and a string, returns the first child of the given element with
* a tag equivalent to the given string.
*
* @method part_grab
* @for partTableGlobal
* @param {jQuery Object} theTree The jQuery object whose child is to be returned
* @param {String} theMember The name of the tag being searched
* @return {jQuery Object} The first child with the given tag. If such a child does not
* exist, null is returned.
*
*/
function part_grab(theTree,theMember){
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
* @method part_grabInd
* @for partTableGlobal
* @param {jQuery Object} theTree The jQuery object whose child is to be returned
* @param {String} theMember The name of the tag being searched
* @param {String} theIndex The ordinal of the matching child to be returned
* @return {jQuery Object} The child meeting the tag and ordinal requirement.
* If such a child does not exist, null is returned.
*
*/
function part_grabInd(theTree,theMember, theIndex){
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
* @method part_addEntry
* @for partTableGlobal
* @param {jQuery Object} theEntry The jQuery object containing the representation of a table
* entry, as extracted from an XML document
* @return {Void}
*
*/
function part_addEntry(theEntry){

	var theName=part_grab(theEntry,"name").innerHTML;
	var theVolume="<text>"+Number.parseFloat(part_grab(theEntry,"volume").innerHTML).toFixed(8)+"</text><br/>"+volElem;

	var theMass=massElem;
	var theCertainty=Number.parseFloat(part_grab(theEntry,"fastener_certainty").innerHTML);
	var theSurfaceArea=Number.parseFloat(part_grab(theEntry,"surface_area").innerHTML).toFixed(8);

	//console.log(theCertainty);

	var fstChecked="<input type='checkbox' onchange='part_flipCheck(this)' value='off'></input>";
	//console.log(theCertainty);
	if(theCertainty>0.5){
		//console.log("Box is checked");
		fstChecked="<input type='checkbox' onchange='part_flipCheck(this)' value='on' checked></input>";
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
* @method part_renderXML
* @for partTableGlobal
* @return {Void}
*
*/
function part_renderXML(){

	theTable.search("").draw();

	console.log(theTable.rows().data());

	var result="<?xml version='1.0' encoding='utf-8'?>\n<parts_properties xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>\n";

	theTable=document.getElementById("body_id");


	var theEntries=part_getChildrenByTag(theTable,"TR");

	var entryPos=0;
	var entryLim=theEntries.length;

	var theCells;
	var thisResult;


	while(entryPos<entryLim){

		thisResult=prat_part_renderEntry(part_getChildrenByTag(theEntries[entryPos],"TD"));
		if(thisResult!=null){
			result=result+thisResult;
		}
		else{
			return null;
		}

		entryPos++;
	}

	result+="</parts_properties>";



	part_sendData(result);


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





function part_render() {

		// The function that will manage frame requests
		requestAnimationFrame( part_render );


		focusPart.Mesh.geometry.computeBoundingBox();
		focusBox=focusPart.Mesh.geometry.boundingBox.clone();

		focusPoint= new THREE.Vector3(
									  (focusBox.min.x+focusBox.max.x)/2,
									  (focusBox.min.y+focusBox.max.y)/2,
									  (focusBox.min.z+focusBox.max.z)/2
									 );

		thePos.normalize();

		thePos.applyEuler(theEul);
		theEul.set(0,0,0,'XYZ');
		thePos.multiplyScalar(theDistance);
		camera.position.copy(thePos);
		camera.position.add(focusPoint);
		camera.lookAt(focusPoint);
		camera.updateMatrix();

		sunLight.position.set( (camera.position.x-focusPoint.x)*2+focusPoint.x,
							   (camera.position.y-focusPoint.y)*2+focusPoint.y,
							   (camera.position.z-focusPoint.z)*2+focusPoint.z );
		sunLight.target.position=focusPoint;

		updateAxisLines();

		renderer.render(scene, camera);
	};




// Converts the information in the given table entry to an XML element (as a plaintext string)
/**
*
* Given an html row element containing data regarding a part, converts the information into an
* xml formatted string and returns it
*
* @method part_renderEntry
* @for partTableGlobal
* @param {HTML Element} theCells An html row element containing information about a part
* @return {String} XML representation of the table entry
*
*/
function part_renderEntry(theCells){

	var massCell=theCells[3];
	massCell=part_getChildrenByTag(massCell,"DIV")[0];

	var texts = part_getChildrenByTag(massCell,"TEXT");
	var inputs = part_getChildrenByTag(massCell,"INPUT");
	var conv = part_conversion(document.getElementById("massUnits"));

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

	if(part_getChildrenByTag(theCells[4],"INPUT")[0].value=="on"){
		checkText="  <fastener_certainty>1</fastener_certainty>\n";
	}


	var result="";
	result=result+"<properties>\n";
	result=result+"  <name>"+theCells[0].innerHTML+"</name>\n";
	result=result+massText;
	if(part_getChildrenByTag(theCells[1],"TEXT")[0].innerHTML=="" || isNaN(Number.parseFloat(part_getChildrenByTag(theCells[1],"TEXT")[0].innerHTML))){
		alert("Volume data not provided for one or more cells");
		return null;
	}
	result=result+"  <volume>"+part_getChildrenByTag(theCells[1],"TEXT")[0].innerHTML+"</volume>\n";
	result=result+"  <surface_area>"+theCells[2].innerHTML+"</surface_area>\n";
	result=result+checkText;
	result=result+"</properties>\n";
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
* @method part_insertMassInput
* @for partTableGlobal
* @param {HTML Element} theButton The button that calls this function
* @return {Void}
*
*/
function part_insertMassInput(theButton){
	theButton.parentElement.innerHTML=	"<div class='masselem'>"+
										"<text>Mass:</text> "+
										"<input type='text'></input>"+
										"<button onclick='part_insertDensityInput(this)'>Input By Volume+Density</button>"+
										"</div>";
}

// Adds in input by part density
/**
*
* A function automatically called by button elements associated with the density option
* in an entry's mass section when pressed. Changes the parent mass section to contain a
* text input element and a button allowing the user to switch over to mass input.
*
* @method part_insertDensityInput
* @for partTableGlobal
* @param {HTML Element} theButton The button that calls this function
* @return {Void}
*
*/
function part_insertDensityInput(theButton){
	theButton.parentElement.innerHTML=	"<div class='masselem'>"+
										"<text></text><text>Density:</text> "+
										"<input type='text' onchange='part_insertMassDisplay(this)'></input>"+
										densityDiv+
										"<button onclick='part_insertMassInput(this)' >Input By Mass</button>"+
										"</div>";
}


// Adds in input for part surface thickness, hiding the origional volume
/**
*
* A function automatically called by button elements associated with the hollow option
* in an entry's volume section when pressed. Changes the parent volume section to contain a
* text input element and a button allowing the user to indicate the part is not hollow.
*
* @method part_insertHollowInput
* @for partTableGlobal
* @param {HTML Element} theButton The button that calls this function
* @return {Void}
*
*/
function part_insertHollowInput(theButton){
	var storage="<p style='display: none;'>"+Number.parseFloat(part_getChildrenByTag(theButton.parentElement,"TEXT")[0].innerHTML)+"</p>";
	var display="<text></text>";
	var backButton="<button onclick='removeHollowInput(this)'>Is Not Hollow</button>";
	var thicknessBox="Thickness: <input type='text' onchange='part_insertVolumeDisplay(this)'></input>";
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
	var storage="<text style='display: block;'>"+Number.parseFloat(part_getChildrenByTag(theButton.parentElement,"P")[0].innerHTML)+"</text>";
	var backButton="<button onclick='part_insertHollowInput(this)'>Is Hollow</button>";
	theButton.parentElement.innerHTML=storage+backButton;
}




// Updates the displayed volume based off of the part's surface area and thickness
/**
*
* A function automatically called by text input elements associated with the hollow option
* in an entry's volume section when changed. Changes the currently displayed volume to
* match the given thickness
*
* @method part_insertVolumeDisplay
* @for partTableGlobal
* @param {HTML Element} theBox The text input element that calls this function
* @return {Void}
*
*/
function part_insertVolumeDisplay(theBox){
	var theThickness=Number.parseFloat(part_getChildrenByTag(theBox.parentElement,"INPUT")[0].value);
	var area=Number.parseFloat(part_getChildrenByTag(theBox.parentElement.parentElement.parentElement,"TD")[2].innerHTML);
	var vol=theThickness*area;
	part_getChildrenByTag(theBox.parentElement,"TEXT")[0].innerHTML=vol.toString()+"\n";
}


// Updates the displayed mass based off of volume and density
/**
*
* A function automatically called by text input elements associated with the density option
* in an entry's mass section when changed. Changes the currently displayed mass to
* match the given density
*
* @method part_insertMassDisplay
* @for partTableGlobal
* @param {HTML Element} theBox The text input element that calls this function
* @return {Void}
*
*/
function part_insertMassDisplay(theBox){
	var theDensity=Number.parseFloat(part_getChildrenByTag(theBox.parentElement,"INPUT")[0].value);
	var theVolume=Number.parseFloat(part_getChildrenByTag(part_getChildrenByTag(theBox.parentElement.parentElement.parentElement,"TD")[1],"TEXT")[0].innerHTML);
	var mass=theDensity*theVolume;
	part_getChildrenByTag(theBox.parentElement,"TEXT")[0].innerHTML= (mass.toFixed(8)).toString()+"\n";
}




// Adds in density dropdown
/**
*
* A function automatically called by button elements associated with accessing sample densities.
* Adds a div element containing several sample density options.
*
* @method part_doDensityDrop
* @for partTableGlobal
* @param {HTML Element} theButton The button element that called this function
* @return {Void}
*
*/
function part_doDensityDrop(theButton){
	theButton.parentElement.innerHTML=undropDensityButton+densityMenu;
}


// Removes density dropdown
/**
*
* A function automatically called by button elements associated with accessing sample densities.
* Removes the sample density option div element.
*
* @method unpart_doDensityDrop
* @for partTableGlobal
* @param {HTML Element} theButton The button element that called this function
* @return {Void}
*
*/
function unpart_doDensityDrop(theButton){
	theButton.parentElement.innerHTML=dropDensityButton;
}




// Sets the value of the density of the part to the density associated with the calling button
/**
*
* A function automatically called by button elements associated with sample densities. Will fill
* the associated density input box with the value associated with the inner text of the button.
*
* @method part_changeDensity
* @for partTableGlobal
* @param {HTML Element} theButton The button element that called this function
* @return {Void}
*
*/
function part_changeDensity(theButton){

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

	part_getChildrenByTag(theButton.parentElement.parentElement.parentElement,"INPUT")[0].value=val;
	part_insertMassDisplay(part_getChildrenByTag(theButton.parentElement.parentElement.parentElement,"INPUT")[0]);

}


// Sets the value of the checkbox to the appropriate value
/**
*
* A function automatically called by text box elements upon becoming checked/unchecked.
* Sets an internal value to indicate the checked state of the element.
*
* @method part_flipCheck
* @for partTableGlobal
* @param {HTML Element} theBox The checkbox element calling this function
* @return {Void}
*
*/
function part_flipCheck(theBox){

	var row = theBox.parentElement.parentElement;

	if(theBox.value=='on'){
		theBox.value='off';
		row.classList.add("nfast");
	}
	else{
		theBox.value='on';
		row.classList.add("fast");
	}
	focusRow.onclick();

}



/**
*
* Takes the value present in the global density input textbox and applies it to all
* parts currently in the table.
*
* @method part_fillGlobalDensity
* @for partTableGlobal
* @return {Void}
*
*/
function part_fillGlobalDensity(){

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
		massElems[pos].innerHTML=		"<text></text><text><br>Density:</text> "+
										"<input type='text' onchange='part_insertMassDisplay(this)' value='"+(theDensity.toFixed(8)).toString()+"'>"+
										"</input>"+
										densityDiv+
										"<button onclick='part_insertMassInput(this)' >Input By Mass</button>";
		part_insertMassDisplay(part_getChildrenByTag(massElems[pos],"BUTTON")[0]);
		pos++;
	}

}






/**
*
* Makes it so that the table row associated with this call has its part model
* displayed
*
* @method part_clickFocus
* @for partTableGlobal
* @return {Void}
*
*/
function part_clickFocus(){
	console.log("Table click registered");
	var pos = 0;
	var lim = parts.length;
	
	while(pos<lim){
		//console.log(part_getChildrenByTag(this,"TD")[0].innerHTML);
		//console.log(grabName(parts[pos].Name));
		//console.log("~~~~~");
		if(grabName(parts[pos].Name)  === part_getChildrenByTag(this,"TD")[0].innerHTML){
			console.log("Found match between "+grabName(parts[pos].Name)+" and "+part_getChildrenByTag(this,"TD")[0].innerHTML)
			if(focusPart !== null){
				focusPart.Mesh.material = new THREE.MeshLambertMaterial(wireSettings);
			}

			//console.log("AAAAAAH");
			if(focusRow !== null){
				var tdList = part_getChildrenByTag(focusRow,"TD");
				var tdPos = 0;
				var tdLim = tdList.length;
				while(tdPos<tdLim){
					tdList[tdPos].classList.remove("focus");
					tdPos++;
				}
			}

			//console.log("AAAAAAH");

			focusPart = parts[pos];

			//console.log("AAAAAAH");
			//console.log(focusPart);
			focusRow = this;

			if(part_getChildrenByTag(this,"TD")[4].childNodes[0].value === "on"){
				focusPart.Mesh.material = new THREE.MeshLambertMaterial({color: 0x884444});
			}
			else{
				focusPart.Mesh.material = new THREE.MeshLambertMaterial({color: 0x444488});
			}
			var tdList = part_getChildrenByTag(focusRow,"TD");
			var tdPos = 0;
			var tdLim = tdList.length;
			while(tdPos<tdLim){
				tdList[tdPos].classList.add("focus");
				tdPos++;
			}

			var rowList = part_getChildrenByTag(document.getElementById("body_id"),"TR");
			var rowPos = 0;
			var lim = rowList.length;
			while(rowPos<lim){
				if(rowList[rowPos] == this){
					break;
				}
				rowPos++;
			}
			var hStep = this.clientHeight;
			this.parentElement.parentElement.parentElement.scrollTop = hStep*rowPos;
			focusIdx = rowPos;

		}
		pos++;
	}

}







/**
*
* Sets up every table entry HTML element with the focus function as its "onclick"
* attribute
*
* @method part_setupClickFocus
* @for partTableGlobal
* @return {Void}
*
*/
function part_setupClickFocus(){

	var rowElems = part_getChildrenByTag(document.getElementById("body_id"),"TR");
	var pos=0;
	var lim=rowElems.length;
	while(pos<lim){
		rowElems[pos].onclick = part_clickFocus;
		pos++;
	}

}







/**
*
* Sets up every table entry HTML element with color highlighting
*
* @method part_setupHighlighting
* @for partTableGlobal
* @return {Void}
*
*/
function part_setupHighlight(){

	var rowElems = part_getChildrenByTag(document.getElementById("body_id"),"TR");
	console.log(rowElems);
	var pos=0;
	var lim=rowElems.length;
	while(pos<lim){
		if(rowElems[pos].childNodes[4].childNodes[0].value === "on"){
			rowElems[pos].classList.add("fast");
		}
		else{
			rowElems[pos].classList.add("nfast");
		}
		pos++;
	}

}






/**
*
* Accepts a string and outputs a part_conversion value for the string's described unit to the standard
* units of this application. If no suitable unit is identified in the string, the part_conversion value
* returned is 0.
*
* @method part_conversion
* @for partTableGlobal
* @param {String} theString A string representation of the unit being looked up
* @return {Float} the unit part_conversion value
*
*/
function part_conversion (theString){

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





/**
*
* Returns a blank mass input element for use in the table
*
* @method part_makeMassElem
* @for partTableGlobal
* @return {HTML element} a blank mass input element
*
*/
function part_makeMassElem(){

	var result=document.createElement("DIV");
	result.className="masselem";
	var massButton=document.createElement("BUTTON");
	massButton.onclick="part_insertMassInput(this)";
	massButton.innerHTML="Input By Mass";
	var densityButton=document.createElement("BUTTON");
	densityButton.onclick="part_insertDensityInput(this)";
	densityButton.innerHTML="Input By Volume+Density";
	result.appendChild(massButton);
	result.appendChild(densityButton);
	return result;

}




/**
*
* Returns a blank volume input element for use in the table
*
* @method part_makeVolElem
* @for partTableGlobal
* @return {HTML element} a blank volume input element
*
*/
function part_makeVolElem(){

	result=document.creatElement("BUTTON");
	result.onclick="part_insertHollowInput(this)";
	result.innerHTML="Is Hollow";
	return result;

}




/**
*
* Returns a standard button for a density dropdown menu
*
* @method part_makeDropButton
* @for partTableGlobal
* @return {HTML element} a standard density dropdown button
*
*/
function part_makeDropButton(){

	result=document.creatElement("BUTTON");
	result.className="dropbtn";
	result.onclick="part_doDensityDrop(this)";
	result.innerHTML="Sample Densities";
	return result;

}


/**
*
* Returns a standard button to remove a density dropdown element
*
* @method part_makeUndropButton
* @for partTableGlobal
* @return {HTML element} a standard density dropdown removal button
*
*/
function part_makeUndropButton(){

	result=document.creatElement("BUTTON");
	result.className="dropbtn";
	result.onclick="unpart_doDensityDrop(this)";
	result.innerHTML="Sample Densities";
	return result;

}



/**
*
* Returns a standard sample density dropdown menu
*
* @method part_makeDensityDiv
* @for partTableGlobal
* @return {HTML element} a sample density dropdown menu
*
*/
function part_makeDensityDiv(){

	result=document.creatElement("DIV");
	result.className="dropdown";
	result.innerHTML=densityMenu;
	return result;

}








/**
*
* Given a mouseup event, sets corresponding internal button states for mouse-related controls
*
* @method part_doMouseUp
* @for directionConfirmGlobal
* @param {mouseup event} theEvent
* @return {Void}
*
*/
function part_doMouseUp(theEvent){
	if(theEvent.button == 0){
		leftDrag = false;
	}
	else if(theEvent.button == 2){
		rightDrag = false;
	}
}


/**
*
* Given a mousedown event, sets corresponding internal button states for mouse-related controls
*
* @method part_doMouseDown
* @for directionConfirmGlobal
* @param {mousedown event} theEvent
* @return {Void}
*
*/
function part_doMouseDown(theEvent){
	if(theEvent.button == 0){
		leftDrag = true;
	}
	else if(theEvent.button == 2){
		rightDrag = true;
	}
}



/**
*
* Given a mouseleave event, sets corresponding internal button states for mouse-related controls
*
* @method part_doMouseLeave
* @for directionConfirmGlobal
* @param {mouseup event} theEvent
* @return {Void}
*
*/
function part_doMouseLeave(theEvent){
	leftDrag = false;
	rightDrag = false;
	lastMouse = null;
}


/**
*
* Given a mousedrag event, rotates the camera or adds a vector to the currently displayed pair,
* depending upon whether or not the left or right mouse button is depressed
*
* @method doPartDrag
* @for directionConfirmGlobal
* @param {mouseup event} theEvent
* @return {Void}
*
*/
function part_doDrag (theEvent){
	console.log("doPartDrag called");
	if(leftDrag===true){
		thePos.normalize();
		theEul.set(theEvent.movementY*(-0.008)*Math.cos(Math.atan2(thePos.x,thePos.z)),
				   theEvent.movementX*(-0.008),
				   theEvent.movementY*(0.008)*Math.sin(Math.atan2(thePos.x,thePos.z)),
				   'ZYX');
	}

}


/**
*
* Prevents the default response of the given event (used to prevent dropdown menus when right
* clicking on the display).
*
* @method part_justDont
* @for directionConfirmGlobal
* @param {Event Object} theEvent The event to suppress the default response of.
* @return {Void}
*
*/
function part_justDont(theEvent){
	theEvent.preventDefault();
}







/**
*
* Given a mousewheel event, changes the distance of the camera from the center of the scene
*
* @method part_doZoom
* @for directionConfirmGlobal
* @param {mouseup event} theEvent
* @return {Void}
*
*/
function part_doZoom(theEvent){
	var theDelta = theEvent.deltaY == 0 ? 0 : ( theEvent.deltaY > 0 ? 1 : -1 );
	theDistance=theDistance*Math.pow(1.001,theDelta*(-40));
}



function part_doSetup(){

	initAxisLines();

	part_setupClickFocus();
	part_setupHighlight();
	part_getChildrenByTag(document.getElementById("body_id"),"TR")[0].click();

}



startupScripts["2"] = function (){




	fileReaders=[];
	inputXML=null;
	textFile=null;
	focusBox=null;
	focusPoint=null;
	focusIdx=null;

	assemblyPairs=[];
	namePairs=[];
	theDirections=[];
	theXML=null;
	thePos= new THREE.Vector3(1,0,0);
	lastMouse=null;
	theDistance= 300;
	theVectors= []; //new THREE.Line(  new THREE.Geometry(),  new THREE.LineBasicMaterial({color: 0x0000ff}))

	theEul= new THREE.Euler(0,0,0,'XYZ');
	baseQuat = new THREE.Quaternion(1,0,0,0);
	deltaQuat = new THREE.Quaternion(1,0,0,0);

	leftDrag = false;
	rightDrag = false;


	inputState={

		W: false,
		A: false,
		S: false,
		D: false,

	}


	wireSettings={transparent: true, opacity: 0.1, color: 0x444444, wireframe: false};



	// Array for storing fileReaders to keep track of them
	fileReaders=[];


	// Setting up the table
	theTable= $('#table_id').DataTable({
		"scrollY": "300px",
		"scroller": true,
		"deferpart_render": false,
		"paging": false,
		"searching": false
	});

	document.addEventListener('keydown', part_registerDown , false);
	document.addEventListener('keyup', part_registerUp , false);
	document.getElementById("display").addEventListener("mousemove", part_doDrag);
	document.getElementById("display").addEventListener("wheel", part_doZoom);

	theXML=$.parseXML(inText);
	console.log(theXML);
	theEntries=part_grab(theXML,"parts_properties");
	theEntries=$(theEntries).children("part_properties");
	console.log(theEntries);
	pos=0;
	lim=theEntries.length;
	//name = null;
	//classif = null;
	while(pos<lim){
		part_addEntry(theEntries[pos]);
		pos++;
	}


	clearScene("display");


	part_doSetup();
	part_render();

}

