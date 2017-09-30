;



if( typeof(startupScripts) == 'undefined'){

	var startupScripts = [
		function(){},
		function(){},
		function(){},
		function(){},
		function(){},
		function(){},
		function(){},
		function(){}
	];

}


/**
*
* Class containing all the methods used in the 3d visual assembly
* @class renderGlobal
* @static
*/




// Put recieved data about assembly into here. The code handles the rest.
// theXMLFile should be a string, and theSTLFiles as a binary ArrayBuffer
// Any text-based STL files should be in an 8-bit encoding
/**
*
* The function which handles the actual rendering of the solution file animation
* and loading in the models
*
* @method recieveData
* @for renderGlobal
* @param {String} theXMLFile
* @param {Object} theSTLFiles
* @return {Void}
*
*/
function receiveData(theXMLFile, theSTLFiles){

	theXML=theXMLFile;

	parts.length=0;
	var pos=0;
	var lim=theSTLFiles.length;
	var partGeom;
	var partMesh;

	while(pos<lim){
		partGeom=null;
		partGeom=theSTLFiles[pos];
		if(partGeom===null){
			partGeom=parseStlBinary(fileReaders[pos].Reader.result);
		}

		//console.log(partGeom);

		partMesh=new THREE.Mesh(
				partGeom,
				new THREE.MeshNormalMaterial( )
		);
		parts.push({
			Mesh: partMesh,
			Name: fileReaders[pos].Name
		})
		scene.add(partMesh);

		pos++;
	}

	renderParts();

}









// Dialates time with the scrolling of the mouse
function zoomIt(e){

	//zoom=zoom*Math.pow(1.001,e.wheelDelta);
	var theDelta = e.deltaY == 0 ? 0 : ( e.deltaY > 0 ? 1 : -1 );
	zoom+=theDelta*(-1);

}


/**
*
*  Attempts to lock the mouse for camera manupulation
*
* @method tryMouseLock
* @for renderGlobal
* @return {Void}
*
*/
function tryMouseLock(){

	var element= document.getElementById("theDisplay");

	element.requestPointerLock = element.requestPointerLock ||
		 element.mozRequestPointerLock ||
		 element.webkitRequestPointerLock;
	// Ask the browser to lock the pointer
	element.requestPointerLock();

}



/**
*
* Adds or Removes event listeners for input meant for camera manipulation.
* Is triggered by a change in the state of the mouse locking or unlocking.
* The mouse locking results in input listeners being added to the webpage,
* whereas the unlocking of the mouse results in the removal of appropriate
* listeners.
*
* @method lockChange
* @for renderGlobal
*
*
* @param {Event} e The event that is to be applied to the function by event listeners upon
* a change in the state of mouselock
* @return {Void}
*
*
*/
function lockChange(e){

	var theTarget=document.getElementById("theDisplay");
	if (document.pointerLockElement === theTarget ||
		document.mozPointerLockElement === theTarget ||
		document.webkitPointerLockElement === theTarget) {
		// Pointer was just locked
		// Enable the mousemove listener
		document.addEventListener("mousemove", mouseMoved, false);
		pointerIsLocked=true;
	}
	else {
		// Pointer was just unlocked
		// Disable the mousemove listener
		document.removeEventListener("mousemove", mouseMoved, false);
		pointerIsLocked=false;
	}

}



/**
*
* Changes the orientation of the camera based off of the mouse movement
* contained in the supplied mouse movement event. X-axis movement
* corresponds to change in the yaw of the camera whereas Y-axis movement
* corresponds to a change in the pitch of the camera.
*
* @method mouseMoved
* @for renderGlobal
*
* @param {Event} e The mouse movement event to be supplied to the function by a mouse
* movement event listener on the web page
* @return {Void}
*
*/
function mouseMoved(e){

	var movementX = e.movementX ||
		  e.mozMovementX        ||
		  e.webkitMovementX     ||
		  0;
	var movementY = e.movementY ||
		  e.mozMovementY        ||
		  e.webkitMovementY     ||
		  0;

	camPitch-=movementY/400;
	if(camPitch> Math.PI/2){
		camPitch= Math.PI/2;
	}
	else if(camPitch< Math.PI/(-2)){
		camPitch= Math.PI/(-2)
	}

	camYaw-=movementX/400;
	if(camPitch> Math.PI){
		camPitch= Math.PI;
	}
	else if(camPitch< Math.PI*(-1)){
		camPitch= Math.PI*(-1);
	}

}



