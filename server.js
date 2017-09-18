


// content of index.js
const express = require('express');
const bodyParser = require('body-parser');
var multer = require('multer'); // v1.0.5
var upload = multer();
const handlebars = require('handlebars');
const fs = require('fs');
const exec = require('child_process').exec;
const execSync = require('child_process').execSync;
const app = express();
const port = 3000;
const tempRoute = "";
const sessionRoute = "";
const theTimeout = 1000*60*60*24;
var theDate = new Date();

var contentManifest = {

    jquery:"jquery.js",
    threeJS:"three.min.js",
    jsstl:"jsstl.js",
    treequence:"treequence.js",
    partRender:"partRender.js",

    uploadMain:"upload.html",
    uploadStyle:"uploadStyle.css",
    uploadScript:"uploadScript.js",

    partLoadMain:"partLoad.html",
    partLoadStyle:"partLoadStyle.css",
    partLoadScript:"partLoadScript.js",

    partPropMain:"partProp.html",
    partStyle:"partPropStyle.css",
    partScript:"partPropScript.js",

    dirLoadMain:"dirLoad.html",
    dirLoadStyle:"dirLoadStyle.css",
    dirLoadScript:"dirLoadScript.js",

    dirConMain:"dirCon.html",
    dirStyle:"dirConStyle.css",
    dirScript:"dirConScript.js",

    planLoadMain:"planLoad.html",
    planLoadStyle:"planLoadStyle.css",
    planLoadScript:"planLoadScript.js",

    renderMain:"render.html",
    renderStyle:"renderStyle.css",
    renderScript:"renderScript.js"

};

var template = Handlebars.compile(fs.readFileSync(tempRoute+pageBase.html));

var sessions = {};

var content = {};

function killDir(thePath){

    fs.readdir(thePath,
        (function(dirPath){
            return (function(err,theFiles){
                for( f in theFiles ){
                    fs.unlinkSync();
                }
                rmdirSync(dirPath);
            })
        })(thePath)
    );

}

function sweepDir(thePath){

    fs.readdir(thePath,
        (function(dirPath){
            return (function(err,theFiles){
                for( f in theFiles ){
                    fs.unlinkSync();
                }
            })
        })(thePath)
    );

}

function sweepSessions(){

    var theKeys = Object.keys(sessions);
    var rightNow = theDate.now();
    for ( k in theKeys ){
        if(sessions[k].startTime + theTimeout < rightNow){
            killDir(sessions[k].filePath+"/intermediate");
            killDir(sessions[k].filePath+"/models");
            killDir(sessions[k].filePath+"/XML");
            killDir(sessions[k].filePath+);
            delete sessions[k];
        }
    }

}

function setupSession(thePath,theModels){

    fs.mkdirSync(thePath);
    fs.mkdirSync(thePath+"/intermediate");
    fs.mkdirSync(thePath+"/models");
    fs.mkdirSync(thePath+"/XML");

}

function getHex( theChar ){

    var hex = "0123456789ABCDEF";
    var bottom = theChar%16;
    var top = (theChar/16)%16;
    return hex[top]+hex[bottom];

}

