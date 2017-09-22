
//  Array for processed parts
var parts=[];

// The XML data for the current stage
var dataText="";

// The current stage
var stage = 0;

// The time since the last check in with the server
var lastCheckin = 0;

// The progress of the web page in loading the file
var prog = 0;

function advanceStage(response,status){

    if(status === "success"){
        if(respData.stage === stage){

        }
        else{

        }
    }

}

function updateProg(response,status){

    if(status === "success"){
        if(respData.stage === stage){

        }
        else{

        }
    }

}

function requestAdvance(){

    $.ajax({
        complete: advanceStage,
        dataType: "json",
        method: "POST",
        timeout: 10000
    });

}

function checkIn(){

    $.ajax({
        complete: updateProg,
        dataType: "json",
        method: "POST",
        timeout: 10000
    });

}