// Changes key press states based off of key presses
/**
*
* Accepts a key press event and, if the key press corresponds to one
* of the keys used for manipulating the view, sets the proper components
* of "inputState" to true.
*
* @method registerDown
* @for renderGlobal
*
*
* @param {Event} e The key down event to be supplied to the function by a key down event
* listener on the web page
* @return {Void}
*
*/
function registerDown(e){



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
		inputState.A=true;
	}
	if(theKey=='S'){
		inputState.S=true;
	}
	if(theKey=='D'){
		inputState.D=true;
	}
	if(theKey=='W'){
		inputState.W=true;
	}
	if(theKey==' '){
		inputState.Space=false;
	}
	if(theKey=='Q'){
		inputState.Q=true;
	}
	if(theKey=='E'){
		inputState.E=true;
	}
	if(theKey=='R'){
		inputState.R=true;
	}
	if(theKey=='F'){
		inputState.F=true;
	}
	return;

}




/**
*
* Accepts a key press release and, if the key release corresponds to one
* of the keys used for manipulating the view, sets the proper components
* of "inputState" to false
*
* @method registerUp
* @for renderGlobal
*
*
* @param {Event} e The key up event to be supplied to the function by a key up event
* listener on the web page
* @return {Void}
*
*/
function registerUp(e){


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
	if(theKey==' '){
		inputState.Space=false;
	}
	if(theKey=='Q'){
		inputState.Q=false;
	}
	if(theKey=='E'){
		inputState.E=false;
	}
	if(theKey=='R'){
		inputState.R=false;
	}
	if(theKey=='F'){
		inputState.F=false;
	}
	return;

}


// Affects the state of the camera/animation based off of the state of the inputs
/**
*
* Once called, interprets the current state of registered inputs and manipulates
* the visualization accordingly, including the accelleration of the camera, as
* affected by the W,S,A, and D keys, and the rotation of the camera if the F key
* is depressed and there currently is a highlighted object of interest
*
* @method manageControls
* @for renderGlobal
* @return {Void}
*
*/
function manageControls(){

	// Set up rotation and relative position deltas
	var theRot= new THREE.Quaternion(0,0,0,0);
	theRot.setFromEuler(camera.rotation);
	var theDir= new THREE.Vector3(0,0,0);


	//
	if(inputState.A==true){
		theDir.x-=1;
	}
	if(inputState.S==true){
		theDir.z+=1;
	}
	if(inputState.D==true){
		theDir.x+=1;
	}
	if(inputState.W==true){
		theDir.z-=1;
	}
	if(inputState.Space==true && inputState.switchPrimed==true){
		inputState.switchPrimed = false;
		treequenceActive = !treequenceActive;
	}
	if(inputState.Space==false){
		inputState.switchPrimed = true;
	}
	if(inputState.Q==true){
		momentum.y-=1;
	}
	if(inputState.E==true){
		momentum.y+=1;
	}
	if(inputState.R==true){
		theTime=0;
	}

	if(theDir.length()>0.1){

		if(theBoost<boostLim){

			theBoost+=boostInc;

		}
		else{

			theBoost=boostLim;

		}

	}
	else{

		theBoost=1;

	}

	theDir.applyQuaternion(theRot);
	theDir.multiplyScalar(theSpeed*theBoost);

	momentum.x+=theDir.x;
	momentum.y+=theDir.y;
	momentum.z+=theDir.z;

	camera.position.x+=momentum.x;
	camera.position.y+=momentum.y;
	camera.position.z+=momentum.z;




	if(inputState.F==true){
		if(focusPoint==null && objectOfInterest!=null){
			focusPoint=objectOfInterest;
		}
		if(focusPoint!=null){

			camera.lookAt(getPartCenter(focusPoint));
			camPitch=camera.rotation.x;
			camYaw=camera.rotation.y;
		}
	}
	else{
		if(focusPoint!=null){
			focusPoint.Mesh.material= getStdMaterial();
			focusPoint=null;
		}
	}

	if(focusPoint==null){
		camera.rotation.x=camPitch;
		camera.rotation.y=camYaw;
	}


}




