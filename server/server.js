var finalhandler = require('finalhandler')
var http = require('http')
var bodyParser = require('body-parser');
var Router = require('router')
var swig = require('swig')
var sqlite3 = require('sqlite3');
const uuidv1 = require('uuid/v4');

var options = {
		memory: false,
		readonly: false,
		fileMustExist: false
};

var db = new sqlite3.Database('metrix.db', options);

swig.setDefaults({ cache: false });
 
var router = Router()
var metrixApi = Router()
router.use('/api/metrix/', metrixApi);

metrixApi.get('/session/list', function(req, res) {		
	db.prepare("SELECT * FROM session ORDER BY created DESC").all(function(err, rows) {
		res.setHeader('Content-Type', 'text/html; charset=utf-8')
		res.end(swig.renderFile('templates/session-list.html', { sessions:rows }))  
	});
})

metrixApi.get('/session/view/:id', function(req, res) {		
	db.prepare("SELECT * FROM session WHERE id = ?").get([req.params.id], function(err, row) {
		res.setHeader('Content-Type', 'text/html; charset=utf-8');
		db.prepare("SELECT * FROM logtext WHERE session = ? ORDER BY sequence ASC").all([req.params.id], function(err, logs) {
			res.end(swig.renderFile('templates/session-view.html', { session: row, logs: logs }))  
		});
	});
})

metrixApi.use(bodyParser.text());

metrixApi.post('/session/create', function (req, res) {
	try {
		var props = {
			"device": "",
			"user": ""
		};
		var lines = req.body.split('\n');
		for (var idx in lines) {
			var text = lines[idx];
			var sep = text.indexOf(':');
			if (sep < 0)
				continue;
			var key = text.substring(0, sep);
			var value = text.substring(sep + 1);
			props[key] = value;
		}

		var secret = uuidv1();
		var p = db.prepare("INSERT INTO session (secret, device, user) VALUES (?, ?, ?)");
		var result = p.run([secret, props.device, props.user]);
		var sess = '/session/insert/' + secret + '/';
		res.end(sess);
		console.log("Started session:" + sess);
	}
	catch (e) {
		console.log("aah ", e);
	}
});

metrixApi.post('/session/insert/:secret/', function(req, res) {
	db.prepare("SELECT * FROM session WHERE secret = ?").get([req.params.secret], function(err, row) {
		if (row == null || row.id == null)
		{
			req.end("Invalid session");
			return;	
		}
		var session = row.id;
		var lines = req.body.split('\n');
		for (var idx in lines)
		{
			var text = lines[idx];
			if (text.length < 3)
				continue;
				
			var pcs = [null, null, null, null];
			var pos = 0;
		
			for (var i=0;i<4;i++)
			{
				var sep = text.indexOf('$', pos);
				if (sep < 0)
					break;
				pcs[i] = text.substring(pos, sep);
				pos = sep + 1;
			}
			
			if (sep < 0 || pcs[3] == null)
			{
				res.end("Not enough parameters");
			}
			
			if (pcs[0] == 'log')
			{
				var k = Number(pcs[1]);
				var type = pcs[2];
				var when = pcs[3];
				var data = text.substring(pos);
				var par = [session, k, data, when];
				db.prepare("INSERT INTO logtext (session, sequence, logline, created) VALUES (?, ?, ?, ?)").run(par);
			}
		}	
		res.end("OK");
	});
})

metrixApi.get('/session/insert/:secret/', function(req, res) {		
	res.end("OK");
})
 
 
var server = http.createServer(function(req, res) {
  router(req, res, finalhandler(req, res))
})
 
server.listen(8765)


