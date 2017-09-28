
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


function advanceStage(response,status){

    if(status === "success"){
        if(respData.stage === stage){
            return;
        }
        else{
            stage = respData.stage;
            var contentElem = document.getElementByID("stageConent");
            contentElem.innerHTML = response;
            if(stage === 1 || stage === 3 || stage === 5 || stage === 7){
                setInterval(updateProg);
            }
        }
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

	console.log(reqStage);
    $.ajax({
        complete: advanceStage,
        dataType: "json",
        method: "GET",
        timeout: 10000,
        url: "/"+reqStage
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
