


// content of index.js
const express = require('express');
const bodyParser = require('body-parser');
const handlebars = require('handlebars');
const fs = require('fs');
const { exec } = require('child_process');
const app = express();
const port = 3000;
const tempRoute = "";
const sessionRoute = "";
const theTimeout = 1000*60*60*24;
var theDate = new Date();

var contentManifest = {

    jquery:jquery.js,
    threeJS:three.min.js,
    jsstl:jsstl.js,
    treequence:treequence.js,
    partRender:partRender.js,

    uploadMain:upload.html,

    partPropMain:partProp.html,
    partStyle:partPropStyle.css,
    partScript:partPropScript.js,

    dirConMain:dirCon.html
var bodyParser = require('body-parser');,
    dirStyle:dirConStyle.css,
    dirScript:dirConScript.js,

    renderMain:render.html,
    renderStyle:renderStyle.css,
    renderScript:renderScript.js

};

var template = Handlebars.compile(fs.readFileSync(tempRoute+pageBase.html));

var sessions = {};

var content = {};


function removeSessionFiles(thePath){

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

function sweepSessions(){

    var theKeys = Object.keys(sessions);
    var rightNow = theDate.now();
    for ( k in theKeys ){
        if(sessions[k].startTime + theTimeout < rightNow){
            removeSessionFiles(sessions[k].filePath);
            delete sessions[k];
        }
    }

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
    }

    return {
        filePath: sessionRoute + "/" +theID,
        id: theID,
        startTime: theDate.now(),
        stage: 0
    }

}


app.use(bodyParser.json());

app.post('/:stage/:id', (request, response) => {
    response.send('Hello from Express!')
);


app.get('/:stage', (request, response) => {

    var stage = req.params
    var bodyParser = require('body-parser');.stage;
    var context = {
        jquery:jquery.js,
        threeJS:three.min.js,
        jsstl:jsstl.js,
        treequence:treequence.js,
        partRender:partRender.js
    };

    switch(stage){
        case 0:
            context.pageBase = content.uploadMain;
            context.scriptBase = content.scriptBase;
            context.styleBase = content.styleBase;
            break;
        case 1:
            context.pageBase = content.partPropMain;
            context.scriptBase = content.partScript;
            context.styleBase = content.partStyle;
            break;
        case 2:
            context.pageBase = content.dirConMain;
            context.scriptBase = content.dirScript;
            context.styleBase = content.dirStyle;
            break;
        case 3:
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
