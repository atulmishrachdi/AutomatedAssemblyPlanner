
var skyColor= 0xFFFFFF;
var assemblyPairs=[];
var namePairs=[];
var confDirs=null;
var unconfDirs=null;
var theXML=null;
var thePos= new THREE.Vector3(1,0,0);
var lastMouse=null;
var theDistance= 300;
var theVec= new THREE.Line(  new THREE.Geometry(),  new THREE.LineBasicMaterial({color: 0x0000ff}));
var theEul= new THREE.Euler(0,0,0,'XYZ');
var baseQuat = new THREE.Quaternion(1,0,0,0);
var deltaQuat = new THREE.Quaternion(1,0,0,0);

var dragInp=false;

theVec.geometry.vertices.push(new THREE.Vector3( 0, 0, 0 ),new THREE.Vector3( 0, 0, 0 ));

var wireSettings={transparent: true, opacity: 0.1, color: 0x444444, wireframe: false};


// Array for storing fileReaders to keep track of them
var fileReaders=[];

// Array for processed STLs
var STLs=[];

//  Array for processed parts
var parts=[];


var lastPair=null;
var currentPair=null;

var theWidth=document.getElementById("display").clientWidth;
var theHeight= document.getElementById("display").clientHeight;

// The scene of the assembly animation
var scene = new THREE.Scene();

scene.add( theVec );

// The camera
var camera = new THREE.PerspectiveCamera( 75, theWidth/theHeight, 1, 16000 );


// Setting up the renderer with the default color and display size
var renderer = new THREE.WebGLRenderer();
renderer.setClearColor( skyColor, 1 );
renderer.setSize(theWidth,theHeight);
console.log(theWidth);
console.log(theHeight);
document.getElementById("display").appendChild( renderer.domElement );

// Setting camera to Yaw-Pitch-Roll configuration
camera.rotation.reorder('YXZ');
camera.position.x=1;
camera.position.y=1;
camera.position.z=1;
console.log(camera.position);


// Adding in a whole bunch of lights for the scene, so the parts are well-lit
var directionalLight = new THREE.DirectionalLight( 0x888888 );
		directionalLight.position.x = 0; 
		directionalLight.position.y = 0; 
		directionalLight.position.z = 1; 
		directionalLight.position.normalize();
		scene.add( directionalLight );
		
var directionalLight = new THREE.DirectionalLight( 0x888888 );
		directionalLight.position.x = 0; 
		directionalLight.position.y = 1; 
		directionalLight.position.z = 0; 
		directionalLight.position.normalize();
		scene.add( directionalLight );
		
var directionalLight = new THREE.DirectionalLight( 0x888888 );
		directionalLight.position.x = 1; 
		directionalLight.position.y = 0; 
		directionalLight.position.z = 0; 
		directionalLight.position.normalize();
		scene.add( directionalLight );
var directionalLight = new THREE.DirectionalLight( 0x888888 );
		directionalLight.position.x = 0; 
		directionalLight.position.y = 0; 
		directionalLight.position.z = -1; 
		directionalLight.position.normalize();
		scene.add( directionalLight );
		
var directionalLight = new THREE.DirectionalLight( 0x888888 );
		directionalLight.position.x = 0; 
		directionalLight.position.y = -1; 
		directionalLight.position.z = 0; 
		directionalLight.position.normalize();
		scene.add( directionalLight );
		
var directionalLight = new THREE.DirectionalLight( 0x888888 );
		directionalLight.position.x = -1; 
		directionalLight.position.y = 0; 
		directionalLight.position.z = 0; 
		directionalLight.position.normalize();
		scene.add( directionalLight );


// Adding in one more light 
var sunLight = new THREE.SpotLight( 0x666666, 6, 32000, 1.2, 1, 1 );
		sunLight.position.set( 4000, 4000, 4000 );
		scene.add( sunLight );



function confirmPair(theButton){
	document.getElementById("confirmed").appendChild(theButton.parentElement);
	theButton.innerHTML="unconfirm";
	theButton.onclick=function(){
		deconfirmPair(theButton);
	};
}

function deconfirmPair(theButton){
	document.getElementById("unconfirmed").appendChild(theButton.parentElement);
	theButton.innerHTML="confirm";
	theButton.onclick=function(){
		confirmPair(theButton);
	};
}


