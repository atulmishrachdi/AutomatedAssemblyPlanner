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


/**
*
* Accepts a fileinput event, presumably from a file upload event listener, and assigns
* functions to each file reader listed in the event to be called upon the full loading
* of that given reader's files
*
* @method readMultipleFiles
* @for renderGlobal
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
						loadParts();
					};
				})(f);
				r.readAsArrayBuffer(f);
				fileReaders.push({Reader: r, Name: f.name});
			}
		}
		//`console.log(fileReaders);
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
* @for renderGlobal
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
		spinOff(handleModels);
	}

}

function handleModels(){

	console.log("Processing model data...");
	parts.length=0;
	var pos=0;
	var lim=fileReaders.length;
	var partGeom=null;
	var partMesh;
	var theCenter;
	var ext;
	var theReader;

	while(pos<lim){
		theReader = fileReaders[pos];
		ext=grabExtension(theReader.Name)[0];
		if(ext.toLowerCase()==="stl"){
			modelNum++;
		}
		pos++;
	}

	console.log("Found "+modelNum+" models");

	pos = 0;
	while(pos<lim){
		theReader = fileReaders[pos];
		spinOff((function(r){
			return function(){
				ext=grabExtension(r.Name)[0];
				if(ext.toLowerCase()==="stl"){
					partGeom=parseStl(r.Reader.result);
					if(partGeom===null){
						partGeom=parseStlBinary(r.Reader.result);
					}
					//console.log(partGeom);
					var postMaterial =
					partMesh=new THREE.Mesh(
							partGeom,
							getStdMaterial()
					);
					parts.push({
						Mesh: partMesh,
						Name: r.Name
					});
					spinOff(handleSTLs);
				}
			}
		}) (theReader));
		pos++;
	}
	console.log("Model data processed");

}

function handleSTLs(){

	stlNum++;
	if(stlNum < modelNum){
		return;
	}
	console.log("Handling "+stlNum+" model files");

	parts.length=0;
	var pos=0;
	var lim=fileReaders.length;
	var ext;
	while(pos<lim){
		theReader = fileReaders[pos];
		spinOff((function(r){
			return function(){
				ext=grabExtension(r.Name)[0];
				if(ext.toLowerCase()==="stl"){
					STLs.push({ Name: r.Name, Data: arrayToString(r.Reader.result) });
					spinOff(giveModels());
				}
			}
		}) (theReader));
		pos++;
	}


}


startupScripts["0"] = function (){
	// Inserts the file loading manager into the document
	document.getElementById('fileinput').addEventListener('change', readMultipleFiles, false);

}