function makeID(){

    var idLen = 16;
    var array = new Uint8Array(idLen);
    window.crypto.getRandomValues(array);
    var result = "";
    var check1 = 0;
    var check2 = 0;
    var check4 = 0;
    var check8 = 0;

    var idPos = 0;
    while(idPos < idLen){
        if(idPos & 1 != 0){
            check1 += array[idPos];
        }
        if(idPos & 2 != 0{
            check2 += array[idPos];
        }
        if(idPos & 4 != 0){
            check4 += array[idPos];
        }
        if(idPos & 8 != 0{
            check8 += array[idPos];
        }
        result = result + getHex(array[idPos]);
        idPos++;
    }

    result = result + getHex(check1) + getHex(check2) + getHex(check4) + getHex(check8);

}

function makeSession(){

    var theId = makeID();
    while( typeof sessions[theId] != 'undefined'){
        sweepSessions();
        theId = makeID();
    var bodyParser = require('body-parser');
    }

    return {
        filePath: sessionRoute + "/" +theID,
        id: theID,
        startTime: theDate.now(),
        stage: 0,
        state = {
            models: [],
            partsPropertiesIn: "",
            partsPropertiesOut: "",
            dirConfirmIn: "",
            dirConfirmOut: "",
            renderIn:""
        }
    }

}



function basicResponse(response, theID){

    return function(error,stdout,stderr){
        response.json({
            sessID: theID,
            failed: (error === null)
        });
    }

}

function verifResponse(response, theID){

    return function(error,stdout,stderr){
        var verif = false;
        for(c in stdout){
            if(c === '~'){
                verif = true;
                break;
            }
        }
        response.json({
            sessID: theID,
            verified: verif,
            failed: (error === null)
        });
    }

}


function progResponse(response, theID, theFile, session, field){

    fs.readFile(sessData.filePath+"/intermediates/prog.txt",
        (function(err,data){
            var prog;
            if(data !== null){
                prog = data.length;
            }
            else{
                prog = 0;
            }
            fs.readFile(theFile,
                (function(err,data){
                    if(data !== null){
                        session.state[field] = data;
                        response.json({
                            sessID: theID,
                            progress: prog,
                            data: data,
                            failed: false
                        });
                    }
                    else{
                        response.json({
                            sessID: theID,
                            progress: prog,
                            data: null,
                            failed: false
                        });
                    }
                })
            );
        })
    );

}


app.use(bodyParser.json());
app.use(bodyParser.urlencoded({ extended: true }));

app.post('/', (request, response) => {

    var data = request.body;

    var stage = data.stage;
    var sessData;
    var sessID;
    if(stage === 0){
        sessData = makeSession();
        sessID = sessData.id;
        sessions[sessID] = sessData;
    }
    else{
        sessID = data.sessID;
        sessData = sessions[sessID];
    }

    switch(stage){
        //================================//================================//================================
        case 0:
            setupSession(sessData.filePath);
            for( p in data.models){
                fs.writeFileSync(sessData.filePath + "/models/" + p.Name, p.Data, 'ascii');
            }
            exec("workspace/FastenerDetection.exe", "workspace", "y", "1", "0.5", "y", "y",basicResponse(response,sessID));
            break;
        //================================//================================//================================
        case 1:
            progResponse(response, sessID, sessData.filePath+"/XML/parts_properties.xml", sessData, "partsPropertiesIn");
            break;
        //================================//================================//================================
        case 2:
            exec("workspace/DisassemblyDirections.exe", "workspace", "y", "1", "0.5", "y", "y",basicResponse(response,sessID));
            break;
        //================================//================================//================================
        case 3:
            progResponse(response, sessID, sessData.filePath+"/XML/directionlist.xml", sessData, "dirConfirmIn");
            break;
        //================================//================================//================================
        case 4:
            exec("workspace/Verification.exe", "workspace", "y", "1", "0.5", "y", "y",verifResponse(response,sessID));
            break;
        //================================//================================//================================
        case 5:
            exec("workspace/AssemblyPlanning.exe", "workspace", "y", "1", "0.5", "y", "y",basicResponse(response,sessID));
            break;
        //================================//================================//================================
        case 6:
            progResponse(response, sessID, sessData.filePath+"/XML/solution.xml", sessData, "renderIn");
            break;
    }

);


app.get('/:stage', (request, response) => {

    var stage = request.params.stage;

    var context = {
        jquery:content.jquery,
        threeJS:content.threeJS,
        jsstl:content.jsstl,
        treequence:content.treequence,
        partRender:content.partRender
    };


    switch(stage){
        case 0:
            context.pageBase = content.uploadMain;
            context.scriptBase = content.scriptBase;
            context.styleBase = content.styleBase;
            break;
        case 1:
            context.pageBase = content.partLoadMain;
            context.scriptBase = content.partLoadScript;
            context.styleBase = content.partLoadStyle;
            break;
        case 2:
            context.pageBase = content.partPropMain;
            context.scriptBase = content.partScript;
            context.styleBase = content.partStyle;
            break;
        case 3:
            context.pageBase = content.dirLoadMain;
            context.scriptBase = content.dirLoadScript;
            context.styleBase = content.dirLoadStyle;
            break;
        case 4:
            context.pageBase = content.dirConMain;
            context.scriptBase = content.dirScript;
            context.styleBase = content.dirStyle;
            break;
        case 5:
            context.pageBase = content.planLoadMain;
            context.scriptBase = content.planLoadScript;
            context.styleBase = content.planLoadStyle;
            break;
        case 6:
            context.pageBase = content.renderMain;
            context.scriptBase = content.renderScript;
            context.styleBase = content.renderStyle;
            break;
    }
    response.send(template(context));

});

app.listen(port, (err) => {
    if (err) {
        return console.log('something bad happened', err)
    }
    console.log(`server is listening on ${port}`)
});
