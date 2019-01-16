var sqlite3 = require('sqlite3');

var options = {
		memory: false,
		readonly: false,
		fileMustExist: false
};

var db = new sqlite3.Database('metrix.db', options);

db.prepare(`CREATE TABLE session (
			id INTEGER PRIMARY KEY AUTOINCREMENT, 
			secret VARCHAR(255) NOT NULL,
			device VARCHAR(255) NOT NULL,
			user VARCHAR(255) NOT NULL,
			created REAL DEFAULT (datetime('now', 'localtime'))
)`).run();

db.prepare(`CREATE TABLE logtext (
	session INT NOT NULL, 
	sequence INT NOT NULL,
	logline VARCHAR(255) NOT NULL,
	created VARCHAR(255) NOT NULL
)`).run();
