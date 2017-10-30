
//  Array for processed parts
var parts=[];

// The XML data for the current stage
var inText="";

// The XML data to be delivered to the server
var outText="";

// The current stage
var stage = 0;

// The time since the last check in with the server
var lastCheckin = 0;

// The amount of time the server should wait between checkins
var checkinWait = 512;

// The progress of the web page in loading the file
var prog = 0;

// The ID number of the current client/server session
var sessID = 0;

var fileReaders = [];

var STLs = [];

var manualFileInput=true;

// Holder for parsed-in XML documents
var theXML=null;

// Array for storing fileReaders to keep track of them
var fileReaders=[];

// Array for processed STLs
var STLs=[];

// Holder for animation frames for parts
var partFrames=null;

// Sets the time to 0, for the sake of starting the animation at the right time
var theTime=0;


// Holder for parsed-in javascript objects from the XML document
var theTreequence=null;

var treequenceActive = false;


var timeAdjustment = 0;


var standard = false;

var modelNum = 0;
var stlNum = 0;
var uploadNum = 0;


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




// Adding in one more light
var sunLight = new THREE.SpotLight( 0xaa5533, 6, 32000, 1.2, 1, 1 );
		sunLight.position.set( 4000, 4000, 4000 );



var theFog=new THREE.Fog( skyColor, 4000, 6000 );

// The tree structure holding animation data
var movementTree=null;
var theCenter= new THREE.Vector3(0,0,0);

// The part directly in front of the camera, if any such part exists
var objectOfInterest=null;

// The part being locked onto by using the 'F' key
var focusPoint = null;
var focusPart = null;
var focusRow = null;

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

var camera;

// The momentum of the camera
var momentum= new THREE.Vector3(0,0,0);

scene = new THREE.Scene();



// Setting up the renderer with the default color
renderer = new THREE.WebGLRenderer();
renderer.setClearColor( skyColor, 1 );

var render;
var doDrag;

var theXAxis=null;
var theYAxis=null;
var theZAxis=null;
var xRet=null;
var yRet=null;


var theTable;


// Some HTML bits to insert into the part properties table as needed

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
function grabName(theName){
	return theName.substr(0, theName.lastIndexOf('.')) || theName;
}


function advanceStage(response,status){

	if(status === "success"){
		//console.log(response.responseText);
		var contentElem = document.getElementById("stageContent");
		contentElem.innerHTML = response.responseText;
		if(stage === 1 || stage === 3 || stage === 5 || stage === 7){
			checkinWait = 512;
			execute();
			setTimeout(checkIn,checkinWait);
		}
		(startupScripts[stage])();
		//console.log(startupScripts[stage]);
	}
    else{
        alert("Server returned status '"+status+"'");
    }

}


function execResp(response,status){

	if(status === "success"){
		console.log("Executed for stage "+stage);
	}
    else{
        alert("Server returned status '"+status+"'");
    }

}


function updateProg(response,status){

	console.log("Update Prog Status:" + status);
	if(status === "success"){
		var resp = response.responseJSON;
		//console.log(resp);
		if(resp.failed){
			alert("Something went wrong on the server, and so this process may not continue. Please contact the webmaster.");
		}
		if(resp.progress <= prog){
			checkinWait = checkinWait * 2;
		}
		else{
			checkinWait = checkinWait / 2;
		}
		if(resp.data === null /*Number.parseInt(resp.progress) < 100*/){
			updateLoad();
			setTimeout(checkIn,checkinWait);
			return;
		}
		else{
			inText = resp.data;
			//console.log(inText);
			updateLoad();
			requestAdvance(stage+1);
			return;
		}
	}
	else{
		//alert("Server returned status '"+status+"'");
		alert("Server did not return success");
	}

}


function giveModelsResponse(response,status){

	if(status === "success"){
		var sucVal = (response.responseJSON.success !== true);
		if(sucVal){
			alert("Failed to upload models.");
		}
		else{
			uploadNum++;
		}
		if(uploadNum < modelNum){
			return;
		}
		requestAdvance(1);
	}
	else{
		//alert("Server returned status '"+status+"'");
		alert("Server did not return success");
	}

}

