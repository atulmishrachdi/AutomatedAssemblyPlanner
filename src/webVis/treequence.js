;




/**
*
* Given a jQuery object, returns a full list of all of its children.
*
* @method whatsIn
* @for renderGlobal
* @param {jQuery Object} theTree The jQuery object whose children should be returned
* @return {Array} Array of the object's children
*
*/
function whatsIn(theTree){

	return document.getElementById("warning").innerHTML=$(theTree).children("*");

}






/**
*
* Given a jQuery object and a string, returns the first child of the given element with
* a tag equivalent to the given string.
*
* @method grab
* @for renderGlobal
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




// returns a list of all children of the given eleemnt with the same tagName
/**
*
* Given a jQuery object and an integer "N", returns the Nth child of the given element with
* the given tag.
*
* @method grabInd
* @for renderGlobal
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



// Converts the given XML element into a javascript object
/**
*
* Given a jQuery object derived from parsing an XML document, extracts all information
* relevant to part movement and composes it into an identically structured tree of
* nested javascript objects which is then returned
*
* @method getMovement
* @for renderGlobal
* @param {jQuery Object} theTree The jQuery object to be parsed over
* @param {Float} myX The X position of the subassembly represented by the root node of theTree
* @param {Float} myY The Y position of the subassembly represented by the root node of theTree
* @param {Float} myZ The Z position of the subassembly represented by the root node of theTree
* @param {Float} myTime The time value of the subassembly represented by the root node of theTree
* @return {Object} The root node of the tree of extracted movement data
*
*/
function getMovement(theTree, myX, myY, myZ, myTreePos, myTime){

	console.log(myTreePos);

	if(($(theTree).children("Install").length==0)){
		return { Name: $(theTree).attr("Name"),
						 X: myX, Y: myY, Z: myZ,
						 TreePosition: myTreePos,
						 Time: myTime,
						 Ref: null,
						 Mov: null,
						 Fst: []
					 };
	}
	else{
		var childTime=parseFloat(grab(grab(theTree,"Install"),"Time").innerHTML)+myTime;
		var movDirection=grab(grab(theTree,"Install"),"InstallDirection");
		var movXDir=parseFloat(grabInd(movDirection,"double",0).innerHTML);
		var movYDir=parseFloat(grabInd(movDirection,"double",1).innerHTML);
		var movZDir=parseFloat(grabInd(movDirection,"double",2).innerHTML);
		var movDistance=parseFloat(grab(grab(theTree,"Install"),"InstallDistance").innerHTML);
		movDistance = Math.min(movDistance,800);
		var movX=myX-movXDir*movDistance;
		var movY=myY-movYDir*movDistance;
		var movZ=myZ-movZDir*movDistance;
		var ref=getMovement(getRef(theTree), myX, myY, myZ,  new THREE.Vector3(movDistance,0,0).add(myTreePos), childTime);
		var mov=getMovement(getMov(theTree), movX, movY, movZ, new THREE.Vector3(movDistance,movDistance,0).add(myTreePos), childTime);
		var fasteners = $(theTree).children("Secure");
		var theFst;
		var theDir;
		var theDist;
		var Fst = [];
		//console.log(fasteners);
		if(fasteners.length >= 1){
			fasteners = $(fasteners[0]).children("Fasteners");
			if(fasteners.length >= 1){
				fasteners = $(fasteners[0]).children("Fastener");
				var pos = 0;
				var lim = fasteners.length;
				while(pos<lim){
					theDist = parseFloat($(fasteners[pos]).children("InstallDistance")[0].innerHTML);
					theDir = ($(fasteners[pos]).children("InstallDirection"))[0];
					theDir = $(theDir).children("double");
					//console.log($(fasteners[pos]).children("InstallDistance"));
					theFst = {  Name: "subasm-"+($(fasteners[pos]).children("Name"))[0].innerHTML,
								X: myX-parseFloat(theDir[0].innerHTML)*theDist,
								Y: myY-parseFloat(theDir[1].innerHTML)*theDist,
								Z: myZ-parseFloat(theDir[2].innerHTML)*theDist,
								TreePosition: new THREE.Vector3(theDist,theDist,0).add(myTreePos),
								Time: childTime,
								Ref: null,
								Mov: null,
								Fst: []
							};
					Fst.push(theFst);
					pos++;
				}
			}
		}

		return { Name: "",
						 X: myX,
						 Y: myY,
						 Z: myZ,
						 TreePosition: myTreePos,
						 Time: myTime,
						 Ref: ref,
						 Mov: mov,
						 Fst: Fst
					 };

	}

}