function changeCurrentPair(theButton){
	
	if(lastPair!==null){
		return;
	}
	
	lastPair=currentPair;
	var theRef=theButton.parentElement.Ref;
	var theMov=theButton.parentElement.Mov;
	var pos=0;
	var lim=assemblyPairs.length;
	while(pos<lim){
		if(assemblyPairs[pos].Ref.Name===theRef & assemblyPairs[pos].Mov.Name===theMov){
			currentPair=assemblyPairs[pos];
			return;
		}
		pos++;
	}
	
}


function grab(theTree,theMember){

	if($(theTree).children(theMember).length!=0){
		return $(theTree).children(theMember)[0];
	}
	else{
		return null;
	}

}


var time=0;
var focusBox;
var focusPoint;

var render = function () {

	// The function that will manage frame requests
	requestAnimationFrame( render );
	
	if(lastPair!==null){
		deHighlight(lastPair);
		highlight(currentPair);
		lastPair=null;
	}
	
	currentPair.Ref.Mesh.geometry.computeBoundingBox();
	currentPair.Mov.Mesh.geometry.computeBoundingBox();
	focusBox=currentPair.Ref.Mesh.geometry.boundingBox.clone();
	focusBox.union(currentPair.Mov.Mesh.geometry.boundingBox);
	
	focusPoint= new THREE.Vector3(
								  (focusBox.min.x+focusBox.max.x)/2,
								  (focusBox.min.y+focusBox.max.y)/2,
								  (focusBox.min.z+focusBox.max.z)/2
								 );
	
	thePos.normalize();
	
	thePos.applyEuler(theEul);
	theEul.set(0,0,0,'XYZ');
	console.log(thePos);
	thePos.multiplyScalar(theDistance);
	camera.position.copy(thePos);
	camera.position.add(focusPoint);
	camera.lookAt(focusPoint);
	camera.updateMatrix();
	
	sunLight.position.set( (camera.position.x-focusPoint.x)*2+focusPoint.x,
						   (camera.position.y-focusPoint.y)*2+focusPoint.y, 
						   (camera.position.z-focusPoint.z)*2+focusPoint.z );
	sunLight.target.position=focusPoint;
	
	
	time+=0.01;
		
	
	// Call for the render
	renderer.render(scene, camera);
};











/**
*
* Accepts a string and outputs the string of all characters following the final '.' symbol
* in the string. This is used internally to extract file extensions from file names.
*
* @method grabExtension
* @for dirConfirmGlobal
* @param {String} theName The file name to be processed
* @return {String} the extension in the given file name. If no extension is found, the 
* 'undefined' value is returned.
* 
*/
function grabExtension(theName){
	return (/[.]/.exec(theName)) ? /[^.]+$/.exec(theName) : undefined;
}

// Returns from the given list of file readers those that have not completed loading
function whoIsLeft(theReaders){

	var pos=0;
	var lim=fileReaders.length;
	var theList=[];
	while(pos<lim){
		if(theReaders[pos].Reader.readyState!=2){
			theList.push(theReaders[pos].Name);
		}
		pos++;
	}
	console.log(theList);

}




/**
*
* Accepts a fileinput event, presumably from a file upload event listener, and assigns
* functions to each file reader listed in the event to be called upon the full loading
* of that given reader's files 
*
* @method readMultipleFiles
* @for dirConfirmGlobal
* @param {Event} evt A fileinput event, to be given by a fileinput event listener
* @return {Void}
* 
*/
function readMultipleFiles(evt) {
	//Retrieve all the files from the FileList object
	var files = evt.target.files; 
			
	if (files) {
		for (var i=0, f; f=files[i]; i++) {
			
			var r = new FileReader();
			var extension=grabExtension(f.name)[0];
			//console.log(f.name);
			
			if(extension===undefined){
				continue;
			}
			if(extension.toLowerCase()==="stl"){
				r.onload = (function(f) {
					return function(e) {
					//console.log(f.name);
						var contents = e.target.result;
						if(r.result!=null){
							STLs.push(r.result);
						}
						loadParts();
					};
				})(f);
				r.readAsArrayBuffer(f);
				fileReaders.push({Reader: r, Name: f.name});
			}
			else if(extension.toLowerCase()==="xml"){
				console.log(f.name);
				if(!(theXML===null)){
					console.log("Warning: More than one XML file provided");
				}
				r.onload = (function(f) {
					return function(e) {
						//console.log(f.name);
						var contents = e.target.result;
						theXML=e.target.result;
						loadParts();
					};
				})(f);
				r.readAsText(f,"US-ASCII");
				fileReaders.push({Reader: r, Name: f.name});
			}
						
		}
		console.log(fileReaders);
	} 
	else {
		  alert("Failed to load files"); 
	}
}


