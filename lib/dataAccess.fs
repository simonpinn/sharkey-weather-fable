namespace SharkeyWeather

open System
open Fable.Core
open Fable.Core.JsInterop

module DataAccess =    
    type QueryType = 
        | Wind
        | Temp
        | Dates   
        | Rain

    let connectionString = "pg://postgres:password@localhost/weather"
    let pg = importDefault<obj> "pg"

    let getSql dataType date = 
        match dataType with
        | Wind -> sprintf "SELECT * FROM wind WHERE to_char(created_date, 'yyyy-MM-DD')='%O' ORDER BY created_date ASC" date
        | Temp -> sprintf "SELECT * FROM temperatureHumidity WHERE to_char(created_date, 'yyyy-MM-DD')='%O' ORDER BY created_date DESC" date
        | Dates -> sprintf "SELECT to_char(created_date, 'yyyy-MM-DD') from wind group by to_char(created_date, 'yyyy-MM-DD') union select to_char(created_date, 'yyyy-MM-DD') from temperatureHumidity group by to_char(created_date, 'yyyy-MM-DD') order by to_char desc";
        | Rain -> sprintf "SELECT * FROM rain WHERE to_char(created_date, 'yyyy-MM-DD')='%O' ORDER BY created_date DESC LIMIT 50" date


    let getData (sql:string, callBack:Action<obj>) =    
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
                    | Some x -> callBack.Invoke((sprintf "An error occurred while querying: %O" err))
                    | None -> callBack.Invoke(result?rows)) 
            |> ignore)    