// Gets the XML representing the reference subassembly of the given XML representation of it's parent assembly
/**
*
* Given a jQuery Object, will return the first child with the tag "Reference" of the first child with
* the tag "Install" of the object. If no such child exists, null is returned.
*
* @method getRef
* @for renderGlobal
* @param {jQuery Object} theTree The jQuery object to be accessed
* @return {jQuery Object} The resulting child
*
*/
function getRef(theTree){

	theTree=grab(theTree,"Install");
	theTree=grab(theTree,"Reference");
	return theTree;

}

// Gets the XML representing the reference subassembly of the given XML representation of it's parent assembly
/**
*
* Given a jQuery Object, will return the first child with the tag "Moving" of the first child with
* the tag "Install" of the object. If no such child exists, null is returned.
*
* @method getMov
* @for renderGlobal
* @param {jQuery Object} theTree The jQuery object to be accessed
* @return {jQuery Object} The resulting child
*
*/
function getMov(theTree){

	theTree=grab(theTree,"Install");
	theTree=grab(theTree,"Moving");
	return theTree;

}



// Returns a tree representing the times of all installations in the  given tree
/**
*
* Given a jQuery object derived from parsing an XML document, extracts all information
* relevant to installation timing and composes it into an identically structured tree of
* nested javascript objects which is then returned
*
* @method getTimes
* @for renderGlobal
* @param {jQuery Object} theTree The jQuery object to be parsed over
* @param {Float} parentTime The time value of the subassembly represented by the root node of theTree
* @return {Object} The root node of the tree of extracted time data
*
*/
function getTimes(theTree, parentTime){

	if(($(theTree).children("Install").length==0)){
		return { Time: parentTime, Ref: null, Mov: null };
	}
	else{
		var myTime=parseFloat(grab(grab(theTree,"Install"),"Time").innerHTML)+parentTime;
		var ref=getTimes(getRef(theTree),myTime);
		var mov=getTimes(getMov(theTree),myTime);

		return { Time: parentTime, Ref: ref, Mov: mov};
	}

}


// Returns the longest period of time from a base parts initial installation to the
// construction of the final product
/**
*
* Given a tree of nested objects, returns the highest "Time" value from all the nodes
*
* @method getLongestTime
* @for renderGlobal
* @param {Object} timeTree The tree of time values
* @return {Object} The highest time value in the tree
*
*/
function getLongestTime(timeTree){

	if(timeTree==null){
		return 0;
	}
	return Math.max(getLongestTime(timeTree.Ref),getLongestTime(timeTree.Mov),timeTree.Time);

}



// Returns a tree-based representation of the names in the given tree
/**
*
* Given a jQuery object derived from parsing an XML document, extracts all part name
* information and composes it into an identically structured tree of nested javascript
* objects which is then returned
*
* @method getNames
* @for renderGlobal
* @param {jQuery Object} theTree The jQuery object to be parsed over
* @return {Object} The root node of the tree of extracted name data
*
*/
function getNames(theTree){

	if(($(theTree).children("Install").length==0)){
		return {Name: $(theTree).attr("Name"), Ref: null, Mov: null};
	}
	else{
		var ref = getNames(getRef(theTree));
		var mov = getNames(getMov(theTree));
		return {Name: "", Ref: ref, Mov: mov};
	}

}