// Inserts the file loading manager into the document
document.getElementById('fileinput').addEventListener('change', readMultipleFiles, false);


/**
*
* Called internally upon every recieved fileload event. Checks if every file reader in the 
* array "fileReaders" has fully read each of their files. If so, then the function converts
* all recieved stl files into threeJS models and executes "renderParts".
*
* @method loadParts
* @for dirConfirmGlobal
* @return {Void}
* 
*/
function loadParts (){

	
		// Looks for unloaded files
		var pos=0;
		var lim=fileReaders.length;
		while(pos<lim){
			if(!(fileReaders[pos].Reader.readyState===2)){
				//console.log(pos);
				//console.log(fileReaders[pos].Name);
				break;
			}
			pos++;
		}
	
	
	// Executes if all files are loaded
	if(pos===lim){
		console.log("ALL DONE");
		parts.length=0;
		pos=0;
		var partGeom=null;
		var partMesh;
		var theCenter;
		var ext;
		while(pos<lim){
			ext=grabExtension(fileReaders[pos].Name)[0];

			if(ext.toLowerCase()==="stl"){
				
				partGeom=parseStl(fileReaders[pos].Reader.result);
				if(partGeom===null){
					partGeom=parseStlBinary(fileReaders[pos].Reader.result);
				}
				
				//console.log(partGeom);
				
				partMesh=new THREE.Mesh( 
						partGeom,
						new THREE.MeshLambertMaterial(wireSettings)
				);
				parts.push({
					Mesh: partMesh,
					Name: fileReaders[pos].Name
				})
				scene.add(partMesh);	
			}
			
			pos++;
		}
		
		getAssemblyPairs();
		linkParts();
		console.log(assemblyPairs);
		highlight(assemblyPairs[0]);
		lastPair=assemblyPairs[0];
		currentPair=assemblyPairs[0];
		console.log("setting up currentPair");
		console.log(currentPair);
		insertAssemblyPairs();
		render();
		
	}
	

}

function linkPair(a,b,vec){
	
	var thePair={Ref: null, Mov: null, Vec: null};
	
	//console.log(parts);
	
	var pos=0;
	var lim=parts.length;
	while(pos<lim){
		//console.log(a);
		//console.log(b);
		//console.log(vec);
		//console.log(parts[pos].Name);
		if(parts[pos].Name===a){
			thePair.Ref=parts[pos];
		}
		if(parts[pos].Name===b){
			thePair.Mov=parts[pos];
		}
		if(thePair.Ref!=null & thePair.Mov!=null){
			thePair.Vec=vec;
			return thePair;
		}
		pos++;
	}
	//console.log(thePair);
	return null;
	
}

function linkParts(){
	
	var pos=0;
	var lim=namePairs.length;
	var thePair=null;
	//console.log(namePairs);
	while(pos<lim){
		thePair=linkPair(namePairs[pos].Ref,namePairs[pos].Mov,namePairs[pos].Vec);
		if(thePair!=null){
			assemblyPairs.push(thePair)
		}
		pos++;
	}
	//console.log(assemblyPairs);
}

function getAssemblyPairs(){
	
	//console.log(theXML);
	var doc = $.parseXML(theXML);
	//console.log(doc);
	var thePairs=grab(doc,"pairList");
	//console.log(thePairs);

	thePairs=$(thePairs).children("pair");
	//console.log(thePairs);
	var pos=0;
	var lim=thePairs.length;
	var thePair;
	var theMov;
	var theRef;
	var theVec;
	while(pos<lim){
		theRef=grab(thePairs[pos],"reference");
		theMov=grab(thePairs[pos],"moving");
		theVec=grab(thePairs[pos],"vector");
		namePairs.push({
			Ref: $(theRef).attr("name"),
			Mov: $(theMov).attr("name"),
			Vec: {
				X: $(theVec).attr("x"),
				Y: $(theVec).attr("y"),
				Z: $(theVec).attr("z")
			}
		});
		pos++;
	}
	//console.log(namePairs);
	
}