/**update
*
* The main portion of the visualization's rendering cycle, managing frame rate,
* input, camera decelleration, keyframe manipulation, model animation, object highlighting,
* and informational display.
*
* @method render
* @for renderGlobal
* @return {Void}
*
*/
var render = function () {

	// The function that will manage frame requests
	requestAnimationFrame( render );



	// Recieve input and set the appropriate state
	manageControls();

	// Apply air friction to camera
	momentum.multiplyScalar(theDrag);


	// Moves the parts along the appropriate motions of the animation
	if(zoom>=0){
		theTime=animate(partFrames,theTime,Math.pow(zoom,1.008),treequenceActive);
	}
	else{
		theTime=animate(partFrames,theTime,0-Math.pow(0-zoom,1.008),treequenceActive);
	}


	// Reset the appearence of the last object of interest
	if(objectOfInterest!=null){
		objectOfInterest.Mesh.material=getStdMaterial();
	}

	// Get the first part being directly looked at and sets it as object of interest
	objectOfInterest=getFirstIntersect(scene,camera,partFrames);


	// Change appearence of the object of interest and display the appropriate information
	if(objectOfInterest!==null && standard !== true){

		mouseOverText=" "+objectOfInterest.Name.substring(0,objectOfInterest.Name.length-4);
		objectOfInterest.Mesh.material=new THREE.MeshStandardMaterial({
			color:0xbbbbbb,
			roughness: 1.0,
			metalness: 1.0,
			shading: THREE.SmoothShading
		} );

	}
	else{

		mouseOverText="";

	}


	// Change appearence of the focus point mesh
	if(focusPoint!=null && standard !== true){

		focusPoint.Mesh.material=new THREE.MeshStandardMaterial({
			color:0xff6666,
			roughness: 1.0,
			metalness: 1.0,
			shading: THREE.SmoothShading
		} );

	}


	// Display information about the object of interest
	document.getElementById("mouseoverName").innerHTML="PART: "+mouseOverText;
	document.getElementById("theTime").innerHTML=("TIME: "+ theTime.toFixed(10)).toString();

	// Update the installation trace lines
	updateLines(movementTree,null,theTime-timeAdjustment,false,treequenceActive);

	updateAxisLines();


	// Call for the render
	renderer.render(scene, camera);

};