function setID(response,status){

	if(status === "success"){
		sessID = response.responseJSON.sessID;
		console.log("Server assigned ID: " + response.responseJSON.sessID);
	}
	else{
		//alert("Server returned status '"+status+"'");
		alert("Server did not return success");
	}

}


function requestAdvance(reqStage){

	stage = reqStage;
	console.log("--->>"+reqStage);
	$.ajax({
		complete: advanceStage,
		dataType: "text",
		method: "GET",
		timeout: 10000,
		url: "/stage/"+reqStage
	});

}

function checkIn(){

	console.log("Sending out check in");
    $.ajax({
        complete: updateProg,
        dataType: "json",
        method: "POST",
        timeout: 10000,
        url: "/checkIn",
        data: {
            stage: stage,
            sessID: sessID,
            textData: outText
        }
    });

}

function execute(){

	console.log("Sending out check in");
    $.ajax({
        complete: execResp,
        dataType: "json",
        method: "POST",
        timeout: 10000,
        url: "/exec",
        data: {
            stage: stage,
            sessID: sessID,
            textData: outText
        }
    });

}

function giveModels(){
	if(STLs.length < modelNum){
		return;
	}
	console.log("Sending off models");
	var pos = 0;
	while(pos < modelNum){
		$.ajax({
			complete: giveModelsResponse,
			contentType: "application/json;charset=UTF-8",
			dataType: "json",
			method: "POST",
			timeout: 10000,
			url: "/giveModel",
			data: JSON.stringify( { sessID: sessID, Model: STLs[pos] } )
		});
		pos++;
	}

}

function getID(){

	$.ajax({
		complete: setID,
		dataType: "json",
		method: "POST",
		timeout: 10000,
		url: "/getID"
	});

}

function spinOff(func) {
    setTimeout(func, 0);
}

function clearScene( dispID ){




	var theWidth=document.getElementById(dispID).clientWidth;
	var theHeight= document.getElementById(dispID).clientHeight;

	// The camera
	camera = new THREE.PerspectiveCamera( 75, theWidth/theHeight, 1, 16000 );

	renderer.setSize(theWidth,theHeight);
	document.getElementById(dispID).appendChild( renderer.domElement );

	// Adding in a whole bunch of lights for the scene, so the parts are well-lit
	var directionalLight = new THREE.DirectionalLight( 0xBBBBBB );
			directionalLight.position.x = 0;
			directionalLight.position.y = 0;
			directionalLight.position.z = 1;
			directionalLight.position.normalize();
			scene.add( directionalLight );

	var directionalLight = new THREE.DirectionalLight( 0xBBBBBB );
			directionalLight.position.x = 0;
			directionalLight.position.y = 1;
			directionalLight.position.z = 0;
			directionalLight.position.normalize();
			scene.add( directionalLight );

	var directionalLight = new THREE.DirectionalLight( 0xBBBBBB );
			directionalLight.position.x = 1;
			directionalLight.position.y = 0;
			directionalLight.position.z = 0;
			directionalLight.position.normalize();
			scene.add( directionalLight );
	var directionalLight = new THREE.DirectionalLight( 0xBBBBBB );
			directionalLight.position.x = 0;
			directionalLight.position.y = 0;
			directionalLight.position.z = -1;
			directionalLight.position.normalize();
			scene.add( directionalLight );

	var directionalLight = new THREE.DirectionalLight( 0xBBBBBB );
			directionalLight.position.x = 0;
			directionalLight.position.y = -1;
			directionalLight.position.z = 0;
			directionalLight.position.normalize();
			scene.add( directionalLight );

	var directionalLight = new THREE.DirectionalLight( 0xBBBBBB );
			directionalLight.position.x = -1;
			directionalLight.position.y = 0;
			directionalLight.position.z = 0;
			directionalLight.position.normalize();
			scene.add( directionalLight );


/*
	sunLight.position.set( 4000, 4000, 4000 );
	scene.add( sunLight );

*/

	var theFog=new THREE.Fog( skyColor, 4000, 6000 );
	scene.fog=theFog;

	
	

}

getID();
requestAdvance(0);