function insertAssemblyPairs(){
	
	var pos=0;
	var lim=assemblyPairs.length;
	console.log("Logging Assembly Pairs")
	console.log(lim);
	console.log(assemblyPairs);
	while(pos<lim){
		console.log("logging a pair");
		console.log(pos);
		console.log(assemblyPairs[pos]);
		var theDiv = document.createElement("div");
		var theText = document.createElement("text");
		theText.innerHTML = assemblyPairs[pos].Ref.Name + " <--- " + assemblyPairs[pos].Mov.Name;
		var theConfBut = document.createElement("button");
		theConfBut.innerHTML = "confirm";
		theConfBut.onclick = function (){
			confirmPair(this);
		}
		var theHighlightBut = document.createElement("button");
		theHighlightBut.innerHTML = "focus";
		theHighlightBut.onclick = (function(position){
			return function(){
				lastPair=currentPair;
				console.log("assigning currentPair");
				console.log(position);
				console.log(assemblyPairs[position]);
				currentPair=assemblyPairs[position]; 
				console.log ("Doing the focus thing");
			}
		})(pos);
		theDiv.appendChild(theText);
		theDiv.appendChild(theHighlightBut);
		theDiv.appendChild(theConfBut);
		document.getElementById("unconfirmed").appendChild(theDiv);
		pos++;
	}
	pos--;
	
}


function deHighlight(thePair){
	console.log(thePair);
	thePair.Ref.Mesh.material=new THREE.MeshLambertMaterial(wireSettings);
	thePair.Mov.Mesh.material=new THREE.MeshLambertMaterial(wireSettings);
}

function highlight(thePair){
	console.log(thePair);
	thePair.Ref.Mesh.material=new THREE.MeshLambertMaterial({color: 0x4444FF /*, transparent: true, opacity: 0.6, depthTest: false */});
	thePair.Mov.Mesh.material=new THREE.MeshLambertMaterial({color: 0xFF4444 /*, transparent: true, opacity: 0.6, depthTest: false */});
	thePair.Ref.Mesh.geometry.computeBoundingBox();
	thePair.Mov.Mesh.geometry.computeBoundingBox();
	var theBox=thePair.Ref.Mesh.geometry.boundingBox.clone();
	theBox.union(thePair.Mov.Mesh.geometry.boundingBox);
	theVec.geometry.vertices[0]=new THREE.Vector3(
								  (theBox.min.x+theBox.max.x)/2,
								  (theBox.min.y+theBox.max.y)/2,
								  (theBox.min.z+theBox.max.z)/2
								 );
	theVec.geometry.vertices[1]=new THREE.Vector3(100*thePair.Vec.X,100*thePair.Vec.Y,100*thePair.Vec.Z);
	theVec.geometry.vertices[1].add(theVec.geometry.vertices[0]);
	theVec.geometry.verticesNeedUpdate=true;
}


function fixOpacity(theSlider){
	
	console.log("Changing Opacity");
	var val = theSlider.value;
	wireSettings.opacity=val;
	var pos=0;
	var lim=parts.length;
	while(pos<lim){
		if(parts[pos].Mesh!=currentPair.Ref.Mesh && parts[pos].Mesh!=currentPair.Mov.Mesh){
			parts[pos].Mesh.material=new THREE.MeshLambertMaterial(wireSettings);
		}
		pos++;
	}
	
}


function doMouseUp(){
	dragInp=false;
}

function doMouseDown(){
	dragInp=true;
}

function doMouseLeave(){
	dragInp=false;
	lastMouse=null;
}

function doDrag(theEvent){
	if(dragInp==true){
		thePos.normalize();
		theEul.set(theEvent.movementY*(-0.02)*Math.cos(Math.atan2(thePos.x,thePos.z)),
				   theEvent.movementX*(-0.01),
				   theEvent.movementY*(0.02)*Math.sin(Math.atan2(thePos.x,thePos.z))
				   ,'ZYX'); 
	}
}

document.getElementById("display").addEventListener("mousemove", doDrag);



