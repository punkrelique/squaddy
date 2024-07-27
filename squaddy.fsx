open System
open System.Net.Http
open System.Net.Http.Json

let args = fsi.CommandLineArgs[1..]

if args.Length = 0 then
    printf "Username was not provided"
    Environment.Exit(-1)
    
let username = args[0]

let caseComparison =
    match Array.tryItem 1 args with
    | Some value when value = "ignorecase" -> StringComparison.OrdinalIgnoreCase
    | Some value when value = "followcase" -> StringComparison.Ordinal
    | _                                    -> StringComparison.Ordinal
    
let findString =
    match Array.tryItem 2 args with
    | Some value when value = "contains" -> fun (str1: string) (str2: string) -> str1.Contains(str2, caseComparison)
    | Some value when value = "exactly"  -> fun (str1: string) (str2: string) -> str1.Equals(str2, caseComparison)
    | _                                  -> fun (str1: string) (str2: string) -> str1.Equals(str2, caseComparison)

let steamApiKey = Environment.GetEnvironmentVariable("SQUADDY_STEAM_API_KEY")
if String.IsNullOrEmpty(steamApiKey) then
    printfn "SQUADDY_STEAM_API_KEY environment variable is not set"
    Environment.Exit(-1)
    
type mySquadStats = { data: seq<{| lastName: string; steamID: string |}>; successStatus: string; successMessage: string }
let mySquadStatsUrlTemplate username = $"https://mysquadstats.com/api/players?search={username}"
let getPlayerStats = task {
    use client = new HttpClient()
    client.DefaultRequestHeaders.Referrer <- Uri("https://mysquadstats.com/")
    let! response = client.GetAsync(mySquadStatsUrlTemplate username)
    let! content = response.Content.ReadFromJsonAsync<mySquadStats>()
    
    return content.data
}

let playerStats = getPlayerStats |> Async.AwaitTask |> Async.RunSynchronously

let steamStatsUrlTemplate steamId = $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={steamApiKey}&steamid={steamId}&format=json&&appids_filter[0]=393380"
type steamInfo = { response: {| game_count: int; games: Option<seq<{| appid: int; playtime_2weeks: int; playtime_forever: int |}>> |} }
let steamClient = new HttpClient()
let getSteamUserGameStats steamId = task {
    let! response = steamClient.GetAsync(steamStatsUrlTemplate steamId)
    let! content = response.Content.ReadFromJsonAsync<steamInfo>()
    
    return content
}

playerStats
    |> Seq.where (fun x -> findString (x.lastName.TrimStart()) username)
    |> Seq.iteri (fun i squadStatsUser -> 
        let hours = 
            (getSteamUserGameStats squadStatsUser.steamID
            |> Async.AwaitTask
            |> Async.RunSynchronously).response.games
            |> function
                | Some games -> games |> Seq.tryLast |> Option.map (fun x -> x.playtime_forever / 60) |> Option.defaultValue 0
                | None -> 0
        
        printfn "%-2d %-20s %-60s %-10d %s"
            (i + 1)
            (squadStatsUser.lastName.TrimStart())
            ("https://steamcommunity.com/profiles/" + squadStatsUser.steamID)
            hours
            (if hours = 0 then "Private" else "Visible")
    )
