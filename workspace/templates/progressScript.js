


function updateLoad(){

    var theText = document.getElementById("loadText");
    var thePercent = document.getElementById("loadPercent");
    var theBar = document.getElementById("loadProgress");
    switch(stage){
        case 1:
            theText.innerHTML = "Anylizing Part Models...";
            break;
        case 3:
            theText.innerHTML = "Detecting Disassembly Directons...";
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
