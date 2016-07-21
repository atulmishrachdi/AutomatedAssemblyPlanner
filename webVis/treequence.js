;

function whatsIn(theTree){

	document.getElementById("warning").innerHTML=$(theTree).children("*");

}


function grab(theTree,theMember){

	if($(theTree).children(theMember).length!=0){
		
		return $(theTree).children(theMember)[0];
		
	}
	else{
	
		return null;
	
	}

}

function grabInd(theTree,theMember, theIndex){

	if($(theTree).children(theMember).length>theIndex){
		
		return $(theTree).children(theMember)[theIndex];
		
	}
	else{
	
		return null;
	
	}

}




function getMovement(theTree, myX, myY, myZ, myTime){

	if(($(theTree).children("Install").length==0)){
		return { Name: $(theTree).attr("Name"), X: myX, Y: myY, Z: myZ, Time: myTime, Ref: null, Mov: null };
	}
	else{
		var childTime=parseFloat(grab(grab(theTree,"Install"),"Time").innerHTML)+myTime;
		var movDirection=grab(grab(theTree,"Install"),"InstallDirection");
		var movXDir=parseFloat(grabInd(movDirection,"double",0).innerHTML);
		var movYDir=parseFloat(grabInd(movDirection,"double",1).innerHTML);
		var movZDir=parseFloat(grabInd(movDirection,"double",2).innerHTML);
		var movDistance=parseFloat(grab(grab(theTree,"Install"),"InstallDistance").innerHTML);
		var movX=myX-movXDir*movDistance;
		var movY=myY-movYDir*movDistance;
		var movZ=myZ-movZDir*movDistance;
		var ref=getMovement(getRef(theTree), myX, myY, myZ, childTime);
		var mov=getMovement(getMov(theTree), movX, movY, movZ, childTime);
		
		//console.log({ Name: "", X: myX, Y: myY, Z: myZ, Time: myTime, Ref: ref, Mov: mov});
		return { Name: "", X: myX, Y: myY, Z: myZ, Time: myTime, Ref: ref, Mov: mov};
		
	}

}






function renderThis(theTreequence, theSigma, theTheme){

	var treeQ = $.parseXML(theTreequence);
	
	treeQ=grab(treeQ,"AssemblyCandidate");
	

	treeQ=grab(treeQ,"Sequence");
	

	treeQ=grab(treeQ,"Subassemblies");
	

	treeQ=grab(treeQ,"SubAssembly");
	

	
	times=getTimes(treeQ,0);
	names=getNames(treeQ);
	spaces=getSpaces(treeQ,3,-1);
	

	
	theTree=mergeTrees(times,spaces,names);
	
	cutOffNames(theTree,similarityCutoff(getNameList(theTree)));
	flipTreeTime(theTree,getLongestTime(theTree));
	
	//makePretty(theTree,0,0,0);
	
	drawFor(theTree,theSigma,theTheme);
	
	return;

}

function getRef(theTree){

	theTree=grab(theTree,"Install");
	
	theTree=grab(theTree,"Reference");
	
	return theTree;

}

function getMov(theTree){

	theTree=grab(theTree,"Install");
	
	theTree=grab(theTree,"Moving");
	
	return theTree;

}

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



function getLongestTime(timeTree){

	if(timeTree==null){
		return 0;
	}
	return Math.max(getLongestTime(timeTree.Ref),getLongestTime(timeTree.Mov),timeTree.Time);

}

function getWidestSpace(theTree){

	if(theTree==null){
		return 0;
	}
	return Math.max(getWidestSpace(theTree.Ref),getWidestSpace(theTree.Mov),theTree.Space);

}


function getNames(theTree){



	if(($(theTree).children("Install").length==0)){
		return {Name: $(theTree).attr("Name"), Ref: null, Mov: null};
	}
	else{
		var ref=getNames(getRef(theTree));
		var mov=getNames(getMov(theTree));
		return {Name: "", Ref: ref, Mov: mov};
	}

}