// Merges the given tree representations of the time, space, and names associated with
// each installation into one tree
/**
*
* Given a three trees of nested javascript objects, one holding time data, one holding
* movement data, and one holding part name data
*
* @method mergeTrees
* @for renderGlobal
* @param {Object} TimeTree The root node of the tree containing time data
* @param {Object} SpaceTree The root node of the tree containing movement data
* @param {Object} NameTree The root node of the tree containing name data
* @return {Object} The root node of the resulting tree
*
*/
function mergeTrees(TimeTree,SpaceTree,NameTree){

	if(TimeTree==null || SpaceTree==null || NameTree==null){
		return null;
	}
	else{
		var ref=mergeTrees(TimeTree.Ref,SpaceTree.Ref,NameTree.Ref);
		var mov=mergeTrees(TimeTree.Mov,SpaceTree.Mov,NameTree.Mov);
		return {Time: TimeTree.Time, Space: SpaceTree.Space, Name: NameTree.Name, Ref: ref, Mov: mov};
	}

}




/**
*
* Given a three trees of nested javascript objects, one holding time data, one holding
* movement data, and one holding part name data
*
* @method getNameList
* @for renderGlobal
* @param {Object} TimeTree The root node of the tree containing time data
* @param {Object} SpaceTree The root node of the tree containing movement data
* @param {Object} NameTree The root node of the tree containing name data
* @return {Object} The root node of the resulting tree
*
*/
function getNameList(theTree){

	if(theTree==null){
		return [];
	}
	else{
		var result;
		if(theTree.Name===""){
			result=[];
		}
		else{
			result=[theTree.Name];
		}
		result=result.concat(getNameList(theTree.Ref));
		result=result.concat(getNameList(theTree.Mov));
		return result;
	}

}



/**
*
* Given an array of strings, returns the first index at which at least
* two of the included strings are different
*
* @method similarityCutoff
* @for renderGlobal
* @param {Array} theList The list of strings to be anylized
* @return {Index} The computed index
*
*/
function similarityCutoff(theList){


	var pos;
	var it=1;
	var lim=theList[0].length;
	var finish=theList.length;
	while(it<finish && lim!=0){
		pos=0;
		while(pos<lim){

			if(theList[it][pos]!=theList[0][pos]){
				lim=pos;
			}
			pos=pos+1;
		}

		it=it+1;
	}

	return lim;

}







/**
*
* Given a tree of nested javascript objects (each with a string attribute "Name") and an
* integer "N", removes the first N characters of each Name attribute
*
* @method cutOffNames
* @for renderGlobal
* @param {Object} theTree The structure containing name data
* @return {Void}
*
*/
function cutOffNames(theTree,theCutOff){

	if(theTree==null){

		return;

	}
	else{

		if(theCutOff<theTree.Name.length){
			theTree.Name=theTree.Name.substr(theCutOff,theTree.Name.length);
		}

		cutOffNames(theTree.Ref,theCutOff);
		cutOffNames(theTree.Mov,theCutOff);
		var pos = 0;
		var lim = theTree.Fst.length;
		while(pos<lim){
			cutOffNames(theTree.Fst[pos],theCutOff)
			pos++;
		}

		return;

	}


}



/**
*
* Given a tree of nested javascript objects (each with a string attribute "Name"), and two lists,
* regTreeNames and fstTreeNames, inserts all regular part names into regTreeNames and inserts all
* fastener part names into fstTreeNames
*
* @method getTreeNames
* @for renderGlobal
* @param {Object} tree
* @param {String List} regTreeNames
* @param {String List} fstTreeNames
* @return {Void}
*
*/
function getTreeNames(tree,regTreeNames,fstTreeNames){

	if(tree===null){
		return;
	}
	else{
		if(tree.Ref===null){
			regTreeNames.push(tree.Name);
		}
		else{
			getTreeNames(tree.Ref,regTreeNames,fstTreeNames);
			getTreeNames(tree.Mov,regTreeNames,fstTreeNames);
		}
		var pos = 0;
		var lim = tree.Fst.length;
		while(pos<lim){
			fstTreeNames.push(tree.Fst[pos].Name);
			pos++;
		}
	}

}




