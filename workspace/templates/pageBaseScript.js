
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




function advanceStage(response,status){

	if(response.status === 200){
		console.log(response.responseText);
		var contentElem = document.getElementById("stageContent");
		contentElem.innerHTML = response.responseText;
		if(stage === 1 || stage === 3 || stage === 5 || stage === 7){
			setInterval(updateProg);
		}
		(startupScripts[stage])();
	}

}

function updateProg(response,status){

    if(status === "success"){
        if(response.prog <= prog){
            checkinWait = checkinWait * 2;
        }
        else{
            checkinWait = checkinWait / 2;
        }
        if(response.stage === stage){
            updateLoad();
            return;
        }
        else{
			stage = response.stage;
            updateLoad();
            clearInterval(updateProg);
            requestAdvance(response.stage);
        }
    }
    else{
        alert("Experiencing Connection Problems");
    }

}

function requestAdvance(reqStage){

	console.log("--->>"+reqStage);
    $.ajax({
        complete: advanceStage,
        dataType: "json",
        method: "GET",
        timeout: 10000,
        url: "/stage/"+reqStage
    });

}

function checkIn(){

    $.ajax({
        complete: updateProg,
        dataType: "json",
        method: "POST",
        timeout: 10000,
        url: "/",
        data: {
            stage: stage,
            sessID: sessID,
            models: (stage == 0) ? parts : [],
            textData: outText
        }
    });

}

requestAdvance(0);
