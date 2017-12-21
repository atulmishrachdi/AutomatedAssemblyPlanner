//<script>


// content of index.js
const express = require('express');
const bodyParser = require('body-parser');
const multer = require('multer'); // v1.0.5
const upload = multer();
const handlebars = require('handlebars');
const fs = require('fs');
const path = require('path');
const sep = path.sep;
const exec = require('child_process').exec;
const execSync = require('child_process').execSync;
const crypto = require('crypto');
const app = express();
const port = 3000;
const sessionRoute = fs.realpathSync(".") + sep + "workspace";
const tempRoute = sessionRoute + sep + "templates";
const theTimeout = 1000 * 60 * 60 * 24;

var OS = process.platform;

var contentManifest = {

    jquery: "jquery.js",
    threeJS: "three.min.js",
    jsstl: "jsstl.js",
    treequence: "treequence.js",
    partRender: "partRender.js",
    tableScript: "datatables.js",
    directionList: "directionList.js",

    tableStyle: "datatables.css",

    baseStyle: "pageBaseStyle.css",
    pageBase: "pageBase.html",
    stageBase: "stageBase.html",

    progMain: "progress.html",
    progStyle: "progressStyle.css",
    progScript: "progressScript.js",

    pageBaseStyle: "pageBaseStyle.css",
    pageBaseScript: "pageBaseScript.js",

    uploadMain: "upload.html",
    uploadStyle: "uploadStyle.css",
    uploadScript: "uploadScript.js",

    partPropMain: "partProp.html",
    partStyle: "partPropStyle.css",
    partScript: "partPropScript.js",

    dirConMain: "dirCon.html",
    dirStyle: "dirConStyle.css",
    dirScript: "dirConScript.js",

    renderMain: "render.html",
    renderStyle: "renderStyle.css",
    renderScript: "renderScript.js"

};

var content = {};

for (var key of Object.keys(contentManifest)) {
    content[key] = fs.readFileSync(tempRoute + sep + contentManifest[key], 'utf8');
}


var baseTemplate = handlebars.compile(content.pageBase);
var stageTemplate = handlebars.compile(content.stageBase);

var sessions = {};


function safeRead(file) {

    var result = null;
    var done = false;
    while (result === null) {
        try {
            result = fs.readFileSync(file, 'ascii');
        }
        catch (theError) {
            result = null;
            switch (theError.code) {
                case "ETXTBSY":
                    console.log("File busy when trying to read file " + file);
                    break;
                case "ENOENT":
                    console.log("No file found when trying to read file " + file);
                    result = "";
                    break;
                default:
                    console.log("Experienced error " + theError +
                        " when trying to read file " + file);
                    result = "";
                    break;
            }
        }
    }
    return result;

}

function killDir(thePath) {

    fs.readdir(thePath,
        (function (dirPath) {
            return (function (err, theFiles) {
                for (f in theFiles) {
                    fs.unlinkSync();
                }
                rmdirSync(dirPath);
            })
        })(thePath)
    );

}

function sweepDir(thePath) {

    fs.readdir(thePath,
        (function (dirPath) {
            return (function (err, theFiles) {
                for (f in theFiles) {
                    fs.unlinkSync();
                }
            })
        })(thePath)
    );

}

function sweepSessions() {

    var theKeys = Object.keys(sessions);
    var rightNow = theDate.now();
    for (k in theKeys) {
        if (sessions[k].startTime + theTimeout < rightNow) {
            killDir(sessions[k].filePath + sep + "intermediate");
            killDir(sessions[k].filePath + sep + "models");
            killDir(sessions[k].filePath + sep + "XML");
            killDir(sessions[k].filePath);
            delete sessions[k];
        }
    }

}

function setupSession(thePath, theModels) {

    fs.mkdirSync(thePath);
    fs.mkdirSync(thePath + sep + "intermediate");
    fs.mkdirSync(thePath + sep + "models");
    fs.mkdirSync(thePath + sep + "XML");

}

function getHex(theChar) {

    var hex = "0123456789ABCDEF";
    var bottom = theChar % 16;
    var top = Math.floor(theChar / 16) % 16;
    // console.log("Bottom: "+bottom+" Top: "+top+" -> "+hex[top]+hex[bottom]);
    return hex[top] + hex[bottom];

}

