OrmGlobal.RegeditDbInfo("def", new DbInfo() { DbType = "mssql", DbConntion = "connection" });
var db = OrmGlobal.Create<Test>();
var lt = db.Size(20).Index(1).ToList();
var lt1=db.Where(n=>n.Ven=="xyz").First();