function getSpaces(theTree, underWidth, isMov){



	if(($(theTree).children("Install").length==0)){
	
		

		if(isMov==1){
		
			return {theNode: {Space: underWidth+1, Ref: null, Mov: null}, Width: underWidth};
		
		}
		else{
		
			return {theNode: {Space: underWidth, Ref: null, Mov: null}, Width: underWidth};
		
		}
	}
	else{
	


		var mySpace=underWidth;
		if(isMov==1){
		
			underWidth=underWidth+2;
		
		}
		

		var ref=getSpaces(getRef(theTree),underWidth,0);

		
		if(($(getMov(theTree)).children("Install").length==0)){
			var mov=getSpaces(getMov(theTree),underWidth,1);
		}
		else{
			var mov=getSpaces(getMov(theTree),ref.Width,1);
		}

		
		
		if(isMov!=-1){
			
			return {theNode:{Space: underWidth, Ref: ref.theNode, Mov: mov.theNode},Width: Math.max(mov.Width,ref.Width)};
			
		}
		else{
		
			return {Space: underWidth, Ref: ref.theNode, Mov: mov.theNode};
		
		}
		
		
	}
	
}

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

function makePretty(theTree,trueCenter,prettyCenter,rightness){

	if(theTree==null){
		return 0;
	}
	else{
	
		var oldSpace=theTree.Space;
	
		if(rightness==0){
			theTree.Space=prettyCenter+(theTree.Space-trueCenter);
			makePretty(theTree.Ref,oldSpace,theTree.Space,1);
			makePretty(theTree.Mov,oldSpace,theTree.Space,1);
		}
		else{
			theTree.Space=prettyCenter-(theTree.Space-trueCenter);
			makePretty(theTree.Ref,oldSpace,theTree.Space,0);
			makePretty(theTree.Mov,oldSpace,theTree.Space,0);
		}
		
	}	

}

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
		
		return;
	
	
	}


}


function drawTime(theTree,theGraph, theTheme){

	var theTime=getLongestTime(theTree);
	var theWidth=getWidestSpace(theTree);
	var step=Math.pow(10,Math.floor(Math.log10(theTime)))/2;
	var pos=step;
	
	var maxim=Math.ceil(theTime/step)*step;
	
	
	
	var presNode={ 
		id: "n"+theGraph.nodes().length.toString(),
		label: "0s",
		x: 0,
		y: 1,
		size: 0.4,
		color:theTheme.Time
	};
	theGraph.addNode(presNode);
	var presFarNode={ 
		id: "n"+theGraph.nodes().length.toString(),
		label: "0s",
		x: 0,
		y: theWidth+1,
		size: 0.4,
		color:theTheme.Time
	};
	theGraph.addNode(presFarNode);
	theGraph.addEdge({
		id: "e"+theGraph.edges().length.toString(),
		source: presNode.id,
		target: presFarNode.id,
		type: 'dashed',
		color:theTheme.Time,
		label:"0s"
	});
	
	
	var lastNode;
	var lastFarNode;
	
	while(pos<=maxim){
		lastNode=presNode;
		lastFarNode=presFarNode;
		presNode={ 
			id: "n"+theGraph.nodes().length.toString(),
			label: pos.toString()+"s",
			x: pos,
			y: 1,
			size: 0.4,
			color:theTheme.Time
		};
		theGraph.addNode(presNode);
		presFarNode={ 
			id: "n"+theGraph.nodes().length.toString(),
			label: pos.toString()+"s",
			x: pos,
			y: theWidth+1,
			size: 0.4,
			color:theTheme.Time
		};
		
		theGraph.addNode(presFarNode);
		
		theGraph.addEdge({
			id: "e"+theGraph.edges().length.toString(),
			source: lastNode.id,
			target: presNode.id,
			type: 'dashed',
			color: theTheme.Time
		});
		
		theGraph.addEdge({
			id: "e"+theGraph.edges().length.toString(),
			source: lastFarNode.id,
			target: presFarNode.id,
			type: 'dashed',
			color: theTheme.Time
		});
		
		theGraph.addEdge({
			id: "e"+theGraph.edges().length.toString(),
			source: presNode.id,
			target: presFarNode.id,
			type: 'dashed',
			color: theTheme.Time,
			label: pos.toString()+"s"
		});
		
		pos=pos+step;
	}

}