function makeID() {

    var idLen = 16;
    var array = crypto.randomBytes(idLen);
    var result = "";
    var check1 = 0;
    var check2 = 0;
    var check4 = 0;
    var check8 = 0;

    var idPos = 0;
    while (idPos < idLen) {
        if (idPos % 2 <= 0) {
            check1 += array[idPos];
        }
        if (idPos % 4 <= 1) {
            check2 += array[idPos];
        }
        if (idPos % 8 <= 3) {
            check4 += array[idPos];
        }
        if (idPos % 16 <= 7) {
            check8 += array[idPos];
        }
        result = result + getHex(array[idPos]);
        idPos++;
    }

    result = result + getHex(check1) + getHex(check2) + getHex(check4) + getHex(check8);

    return result;

}


function makeSession() {

    var theID = makeID();
    while (typeof (sessions[theID]) !== 'undefined') {
        sweepSessions();
        theID = makeID();
        var bodyParser = require('body-parser');
    }
    return {
        filePath: sessionRoute + sep + theID,
        id: theID,
        startTime: Date.now(),
        stage: 0,
        workingOn: 0,
        state: {
            models: [],
            partsPropertiesIn: "",
            partsPropertiesOut: "",
            dirConfirmIn: "",
            dirConfirmOut: "",
            renderIn: ""
        }
    };

}


function exeDone(exeFile, sessID) {
    return (function (err, stdout, stderr) {
        console.log(exeFile + " finished for session " + sessID);
        console.log("Output was: \n\n\n" + stdout + "\n\n\n");
        console.log("Error stuff was: \n\n\n" + stderr + "\n\n\n");
    })
}

function runResponse(response, exeFile, sessID, textFile, textData) {


    return (function (err, data) {
        fs.writeFileSync(sessions[sessID].filePath + sep + "prog.txt", "0");
        console.log("Executing: " + exeFile + " \"" +
            sessions[sessID].filePath + "\"  y  1  0.5  y  y");
        	exec(	"dotnet " + exeFile + " " + sessions[sessID].filePath +
        			"  y  1  0.5  y  y", exeDone(exeFile,sessID));
        //if(OS === 'win32'){
        //	console.log("Executing: "+exeFile + " \"" +
        //				sessions[sessID].filePath + "\"  y  1  0.5  y  y");
        //	exec(	exeFile + " \"" + sessions[sessID].filePath +
        //			"\"  y  1  0.5  y  y", exeDone(exeFile,sessID));
        //}
        //else{
        //	console.log("Executing for Linux");
        //	exec(	"mono " + exeFile + " " + sessions[sessID].filePath +
        //			"  y  1  0.5  y  y", exeDone(exeFile,sessID));
        //}

        response.json({
            stage: sessions[sessID].stage,
            progress: 0,
            data: null,
            failed: false
        });
    });

}

function execResponse(response, exeFile, sessID, textFile, textData) {

    console.log("Executing file '" + exeFile + "' for " + sessID);
    if (textFile === "") {
        (runResponse(response, exeFile, sessID, textFile, textData))();
    }
    else {
        fs.writeFile(textFile, textData, runResponse(response, exeFile, sessID, textFile, textData));
    }

}


