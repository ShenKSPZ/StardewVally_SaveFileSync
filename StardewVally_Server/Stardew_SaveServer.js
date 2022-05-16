const IP = "localhost"

const http = require('http');
const fs = require('fs');
var join = require('path').join;
const { v4: uuidv4 } = require('uuid');
let fileNameReg = /(?<=-------FileName = ).*?(?=-------)/;

const server = http.createServer()

server.listen(8082, IP, () => {
    console.log(`server启动成功`)
    console.log(server.address())
    server.on('request', (req, res) =>{
        var comment = req.url.split('/');
        console.log(comment);
        if(comment[1] == "StardewVally_ServerPing"){
            var lastTime = `0`;
            if(fs.existsSync(`${__dirname}/dataBases/Storage/${comment[2]}/mainfest.json`))
            {
                var content = fs.readFileSync(`${__dirname}/dataBases/Storage/${comment[2]}/mainfest.json`)
                var mainfest = JSON.parse(content);
                lastTime = mainfest.lastUploadTime;
                res.write(`Ping_Success/${lastTime}`);
                res.end();
            }
            else{
                res.write(`Ping_Success/0`);
                res.end();
            }
        }else if(comment[1] == "StardewVally_SaveFileUpload"){
            let data = '';
            var first = true;
            req.on('data', chunk => {
                data += chunk;
            })
            req.on('end', ()=>{
                var fileName = data.match(fileNameReg)[0];
                var uid = uuidv4();
                var mainfest = {};
                if(!fs.existsSync(`${__dirname}/dataBases/Storage/${fileName}`)){
                    fs.mkdirSync(`${__dirname}/dataBases/Storage/${fileName}`);
                }
                if(!fs.existsSync(`${__dirname}/dataBases/Storage/${fileName}/mainfest.json`)){
                    let mainfest = {
                        lastUploadTime: comment[2],
                        saveFiles: `${fileName}--${uid}`,
                    }
                    fs.writeFileSync(`${__dirname}/dataBases/Storage/${fileName}/mainfest.json`, JSON.stringify(mainfest));
                }else{
                    mainfest = JSON.parse(fs.readFileSync(`${__dirname}/dataBases/Storage/${fileName}/mainfest.json`));
                }
                
                fs.writeFile(`${__dirname}/dataBases/Storage/${fileName}/${fileName}--${uid}`, data.replace(`-------FileName = ${fileName}-------`, ''), (err)=>{
                    if(err){
                        res.writeHead(200, {
                            "Content-Type": "text/plain;charset=utf-8",
                        });
                        res.write(err);
                        res.end();
                    }
                    else{
                        if(fs.existsSync(`${__dirname}/dataBases/Storage/${fileName}/${mainfest.saveFiles}`)){
                            fs.rmSync(`${__dirname}/dataBases/Storage/${fileName}/${mainfest.saveFiles}`);
                        }
                        mainfest.lastUploadTime = comment[2];
                        mainfest.saveFiles = `${fileName}--${uid}`;
                        fs.writeFileSync(`${__dirname}/dataBases/Storage/${fileName}/mainfest.json`, JSON.stringify(mainfest));
                        res.writeHead(200, {
                            "Content-Type": "text/plain;charset=utf-8",
                        });
                        res.write("FileReceiveSuccess");
                        res.end();
                    }
                })
            })
        }else if(comment[1] == "StardewVally_GetCloundSaveFileList"){
            var saveFiles = ''

            var allFiles = fs.readdirSync(`${__dirname}/dataBases/Storage/`);
            allFiles.forEach((item, index) =>{
                let fPath = join(`${__dirname}/dataBases/Storage/`, item);
                let stat = fs.statSync(fPath);
                if(stat.isDirectory() == true){
                    saveFiles += `${item}/`
                }
            })
            res.write(saveFiles);
            res.end();
        }else if(comment[1] == "StardewVally_GetCloundSaveFileByName"){
            var mainfestPath = `${__dirname}/dataBases/Storage/${comment[2]}/mainfest.json`;
            var content = ``
            if(fs.existsSync(mainfestPath)){
                var mainfest = JSON.parse(fs.readFileSync(`${__dirname}/dataBases/Storage/${comment[2]}/mainfest.json`));
                var saveFilePath = `${__dirname}/dataBases/Storage/${comment[2]}/${mainfest.saveFiles}`;
                if(fs.existsSync(saveFilePath)){
                    content = fs.readFileSync(saveFilePath)
                    res.write(content);
                    res.end();
                }
                else{
                    res.write(`FindSaveFileFailure`);
                    res.end();
                }
            }
            else{
                res.write(`FindSaveFileFailure`);
                res.end();
            }
        }
        
    })
})