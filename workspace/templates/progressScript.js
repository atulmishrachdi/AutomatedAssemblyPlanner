


function updateLoad(){

    var theText = document.getElementById("loadText");
    var thePercent = document.getElementById("loadPercent");
    var theBar = document.getElementById("loadProgress");
    switch(stage){
        case 1:
            theText.innerHTML = "Analyzing Part Models...";
            break;
        case 3:
            theText.innerHTML = "Detecting Disassembly Directions...";
            break;
        case 5:
            theText.innerHTML = "Verifying Assembly Connectedness...";
            break;
		case 6:
            theText.innerHTML = "Generating Assembly Plan...";
            break;
        default:
            theText.innerHTML = "";
            break;

    }
    thePercent.innerHTML = ""+prog+"%";
    theBar.style.width =  ""+prog+"%";

}