function verifResponse(response, theID) {

    return function (error, stdout, stderr) {
        var verif = false;
        for (c in stdout) {
            if (c === '~') {
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


function progResponse(response, theID, theFile, session, field) {

    var prog = safeRead(session.filePath + sep + "prog.txt");
    if (prog === "") {
        prog = "0";
    }
    //console.log("Read in prog, result was: "+prog);
    fs.readFile(theFile, 'ascii',
        (function (err, data) {
            if (typeof (data) !== "undefined") {
                //console.log("Read in data, result was: "+data);
                session.state[field] = data;
                response.json({
                    stage: session.stage,
                    progress: prog,
                    data: data,
                    failed: false
                });
            }
            else {
                //console.log("Failed to load in result. Error: "+err);
                response.json({
                    stage: session.stage,
                    progress: prog,
                    data: null,
                    failed: false
                });
            }
        })
    );

}




app.use(bodyParser.json({ limit: '500gb' }));
app.use(bodyParser.urlencoded({ limit: '500gb', extended: true }));

app.post('/checkIn', (request, response) => {

    var data = request.body;

    var stage = data.stage;
    sessID = data.sessID;
    sessData = sessions[sessID];
    console.log("Received check in from session " + sessID + " for stage " + stage);
    //console.log(request.body);

    switch (stage) {
        case "1":
            progResponse(response, sessID, sessData.filePath + sep +
                "XML" + sep + "parts_properties.xml",
                sessData, "partsPropertiesIn");
            break;
        //================================//================================//==
        case "3":
            progResponse(response, sessID, sessData.filePath + sep + "XML" + sep +
                "directionList.xml",
                sessData, "dirConfirmIn");
            break;
        //================================//================================//==
        case "5":
            progResponse(response, sessID, sessData.filePath + sep + "XML" + sep +
                "verificationState.txt", sessData,
                "dirConfirmIn");
            break;
        //================================//================================//==
        case "6":
            progResponse(response, sessID, sessData.filePath + sep + "XML" + sep +
                "solution.xml", sessData,
                "renderIn");
            break;
        default:
            console.log("Invalid stage value '" + stage + "' fell through");
    }

});

app.post('/exec', (request, response) => {

    var data = request.body;

    var stage = data.stage;

    var textData = data.textData;

    sessID = data.sessID;
    sessData = sessions[sessID];
    console.log("Recieved check in from session " + sessID + " for stage " + stage);
    //console.log(request.body);

    sessions[sessID].workingOn = stage;
    switch (stage) {
        //================================//================================//==
        case "1":
            execResponse(response, "bin//netcoreapp2.0//FastenerDetection.dll", sessID, "", "");
            break;
        //================================//================================//==
        case "3":
            execResponse(response, "bin//netcoreapp2.0//DisassemblyDirections.dll", sessID,
                sessData.filePath + sep + "XML" + sep +
                "parts_properties2.xml", textData);
            break;
        //================================//================================//==
        case "5":
            execResponse(response, "bin/netcoreapp2.0/Verification.dll", sessID,
                sessData.filePath + sep + "XML" + sep +
                "directionList2.xml", textData);
            break;
        //================================//================================//==
        case "6":
            execResponse(response, "bin/netcoreapp2.0/AssemblyPlanning.dll", sessID,
                sessData.filePath + sep + "XML" + sep +
                "directionList2.xml", textData);
            break;
        default:
            console.log("Invalid stage value '" + stage + "' fell through");
    }

});


app.get('/', (request, response) => {

    response.send(baseTemplate({
        baseStyle: content["tableStyle"]
        + "\n" + content["baseStyle"]
    }));

});


app.get('/static/:name', (request, response) => {

    var name = request.params.name;
    response.send(content[name]);

});


app.get('/stage/:stage', (request, response) => {

    var stage = request.params.stage;

    var context = {};

    switch (stage) {
        case "0":
            context.stageHTML = content.uploadMain;
            context.stageStyle = content.uploadStyle;
            break;
        case "1":
            context.stageHTML = content.progMain;
            context.stageStyle = content.progStyle;
            break;
        case "2":
            context.stageHTML = content.partPropMain;
            context.stageStyle = content.partStyle;
            break;
        case "3":
            context.stageHTML = content.progMain;
            context.stageStyle = content.progStyle;
            break;
        case "4":
            context.stageHTML = content.dirConMain;
            context.stageStyle = content.dirStyle;
            break;
        case "5":
            context.stageHTML = content.progMain;
            context.stageStyle = content.progStyle;
            break;
        case "6":
            context.stageHTML = content.progMain;
            context.stageStyle = content.progStyle;
            break;
        case "7":
            context.stageHTML = content.renderMain;
            context.stageStyle = content.renderStyle;
            break;
    }
    response.send(stageTemplate(context));

});

app.post('/getID', (request, response) => {

    var sessData = makeSession();
    var sessID = sessData.id;
    sessions[sessID] = sessData;
    setupSession(sessData.filePath);
    console.log("Set up session " + sessID);
    response.json({
        sessID: sessID
    });


});


app.post('/giveModel', (request, response) => {

    var sessData = sessions[request.body.sessID];
    var model = request.body.Model;
    sessData.state.models.push(model);
    fs.writeFileSync(sessData.filePath + sep + "models" + sep +
        model.Name, model.Data, 'ascii');
    response.json({
        success: true
    });

});


app.listen(port, (err) => {
    if (err) {
        return console.log('something bad happened', err)
    }
    console.log(`server is listening on ${port}`)
});


//</script>