/**
*
* Given a list of parts, returns a list of the names of each part
*
* @method getPartNames
* @for renderGlobal
* @param {Part List} parts The list of part objects.
* @return {String List}
*
*/
function getPartNames(parts){

	var result = [];
	var pos = 0;
	var lim = parts.length;
	while(pos<lim){
		result.push(parts[pos].Name);
		pos++;
	}
	return result;

}





/**
*
* Given a tree of nested javascript objects (each with a float attribute "Time") and a
* float "N", sets each Time value to N minus that value
*
* @method flipTreeTime
* @for renderGlobal
* @param {Object} theTree The structure containing time data
* @param {Float} axis The value used to mirror the time values
* @return {Void}
*
*/
function flipTreeTime(theTree,axis){

	if(theTree==null){
		return;
	}
	else{
		theTree.Time=axis-theTree.Time;
		console.log(theTree.Time);
		flipTreeTime(theTree.Ref,axis);
		flipTreeTime(theTree.Mov,axis);
		var pos = 0;
		var lim = theTree.Fst.length;
		while(pos<lim){
			flipTreeTime(theTree.Fst[pos],axis);
			pos++;
		}
		return;
	}

}




/**
*
* Given a tree of nested javascript objects, returns the depth of the tree
*
* @method getDepth
* @for renderGlobal
* @param {Object} theTree The object structure
* @return {Int} The depth of the object
*
*/
function getDepth(theTree){

	if(theTree==null){
		return 0;
	}
	return 1+Math.max(getDepth(theTree.Ref, theTree.Mov));

}






// Selects a random UTF symbol from the set of closed ranges supplied
/**
*
* Given a staggered array of integer pairs, returns a random UTF character with a UTF value
* within one of the given integer ranges (inclusive)
*
* @method getRandomUTF
* @for renderGlobal
* @param {Array} selectSpace A staggered array of integer range limits
* @return {Void}
*
*/
function getRandomUTF (selectSpace){

	// If there are no ranges or one is not a complete pair, return '?'
	if(selectSpace.length%2==1 || selectSpace.length==0){
		return '?';
	}


	// Count up the number of symbols available
	var pos=0;
	var lim=selectSpace.length/2;
	var spaceSize=0;
	while(pos<lim){
		spaceSize=spaceSize+selectSpace[pos+1]-selectSpace[pos];
		pos=pos+2;
	}

	// Get a number in the range
	var sel=Math.random() * (spaceSize);

	// Get the right symbol from the right list
	pos=0;
	while(sel>(selectSpace[pos+1]-selectSpace[pos])){
		sel=sel-(selectSpace[pos+1]-selectSpace[pos]);
		pos=pos+2;
	}

	// convert the number to a character
	var result= String.fromCharCode(selectSpace[pos] + Math.random() * (selectSpace[pos+1]-selectSpace[pos]+1));


	return result;

}




// Populates the given html element with a representation of the given tree structure
/**
*
* Given a tree of nested javascript objects and an html element, inserts the contents
* of the root node of the given tree as an html element into the given element. Returns
* the name of the generated node.
*
* @method insertTreequenceHTML
* @for renderGlobal
* @param {Object} theTree The tree structure
* @param {HTML Element} parentElement The html element to contain the node information
* @return {Void}
*
*/
function insertTreequenceHTML(theTree,parentElement){


	if(theTree==null){
		return "";
	}

	// Set up the show/hide button
	var theButton=document.createElement("BUTTON");
	var theName="";
	theButton.innerHTML="-";
	theButton.setAttribute("onclick","swapHiding(parentElement)");
	theButton.classList.add("expButton");


	// If not a leaf, attach button
	if(theTree.Ref!=null || theTree.Mov!=null){
		parentElement.appendChild(theButton);
		//parentElement.appendChild(document.createElement("P"));
	}
	// If a leaf, make a placeholder symbol
	else{
		theName = theTree.Name.substring(0,theTree.Name.length);
		parentElement.appendChild(document.createTextNode(theName));
	}

	var movName;
	var refName;
	var childDiv = document.createElement("DIV");
	childDiv.classList.add("childNode");
	parentElement.appendChild(childDiv);

	// Attach ref and mov branches and get their names
	if(theTree.Mov!=null){
		var theMov=document.createElement("DIV");
		theMov.classList.add("movBranch");
		childDiv.appendChild(theMov);
		movName=insertTreequenceHTML(theTree.Mov,theMov);
	}
	if(theTree.Ref!=null){
		var theRef=document.createElement("DIV");
		theRef.classList.add("refBranch");
		childDiv.appendChild(theRef);
		refName=insertTreequenceHTML(theTree.Ref,theRef);
	}

	// If not a leaf, make name and insert it
	if(theTree.Ref!=null || theTree.Mov!=null){
		theName=movName+" --> "+refName;
		var theText = document.createTextNode(theName);
		var textDiv = document.createElement("DIV");
		textDiv.appendChild(theText);
		textDiv.classList.add("rootNode");
		parentElement.insertBefore(textDiv,childDiv);
	}

	// Hide all children of the element
	hideChildren(parentElement);

	return theName;

}





