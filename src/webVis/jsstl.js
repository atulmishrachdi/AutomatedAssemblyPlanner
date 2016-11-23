// Code copied from Github repo of tonylukasavage (and somewhat modified)
// License of this code, as stated on the repo is:
//
//    Do whatever you want with this code. I offer it without expectation or warranty. 
//    No need to credit me in your project or source code. A digital high five would be
//    nice, but is not required.
//


// Notes:
// - STL file format: http://en.wikipedia.org/wiki/STL_(file_format)
// - 80 byte unused header
// - All binary STLs are assumed to be little endian, as per wiki doc
/**
*
* Converts a given block of binary stl data (as an arraybuffer) to a threeJS representation
* of the geometry. Function courtesy of 'tonylukasavage' from Github, who released this to
* the public domain. 
*
* @method parseStlBinary
* @for renderGlobal
* @param {Arraybuffer} stl The binary stl data
* @return {Object} threeJS geometry object
* 
*/
var parseStlBinary = function(stl) {
	var geo = new THREE.Geometry();
	var dv = new DataView(stl, 80); // 80 == unused header
	var isLittleEndian = true;
	var triangles = dv.getUint32(0, isLittleEndian); 
	// console.log('arraybuffer length:  ' + stl.byteLength);
	// console.log('number of triangles: ' + triangles);
	var offset = 4;
	for (var i = 0; i < triangles; i++) {
		// Get the normal for this triangle
		var normal = new THREE.Vector3(
			dv.getFloat32(offset, isLittleEndian),
			dv.getFloat32(offset+4, isLittleEndian),
			dv.getFloat32(offset+8, isLittleEndian)
		);
		offset += 12;
		// Get all 3 vertices for this triangle
		for (var j = 0; j < 3; j++) {
			geo.vertices.push(
				new THREE.Vector3(
					dv.getFloat32(offset, isLittleEndian),
					dv.getFloat32(offset+4, isLittleEndian),
					dv.getFloat32(offset+8, isLittleEndian)
				)
			);
			offset += 12
		}
		// there's also a Uint16 "attribute byte count" that we
		// don't need, it should always be zero.
		offset += 2;   
		// Create a new face for from the vertices and the normal             
		geo.faces.push(new THREE.Face3(i*3, i*3+1, i*3+2, normal));
	}
	// The binary STL I'm testing with seems to have all
	// zeroes for the normals, unlike its ASCII counterpart.
	// We can use three.js to compute the normals for us, though,
	// once we've assembled our geometry. This is a relatively 
	// expensive operation, but only needs to be done once.
	geo.computeFaceNormals();
	
	return geo;
};  



/**
*
* Processes a given string to make it parsible for parseStl and returns
* the results
*
* @method trim
* @for renderGlobal
* @param {String} str ASCII STL data
* @return {String} processed string
* 
*/
function trim (str) {
	str = str.replace(/^\s+/, '');
	for (var i = str.length - 1; i >= 0; i--) {
		if (/\S/.test(str.charAt(i))) {
			str = str.substring(0, i + 1);
			break;
		}
	}
	return str;
}
			

			
// Added this in to turn the input buffer into an actual string
/**
*
* Converts an arraybuffer into a string of equivalent binary content
* @method arrayToString
* @for renderGlobal
* @param {Arraybuffer} buf The arraybuffer
* @return {String} 
* 
*/
function arrayToString(buf) {
	var pos=0;
	var arr=new Uint8Array(buf);
	var lim=arr.length;
	var result="";
	while(pos<lim){
		result=result+String.fromCharCode(arr[pos]);
		pos++;
	}
	return result;
}



/**
*
* Converts a given block of ASCII stl data (as an arraybuffer) to a threeJS representation
* of the geometry. Function courtesy of 'tonylukasavage' from Github, who released this to
* the public domain. 
*
* @method parseStl
* @for renderGlobal
* @param {Arraybuffer} stl The ASCII stl data
* @return {Object} threeJS geometry object
* 
*/
var parseStl = function(stl) {
	
	var state = '';
	
	stl=arrayToString(stl);
	
	
	var lines = stl.split('\n');
	var geo = new THREE.Geometry();
	var name, parts, line, normal, done, vertices = [];
	var vCount = 0;
	stl = null;
	for (var len = lines.length, i = 0; i < len; i++) {
		if (done) {
			break;
		}
		line = trim(lines[i]);
		parts = line.split(' ');
		switch (state) {
			case '':
				if (parts[0] !== 'solid') {
					console.error(line);
					console.error('Invalid state "' + parts[0] + '", should be "solid"');
					return null;
				} else {
					name = parts[1];
					state = 'solid';
				}
				break;
			case 'solid':
				if (parts[0] !== 'facet' || parts[1] !== 'normal') {
					console.error(line);
					console.error('Invalid state "' + parts[0] + '", should be "facet normal"');
					return null;
				} else {
					normal = [
						parseFloat(parts[2]), 
						parseFloat(parts[3]), 
						parseFloat(parts[4])
					];
					state = 'facet normal';
				}
				break;
			case 'facet normal':
				if (parts[0] !== 'outer' || parts[1] !== 'loop') {
					console.error(line);
					console.error('Invalid state "' + parts[0] + '", should be "outer loop"');
					return null;
				} else {
					state = 'vertex';
				}
				break;
			case 'vertex': 
				if (parts[0] === 'vertex') {
					geo.vertices.push(new THREE.Vector3(
						parseFloat(parts[1]),
						parseFloat(parts[2]),
						parseFloat(parts[3])
					));
				} else if (parts[0] === 'endloop') {
					geo.faces.push( new THREE.Face3( vCount*3, vCount*3+1, vCount*3+2, new THREE.Vector3(normal[0], normal[1], normal[2]) ) );
					vCount++;
					state = 'endloop';
				} else {
					console.error(line);
					console.error('Invalid state "' + parts[0] + '", should be "vertex" or "endloop"');
					return null;
				}
				break;
			case 'endloop':
				if (parts[0] !== 'endfacet') {
					console.error(line);
					console.error('Invalid state "' + parts[0] + '", should be "endfacet"');
					return null;
				} else {
					state = 'endfacet';
				}
				break;
			case 'endfacet':
				if (parts[0] === 'endsolid') {
					return geo;
					done = true;
				} else if (parts[0] === 'facet' && parts[1] === 'normal') {
					normal = [
						parseFloat(parts[2]), 
						parseFloat(parts[3]), 
						parseFloat(parts[4])
					];
					if (vCount % 1000 === 0) {
						//console.log(normal);
					}
					state = 'facet normal';
				} else {
					console.error(line);
					console.error('Invalid state "' + parts[0] + '", should be "endsolid" or "facet normal"');
					return null;
				}
				break;
			default:
				console.error('Invalid state "' + state + '"');
				break;
		}
	}
};