/**
*
* Accepts a string and outputs the string of all characters following the final '.' symbol
* in the string. This is used internally to extract file extensions from file names.
*
* @method grabExtension
* @for renderGlobal
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
* Called internally by "loadParts". Parses the text stored in "theXML" into a tree
* structure composed of nested javascript objects and converts that structure into a
* series of keyframe arrays, each of which are stored alongside their respective
* parts in "partFrames". Additionally, generates the path lines for each subassembly
* and inserts those lines into "scene".
*
* @method renderParts
* @for renderGlobal
* @return {Void}
*
*/
function renderParts(){



	// Cuts of the common first characters from all the part names
	cutoffPartNames(parts);

	// Parses in the xml of the treequence
	//console.log(theXML);
	var treeQ = $.parseXML(theXML);
	//console.log(treeQ);
	treeQ=grab(treeQ,"AssemblyCandidate");
	//console.log(treeQ);
	treeQ=grab(treeQ,"Sequence");
	//console.log(treeQ);
	treeQ=grab(treeQ,"Subassemblies");
	//console.log(treeQ);
	treeQ=grab(treeQ,"SubAssembly");

	// Turns the treequence into a tree storing the movement data of each subassembly
	console.log(new THREE.Vector3(0,0,0));
	var moveTree=getMovement(treeQ,0,0,0,new THREE.Vector3(0,0,0),0);
	console.log(moveTree);

	// Cuts off the common first characters of all the part names in the tree
	cutOffNames(moveTree,similarityCutoff(getNameList(moveTree)));
	//console.log(moveTree);
	//printAllNames(parts,moveTree);

	// Makes a series of keyframes for each part for evaluation in the animation
	var theFrameLists=makeKeyFrames(moveTree,[],[]);
	timeAdjustment = addCurveKeyFrames( theFrameLists, new THREE.Vector3 ( 1000,1000,1000 ) );
	//bumpTreeTimes(moveTree,10*timeAdjustment);
	//console.log(theFrameLists);
	//console.log(parts);

	// Links each key frame list object to the appropriate part object
	partFrames= bindPartsToKeyFrames(theFrameLists,parts);
	//console.log(partFrames.length.toString());
	//console.log(partFrames.length.toString());
	//console.log(partFrames);
	//showFrames(theFrameLists);

	// Zeroes the time, for obvious reasons
	theTime=0;

	// Adds the movement trace lines to the scene
	addLines(moveTree,null,scene,false);

	// Stores the movement tree for later use
	movementTree=moveTree;

	// Mirrors the time measurements at each keyfram to turn the dissassembly into
	// an assembly animation
	flipTreeTime(movementTree,getLongestTime(movementTree));
	//console.log(partFrames.length.toString());
	addDisplacement(movementTree, partFrames, 0);

	// Populates the treequence graphic
	document.getElementById("treequenceDiv").classList.add("refBranch");
	insertTreequenceHTML(movementTree,document.getElementById("treequenceDiv"));
	showHideTreequence();

	// Fixes a minor thing in the treequence graphic
	getChildrenByTag(document.getElementById("treequenceDiv"),"BUTTON")[0].innerHTML="+";
	getChildrenByTag(document.getElementById("treequenceDiv"),"DIV")[0].classList.add("rootNode");
	// Begins to display the parts
	initAxisLines();

	alignAssemblyCenter();

	addGrid(50000,500, -1000, 0x888888);
	addGrid(50000,500, 8000, 0x888888);

	var pos = 0;
	while(pos<100){
		addCylender(200, -1000, 8000, (pos%10)/10*50000-25000, pos/10/10*50000-25000, 8, 12, 0x888888);
		pos++;
	}

	render();

}




// Toggles the display of the treequence graphic
/**
*
* Toggles the display of the HTML div element containing the treequence representation of
* the assembly.
*
* @method showHideTreequence
* @for renderGlobal
* @return {Void}
*
*/
function showHideTreequence(){

	TDiv=document.getElementById("treequenceDiv");
	if(TDiv.state=="shown"){
		TDiv.state="notShown";
		TDiv.classList.remove("shown");
		TDiv.classList.add("hidden");
	}
	else{
		TDiv.state="shown";
		TDiv.classList.remove("hidden");
		TDiv.classList.add("shown");
	}

}