function scaleGraph(theGraph, xScale, yScale){

	var pos=0;
	var lim=theGraph.nodes().length;
	theNodes=theGraph.nodes();
	while(pos<lim){
		theNodes[pos].x=theNodes[pos].x*xScale;
		theNodes[pos].y=theNodes[pos].y*yScale;
		pos++;
	}
	
}

function flipTreeTime(theTree,axis){

	if(theTree==null){
		return;
	}
	else{
		theTree.Time=axis-theTree.Time;
		flipTreeTime(theTree.Ref,axis);
		flipTreeTime(theTree.Mov,axis);
		return;
	}

}

function flipAxis(theSigma){

	var theGraph=theSigma.graph;
	var pos=0;
	var holder;
	var lim=theGraph.nodes().length;
	theNodes=theGraph.nodes();
	while(pos<lim){
		holder=theNodes[pos].x;
		theNodes[pos].x=theNodes[pos].y;
		theNodes[pos].y=holder;
		pos++;
	}
	theSigma.refresh();

}

function getDepth(theTree){

	if(theTree==null){
		return 0;			
	}
	return 1+Math.max(getDepth(theTree.Ref, theTree.Mov));

}


function adjustGraph(theTree,theGraph){

	var xAdjust=getDepth(theTree)/getLongestTime(theTree);
	scaleGraph(theGraph,xAdjust,1);
	return;

}

function curveEdges(theGraph){

	var pos=0;
	var holder;
	var theChild;
	var theParent;
	var lim=theGraph.edges().length;
	theEdges=theGraph.edges();
	while(pos<lim){
		theChild=theGraph.nodes(theEdges[pos].source);
		theParent=theGraph.nodes(theEdges[pos].target);
		if((Math.abs(theChild.x-theParent.x) < Math.abs(theChild.y-theParent.y)) && theEdges[pos].type==='arrow'){
			theEdges[pos].type='curvedArrow';
		}
		pos++;
	}
	s.refresh();

}





function fillGraph(theTree, theGraph, theTheme, parentNode, isMov){

	if(theTree==null){			
		return;			
	}
	else{
	
		var myNode= { 
			id: "n"+theGraph.nodes().length.toString(),
			label: theTree.Name,
			x: theTree.Time,
			y: theTree.Space,
			size: 0,
			color: theTheme.Main
		}
		
		if(theTree.Ref===null){
		
			if(isMov){
				myNode.size=1;
				myNode.color= theTheme.Mov;
			}
			else{
				myNode.size=1;
				myNode.color= theTheme.Ref;					
			}
			
		
		}
		
		theGraph.addNode(myNode);
		
		fillGraph(theTree.Ref,theGraph,theTheme,myNode.id,0);
		fillGraph(theTree.Mov,theGraph,theTheme,myNode.id,1);
		
		
		if(!(parentNode === null)){
			var myEdge
			if(isMov==0){
				myEdge={
					id: "e"+theGraph.edges().length.toString(),
					source: myNode.id,
					target: parentNode,
					type: 'arrow',
					size: 1
				}
			}
			else{
				myEdge={
					id: "e"+theGraph.edges().length.toString(),
					source: myNode.id,
					target: parentNode,
					type: 'arrow',
					size: 1
				}
			}
			theGraph.addEdge(myEdge);					
		}
		return;
		
	}
}




function drawFor(theTree, theSigma,theTheme){

	theGraph=theSigma.graph;
	theGraph.clear();
	drawTime(theTree,theGraph,theTheme);
	fillGraph(theTree,theGraph,theTheme,null,0);
	adjustGraph(theTree,theGraph);
	curveEdges(theGraph);
	theSigma.refresh();			

}