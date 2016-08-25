;

function makeHUDMeta(theX, theY, theHeight, theWidth, theShift, theHWarp, theWWarp, direction, contents){
	
	if( typeof theX != "number" |
		typeof theY != "number" |
		typeof theHeight != "number" |
		typeof theWidth != "number" |
		typeof direction != "string"|
		typeof theShift != "boolean"|
		typeof theHWarp != "boolean"|
		typeof theWWarp != "boolean"){
		return null;
	}
	
	
	return {
		HUDType: "meta",
		Hidden: false,
		X: theX,
		y: theY,
		H: theHeight,
		W: theWidth,
		Shift: theShift,
		HWarp: theHWarp,
		WWarp: theWWarp,
		Direction: direction,
		Content: contents
	};	
	
}


function makeHUDText(theText){
	
	
	if(typeof theText != "string"){
		return null;
	}
	
	return {
		HUDType: "text",
		Text: theText
	};
	
}

function makeHUDToggle(){
	
	return {
		HUDType: "toggle",
		Engaged: false
	}
	
}

function makeHUDDataTree(theTree){
	
	if(theTree==null){
		return null;
	}
	else{
		var ref=makeHUDDataTree(theTree.Ref);
		var mov=makeHUDDataTree(theTree.Mov);
		var theContents=[];
		if(ref!=null){
			
			theContents=theContents.concat(ref);
		}
		if(mov!=null){
			theContents=theContents.concat(mov);
		}
		
	}
	
	var result=makeHUDMeta(0, 0, 0, 0, "relative", "down", []);
	var bar=makeHUDMeta(0, 0, 15, 0, "absolute", "right", []);
	var nameProp=makeHUDText(theTree.Name);
	var expandToggle=makeHUDToggle;
	bar.Content.push(expandToggle);
	bar.Content.push(nameProp);
	result.Content.push(bar);
	result.Content=result.Content.concat(theContents);

	return result;
	
}


function autoFitMeta(theMeta){
	
	if(theMeta==null | theMeta.HUDType!="meta"){
		return null;
	}
	var pos=0;
	var lim=theMeta.Content.length;
	var shiftMetas=[];
	var staticMetas=[];
	var adjustedMeta;
	while(pos<lim){
		adjustedMeta=_autoFitMeta(theMeta.Content[pos]);
		if(adjustedMeta!=null){
			if(adjustedMeta.Shiftable){
				shiftMetas.push(newFitData);
			}
			else{
				staticMetas.push(newFitData);
			}
		}
		pos++;
	}

	resolveMetaCollisions(staticMetas,shiftMetas,theMeta.Direction);
	if(theMeta.HWarp){
		pos=0;
		lim=staticMetas.length;
		while(pos<lim){
			if(staticMetas[pos].Y+staticMetas[pos].H>theMeta.H){
				theMeta.H=staticMetas[pos].Y+staticMetas[pos].H;
			}
			pos++;
		}
	}
	if(theMeta.WWarp){
		pos=0;
		lim=staticMetas.length
		while(pos<lim){
			if(staticMetas[pos].X+staticMetas[pos].W>theMeta.W){
				theMeta.W=staticMetas[pos].X+staticMetas[pos].W;
			}
			pos++;
		}
	}
	return theMeta;
	
}

function resolveMetaCollisions(theStatics,theShifts,shiftDir){
	
	theShifts.reverse();
	var pos;
	var lim;
	var shift;
	var collided;
	while(theShifts.length!=0){
		shift=theShifts.pop();
		pos=0;
		lim=theStatics.length;
		collided=true;
		while(collided==true){
			collided=false;
			while(pos<lim){
				if(metasCollide(theStatics[pos],shift)){
					collided=true;
					if(shiftDir==="up"){
						shift.Y=theStatics[pos].Y-shift.H;
					}
					else if(shiftDir==="down"){
						shift.Y=theStatics[pos].Y+theStatics[pos].H;
					}
					else if(shiftDir==="left"){
						shift.X=theStatics[pos].X-shift.W;
					}
					else if(shiftDir==="right"){
						shift.X=theStatics[pos].X+theStatics[pos].W;
					}
					else{
						console.log("ERROR: Bad direction given for resolving a meta collision");
					}
				}
			}
		}
		theStatics.push(shift);
	}
	
	
	
}

function metasCollide(meta1,meta2){
	if(_metasCollide(meta1,meta2) | _metasCollide(meta2,meta1){
		return true;
	}
	return false;
}


function _metasCollide(meta1, meta2){
	if(meta1.X>meta2.X & meta1.X<meta2.X+meta2.W){
		if(meta1.Y>meta2.Y & meta1.Y<meta2.Y){
			return true;
		}
		if(meta1.Y+meta1.H>meta2.Y & meta1.Y+meta1.H<meta2.Y+meta2.H){
			return true;
		}		
	}
	if(meta1.X+meta1.W>meta2.X & meta1.X+meta1.W<meta2.X+meta2.W){
		if(meta1.Y>meta2.Y & meta1.Y<meta2.Y){
			return true;
		}
		if(meta1.Y+meta1.H>meta2.Y & meta1.Y+meta1.H<meta2.Y+meta2.H){
			return true;
		}		
	}
	return false;
}


function evalHUDPrime (){
	
	
	
}
function evalHUDRelease(){
	
	
	
}

function renderHUD(){
	
	
	
	
}