startupScripts[6] = function(){
	//
	//    Pretty Important: Keep this as true unless/until you've incorperated some other
	//                      method of getting file input/output
	//
	var manualFileInput=true;


	if(manualFileInput==true){

		document.getElementById("HUD").innerHTML="<input type='file' id='fileinput' multiple />"+document.getElementById("HUD").innerHTML;

	}



	// Holder for parsed-in XML documents
	var theXML=null;

	// Array for storing fileReaders to keep track of them
	var fileReaders=[];

	// Array for processed STLs
	var STLs=[];

	//  Array for processed parts
	var parts=[];

	// Holder for animation frames for parts
	var partFrames=null;

	// Sets the time to 0, for the sake of starting the animation at the right time
	var theTime=0;


	// Holder for parsed-in javascript objects from the XML document
	var theTreequence=null;

	var treequenceActive = false;


	var timeAdjustment = 0;

	var standard = false;


	// Holds the state of button press inputs to smooth out control response



	/**
	*
	* Contains a representation of the last keyboard events reported by the
	* web page for each given key that acts as input for manipulating the
	* visulization: 'W','A','S','D','R','F', and the 'Space' key
	*
	* @element inputState
	* @for renderGlobal
	* @return {Void}
	*
	*/
	var inputState={

		W: false,
		A: false,
		S: false,
		D: false,
		R: false,
		F: false,
		Q: false,
		E: false,
		Space: false,
		switchPrimed: false

	}


	// The color of the background of the scene
	var skyColor= 0xFFFFFF;

	if(standard){
		skyColor = 0x000000;
	}

	// The tree structure holding animation data
	var movementTree=null;
	var theCenter= new THREE.Vector3(0,0,0);

	// The part directly in front of the camera, if any such part exists
	var objectOfInterest=null;

	// The part being locked onto by using the 'F' key
	var focusPoint=null;

	// Name of the part being looked at, if there is any such part
	var mouseOverText="";

	// Time dialation coefficeint
	var zoom=0.2;

	// Base Accelleration
	var theSpeed=0.2;

	// Accelleration bonus variables
	var theBoost=1; // Initial accelleration bonus
	var boostLim=25; // Limit to accelleration bonus
	var boostInc=0.1; // Rate of accelleration bonus increase

	// Coefficient of drag camera experiences
	var theDrag=0.96;

	// Angles of camera
	var camYaw=0;
	var camPitch=Math.PI/2;

	// The momentum of the camera
	var momentum= new THREE.Vector3(0,0,0);

	// The scene of the assembly animation
	var scene = new THREE.Scene();

	// The camera
	var camera = new THREE.PerspectiveCamera( 75, window.innerWidth/window.innerHeight, 5, 30000 );
				 /*new THREE.OrthographicCamera(	10*window.innerWidth/-2, 10*window.innerWidth/2,
					 							10*window.innerHeight/-2, 10*window.innerHeight/2,
												5, 70000);*/
	camera.position.x=0;
	camera.position.z=0;
	camera.position.y=0;

	// The variable holding the state of whether or not the pointer is locked
	var pointerIsLocked=false;


	// Setting up the renderer with the default color and display size
	var renderer = new THREE.WebGLRenderer();
	renderer.setClearColor( skyColor, 1 );
	renderer.setSize( window.innerWidth*0.98, window.innerHeight*0.96);
	renderer.setFaceCulling(THREE.CullFaceNone,THREE.FrontFaceDirectionCCW);
	document.body.appendChild( renderer.domElement );

	// Setting camera to Yaw-Pitch-Roll configuration
	camera.rotation.reorder('YXZ');


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
	var sunLight = new THREE.SpotLight( 0xaa5533, 6, 32000, 1.2, 1, 1 );
			sunLight.position.set( 4000, 4000, 4000 );
			scene.add( sunLight );



	var theFog=new THREE.Fog( skyColor, 4000, 6000 );
	scene.fog=theFog;

	var theXAxis=null;
	var theYAxis=null;
	var theZAxis=null;
	var xRet=null;
	var yRet=null;




	// Adding a whole bunch of event listeners for input
	document.getElementById("theDisplay").addEventListener("wheel", zoomIt);
	document.addEventListener('pointerlockchange', lockChange, false);
	document.addEventListener('mozpointerlockchange', lockChange, false);
	document.addEventListener('webkitpointerlockchange', lockChange, false);
	document.addEventListener('keydown', registerDown , false);
	document.addEventListener('keyup', registerUp , false);

	renderParts();
	
}