// Shows/hides the given node based off of the text in its associated button
/**
*
* Given an html node containing a button, hides all child treequence elements
* if the button text is not "+" and shows them if it is.
*
* @method swapHiding
* @for renderGlobal
* @param {HTML Element} theNode The html element whose treequence elements are to be manipulated.
* @return {Void}
*
*/
function swapHiding(theNode){

	var buttonState=getChildrenByTag(theNode,"BUTTON");
	if(buttonState==null || buttonState.length<1){
		return;
	}
	var theButton=buttonState[0];

	if(theButton.innerHTML=="+"){
		theButton.innerHTML="-";
		showChildren(theNode);
	}
	else{
		theButton.innerHTML="+";
		hideChildren(theNode);
	}


}






// shows the given node
/**
*
* Given an HTML element, sets the style attributes of that element to display it's contents.
*
* @method show
* @for renderGlobal
* @param {HTML Element} theNode The HTML element to be shown.
* @return {Void}
*
*/
function show(theNode){

	if(theNode.classList.contains("hidden")){
		theNode.classList.remove("hidden");
	}
	if(! theNode.classList.contains("shown")){
		theNode.classList.add("shown");
	}

}




// Hides the given node
/**
*
* Given an HTML element, sets the style attributes of that element to hide it's contents.
*
* @method hide
* @for renderGlobal
* @param {HTML Element} theNode The HTML element to be hidden.
* @return {Void}
*
*/
function hide(theNode){

	if(theNode.classList.contains("shown")){
		theNode.classList.remove("shown");
	}
	if(! theNode.classList.contains("hidden")){
		theNode.classList.add("hidden");
	}

}






// shows the given node's child nodes
/**
*
* Given an HTML element, sets the style attributes of that element's children
* to display their contents.
*
* @method showChildren
* @for renderGlobal
* @param {HTML Element} theNode The HTML element whose children are to be shown.
* @return {Void}
*
*/
function showChildren(theNode){

	var theChildren = getChildrenByTag(theNode,"DIV");
	var pos=0;
	var lim=theChildren.length;
	while(pos<lim){
		show(theChildren[pos]);
		showChildren(theChildren[pos]);
		pos++;
	}

}





// hides the given node's child nodes
/**
*
* Given an HTML element, sets the style attributes of that element's children
* to hide their contents.
*
* @method hideChildren
* @for renderGlobal
* @param {HTML Element} theNode The HTML element whose children are to be hidden.
* @return {Void}
*
*/
function hideChildren(theNode){

	var theChildren = getChildrenByTag(theNode,"DIV");
	var pos=0;
	var lim=theChildren.length;
	while(pos<lim){
		hideChildren(theChildren[pos]);
		hide(theChildren[pos]);
		pos++;
	}

}





// returns a list of all the child nodes of the given node with the given tag type
/**
*
* Given an HTML element and a string, returns a list containing all child elements
* of the given element with a tag equivalent to the given string
*
* @method getChildrenByTag
* @for renderGlobal
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
