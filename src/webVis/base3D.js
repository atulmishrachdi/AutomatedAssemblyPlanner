

// Array for storing fileReaders to keep track of them
var fileReaders=[];

// Array for processed STLs
var STLs=[];

//  Array for processed parts
var parts=[];


var theWidth=document.getElementById("display").clientWidth;
var theHeight= document.getElementById("display").clientHeight;


// The scene of the assembly animation
var scene = new THREE.Scene();

// The camera
var camera = new THREE.PerspectiveCamera( 75, theWidth/theHeight, 1, 16000 );


var theXAxis = null;
var theYAxis = null;
var theZAxis = null;
var theAddAxis = null;
var xRet=null;
var yRet=null;

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


/**
*
* Accepts a string and outputs the string of all characters following the final '.' symbol
* in the string. This is used internally to extract file extensions from file names.
*
* @method grabExtension
* @for directionConfirmGlobal
* @param {String} theName The file name to be processed
* @return {String} the extension in the given file name. If no extension is found, the 
* 'undefined' value is returned.
* 
*/
function grabExtension(theName){
	return (/[.]/.exec(theName)) ? /[^.]+$/.exec(theName) : undefined;
}





// Returns from the given list of file readers those that have not completed loading
/**
*
* Outputs through the console the list of FileReaders in theReaders which have 
* not yet completed their loading
*
* @method whoIsLeft
* @for  directionConfirmGlobal
* @param {FileReader Object List} theReaders The list of FileReaders to be checked
* @return {Void}
* 
*/
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
* @for directionConfirmGlobal
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



/**
*
* Called internally upon every recieved fileload event. Checks if every file reader in the 
* array "fileReaders" has fully read each of their files. If so, then the function converts
* all recieved stl files into threeJS models and executes "renderParts".
*
* @method loadParts
* @for directionConfirmGlobal
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
		//console.log("ALL DONE");
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
		
		renderParts();
		
	}
	

}


// Inserts the file loading manager into the document
document.getElementById('fileinput').addEventListener('change', readMultipleFiles, false);



function pairPartsAndModels (partList,modelList) {

	var result = {};
	var pos = 0;
	var lim = partList.length;
	var modPos;
	var modLim = modelList.length;
	while(pos < lim){
		modPos = 0;
		while(){
			if(partList[pos].Name == 
			modPos++;
		}
		pos++;
	}


}







