#r "../node_modules/fable-core/Fable.Core.dll"
#I "../node_modules/fable-import-express"
#load "Fable.Import.Express.fs"

open System
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import

type QueryType = 
    | Wind
    | Temp
    | Dates   
    | Rain
    
type RootData = {
    dates: obj;
    currentDate: string;
}

let connectionString = "pg://postgres:password@localhost/weather"
let pg = importDefault<obj> "pg"

let getData (res:express.Response) (sql:string) callBack =
    let client = createNew pg?Client connectionString
    client?connect(fun connectionError -> 
        match connectionError with
        | Some x -> printfn "An error occurred while connecting: %O" connectionError
        | None -> printfn "Connected"
        printfn "%O" sql |> ignore
        client?query(
            sql, 
            fun err result -> 
                match err with
                | Some x -> res.send((sprintf "An error occurred while querying: %O" err))
                | None -> callBack result?rows) 
        |> ignore)                 
    
let getDate (req:express.Request) = 
    match unbox req.query?date with
    | Some x -> x
    | None -> DateTime.Now.ToString("yyyy-MM-dd")

let getSql dataType date = 
    match dataType with
    | Wind -> sprintf "SELECT * FROM wind WHERE to_char(created_date, 'yyyy-MM-DD')='%O' ORDER BY created_date ASC" date
    | Temp -> sprintf "SELECT * FROM temperatureHumidity WHERE to_char(created_date, 'yyyy-MM-DD')='%O' ORDER BY created_date DESC" date
    | Dates -> sprintf "SELECT to_char(created_date, 'yyyy-MM-DD') from wind group by to_char(created_date, 'yyyy-MM-DD') union select to_char(created_date, 'yyyy-MM-DD') from temperatureHumidity group by to_char(created_date, 'yyyy-MM-DD') order by to_char desc";
    | Rain -> sprintf "SELECT * FROM rain WHERE to_char(created_date, 'yyyy-MM-DD')='%O' ORDER BY created_date DESC LIMIT 50" date

let getQueryData (req:express.Request) (res:express.Response) (dataType:QueryType) callBack =
    let date = getDate req    
    getData res (getSql dataType date) callBack

let app = express.Invoke()
app?set("view engine", "ejs");
app?``use``(express?``static``("bower_components"));

//TODO: Move the getData function into a supporting file

app.get
  (U2.Case1 "/api/data/wind", fun (req:express.Request) (res:express.Response) _ -> getQueryData req res Wind (fun r -> res.send(r)) |> box)
|> ignore

app.get
  (U2.Case1 "/api/data/temp", fun (req:express.Request) (res:express.Response) _ -> getQueryData req res Temp (fun r -> res.send(r)) |> box)
|> ignore

app.get
  (U2.Case1 "/api/data/rain", fun (req:express.Request) (res:express.Response) _ -> getQueryData req res Rain (fun r -> res.send(r)) |> box)
|> ignore

app.get
  (U2.Case1 "/", fun (req:express.Request) (res:express.Response) _ -> 
    let date = getDate req
    getQueryData req res Dates (fun r -> 
        res.render("index", {dates = r; currentDate = date})
        res) 
    |> box)
|> ignore

// Get PORT environment variable or use default
let port =
  match unbox Node.``process``.env?PORT with
  | Some x -> x 
  | None -> 8080

// Start the server on the port
app.listen(port, unbox (fun () ->
  printfn "Server started: http://localhost:%i/" port))
|> ignore
